using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Maps;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

public partial class GameMapManager
{
    private GameTicker _gameTicker = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    private bool _playedMapMemoryEnabled = false;
    [ViewVariables(VVAccess.ReadOnly)]
    private bool _playedMapMemoryCycleEnabled;
    [ViewVariables(VVAccess.ReadOnly)]
    private int _playedMapQueueDepth = 1;
    [ViewVariables(VVAccess.ReadOnly)]
    private readonly Queue<ProtoId<GameMapPrototype>> _playedMapQueue = new();
    [ViewVariables(VVAccess.ReadOnly)]
    private ProtoId<GameMapPrototype>? _lastPlayedMap;

    private void InitilizeSS220()
    {
        _gameTicker = _entityManager.System<GameTicker>();
        _gameTicker.OnMainStationMapLoaded += OnMainStationMapLoaded;

        _configurationManager.OnValueChanged(CCVars220.GamePlayedMapMemory, value =>
        {
            _playedMapMemoryEnabled = value;
            if (_playedMapMemoryEnabled)
                return;

            _playedMapQueue.Clear();
            _lastPlayedMap = null;
        }, true);
        _configurationManager.OnValueChanged(CCVars220.GamePlayedMapMemoryCycle, value =>
        {
            _playedMapMemoryCycleEnabled = value;
            if (_playedMapMemoryCycleEnabled)
                return;

            if (_playedMapQueue.Count == 0 && _lastPlayedMap != null)
                _playedMapQueue.Enqueue(_lastPlayedMap.Value);

            TrimPlayedMapQueue();
        }, true);
        _configurationManager.OnValueChanged(CCVars220.GamePlayedMapMemoryDepth, value =>
        {
            _playedMapQueueDepth = Math.Max(value, 0);
            TrimPlayedMapQueue();
        }, true);
    }

    private void OnMainStationMapLoaded(GameMapPrototype proto)
    {
        EnqueuePlayedMap(proto);
    }

    private void EnqueuePlayedMap(ProtoId<GameMapPrototype> proto)
    {
        if (!_playedMapMemoryEnabled)
            return;

        _lastPlayedMap = proto;

        if (_playedMapMemoryCycleEnabled && _playedMapQueue.Contains(proto))
        {
            var playedMaps = _playedMapQueue.Where(map => map != proto).ToArray();
            _playedMapQueue.Clear();
            foreach (var playedMap in playedMaps)
            {
                _playedMapQueue.Enqueue(playedMap);
            }
        }

        _playedMapQueue.Enqueue(proto);
        TrimPlayedMapQueue();
    }

    private void TrimPlayedMapQueue()
    {
        if (_playedMapMemoryCycleEnabled)
            return;

        while (_playedMapQueue.Count > _playedMapQueueDepth)
            _playedMapQueue.Dequeue();
    }

    private bool IsPlayedMapEligible(GameMapPrototype map)
    {
        if (!_playedMapMemoryEnabled)
            return true;

        if (_playedMapQueue.Contains(map.ID))
            return false;

        if (!_playedMapMemoryCycleEnabled)
            return true;

        if (_lastPlayedMap?.Id != map.ID)
            return true;

        return !AllVotableMaps().Any(other => other.ID != map.ID && IsMapEligible(other, ignorePlayedMapMemory: true));
    }

    private bool TryResetPlayedMapCycle(out GameMapPrototype[] maps)
    {
        maps = [];

        if (!_playedMapMemoryEnabled)
            return false;

        if (!_playedMapMemoryCycleEnabled)
            return false;

        maps = [.. AllVotableMaps().Where(map => IsMapEligible(map, ignorePlayedMapMemory: true))];
        if (maps.Length == 0)
            return false;

        _playedMapQueue.Clear();
        maps = [.. maps.Where(IsPlayedMapEligible)];
        return true;
    }
}
