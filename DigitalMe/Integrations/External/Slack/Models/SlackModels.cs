using System.Text.Json.Serialization;

namespace DigitalMe.Integrations.External.Slack.Models;

#region Base Response Types

/// <summary>
/// Base Slack API response
/// </summary>
public class SlackApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("warning")]
    public string? Warning { get; set; }
}

#endregion

#region Message Types

/// <summary>
/// Slack message request structure
/// </summary>
public class SlackMessageRequest
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("blocks")]
    public object? Blocks { get; set; }

    [JsonPropertyName("attachments")]
    public IEnumerable<SlackAttachment>? Attachments { get; set; }

    [JsonPropertyName("thread_ts")]
    public string? ThreadTimestamp { get; set; }

    [JsonPropertyName("reply_broadcast")]
    public bool? ReplyBroadcast { get; set; }

    [JsonPropertyName("unfurl_links")]
    public bool? UnfurlLinks { get; set; } = true;

    [JsonPropertyName("unfurl_media")]
    public bool? UnfurlMedia { get; set; } = true;
}

/// <summary>
/// Slack message response
/// </summary>
public class SlackMessageResponse : SlackApiResponse
{
    [JsonPropertyName("ts")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("message")]
    public SlackMessage? Message { get; set; }
}

/// <summary>
/// Slack message structure
/// </summary>
public class SlackMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("thread_ts")]
    public string? ThreadTimestamp { get; set; }

    [JsonPropertyName("reply_count")]
    public int? ReplyCount { get; set; }

    [JsonPropertyName("reactions")]
    public IEnumerable<SlackReaction>? Reactions { get; set; }

    [JsonPropertyName("attachments")]
    public IEnumerable<SlackAttachment>? Attachments { get; set; }

    [JsonPropertyName("blocks")]
    public object? Blocks { get; set; }
}

/// <summary>
/// Slack messages list response
/// </summary>
public class SlackMessagesResponse : SlackApiResponse
{
    [JsonPropertyName("messages")]
    public IEnumerable<SlackMessage> Messages { get; set; } = Enumerable.Empty<SlackMessage>();

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("response_metadata")]
    public SlackResponseMetadata? ResponseMetadata { get; set; }
}

#endregion

#region File Types

/// <summary>
/// Slack file response
/// </summary>
public class SlackFileResponse : SlackApiResponse
{
    [JsonPropertyName("file")]
    public SlackFile? File { get; set; }
}

/// <summary>
/// Slack file structure
/// </summary>
public class SlackFile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("mimetype")]
    public string? MimeType { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url_private")]
    public string? UrlPrivate { get; set; }

    [JsonPropertyName("url_private_download")]
    public string? UrlPrivateDownload { get; set; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }
}

#endregion

#region Channel Types

/// <summary>
/// Slack channel structure
/// </summary>
public class SlackChannel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_channel")]
    public bool IsChannel { get; set; }

    [JsonPropertyName("is_group")]
    public bool IsGroup { get; set; }

    [JsonPropertyName("is_im")]
    public bool IsDirectMessage { get; set; }

    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("is_general")]
    public bool IsGeneral { get; set; }

    [JsonPropertyName("topic")]
    public SlackChannelTopic? Topic { get; set; }

    [JsonPropertyName("purpose")]
    public SlackChannelPurpose? Purpose { get; set; }

    [JsonPropertyName("num_members")]
    public int? MemberCount { get; set; }
}

/// <summary>
/// Slack channel topic
/// </summary>
public class SlackChannelTopic
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("last_set")]
    public long LastSet { get; set; }
}

/// <summary>
/// Slack channel purpose
/// </summary>
public class SlackChannelPurpose
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    [JsonPropertyName("last_set")]
    public long LastSet { get; set; }
}

/// <summary>
/// Slack channels list response
/// </summary>
public class SlackChannelsResponse : SlackApiResponse
{
    [JsonPropertyName("channels")]
    public IEnumerable<SlackChannel> Channels { get; set; } = Enumerable.Empty<SlackChannel>();

