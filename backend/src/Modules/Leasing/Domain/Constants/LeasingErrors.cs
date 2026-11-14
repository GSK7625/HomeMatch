using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Domain.Constants;

public static class LeasingErrors
{
    public static readonly Error AuthenticationRequired = new(
        "Leasing.AuthenticationRequired",
        "Yêu cầu xác thực tài khoản để thực hiện thao tác này.");

    public static readonly Error NotFound = new(
        "Leasing.NotFound",
        "Không tìm thấy hợp đồng thuê yêu cầu.");

    public static readonly Error Forbidden = new(
        "Leasing.Forbidden",
        "Bạn không có quyền truy cập hoặc thực hiện thao tác trên hợp đồng này.");

    public static readonly Error RoomNotFound = new(
        "Leasing.RoomNotFound",
        "Không tìm thấy thông tin phòng cho thuê hoặc phòng không tồn tại.");

    public static readonly Error RoomNotOwnedByLandlord = new(
        "Leasing.RoomNotOwnedByLandlord",
        "Phòng cho thuê không thuộc quyền quản lý hoặc sở hữu của Chủ nhà.");

    public static readonly Error TenantNotFound = new(
        "Leasing.TenantNotFound",
        "Không tìm thấy thông tin tài khoản người thuê nhà.");

    public static readonly Error SelfLeasingNotAllowed = new(
        "Leasing.SelfLeasingNotAllowed",
        "Chủ nhà không thể tự tạo hợp đồng thuê phòng cho chính tài khoản của mình.");

    public static readonly Error InvalidDateRange = new(
        "Leasing.InvalidDateRange",
        "Ngày bắt đầu thuê phải trước ngày kết thúc thuê.");

    public static readonly Error InvalidMonthlyRent = new(
        "Leasing.InvalidMonthlyRent",
        "Giá tiền thuê nhà hàng tháng phải lớn hơn 0 VNĐ.");

    public static readonly Error InvalidDepositAmount = new(
        "Leasing.InvalidDepositAmount",
        "Số tiền đặt cọc theo thỏa thuận không được là số âm.");

    public static readonly Error InvalidBillingDueDay = new(
        "Leasing.InvalidBillingDueDay",
        "Ngày thu tiền định kỳ hàng tháng phải nằm trong khoảng từ ngày 1 đến ngày 28.");

    public static readonly Error ScheduleConflict = new(
        "Leasing.ScheduleConflict",
        "Phòng này đã có hợp đồng thuê hoặc đang được giữ chỗ trong khoảng thời gian yêu cầu.");

    public static Error InvalidStatusTransition(string currentStatus, string targetStatus) => new(
        "Leasing.InvalidStatusTransition",
        $"Không thể chuyển trạng thái hợp đồng từ '{currentStatus}' sang '{targetStatus}'.");

    public static readonly Error CannotModifyNonDraft = new(
        "Leasing.CannotModifyNonDraft",
        "Chỉ có thể chỉnh sửa nội dung hợp đồng khi hợp đồng đang ở trạng thái Bản nháp (DRAFT).");

    public static readonly Error SignatureExpired = new(
        "Leasing.SignatureExpired",
        "Thời hạn ký hợp đồng trực tuyến đã kết thúc, không thể thực hiện ký.");

    public static readonly Error AlreadySignedByParty = new(
        "Leasing.AlreadySignedByParty",
        "Bạn đã ký xác nhận hợp đồng này trước đó, không thể ký lặp lại.");

    public static readonly Error UnauthorizedSigner = new(
        "Leasing.UnauthorizedSigner",
        "Tài khoản hiện tại không phải Chủ nhà hoặc Người thuê đứng tên trên hợp đồng này.");

    public static readonly Error SignatureEvidenceRequired = new(
        "Leasing.SignatureEvidenceRequired",
        "Bắt buộc phải có mã băm tài liệu ký (signed document hash) và thời điểm ký hợp lệ.");

    public static readonly Error InvalidOtp = new(
        "Leasing.InvalidOtp",
        "Mã OTP xác thực không chính xác hoặc đã hết hạn hiệu lực.");

    public static readonly Error DocumentHashMismatch = new(
        "Leasing.DocumentHashMismatch",
        "Mã băm nội dung hợp đồng không khớp với phiên bản bên kia đã ký.");

    public static readonly Error UserEmailNotFound = new(
        "Leasing.UserEmailNotFound",
        "Không tìm thấy địa chỉ email đã xác thực của người dùng để gửi mã OTP.");

    public static readonly Error HandoverNotPermitted = new(
        "Leasing.HandoverNotPermitted",
        "Chỉ có thể xác nhận bàn giao khi hợp đồng đã hết hạn thuê hoặc chấm dứt trước hạn.");

    public static readonly Error OnlyLandlordCanConfirmHandover = new(
        "Leasing.OnlyLandlordCanConfirmHandover",
        "Chỉ Chủ nhà quản lý hợp đồng mới có quyền xác nhận bàn giao phòng.");

    public static readonly Error InvalidHandoverDate = new(
        "Leasing.InvalidHandoverDate",
        "Ngày bàn giao phòng thực tế không thể trước ngày bắt đầu thuê.");

    public static readonly Error CannotCancelActiveContract = new(
        "Leasing.CannotCancelActiveContract",
        "Không thể hủy hợp đồng khi hai bên đã ký xong hoặc hợp đồng đang có hiệu lực. Vui lòng thực hiện quy trình chấm dứt trước hạn.");

    public static readonly Error CannotTerminateNonActiveContract = new(
        "Leasing.CannotTerminateNonActiveContract",
        "Chỉ có thể chấm dứt trước hạn khi hợp đồng đang ở trạng thái Hoạt động (ACTIVE).");

    public static readonly Error InvalidDepositTransactionType = new(
        "Leasing.InvalidDepositTransactionType",
        "Loại giao dịch tiền cọc không hợp lệ (chỉ hỗ trợ RECEIVED, REFUNDED, DEDUCTED).");

    public static readonly Error InvalidDepositTransactionAmount = new(
        "Leasing.InvalidDepositTransactionAmount",
        "Số tiền giao dịch cọc phải lớn hơn 0 VNĐ.");

    public static readonly Error DeductionReasonRequired = new(
        "Leasing.DeductionReasonRequired",
        "Giao dịch khấu trừ tiền cọc bắt buộc phải cung cấp lý do cụ thể.");

    public static readonly Error DepositExceedsAgreed = new(
        "Leasing.DepositExceedsAgreed",
        "Tổng số tiền cọc thực nhận không được vượt quá số tiền cọc đã thỏa thuận trên hợp đồng.");

    public static readonly Error InsufficientDepositBalance = new(
        "Leasing.InsufficientDepositBalance",
        "Số tiền hoàn trả hoặc khấu trừ vượt quá số dư tiền cọc hiện có của hợp đồng.");

    public static readonly Error HandoverAlreadyConfirmed = new(
        "Leasing.HandoverAlreadyConfirmed",
        "Hợp đồng này đã được xác nhận bàn giao phòng trước đó, không thể xác nhận lặp lại.");

    public static Error DepositNotPermittedInStatus(string currentStatus, string transactionType) => new(
        "Leasing.DepositNotPermittedInStatus",
        $"Không thể thực hiện giao dịch cọc '{transactionType}' khi hợp đồng đang ở trạng thái '{currentStatus}'.");
}
