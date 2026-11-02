using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Npgsql;

namespace HomeMatch.IntegrationTests.Infrastructure;

/// <summary>Uses installed PostgreSQL binaries; never stops or edits an existing database service.</summary>
internal sealed class TemporaryPostgresCluster : IAsyncDisposable
{
    private readonly string _binDirectory = FindBinaryDirectory();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "homematch-identity-tests-" + Guid.NewGuid().ToString("N"));
    private string DataDirectory => Path.Combine(_directory, "data");

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        Directory.CreateDirectory(_directory);
        var passwordFile = Path.Combine(_directory, "initial-password");
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await File.WriteAllTextAsync(passwordFile, password);

        await RunAsync("initdb", "--pgdata", DataDirectory, "--username", "homematch_test_admin",
            "--pwfile", passwordFile, "--auth-host=scram-sha-256", "--auth-local=scram-sha-256",
            "--encoding=UTF8", "--locale=C", "--no-instructions");
        File.Delete(passwordFile);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        await RunAsync("pg_ctl", "-D", DataDirectory, "-l", Path.Combine(_directory, "server.log"),
            "-o", $"-h 127.0.0.1 -p {port}", "-w", "-t", "30", "start");

        ConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1", Port = port, Database = "postgres",
            Username = "homematch_test_admin", Password = password,
            Pooling = false, Timeout = 10, CommandTimeout = 30,
            IncludeErrorDetail = false, ApplicationName = "HomeMatch.Identity.Tests"
        }.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (File.Exists(Path.Combine(DataDirectory, "postmaster.pid")))
        {
            await RunAsync("pg_ctl", "-D", DataDirectory, "-m", "fast", "-w", "-t", "30", "stop");
        }

        if (!Directory.Exists(_directory)) return;
        // Only remove the exact temporary directory created by this instance.
        var fullPath = Path.GetFullPath(_directory);
        var parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if (Path.GetDirectoryName(fullPath) != parent
            || !Path.GetFileName(fullPath).StartsWith("homematch-identity-tests-", StringComparison.Ordinal)
            || (new DirectoryInfo(fullPath).Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Refusing to remove an unexpected PostgreSQL test directory.");
        }

        Directory.Delete(fullPath, recursive: true);
    }

    private async Task RunAsync(string command, params string[] arguments)
    {
        var start = new ProcessStartInfo(Path.Combine(_binDirectory, command + (OperatingSystem.IsWindows() ? ".exe" : "")))
        {
            UseShellExecute = false, CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            // pg_ctl starts a long-lived server on Windows; inherited pipes can keep reads open.
            RedirectStandardOutput = command == "initdb", RedirectStandardError = command == "initdb"
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start PostgreSQL test tool.");
        var output = start.RedirectStandardOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
        var error = start.RedirectStandardError ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var stdout = await output;
        var stderr = await error;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"PostgreSQL test tool {command} failed: {stderr}\n{stdout}");
        }
    }

    private static string FindBinaryDirectory()
    {
        var supplied = Environment.GetEnvironmentVariable("HOMEMATCH_TEST_POSTGRES_BIN");
        var candidates = new[]
        {
            supplied,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL", "16", "bin"),
            "/usr/lib/postgresql/16/bin"
        };
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate)
                && File.Exists(Path.Combine(candidate, OperatingSystem.IsWindows() ? "initdb.exe" : "initdb")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Real PostgreSQL tests require HOMEMATCH_TEST_POSTGRES_CONNECTION (with CREATEDB), or HOMEMATCH_TEST_POSTGRES_BIN pointing to installed PostgreSQL 16 binaries.");
    }
}