    [JsonPropertyName("response_metadata")]
    public SlackResponseMetadata? ResponseMetadata { get; set; }
}

/// <summary>
/// Slack channel info response
/// </summary>
public class SlackChannelResponse : SlackApiResponse
{
    [JsonPropertyName("channel")]
    public SlackChannel? Channel { get; set; }
}

#endregion

#region User Types

/// <summary>
/// Slack user structure
/// </summary>
public class SlackUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("real_name")]
    public string? RealName { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    [JsonPropertyName("is_primary_owner")]
    public bool IsPrimaryOwner { get; set; }

    [JsonPropertyName("is_restricted")]
    public bool IsRestricted { get; set; }

    [JsonPropertyName("is_ultra_restricted")]
    public bool IsUltraRestricted { get; set; }

    [JsonPropertyName("deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("profile")]
    public SlackUserProfile? Profile { get; set; }

    [JsonPropertyName("tz")]
    public string? Timezone { get; set; }

    [JsonPropertyName("tz_label")]
    public string? TimezoneLabel { get; set; }

    [JsonPropertyName("tz_offset")]
    public int TimezoneOffset { get; set; }
}

/// <summary>
/// Slack user profile
/// </summary>
public class SlackUserProfile
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("skype")]
    public string? Skype { get; set; }

    [JsonPropertyName("real_name")]
    public string? RealName { get; set; }

    [JsonPropertyName("real_name_normalized")]
    public string? RealNameNormalized { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("display_name_normalized")]
    public string? DisplayNameNormalized { get; set; }

    [JsonPropertyName("status_text")]
    public string? StatusText { get; set; }

    [JsonPropertyName("status_emoji")]
    public string? StatusEmoji { get; set; }

    [JsonPropertyName("avatar_hash")]
    public string? AvatarHash { get; set; }

    [JsonPropertyName("image_24")]
    public string? Image24 { get; set; }

    [JsonPropertyName("image_32")]
    public string? Image32 { get; set; }

    [JsonPropertyName("image_48")]
    public string? Image48 { get; set; }

    [JsonPropertyName("image_72")]
    public string? Image72 { get; set; }

    [JsonPropertyName("image_192")]
    public string? Image192 { get; set; }

    [JsonPropertyName("image_512")]
    public string? Image512 { get; set; }
}

/// <summary>
/// Slack users list response
/// </summary>
public class SlackUsersResponse : SlackApiResponse
{
    [JsonPropertyName("members")]
    public IEnumerable<SlackUser> Members { get; set; } = Enumerable.Empty<SlackUser>();

    [JsonPropertyName("response_metadata")]
    public SlackResponseMetadata? ResponseMetadata { get; set; }
}

/// <summary>
/// Slack user info response
/// </summary>
public class SlackUserResponse : SlackApiResponse
{
    [JsonPropertyName("user")]
    public SlackUser? User { get; set; }
}

/// <summary>
/// Slack auth test response
/// </summary>
public class SlackAuthResponse : SlackApiResponse
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("team")]
    public string? Team { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("team_id")]
    public string? TeamId { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("bot_id")]
    public string? BotId { get; set; }
}

#endregion

#region Interactive Elements

/// <summary>
/// Slack button for interactive messages
/// </summary>
public class SlackButton
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("action_id")]
    public string ActionId { get; set; } = string.Empty;

    [JsonPropertyName("style")]
    public string? Style { get; set; } // primary, danger, etc.

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("confirm")]
    public SlackConfirmDialog? Confirm { get; set; }
}

/// <summary>
/// Slack confirmation dialog
/// </summary>
public class SlackConfirmDialog
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("confirm")]
    public string Confirm { get; set; } = "Yes";

    [JsonPropertyName("deny")]
    public string Deny { get; set; } = "No";

    [JsonPropertyName("style")]
    public string? Style { get; set; } // danger
}

