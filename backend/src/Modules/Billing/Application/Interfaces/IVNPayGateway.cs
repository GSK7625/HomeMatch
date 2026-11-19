using HomeMatch.Modules.Billing.Application.Models;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface IVNPayGateway
{
    bool IsSandboxSimulatorMode { get; }
    string CreatePaymentUrl(VNPayPaymentRequest request);
    string CreateSandboxCallbackQueryString(string paymentCode, long amountVnd, string orderInfo, bool isSuccess, string bankCode = "NCB");
    VNPayCallbackResult ProcessCallback(IEnumerable<KeyValuePair<string, string>> queryParameters);
}
