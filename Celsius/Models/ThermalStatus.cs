namespace Celsius.Models;

/// <summary>İşlemcinin termal durum sınıflandırması.</summary>
public enum ThermalStatus
{
    Healthy = 0,
    Warm = 1,
    Caution = 2,
    MaintenanceRequired = 3,
    Critical = 4,
    Unavailable = 5
}