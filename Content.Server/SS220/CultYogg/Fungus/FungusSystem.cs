// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.FungusMachine;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Fungus;

public sealed partial class FungusSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private Content.Server.SS220.CultYogg.FungusMachine.FungusMachineSystem _fungusMachine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FungusComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<FungusComponent, InteractHandEvent>(OnInteractHand);

        Subs.BuiEvents<FungusMachineComponent>(FungusMachineUiKey.Key,
            subs =>
        {
            subs.Event<FungusSelectedId>(OnUIButton);
            subs.Event<FungusHarvestRequested>(OnHarvestRequested);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FungusComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;

            plantHolder.NextUpdate = _gameTiming.CurTime + plantHolder.UpdateDelay;
            var wasHarvestReady = plantHolder.HarvestReady;
            UpdateFungus(uid, plantHolder);

            if (wasHarvestReady != plantHolder.HarvestReady &&
                TryComp<FungusMachineComponent>(uid, out var machine))
                _fungusMachine.UpdateFungusMachineInterfaceState((uid, machine));
        }
    }

    /// <summary>
    /// Returns the current plant growth stage within the range defined by the seed prototype.
    /// </summary>
    private int GetCurrentGrowthStage(Entity<FungusComponent> entity)
    {
        var (_, component) = entity;

        if (component.Seed == null)
            return 0;

        var result = Math.Max(1, (int) (component.Age * component.Seed.GrowthStages / component.Seed.Maturation));
        return Math.Min(result, component.Seed.GrowthStages);
    }

    private void OnExamine(Entity<FungusComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FungusComponent)))
        {
            if (entity.Comp.Seed == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
                return;
            }

            args.PushMarkup(Loc.GetString(
                "cult-yogg-fungus-examine-growing",
                ("seedName", Loc.GetString(entity.Comp.Seed.DisplayName))));

            args.PushMarkup(entity.Comp.HarvestReady
                ? Loc.GetString("cult-yogg-fungus-examine-ready")
                : Loc.GetString("cult-yogg-fungus-examine-not-ready"));
        }
    }

    private void OnInteractHand(Entity<FungusComponent> entity, ref InteractHandEvent args)
    {
        if (DoHarvest(entity, args.User, entity.Comp) && TryComp<FungusMachineComponent>(entity, out var machine))
            _fungusMachine.UpdateFungusMachineInterfaceState((entity, machine));
    }

    public void UpdateFungus(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var curTime = _gameTiming.CurTime;

        if (curTime < (component.LastCycle + component.CycleDelay)
            || component.Seed == null)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
            return;
        }

        component.LastCycle = curTime;
        component.Age += 1;

        component.UpdateSpriteAfterUpdate = true;
        if (component.Seed.ProductPrototypes.Count > 0)
        {
            if (component.Age > component.Seed.Production)
            {
                if (component.Age - component.LastProduce > component.Seed.Production && !component.HarvestReady)
                {
                    component.HarvestReady = true;
                    component.LastProduce = component.Age;
                }
            }
            else
            {
                if (component.HarvestReady)
                {
                    component.HarvestReady = false;
                    component.LastProduce = component.Age;
                }
            }
        }

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    public bool DoHarvest(EntityUid uid, EntityUid user, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Seed == null || Deleted(user))
            return false;


        if (!component.HarvestReady)
            return false;

        if (TryComp<HandsComponent>(user, out var hands))
        {
            if (!_botany.CanHarvest(component.Seed, _hands.GetActiveItem((user, hands))))
            {
                return false;
            }
        }
        else if (!_botany.CanHarvest(component.Seed))
        {
            return false;
        }

        _botany.Harvest(component.Seed, user);
        AfterHarvest(uid, component);
        return true;

    }

    private void AfterHarvest(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.HarvestReady = false;
        component.LastProduce = component.Age;

        if (component.Seed?.HarvestRepeat == HarvestType.NoRepeat)
            RemovePlant(uid, component);
        UpdateSprite(uid, component);
    }

    public void RemovePlant(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Seed = null;
        component.Age = 0;
        component.LastProduce = 0;
        component.HarvestReady = false;
        component.SelectedCultureId = null;

        UpdateSprite(uid, component);
    }

    private FungusMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, FungusMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.Inventory.GetValueOrDefault(entryId);
    }

    private void OnUIButton(Entity<FungusMachineComponent> entity, ref FungusSelectedId args)
    {
        var (uid, component) = entity;

        if (args.Actor is not { Valid: true } ent || Deleted(ent))
            return;

        if (_whitelist.IsWhitelistFail(component.UsersWhitelist, ent))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-fungus-denied-to-use"), uid, ent);
            return;
        }

        var entry = GetEntry(uid, args.Id, component);

        if (entry == null)
        {
            _popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid);
            return;
        }

        if (string.IsNullOrEmpty(entry.Id))
            return;

        var proto = _prototype.Index(entry.Id);

        if (!TryComp(uid, out FungusComponent? fungusComponent))
            return;

        if (fungusComponent.SelectedCultureId == entry.Id)
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-fungus-already-growing"), uid, ent);
            return;
        }

        if (!proto.TryGetComponent<SeedComponent>("Seed", out var seedComponent))
            return;

        if (!_botany.TryGetSeed(seedComponent, out var seed))
            return;

        _popup.PopupEntity(Loc.GetString("plant-holder-component-plant-success-message",
                ("seedName",  Loc.GetString(seed.Name)),
                ("seedNoun", Loc.GetString(seed.Noun))),
            uid,
            ent,
            PopupType.Medium);

        fungusComponent.Seed = seed;
        fungusComponent.Age = 1;
        fungusComponent.LastProduce = 1;
        fungusComponent.HarvestReady = false;
        fungusComponent.SelectedCultureId = entry.Id;
        fungusComponent.LastCycle = _gameTiming.CurTime;
        UpdateSprite(uid, fungusComponent);
        _fungusMachine.UpdateFungusMachineInterfaceState(entity);
    }

    private void OnHarvestRequested(Entity<FungusMachineComponent> entity, ref FungusHarvestRequested args)
    {
        var (uid, component) = entity;

        if (args.Actor is not { Valid: true } actor || Deleted(actor))
            return;

        if (_whitelist.IsWhitelistFail(component.UsersWhitelist, actor))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-fungus-denied-to-use"), uid, actor);
            return;
        }

        if (!TryComp<FungusComponent>(uid, out var fungus))
            return;

        if (!fungus.HarvestReady)
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-fungus-harvest-not-ready"), uid, actor);
            return;
        }

        DoHarvest(uid, actor, fungus);
        _fungusMachine.UpdateFungusMachineInterfaceState(entity);
    }

    public void UpdateSprite(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (component.Seed == null)
        {
            _appearance.SetData(uid, FungusMachineVisuals.State, FungusMachineVisualState.Empty, app);
            _appearance.SetData(uid, PlantHolderVisuals.PlantState, "", app);
            _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
            return;
        }

        _appearance.SetData(uid, FungusMachineVisuals.State,
            component.HarvestReady
                ? FungusMachineVisualState.Grown
                : FungusMachineVisualState.Growing,
            app);

        _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);

        if (component.HarvestReady)
        {
            _appearance.SetData(uid, PlantHolderVisuals.PlantState, "harvest", app);
            return;
        }

        if (component.Age < component.Seed.Maturation)
        {
            _appearance.SetData(uid,
                PlantHolderVisuals.PlantState,
                $"stage-{GetCurrentGrowthStage((uid, component))}",
                app);
            component.LastProduce = component.Age;
            return;
        }

        _appearance.SetData(uid,
            PlantHolderVisuals.PlantState,
            $"stage-{component.Seed.GrowthStages}",
            app);
    }
}
