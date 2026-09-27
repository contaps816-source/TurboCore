using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TurboCore.Services;

namespace TurboCore.Views;

public partial class LoginWindow : Window
{
    private readonly LicenseService _licenseService = new();

    // TODO: substitui pelo link real do teu servidor de Discord
    private const string DiscordInviteUrl = "https://discord.gg/o-teu-servidor";

    public LoginWindow()
    {
        InitializeComponent();
        TryAutoLogin();
    }

    private async void TryAutoLogin()
    {
        var savedKey = _licenseService.LoadSavedKey();
        if (string.IsNullOrWhiteSpace(savedKey)) return;

        var result = await _licenseService.ValidateAsync(savedKey);
        if (result.IsValid)
        {
            OpenMainWindow(result.PlanName);
        }
    }

    private void KeyInput_GotFocus(object sender, RoutedEventArgs e)
    {
        if (KeyInput.Text == "TURBO-XXXX-XXXX-XXXX")
            KeyInput.Text = string.Empty;
    }

    private async void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        ActivateButton.IsEnabled = false;
        StatusText.Text = "A validar...";
        StatusText.Foreground = System.Windows.Media.Brushes.Gray;

        var key = KeyInput.Text.Trim();
        var result = await _licenseService.ValidateAsync(key);

        if (result.IsValid)
        {
            _licenseService.SaveActivation(key);
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("Accent2Brush");
            StatusText.Text = "Key ativada com sucesso!";
            OpenMainWindow(result.PlanName);
        }
        else
        {
            StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            StatusText.Text = result.Message;
            ActivateButton.IsEnabled = true;
        }
    }

    private void OpenMainWindow(string? plan)
    {
        var main = new MainWindow(plan);
        main.Show();
        Close();
    }

    private void DiscordLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(DiscordInviteUrl) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show("Não foi possível abrir o link. Visita-nos no Discord manualmente.",
                "TurboCore", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
