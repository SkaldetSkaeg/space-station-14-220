// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CCVar;
using Content.Shared.Chat.TypingIndicator;

namespace Content.Client.Chat.TypingIndicator;

public sealed partial class TypingIndicatorSystem
{
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
    /// Stores the effective input channel supplied by the UI and updates the indicator when it changes.
    /// </summary>
    public void RefreshChatChannel(bool telepathyInput)
    {
        if (_telepathyInput == telepathyInput)
            return;

        _telepathyInput = telepathyInput;
        ClientUpdateTyping();
    }

    /// <summary>
    /// Updates the input channel before the focus change emits a typing event.
    /// </summary>
    public void ClientChangedChatFocus(bool isFocused, bool telepathyInput)
    {
        _telepathyInput = telepathyInput;
        ClientChangedChatFocus(isFocused);
    }

    private TypingChangedEvent CreateTypingChangedEvent(TypingIndicatorState state)
    {
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
