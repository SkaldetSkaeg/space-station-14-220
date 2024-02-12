using System.Diagnostics.CodeAnalysis;
using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.SS220.Cargo.Events;
using Content.Server.Station.Systems;

namespace Content.Server.SS220.Cargo
{
    public sealed partial class CargoCashOutSystem : SharedCargoSystem
    {
        /// <summary>
        /// How much time to wait (in seconds) before increasing bank accounts balance.
        /// </summary>
        private const int Delay = 10;

        /// <summary>
        /// Keeps track of how much time has elapsed since last balance increase.
        /// </summary>
        private float _timer;

        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly StationSystem _station = default!;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleCashOutMessage>(CashOut);
        }

        private void CashOut(EntityUid uid, CargoOrderConsoleComponent component, ref CargoConsoleCashOutMessage arg)
        {
            if (arg.Amount == 0)
                return;

            var stationUid = _station.GetOwningStation(arg.Used);

            if (!TryComp(stationUid, out StationBankAccountComponent? bank))
                return;

            if (bank == null)
                return;

            if (arg.Amount > bank.Balance)
                return;

            var cargoSystem = _entitySystemManager.GetEntitySystem<CargoSystem>();

            cargoSystem.UpdateBankAccount(stationUid.Value, bank, -arg.Amount);
        }
    }
}
