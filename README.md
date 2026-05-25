# WinAiBar
Tracker de consommation **Claude** + **GitHub Copilot** pour Windows

Prérequis :
winget install Microsoft.WindowsAppRuntime.1.6
winget install Microsoft.DotNet.SDK.9
dotnet tool install --global vpk
winget install SQLite.SQLite

 Microsoft.WindowsAppSDK 1.8

 Log : %LOCALAPPDATA%\WinAIBar\logs

[ccstatusline — Claude Code status line reference](https://github.com/sirmalloc/ccstatusline)

notepad $PROFILE

$env:GITHUB_TOKEN = (Read-Host "GitHub token")
$env:CLAUDE_CODE_USE_POWERSHELL_TOOL=1
$env:ANTHROPIC_MODEL = "sonnet"

 fix the comment review of the pr (use mcp)