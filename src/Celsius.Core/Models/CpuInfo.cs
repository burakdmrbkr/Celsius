using Celsius.Core.Models;

namespace Celsius.Core.Models;

/// <summary>
/// Detected CPU identity, used to resolve the thermal specification.
/// </summary>
/// <param name="Name">Raw marketing name, e.g. "12th Gen Intel(R) Core(TM) i7-12700K".</param>
/// <param name="Vendor">CPU vendor.</param>
/// <param name="ModelKey">Normalized model token for lookups, e.g. "i7-12700k".</param>
public sealed record CpuInfo(string Name, CpuVendor Vendor, string ModelKey);

/// <summary>Known CPU vendors.</summary>
public enum CpuVendor
{
    /// <summary>Vendor could not be determined.</summary>
    Unknown = 0,

    /// <summary>Intel.</summary>
    Intel,

    /// <summary>AMD.</summary>
    Amd,
}

/// <summary>
/// Physical graphics adapter identity, discovered via DXGI (plus WMI fallback).
/// </summary>
/// <param name="Name">Adapter description.</param>
/// <param name="Vendor">Vendor derived from the PCI vendor id.</param>
/// <param name="VendorId">Raw PCI vendor id, e.g. 0x10DE.</param>
/// <param name="DeviceId">Raw PCI device id, used to correlate with sensor backends.</param>
/// <param name="Luid">Adapter LUID, used to correlate with sensor backends.</param>
/// <param name="IsDiscrete">True when the adapter is considered discrete.</param>
/// <param name="IsDisplayAdapter">True when the adapter is currently driving a display output.</param>
public sealed record GpuInfo(
    string Name,
    GpuVendor Vendor,
    uint VendorId,
    uint DeviceId,
    long Luid,
    bool IsDiscrete,
    bool IsDisplayAdapter);
