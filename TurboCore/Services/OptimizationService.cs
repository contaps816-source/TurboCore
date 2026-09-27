using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TurboCore.Services;

public record OptimizationStepResult(string Name, bool Success, string Detail);

/// <summary>
/// Ações de otimização "clássicas" de Windows: limpeza de temporários,
/// plano de energia, flush de DNS, etc. Tudo reversível/seguro — nada
/// toca em ficheiros de sistema protegidos ou noutros processos/jogos.
/// </summary>
public class OptimizationService
{
    public async Task<OptimizationStepResult> CleanTempFilesAsync()
    {
        return await Task.Run(() =>
        {
            long freedBytes = 0;
            int filesDeleted = 0;
            var folders = new[]
            {
                Path.GetTempPath(),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Temp"
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var file in SafeEnumerateFiles(folder))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var size = info.Length;
                        info.Delete();
                        freedBytes += size;
                        filesDeleted++;
                    }
                    catch
                    {
                        // Ficheiro em uso ou sem permissão — ignora e continua
                    }
                }
            }

            var freedMb = freedBytes / (1024.0 * 1024.0);
            return new OptimizationStepResult("Limpeza de Temporários", true,
                $"{filesDeleted} ficheiros removidos, {freedMb:F1} MB libertados.");
        });
    }

    private static IEnumerable<string> SafeEnumerateFiles(string folder)
    {
        IEnumerable<string> files = Array.Empty<string>();
        try { files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories); }
        catch { /* pasta protegida */ }
        return files;
    }

    public async Task<OptimizationStepResult> SetHighPerformancePowerPlanAsync()
    {
        return await Task.Run(() =>
        {
            // GUID público da Microsoft para "Alto Desempenho"
            const string highPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
            var ok = RunPowercfg($"/s {highPerfGuid}");
            return new OptimizationStepResult("Plano de Energia",  ok,
                ok ? "Plano de energia definido para Alto Desempenho."
                   : "Não foi possível alterar o plano de energia (precisa de admin).");
        });
    }

    public async Task<OptimizationStepResult> FlushDnsAsync()
    {
        return await Task.Run(() =>
        {
            var ok = RunCommand("ipconfig", "/flushdns");
            return new OptimizationStepResult("Flush de DNS", ok,
                ok ? "Cache de DNS limpa." : "Falha ao limpar a cache de DNS.");
        });
    }

    public async Task<OptimizationStepResult> DisableUnnecessaryStartupAsync(IEnumerable<string> appsToDisable)
    {
        return await Task.Run(() =>
        {
            int disabled = 0;
            const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
            if (key == null)
                return new OptimizationStepResult("Programas de Arranque", false, "Chave de registo não encontrada.");

            foreach (var name in key.GetValueNames())
            {
                if (appsToDisable.Any(a => name.Contains(a, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        key.DeleteValue(name, throwOnMissingValue: false);
                        disabled++;
                    }
                    catch { /* sem permissão para esta entrada */ }
                }
            }

            return new OptimizationStepResult("Programas de Arranque", true,
                $"{disabled} entradas de arranque desativadas.");
        });
    }

    private static bool RunPowercfg(string args) => RunCommand("powercfg", args);

    private static bool RunCommand(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
