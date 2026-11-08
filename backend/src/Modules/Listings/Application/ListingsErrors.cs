using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application;

public static class ListingsErrors
{
    public static readonly Error ListingNotFound = new("Listing.NotFound", "Không tìm thấy tin đăng.");
    public static readonly Error RoomNotFound = new("Room.NotFound", "Không tìm thấy phòng thuộc tài khoản này.");
    public static readonly Error DuplicateSlug = new("Listing.DuplicateSlug", "Đường dẫn tin đã tồn tại.");
    public static readonly Error InvalidInput = new("Listing.InvalidInput", "Thông tin tin đăng không hợp lệ.");
    public static readonly Error InvalidCoordinates = new("Listing.InvalidCoordinates", "Tọa độ phải có cả kinh độ và vĩ độ hợp lệ.");
    public static readonly Error InvalidStatus = new("Listing.InvalidStatus", "Trạng thái tin đăng không hợp lệ.");
    public static readonly Error RoomUnavailable = new("Listing.RoomUnavailable", "Phòng phải còn trống và thuộc chủ nhà.");
    public static readonly Error Incomplete = new("Listing.Incomplete", "Tin cần tọa độ và ít nhất một ảnh đại diện.");
    public static readonly Error MissingTariffs = new("Listing.MissingTariffs", "Cần có đơn giá điện và nước đang hiệu lực.");
    public static readonly Error RoomAlreadyPublished = new("Listing.RoomAlreadyPublished", "Phòng đã có tin đang đăng.");
    public static readonly Error ImageNotFound = new("Listing.ImageNotFound", "Không tìm thấy ảnh của tin.");
    public static readonly Error InvalidImage = new("Listing.InvalidImage", "Thông tin ảnh không hợp lệ.");
    public static readonly Error LastImage = new("Listing.LastImage", "Ẩn tin trước khi xóa ảnh cuối cùng.");
    public static readonly Error ImageUploadUnavailable = new("Listing.ImageUploadUnavailable", "Cần tích hợp dịch vụ lưu ảnh trước khi thêm ảnh ở môi trường này.");
    public static readonly Error InvalidLimit = new("Listing.InvalidLimit", "Giới hạn phải từ 1 đến 50.");
}

