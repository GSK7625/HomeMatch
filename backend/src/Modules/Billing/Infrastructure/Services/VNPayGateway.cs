using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Application.Models;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Billing.Infrastructure.Services;

public class VNPayGateway(IOptions<VNPayOptions> options) : IVNPayGateway
{
    private readonly VNPayOptions _options = options.Value;

    public bool IsSandboxSimulatorMode =>
        string.IsNullOrWhiteSpace(_options.TmnCode) ||
        _options.TmnCode.StartsWith("your_", StringComparison.OrdinalIgnoreCase);

    public string CreatePaymentUrl(VNPayPaymentRequest request)
    {
        if (IsSandboxSimulatorMode)
        {
            var simulatorParams = new Dictionary<string, string>
            {
                ["paymentCode"] = request.PaymentCode,
                ["amount"] = request.Amount.ToString(CultureInfo.InvariantCulture),
                ["orderInfo"] = request.OrderInfo,
            };
            var simQuery = string.Join("&", simulatorParams.Select(kv =>
                $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
            return $"/billing/sandbox-pay?{simQuery}";
        }

        var rawData = new SortedDictionary<string, string>(new VnPayComparer())
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = _options.Command,
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = request.CreatedAt.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = _options.CurrCode,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(request.ClientIpAddress) ? "127.0.0.1" : request.ClientIpAddress,
            ["vnp_Locale"] = _options.Locale,
            ["vnp_OrderInfo"] = request.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = _options.ReturnUrl,
            ["vnp_TxnRef"] = request.PaymentCode,
            ["vnp_ExpireDate"] = request.ExpiredAt.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss")
        };

        var queryString = new StringBuilder();
        var signData = new StringBuilder();

        foreach (var (key, value) in rawData)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            var encodedKey = WebUtility.UrlEncode(key);
            var encodedValue = WebUtility.UrlEncode(value);

            if (queryString.Length > 0)
            {
                queryString.Append('&');
                signData.Append('&');
            }

            queryString.Append($"{encodedKey}={encodedValue}");
            signData.Append($"{encodedKey}={encodedValue}");
        }

        var secureHash = HmacSha512(_options.HashSecret, signData.ToString());
        queryString.Append($"&vnp_SecureHash={secureHash}");

        return $"{_options.BaseUrl}?{queryString}";
    }

    public string CreateSandboxCallbackQueryString(
        string paymentCode,
        long amountVnd,
        string orderInfo,
        bool isSuccess,
        string bankCode = "NCB")
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
        var responseCode = isSuccess ? "00" : "24";
        var rawData = new SortedDictionary<string, string>(new VnPayComparer())
        {
            ["vnp_Amount"] = (amountVnd * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_BankCode"] = bankCode,
            ["vnp_CardType"] = "ATM",
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_PayDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_TransactionNo"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_TransactionStatus"] = responseCode,
            ["vnp_TxnRef"] = paymentCode
        };

        var queryString = new StringBuilder();
        var signData = new StringBuilder();

        foreach (var (key, value) in rawData)
        {
            if (string.IsNullOrEmpty(value)) continue;
            var encodedKey = WebUtility.UrlEncode(key);
            var encodedValue = WebUtility.UrlEncode(value);
            if (queryString.Length > 0)
            {
                queryString.Append('&');
                signData.Append('&');
            }
            queryString.Append($"{encodedKey}={encodedValue}");
            signData.Append($"{encodedKey}={encodedValue}");
        }

        var secureHash = HmacSha512(_options.HashSecret, signData.ToString());
        queryString.Append($"&vnp_SecureHash={secureHash}");
        return queryString.ToString();
    }

    public VNPayCallbackResult ProcessCallback(IEnumerable<KeyValuePair<string, string>> queryParameters)
    {
        var rawParams = queryParameters.ToList();
        var rawDataString = string.Join("&", rawParams.Select(kv => $"{kv.Key}={kv.Value}"));

        string? receivedHash = null;
        var validData = new SortedDictionary<string, string>(new VnPayComparer());

        foreach (var (key, value) in rawParams)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                receivedHash = value;
            }
            else if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                     !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                validData[key] = value;
            }
        }

        if (string.IsNullOrWhiteSpace(receivedHash))
        {
            return new VNPayCallbackResult(
                IsValidSignature: false,
                IsSuccess: false,
                PaymentCode: validData.GetValueOrDefault("vnp_TxnRef", string.Empty),
                GatewayTransactionNo: null,
                BankCode: null,
                CardType: null,
                ResponseCode: "97",
                TransactionStatus: null,
                AmountInVnd: 0,
                RawData: rawDataString,
                ErrorMessage: "Thiếu chữ ký vnp_SecureHash từ VNPay.");
        }

        var signData = new StringBuilder();
        foreach (var (key, value) in validData)
        {
            if (signData.Length > 0)
            {
                signData.Append('&');
            }

            signData.Append($"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(value)}");
        }

        var computedHash = HmacSha512(_options.HashSecret, signData.ToString());
        var isValidSignature = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);

        var paymentCode = validData.GetValueOrDefault("vnp_TxnRef", string.Empty);
        var gatewayTxnNo = validData.GetValueOrDefault("vnp_TransactionNo");
        var bankCode = validData.GetValueOrDefault("vnp_BankCode");
        var cardType = validData.GetValueOrDefault("vnp_CardType");
        var responseCode = validData.GetValueOrDefault("vnp_ResponseCode", "99");
        var transactionStatus = validData.GetValueOrDefault("vnp_TransactionStatus");

        long amountInVnd = 0;
        if (long.TryParse(validData.GetValueOrDefault("vnp_Amount"), out var rawAmount))
        {
            amountInVnd = rawAmount / 100;
        }

        var isSuccess = isValidSignature &&
                        responseCode == "00" &&
                        (transactionStatus == null || transactionStatus == "00");

        return new VNPayCallbackResult(
            IsValidSignature: isValidSignature,
            IsSuccess: isSuccess,
            PaymentCode: paymentCode,
            GatewayTransactionNo: gatewayTxnNo,
            BankCode: bankCode,
            CardType: cardType,
            ResponseCode: responseCode,
            TransactionStatus: transactionStatus,
            AmountInVnd: amountInVnd,
            RawData: rawDataString,
            ErrorMessage: isValidSignature ? null : "Chữ ký bảo mật không khớp.");
    }

    private static string HmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private sealed class VnPayComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
