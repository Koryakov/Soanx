﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TdLib.TdApi.MessageContent;
using static TdLib.TdApi.MessageSender;
using static TdLib.TdApi;
using Soanx.TelegramModels;
using Soanx.Repository;
using Soanx.Repositories.Models;
using Soanx.CurrencyExchange.Models;
using TdLib;
using static TdLib.TdApi.Update;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace Soanx.TelegramAnalyzer
{

    public static class DateTimeHelper {

        public static DateTime FromUnixTime(int unixTimestamp) {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }
        public static int ToUnixTime(DateTime dateTime) {
            return (int)((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }
    }

    public static class MessageConverter {

        public static TgMessage2 ConvertTgMessage(TdLib.TdApi.Message message, SoanxTdUpdateType updateType, string rawData) {
            //TODO: store messages to Queue
            //((TdLib.TdApi.MessageContent.MessageText)update.Message.Content).Text.Text
            TgMessage2 tgMessage = new() {
                TgMessageId = message.Id,
                UpdateType = updateType,
                TgChatId = message.ChatId,
                RawData = rawData,
                //TODO: check is local or UTC Datetime
                CreatedDate = DateTimeHelper.FromUnixTime(message.Date),
            };
            InitializeSenderInfo(ref tgMessage, message.SenderId);
            InitializeMessageContentInfo(ref tgMessage, message.Content);

            return tgMessage;
        }

        private static void InitializeMessageContentInfo(ref TgMessage2 tgMessage, MessageContent messageContent) {
            switch (messageContent) {
                case MessageContent.MessageText:
                    tgMessage.MessageContentType = MessageContentType.MessageText;
                    tgMessage.Text = ((MessageText)messageContent).Text.Text;
                    break;
                default:
                    tgMessage.MessageContentType = MessageContentType.None;
                    tgMessage.Text = string.Empty;
                    break;
            }
        }

        private static void InitializeSenderInfo(ref TgMessage2 tgMessage, MessageSender sender) {
            switch (sender) {
                case MessageSender.MessageSenderChat:
                    tgMessage.SenderType = SenderType.Chat;
                    tgMessage.SenderId = ((MessageSenderChat)sender).ChatId;
                    break;
                case MessageSender.MessageSenderUser:
                    tgMessage.SenderType = SenderType.User;
                    tgMessage.SenderId = ((MessageSenderUser)sender).UserId;
                    break;
                default:
                    tgMessage.SenderType = SenderType.Unknown;
                    tgMessage.SenderId = 0;
                    break;
            }
        }

        public static List<TgMessageRaw> ConvertToTgMessageRawList(List<TdLib.TdApi.Message> tdMessageList, SoanxTdUpdateType updateType) {
            //TODO: store messages to Queue
            //((TdLib.TdApi.MessageContent.MessageText)update.Message.Content).Text.Text
            var messageRawList = new List<TgMessageRaw>(tdMessageList.Count);

            foreach (var tdMessage in tdMessageList) {
                TgMessageRaw messageRaw = ConvertToTgMessageRaw(tdMessage, updateType);
                messageRawList.Add(messageRaw);
            }
            return messageRawList;
        }

        public static TgMessageRaw ConvertToTgMessageRaw(TdApi.Message tdMessage, SoanxTdUpdateType updateType) {
            var date = DateTimeHelper.FromUnixTime(tdMessage.Date);

            TgMessageRaw messageRaw = new() {
                TgChatId = tdMessage.ChatId,
                TgChatMessageId = tdMessage.Id,
                UpdateType = updateType,
                //TODO: check is local or UTC Datetime
                CreatedDate = date,
                ModifiedDate = date,
            };
            InitializeMessageContentInfo(ref messageRaw, tdMessage.Content);
            InitializeSenderInfo(ref messageRaw, tdMessage.SenderId);
            return messageRaw;
        }

        public static TgMessage ConvertToTgMessage(TdApi.Message tdMessage, SoanxTdUpdateType updateType) {
            var date = DateTimeHelper.FromUnixTime(tdMessage.Date);

            TgMessage messageRaw = new() {
                TgChatId = tdMessage.ChatId,
                TgMessageId = tdMessage.Id,
                UpdateType = updateType,
                //TODO: check is local or UTC Datetime
                CreatedDateUTC = date,
                GrabbedDateUTC = DateTime.UtcNow,
            };
            InitializeMessageContentInfo(ref messageRaw, tdMessage.Content);
            InitializeSenderInfo(ref messageRaw, tdMessage.SenderId);
            return messageRaw;
        }

        public static TgMessage ConvertToTgMessage(UpdateNewMessage msgNew) {

            TgMessage tgMessage = new() {
                TgChatId = msgNew.Message.ChatId,
                TgMessageId = msgNew.Message.Id,
                UpdateType = SoanxTdUpdateType.UpdateNewMessage,
                CreatedDateUTC = DateTimeHelper.FromUnixTime(msgNew.Message.Date),
                GrabbedDateUTC = DateTime.UtcNow,
            };
            InitializeMessageContentInfo(ref tgMessage, msgNew.Message.Content);
            InitializeSenderInfo(ref tgMessage, msgNew.Message.SenderId);

            return tgMessage;
        }

        public static TgMessage ConvertToTgMessage(UpdateMessageContent msgContent) {

            TgMessage tgMessage = new() {
                TgChatId = msgContent.ChatId,
                TgMessageId = msgContent.MessageId,
                UpdateType = SoanxTdUpdateType.UpdateMessageEdited,
                CreatedDateUTC = null,
                GrabbedDateUTC = DateTime.UtcNow,
            };
            InitializeMessageContentInfo(ref tgMessage, msgContent.NewContent);

            return tgMessage;
        }

        public static List<TgMessage> ConvertToTgMessageCollection(UpdateDeleteMessages msgDeleted) {
            var tgMessagesForDelete = new List<TgMessage>();
            foreach (long messageId in msgDeleted.MessageIds) {
                tgMessagesForDelete.Add(new() {
                    TgChatId = msgDeleted.ChatId,
                    TgMessageId = messageId,
                    UpdateType = SoanxTdUpdateType.UpdateDeleteMessages,
                    CreatedDateUTC = DateTime.UtcNow,
                });
            }
            return tgMessagesForDelete;
        }

        private static void InitializeMessageContentInfo(ref TgMessage tgMessage, MessageContent messageContent) {
            switch (messageContent) {
                case MessageContent.MessageText:
                    tgMessage.ContentType = MessageContentType.MessageText;
                    tgMessage.Text = ((MessageText)messageContent).Text.Text;
                    break;
                default:
                    tgMessage.ContentType = MessageContentType.None;
                    tgMessage.Text = string.Empty;
                    break;
            }
        }

        private static void InitializeSenderInfo(ref TgMessage tgMessage, MessageSender sender) {
            switch (sender) {
                case MessageSender.MessageSenderChat:
                    tgMessage.SenderType = SenderType.Chat;
                    tgMessage.SenderId = ((MessageSenderChat)sender).ChatId;
                    break;
                case MessageSender.MessageSenderUser:
                    tgMessage.SenderType = SenderType.User;
                    tgMessage.SenderId = ((MessageSenderUser)sender).UserId;
                    break;
                default:
                    tgMessage.SenderType = SenderType.Unknown;
                    tgMessage.SenderId = 0;
                    break;
            }
        }

        public static TgMessageRaw ConvertToTgMessageRaw(UpdateNewMessage message) {

            TgMessageRaw messageRaw = new() {
                TgChatId = message.Message.ChatId,
                TgChatMessageId = message.Message.Id,
                UpdateType = SoanxTdUpdateType.UpdateNewMessage,
                CreatedDate = DateTimeHelper.FromUnixTime(message.Message.Date),
                ModifiedDate = DateTimeHelper.FromUnixTime(message.Message.EditDate),
            };
            InitializeMessageContentInfo(ref messageRaw, message.Message.Content);
            InitializeSenderInfo(ref messageRaw, message.Message.SenderId);

            return messageRaw;
        }

        private static void InitializeMessageContentInfo(ref TgMessageRaw tgMessageRaw, MessageContent messageContent) {
            switch (messageContent) {
                case MessageContent.MessageText:
                    tgMessageRaw.ContentType = MessageContentType.MessageText;
                    tgMessageRaw.Text = ((MessageText)messageContent).Text.Text;
                    break;
                default:
                    tgMessageRaw.ContentType = MessageContentType.None;
                    tgMessageRaw.Text = string.Empty;
                    break;
            }
        }

        private static void InitializeSenderInfo(ref TgMessageRaw tgMessageRaw, MessageSender sender) {
            switch (sender) {
                case MessageSender.MessageSenderChat:
                    tgMessageRaw.SenderType = SenderType.Chat;
                    tgMessageRaw.SenderId = ((MessageSenderChat)sender).ChatId;
                    break;
                case MessageSender.MessageSenderUser:
                    tgMessageRaw.SenderType = SenderType.User;
                    tgMessageRaw.SenderId = ((MessageSenderUser)sender).UserId;
                    break;
                default:
                    tgMessageRaw.SenderType = SenderType.Unknown;
                    tgMessageRaw.SenderId = 0;
                    break;
            }
        }

    }

}
