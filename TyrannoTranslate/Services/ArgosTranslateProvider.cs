using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using TyrannoTranslate.Helpers;

namespace TyrannoTranslate.Services;

public sealed class ArgosTranslateProvider : ITranslationProvider
{
    public string Name => "Argos Translate";
    public string Description => "Offline (free, no data sent, quality limited by model)";

    public bool IsConfigured => true;

    public string ModelPath { get; set; } = "";

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts, string sourceLang, string targetLang,
        CancellationToken ct = default)
    {
        var pythonExe = PythonInterpreterLocator.FindPython();
        if (pythonExe == null)
            throw new InvalidOperationException("Python 3.14 not found. Please install Python 3.14.10.");

        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "argos_translate.py");
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Translation script not found: {scriptPath}");

        var input = new Dictionary<string, object>
        {
            ["source_lang"] = sourceLang,
            ["target_lang"] = targetLang,
            ["texts"] = texts,
            ["packages_dir"] = ModelPath
        };
        var jsonInput = JsonSerializer.Serialize(input);

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardInputEncoding = new UTF8Encoding(false)
        };
        psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

        using var process = new Process { StartInfo = psi };
        process.Start();

        await process.StandardInput.WriteAsync(jsonInput);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdoutTask, stderrTask);

        var jsonOutput = stdoutTask.Result;
        var stderrOutput = stderrTask.Result;
        process.WaitForExit();

        if (string.IsNullOrWhiteSpace(jsonOutput))
        {
            var detail = !string.IsNullOrWhiteSpace(stderrOutput)
                ? $"Python error: {stderrOutput}"
                : "No output from script";
            throw new InvalidOperationException(detail);
        }

        using var doc = JsonDocument.Parse(jsonOutput);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.GetBoolean())
        {
            return root.GetProperty("results").EnumerateArray()
                .Select(r => r.GetString() ?? "")
                .ToList();
        }

        var error = root.TryGetProperty("error", out var errEl)
            ? errEl.GetString()
            : "Unknown error";
        throw new InvalidOperationException(error ?? "Unknown error");
    }

}