/// <summary>
/// Slack message attachment
/// </summary>
public class SlackAttachment
{
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("pretext")]
    public string? Pretext { get; set; }

    [JsonPropertyName("author_name")]
    public string? AuthorName { get; set; }

    [JsonPropertyName("author_link")]
    public string? AuthorLink { get; set; }

    [JsonPropertyName("author_icon")]
    public string? AuthorIcon { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("title_link")]
    public string? TitleLink { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("fields")]
    public IEnumerable<SlackField>? Fields { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("thumb_url")]
    public string? ThumbUrl { get; set; }

    [JsonPropertyName("footer")]
    public string? Footer { get; set; }

    [JsonPropertyName("footer_icon")]
    public string? FooterIcon { get; set; }

    [JsonPropertyName("ts")]
    public long? Timestamp { get; set; }

    [JsonPropertyName("mrkdwn_in")]
    public IEnumerable<string>? MarkdownIn { get; set; }
}

/// <summary>
/// Slack attachment field
/// </summary>
public class SlackField
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("short")]
    public bool Short { get; set; }
}

#endregion

#region Reaction Types

/// <summary>
/// Slack reaction structure
/// </summary>
public class SlackReaction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("users")]
    public IEnumerable<string> Users { get; set; } = Enumerable.Empty<string>();
}

/// <summary>
/// Slack reactions response
/// </summary>
public class SlackReactionsResponse : SlackApiResponse
{
    [JsonPropertyName("message")]
    public SlackMessage? Message { get; set; }
}

#endregion

#region Metadata Types

/// <summary>
/// Slack API response metadata for pagination
/// </summary>
public class SlackResponseMetadata
{
    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("messages")]
    public IEnumerable<string>? Messages { get; set; }

    [JsonPropertyName("warnings")]
    public IEnumerable<string>? Warnings { get; set; }
}

#endregion

#region Webhook Types

/// <summary>
/// Slack webhook event structure
/// </summary>
public class SlackWebhookEvent
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("team_id")]
    public string? TeamId { get; set; }

    [JsonPropertyName("api_app_id")]
    public string? ApiAppId { get; set; }

    [JsonPropertyName("event")]
    public SlackEvent? Event { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    [JsonPropertyName("event_time")]
    public long? EventTime { get; set; }

    [JsonPropertyName("authorizations")]
    public IEnumerable<SlackAuthorization>? Authorizations { get; set; }

    [JsonPropertyName("is_ext_shared_channel")]
    public bool? IsExtSharedChannel { get; set; }

    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; } // For URL verification
}

/// <summary>
/// Slack event data structure
/// </summary>
public class SlackEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("ts")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("event_ts")]
    public string? EventTimestamp { get; set; }

    [JsonPropertyName("channel_type")]
    public string? ChannelType { get; set; }

    [JsonPropertyName("thread_ts")]
    public string? ThreadTimestamp { get; set; }

    [JsonPropertyName("reaction")]
    public string? Reaction { get; set; }

    [JsonPropertyName("item")]
    public SlackReactionItem? Item { get; set; }

    [JsonPropertyName("item_user")]
    public string? ItemUser { get; set; }

    [JsonPropertyName("files")]
    public IEnumerable<SlackFile>? Files { get; set; }

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }
}

/// <summary>
/// Slack authorization structure for webhook events
/// </summary>
public class SlackAuthorization
{
    [JsonPropertyName("enterprise_id")]
    public string? EnterpriseId { get; set; }

    [JsonPropertyName("team_id")]
    public string? TeamId { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("is_enterprise_install")]
    public bool? IsEnterpriseInstall { get; set; }
}

/// <summary>
/// Slack reaction item structure
/// </summary>
public class SlackReactionItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("ts")]
    public string? Timestamp { get; set; }
}

