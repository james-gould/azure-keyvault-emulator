using AzureKeyVaultEmulator.Aspire.Hosting.Exceptions;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AzureKeyVaultEmulator.Aspire.Hosting.Helpers;

internal static class AzureKeyVaultEnvHelper
{
    private static readonly string[] _defaultVars =
    [
        "BUILD_BUILDID", // Azure DevOps
        "CI", // Jenkins, TeamCity, etc
        "GITHUB_ACTIONS" // Github, obviously.
    ];

    /// <summary>
    /// Typically used to run commands with `sudo`
    /// </summary>
    /// <param name="command"></param>
    public static void Bash(string command)
    {
        Exec("/bin/bash", $"-c \"{command}\"");
    }

    public static void Exec(string executable, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
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
            throw new KeyVaultEmulatorException($"Command failed: {executable} {arguments}\n{err}");
    }

    /// <summary>
    /// Detects env vars injected by the vast majority of CI/CD runners.
    /// </summary>
    /// <returns></returns>
    public static bool IsCiCdEnvironment()
        => _defaultVars.Any(env => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(env)));

    /// <summary>
    /// <para>Determines the appropriate container tag based off the current process architecture.</para>
    /// <para>linx/arm64/v8 images are built alongside amd64 images, see issue #338</para>
    /// </summary>
    /// <returns>The container tag to use for the Emulator.</returns>
    public static string GetContainerTag()
        => RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? KeyVaultEmulatorContainerConstants.ArmTag
                : KeyVaultEmulatorContainerConstants.Tag;
}
