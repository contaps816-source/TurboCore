using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using TurboCore.Models;

namespace TurboCore.Services;

/// <summary>
/// Serviço responsável por validar e persistir a key de ativação do TurboCore.
///
/// MODO ATUAL: validação LOCAL (lista/hashes embutidos no código), só para
/// testares a app de ponta a ponta sem precisares do backend/bot ainda.
///
/// QUANDO TIVERES A API DE LICENCIAMENTO PRONTA:
/// substitui o corpo de ValidateAsync por uma chamada HTTP, por exemplo:
///
///   var response = await _http.PostAsJsonAsync("https://api.teu-dominio.com/validate", new {
///       key, hwid = GetHardwareId()
///   });
///   var result = await response.Content.ReadFromJsonAsync<LicenseResult>();
///
/// Isto mantém o resto da app (LoginWindow, MainWindow) igual — só este
/// ficheiro muda.
/// </summary>
public class LicenseService
{
    private static readonly string StorageFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TurboCore", "license.dat");

    // --- MODO LOCAL DE TESTE ---
    // Guarda aqui SHA-256 das keys válidas (nunca a key em texto simples).
    // Gera o hash de uma key nova com: GenerateKeyHash("TURBO-XXXX-XXXX-XXXX")
    private static readonly HashSet<string> ValidKeyHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Exemplo de key de teste: "TURBO-DEMO-0000-0000"
        "F1B7C5B0B9F0A6F6E3B7A4B6E9C9B3B0B9C6F6E3B7A4B6E9C9B3B0B9C6F6E3B7" // placeholder, substitui pelo hash real
    };

    public static string ComputeKeyHash(string key)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key.Trim().ToUpperInvariant()));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Valida uma key. Hoje: local. Amanhã: chamada à API.</summary>
    public Task<LicenseResult> ValidateAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult(LicenseResult.Fail("Introduz uma key."));

        var normalized = key.Trim();
        if (!LooksLikeKeyFormat(normalized))
            return Task.FromResult(LicenseResult.Fail("Formato de key inválido. Exemplo: TURBO-XXXX-XXXX-XXXX"));

        var hash = ComputeKeyHash(normalized);

        // ---- Placeholder: aceita também a key de demonstração para testares já a UI ----
        if (normalized.Equals("TURBO-DEMO-0000-0000", StringComparison.OrdinalIgnoreCase) ||
            ValidKeyHashes.Contains(hash))
        {
            return Task.FromResult(LicenseResult.Ok(plan: "Lifetime", expires: null));
        }

        return Task.FromResult(LicenseResult.Fail("Key inválida ou já utilizada."));
    }

    private static bool LooksLikeKeyFormat(string key) =>
        key.Split('-').Length == 4 && key.StartsWith("TURBO", StringComparison.OrdinalIgnoreCase);

    /// <summary>ID único da máquina, útil para travar a key a um único PC no backend.</summary>
    public static string GetHardwareId()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (ManagementObject mo in searcher.Get())
            {
                var uuid = mo["UUID"]?.ToString();
                if (!string.IsNullOrWhiteSpace(uuid))
                    return ComputeKeyHash(uuid)[..16];
            }
        }
        catch
        {
            // Ignora falhas de WMI (ex.: sandbox/VM restrita) e recorre a um ID de fallback
        }
        return ComputeKeyHash(Environment.MachineName + Environment.ProcessorCount)[..16];
    }

    public void SaveActivation(string key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorageFile)!);
        var plain = Encoding.UTF8.GetBytes(key.Trim());
        var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(StorageFile, protectedBytes);
    }

    public string? LoadSavedKey()
    {
        try
        {
            if (!File.Exists(StorageFile)) return null;
            var protectedBytes = File.ReadAllBytes(StorageFile);
            var plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }

    public void ClearActivation()
    {
        if (File.Exists(StorageFile)) File.Delete(StorageFile);
    }
}
