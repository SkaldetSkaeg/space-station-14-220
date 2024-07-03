// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing;
using Robust.Client.Player;

namespace Content.Client.SS220.CriminalRecords.UI;

[GenerateTypedNameReferences]
public sealed partial class CharacterVisualisation : BoxContainer
{
    private readonly IEntityManager _entMan;
    private readonly IPrototypeManager _prototype;
    private readonly IPlayerManager _player;
    private readonly ClientInventorySystem _inventorySystem;
    private EntityUid _previewDummy;
    private readonly SpriteView _face;
    private readonly SpriteView _side;

    public CharacterVisualisation()
    {
        RobustXamlLoader.Load(this);

        _entMan = IoCManager.Resolve<IEntityManager>();
        _prototype = IoCManager.Resolve<IPrototypeManager>();
        _player = IoCManager.Resolve<IPlayerManager>();
        _inventorySystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ClientInventorySystem>();

        _face = new SpriteView() { Scale = new Vector2(5, 5) };
        _side = new SpriteView() { Scale = new Vector2(5, 5), OverrideDirection = Direction.East };

        AddChild(_face);
        AddChild(_side);
    }

    public void ResetCharacterSpriteView()
    {
        _face.SetEntity(null);
        _side.SetEntity(null);
        _entMan.DeleteEntity(_previewDummy);
    }

    public void SetupCharacterSpriteView(HumanoidCharacterProfile profile, string jobPrototype)
    {
        HumanoidAppearanceSystem appearanceSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<HumanoidAppearanceSystem>();

        _entMan.DeleteEntity(_previewDummy);

        _previewDummy = _entMan.SpawnEntity(_prototype.Index<SpeciesPrototype>(profile.Species).DollPrototype, MapCoordinates.Nullspace);
        appearanceSystem.LoadProfile(_previewDummy, profile);
        var realjobprototype = _prototype.Index<JobPrototype>(jobPrototype ?? SharedGameTicker.FallbackOverflowJob);
        GiveDummyJobClothes(_previewDummy, profile, realjobprototype);

        _face.SetEntity(_previewDummy);
        _side.SetEntity(_previewDummy);
    }

    private void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile, JobPrototype job)
    {
        if (!_inventorySystem.TryGetSlots(dummy, out var slots))
            return;

        if (job.StartingGear == null)
            return;

        var gear = _prototype.Index<StartingGearPrototype>(job.StartingGear);

        foreach (var slot in slots)
        {
            var itemType = gear.GetGear(slot.Name);

            if (_inventorySystem.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                _entMan.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = _entMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                _inventorySystem.TryEquip(dummy, item, slot.Name, silent: true, force: true);
            }
        }

        if (profile.Loadouts.TryGetValue(LoadoutSystem.GetJobPrototype(job.ID), out var jobLoadout))
            GiveDummyLoadout(dummy, jobLoadout);
        else
        {
            jobLoadout = new RoleLoadout(LoadoutSystem.GetJobPrototype(job.ID));
            jobLoadout.SetDefault(profile, _player.LocalSession, _prototype);
            GiveDummyLoadout(dummy, jobLoadout);
        }
    }

    private void GiveDummyLoadout(EntityUid dummy, RoleLoadout jobLoadout)
    {
        if (!_inventorySystem.TryGetSlots(dummy, out var slots))
            return;

        foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in loadouts)
            {
                if (!_prototype.TryIndex(loadout.Prototype, out var loadoutProto))
                    continue;

                var loadoutGear = _prototype.Index(loadoutProto.Equipment);

                foreach (var slot in slots)
                {
                    var itemType = loadoutGear.GetGear(slot.Name);

                    if (itemType != string.Empty)
                    {
                        var item = _entMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                        _inventorySystem.TryEquip(dummy, item, slot.Name, silent: true, force: true);
                    }
                }
            }
        }
    }
}