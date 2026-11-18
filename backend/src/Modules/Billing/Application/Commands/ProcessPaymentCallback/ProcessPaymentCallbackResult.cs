using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Models;

namespace HomeMatch.Modules.Billing.Application.Commands.ProcessPaymentCallback;

public sealed record ProcessPaymentCallbackResult(
    bool IsSuccess,
    VNPayIpnResponse IpnResponse,
    PaymentDto? Payment,
    string? ErrorMessage = null);
