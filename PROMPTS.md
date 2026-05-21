# WinAIBar — Plan de développement par prompts

> Tracker de consommation **Claude** + **GitHub Copilot** pour Windows.
> Stack : **.NET 9 / WinUI 3 (Windows App SDK 1.6+)**, **EF Core SQLite**, **CommunityToolkit.Mvvm**, **LiveChartsCore**, **H.NotifyIcon.WinUI**, **Velopack**, **xUnit v3**.
>
> Chaque prompt ci-dessous est **auto-suffisant**, à coller dans une session Claude après avoir exécuté les précédents. L'IA doit, à chaque étape : (1) créer/modifier exactement les fichiers listés, (2) ne **rien** faire en plus, (3) renvoyer le diff + les commandes de vérification.

---

## Pré-requis machine

Avant le Prompt 01, exécute une fois :

```powershell
winget install Microsoft.WindowsAppRuntime.1.6
winget install Microsoft.DotNet.SDK.9
# ~~dotnet workload install winappsdk~~  ← N'EXISTE PAS en .NET 9, ne pas exécuter
dotnet tool install --global vpk
```

> <mark>⚠️ `dotnet workload install winappsdk` n'existe **pas** en .NET 9 (existait en .NET 6/7 uniquement). Ne pas exécuter.</mark>

Vérifie :

```powershell
dotnet --version          # ≥ 9.0
vpk --version             # Velopack présent
```

---

## Phase 0 — Bootstrap

### Prompt 01 — Solution + projets + Central Package Management

```
Tu es développeur .NET 9 / WinUI 3. Crée la solution WinAIBar dans
C:\Users\Nicolas\source\repos\WinAIBar (dossier existant, vide).

Crée exactement :
- WinAIBar.slnx à la racine
- src\WinAIBar\WinAIBar.csproj (WinUI 3 unpackaged, TFM net9.0-windows10.0.19041.0,
  OutputType WinExe, WindowsPackageType=None, EnableMsixTooling=false,
  EnableCoreMrtTooling=false [voir note build CLI ci-dessous],
  UseWinUI=true, RuntimeIdentifiers=win-x64;win-arm64, Nullable enable,
  ImplicitUsings enable, LangVersion latest).
- src\WinAIBar\App.xaml + App.xaml.cs (Application minimaliste qui ouvre
  MainWindow vide).
- src\WinAIBar\MainWindow.xaml + .xaml.cs (Window avec un TextBlock "WinAIBar").
- src\WinAIBar\app.manifest (DPI per-monitor v2, supportedOS Windows 10/11).
- tests\WinAIBar.Tests\WinAIBar.Tests.csproj (net9.0, xUnit v3, NSubstitute).
- Directory.Build.props (TreatWarningsAsErrors=true, AnalysisLevel=latest-recommended,
  EnforceCodeStyleInBuild=true).
- Directory.Packages.props (ManagePackageVersionsCentrally=true) avec versions
  épinglées pour TOUS les packages listés ci-dessous (rien d'autre, je veux pouvoir
  auditer la liste) :

  Microsoft.WindowsAppSDK 1.6.241114003
  Microsoft.Windows.SDK.BuildTools 10.0.26100.1742
  CommunityToolkit.Mvvm 8.4.0
  CommunityToolkit.WinUI.Controls.SettingsControls 8.1.240916
  H.NotifyIcon.WinUI 2.2.0
  LiveChartsCore.SkiaSharpView.WinUI 2.0.0-rc5.4
  Microsoft.Extensions.Hosting 9.0.0
  Microsoft.Extensions.Http 9.0.0
  Microsoft.Extensions.Http.Polly 9.0.0
  Microsoft.Extensions.Configuration.Json 9.0.0
  Microsoft.Extensions.Configuration.UserSecrets 9.0.0
  Microsoft.Extensions.Logging 9.0.0
  Serilog.Extensions.Hosting 9.0.0
  Serilog.Sinks.File 6.0.0
  Serilog.Sinks.Debug 3.0.0
  Polly 8.5.0
  Microsoft.EntityFrameworkCore.Sqlite 9.0.0
  Microsoft.EntityFrameworkCore.Design 9.0.0
  System.Security.Cryptography.ProtectedData 9.0.0
  CommunityToolkit.WinUI.Notifications 7.1.2
  Velopack 0.0.1298
  xunit.v3 1.0.0
  xunit.runner.visualstudio 3.0.0
  NSubstitute 5.3.0
  Microsoft.NET.Test.Sdk 17.12.0

Le projet WinAIBar référence pour l'instant uniquement Microsoft.WindowsAppSDK
et Microsoft.Windows.SDK.BuildTools. Les autres seront ajoutés par les prompts
suivants au fur et à mesure.

À la fin, fournis :
1. Le diff complet
2. Les commandes : `dotnet restore`, `dotnet build -p:Platform=x64`, `dotnet run --project src\WinAIBar -p:Platform=x64`
3. Confirme que la fenêtre vide WinUI 3 s'ouvre
```

> <mark>**Note build CLI — `EnableCoreMrtTooling=false`**
>
> Sans cette propriété, `dotnet build` échoue avec `MSB4062: Cannot load task ExpandPriContent` car
> `MrtCore.PriGen.targets` (dans `Microsoft.WindowsAppSDK`) cherche `Microsoft.Build.Packaging.Pri.Tasks.dll`
> dans le répertoire MSBuild du SDK dotnet, où elle n'existe pas (elle n'est fournie que par VS).
> En .NET 9, il n'existe aucun workload pour l'installer.
>
> `EnableCoreMrtTooling=false` empêche l'import de `MrtCore.PriGen.targets` (cf. `MrtCore.targets` ligne 9 :
> `<Import ... Condition="'$(EnableCoreMrtTooling)'=='true'"/>`).
> C'est l'approche utilisée par le tooling officiel Microsoft (visible dans
> `Microsoft.Windows.SDK.BuildTools.MSIX.Common.props`).
>
> Pour une app **non-packagée** sans `.resw`, c'est sans impact fonctionnel : les ressources WinUI
> viennent du runtime `Microsoft.WindowsAppRuntime` installé system-wide.
>
> Le build doit être lancé avec `-p:Platform=x64` (ou `arm64`) — `dotnet build` sans plateforme
> explicite cible `AnyCPU`, qui n'est pas supporté pour les projets WinUI 3.</mark>

