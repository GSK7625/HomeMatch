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
  - `500 Internal Server Error`: Lỗi hệ thống ngoài tầm kiểm soát.

## 2. Cấu trúc Response chuẩn (`ApiResponse<T>`)
```json
{
  "isSuccess": true,
  "data": { ... },
  "error": null,
  "timestamp": "2026-10-24T07:00:00Z"
}
```

Trường hợp lỗi:
```json
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "Listing.NotFound",
    "message": "Không tìm thấy tin đăng yêu cầu"
  },
  "timestamp": "2026-10-24T07:00:00Z"
}
```
