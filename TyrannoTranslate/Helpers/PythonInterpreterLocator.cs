using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TyrannoTranslate.Helpers;

public static class PythonInterpreterLocator
{
    private const string PythonVersionCheck =
        "import sys; print(sys.executable if sys.version_info[:2] == (3, 14) else '')";

    public static string? FindPython()
    {
        var launcherPython = TryResolvePython("py", ["-3.14", "-c", "import sys; print(sys.executable)"]);
        if (launcherPython != null)
            return launcherPython;

        foreach (var executable in new[] { "python3.14", "python", "python3" })
        {
            var python = TryResolvePython(executable, ["-c", PythonVersionCheck]);
            if (python != null)
                return python;
        }

        return null;
    }

    private static string? TryResolvePython(string executable, IReadOnlyList<string> arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process == null || !process.WaitForExit(2000))
                return null;

            var pythonPath = process.StandardOutput.ReadToEnd().Trim();
            return process.ExitCode == 0 && File.Exists(pythonPath)
                ? pythonPath
                : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }
}
