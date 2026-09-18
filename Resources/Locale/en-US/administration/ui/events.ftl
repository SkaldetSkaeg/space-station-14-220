admin-events-title = Events
admin-events-refresh = Refresh
admin-events-add = Add
admin-events-add-title = Add a GameRule
admin-events-add-hint = Select a rule to add to the round. During an ongoing round, it will start immediately or after its configured delay.
admin-events-add-search = Search by ID or name
admin-events-add-cancel = Cancel
admin-events-add-selected = Selected: { $id }
admin-events-add-pending = Adding rule…
admin-events-add-failed = Could not add the rule. It may no longer exist or you may lack permission.
admin-events-add-empty = No matching GameRules.
admin-events-rules = Current GameRules
admin-events-tables = Event tables
admin-events-enabled = Automatic events: enabled
admin-events-disabled = Automatic events: disabled
admin-events-active = Active
admin-events-delayed = Start delayed
admin-events-pending = Pending
admin-events-no-rules = No unfinished GameRules in this round.
admin-events-select-rule = Select a GameRule on the left to view its event tables.
admin-events-rule-no-table = This GameRule has no event table.
admin-events-empty-table = No station events in this table.
admin-events-inline-table = Inline table
admin-events-tables-hint = Triggered events have already started in this round; waiting events are eligible for selection. Red events cannot currently be selected, including for a repeat run. Click i for details. Use Refresh to update.
admin-events-category-triggered = Triggered this round ({ $count })
admin-events-category-waiting = Waiting ({ $count })
admin-events-category-unavailable = Unavailable now ({ $count })
admin-events-info-title = Event information
admin-events-info-button = Information about { $id }
admin-events-info-no-description = This event has no description in its prototype.
admin-events-info-weight = { $weight } (not a percentage)
admin-events-info-parameter = Parameter
admin-events-info-value = Value
admin-events-info-weight-label = Event weight
admin-events-info-occurrences-label = Starts this round (all schedulers)
admin-events-info-players-label = Minimum players
admin-events-info-earliest-label = Earliest start (into the round)
admin-events-info-repeat-label = Repeat interval
admin-events-info-limit-label = Limit per round
admin-events-info-round-end-label = Can start after the evacuation shuttle can no longer be recalled
admin-events-info-minutes = { $minutes } min
admin-events-info-snapshot = Status at the last refresh. Availability does not guarantee a run: the scheduler timer and random selection still apply.
admin-events-available = Available for selection under the current conditions.
admin-events-unavailable-disabled = Unavailable: automatic events are disabled.
admin-events-unavailable-scheduler = Unavailable: this scheduler is not active.
admin-events-unavailable-conditions = Unavailable: event or table conditions are not met, the event is excluded, or its weight is zero.
admin-events-entry-unlimited = unlimited
admin-events-entry-limit = { $count }
admin-events-entry-round-end-allowed = Yes
admin-events-entry-round-end-blocked = No
cmd-eventsui-desc = Opens the current round's GameRules and event tables.
cmd-eventsui-help = Usage: eventsui

admin-events-add-subcategory = Event category
admin-events-add-all-events = All GameRules
admin-events-group-gamerules = GameRules
admin-events-group =
    { $category ->
        [Events] { admin-events-group-events }
        [Schedulers] { admin-events-group-schedulers }
        [Roles] { admin-events-group-roles }
        [RoundComposition] { admin-events-group-round-composition }
        [StationVariations] { admin-events-group-station-variations }
        [RoundControl] { admin-events-group-round-control }
        [SpecialModes] { admin-events-group-special-modes }
       *[other] { admin-events-group-other }
    }
admin-events-subgroup =
    { $category ->
        [Antagonists] { admin-events-subgroup-antagonists }
        [DerelictCyborgs] { admin-events-subgroup-cyborgs }
        [Creatures] { admin-events-subgroup-creatures }
        [CargoGifts] { admin-events-subgroup-cargo-gifts }
        [Meteors] { admin-events-subgroup-meteors }
        [Shuttles] { admin-events-subgroup-shuttles }
       *[other] { admin-events-subgroup-effects }
    }
admin-events-group-events = Events
admin-events-group-schedulers = Schedulers
admin-events-group-roles = Roles and antagonists
admin-events-group-round-composition = Round composition
admin-events-group-station-variations = Station variations
admin-events-group-round-control = Round control
admin-events-group-special-modes = Special modes
admin-events-group-other = Other rules
admin-events-subgroup-effects = Incidents and effects
admin-events-subgroup-antagonists = Antagonists and dangerous roles
admin-events-subgroup-cyborgs = Derelict cyborgs
admin-events-subgroup-creatures = Vent creatures
admin-events-subgroup-cargo-gifts = Cargo gifts
admin-events-subgroup-meteors = Meteors and space hazards
admin-events-subgroup-shuttles = Unknown shuttles

admin-events-stop = Stop selected
admin-events-stop-tooltip = Ends the selected GameRule or cancels its pending start. Existing effects and spawned entities may remain, depending on the rule.
admin-events-stop-waiting = Stopping the selected GameRule...
admin-events-stop-failed = Could not stop the GameRule. It may have already ended, or you may no longer have permission.
admin-events-current-schedulers = Current schedulers
admin-events-current-events = Current GameRules
admin-events-history = GameRule history
admin-events-history-hint = GameRules added this round, newest first, including cancelled starts. Times are measured from round start. Ended means the GameRule finished; its effects may remain.
admin-events-history-empty = No GameRules have been added this round.
admin-events-history-added = Added
admin-events-history-started = Started
admin-events-history-ended = Ended
admin-events-history-source = Source
admin-events-history-reason = End reason
admin-events-history-status =
    { $status ->
        [Pending] { admin-events-pending }
        [Delayed] { admin-events-delayed }
        [Active] { admin-events-active }
        [Ended] Ended
        [Stopped] Stopped
        [Cancelled] Cancelled
       *[other] { admin-events-history-unknown }
    }
admin-events-history-unknown = Unknown
admin-events-history-empty-value = —
admin-events-history-source-admin = Administrator: { $name }
admin-events-history-source-console = Server console
admin-events-history-source-scheduler = { $name } ({ $entity }) / { $table }
admin-events-history-end-reason =
    { $reason ->
        [DurationElapsed] Configured duration elapsed
        [Administrator] Stopped by an administrator
        [ServerConsole] Stopped through the server console
        [RulesCleared] GameRules cleared
        [EntityDeleted] GameRule entity deleted
       *[other] { admin-events-history-unknown }
    }
admin-events-history-reason-by = { $reason } ({ $name })
admin-events-next-attempt = Next event attempt: { $time }
admin-events-next-attempt-paused = Next event attempt: paused ({ $time } remaining)
admin-events-next-attempt-inactive = Next event attempt: scheduler is not active
admin-events-next-attempt-tooltip = Updates every second. When the timer expires, the scheduler attempts to select an event. A successful start is not guaranteed.
admin-events-likelihood-title = Likely events — relative weights
admin-events-likelihood-weight = Weight: { $weight }
admin-events-likelihood-more = { $count } more eligible events in the list below.
admin-events-likelihood-empty = No eligible events at the last refresh.
admin-events-likelihood-hint = Highest eligible weights at the last refresh. Table randomness may change the candidates. These bars are not percentages; use Refresh to update.
admin-events-delay-none = Configured start delay: none.
admin-events-delay-fixed = Configured start delay: { $seconds } s.
admin-events-delay-range = Configured start delay: { $min }–{ $max } s (random).
