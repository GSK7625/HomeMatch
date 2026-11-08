using System.ComponentModel.DataAnnotations;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

internal sealed class PropertyRequestFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.FirstOrDefault(argument =>
            argument is CreatePropertyRequest or UpdatePropertyRequest
                or CreateRoomRequest or UpdateRoomRequest or SetRoomStatusRequest
                or SetPropertyTariffsRequest);
        if (request is not null)
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            {
                var details = errors.SelectMany(error => error.MemberNames.DefaultIfEmpty(string.Empty)
                    .Select(field => new ErrorDetail(field, error.ErrorMessage ?? "Dữ liệu không hợp lệ."))).ToArray();
                return Results.BadRequest(ApiResponse<object>.Failure(
                    Error.Validation("Request.Invalid", "Thông tin căn không hợp lệ.", details)));
            }
        }

        return await next(context);
    }
}
