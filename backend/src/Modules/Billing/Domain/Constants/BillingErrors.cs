using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Domain.Constants;

public static class BillingErrors
{
    public static readonly Error AuthenticationRequired = new(
        "Billing.AuthenticationRequired",
        "Yêu cầu xác thực tài khoản để thực hiện thao tác này.");

    public static readonly Error Forbidden = new(
        "Billing.Forbidden",
        "Bạn không có quyền thực hiện thao tác hoặc truy cập dữ liệu này.");

    public static readonly Error InvoiceNotFound = new(
        "Billing.InvoiceNotFound",
        "Không tìm thấy thông tin hóa đơn yêu cầu.");

    public static readonly Error InvoiceAlreadyPaid = new(
        "Billing.InvoiceAlreadyPaid",
        "Hóa đơn này đã được thanh toán thành công.");

    public static readonly Error InvoiceCancelled = new(
        "Billing.InvoiceCancelled",
        "Hóa đơn này đã bị hủy.");

    public static readonly Error CannotCancelPaidInvoice = new(
        "Billing.CannotCancelPaidInvoice",
        "Hóa đơn đã thanh toán không được phép hủy.");

    public static readonly Error CannotCancelInvoiceWithPendingPayment = new(
        "Billing.CannotCancelInvoiceWithPendingPayment",
        "Không thể hủy hóa đơn khi đang có giao dịch thanh toán PENDING.");

    public static readonly Error DuplicatePeriodInvoice = new(
        "Billing.DuplicatePeriodInvoice",
        "Hợp đồng này đã có hóa đơn trong kỳ thanh toán được chỉ định và chưa bị hủy.");

    public static readonly Error ContractNotActive = new(
        "Billing.ContractNotActive",
        "Hợp đồng phải ở trạng thái ACTIVE mới được tạo hóa đơn định kỳ.");

    public static readonly Error ContractExpiredPeriodInvalid = new(
        "Billing.ContractExpiredPeriodInvalid",
        "Kỳ hóa đơn không thuộc thời gian thuê hợp lệ của hợp đồng.");

    public static readonly Error PaymentNotFound = new(
        "Billing.PaymentNotFound",
        "Không tìm thấy thông tin giao dịch thanh toán.");

    public static readonly Error PendingPaymentAlreadyExists = new(
        "Billing.PendingPaymentAlreadyExists",
        "Hóa đơn đang có giao dịch thanh toán PENDING đang xử lý.");

    public static readonly Error InvalidPaymentAmount = new(
        "Billing.InvalidPaymentAmount",
        "Số tiền thanh toán phải bằng chính xác tổng giá trị hóa đơn.");

    public static readonly Error InvalidGatewaySignature = new(
        "Billing.InvalidGatewaySignature",
        "Chữ ký phản hồi từ cổng thanh toán không hợp lệ.");

    public static readonly Error PaymentAlreadyProcessed = new(
        "Billing.PaymentAlreadyProcessed",
        "Giao dịch thanh toán này đã được ghi nhận trước đó.");
}
