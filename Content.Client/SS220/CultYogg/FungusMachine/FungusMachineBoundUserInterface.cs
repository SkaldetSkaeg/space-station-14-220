// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.FungusMachine;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.CultYogg.FungusMachine;

public sealed class FungusMachineBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private FungusMachineMenu? _menu;

    [ViewVariables]
    private FungusMachineInterfaceState? _cachedState;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<FungusMachineMenu>();
        _menu.OpenCenteredLeft();
        _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _menu.OnGrowRequested += OnGrowRequested;
        _menu.OnHarvestRequested += OnHarvestRequested;

        if (_cachedState != null)
            _menu.UpdateState(_cachedState);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not FungusMachineInterfaceState newState)
            return;

        _cachedState = newState;
        _menu?.UpdateState(newState);
    }

    private void OnGrowRequested(string cultureId)
    {
        SendMessage(new FungusSelectedId(cultureId));
    }

    private void OnHarvestRequested()
    {
        SendMessage(new FungusHarvestRequested());
    }
}
