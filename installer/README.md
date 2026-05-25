# Aevix installer

Two ways to ship Aevix to a Windows user:

| What | Where | Notes |
| --- | --- | --- |
| **Portable zip** | `publish\Aevix-Windows-v1.0-win-x64.zip` (~232 MB) | Unzip + run `Aevix.App.exe`. No install. |
| **Setup .exe** | `publish\installer\Aevix-Setup-v1.0.0.exe` (~232 MB) | Standard Windows installer — Program Files, Start Menu, uninstall entry. |

## Building the setup .exe

One-time:

1. Install **Inno Setup 6** — https://jrsoftware.org/isdl.php
2. Default install path is fine; the build script auto-finds `ISCC.exe`.

Then any time:

```powershell
pwsh installer\build.ps1
```

That:
1. Runs `dotnet publish -c Release -r win-x64 --self-contained` into `publish\win-x64\`
2. Runs `ISCC.exe installer\Aevix.iss`
3. Drops `publish\installer\Aevix-Setup-v1.0.0.exe`

If you only tweaked `Aevix.iss` and don't need a fresh publish, pass `-NoPublish`:

```powershell
pwsh installer\build.ps1 -NoPublish
```

## What the installer does

- Installs into `%ProgramFiles%\Aevix\` (per-machine, requires admin).
- Creates Start Menu entry and an opt-in Desktop shortcut.
- Registers an uninstaller in Add/Remove Programs.
- Leaves `%LOCALAPPDATA%\Aevix\` (playlists, settings, crash log) untouched
  on uninstall. Uncomment the line in `[UninstallDelete]` if you want a
  full wipe on uninstall.

## Bumping the version

Edit the `#define AppVersion` line at the top of `Aevix.iss`. The script
uses it for both the wizard banner and the output filename.