**Acceptance** : `dotnet build -p:Platform=x64` passe sans warning ; F5 lance une fenêtre vide.

---

### Prompt 02 — Generic Host + DI + logging + HttpClient

```
Sur la base du Prompt 01, ajoute au projet WinAIBar les packages :
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Http
- Microsoft.Extensions.Http.Polly
- Microsoft.Extensions.Configuration.Json
- Microsoft.Extensions.Logging
- Serilog.Extensions.Hosting
- Serilog.Sinks.File
- Serilog.Sinks.Debug
- Polly

Crée :
- src\WinAIBar\Infrastructure\AppHost.cs : classe statique exposant `IHost Current`
  et `Task StartAsync()`. Configure :
    * `AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)`
    * Serilog : sink Debug + sink File vers `%LocalAppData%\WinAIBar\logs\winaibar-.log`
      avec rolling daily, 7 jours rétention.
    * `IHttpClientFactory` standard + politique Polly retry exponentiel (3 essais,
      jitter) pour status 5xx + 408 + 429 (avec respect du `Retry-After`).
- src\WinAIBar\appsettings.json : section `Logging` (niveaux par défaut).
- Modifie App.xaml.cs pour appeler `AppHost.StartAsync()` dans OnLaunched AVANT
  d'ouvrir MainWindow, et `AppHost.Current.StopAsync()` dans OnClosed du MainWindow.
- src\WinAIBar\Infrastructure\PathProvider.cs : statique, expose `LocalAppData`
  (`%LocalAppData%\WinAIBar`), `LogsDirectory`, `DataDirectory`. Crée les dossiers
  s'ils n'existent pas.

Le dossier `%LocalAppData%\WinAIBar` doit être créé au démarrage.
Un message INFO "WinAIBar started v{version}" doit apparaître dans les logs.

Livre : diff + screenshot du fichier de log généré.
```

**Acceptance** : un fichier `winaibar-YYYYMMDD.log` est créé au lancement.

---

### Prompt 03 — Shell de navigation + thème Windows

```
Sur la base du Prompt 02, transforme MainWindow en un Shell de navigation.

Crée :
- src\WinAIBar\Views\Shell.xaml (+ .xaml.cs) : UserControl avec NavigationView,
  PaneDisplayMode=LeftCompact, items :
    * Dashboard (Symbol Home)
    * Claude (Symbol Cloud)
    * Copilot (Symbol World)
    * History (Symbol Clock)
    * Health (Symbol Repair)
    * Cost (Symbol Calculator)
  Footer item :
    * Settings (Symbol Setting)
  Le content host est un Frame nommé `ContentFrame`.
- src\WinAIBar\Views\Pages\DashboardPage.xaml (+ vide)
- src\WinAIBar\Views\Pages\ClaudePage.xaml (vide)
- src\WinAIBar\Views\Pages\CopilotPage.xaml (vide)
- src\WinAIBar\Views\Pages\HistoryPage.xaml (vide)
- src\WinAIBar\Views\Pages\HealthPage.xaml (vide)
- src\WinAIBar\Views\Pages\CostPage.xaml (vide)
- src\WinAIBar\Views\Pages\SettingsPage.xaml (vide)
  Chaque page : Grid avec TextBlock "Page <Nom>".

Modifie MainWindow.xaml pour contenir uniquement <local:Shell />.

Ajoute le package CommunityToolkit.Mvvm. Crée :
- src\WinAIBar\ViewModels\ShellViewModel.cs : ObservableObject avec
  ObservableProperty `SelectedItem` + RelayCommand `NavigateCommand(string tag)`
  qui fait `ContentFrame.Navigate(Type.GetType(...))` (via callback Action).

Thème : dans Shell.xaml.cs, écoute `UISettings.ColorValuesChanged` et applique
`Application.Current.RequestedTheme = ElementTheme.Default` (suit Windows). Ne
force aucun thème.

Livre : diff + commande F5 + confirme que les 7 pages s'affichent quand on clique
dans le NavigationView.
```

**Acceptance** : navigation fluide entre les 7 pages, thème suit Windows (test : changer Windows en sombre depuis Settings, la fenêtre doit suivre sans redémarrer).

---

## Phase 1 — Domaine + storage

### Prompt 04 — Modèles domaine

```
Crée le modèle métier dans src\WinAIBar\Models\ :

- ProviderId.cs : enum { Claude, Copilot }
- UsageQuota.cs : record class avec
    string Key (ex. "session-5h", "weekly-all"),
    string Label (UI),
    double Utilization (0.0 .. 1.0+ ; >1 = overage),
    DateTimeOffset? ResetsAt,
    long? Used,
    long? Limit,
    string? Unit (ex. "tokens", "requests", "credits"),
    string? Model (optionnel : "sonnet", "opus", "gpt-4", ...).
- ProviderSnapshot.cs : record class avec
    ProviderId Provider,
    DateTimeOffset CapturedAt,
    IReadOnlyList<UsageQuota> Quotas,
    string? RawPayload (JSON brut, utile pour debug + page Health).
- ProviderStatus.cs : enum { Unknown, Healthy, Degraded, Failed, Unauthorized }.
- ProviderHealth.cs : record class avec
    ProviderId Provider,
    ProviderStatus Status,
    DateTimeOffset CheckedAt,
    int? LastHttpStatus,
    string? LastError,
    TimeSpan? Latency.

Tous les types sont immuables (record class init-only). Pas de logique : juste
des structures de données.

Ajoute tests\WinAIBar.Tests\Models\UsageQuotaTests.cs avec un test xUnit v3
trivial qui construit un quota et valide ses propriétés (sanity check du toolchain).

Livre : diff + `dotnet test` qui passe.
```

