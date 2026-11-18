using HomeMatch.Modules.Billing.Application.Models;

namespace HomeMatch.Modules.Billing.Application.Interfaces;

public interface IVNPayGateway
{
    string CreatePaymentUrl(VNPayPaymentRequest request);
    VNPayCallbackResult ProcessCallback(IEnumerable<KeyValuePair<string, string>> queryParameters);
}
