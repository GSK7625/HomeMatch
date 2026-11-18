namespace HomeMatch.Modules.Billing.Application.Commands.ProcessPaymentCallback;

public sealed record ProcessPaymentCallbackCommand(
    IEnumerable<KeyValuePair<string, string>> QueryParameters);