**Acceptance** : `dotnet test` vert.

---

### Prompt 05 — EF Core SQLite + repository historique

```
Ajoute les packages :
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.Design

Crée :
- src\WinAIBar\Data\Entities\SnapshotEntity.cs :
    int Id (PK auto), ProviderId Provider, DateTimeOffset CapturedAt,
    string? RawPayload, List<QuotaEntity> Quotas.
- src\WinAIBar\Data\Entities\QuotaEntity.cs :
    int Id, int SnapshotId, string Key, string Label, double Utilization,
    DateTimeOffset? ResetsAt, long? Used, long? Limit, string? Unit, string? Model.
- src\WinAIBar\Data\WinAIBarDbContext.cs : DbSet<SnapshotEntity>, DbSet<QuotaEntity>.
  Configure dans `OnConfiguring` :
    `optionsBuilder.UseSqlite($"Data Source={PathProvider.DataDirectory}\\history.db")`.
  Index sur (Provider, CapturedAt DESC).
- src\WinAIBar\Data\Abstractions\IHistoryRepository.cs :
    Task SaveAsync(ProviderSnapshot snapshot, CancellationToken ct);
    Task<IReadOnlyList<ProviderSnapshot>> GetRecentAsync(ProviderId provider, int count, CancellationToken ct);
    Task<IReadOnlyList<ProviderSnapshot>> GetRangeAsync(ProviderId provider, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);
- src\WinAIBar\Data\HistoryRepository.cs : implémentation EF Core, mapping
  entités <-> domain records.

Au démarrage (dans AppHost.StartAsync), appelle `dbContext.Database.MigrateAsync()`.
Crée la migration initiale via :
    dotnet ef migrations add Initial --project src\WinAIBar --startup-project src\WinAIBar --output-dir Data\Migrations

Enregistre WinAIBarDbContext en DI avec AddDbContext (Scoped) + IHistoryRepository
en Scoped (factory qui résout un DbContext via IServiceScopeFactory pour la
background service).

Tests : tests\WinAIBar.Tests\Data\HistoryRepositoryTests.cs avec une base SQLite
en mémoire (`UseSqlite("DataSource=:memory:")`), valide Save + GetRecent.

Livre : diff + `dotnet test` + un appel manuel `sqlite3 %LocalAppData%\WinAIBar\history.db ".tables"`.
```

**Acceptance** : `history.db` créé avec tables `Snapshots` et `Quotas` ; tests verts.

---

## Phase 2 — Provider Anthropic

### Prompt 06 — Lecture des credentials Claude Code

```
Crée :
- src\WinAIBar\Services\Anthropic\AnthropicCredentials.cs : record class avec
    string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt.
- src\WinAIBar\Services\Anthropic\IAnthropicCredentialProvider.cs :
    Task<AnthropicCredentials?> GetAsync(CancellationToken ct);
    bool IsAvailable();
- src\WinAIBar\Services\Anthropic\AnthropicCredentialProvider.cs :
  Lit `%USERPROFILE%\.claude\.credentials.json`.
  Le JSON a la forme (vérifiée sur Windows avec Claude Code 1.x) :
    {
      "claudeAiOauth": {
        "accessToken": "...",
        "refreshToken": "...",
        "expiresAt": 1234567890123,           // ms epoch
        "scopes": ["..."]
      }
    }
  - Si fichier absent → retourne null, log warning.
  - Si JSON invalide → retourne null, log error.
  - Si `expiresAt` < maintenant - 60s → retourne null, log warning "token expired".
  - Implémente un cache mémoire 30s (les autres services le polleront).

Enregistre en DI Singleton.

Tests xUnit v3 : crée des fichiers fixtures dans tests\WinAIBar.Tests\TestData\
puis utilise une abstraction `IFileSystem` (System.IO.Abstractions OU une simple
interface `Func<string,string?> FileReader` injectée) pour tester sans toucher
au disque réel. Valide : fichier absent, JSON valide, JSON invalide, token expiré.

Livre : diff + tests verts.
```

**Acceptance** : Les 4 cas de tests passent ; en lançant l'app, le log indique "Anthropic credentials loaded" ou "missing".

---

### Prompt 07 — Client HTTP Anthropic Usage

```
Crée :
- src\WinAIBar\Services\Anthropic\Dto\AnthropicUsageResponse.cs : record class
  reflétant l'API non publique `GET https://api.anthropic.com/api/oauth/usage` :
    JsonElement RawData (on garde le JSON brut pour le snapshot)
  + propriétés typées tentatives :
    Dictionary<string, AnthropicQuotaDto> Quotas
  AnthropicQuotaDto : record class { double Utilization; DateTimeOffset? ResetsAt;
    long? Used; long? Limit; string? Label; }
  Comme le schéma exact évolue, parse en mode tolérant : JsonDocument + extraction
  défensive des champs `utilization`, `resets_at`, `used`, `limit`.
- src\WinAIBar\Services\Anthropic\IAnthropicUsageClient.cs :
    Task<ProviderSnapshot> FetchAsync(CancellationToken ct);
- src\WinAIBar\Services\Anthropic\AnthropicUsageClient.cs : typed HttpClient.
  - BaseAddress : https://api.anthropic.com
  - Header par requête : Authorization Bearer <token depuis credential provider>
  - Header constant : anthropic-beta: oauth-2025-04-20
  - User-Agent : WinAIBar/<version>
  - GET /api/oauth/usage
  - Retry Polly déjà configuré dans Prompt 02. Ajoute en local une gestion 401 :
    si 401, throws `AnthropicUnauthorizedException` (que le service appelant
    traduira en `ProviderStatus.Unauthorized`).
  - Mappe la réponse en `ProviderSnapshot { Provider = Claude, CapturedAt = now,
    Quotas = ..., RawPayload = <json brut> }`.

