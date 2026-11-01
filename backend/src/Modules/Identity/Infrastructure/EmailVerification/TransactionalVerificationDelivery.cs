using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Shared.Contracts.Notifications;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed class TransactionalVerificationDelivery(ITransactionalEmailSender sender) : IEmailVerificationDelivery
{
    public bool IsAvailable => sender.IsAvailable;
    public string SenderKey => sender.SenderKey;
    public async Task<VerificationDeliveryOutcome> SendAsync(PendingVerification reservation, CancellationToken cancellationToken)
    {
        var message = new TransactionalEmail(reservation.Email, "HomeMatch — Xác minh email",
            $"Mã xác minh email HomeMatch của bạn: {reservation.Code}\nMã có hiệu lực tối đa 5 phút kể từ lúc yêu cầu.\nKhông chia sẻ mã này với người khác. Nếu không yêu cầu, bạn có thể bỏ qua thư.");
        return await sender.SendAsync(message, cancellationToken) switch
        {
            EmailSubmissionOutcome.Accepted => VerificationDeliveryOutcome.Accepted,
            EmailSubmissionOutcome.Rejected => VerificationDeliveryOutcome.Rejected,
            EmailSubmissionOutcome.Unknown => VerificationDeliveryOutcome.Unknown,
            _ => VerificationDeliveryOutcome.Unavailable
        };
    }
}
