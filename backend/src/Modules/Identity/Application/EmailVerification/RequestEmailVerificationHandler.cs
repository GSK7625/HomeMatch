namespace HomeMatch.Modules.Identity.Application.EmailVerification;

public sealed class RequestEmailVerificationHandler(IEmailVerificationStore store, IEmailVerificationDelivery delivery)
{
    public async Task<VerificationResult> HandleAsync(VerificationSubject subject, CancellationToken cancellationToken)
    {
        var reserved = await store.ReserveAsync(subject, delivery.IsAvailable, delivery.SenderKey, cancellationToken);
        if (reserved.Outcome != VerificationOutcome.Reserved) return reserved;
        var pending = reserved.Reservation ?? throw new InvalidOperationException("Missing verification reservation.");
        // Reserve committed before the network call; cancellation/crash leaves an unusable Pending lease.
        var outcome = await delivery.SendAsync(pending, cancellationToken);
        return await store.CompleteAsync(subject, pending.Id, outcome, cancellationToken);
    }
}
