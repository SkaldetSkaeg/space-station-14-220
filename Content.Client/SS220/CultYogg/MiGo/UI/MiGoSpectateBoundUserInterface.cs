// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.UserInterface;


namespace Content.Client.SS220.CultYogg.MiGo.UI;

public sealed class MiGoSpectateBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MiGoSpectateWindow? _window;

    [ViewVariables]
    private EntityUid? _currentCamera;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<MiGoSpectateWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MiGoSpectateBuiState cState)
            return;

        //_window.MapViewer.SetSelectedAddress(cast.ActiveAddress);

        //var active = EntMan.GetEntity(cast.ActiveCamera);

        //if (active == null)
        //{
        //    _window.UpdateState(null, cast.Subnets, cast.ActiveAddress, cast.Cameras);

        //    if (_currentCamera != null)
        //    {
        //        _surveillanceCameraMonitorSystem.RemoveTimer(Owner);
        //        _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
        //        _currentCamera = null;
        //    }
        //}
        //else
        //{
        //    if (_currentCamera == null)
        //    {
        //        _eyeLerpingSystem.AddEye(active.Value);
        //        _currentCamera = active;
        //    }
        //    else if (_currentCamera != active)
        //    {
        //        _eyeLerpingSystem.RemoveEye(_currentCamera.Value);
        //        _eyeLerpingSystem.AddEye(active.Value);
        //        _currentCamera = active;
        //    }

        //    if (EntMan.TryGetComponent<EyeComponent>(active, out var eye))
        //    {
        //        _window.UpdateState(eye.Eye, cast.Subnets, cast.ActiveAddress, cast.Cameras);
        //    }
        //}
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window == null)
            return;

        _window.OnClose -= Close;
    }
}
