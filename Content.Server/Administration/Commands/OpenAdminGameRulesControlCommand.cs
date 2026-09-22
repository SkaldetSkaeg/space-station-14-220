using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class OpenAdminGameRulesControlCommand : LocalizedCommands
{
    [Dependency] private EuiManager _eui = default!;

    public override string Command => "gamerulesui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _eui.OpenEui(new AdminGameRulesControlEui(), shell.Player);
    }
}
