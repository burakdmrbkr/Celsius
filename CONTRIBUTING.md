# Contributing to Celsius

Thanks for your interest in improving Celsius! This document explains how to get
set up and what we expect from contributions.

## Getting started

1. **Prerequisites**
   - Windows 10/11 (x64)
   - [.NET 10 SDK](https://dotnet.microsoft.com/download)
   - A Git client

2. **Clone and build**

   ```powershell
   git clone https://github.com/burakdmrbkr/Celsius.git
   cd Celsius
   dotnet build Celsius.slnx -c Release
   dotnet test  Celsius.slnx -c Release
   ```

3. **Run the app** (will prompt for elevation — this is required for CPU
   temperature sensors):

   ```powershell
   dotnet run --project src/Celsius.App
   ```

There is also a console harness for verifying hardware access without the UI:

```powershell
dotnet run --project tools/SmokeTest
```

## Project layout

- `src/Celsius.Core` — hardware, thermal, stress and settings logic (no UI).
- `src/Celsius.App` — WPF dashboard, stress window, settings and tray icon.
- `tests/Celsius.Core.Tests` — xUnit tests for the core.
- `docs` — architecture notes and screenshots.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for a deeper look.

## Coding guidelines

- Target **.NET 10**, `Nullable` and `ImplicitUsings` enabled (see
  `Directory.Build.props`).
- Keep **all hardware access inside `Celsius.Core`** and behind
  `IHardwareMonitor` so it stays testable. The UI must not touch
  LibreHardwareMonitor or WMI directly.
- Wrap hardware/WMI calls in try/catch and **degrade gracefully**; log errors via
  `FileLogger` (errors/critical only).
- Prefer immutable models returned to the UI (e.g. `SystemSnapshot`).
- Never poll sensors on the UI thread.
- Add XML doc comments to public types and members.
- Keep UI strings in the `.resx` files (add both English and Turkish entries).

## Tests

- Add unit tests to `Celsius.Core.Tests` for any new logic.
- Tests must **not** require real hardware, elevation or a display — use fakes or
  pure functions.
- Run `dotnet test` before opening a pull request.

## Pull requests

1. Fork the repository and create a feature branch.
2. Make your change with focused commits.
3. Ensure `dotnet build -c Release` and `dotnet test` both succeed.
4. Fill out the pull request template, linking any related issues.
5. Keep pull requests scoped — one logical change per PR.

## Reporting bugs & requesting features

Use the issue templates on GitHub. For bug reports, please include your Celsius
version, Windows version, CPU/GPU model, whether you ran as administrator, and
any relevant log lines from `%AppData%\Celsius\logs`.

## License

By contributing, you agree that your contributions are licensed under the
[MIT License](LICENSE).
