# Patched LibreHardwareMonitorLib (0.9.6-celsius1)

Celsius depends on [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
for hardware sensors. Upstream **0.9.6** has a bug that makes **all** sensors
unavailable on some boards:

```csharp
// LibreHardwareMonitorLib/Hardware/SMBios.cs, GetDate()
if (month > 12 || day > 31)
    return null;

return new DateTime(year < 100 ? 1900 + year : year, month, day); // throws for month/day == 0
```

Boards that report a BIOS date like `00/00/0000` (confirmed on an MSI HN B85
with AMI BIOS 7.3.9) make `new DateTime(y, 0, 0)` throw
*"Year, Month, and Day parameters describe an un-representable DateTime."* The
exception escapes `SMBios` → `Computer.Open()`, which aborts the whole hardware
tree: CPU and GPU temperatures, clock, load and memory all come back empty even
though the app runs elevated and PawnIO is installed.

The patch rejects `month`/`day` values below 1 (and out-of-range years) so the
BIOS date becomes `null` instead of throwing. Everything else is untouched.

## Why a local package

The fix lives inside an LHM type (`SMBios`), so it cannot be worked around from
Celsius. Rather than vendoring the entire LHM source tree, we build a patched
`LibreHardwareMonitorLib` NuGet package with a distinct version
(`0.9.6-celsius1`) and consume it from a local feed.

## How to reproduce the package

```powershell
# 1. Clone the matching upstream tag
git clone --depth 1 --branch v0.9.6 `
  https://github.com/LibreHardwareMonitor/LibreHardwareMonitor.git lhm-src

# 2. Apply the patch (see lhm-0.9.6-smbios-date.patch in this folder)
cd lhm-src
git apply ..\Celsius\tools\lhm-patch\lhm-0.9.6-smbios-date.patch

# 3. Pack it for Celsius' target framework
cd LibreHardwareMonitorLib
dotnet pack -c Release -p:Platform=x64 -p:Version=0.9.6-celsius1 `
  -p:TargetFrameworks=net10.0 -o <repo>\local-nuget
```

`nuget.config` in the repository root points at `local-nuget`, and
`src/Celsius.Core/Celsius.Core.csproj` references `0.9.6-celsius1`.

> `local-nuget/` is intentionally git-ignored: the package is a build artifact,
> not source. CI must run the steps above before restoring.

## Removing this patch

Drop it as soon as upstream ships a release containing a `GetDate` guard for
zero month/day (or we adopt such a release). At that point:

1. Point the `PackageReference` back at the official version.
2. Delete this folder, `nuget.config` and the local feed step in CI.
