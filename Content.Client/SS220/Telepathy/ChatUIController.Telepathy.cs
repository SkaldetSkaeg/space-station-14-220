// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.SS220.Telepathy;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    /// <summary>
    /// Resolves telepathy from the focused input using the same prefix precedence as message submission.
    /// </summary>
    public bool IsTelepathyChatFocused()
    {
        foreach (var box in _chats)
        {
            if (!box.ChatInput.Input.HasKeyboardFocus())
                continue;

            var channel = SplitInputContents(box.ChatInput.Input.Text).chatChannel;
            if (channel == ChatSelectChannel.None)
                channel = box.SelectedChannel;

            return channel == ChatSelectChannel.Telepathy;
        }

        return false;
    }

    private void RefreshTelepathyTyping(ChatBox box, ChatSelectChannel prefixChannel)
    {
        if (!box.ChatInput.Input.HasKeyboardFocus())
            return;

        var channel = prefixChannel == ChatSelectChannel.None ? box.SelectedChannel : prefixChannel;
        _typingIndicator?.RefreshChatChannel(channel == ChatSelectChannel.Telepathy);
    }

    /// <summary>
    /// Recalculates chat channels after changes to the local player's abilities.
    /// </summary>
    public void RefreshChannelPermissions()
    {
        UpdateChannelPermissions();
    }

    private void UpdateTelepathyChannelPermissions(bool isAdmin)
    {
        if (!CanSelectTelepathyChannel(isAdmin))
            return;

        FilterableChannels |= ChatChannel.Telepathy;
        CanSendChannels |= ChatSelectChannel.Telepathy;
    }

    private bool CanSelectTelepathyChannel(bool isAdmin)
    {
        var entity = _player.LocalEntity;
        if (entity == null)
            return isAdmin;

        if (EntityManager.HasComponent<TelepathyComponent>(entity.Value))
            return true;

        return isAdmin;
    }
}
