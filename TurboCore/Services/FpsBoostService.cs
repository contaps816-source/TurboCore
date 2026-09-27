using System.Diagnostics;
using Microsoft.Win32;

namespace TurboCore.Services;

public record FpsBoostStepResult(string Name, bool Success, string Detail);

/// <summary>
/// Tweaks de sistema (não de jogo) para reduzir overhead e priorizar o
/// processo do jogo em primeiro plano. Não interage com a memória,
/// processo ou ficheiros de nenhum jogo — apenas ajusta definições do
/// Windows (prioridade de processo, efeitos visuais, energia, rede).
/// </summary>
public class FpsBoostService
{
    public async Task<FpsBoostStepResult> BoostForegroundGameAsync()
    {
        return await Task.Run(() =>
        {
            var proc = GetLikelyGameProcess();
            if (proc == null)
                return new FpsBoostStepResult("Prioridade do Jogo", false, "Nenhum jogo em primeiro plano detetado.");

            try
            {
                proc.PriorityClass = ProcessPriorityClass.High;
                return new FpsBoostStepResult("Prioridade do Jogo", true,
                    $"Prioridade de \"{proc.ProcessName}\" definida para Alta.");
            }
            catch (Exception ex)
            {
                return new FpsBoostStepResult("Prioridade do Jogo", false, $"Falhou: {ex.Message}");
            }
        });
    }

    private static Process? GetLikelyGameProcess()
    {
        // Heurística simples: maior processo com janela visível e sem ser processo de sistema/shell.
        var excluded = new[] { "explorer", "turbocore", "dwm", "svchost", "csrss" };
        return Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
            .Where(p => !excluded.Contains(p.ProcessName.ToLowerInvariant()))
            .OrderByDescending(p =>
            {
                try { return p.WorkingSet64; } catch { return 0L; }
            })
            .FirstOrDefault();
    }

    public async Task<FpsBoostStepResult> SetUltimatePerformancePlanAsync()
    {
        return await Task.Run(() =>
        {
            var ok = RunCommand("powercfg", "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
            if (!ok)
            {
                // Plano "Desempenho Máximo" pode não existir ainda: duplica-o do template oculto e ativa.
                RunCommand("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                ok = RunCommand("powercfg", "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
            }
            return new FpsBoostStepResult("Plano de Energia (Máximo)", ok,
                ok ? "Plano de Desempenho Máximo ativado." : "Não foi possível ativar (precisa de admin).");
        });
    }

    public async Task<FpsBoostStepResult> DisableVisualEffectsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
                key.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); // 2 = Ajustar para melhor desempenho
                return new FpsBoostStepResult("Efeitos Visuais", true, "Efeitos visuais reduzidos ao mínimo.");
            }
            catch (Exception ex)
            {
                return new FpsBoostStepResult("Efeitos Visuais", false, $"Falhou: {ex.Message}");
            }
        });
    }

    public async Task<FpsBoostStepResult> DisableGameBarAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar");
                key.SetValue("AutoGameModeEnabled", 0, RegistryValueKind.DWord);
                key.SetValue("ShowStartupPanel", 0, RegistryValueKind.DWord);

                using var key2 = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
                key2.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);

                return new FpsBoostStepResult("Xbox Game Bar / DVR", true, "Game Bar e gravação em segundo plano desativadas.");
            }
            catch (Exception ex)
            {
                return new FpsBoostStepResult("Xbox Game Bar / DVR", false, $"Falhou: {ex.Message}");
            }
        });
    }

    public async Task<FpsBoostStepResult> OptimizeNetworkAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Desativa o algoritmo de Nagle nos adaptadores de rede — tweak padrão de latência,
                // amplamente documentado pela Microsoft (chave TcpAckFrequency / TCPNoDelay).
                const string basePath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                using var interfaces = Registry.LocalMachine.OpenSubKey(basePath, writable: true);
                if (interfaces == null)
                    return new FpsBoostStepResult("Otimização de Rede", false, "Chave de registo não encontrada.");

                int changed = 0;
                foreach (var sub in interfaces.GetSubKeyNames())
                {
                    using var iface = interfaces.OpenSubKey(sub, writable: true);
                    if (iface == null) continue;
                    iface.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                    iface.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                    changed++;
                }

                return new FpsBoostStepResult("Otimização de Rede", true,
                    $"Nagle desativado em {changed} interface(s). Reinicia para aplicar totalmente.");
            }
            catch (Exception ex)
            {
                return new FpsBoostStepResult("Otimização de Rede", false, $"Falhou (precisa de admin): {ex.Message}");
            }
        });
    }

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
