using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.CultYogg.MiGo;

/// <summary>
///     This class is needed to simplify the transfer of the healing effect between methods.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class MiGoHealSpecifier
{
    /// <summary>
    ///     Dictionary of wich damage will be healed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HealAmount { get; set; } = new();

    /// <summary>
    /// Frequency between healing events
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? HealingFreq;

    /// <summary>
    /// Modify bloodloss amount
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float? BloodlossModifier;

    /// <summary>
    /// Restore missing blood.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float? ModifyBloodLevel;

    #region constructors
    /// <summary>
    ///     Constructor that just results in an empty dictionary.
    /// </summary>
    public MiGoHealSpecifier() { }

    /// <summary>
    ///     Constructor that takes a single damage type prototype and a damage value.
    /// </summary>
    public MiGoHealSpecifier(DamageSpecifier healAmount, TimeSpan healingFreq)
    {
        HealAmount = healAmount;
        HealingFreq = healingFreq;
    }

    public MiGoHealSpecifier(DamageSpecifier healAmount, TimeSpan healingFreq, float bloodlossModifier, float modifyBloodLevel)
    {
        HealAmount = healAmount;
        HealingFreq = healingFreq;
        BloodlossModifier = bloodlossModifier;
        ModifyBloodLevel = modifyBloodLevel;
    }
    #endregion constructors

    /*
    public bool Equals(DamageSpecifier? other)
    {
        if
    }
    */
}
