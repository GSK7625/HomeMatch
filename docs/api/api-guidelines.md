# API Guidelines - HomeMatch

## 1. Chuẩn thiết kế RESTful
- Base URL cho API: `/api/v1/<resource>`
- Format dữ liệu: `application/json`
- Sử dụng chuẩn HTTP Status Code:
  - `200 OK`: Thành công với response payload.
  - `201 Created`: Tạo mới thành công (kèm header `Location` nếu phù hợp).
  - `204 No Content`: Cập nhật hoặc xóa thành công, không trả về body.
  - `400 Bad Request`: Lỗi validation đầu vào.
  - `401 Unauthorized`: Chưa xác thực JWT.
  - `403 Forbidden`: Không có quyền truy cập tài nguyên.
  - `404 Not Found`: Không tìm thấy tài nguyên.
  - `409 Conflict`: Xung đột dữ liệu hoặc trạng thái nghiệp vụ.
  - `500 Internal Server Error`: Lỗi hệ thống ngoài tầm kiểm soát.

## 2. Cấu trúc Response chuẩn (`ApiResponse<T>`)
```json
{
  "isSuccess": true,
  "data": { ... },
  "error": null,
  "timestamp": "2026-10-31T09:00:00Z"
}
```

Trường hợp lỗi:
```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "Listing.NotFound",
    "message": "Không tìm thấy tin đăng yêu cầu",
    "details": [
      { "field": "price", "message": "Giá thuê phải lớn hơn 0" }
    ]
  },
  "timestamp": "2026-10-31T09:00:00Z"
}
```

- Header phản hồi luôn có `X-Trace-Id` để đối chiếu log.
- `204 No Content` không có response body.
- Các endpoint hệ thống (Health check, OpenAPI, Scalar) được miễn bọc `ApiResponse<T>`.
- Tuyệt đối không trả `exception.Message` hoặc StackTrace thô ra client.

## 3. Phân trang & Sắp xếp (Pagination & Sorting)
- Chuẩn query params: `page=1`, `pageSize=20` (tối đa 100).
- Response phân trang gồm:
```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 105
}
```
- Sắp xếp chỉ cho phép trên danh sách whitelist các trường hợp lệ và luôn có khóa phụ ổn định.

## 4. Bảo mật & Phân quyền (Security & Authorization)
- Xác thực qua JWT Bearer token (`Authorization: Bearer <token>`).
- Kiểm tra Policy và bắt buộc kiểm tra **quyền sở hữu tài nguyên (Resource Ownership)** (Landlord chỉ thao tác trên tài sản/tin đăng thuộc quyền sở hữu của mình).

