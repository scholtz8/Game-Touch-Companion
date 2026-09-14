# Game Touch Companion

[English](README.md) | [Español](README.es.md)

**Game Touch Companion** is a Windows application for multi-monitor gaming setups. It lets you interact with maps, guides, wikis, and other web content on a secondary touchscreen **without minimizing the game or intentionally taking foreground focus away from it**.

> **Status:** Experimental / pre-beta. Core functionality is working, but compatibility still needs to be validated across more games, display configurations, DPI settings, and hardware.

## Why this project exists

Many games benefit from having a map, guide, build planner, wiki, or checklist open on a second screen. On a touchscreen setup, however, touching that secondary display can cause the game to lose focus, pause, minimize, or stop receiving input.

Game Touch Companion is designed around a different approach: the Companion window is shown on the secondary display using Windows no-activation behavior so that it can remain usable without becoming the foreground application in supported scenarios.

The project does **not** inject code into games, modify game memory, install drivers, or bypass anti-cheat systems.

## Key features

- Secondary fullscreen Companion window for maps, guides, and web content
- Touch-friendly WebView2 browser
- No-activation window behavior using Win32 APIs
- Multi-monitor selection and layout awareness
- Per-game profiles with saved URLs and display preferences
- Automatic game detection and optional Companion auto-open
- Favorites and configurable home page
- Customizable Companion toolbar
- Light and dark themes
- English and Spanish interface
- System tray integration
- Optional startup with Windows
- Local diagnostics and logging
- Portable self-contained Windows x64 build

## How it works

The Companion window uses Windows APIs such as:

- `WS_EX_NOACTIVATE`
- `WM_MOUSEACTIVATE -> MA_NOACTIVATE`
- `SW_SHOWNOACTIVATE`
- `SWP_NOACTIVATE`

The goal is to keep the game as the foreground window while the user interacts with the Companion on another display.

Actual behavior can vary between games, display modes, input methods, and Windows configurations, so real hardware testing is still required.

## Requirements

### To run the portable build

- Windows 10 or Windows 11 x64
- Microsoft Edge WebView2 Evergreen Runtime
- Two displays recommended
- A touchscreen secondary display is recommended for the intended use case

The portable build is self-contained and includes the required .NET runtime.

### To build from source

- Windows 10/11 x64
- .NET 10 SDK
- Git
- Microsoft Edge WebView2 Evergreen Runtime

## Quick start

1. Launch **Game Touch Companion**.
2. Select the display used by the game and the display used by the Companion.
3. Enter the map, guide, or website URL you want to use.
4. Open the Companion on the secondary display.
5. Start or return to your game.
6. Test touch, scrolling, dragging, and navigation on the secondary display.

For the best results, start by testing games in **Borderless Windowed** mode.

## Game profiles

Profiles can store settings for individual games, including:

- Profile name
- Game executable name
- Map or website URL
- Preferred Companion display
- Automatic opening when the game is detected

Profile data is stored locally in:

```text
%LOCALAPPDATA%\GameTouchCompanion\
```

## Game detection

Game Touch Companion can monitor running processes and match them against saved profiles.

When automatic detection is enabled and a compatible game window is found, the app can prepare and open the corresponding Companion without intentionally activating it.

The application does not start the game itself and does not inject anything into the game process.

## Browser and WebView2 security

The embedded browser is intentionally restricted. The application blocks or limits several behaviors that could create unwanted windows, prompts, or focus changes, including unsupported external schemes and selected browser interactions.

Third-party websites may still behave differently, and login flows, DRM, file pickers, downloads, and unusual browser interactions may require additional compatibility testing.

## Build and test

From the repository root:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'
dotnet restore
dotnet build
dotnet test
```

Run the application:

```powershell
dotnet run --project src/GameTouchCompanion.App/GameTouchCompanion.App.csproj
```

Run the focus test utility:

```powershell
dotnet run --project tools/FocusProbe/FocusProbe.csproj
```

`FocusProbe` is used to verify whether the foreground window changes while interacting with the Companion.

## Optional WebView2 integration tests

Some browser tests require a normal interactive Windows desktop session and are disabled by default.

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
    -p:Platform=x64 `
    --filter 'Category=BrowserRuntime'

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS
```

## Create a portable build

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Publish-Portable.ps1
```

Generated builds are placed under:

```text
artifacts\releases\
```

To validate an extracted portable folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
    -File tools/Test-Portable.ps1 `
    -Folder 'C:\path\to\extracted-folder'
```

Do not run the application directly from inside the ZIP. Extract the full package first.

## Diagnostics

Application logs are stored locally in:

```text
%LOCALAPPDATA%\GameTouchCompanion\Logs
```

Diagnostic helpers are available in the repository:

```powershell
./tools/CollectDiagnostics.ps1
./tools/New-GptHandoff.ps1
```

The application does not include external telemetry or analytics.

## Project structure

```text
Game-Touch-Companion/
├── src/
│   ├── GameTouchCompanion.App/
│   ├── GameTouchCompanion.Core/
│   └── GameTouchCompanion.Native/
├── tests/
├── tools/
├── docs/
├── assets/
└── artifacts/
```

- **App** — WPF user interface, Companion window, WebView2 integration
- **Core** — profiles, configuration, state, and application logic
- **Native** — Win32 interoperability and window/display services
- **tests** — unit and integration tests
- **tools** — FocusProbe, packaging, diagnostics, and test utilities

## Known limitations

- Real touch/focus behavior must be tested on physical hardware.
- Exclusive fullscreen games may behave differently from Borderless Windowed games.
- Some games may stop accepting keyboard or mouse input when another window receives interaction even if the game remains foreground.
- WebView2 behavior can vary between websites.
- Display identifiers such as `\\.\DISPLAY2` may change after reconnecting monitors, docks, ports, or GPUs.
- Keyboard-heavy interaction inside a no-activate Companion window is intentionally limited.

## Beta validation goals

Before calling the project Beta, the current test plan aims to validate:

- Multiple games
- Multiple hardware/display configurations
- Different DPI/scaling combinations
- Controller + touchscreen usage
- Repeated touch interactions without unexpected focus changes
- Longer real-world gaming sessions

See the documentation under [`docs/`](docs/) for the current test plans and technical notes.

## Tech stack

- C#
- .NET 10
- WPF
- Microsoft Edge WebView2
- Win32 / User32
- xUnit
- Serilog

## Privacy

Game Touch Companion is designed to operate locally.

It does not intentionally send your game list, profiles, URLs, logs, or diagnostics to an external service. Web content loaded in WebView2 naturally communicates with the websites you choose to open.

## Disclaimer

Game Touch Companion is an independent experimental project and is not affiliated with Microsoft, game publishers, game developers, or third-party map and guide providers.

Compatibility is not guaranteed for every game or website.

## License

Game Touch Companion is licensed under the GNU General Public License v3.0 or later (`GPL-3.0-or-later`). See the [LICENSE](LICENSE) file for the complete license text.


### Companion touch browsing

The Companion uses browser-style compact tabs whose labels follow each website title, supports per-tab close controls and blank temporary tabs, includes an editable touch address bar, and provides an in-app bottom touch keyboard for web text fields without activating a separate OS keyboard window. Hiding the navigation toolbar also hides the tab strip.