/// <summary>
/// Slack interactive component payload
/// </summary>
public class SlackInteractionPayload
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public SlackUser? User { get; set; }

    [JsonPropertyName("api_app_id")]
    public string? ApiAppId { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("container")]
    public object? Container { get; set; }

    [JsonPropertyName("trigger_id")]
    public string? TriggerId { get; set; }

    [JsonPropertyName("team")]
    public SlackTeam? Team { get; set; }

    [JsonPropertyName("enterprise")]
    public object? Enterprise { get; set; }

    [JsonPropertyName("is_enterprise_install")]
    public bool? IsEnterpriseInstall { get; set; }

    [JsonPropertyName("channel")]
    public SlackChannel? Channel { get; set; }

    [JsonPropertyName("message")]
    public SlackMessage? Message { get; set; }

    [JsonPropertyName("response_url")]
    public string? ResponseUrl { get; set; }

    [JsonPropertyName("actions")]
    public IEnumerable<SlackAction>? Actions { get; set; }

    [JsonPropertyName("view")]
    public object? View { get; set; }
}

/// <summary>
/// Slack team structure
/// </summary>
public class SlackTeam
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Slack action structure for interactive components
/// </summary>
public class SlackAction
{
    [JsonPropertyName("action_id")]
    public string ActionId { get; set; } = string.Empty;

    [JsonPropertyName("block_id")]
    public string? BlockId { get; set; }

    [JsonPropertyName("text")]
    public object? Text { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("action_ts")]
    public string? ActionTimestamp { get; set; }

    [JsonPropertyName("selected_option")]
    public object? SelectedOption { get; set; }

    [JsonPropertyName("selected_options")]
    public object[]? SelectedOptions { get; set; }
}

/// <summary>
/// Slack slash command structure
/// </summary>
public class SlackSlashCommand
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("team_id")]
    public string? TeamId { get; set; }

    [JsonPropertyName("team_domain")]
    public string? TeamDomain { get; set; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("channel_name")]
    public string? ChannelName { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("api_app_id")]
    public string? ApiAppId { get; set; }

    [JsonPropertyName("is_enterprise_install")]
    public string? IsEnterpriseInstall { get; set; }

    [JsonPropertyName("response_url")]
    public string? ResponseUrl { get; set; }

    [JsonPropertyName("trigger_id")]
    public string? TriggerId { get; set; }
}

/// <summary>
/// Slack interaction response structure
/// </summary>
public class SlackInteractionResponse
{
    [JsonPropertyName("response_type")]
    public string? ResponseType { get; set; } // "ephemeral" or "in_channel"

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("blocks")]
    public object? Blocks { get; set; }

    [JsonPropertyName("attachments")]
    public IEnumerable<SlackAttachment>? Attachments { get; set; }

    [JsonPropertyName("replace_original")]
    public bool? ReplaceOriginal { get; set; }

    [JsonPropertyName("delete_original")]
    public bool? DeleteOriginal { get; set; }
}

/// <summary>
/// Slack slash command response structure
/// </summary>
public class SlackSlashCommandResponse
{
    [JsonPropertyName("response_type")]
    public string? ResponseType { get; set; } // "ephemeral" or "in_channel"

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("blocks")]
    public object? Blocks { get; set; }

    [JsonPropertyName("attachments")]
    public IEnumerable<SlackAttachment>? Attachments { get; set; }
}

/// <summary>
/// Slack modal structure
/// </summary>
public class SlackModal
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "modal";

    [JsonPropertyName("callback_id")]
    public string? CallbackId { get; set; }

    [JsonPropertyName("title")]
    public object? Title { get; set; }

    [JsonPropertyName("submit")]
    public object? Submit { get; set; }

    [JsonPropertyName("close")]
    public object? Close { get; set; }

    [JsonPropertyName("blocks")]
    public object? Blocks { get; set; }

    [JsonPropertyName("private_metadata")]
    public string? PrivateMetadata { get; set; }

    [JsonPropertyName("clear_on_close")]
    public bool? ClearOnClose { get; set; }

    [JsonPropertyName("notify_on_close")]
    public bool? NotifyOnClose { get; set; }
}

#endregion
