using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Viewings.Presentation.Endpoints.Requests;

public sealed record BookViewingAppointmentRequest(
    long ListingId,
    DateTimeOffset AppointmentTime,
    int DurationMinutes = 30,
    string? Notes = null,
    bool IsVirtualViewing = false)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (ListingId <= 0)
        {
            errors.Add(new ErrorDetail("listingId", "Mã tin đăng phòng cho thuê (listingId) phải lớn hơn 0."));
        }

        if (AppointmentTime == default)
        {
            errors.Add(new ErrorDetail("appointmentTime", "Thời gian hẹn xem phòng (appointmentTime) không hợp lệ."));
        }

        if (DurationMinutes < 15 || DurationMinutes > 180)
        {
            errors.Add(new ErrorDetail("durationMinutes", "Thời lượng xem phòng phải nằm trong khoảng từ 15 đến 180 phút."));
        }

        if (Notes is not null && Notes.Length > 500)
        {
            errors.Add(new ErrorDetail("notes", "Ghi chú không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}

public sealed record ConfirmViewingAppointmentRequest(
    string? VirtualRoomUrl = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (!string.IsNullOrWhiteSpace(VirtualRoomUrl) && VirtualRoomUrl.Length > 500)
        {
            errors.Add(new ErrorDetail("virtualRoomUrl", "Đường dẫn phòng xem online không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}

public sealed record CancelViewingAppointmentRequest(
    string? Reason = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        var errors = new List<ErrorDetail>();

        if (Reason is not null && Reason.Length > 500)
        {
            errors.Add(new ErrorDetail("reason", "Lý do hủy lịch không được vượt quá 500 ký tự."));
        }

        return errors;
    }
}
