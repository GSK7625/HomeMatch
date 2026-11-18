namespace HomeMatch.Modules.Billing.Application.Commands.ProcessInvoiceOverdue;

public sealed record ProcessInvoiceOverdueResult(
    int OverdueInvoicesCount,
    int ExpiredPaymentsCount);
