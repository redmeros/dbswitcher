# DBSwitcher

WPF tool (.NET 10, x64) for switching the database configuration of Autodesk Advance Steel 2023/2027 and Revit 2023.
Each switch rewrites `DatabaseConfiguration.xml` (after making a backup) and, when the config asks for it,
replaces the `support` directory with a symbolic link. If any step fails, the earlier steps are rolled back.

## Requirements

- Windows, .NET 10 SDK
- GNU make and Git for Windows (the Makefile uses Git's `sh.exe`; override the location with `GIT_BIN=...`)
- `vpk` for releases: `dotnet tool install -g vpk --version 1.2.161`
- Creating symbolic links needs administrator rights or Windows Developer Mode.
  Without it, switching to a config that uses a link fails with a message and changes nothing.

## Commands

| Command        | What it does                                                     |
|----------------|------------------------------------------------------------------|
| `make run`     | Run the app                                                      |
| `make build`   | Build (`CONFIG=Release` by default)                              |
| `make test`    | Run the NUnit tests (they use a fake ProgramData, not the real one) |
| `make clean`   | Remove build output and `publish/`                               |
| `make version` | Show the last released version and the next one                  |
| `make release` | Publish, build the Velopack package and copy it to the UNC share |

## Configs

Config files live in `DBSwitcher/configs/` and are copied next to the exe:

- `AS<year>_<name>.config.json` for Advance Steel
- `RVT<year>_<name>.config.json` for Revit

Files that are not valid JSON or do not match the name pattern are skipped, and the app shows which ones.
Each config lists data sources and, optionally, a support directory link. Data sources are compared
regardless of order.

## Versioning and releases

The `VERSION` file in the repo root holds the last released version and is the only place it is stored.
The build reads it, and a test checks that the assembly version matches.

```
make release                # 1.2.5 -> 1.2.6
make release BUMP=minor     # 1.2.5 -> 1.3.0
make release BUMP=major     # 1.2.5 -> 2.0.0
make release VERSION=1.4.0  # explicit version
```

`make release` refuses to run with uncommitted changes to tracked files (override with `FORCE=1`).
`VERSION` is written only after the release succeeds, so commit it afterwards.
The package goes to the share set by `UNC` in the Makefile.

## Updates

The app is installed with Velopack: run `Setup.exe` from the UNC share once. On startup an installed copy
checks the same share, and if there is a newer version it asks whether to update.
The update never interrupts a switch in progress; the prompt and the restart wait until it finishes.
A copy run from `make run` is never updated. If the share is unreachable, the check is skipped
and a warning goes to the log.

The packages are not code-signed, so anyone who can write to the share can ship code to every install.
Keep write access to the share restricted.

## Logs

NLog writes the logs; see `DBSwitcher/NLog.config` for the location.
