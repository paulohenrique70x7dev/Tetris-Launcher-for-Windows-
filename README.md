# Tetris Windows Edition

Customizador do Windows com identidade visual Tetris, construído em C# / WPF
(.NET 8), seguindo a arquitetura modular já usada no protótipo do wallpaper
animado (WPF + WebView2 + WorkerW).

> Este código foi escrito fora do Windows, então não foi compilado nem
> testado em uma máquina real. É código-fonte completo, pronto para você
> abrir no Visual Studio (ou `dotnet build`/`dotnet publish` no seu
> notebook) e ajustar o que precisar. O instalador/empacotamento final é
> por sua conta, como combinamos — o workflow em `.github/workflows/build.yml`
> só compila os `.exe` e sobe como artefato.

## Estrutura

```
TetrisWindowsEdition.sln
src/
  TetrisWindowsEdition.App/          -> aplicativo principal (WPF)
    Modules/                         -> um arquivo por módulo do spec
    Native/NativeMethods.cs          -> P/Invoke Win32 centralizado
    Resources/                       -> wallpapers, cursores, sons, ícone (gerados)
    Assets/ThemeStyles.xaml          -> estilo visual "blocos Tetris"
    MainWindow.xaml(.cs)             -> painel principal + abas
  TetrisWindowsEdition.Screensaver/  -> protetor de tela (WinForms), vira .scr
```

## Como compilar

```powershell
# app principal
dotnet publish src\TetrisWindowsEdition.App\TetrisWindowsEdition.App.csproj -c Release -r win-x64 --self-contained false

# protetor de tela
dotnet publish src\TetrisWindowsEdition.Screensaver\TetrisWindowsEdition.Screensaver.csproj -c Release -r win-x64 --self-contained false

# depois, copie o .exe do protetor de tela para a pasta do app principal,
# renomeado para TetrisWindowsEdition.Screensaver.scr
```

Pré-requisitos: **.NET 8 SDK** com workload de desktop (`Microsoft.NET.Sdk` +
`UseWPF`/`UseWindowsForms`), que já vem com o Visual Studio 2022 (workload
".NET desktop development").

## O que está implementado de verdade (não só estrutura)

- **Backup/Restauração** (item 4/15/18): `BackupManager` captura um retrato
  JSON do estado atual (wallpaper, cores, cursores, sons, inicialização,
  protetor de tela) *antes* de qualquer alteração; `RestoreManager` devolve
  tudo, de forma idempotente. `ChangeHistory` registra cada ação.
- **Cores** (item 6): 5 esquemas (`ColorSchemes`), aplicados via Registro
  oficial (`DWM\AccentColor`, `Themes\Personalize`), com o broadcast de
  `WM_SETTINGCHANGE` que faz o Windows atualizar sem logoff.
- **Papel de parede** (item 5): `SystemParametersInfo` oficial. 5 wallpapers
  temáticos já gerados em `Resources/Wallpapers` (peças caindo, estáticas —
  o wallpaper *animado* reaproveita a técnica WorkerW do protótipo, em
  `WallpaperLiveHost`).
- **Cursores** (item 7): 14 cursores `.cur` gerados (blocos/peças Tetris),
  aplicados via `Control Panel\Cursors` + `SPI_SETCURSORS`.
- **Sons** (item 8): 10 eventos com `.wav` **sintetizados do zero** (ondas
  quadradas/triangulares geradas por script, sem nenhuma amostra protegida),
  aplicados via `AppEvents\Schemes`.
- **Proteção de tela** (item 10): projeto `.Screensaver` separado, com
  animação GDI+ leve (peças caindo), suportando `/s`, `/c`, `/p` como todo
  `.scr` do Windows precisa suportar.
- **Tela de bloqueio** (item 9): implementada **apenas onde o Windows
  realmente permite** — política de grupo em Pro/Enterprise, via processo
  elevado à parte. Em Home, o app explica a limitação e não finge que
  funciona (`LockScreenModule`, `CompatibilityAnalyzer`).
- **Inicialização automática** (item 19): `HKCU\...\Run`, sem admin.
- **Exportar tema `.theme`** (item 13): `ThemeExporter` gera um `.theme`
  de verdade, abrível pelo Explorer.
- **Análise de compatibilidade** (item 21): `CompatibilityAnalyzer` lista o
  que é 100% suportado, parcial ou impossível — e por quê.
- **Detecção de Windows 10/11/edição** (item 16): `WindowsEnvironment`.

## O que ficou como próximo passo (não implementado ainda)

- Ícone/bandeja do sistema com `Hardcodet.NotifyIcon.Wpf` (pacote já
  referenciado no `.csproj`, mas o `TrayIconService` ainda não foi escrito —
  é o próximo módulo natural).
- Cursores animados (`AppStarting`/`Wait` estão como `.cur` estático; virar
  `.ani` de verdade é só trocar a extensão no dicionário do `CursorsModule`
  e gerar o RIFF).
- Menu Iniciar / barra de tarefas (item 12): o spec já pede para não
  tentar hacks aqui — vale mapear com `CompatibilityAnalyzer` o que o
  Windows 11 realmente libera antes de codar algo.
- Instalador (ficou por sua conta, como combinado).
