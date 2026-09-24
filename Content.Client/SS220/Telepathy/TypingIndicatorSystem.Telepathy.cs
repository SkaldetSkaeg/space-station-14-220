// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.CCVar;
using Content.Shared.Chat.TypingIndicator;
using Robust.Client.UserInterface;

namespace Content.Client.Chat.TypingIndicator;

public sealed partial class TypingIndicatorSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private bool _telepathyInput;
    private bool _typingUpdateQueued;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_typingUpdateQueued)
            return;

        if (!_time.InPrediction)
            return;

        if (!_time.IsFirstTimePredicted)
            return;

        _typingUpdateQueued = false;
        ClientUpdateTyping();
    }

    /// <summary>
    /// Updates the indicator when the effective channel changes without a focus or typing event.
    /// </summary>
    public void RefreshChatChannel(bool telepathyInput)
    {
        if (_telepathyInput == telepathyInput)
            return;

        ClientUpdateTyping();
    }

    private TypingChangedEvent CreateTypingChangedEvent(TypingIndicatorState state)
    {
        _telepathyInput = _ui.GetUIController<ChatUIController>().IsTelepathyChatFocused();
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            state = TypingIndicatorState.None;

        return new TypingChangedEvent(state) { IsTelepathy = _telepathyInput };
    }

    private void SendTypingChangedEvent(TypingIndicatorState state)
    {
        // Replicated CVar changes may run while applying server state, outside prediction.
        if (!_time.InPrediction)
        {
            _typingUpdateQueued = true;
            return;
        }

        if (!_time.IsFirstTimePredicted)
        {
            _typingUpdateQueued = true;
            return;
        }

        _typingUpdateQueued = false;
        RaisePredictiveEvent(CreateTypingChangedEvent(state));
    }
}
