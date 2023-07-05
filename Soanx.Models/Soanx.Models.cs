
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soanx.Models;

public enum SenderType {
    Unknown = 0,
    Chat = 1,
    User = 2
}
    
public class TgMessage {

	[Key]
    public long Id { get; set; }

    public UpdateType UpdateType { get; set; }
        
    public SenderType SenderType { get; set; }

    public long TgChatId { get; set; }

    public long TgMessageId { get; set; }

    public long SenderId { get; set; }
    public MessageContentType MessageContentType { get; set; }
    public string Text { get; set; }

    public DateTime CreatedDate { get; set; }
        
    public string RawData { get; set; }

	[Column(TypeName = "jsonb")]
    public string ExtractedFacts { get; set; }
        
}

public enum UpdateType {
    None = 0,
    UpdateActiveEmojiReactions = 1,
    UpdateActiveNotifications = 2,
    UpdateAnimatedEmojiMessageClicked = 3,
    UpdateAnimationSearchParameters = 4,
    UpdateAttachmentMenuBots = 5,
    UpdateAuthorizationState = 6,
    UpdateBasicGroup = 7,
    UpdateBasicGroupFullInfo = 8,
    UpdateCall = 9,
    UpdateChatAction = 10,
    UpdateChatActionBar = 11,
    UpdateChatAvailableReactions = 12,
    UpdateChatDefaultDisableNotification = 13,
    UpdateChatDraftMessage = 14,
    UpdateChatFilters = 15,
    UpdateChatHasProtectedContent = 16,
    UpdateChatHasScheduledMessages = 17,
    UpdateChatIsBlocked = 18,
    UpdateChatIsMarkedAsUnread = 19,
    UpdateChatLastMessage = 20,
    UpdateChatMember = 21,
    UpdateChatMessageSender = 22,
    UpdateChatMessageTtl = 23,
    UpdateChatNotificationSettings = 24,
    UpdateChatOnlineMemberCount = 25,
    UpdateChatPendingJoinRequests = 26,
    UpdateChatPermissions = 27,
    UpdateChatPhoto = 28,
    UpdateChatPosition = 29,
    UpdateChatReadInbox = 30,
    UpdateChatReadOutbox = 31,
    UpdateChatReplyMarkup = 32,
    UpdateChatTheme = 33,
    UpdateChatThemes = 34,
    UpdateChatTitle = 35,
    UpdateChatUnreadMentionCount = 36,
    UpdateChatUnreadReactionCount = 37,
    UpdateChatVideoChat = 38,
    UpdateConnectionState = 39,
    UpdateDefaultReactionType = 40,
    UpdateDeleteMessages = 41,
    UpdateDiceEmojis = 42,
    UpdateFavoriteStickers = 43,
    UpdateFile = 44,
    UpdateFileAddedToDownloads = 45,
    UpdateFileDownload = 46,
    UpdateFileDownloads = 47,
    UpdateFileGenerationStart = 48,
    UpdateFileGenerationStop = 49,
    UpdateFileRemovedFromDownloads = 50,
    UpdateForumTopicInfo = 51,
    UpdateGroupCall = 52,
    UpdateGroupCallParticipant = 53,
    UpdateHavePendingNotifications = 54,
    UpdateInstalledStickerSets = 55,
    UpdateLanguagePackStrings = 56,
    UpdateMessageContent = 57,
    UpdateMessageContentOpened = 58,
    UpdateMessageEdited = 59,
    UpdateMessageInteractionInfo = 60,
    UpdateMessageIsPinned = 61,
    UpdateMessageLiveLocationViewed = 62,
    UpdateMessageMentionRead = 63,
    UpdateMessageSendAcknowledged = 64,
    UpdateMessageSendFailed = 65,
    UpdateMessageSendSucceeded = 66,
    UpdateMessageUnreadReactions = 67,
    UpdateNewCallbackQuery = 68,
    UpdateNewCallSignalingData = 69,
    UpdateNewChat = 70,
    UpdateNewChatJoinRequest = 71,
    UpdateNewChosenInlineResult = 72,
    UpdateNewCustomEvent = 73,
    UpdateNewCustomQuery = 74,
    UpdateNewInlineCallbackQuery = 75,
    UpdateNewInlineQuery = 76,
    UpdateNewMessage = 77,
    UpdateNewPreCheckoutQuery = 78,
    UpdateNewShippingQuery = 79,
    UpdateNotification = 80,
    UpdateNotificationGroup = 81,
    UpdateOption = 82,
    UpdatePoll = 83,
    UpdatePollAnswer = 84,
    UpdateRecentStickers = 85,
    UpdateSavedAnimations = 86,
    UpdateSavedNotificationSounds = 87,
    UpdateScopeNotificationSettings = 88,
    UpdateSecretChat = 89,
    UpdateSelectedBackground = 90,
    UpdateServiceNotification = 91,
    UpdateStickerSet = 92,
    UpdateSuggestedActions = 93,
    UpdateSupergroup = 94,
    UpdateSupergroupFullInfo = 95,
    UpdateTermsOfService = 96,
    UpdateTrendingStickerSets = 97,
    UpdateUnreadChatCount = 98,
    UpdateUnreadMessageCount = 99,
    UpdateUser = 100,
    UpdateUserFullInfo = 101,
    UpdateUserPrivacySettingRules = 102,
    UpdateUsersNearby = 103,
    UpdateUserStatus = 104,
    UpdateWebAppMessageSent = 105
}

