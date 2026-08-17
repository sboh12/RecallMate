# RecallMate — VS Code Setup Guide (Windows 10)

A minimal WPF starter app: a searchable timeline of "snapshots" (window activity),
built with .NET 9 and the CommunityToolkit.Mvvm source generators.

WPF is Windows-only, so this must be built and run **on a Windows 10 machine**
(not WSL, not Mac/Linux).

## What was fixed vs. the original spec

A couple of small bugs in the original snippet were corrected so the project
actually compiles and runs:
- `IDataService.cs` was missing `using RecallMate.Models;` — added.
- `MainWindow.xaml` set `<Window.DataContext><vm:MainViewModel /></Window.DataContext>`,
  but `MainViewModel` has no parameterless constructor (it requires an
  `IDataService`). This would throw at runtime. Removed it from XAML — the
  code-behind already sets `DataContext` correctly with a `DummyDataService`.
- Added the missing `App.xaml.cs` code-behind that `App.xaml` requires.

## 1. Install prerequisites

1. **.NET 9 SDK**
   Download and run the installer: https://dotnet.microsoft.com/download/dotnet/9.0
   Verify in a terminal:
   ```
   dotnet --version
   ```
   should print something starting with `9.`

2. **Visual Studio Code**
   https://code.visualstudio.com/

3. **VS Code extensions** (open VS Code → Extensions panel → search & install):
   - **C# Dev Kit** (by Microsoft) — includes the base C# extension, IntelliSense,
     debugger, and test explorer.
   - Optional but helpful: **XAML Styler** for formatting `.xaml` files.

   > Note: WPF XAML doesn't get a live visual designer in VS Code (that's a
   > Visual Studio-only feature). You'll edit XAML as text and run the app to
   > see changes — perfectly workable for a project this size.

## 2. Get the project onto disk

Unzip `RecallMate.zip` anywhere, e.g. `C:\Projects\RecallMate`.
You should see:
```
RecallMate/
├── RecallMate.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Models/
│   └── Snapshot.cs
├── ViewModels/
│   └── MainViewModel.cs
└── Services/
    ├── IDataService.cs
    └── DummyDataService.cs
```

## 3. Open and restore

1. Open VS Code.
2. `File → Open Folder…` → select the `RecallMate` folder.
3. When prompted "Required assets to build and debug are missing", click **Yes**
   (this generates a `.vscode/launch.json` and `tasks.json` for you).
4. Open a terminal in VS Code (`` Ctrl+` ``) and run:
   ```
   dotnet restore
   ```
   This pulls down `CommunityToolkit.Mvvm`.

## 4. Build

```
dotnet build
```

If this is the first time building, the MVVM Toolkit's source generators
create the `SearchQuery`, `StatusText`, `SearchCommand`, and `RefreshCommand`
members on `MainViewModel` automatically — you won't see them in the `.cs`
file itself, that's expected.

## 5. Run

Either:
- Press **F5** in VS Code (uses the auto-generated launch config), or
- From the terminal:
  ```
  dotnet run
  ```

A window titled "RecallMate" should appear with a search box and a list of
5 dummy snapshots grouped by day. Typing something like `sprint` and hitting
Enter (or clicking Search) filters the list.

## 6. What's now real: capture, storage, and URLs

Three pieces got wired up:

**SQLite persistence** — `Services/SqliteDataService.cs` replaces the
in-memory `DummyDataService` as what the app actually uses. Data lives at
`%LocalAppData%\RecallMate\recallmate.db` and survives restarts. On first
run (empty DB) it seeds the same 5 sample rows the old dummy service had, so
the timeline isn't blank before capture kicks in. `DummyDataService.cs` is
still in the project as a reference/testing implementation, just unused.

**Self-exclusion** — `WindowCaptureService` now compares the foreground
window's process ID against `Environment.ProcessId` and skips capturing
RecallMate's own window. Without this, clicking Search or scrolling the
timeline would spam entries into the history you're trying to browse.

**Browser URL capture** — `Services/BrowserUrlHelper.cs` uses UI Automation
to read whatever's currently in the address bar of Chrome, Edge, Firefox,
Brave, Opera, or Vivaldi, and `WindowCaptureService` attaches it to
`Snapshot.Url` when the foreground window belongs to one of those processes.
This is best-effort, not a network hook:
- It only sees what's *displayed* in the address bar right now — not full
  navigation history within a tab.
- It relies on the browser exposing standard accessibility (`Name` containing
  "address" or "search" on an `Edit` control), which all major Chromium/Gecko
  browsers do, but a UI update on their end could change that and silently
  break detection. Wrapped in try/catch so it degrades to `Url = null` rather
  than crashing.
- The `UIAutomationClient`/`UIAutomationTypes` references added to the
  `.csproj` are what make `System.Windows.Automation` available — they ship
  with the Windows Desktop runtime, no extra install needed.

Try it: run the app, browse to a site in Chrome/Edge/Firefox, switch away and
back — the new timeline entry should show the URL. Then click into
RecallMate's own search box for a few seconds and confirm no entry for
RecallMate itself shows up.

## 7. Where to go from here

`IDataService`, `IWindowCaptureService`, and `BrowserUrlHelper` are the seams
left to build on:
- Semantic search (embeddings via ONNX Runtime) instead of the current
  `LIKE`-based `SearchAsync` in `SqliteDataService`.
- An LLM call to fill in `Snapshot.Summary` for each capture.
- A settings screen to change the poll interval or exclude specific apps
  beyond just RecallMate itself.

## Troubleshooting

- **"error NETSDK1100: To build a project targeting Windows..."** — you're
  building on a non-Windows machine. WPF only builds/runs on Windows.
- **C# Dev Kit asks you to sign in** — a free Microsoft account sign-in is
  required to use C# Dev Kit's debugger; the base C# extension works without
  it if you'd rather skip that.
- **`dotnet` not recognized** — the SDK installer usually updates PATH
  automatically, but you may need to restart VS Code (or your terminal) after
  installing it.
