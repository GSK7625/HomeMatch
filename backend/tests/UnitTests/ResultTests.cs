using HomeMatch.Shared.Common;

namespace HomeMatch.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("Resource.Conflict", "Tài nguyên đã tồn tại");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void TypedResult_Success_ShouldReturnValue()
    {
        // Act
        var result = Result.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void TypedResult_Failure_AccessingValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var error = new Error("Resource.NotFound", "Không tìm thấy");
        var result = Result.Failure<int>(error);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Validation_ShouldCreateErrorWithDetails()
    {
        // Arrange
        var details = new[]
        {
            new ErrorDetail("name", "Tên không được để trống")
        };

        // Act
        var error = Error.Validation("General.Validation", "Validation failed", details);

        // Assert
        Assert.Equal("General.Validation", error.Code);
        Assert.NotNull(error.Details);
        Assert.Single(error.Details);
        Assert.Equal("name", error.Details.First().Field);
    }
}
