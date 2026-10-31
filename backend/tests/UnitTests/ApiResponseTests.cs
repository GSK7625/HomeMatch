using System.Text.Json;
using HomeMatch.Shared.Common;

namespace HomeMatch.UnitTests;

public class ApiResponseTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulApiResponse()
    {
        // Arrange
        var testData = new { Name = "Test Room", Price = 3500000 };

        // Act
        var response = ApiResponse.Success(testData);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(testData, response.Data);
        Assert.Null(response.Error);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void Failure_ShouldCreateFailedApiResponse()
    {
        // Arrange
        var error = new Error("Property.NotFound", "Không tìm thấy phòng trọ yêu cầu");

        // Act
        var response = ApiResponse.Failure<string>(error);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Null(response.Data);
        Assert.NotNull(response.Error);
        Assert.Equal("Property.NotFound", response.Error.Code);
        Assert.Equal("Không tìm thấy phòng trọ yêu cầu", response.Error.Message);
        Assert.Null(response.Error.Details);
    }

    [Fact]
    public void Failure_WithValidationDetails_ShouldContainErrorDetails()
    {
        // Arrange
        var details = new List<ErrorDetail>
        {
            new("title", "Tiêu đề không được để trống"),
            new("price", "Giá phòng phải lớn hơn 0")
        };
        var error = Error.Validation("Property.ValidationFailed", "Dữ liệu không hợp lệ", details);

        // Act
        var response = ApiResponse.Failure<object>(error);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.NotNull(response.Error);
        Assert.NotNull(response.Error.Details);
        Assert.Equal(2, response.Error.Details.Count);
    }

    [Fact]
    public void Serialization_ShouldProduceCamelCaseProperties()
    {
        // Arrange
        var details = new List<ErrorDetail> { new("email", "Email không đúng định dạng") };
        var error = Error.Validation("Auth.InvalidInput", "Lỗi dữ liệu", details);
        var response = ApiResponse.Failure<string>(error);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var json = JsonSerializer.Serialize(response, options);

        // Assert
        Assert.Contains("\"isSuccess\":false", json);
        Assert.Contains("\"code\":\"Auth.InvalidInput\"", json);
        Assert.Contains("\"field\":\"email\"", json);
        Assert.Contains("\"timestamp\":", json);
    }
}
