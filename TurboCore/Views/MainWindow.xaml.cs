using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TurboCore.Services;

namespace TurboCore.Views;

public partial class MainWindow : Window
{
    private readonly OptimizationService _optimization = new();
    private readonly FpsBoostService _fpsBoost = new();

    public MainWindow(string? planName = null)
    {
        InitializeComponent();
        PlanBadge.Text = $"Plano: {planName ?? "Ativo"}";
    }

    private async void RunPcOptimization_Click(object sender, RoutedEventArgs e)
    {
        RunPcButton.IsEnabled = false;
        LogPanel.Children.Clear();

        if (ChkCleanTemp.IsChecked == true)
            AddLog(await _optimization.CleanTempFilesAsync());

        if (ChkFlushDns.IsChecked == true)
            AddLog(await _optimization.FlushDnsAsync());

        if (ChkHighPower.IsChecked == true)
            AddLog(await _optimization.SetHighPerformancePowerPlanAsync());

        if (ChkStartup.IsChecked == true)
        {
            var apps = new[] { "Spotify", "Steam", "Discord", "Skype" }; // TODO: tornar configurável na UI
            AddLog(await _optimization.DisableUnnecessaryStartupAsync(apps));
        }

        RunPcButton.IsEnabled = true;
    }

    private async void RunFpsBoost_Click(object sender, RoutedEventArgs e)
    {
        RunFpsButton.IsEnabled = false;
        LogPanel.Children.Clear();

        if (ChkBoostForeground.IsChecked == true)
            AddLog(await _fpsBoost.BoostForegroundGameAsync());

        if (ChkUltimatePower.IsChecked == true)
            AddLog(await _fpsBoost.SetUltimatePerformancePlanAsync());

        if (ChkVisualEffects.IsChecked == true)
            AddLog(await _fpsBoost.DisableVisualEffectsAsync());

        if (ChkGameBar.IsChecked == true)
            AddLog(await _fpsBoost.DisableGameBarAsync());

        if (ChkNetwork.IsChecked == true)
            AddLog(await _fpsBoost.OptimizeNetworkAsync());

        RunFpsButton.IsEnabled = true;
    }

    private void AddLog(OptimizationStepResult result) => AddLogLine(result.Name, result.Success, result.Detail);
    private void AddLog(FpsBoostStepResult result) => AddLogLine(result.Name, result.Success, result.Detail);

    private void AddLogLine(string name, bool success, string detail)
    {
        var line = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 2),
            Foreground = success ? (Brush)FindResource("Accent2Brush") : Brushes.OrangeRed
        };
        line.Inlines.Add(new System.Windows.Documents.Run($"{(success ? "✓" : "✗")} {name}: ") { FontWeight = FontWeights.Bold });
        line.Inlines.Add(new System.Windows.Documents.Run(detail) { Foreground = (Brush)FindResource("SubTextBrush") });
        LogPanel.Children.Add(line);
    }
}
