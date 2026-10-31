using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests.Infrastructure;

internal static class OtpTestHelper
{
    public static async Task VerifyRegistrationAsync(WebApplicationFactory<Program> factory, string email,
        Func<Guid, string, Task<HttpResponseMessage>> submit)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var challenge = await db.PublicOtpChallenges.Where(x => x.Email == email.ToUpperInvariant() && x.Purpose == "Registration")
            .OrderByDescending(x => x.CreatedAt).FirstAsync();
        var code = scope.ServiceProvider.GetRequiredService<CapturingEmailSender>().LatestCode(email);
        using var response = await submit(challenge.Id, code);
        response.EnsureSuccessStatusCode();
    }
}
