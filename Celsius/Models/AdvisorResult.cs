namespace Celsius.Models;

/// <summary>
/// AI danışman servisinin bir değerlendirme sonucu: durum + kullanıcıya yönelik öneriler.
/// </summary>
public class AdvisorResult
{
    public AdvisorResult(ThermalStatus status, string summary, double? temp, IReadOnlyList<string> suggestions)
    {
        Status = status;
        Summary = summary;
        Temperature = temp;
        Suggestions = suggestions;
    }

    public ThermalStatus Status { get; }
    public string Summary { get; }
    public double? Temperature { get; }
    public IReadOnlyList<string> Suggestions { get; }

    public string SuggestionsJoined => Suggestions.Count == 0 ? "" : string.Join("\n• ", Suggestions.Prepend("•"));
}