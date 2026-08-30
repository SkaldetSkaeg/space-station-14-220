// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.SS220.CultYogg.Fungus;
using Content.Shared.SS220.CultYogg.FungusMachine;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.FungusMachine;

public sealed partial class FungusMachineSystem : SharedFungusMachineSystem
{
    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private BotanySystem _botany = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<FungusMachineComponent>(FungusMachineUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
        });
    }

    protected override void OnComponentInit(Entity<FungusMachineComponent> ent, ref ComponentInit args)
    {
        base.OnComponentInit(ent, ref args);

        ent.Comp.Container = _containerSystem.EnsureContainer<Container>(ent.Owner, FungusMachineComponent.ContainerId);
    }

    private void OnBoundUIOpened(Entity<FungusMachineComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateFungusMachineInterfaceState(ent);
    }

    public void UpdateFungusMachineInterfaceState(Entity<FungusMachineComponent> ent)
    {
        TryComp<FungusComponent>(ent, out var fungus);
        var inventory = GetInventory(ent);
        PopulateGrowthInformation(inventory, fungus?.CycleDelay ?? TimeSpan.FromSeconds(15));

        var harvest = GetHarvestTiming(fungus);
        var state = new FungusMachineInterfaceState(
            inventory,
            fungus?.Seed == null ? null : fungus.SelectedCultureId,
            fungus?.Seed != null && fungus.HarvestReady,
            harvest.EndTime,
            harvest.Duration,
            fungus?.Seed?.Yield ?? 0);

        _userInterfaceSystem.SetUiState(ent.Owner, FungusMachineUiKey.Key, state);
    }

    private (TimeSpan EndTime, TimeSpan Duration) GetHarvestTiming(FungusComponent? fungus)
    {
        if (fungus?.Seed == null || fungus.HarvestReady)
            return (TimeSpan.Zero, TimeSpan.Zero);

        var firstHarvest = fungus.LastProduce < fungus.Seed.Maturation;
        var cycleStartAge = firstHarvest ? 1 : fungus.LastProduce;
        var targetAge = firstHarvest
            ? fungus.Seed.Maturation + fungus.Seed.Production
            : fungus.LastProduce + fungus.Seed.Production + 1;
        var totalCycles = Math.Max(1, targetAge - cycleStartAge);
        var currentCycleElapsed = Math.Clamp(
            (_gameTiming.CurTime - fungus.LastCycle).TotalSeconds,
            0,
            fungus.CycleDelay.TotalSeconds);

        return (
            _gameTiming.CurTime + TimeSpan.FromSeconds(Math.Max(0,
                Math.Max(0, targetAge - fungus.Age) * fungus.CycleDelay.TotalSeconds - currentCycleElapsed)),
            TimeSpan.FromSeconds(totalCycles * fungus.CycleDelay.TotalSeconds));
    }

    private void PopulateGrowthInformation(
        List<FungusMachineInventoryEntry> inventory,
        TimeSpan cycleDelay)
    {
        foreach (var entry in inventory)
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>(entry.Id, out var seedPrototype) ||
                !seedPrototype.TryGetComponent<SeedComponent>("Seed", out var seedComponent) ||
                !_botany.TryGetSeed(seedComponent, out var seed))
            {
                continue;
            }

            entry.GrowthStages = seed.GrowthStages;
            entry.Yield = seed.Yield;
            entry.MaturationCycles = (int) MathF.Ceiling(seed.Maturation);
            entry.ProductionCycles = (int) MathF.Ceiling(seed.Production);
            entry.FirstHarvestSeconds = (int) Math.Ceiling(
                Math.Max(1, seed.Maturation + seed.Production - 1) * cycleDelay.TotalSeconds);
            entry.HarvestRepeats = seed.HarvestRepeat != HarvestType.NoRepeat;
            entry.RepeatHarvestSeconds = entry.HarvestRepeats
                ? (int) Math.Ceiling((seed.Production + 1) * cycleDelay.TotalSeconds)
                : 0;
        }
    }
}
