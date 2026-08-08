namespace Celsius.Models;

/// <summary>Sol menüdeki bir gezinme öğesi.</summary>
public class NavItem
{
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required object ViewModel { get; init; }
}