using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.GameRules;

[UsedImplicitly]
public sealed class AdminGameRulesControlEui : BaseEui
{
    private readonly AdminGameRulesControlWindow _window = new();

    public AdminGameRulesControlEui()
    {
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.RefreshButton.OnPressed += _ => SendMessage(new RefreshAdminGameRulesControlMessage());
        _window.AddRuleRequested += id => SendMessage(new AddAdminGameRuleMessage(id));
        _window.StopRuleRequested += entity => SendMessage(new StopAdminGameRuleMessage(entity));
        _window.TimerRefreshRequested += entity => SendMessage(new RequestAdminSchedulerTimerMessage(entity));
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is AdminGameRulesControlEuiState events)
            _window.UpdateState(events);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is AddAdminGameRuleResultMessage result)
            _window.HandleAddResult(result.Entity);

        if (msg is StopAdminGameRuleResultMessage stop)
            _window.HandleStopResult(stop.Success);

        if (msg is AdminSchedulerTimerMessage timer)
            _window.UpdateTimer(timer.Timer);
    }
}
