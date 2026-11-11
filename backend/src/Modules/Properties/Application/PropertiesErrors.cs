using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application;

public static class PropertiesErrors
{
    public static readonly Error PropertyNotFound = new("Property.NotFound", "Không tìm thấy căn thuộc tài khoản này.");
    public static readonly Error InvalidProperty = new("Property.InvalidInput", "Thông tin căn không hợp lệ.");
    public static readonly Error InvalidAddress = new("Property.InvalidAddress", "Phường/xã không thuộc tỉnh/thành phố đã chọn.");
    public static readonly Error RoomNotFound = new("Room.NotFound", "Không tìm thấy phòng trong căn này.");
    public static readonly Error InvalidRoom = new("Room.InvalidInput", "Thông tin phòng không hợp lệ.");
    public static readonly Error DuplicateRoomNumber = new("Room.DuplicateNumber", "Số phòng đã tồn tại trong căn này.");
    public static readonly Error InvalidRoomStatus = new("Room.InvalidStatus", "Trạng thái phòng không hợp lệ.");
    public static readonly Error InvalidAmenities = new("Room.InvalidAmenities", "Danh sách tiện ích không hợp lệ.");
    public static readonly Error TariffNotFound = new("Tariff.NotFound", "Không tìm thấy đơn giá.");
    public static readonly Error InvalidTariff = new("Tariff.InvalidInput", "Loại phí, cách tính hoặc đơn giá không hợp lệ.");
    public static readonly Error DuplicateTariffType = new("Tariff.DuplicateType", "Danh sách biểu giá chứa loại phí trùng lặp.");
    public static readonly Error DuplicateTariffDate = new("Tariff.DuplicateDate", "Đã có đơn giá cùng loại và ngày hiệu lực trong phạm vi này.");
    public static readonly Error TariffAlreadyEffective = new("Tariff.AlreadyEffective", "Đơn giá đã có hiệu lực. Hãy tạo mức giá mới với ngày hiệu lực mới.");
    public static readonly Error InvalidTariffDate = new("Tariff.InvalidDate", "Chỉ có thể sửa đơn giá chưa có hiệu lực.");
}
