using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Timing;

namespace Content.Server.Administration.UI;

/// <summary>
/// Sends round event information exclusively to the administrator who opened the viewer.
/// </summary>
public sealed partial class AdminEventsEui : BaseEui
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IGameTiming _timing = default!;
    private TimeSpan _nextTimerUpdate;

    public override void Opened()
    {
        _admins.OnPermsChanged += OnPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Admin))
            return new AdminEventsEuiState();

        var state = _entities.System<AdminEventsSystem>().GetSnapshot();
        state.CanAddRules = _admins.HasAdminFlag(Player, AdminFlags.Fun);
        state.CanStopRules = _admins.HasAdminFlag(Player, AdminFlags.Fun);
        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (IsShutDown)
            return;

        if (!_admins.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        if (msg is RefreshAdminEventsMessage)
            StateDirty();

        if (msg is RequestAdminSchedulerTimerMessage timer)
        {
            if (_timing.RealTime < _nextTimerUpdate)
                return;

            _nextTimerUpdate = _timing.RealTime + TimeSpan.FromSeconds(0.5);
            SendMessage(new AdminSchedulerTimerMessage(_entities.System<AdminEventsSystem>().GetSchedulerTimer(timer.Scheduler)));
        }

        if (msg is AddAdminGameRuleMessage add)
        {
            var entity = _entities.System<AdminEventsSystem>().TryAddRule(Player, add.Prototype);
            SendMessage(new AddAdminGameRuleResultMessage(entity));
            StateDirty();
        }

        if (msg is StopAdminGameRuleMessage stop)
        {
            var success = _entities.System<AdminEventsSystem>().TryStopRule(Player, stop.Entity);
            SendMessage(new StopAdminGameRuleResultMessage(success));
            StateDirty();
        }
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!_admins.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        StateDirty();
    }
}
