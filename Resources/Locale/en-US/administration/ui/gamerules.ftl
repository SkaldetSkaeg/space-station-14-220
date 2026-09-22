admin-gamerules-title = GameRules
admin-gamerules-refresh = Refresh
admin-gamerules-add = Add
admin-gamerules-add-title = Add a GameRule
admin-gamerules-add-hint = Select a rule to add to the round. During an ongoing round, it will start immediately or after its configured delay.
admin-gamerules-add-search = Search by ID or name
admin-gamerules-add-cancel = Cancel
admin-gamerules-add-selected = Selected: { $id }
admin-gamerules-add-pending = Adding rule…
admin-gamerules-add-failed = Could not add the rule. It may no longer exist or you may lack permission.
admin-gamerules-add-empty = No matching GameRules.
admin-gamerules-rules = Current GameRules
admin-gamerules-tables = Event tables
admin-gamerules-enabled = Automatic events: enabled
admin-gamerules-disabled = Automatic events: disabled
admin-gamerules-active = Active
admin-gamerules-delayed = Start delayed
admin-gamerules-pending = Pending
admin-gamerules-no-rules = No unfinished GameRules in this round.
admin-gamerules-select-rule = Select a GameRule on the left to view its event tables.
admin-gamerules-rule-no-table = This GameRule has no event table.
admin-gamerules-empty-table = No station events in this table.
admin-gamerules-inline-table = Inline table
admin-gamerules-tables-hint = Triggered events have already started in this round; waiting events are eligible for selection. Red events cannot currently be selected, including for a repeat run. Click i for details. Use Refresh to update.
admin-gamerules-category-triggered = Triggered this round ({ $count })
admin-gamerules-category-waiting = Waiting ({ $count })
admin-gamerules-category-unavailable = Unavailable now ({ $count })
admin-gamerules-info-title = Event information
admin-gamerules-info-button = Information about { $id }
admin-gamerules-info-no-description = This event has no description in its prototype.
admin-gamerules-info-weight = { $weight } (not a percentage)
admin-gamerules-info-parameter = Parameter
admin-gamerules-info-value = Value
admin-gamerules-info-weight-label = Event weight
admin-gamerules-info-occurrences-label = Starts this round (all schedulers)
admin-gamerules-info-players-label = Minimum players
admin-gamerules-info-earliest-label = Earliest start (into the round)
admin-gamerules-info-repeat-label = Repeat interval
admin-gamerules-info-limit-label = Limit per round
admin-gamerules-info-round-end-label = Can start after the evacuation shuttle can no longer be recalled
admin-gamerules-info-minutes = { $minutes } min
admin-gamerules-info-snapshot = Status at the last refresh. Availability does not guarantee a run: the scheduler timer and random selection still apply.
admin-gamerules-available = Available for selection under the current conditions.
admin-gamerules-availability =
    { $availability ->
        [Available] { admin-gamerules-available }
        [EventsDisabled] { admin-gamerules-unavailable-disabled }
        [SchedulerInactive] { admin-gamerules-unavailable-scheduler }
       *[other] { admin-gamerules-unavailable-conditions }
    }
admin-gamerules-unavailable-disabled = Unavailable: automatic events are disabled.
admin-gamerules-unavailable-scheduler = Unavailable: this scheduler is not active.
admin-gamerules-unavailable-conditions = Unavailable: event or table conditions are not met, the event is excluded, or its weight is zero.
admin-gamerules-entry-unlimited = unlimited
admin-gamerules-entry-limit = { $count }
admin-gamerules-entry-round-end-allowed = Yes
admin-gamerules-entry-round-end-blocked = No
cmd-gamerulesui-desc = Opens the current round's GameRules and event tables.
cmd-gamerulesui-help = Usage: gamerulesui

admin-gamerules-add-category = Category
admin-gamerules-add-all-categories = All categories
admin-gamerules-group-gamerules = GameRules
admin-gamerules-group-schedulers = Schedulers

admin-gamerules-stop = Stop selected
admin-gamerules-stop-tooltip = Ends the selected GameRule or cancels its pending start. Existing effects and spawned entities may remain, depending on the rule.
admin-gamerules-stop-waiting = Stopping the selected GameRule...
admin-gamerules-stop-failed = Could not stop the GameRule. It may have already ended, or you may no longer have permission.
admin-gamerules-current-schedulers = Current schedulers
admin-gamerules-current-rules = Current GameRules
admin-gamerules-history = GameRule history
admin-gamerules-history-hint = GameRules added this round, newest first, including cancelled starts. Times are measured from round start. Ended means the GameRule finished; its effects may remain.
admin-gamerules-history-empty = No GameRules have been added this round.
admin-gamerules-history-added = Added
admin-gamerules-history-started = Started
admin-gamerules-history-ended = Ended
admin-gamerules-history-source = Source
admin-gamerules-history-reason = End reason
admin-gamerules-history-status =
    { $status ->
        [Pending] { admin-gamerules-pending }
        [Delayed] { admin-gamerules-delayed }
        [Active] { admin-gamerules-active }
        [Ended] Ended
        [Stopped] Stopped
        [Cancelled] Cancelled
       *[other] { admin-gamerules-history-unknown }
    }
admin-gamerules-history-unknown = Unknown
admin-gamerules-history-empty-value = —
admin-gamerules-history-source-admin = Administrator: { $name }
admin-gamerules-history-source-value =
    { $kind ->
        [Administrator] { admin-gamerules-history-source-admin }
        [ServerConsole] { admin-gamerules-history-source-console }
        [Scheduler] { admin-gamerules-history-source-scheduler }
       *[other] { admin-gamerules-history-unknown }
    }
admin-gamerules-history-source-console = Server console
admin-gamerules-history-source-scheduler = { $name } ({ $entity }) / { $table }
admin-gamerules-history-end-reason =
    { $reason ->
        [DurationElapsed] Configured duration elapsed
        [Administrator] Stopped by an administrator
        [ServerConsole] Stopped through the server console
        [RulesCleared] GameRules cleared
        [EntityDeleted] GameRule entity deleted
       *[other] { admin-gamerules-history-unknown }
    }
admin-gamerules-history-reason-by = { $reason } ({ $name })
admin-gamerules-next-attempt = Next event attempt: { $time }
admin-gamerules-next-attempt-paused = Next event attempt: paused ({ $time } remaining)
admin-gamerules-next-attempt-inactive = Next event attempt: scheduler is not active
admin-gamerules-next-attempt-tooltip = Updates every second. When the timer expires, the scheduler attempts to select an event. A successful start is not guaranteed.
admin-gamerules-likelihood-title = Likely events — relative weights
admin-gamerules-likelihood-weight = Weight: { $weight }
admin-gamerules-likelihood-more = { $count } more eligible events in the list below.
admin-gamerules-likelihood-empty = No eligible events at the last refresh.
admin-gamerules-likelihood-hint = Highest eligible weights at the last refresh. Table randomness may change the candidates. These bars are not percentages; use Refresh to update.
admin-gamerules-delay-fixed = Configured start delay: { $seconds } s.
admin-gamerules-delay-range = Configured start delay: { $min }–{ $max } s (random).