public enum MessageContentType {
    None = 0,
    MessageChatChangeTitle = 1,
    MessageAnimatedEmoji = 2,
    MessageAnimation = 3,
    MessageAudio = 4,
    MessageBasicGroupChatCreate = 5,
    MessageCall = 6,
    MessageChatAddMembers = 7,
    MessageChatChangePhoto = 8,
    MessageChatDeleteMember = 9,
    MessageChatDeletePhoto = 10,
    MessageChatJoinByLink = 11,
    MessageChatJoinByRequest = 12,
    MessageChatSetTheme = 13,
    MessageChatSetTtl = 14,
    MessageChatUpgradeFrom = 15,
    MessageChatUpgradeTo = 16,
    MessageContact = 17,
    MessageContactRegistered = 18,
    MessageCustomServiceAction = 19,
    MessageDice = 20,
    MessageDocument = 21,
    MessageExpiredPhoto = 22,
    MessageExpiredVideo = 23,
    MessageForumTopicCreated = 24,
    MessageForumTopicEdited = 25,
    MessageForumTopicIsClosedToggled = 26,
    MessageForumTopicIsHiddenToggled = 27,
    MessageGame = 28,
    MessageGameScore = 29,
    MessageGiftedPremium = 30,
    MessageInviteVideoChatParticipants = 31,
    MessageInvoice = 32,
    MessageLocation = 33,
    MessagePassportDataReceived = 34,
    MessagePassportDataSent = 35,
    MessagePaymentSuccessful = 36,
    MessagePaymentSuccessfulBot = 37,
    MessagePhoto = 38,
    MessagePinMessage = 39,
    MessagePoll = 40,
    MessageProximityAlertTriggered = 41,
    MessageScreenshotTaken = 42,
    MessageSticker = 43,
    MessageSupergroupChatCreate = 44,
    MessageText = 45,
    MessageUnsupported = 46,
    MessageVenue = 47,
    MessageVideo = 48,
    MessageVideoChatEnded = 49,
    MessageVideoChatScheduled = 50,
    MessageVideoChatStarted = 51,
    MessageVideoNote = 52,
    MessageVoiceNote = 53,
    MessageWebAppDataReceived = 54,
    MessageWebAppDataSent = 55,
    MessageWebsiteConnected = 56
}
