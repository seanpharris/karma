namespace Karma.Data;

using System.Collections.Generic;
using System.Linq;

public enum LeaderboardRole
{
    None,
    Saint,
    Scourge
}

public sealed class PlayerState
{
    public PlayerState(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public PlayerKarma Karma { get; } = new();
    public bool IsAlive { get; private set; } = true;
    public int MaxHealth { get; } = 100;
    public int Health { get; private set; } = 100;
    public int Scrip { get; private set; }
    public TilePosition Position { get; private set; } = TilePosition.Origin;
    public PlayerAppearanceSelection Appearance { get; private set; } = PlayerAppearanceSelection.Default;
    public string TeamId { get; private set; } = string.Empty;
    public bool HasTeam => !string.IsNullOrWhiteSpace(TeamId);
    public IReadOnlyList<GameItem> Inventory => _inventory;
    public IReadOnlyDictionary<EquipmentSlot, GameItem> Equipment => _equipment;

    private readonly List<GameItem> _inventory = new();
    private readonly Dictionary<EquipmentSlot, GameItem> _equipment = new();

    public void ApplyKarma(int amount)
    {
        Karma.Apply(amount);
    }

    public void KarmaBreak()
    {
        Karma.Reset();
        IsAlive = true;
        Health = MaxHealth;
        ClearTeamStatus();
    }

    public void SetTeam(string teamId)
    {
        TeamId = teamId.Trim();
    }

    public void ClearTeamStatus()
    {
        TeamId = string.Empty;
    }

    public void SetPosition(TilePosition position)
    {
        Position = position;
    }

    public void SetAppearance(PlayerAppearanceSelection appearance)
    {
        Appearance = appearance.Normalized();
    }

    public void AddItem(GameItem item)
    {
        _inventory.Add(item);
    }

    public void AddScrip(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Scrip += amount;
    }

    public bool SpendScrip(int amount)
    {
        if (amount <= 0 || Scrip < amount)
        {
            return false;
        }

        Scrip -= amount;
        return true;
    }

    public bool HasItem(string itemId)
    {
        return _inventory.Any(item => item.Id == itemId);
    }

    public bool ConsumeItem(string itemId)
    {
        var index = _inventory.FindIndex(item => item.Id == itemId);
        if (index < 0)
        {
            return false;
        }

        _inventory.RemoveAt(index);
        return true;
    }

    public IReadOnlyList<GameItem> DrainInventory()
    {
        var items = _inventory.ToArray();
        _inventory.Clear();
        return items;
    }

    public int AttackPower => _equipment.TryGetValue(EquipmentSlot.MainHand, out var weapon)
        ? weapon.Power
        : 5;

    public int Defense => _equipment.Values.Sum(item => item.Defense);

    public bool Equip(GameItem item)
    {
        if (item.Slot == EquipmentSlot.None)
        {
            return false;
        }

        _equipment[item.Slot] = item;
        return true;
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsAlive)
        {
            return false;
        }

        var reducedAmount = System.Math.Max(1, amount - Defense);
        Health = System.Math.Max(0, Health - reducedAmount);
        if (Health > 0)
        {
            return false;
        }

        IsAlive = false;
        return true;
    }

    public void Heal(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        Health = System.Math.Min(MaxHealth, Health + amount);
    }
}

public sealed record PlayerAppearanceSelection(
    string BaseLayerId,
    string SkinLayerId,
    string HairLayerId,
    string OutfitLayerId,
    string HeldToolLayerId)
{
    public static PlayerAppearanceSelection Default { get; } = new(
        "base_body_32x64",
        "skin_medium_32x64",
        "hair_short_dark_32x64",
        "outfit_engineer_32x64",
        string.Empty);

    public IReadOnlyDictionary<string, string> ToLayerIdsBySlot()
    {
        var layers = new Dictionary<string, string>
        {
            ["base"] = BaseLayerId,
            ["skin"] = SkinLayerId,
            ["hair"] = HairLayerId,
            ["outfit"] = OutfitLayerId
        };

        if (!string.IsNullOrWhiteSpace(HeldToolLayerId))
        {
            layers["held_tool"] = HeldToolLayerId;
        }

        return layers;
    }

    public PlayerAppearanceSelection Normalized()
    {
        return new PlayerAppearanceSelection(
            Normalize(BaseLayerId, Default.BaseLayerId),
            Normalize(SkinLayerId, Default.SkinLayerId),
            Normalize(HairLayerId, Default.HairLayerId),
            Normalize(OutfitLayerId, Default.OutfitLayerId),
            Normalize(HeldToolLayerId, Default.HeldToolLayerId));
    }

    private static string Normalize(string layerId, string fallback)
    {
        return string.IsNullOrWhiteSpace(layerId)
            ? fallback
            : layerId.Trim();
    }
}

public readonly record struct TilePosition(int X, int Y)
{
    public static TilePosition Origin { get; } = new(0, 0);

    public int DistanceSquaredTo(TilePosition other)
    {
        var deltaX = X - other.X;
        var deltaY = Y - other.Y;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }
}

public sealed record LeaderboardStanding(
    string ParagonPlayerId,
    string ParagonName,
    int ParagonScore,
    string RenegadePlayerId,
    string RenegadeName,
    int RenegadeScore)
{
    public string SaintPlayerId => ParagonScore > 0 ? ParagonPlayerId : string.Empty;
    public string SaintName => ParagonScore > 0 ? ParagonName : "None";
    public string ScourgePlayerId => RenegadeScore < 0 ? RenegadePlayerId : string.Empty;
    public string ScourgeName => RenegadeScore < 0 ? RenegadeName : "None";

    public string Summary =>
        $"Saint: {SaintName} ({ParagonScore:+#;-#;0}) | " +
        $"Scourge: {ScourgeName} ({RenegadeScore:+#;-#;0})";

    public LeaderboardRole GetRole(string playerId, int score)
    {
        if (score > 0 && SaintPlayerId == playerId)
        {
            return LeaderboardRole.Saint;
        }

        if (score < 0 && ScourgePlayerId == playerId)
        {
            return LeaderboardRole.Scourge;
        }

        return LeaderboardRole.None;
    }
}