Enregistre :
    services.AddHttpClient<IAnthropicUsageClient, AnthropicUsageClient>()
        .AddPolicyHandler(...); // déjà global mais on peut surcharger

Tests : utilise `HttpMessageHandler` mocké (NSubstitute) pour valider :
  - 200 + JSON exemple → snapshot avec N quotas.
  - 401 → AnthropicUnauthorizedException.
  - 429 → retry puis succès.

Inclus dans tests\WinAIBar.Tests\TestData\anthropic-usage-200.json un échantillon
réaliste (mets des valeurs plausibles).

Livre : diff + tests verts.
```

**Acceptance** : Tests verts ; un test E2E manuel optionnel `dotnet run -- --debug-claude` (que tu n'implémentes pas encore) sera ajouté plus tard.

---

### Prompt 08 — Service polling Claude

```
Crée :
- src\WinAIBar\Services\Anthropic\ClaudePollingService.cs : `BackgroundService`.
  Logique :
    * Au démarrage : attendre 5s puis première récupération.
    * Intervalles : 7 minutes en mode normal, 20 minutes en mode idle (détection :
      pas d'interaction utilisateur depuis 15 min via `GetLastInputInfo` Win32),
      backoff exponentiel (max 60 min) sur erreurs consécutives.
    * Source de fraîcheur : si `IAnthropicCredentialProvider.IsAvailable() == false`
      → snapshot avec quotas vides + ProviderStatus.Unauthorized, attente 1 min.
    * Sur succès : persiste via `IHistoryRepository.SaveAsync`.
    * Publie l'événement via `WeakReferenceMessenger.Default.Send(snapshot)`
      (CommunityToolkit.Mvvm.Messaging).
    * Maintient l'état courant exposé via un service Singleton
      `IClaudeStateService` (record class `LatestSnapshot`, `LatestHealth`,
      `IObservable<ProviderSnapshot>` via Subject<T>).

- src\WinAIBar\Services\Anthropic\IClaudeStateService.cs + implémentation Singleton.
- src\WinAIBar\Infrastructure\IdleDetector.cs : helper P/Invoke
  `user32!GetLastInputInfo` qui retourne TimeSpan d'inactivité.

Enregistre dans AppHost :
    services.AddSingleton<IClaudeStateService, ClaudeStateService>();
    services.AddHostedService<ClaudePollingService>();

Tests : test du polling service avec un mock de IAnthropicUsageClient :
  - 3 appels successifs → 3 SaveAsync.
  - Erreur 5xx → backoff appliqué (utilise un IClock mockable / TimeProvider).

Livre : diff + tests verts + un screenshot du fichier log montrant la première
fetch.
```

**Acceptance** : Au lancement, après ~5s, une ligne de log "Claude snapshot captured: N quotas" apparaît et un Row est inséré dans `Snapshots`.

---

## Phase 3 — Provider GitHub Copilot

### Prompt 09 — GitHub Device Code Flow

```
Crée :
- src\WinAIBar\Services\GitHub\GitHubOAuthOptions.cs : record class.
    ClientId = "Iv1.b507a08c87ecfe98"  (client public Copilot, déjà documenté)
    DeviceCodeUrl = "https://github.com/login/device/code"
    TokenUrl = "https://github.com/login/oauth/access_token"
    Scope = "read:email"
- src\WinAIBar\Services\GitHub\Dto\DeviceCodeResponse.cs : record class :
    string DeviceCode, string UserCode, string VerificationUri,
    int ExpiresIn, int Interval.
- src\WinAIBar\Services\GitHub\Dto\AccessTokenResponse.cs : record class :
    string? AccessToken, string? TokenType, string? Scope, string? Error.
- src\WinAIBar\Services\GitHub\IGitHubDeviceCodeAuthenticator.cs :
    Task<DeviceCodeResponse> StartAsync(CancellationToken ct);
    Task<string> PollForTokenAsync(DeviceCodeResponse start, IProgress<string>? progress, CancellationToken ct);
- src\WinAIBar\Services\GitHub\GitHubDeviceCodeAuthenticator.cs : typed HttpClient.
  - POST DeviceCodeUrl avec body form-urlencoded { client_id, scope }, Accept: application/json
  - Polling TokenUrl avec body { client_id, device_code, grant_type=urn:ietf:params:oauth:grant-type:device_code }
    Intervalle = Interval secondes. Si error=slow_down → augmenter de 5s.
    Si error=authorization_pending → retry. Si error=expired_token / access_denied → throw.
  - User-Agent : WinAIBar/<version>.

Tests : mock HttpMessageHandler, simule :
  - start → device code OK
  - poll : authorization_pending puis success → token retourné
  - poll : expired_token → DeviceCodeExpiredException

Livre : diff + tests verts.
```

**Acceptance** : Tests verts.

---

### Prompt 10 — Coffre token GitHub (DPAPI)

```
Ajoute le package System.Security.Cryptography.ProtectedData.

Crée :
- src\WinAIBar\Services\GitHub\IGitHubTokenStore.cs :
    Task SaveAsync(string token, CancellationToken ct);
    Task<string?> LoadAsync(CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
    bool HasToken();
- src\WinAIBar\Services\GitHub\GitHubTokenStore.cs :
  Stocke le token chiffré DPAPI (`ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`)
  dans `%LocalAppData%\WinAIBar\github.token`.
  HasToken : fichier existe + non vide. LoadAsync : décrypte ou retourne null si erreur.
  Toutes les opérations sont atomiques (write tmp + replace).

Enregistre en DI Singleton.

Tests : utilise un dossier temporaire (`Path.GetTempPath()`) pour valider
roundtrip Save → Load → Clear.

Livre : diff + tests verts.
```

**Acceptance** : Tests verts ; un appel manuel via REPL `dotnet-script` (optionnel) confirme que le fichier est bien chiffré (illisible en texte brut).

---

### Prompt 11 — Client Copilot

```
Crée :
- src\WinAIBar\Services\GitHub\Dto\CopilotInternalResponse.cs : record class
  qui parse partiellement la réponse de `GET https://api.github.com/copilot_internal/user`.
  Champs principaux (à confirmer empiriquement, parser tolérant via JsonDocument) :
    string? Login;
    int? ChatEnabled;
    long? AccessTypeSku;
    Dictionary<string, double>? Quotas;  // selon retour
    JsonElement RawData;
- src\WinAIBar\Services\GitHub\Dto\CopilotPremiumUsageResponse.cs : pour
  `/users/{username}/settings/billing/premium_request/usage` : usageItems[].
- src\WinAIBar\Services\GitHub\ICopilotUsageClient.cs :
    Task<ProviderSnapshot> FetchAsync(CancellationToken ct);
- src\WinAIBar\Services\GitHub\CopilotUsageClient.cs : typed HttpClient.
  - BaseAddress : https://api.github.com
  - Headers constants : Accept = application/vnd.github+json,
    X-GitHub-Api-Version = 2025-05-01, User-Agent = WinAIBar/<version>.
  - Lit le token via IGitHubTokenStore, ajoute Authorization Bearer.
  - Étape 1 : GET /copilot_internal/user → extrait login + premiers quotas.
  - Étape 2 si login connu : GET /users/{login}/settings/billing/premium_request/usage
    → quota "premium-requests" + "credits".
  - Mappe vers `ProviderSnapshot { Provider = Copilot, ... }`.
  - 401 → CopilotUnauthorizedException.
  - 404 sur copilot_internal → flag dans `ProviderHealth` (sera lu par la page Health
    pour alerter "endpoint potentiellement obsolète").

Enregistre en DI + AddHttpClient<ICopilotUsageClient, CopilotUsageClient>().

Tests : 3 scénarios mockés :
  - copilot_internal 200 + premium_request 200 → snapshot complet.
  - copilot_internal 404 → snapshot dégradé (1 quota indicateur "endpoint_missing").
  - 401 → exception.

Livre : diff + tests verts + fichier JSON fixture réaliste dans TestData/.
```

**Acceptance** : Tests verts.

---

### Prompt 12 — Service polling Copilot

```
Symétrique au Prompt 08 mais pour Copilot.

Crée :
- src\WinAIBar\Services\GitHub\CopilotPollingService.cs : BackgroundService.
  - Intervalles : 5 min normal, 15 min idle, backoff exp max 60 min.
  - Si IGitHubTokenStore.HasToken() == false → snapshot avec
    ProviderStatus.Unauthorized + attente 1 min.
  - Persiste + publie via Messenger comme pour Claude.
- src\WinAIBar\Services\GitHub\ICopilotStateService.cs + implémentation Singleton.

Enregistre en DI.

Tests : analogues au Prompt 08.

Livre : diff + tests verts. Note : sans token, on n'observe pas de snapshot
populé tant que Prompt 18 (Settings → login) n'a pas été fait. Logger
explicitement "Copilot polling skipped: no token".
```

**Acceptance** : Au lancement, un log "Copilot polling skipped: no token" apparaît toutes les minutes.

---

## Phase 4 — UI tray + flyout + dashboard

### Prompt 13 — Icône tray dynamique

```
Ajoute le package H.NotifyIcon.WinUI.

Crée :
- src\WinAIBar\Services\Tray\ITrayIconRenderer.cs :
    Icon Render(double maxUtilization);  // 0..1+
  Logique de couleur :
    < 0.50 → bleu (#0078D4)
    < 0.75 → vert clair (#107C10)
    < 0.90 → orange (#FFB900)
    < 1.00 → rouge (#D13438)
    >= 1.00 → rouge clignotant (deux frames alternés, on prend juste rouge sombre #A4262C)
- src\WinAIBar\Services\Tray\TrayIconRenderer.cs : utilise System.Drawing
  pour générer un Icon 32×32 (disque plein) avec en surimpression le pourcentage
  en blanc, font Segoe UI 11pt bold.
- src\WinAIBar\Services\Tray\TrayController.cs : Singleton qui :
    * Crée un `H.NotifyIcon.WinUI.TaskbarIcon` dans une fenêtre cachée au démarrage.
    * Souscrit aux messengers Claude + Copilot, recalcule maxUtilization sur les
      quotas critiques (session + weekly all + premium-requests), regénère l'icône.
    * Tooltip dynamique : "Claude session 64% · Copilot 12% · resets in 4h 48m"
    * LeftClick : ouvre le TrayFlyout (Prompt 14).
    * RightClick : ContextFlyout avec "Open dashboard", "Refresh now", "Settings", "Exit".

Initialise TrayController dans App.xaml.cs OnLaunched APRÈS AppHost.StartAsync.

Ajoute un argument `--debug-fake-quota=0.8` qui injecte un faux snapshot pour
tester visuellement la coloration.

Livre : diff + screenshot de l'icône taskbar dans les 4 états (bleu/vert/orange/rouge).
```

**Acceptance** : L'icône change de couleur selon les snapshots reçus.

---

### Prompt 14 — Tray flyout (widget compact)

```
Crée :
- src\WinAIBar\Views\Flyouts\TrayFlyout.xaml (+ .xaml.cs) : Window borderless,
  taille 360×420, positionnée au-dessus de la zone de notification (calcul via
  `Shell_NotifyIconGetRect` ou simplement écran.WorkArea.BottomRight - taille).
  Contenu :
    * Header : logo + titre "WinAIBar" + bouton ⚙ qui ouvre la fenêtre détaillée.
    * Section Claude :
        - Barre "Session 5h" avec % et "resets in 4h 48m"
        - Barre "Weekly · All" avec % et "resets in 3d 12h"
    * Section Copilot :
        - Barre "Premium requests" avec % et resets monthly
        - Barre "Credits" avec montant restant.
    * Footer : "Last update: 14:23" + bouton Refresh (cooldown 15s, désactivé
      pendant le cooldown).
- src\WinAIBar\ViewModels\TrayFlyoutViewModel.cs : ObservableObject avec
  ObservableProperty pour chaque barre, RelayCommand RefreshCommand.
- src\WinAIBar\Views\Controls\QuotaBar.xaml (UserControl simple : label + barre
  ProgressBar colorée + texte droite "65% · resets in 4h 48m").

Le flyout se ferme si la fenêtre perd le focus (`Deactivated` event).

Livre : diff + screenshot du flyout ouvert.
```

**Acceptance** : Clic gauche sur l'icône tray → flyout apparaît ; perd focus → disparaît.

---

### Prompt 15 — Dashboard page

```
Crée :
- src\WinAIBar\ViewModels\DashboardViewModel.cs : ObservableObject avec
  ObservableProperty `ClaudeSnapshot`, `CopilotSnapshot`, `LastRefreshedAt`,
  `RefreshCommand` (cooldown 15s).
- Remplace src\WinAIBar\Views\Pages\DashboardPage.xaml par une grille deux
  colonnes :
    [ Claude ]                | [ Copilot ]
    Header avec ProviderStatus|
    UsageGauge × 3 majeurs    | UsageGauge × 2 majeurs
    Liste compacte autres quotas

  Pour l'instant utilise un UserControl placeholder `UsageGauge` qui affiche
  juste un ProgressBar circulaire + label (sera amélioré en Prompt 16).
  
- Ajoute une barre top avec bouton "Refresh now" + horodatage.
- DI : DashboardViewModel est Transient, instancié depuis le Frame.Navigate.

Livre : diff + screenshot du dashboard avec données réelles Claude (Copilot
restera vide tant que pas loggué).
```

**Acceptance** : Le dashboard reflète les snapshots Claude.

---

## Phase 5 — Charts + historique

### Prompt 16 — Contrôle UsageGauge avec LiveChartsCore

```
Ajoute le package LiveChartsCore.SkiaSharpView.WinUI.

Crée src\WinAIBar\Views\Controls\UsageGauge.xaml (+ .xaml.cs) : UserControl
WinUI 3 avec DependencyProperties :
    double Value (0..1+)
    string Label
    DateTimeOffset? ResetsAt
    string? Subtitle (ex. "12 345 / 50 000 tokens")

Rendu : `PieChart` semi-circulaire de LiveChartsCore (`InnerRadius=60`,
`MaxAngle=270`, `InitialRotation=-135`), 2 séries :
    - Value (couleur dynamique selon seuils — réutilise la palette du Prompt 13)
    - Reste (couleur LightGray à 30% opacité)
Au centre : `Value` en gros (32pt), `Label` en dessous, `Subtitle` en petit,
`ResetsAt` formaté "in 4h 48m" en très petit gris.

Accessibilité : AutomationProperties.Name = "$"Label : {percent}%, resets in {duration}".

Remplace les placeholders du Prompt 15 par ce contrôle.

Livre : diff + screenshot des UsageGauges en différents états.
```

**Acceptance** : Visuel proche d'une jauge type "Apple Watch fitness ring".

---

### Prompt 17 — Page History + sparklines 14j

```
Remplace HistoryPage.xaml par :
- ComboBox filtre provider (Tous / Claude / Copilot)
- ComboBox filtre période (24h / 7j / 14j / 30j)
- Pour chaque quota distinct sur la période :
    - Header (label + provider + utilization actuelle)
    - `CartesianChart` LiveChartsCore avec :
        - Axe X : DateTimeAxis avec labels auto.
        - Axe Y : 0..max observé (avec marge 10%).
        - LineSeries lissée, couleur selon provider.
        - Section colorée >75% en jaune transparent, >90% en rouge transparent.

Crée src\WinAIBar\ViewModels\HistoryViewModel.cs : charge via `IHistoryRepository.GetRangeAsync`,
groupe par `quota.Key`, projette en `ObservableCollection<ChartSeries>`.

Pagination/perf : si >5000 points, sous-échantillonne en buckets 10min comme
claude-meter.

Livre : diff + screenshot avec >24h de données réelles Claude.
```

**Acceptance** : Sparklines fluides ; filtre période recharge le chart sans bug.

---

## Phase 6 — Health + Cost + Export

### Prompt 18 — Health service + page

```
Crée :
- src\WinAIBar\Services\Health\IHealthService.cs :
    IObservable<ProviderHealth> HealthStream { get; }
    IReadOnlyList<ProviderHealth> Current { get; }
    IReadOnlyList<HealthIncident> RecentIncidents { get; }
    Task<ProviderHealth> ProbeAsync(ProviderId provider, CancellationToken ct);
- src\WinAIBar\Services\Health\HealthIncident.cs : record class
    ProviderId Provider, DateTimeOffset At, string Endpoint, int? HttpStatus,
    string Message, HealthSeverity Severity (enum Info/Warning/Error).
- src\WinAIBar\Services\Health\HealthService.cs : Singleton.
  - Souscrit aux polling services (succès/erreur).
  - Conserve les 50 derniers incidents en mémoire + persiste vers SQLite
    (nouvelle table HealthIncidents, ajoute une migration EF Core).
  - Sur l'endpoint Copilot `copilot_internal/user` : si réponse 200 mais
    schéma JSON ne contient PAS les champs attendus (heuristique : pas de
    `login` ou pas de quotas) → enregistre incident Warning avec message
    "copilot_internal schema may have changed" + payload tronqué.
  - Probe manuel : déclenche un appel unique au client correspondant.

Remplace HealthPage.xaml par :
    - Pour chaque provider, une carte avec :
        - Badge coloré (vert/orange/rouge/gris) selon `ProviderStatus`.
        - Dernier code HTTP, latence, timestamp dernier succès.
        - Bouton "Probe now" qui appelle `ProbeAsync`.
    - Liste virtualisée des incidents (DataGrid ou ItemsRepeater) triée
      desc par date.
    - Bouton "Copy diagnostic to clipboard" qui copie un résumé texte
      (provider + url + status + corrélation).

Livre : diff + migration EF + screenshot Health avec un faux 404 simulé.
```

**Acceptance** : Débrancher le réseau passe les badges en rouge en <2 polling cycles ; un incident apparaît dans la liste.

---

### Prompt 19 — Pricing + page Cost

```
Crée :
- src\WinAIBar\Services\Pricing\PricingTable.cs : record class avec
    Dictionary<string, ModelPricing> Models;
    DateTimeOffset UpdatedAt;
    string Source;
- ModelPricing : record class { decimal InputPerMillionTokens; decimal OutputPerMillionTokens;
    decimal? CachedInputPerMillionTokens; }
- src\WinAIBar\Resources\pricing.json : embarqué dans le projet (Build Action
  EmbeddedResource). Contenu initial (à mettre à jour manuellement) :
    {
      "updatedAt": "2026-05-20",
      "source": "Manuellement saisi depuis docs Anthropic + GitHub Copilot",
      "models": {
        "claude-sonnet-4-6": { "input": 3.0, "output": 15.0, "cachedInput": 0.30 },
        "claude-opus-4-7":   { "input": 15.0, "output": 75.0, "cachedInput": 1.50 },
        "claude-haiku-4-5":  { "input": 0.80, "output": 4.0,  "cachedInput": 0.08 },
        "copilot-credit":    { "input": 0,    "output": 0,    "creditUsd": 1.00 }
      }
    }
- src\WinAIBar\Services\Pricing\IPricingProvider.cs : Task<PricingTable> GetAsync();
- src\WinAIBar\Services\Pricing\IPricingEstimator.cs :
    decimal EstimateCost(ProviderSnapshot snapshot, PricingTable table);
    decimal EstimateForRange(IEnumerable<ProviderSnapshot> snapshots, PricingTable table);

Remplace CostPage.xaml par :
    - 3 cartes "Aujourd'hui", "Cette semaine", "Ce mois" avec total $ + delta vs période précédente.
    - `CartesianChart` cumulatif (StackedAreaSeries) par modèle.
    - Tableau détaillé : modèle | tokens | input $ | output $ | cached $ | total $.
    - Mention "Estimation indicative, basée sur pricing.json updated 2026-05-20".

Livre : diff + screenshot avec données réelles ou simulées.
```

**Acceptance** : La page affiche des montants > 0 cohérents après ≥1 jour de données.

---

### Prompt 20 — Export historique CSV/JSON

```
Crée :
- src\WinAIBar\Services\Export\IExportService.cs :
    Task ExportCsvAsync(ProviderId? provider, DateTimeOffset from, DateTimeOffset to, string targetPath, CancellationToken ct);
    Task ExportJsonAsync(...);
- src\WinAIBar\Services\Export\ExportService.cs : implémentation. CSV : colonnes
  CapturedAt, Provider, QuotaKey, Label, Utilization, Used, Limit, Unit, ResetsAt.
  JSON : tableau de ProviderSnapshot, JsonSerializer indent.

Ajoute à HistoryPage un bouton "Export…" qui :
- Ouvre `FileSavePicker` WinUI 3 (filtre CSV / JSON).
- Appelle ExportService.
- Toast de succès avec lien "Ouvrir le dossier".

Ajoute à SettingsPage (Prompt 21) un bouton "Open data folder" qui ouvre
`%LocalAppData%\WinAIBar` dans l'Explorateur via `Process.Start("explorer.exe", path)`.

Livre : diff + un fichier exporté ouvert dans Excel et VS Code.
```

**Acceptance** : Les exports s'ouvrent correctement, données complètes sur la période.

---

## Phase 7 — Settings + lifecycle

### Prompt 21 — Page Settings complète

```
Ajoute le package CommunityToolkit.WinUI.Controls.SettingsControls.

Remplace SettingsPage.xaml par une ScrollView contenant des SettingsExpander :

1. **Compte Claude**
   - Statut credentials (présent/expiré/manquant)
   - Bouton "Ouvrir le dossier .claude" → explorer.exe %USERPROFILE%\.claude
   - Bouton "Re-tester maintenant"

2. **Compte GitHub Copilot**
   - Statut (token présent ? login récupéré ?)
   - Bouton "Se connecter avec GitHub" → lance Prompt 09 + Prompt 10
     (dialog avec UserCode + VerificationUri + Copy + Open browser, polling
     en arrière-plan, dialog se ferme sur succès)
   - Champ "Coller un PAT manuellement" + bouton Save → IGitHubTokenStore
   - Bouton "Déconnexion" → IGitHubTokenStore.ClearAsync

3. **Refresh**
   - Intervalle Claude (NumberBox minutes, défaut 7)
   - Intervalle Copilot (défaut 5)
   - Toggle "Polling adaptatif idle"

4. **Alertes**
   - 3 NumberBox seuils (75/90/95% par défaut)
   - Toggle "Toasts actifs"

5. **Démarrage**
   - Toggle "Lancer au démarrage de Windows" → IStartupService (Prompt 22)
   - Toggle "Démarrer minimisé en tray"

6. **Données**
   - Bouton "Open data folder"
   - Bouton "Reset history" (confirmation modale)
   - Slider rétention max (jours, défaut 90)

7. **À propos**
   - Version + date build
   - Lien GitHub release
   - Lien vers les sources des données (Anthropic / GitHub APIs)

Persiste tous les paramètres via `IConfiguration` + un service
`ISettingsStore` qui sérialise en `%LocalAppData%\WinAIBar\settings.json`.
NE PAS utiliser `ApplicationData.Current.LocalSettings` (réservé aux apps
packagées).

Livre : diff + screenshot de chaque expander.
```

**Acceptance** : Tous les paramètres sont persistés ; le redémarrage charge les valeurs sauvegardées.

---

### Prompt 22 — Autostart via clé registre HKCU\Run

```
Crée :
- src\WinAIBar\Services\Autostart\IStartupService.cs :
    bool IsEnabled();
    Task EnableAsync();
    Task DisableAsync();
- src\WinAIBar\Services\Autostart\StartupService.cs :
  - Clé : HKCU\Software\Microsoft\Windows\CurrentVersion\Run
  - Nom : "WinAIBar"
  - Valeur : "\"<path to WinAIBar.exe>\" --silent"
  - Utilise Microsoft.Win32.Registry (Windows-only).
  - EnableAsync : écrit la valeur (chemin courant Process.GetCurrentProcess().MainModule.FileName).
  - DisableAsync : supprime la valeur si présente (no-op si absente).

Dans App.xaml.cs OnLaunched : si args contient "--silent", ne pas ouvrir
MainWindow mais laisser seulement le tray actif. Ajoute une option
ShowWindowFromTray() exposée via une commande "Open" du context menu.

Câble dans SettingsPage le toggle "Lancer au démarrage" sur IStartupService.

Tests : utilise une sous-clé de test sous HKCU pour valider Enable/Disable
sans polluer la vraie clé Run.

Livre : diff + test manuel : toggle ON, redémarrer Windows, l'app doit
apparaître dans le tray sans fenêtre.
```

**Acceptance** : Comportement vérifié.

---

## Phase 8 — Polish + packaging

### Prompt 23 — Toasts + Velopack + page About

```
Ajoute le package CommunityToolkit.WinUI.Notifications.

Crée :
- src\WinAIBar\Services\Notifications\IThresholdNotifier.cs :
    void NotifyIfBreached(ProviderSnapshot snapshot, IReadOnlyList<double> thresholds);
- src\WinAIBar\Services\Notifications\ThresholdNotifier.cs :
  Garde un état (dernier seuil franchi par quota) pour ne pas spammer.
  Toast WinUI : titre "Claude session at 90%", corps "Resets in 1h 12m",
  bouton "Open dashboard".

Câble : souscrit aux messengers Claude + Copilot, lit les seuils depuis
ISettingsStore.

---

**Velopack** :

Ajoute le package Velopack au projet WinAIBar.

Dans App.xaml.cs, AVANT toute autre initialisation, appelle :
    VelopackApp.Build().Run();
Cela gère les hooks d'install/update.

Crée :
- src\WinAIBar\Services\Update\IUpdateService.cs : Task CheckAsync();
- src\WinAIBar\Services\Update\UpdateService.cs :
    UpdateManager mgr = new(new GithubSource("https://github.com/<user>/WinAIBar", null, false));
    var info = await mgr.CheckForUpdatesAsync();
    if (info != null) await mgr.DownloadUpdatesAsync(info);
    Si update prête, planifie `mgr.ApplyUpdatesAndRestart(info)` au prochain
    `MainWindow.Closed`.

Appelle UpdateService.CheckAsync 5s après le démarrage, puis toutes les 6h.

Ajoute à SettingsPage section "À propos" :
- Version (Assembly.GetExecutingAssembly().GetName().Version)
- Bouton "Vérifier les mises à jour maintenant"
- Date dernière vérif
- Lien GitHub releases

---

**Build script** :

Crée `build.ps1` à la racine :
```
dotnet publish src\WinAIBar -c Release -r win-x64 --self-contained -o publish\win-x64
vpk pack -u WinAIBar -v 0.1.0 -p publish\win-x64 -e WinAIBar.exe --packTitle "WinAIBar" --packAuthors "Nicolas Dufaut"
```

Livre : diff + le dossier `releases\` produit avec un .nupkg + un Setup.exe
+ un RELEASES, plus une démo d'auto-update en bumpant à 0.1.1.
```

**Acceptance** : `build.ps1` produit un installeur fonctionnel ; lancement → tray présent ; bump version → update détectée au démarrage.

---

## Annexes

### Convention de réponse attendue de l'IA pour chaque prompt

1. **Plan** : 2-3 lignes décrivant l'approche.
2. **Diff complet** : tous les fichiers créés/modifiés intégralement.
3. **Commandes** : la séquence à exécuter (`dotnet restore`, `dotnet build`, `dotnet test`, scénario manuel).
4. **Acceptance** : confirmer point par point les critères listés.
5. **Pas de hors-sujet** : aucun refactoring, aucune dépendance non listée, aucun fichier supplémentaire (README, .gitignore exclus — déjà créés au prompt 01 si demandé).

### Stratégie de validation incrémentale

Après chaque prompt :
```powershell
dotnet build
dotnet test
git add -A
git commit -m "prompt-NN: <résumé>"
```

Ne passe **jamais** au prompt suivant si :
- `dotnet build` échoue ou produit des warnings.
- Les acceptance criteria ne sont pas remplis.
- Tu n'as pas commité (rollback facile).

### Sources techniques

- Claude usage API : `https://api.anthropic.com/api/oauth/usage` (header `anthropic-beta: oauth-2025-04-20`)
- Copilot internal : `https://api.github.com/copilot_internal/user` (header `X-GitHub-Api-Version: 2025-05-01`) — **non documenté, surveillé par Health**
- Copilot billing public : `https://api.github.com/users/{username}/settings/billing/premium_request/usage`
- GitHub Copilot OAuth public client : `Iv1.b507a08c87ecfe98`
- Inspirations UI : [JackBhanded/claude-meter](https://github.com/JackBhanded/claude-meter), [estruyf/github-copilot-usage-tauri](https://github.com/estruyf/github-copilot-usage-tauri)
