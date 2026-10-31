using Microsoft.EntityFrameworkCore.Migrations;
namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations;

public partial class ConvertIdentityToBigInt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(ReadSql("Up"));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(ReadSql("Down"));
    private static string ReadSql(string direction)
    {
        var assembly = typeof(ConvertIdentityToBigInt).Assembly;
        var name = assembly.GetManifestResourceNames().Single(value => value.EndsWith($"ConvertIdentityToBigInt.{direction}.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Missing conversion SQL.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
