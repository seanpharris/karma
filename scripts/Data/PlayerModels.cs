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
    public bool IsDown { get; private set; } = false;
    public int MaxHealth { get; } = 100;
    public int Health { get; private set; } = 100;
    public int MaxStamina { get; } = 100;
    public int Stamina { get; private set; } = 100;
    public int MaxAmmo { get; private set; } = 0;
    public int CurrentAmmo { get; private set; } = 0;
    public int MaxHunger { get; } = 100;
    public int Hunger { get; private set; } = 100;
    public int MaxCleanliness { get; } = 100;
    public int Cleanliness { get; private set; } = 100;
    public global::Karma.Audio.VoiceSlot VoiceSlot { get; private set; } = global::Karma.Audio.VoiceSlot.Voice1;

    public void SetVoiceSlot(global::Karma.Audio.VoiceSlot slot)
    {
        VoiceSlot = slot;
    }

    public void SpendStamina(int amount)
    {
        if (amount <= 0) return;
        Stamina = System.Math.Max(0, Stamina - amount);
    }

    public void RegenStamina(int amount)
    {
        if (amount <= 0) return;
        Stamina = System.Math.Min(MaxStamina, Stamina + amount);
    }

    public void SetAmmo(int magazineSize, int currentAmmo)
    {
        MaxAmmo = System.Math.Max(0, magazineSize);
        CurrentAmmo = System.Math.Clamp(currentAmmo, 0, MaxAmmo);
    }

    public bool ConsumeAmmo()
    {
        if (CurrentAmmo <= 0) return false;
        CurrentAmmo--;
        return true;
    }

    public void SpendHunger(int amount)
    {
        if (amount <= 0) return;
        Hunger = System.Math.Max(0, Hunger - amount);
    }

    public void RestoreHunger(int amount)
    {
        if (amount <= 0) return;
        Hunger = System.Math.Min(MaxHunger, Hunger + amount);
    }

    public void SpendCleanliness(int amount)
    {
        if (amount <= 0) return;
        Cleanliness = System.Math.Max(0, Cleanliness - amount);
    }

    public void RestoreCleanliness(int amount)
    {
        if (amount <= 0) return;
        Cleanliness = System.Math.Min(MaxCleanliness, Cleanliness + amount);
    }

    public void ResetCleanliness()
    {
        Cleanliness = MaxCleanliness;
    }

    public int Scrip { get; private set; }
    public TilePosition Position { get; private set; } = TilePosition.Origin;
    public PlayerAppearanceSelection Appearance { get; private set; } = PlayerAppearanceSelection.Default;
    public string LpcBundleId { get; private set; } = string.Empty;
    public string TeamId { get; private set; } = string.Empty;
    public bool HasTeam => !string.IsNullOrWhiteSpace(TeamId);
    public IReadOnlyList<GameItem> Inventory => _inventory;
    public IReadOnlyDictionary<EquipmentSlot, GameItem> Equipment => _equipment;

    public int MaxInventorySlots
    {
        get
        {
            var boost = _equipment.TryGetValue(EquipmentSlot.Backpack, out var pack) ? pack.InventoryBoost : 0;
            return StarterItems.BaseInventorySlots + System.Math.Max(0, boost);
        }
    }

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
        IsDown = false;
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

    public void SetLpcBundleId(string bundleId)
    {
        LpcBundleId = string.IsNullOrWhiteSpace(bundleId) ? string.Empty : bundleId.Trim();
    }

    public void AddItem(GameItem item)
    {
        _inventory.Add(item);
    }

    // Capacity-respecting variant: returns false when the inventory is already
    // at MaxInventorySlots. AddItem stays as the unchecked path so test
    // fixtures and special server flows (trophies, karma break drops) can
    // bypass the cap explicitly.
    public bool TryAddItem(GameItem item)
    {
        if (_inventory.Count >= MaxInventorySlots) return false;
        _inventory.Add(item);
        return true;
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
        return TryTakeItem(itemId, out _);
    }

    public bool TryTakeItem(string itemId, out GameItem item)
    {
        var index = _inventory.FindIndex(item => item.Id == itemId);
        if (index < 0)
        {
            item = null;
            return false;
        }

        item = _inventory[index];
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

    public bool ReplaceEquippedItem(EquipmentSlot slot, GameItem item)
    {
        if (!_equipment.ContainsKey(slot) || item is null || item.Slot != slot)
            return false;

        _equipment[slot] = item;
        return true;
    }

    public bool WearEquippedItem(EquipmentSlot slot, int amount, out GameItem item)
    {
        item = null;
        if (amount <= 0 || !_equipment.TryGetValue(slot, out var equipped) || equipped.MaxDurability <= 0)
            return false;

        item = equipped with { Durability = System.Math.Max(0, equipped.Durability - amount) };
        _equipment[slot] = item;
        return true;
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsAlive || IsDown)
        {
            return false;
        }

        var reducedAmount = System.Math.Max(1, amount - Defense);
        Health = System.Math.Max(0, Health - reducedAmount);
        if (Health > 0)
        {
            return false;
        }

        IsDown = true;
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

    public bool Rescue(int healAmount)
    {
        if (!IsDown)
        {
            return false;
        }

        IsDown = false;
        Health = System.Math.Min(MaxHealth, healAmount);
        return true;
    }
}

public sealed record PlayerAppearanceSelection(
    string BaseLayerId,
    string SkinLayerId,
    string HairLayerId,
    string OutfitLayerId,
    string HeldToolLayerId,
    string PantsLayerId = "",
    string ShirtLayerId = "")
{
    public static PlayerAppearanceSelection Default { get; } = new(
        "base_body_32x64",
        "skin_medium_32x64",
        "hair_short_dark_32x64",
        "outfit_engineer_32x64",
        string.Empty,
        "pants_blue_32x64",
        "shirt_black_32x64");

    public IReadOnlyDictionary<string, string> ToLayerIdsBySlot()
    {
        var layers = new Dictionary<string, string>
        {
            ["base"] = BaseLayerId,
            ["skin"] = SkinLayerId,
            ["hair"] = HairLayerId,
            ["outfit"] = OutfitLayerId
        };

        if (!string.IsNullOrWhiteSpace(PantsLayerId))
        {
            layers["pants"] = PantsLayerId;
        }

        if (!string.IsNullOrWhiteSpace(ShirtLayerId))
        {
            layers["shirt"] = ShirtLayerId;
        }

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
            Normalize(HeldToolLayerId, Default.HeldToolLayerId),
            string.IsNullOrWhiteSpace(PantsLayerId) ? string.Empty : PantsLayerId.Trim(),
            string.IsNullOrWhiteSpace(ShirtLayerId) ? string.Empty : ShirtLayerId.Trim());
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
