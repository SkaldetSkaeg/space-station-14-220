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
        string? selectedCultureId = null;
        var harvestReady = false;
        var harvestProgress = 0f;
        var secondsUntilHarvest = 0f;
        var yield = 0;

        if (TryComp<FungusComponent>(ent, out var fungus) && fungus.Seed != null)
        {
            selectedCultureId = fungus.SelectedCultureId;
            harvestReady = fungus.HarvestReady;
            yield = fungus.Seed.Yield;

            if (harvestReady)
            {
                harvestProgress = 1f;
            }
            else
            {
                var firstHarvest = fungus.LastProduce < fungus.Seed.Maturation;
                var cycleStartAge = firstHarvest ? 1 : fungus.LastProduce;
                var targetAge = firstHarvest
                    ? fungus.Seed.Maturation + fungus.Seed.Production
                    : fungus.LastProduce + fungus.Seed.Production + 1;
                var totalCycles = Math.Max(1, targetAge - cycleStartAge);
                var completedCycles = Math.Clamp(fungus.Age - cycleStartAge, 0, totalCycles);
                harvestProgress = (float) completedCycles / totalCycles;

                var remainingCycles = Math.Max(0, targetAge - fungus.Age);
                var currentCycleElapsed = Math.Clamp(
                    (_gameTiming.CurTime - fungus.LastCycle).TotalSeconds,
                    0,
                    fungus.CycleDelay.TotalSeconds);
                secondsUntilHarvest = (float) Math.Max(
                    0,
                    remainingCycles * fungus.CycleDelay.TotalSeconds - currentCycleElapsed);
            }
        }

        var inventory = GetInventory(ent);
        PopulateGrowthInformation(inventory, fungus?.CycleDelay ?? TimeSpan.FromSeconds(15));

        var state = new FungusMachineInterfaceState(
            inventory,
            selectedCultureId,
            harvestReady,
            harvestProgress,
            secondsUntilHarvest,
            yield);

        _userInterfaceSystem.SetUiState(ent.Owner, FungusMachineUiKey.Key, state);
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
