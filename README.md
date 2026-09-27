# TurboCore

App de otimização de PC e boost de FPS para Windows, em C# / WPF (.NET 8).

## Requisitos
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recomendado) ou `dotnet` CLI

## Como correr
```bash
cd TurboCore
dotnet restore
dotnet run --project TurboCore
```
A app pede privilégios de administrador ao abrir (necessário para plano de
energia, registo do sistema, serviços de arranque, etc.).

## Key de teste
Para testares já a ativação sem teres o backend/bot prontos, usa a key de
demonstração embutida no código:

```
TURBO-DEMO-0000-0000
```

Isto está definido em `Services/LicenseService.cs`, no método `ValidateAsync`.

## Estrutura do projeto
```
TurboCore/
├── App.xaml / App.xaml.cs        # arranque da aplicação e tema (dark mode)
├── Views/
│   ├── LoginWindow.xaml(.cs)     # ecrã de ativação por key
│   └── MainWindow.xaml(.cs)      # ecrã principal (Otimização de PC / Boost de FPS)
├── Services/
│   ├── LicenseService.cs         # valida/guarda a key (local por agora)
│   ├── OptimizationService.cs    # limpeza, DNS, plano de energia, arranque
│   └── FpsBoostService.cs        # prioridade de processo, rede, efeitos visuais, Game Bar
└── Models/
    └── LicenseResult.cs
```

## Ligar à tua API de licenciamento (quando tiveres o bot/backend prontos)

Neste momento a validação da key é **local** (só para testar a app). Quando
tiveres a API de licenciamento (a que o bot de Discord vai gerar as keys),
o único ficheiro que precisas de mudar é `Services/LicenseService.cs`:

1. Adiciona um `HttpClient` ao serviço.
2. No método `ValidateAsync`, troca a validação local por uma chamada
   `POST` à tua API, enviando a key e o `GetHardwareId()` (para travar a
   key a uma única máquina).
3. A API deve devolver algo como:
   ```json
   { "isValid": true, "planName": "Lifetime", "expiresAt": null }
   ```
4. O resto da app (LoginWindow, MainWindow) não precisa de nenhuma alteração.

O código já tem comentários `TODO` a marcar exatamente este ponto.

## Segurança e distribuição
- Nunca coloques chaves de API, tokens do bot ou credenciais de pagamento
  dentro deste projeto — isso pertence ao bot/backend, não à app do cliente.
- Antes de distribuíres, assina o executável com um certificado de
  assinatura de código para reduzir alertas do Windows Defender/SmartScreen
  (comum em apps deste tipo por mexerem no registo e serviços do sistema).
- Considera publicar como single-file (`dotnet publish -c Release -r win-x64 --self-contained`)
  para facilitar a distribuição de um único `.exe`.

## Próximos passos sugeridos
- [ ] API de licenciamento (gera/valida/revoga keys, liga ao HWID)
- [ ] Bot de Discord (loja + entrega automática da key após pagamento)
- [ ] Auto-update da app (ex.: Squirrel.Windows ou verificação de versão simples)
