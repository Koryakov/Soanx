﻿using Soanx.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TdLib.TdApi.MessageContent;
using static TdLib.TdApi.MessageSender;
using static TdLib.TdApi;

namespace Soanx.TelegramAnalyzer {

    public static class DateTimeHelper {

        public static DateTime FromUnixTime(int unixTimestamp) {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        public static int ToUnixTime(DateTime dateTime) {
            return (int)((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }

    }

    public static class MessageConverter {

        public static TgMessage ConvertTgMessage(Message message, UpdateType updateType, string rawData) {
            //TODO: store messages to Queue
            //((TdLib.TdApi.MessageContent.MessageText)update.Message.Content).Text.Text
            TgMessage tgMessage = new() {
                TgMessageId = message.Id,
                UpdateType = updateType,
                TgChatId = message.ChatId,
                RawData = rawData,
                //TODO: check is localor UTC Datetime
                CreatedDate = DateTimeHelper.FromUnixTime(message.Date),
            };
            InitializeSenderInfo(ref tgMessage, message.SenderId);
            InitializeMessageContentInfo(ref tgMessage, message.Content);

            return tgMessage;
        }

        private static void InitializeMessageContentInfo(ref TgMessage tgMessage, MessageContent messageContent) {
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

    }

    internal class ChatHelper {

        //public void Read

    }
}
