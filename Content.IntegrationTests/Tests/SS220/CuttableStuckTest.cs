// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.SS220;

public sealed class CuttableStuckTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    private EntityUid _wearer;
    private EntityUid _user;
    private EntityUid _item;
    private EntityUid _knife;
    private string _slot;
    private bool _inHand;

    private async Task Prepare(string prototype, bool self, string slot = null)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            _wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            // Natural regeneration must not heal the cut between completion and the damage assertion.
            SEntMan.RemoveComponent<PassiveDamageComponent>(_wearer);
            _user = self ? _wearer : SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            _item = SEntMan.SpawnEntity(prototype, map.GridCoords);
            _knife = SEntMan.SpawnEntity("KitchenKnife", map.GridCoords);
            _inHand = slot == null;
            _slot = slot ?? SEntMan.GetComponent<HandsComponent>(_wearer).SortedHands[0];

            if (_inHand)
                hands.DoPickup(_wearer, _slot, _item);
            else
                Assert.That(SEntMan.System<InventorySystem>().TryEquip(_wearer, _item, _slot, force: true));

            var knifeHand = SEntMan.GetComponent<HandsComponent>(_user).SortedHands[self && _inHand ? 1 : 0];
            Assert.That(hands.TryPickup(_user, _knife, knifeHand));
            hands.SetActiveHand(_user, knifeHand);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck);
        });
    }

    private void ClickItem()
    {
        if (_user != _wearer)
        {
            var message = new StrippingSlotButtonPressed(_slot, _inHand) { Actor = _user };
            SEntMan.EventBus.RaiseLocalEvent(_wearer, message);
            return;
        }

        if (_inHand)
        {
            SEntMan.System<SharedHandsSystem>().TryInteractHandWithActiveHand(_user, _slot);
            return;
        }

        SEntMan.System<SharedInteractionSystem>().InteractUsing(_user, _knife, _item,
            SEntMan.GetComponent<TransformComponent>(_item).Coordinates);
    }

    private Verb[] CutVerbs(EntityUid target)
    {
        return SEntMan.System<SharedVerbSystem>().GetLocalVerbs(target, _user, typeof(EquipmentVerb))
            .Where(v => v.Text == Loc.GetString("cuttable-stuck-verb")).ToArray();
    }

    private void SelectCutVerb(EntityUid target)
    {
        var verb = CutVerbs(target).Single();
        Assert.That(verb.Category, Is.Null, "Cutting must be a direct verb, without an item-selection submenu.");
        SEntMan.System<SharedVerbSystem>().ExecuteVerb(verb, _user, target);
    }

    private Content.Shared.DoAfter.DoAfter RunningCut()
    {
        var doAfters = SEntMan.GetComponent<DoAfterComponent>(_user).DoAfters;
        return doAfters.Values.Single(d => d.Args.Event is CutStuckDoAfterEvent && !d.Cancelled && !d.Completed);
    }

    private int SlashDamage(EntityUid uid)
    {
        var damage = SEntMan.System<DamageableSystem>().GetAllDamage(uid);
        return damage.DamageDict.TryGetValue("Slash", out var slash) ? slash.Int() : 0;
    }

    [TestCase("ClawCultYogg", true, null, 20)]
    [TestCase("ClawCultYogg", false, null, 20)]
    [TestCase("SedativeStingCultYogg", true, null, 5)]
    [TestCase("ClothingBackpackCultYogg", true, "back", 20)]
    [TestCase("ClothingBackpackCultYogg", false, "back", 20)]
    public async Task VerbCutsOnlyAfterDoAfter(string prototype, bool self, string slot, int damage)
    {
        await Prepare(prototype, self, slot);
        await Server.WaitAssertion(() =>
        {
            Assert.That(CutVerbs(_wearer), Is.Empty);
            Assert.That(CutVerbs(_item), Has.Length.EqualTo(1));
            SelectCutVerb(_item);
            Assert.That(RunningCut(), Is.Not.Null);
            Assert.That(SlashDamage(_wearer), Is.Zero);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck);
        });

        await Pair.RunSeconds(5.5f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck, Is.False);
            Assert.That(SEntMan.System<SharedContainerSystem>().IsEntityInContainer(_item), Is.False);
            Assert.That(SlashDamage(_wearer), Is.EqualTo(damage));
        });
    }

    [TestCase("drop")]
    [TestCase("switch")]
    [TestCase("move-user")]
    [TestCase("move-wearer")]
    [TestCase("remove-item")]
    public async Task InterruptedCutDoesNotDamageWearer(string interruption)
    {
        await Prepare("ClawCultYogg", false);
        Content.Shared.DoAfter.DoAfter cut = null;
        await Server.WaitAssertion(() =>
        {
            SelectCutVerb(_item);
            cut = RunningCut();
        });
        await Pair.RunSeconds(0.5f);
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var transform = SEntMan.System<SharedTransformSystem>();
            var containers = SEntMan.System<SharedContainerSystem>();
            switch (interruption)
            {
                case "drop":
                    Assert.That(hands.TryDrop(_user, _knife));
                    break;
                case "switch":
                    hands.SetActiveHand(_user, SEntMan.GetComponent<HandsComponent>(_user).SortedHands[1]);
                    break;
                case "move-user":
                    transform.SetCoordinates(_user, SEntMan.GetComponent<TransformComponent>(_user).Coordinates.Offset(new Vector2(2, 0)));
                    break;
                case "move-wearer":
                    transform.SetCoordinates(_wearer, SEntMan.GetComponent<TransformComponent>(_wearer).Coordinates.Offset(new Vector2(2, 0)));
                    break;
                case "remove-item":
                    Assert.That(containers.TryGetContainingContainer((_item, null, null), out var container));
                    Assert.That(containers.Remove(_item, container, force: true));
                    break;
            }
        });
        await Pair.RunSeconds(5.5f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(cut.Cancelled);
            Assert.That(SlashDamage(_wearer), Is.Zero);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck,
                Is.EqualTo(interruption != "remove-item"));
        });
    }

    [Test]
    public async Task ArmorCannotBeCut()
    {
        await Prepare("ClothingOuterHardsuitCultYogg", false, "outerClothing");
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<CuttableStuckComponent>(_item), Is.False);
            Assert.That(CutVerbs(_wearer), Is.Empty);
            Assert.That(CutVerbs(_item), Is.Empty);
            var doAfters = SEntMan.GetComponent<DoAfterComponent>(_user).DoAfters;
            Assert.That(doAfters.Values.Any(d => d.Args.Event is CutStuckDoAfterEvent), Is.False);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck);
            Assert.That(SlashDamage(_wearer), Is.Zero);
        });
    }

    [Test]
    public async Task CuttingRequiresHeldKnifeAndAttachedItem()
    {
        await Prepare("ClawCultYogg", false);
        await Server.WaitAssertion(() =>
        {
            var cut = SEntMan.System<CuttableStuckSystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.TryDrop(_user, _knife));
            Assert.That(CutVerbs(_item), Is.Empty);
            Assert.That(cut.TryStartCut(_user, _item, _knife), Is.False);

            var wrench = SEntMan.SpawnEntity("Wrench", SEntMan.GetComponent<TransformComponent>(_user).Coordinates);
            Assert.That(hands.TryPickup(_user, wrench));
            Assert.That(CutVerbs(_item), Is.Empty);
            Assert.That(cut.TryStartCut(_user, _item, wrench), Is.False);
            Assert.That(hands.TryDrop(_user, wrench));
            Assert.That(hands.TryPickup(_user, _knife));

            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainingContainer((_item, null, null), out var container));
            Assert.That(containers.Remove(_item, container, force: true));
            Assert.That(CutVerbs(_item), Is.Empty);
            Assert.That(cut.TryStartCut(_user, _item, _knife), Is.False);
            Assert.That(SlashDamage(_wearer), Is.Zero);
        });
    }

    [TestCase(true, null)]
    [TestCase(false, null)]
    [TestCase(true, "back")]
    [TestCase(false, "back")]
    public async Task PlainClickDoesNotStartCutting(bool self, string slot)
    {
        await Prepare(slot == null ? "ClawCultYogg" : "ClothingBackpackCultYogg", self, slot);
        await Server.WaitAssertion(() =>
        {
            ClickItem();
            var doAfters = SEntMan.GetComponent<DoAfterComponent>(_user).DoAfters;
            Assert.That(doAfters.Values.Any(d => d.Args.Event is CutStuckDoAfterEvent), Is.False);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck);
        });
    }

    [Test]
    public async Task ItemVerbOnlyCutsSelectedItem()
    {
        await Prepare("ClawCultYogg", false);
        EntityUid other = default;
        await Server.WaitAssertion(() =>
        {
            other = SEntMan.SpawnEntity("ClawCultYogg", SEntMan.GetComponent<TransformComponent>(_wearer).Coordinates);
            var hand = SEntMan.GetComponent<HandsComponent>(_wearer).SortedHands[1];
            SEntMan.System<SharedHandsSystem>().DoPickup(_wearer, hand, other);
            Assert.That(CutVerbs(_wearer), Is.Empty);
            Assert.That(CutVerbs(other), Has.Length.EqualTo(1));
            SelectCutVerb(_item);
        });
        await Pair.RunSeconds(5.5f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(_item).IsStuck, Is.False);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(other).IsStuck);
            Assert.That(SlashDamage(_wearer), Is.EqualTo(20));
        });
    }
}
