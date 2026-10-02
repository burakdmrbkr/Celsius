using Celsius.Core.Models;
using Vortice.DXGI;

namespace Celsius.Core.Gpu;

/// <summary>
/// Enumerates physical graphics adapters using DXGI, which reports correct
/// video memory sizes (unlike WMI's 32-bit <c>AdapterRAM</c>) and reliable
/// vendor ids.
/// </summary>
public sealed class GpuInfoProvider
{
    private const uint VendorNvidia = 0x10DE;
    private const uint VendorAmd = 0x1002;
    private const uint VendorIntel = 0x8086;
    private const uint VendorMicrosoft = 0x1414;

    // Discrete GPUs are expected to expose at least this much dedicated memory.
    private const ulong DiscreteMemoryThresholdBytes = 1UL * 1024 * 1024 * 1024;

    /// <summary>
    /// Returns every physical adapter found, or an empty list when enumeration
    /// is unavailable. Never throws.
    /// </summary>
    public IReadOnlyList<GpuInfo> EnumerateGpus()
    {
        var result = new List<GpuInfo>();

        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

            for (uint index = 0; ; index++)
            {
                var hr = factory.EnumAdapters1(index, out var adapter);
                if (hr.Failure || adapter is null)
                {
                    break;
                }

                using (adapter)
                {
                    var desc = adapter.Description1;
                    var vendor = MapVendor(desc.VendorId);

                    // Skip the Microsoft software/WARP adapter.
                    if (vendor == GpuVendor.Microsoft)
                    {
                        continue;
                    }

                    var isDisplayAdapter = HasOutput(adapter);
                    var luid = ((long)desc.Luid.HighPart << 32) | (uint)desc.Luid.LowPart;

                    result.Add(new GpuInfo(
                        Name: desc.Description.TrimEnd('\0').Trim(),
                        Vendor: vendor,
                        VendorId: desc.VendorId,
                        DeviceId: desc.DeviceId,
                        Luid: luid,
                        IsDiscrete: IsDiscrete(vendor, desc.DedicatedVideoMemory),
                        IsDisplayAdapter: isDisplayAdapter));
                }
            }
        }
        catch
        {
            // DXGI enumeration can fail in constrained environments; return
            // whatever we have rather than crashing the monitor.
        }

        return result;
    }

    private static bool HasOutput(IDXGIAdapter1 adapter)
    {
        try
        {
            return adapter.EnumOutputs(0, out var output).Success && output is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Maps a raw PCI vendor id to the <see cref="GpuVendor"/> enum.</summary>
    internal static GpuVendor MapVendor(uint vendorId) => vendorId switch
    {
        VendorNvidia => GpuVendor.Nvidia,
        VendorAmd => GpuVendor.Amd,
        VendorIntel => GpuVendor.Intel,
        VendorMicrosoft => GpuVendor.Microsoft,
        0 => GpuVendor.Unknown,
        _ => GpuVendor.Other,
    };

    /// <summary>
    /// Heuristically decides whether an adapter is discrete. NVIDIA is always
    /// treated as discrete; AMD/Intel are discrete when they expose a large
    /// amount of dedicated memory.
    /// </summary>
    internal static bool IsDiscrete(GpuVendor vendor, ulong dedicatedVideoMemory) => vendor switch
    {
        GpuVendor.Nvidia => true,
        GpuVendor.Amd or GpuVendor.Intel => dedicatedVideoMemory >= DiscreteMemoryThresholdBytes,
        _ => false,
    };
}
