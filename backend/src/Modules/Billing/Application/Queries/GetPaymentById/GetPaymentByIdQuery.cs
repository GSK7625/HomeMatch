namespace HomeMatch.Modules.Billing.Application.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(
    long PaymentId,
    long UserId,
    bool IsAdmin);
