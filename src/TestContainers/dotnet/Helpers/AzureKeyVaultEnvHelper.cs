using AzureKeyVaultEmulator.TestContainers.Exceptions;
using System.Diagnostics;

namespace AzureKeyVaultEmulator.TestContainers.Helpers;

internal static class AzureKeyVaultEnvHelper
{
    public static void Bash(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);

        var err = proc?.StandardError.ReadToEnd();
        var output = proc?.StandardOutput.ReadToEnd();

        proc?.WaitForExit();

        if (!string.IsNullOrEmpty(output))
            Console.WriteLine(output);

        if (!string.IsNullOrEmpty(err))
            Console.WriteLine(err);

        if (proc?.ExitCode != 0)
            throw new KeyVaultEmulatorException($"Command failed: {command}\n{err}");
    }

    public static bool IsCiCdEnvironment()
    {
        var ci = Environment.GetEnvironmentVariable("CI")?.ToLowerInvariant();
        var gh = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        return ci == "true" || gh == "true";
    }
}
