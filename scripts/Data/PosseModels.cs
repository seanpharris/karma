using System;

namespace Karma.Data;

public sealed record PosseInfo(string Id, string Name, string LeaderId);

public static class PosseNameGenerator
{
    private static readonly string[] Adjectives =
    {
        "Amber", "Ashen", "Bold", "Bright", "Cinder", "Crimson", "Dusty", "Ember", "Fabled", "Frontier",
        "Golden", "Hollow", "Iron", "Jade", "Kindred", "Lone", "Moonlit", "Nimble", "Prairie", "Quiet",
        "Ragged", "River", "Silver", "Steady", "Storm", "Sunset", "Timber", "Vigilant", "Wild", "Winding"
    };

    private static readonly string[] Animals =
    {
        "Badgers", "Bears", "Coyotes", "Cranes", "Deer", "Eagles", "Elk", "Falcons", "Foxes", "Hawks",
        "Horses", "Lynx", "Mustangs", "Owls", "Ravens", "Stags", "Vipers", "Wolves", "Wrens", "Yaks"
    };

    public static string Generate(string posseId)
    {
        var hash = string.IsNullOrWhiteSpace(posseId)
            ? 0
            : StringComparer.Ordinal.GetHashCode(posseId);
        var adjective = Adjectives[Math.Abs(hash % Adjectives.Length)];
        var animal = Animals[Math.Abs((hash / Adjectives.Length) % Animals.Length)];
        return $"{adjective} {animal}";
    }
}
