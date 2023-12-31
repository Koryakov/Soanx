﻿using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Soanx.Repository;
using Soanx.TelegramModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Soanx.Repositories.Models;

public class TgMessage {

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    [Required]
    public long TgChatId { get; set; }
    [Required]
    public long TgMessageId { get; set; }
    public long? SenderId { get; set; }
    public string Text { get; set; }
    public SenderType SenderType { get; set; }
    public DateTime? CreatedDateUTC { get; set; }
    [Required]
    public DateTime GrabbedDateUTC { get; set; }
    [Required]
    public SoanxTdUpdateType UpdateType { get; set; }
    public MessageContentType ContentType { get; set; }
    public DateTime? AnalyzedStatusModifiedDateUTC { get; set; }
    public TgMessageAnalyzedStatus AnalyzedStatus { get; set; }

    public enum TgMessageAnalyzedStatus {
        Unknown = 0,
        InProcess = 1,
        Analyzed = 2
    }
}


public class TgMessage2 {

    [Key]
    public long Id { get; set; }
    public SoanxTdUpdateType UpdateType { get; set; }
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

public enum TgMessageRawProcessingStatus {
    Unknown = 0,
    NotProcessed = 1,
    Processed = 2,
}

public interface ITgMessageUniqueIds {
    public long TgChatId { get; set; }
    public long TgChatMessageId { get; set; }
}

public class TgMessageUniqueIds {
    public long TgChatId { get; set; }
    public long TgChatMessageId { get; set; }

    public TgMessageUniqueIds() { }
    public TgMessageUniqueIds(long chatId, long messageId) {
        TgChatId = chatId;
        TgChatMessageId = messageId;
    }
}

public class TgMessageRaw: TgMessageUniqueIds {

    public TgMessageRaw() { }
    public TgMessageRaw(long chatId, long messageId) : base(chatId, messageId) { }

    [Key]
    public long Id { get; set; }
    public TgMessageRawProcessingStatus ProcessingStatus { get; set; }
    /// <summary>
    /// Actual only for storing from events and always "None" for reading previous messages
    /// </summary>
    public SoanxTdUpdateType UpdateType { get; set; }
    public long TgChatId { get; set; }
    public long TgChatMessageId { get; set; }
    public SenderType SenderType { get; set; }
    public long SenderId { get; set; }
    public MessageContentType ContentType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string Text { get; set; }
}

public class TgMessageRawComparer : IEqualityComparer<TgMessageRaw> {
    private Serilog.ILogger log = Log.ForContext<TgMessageRawComparer>();

    public bool Equals(TgMessageRaw x, TgMessageRaw y) {
        bool result = x.TgChatId == y.TgChatId && x.TgChatMessageId == y.TgChatMessageId;
        //log.Verbose("Equals={@result}. x.ChatId = {@xChatId}, x.MsgId = {@xMsgId}, y.ChatId = {@yChatId}, y.MsgId = {@yMsgId}", result, x.TgChatId, x.TgChatMessageId, y.TgChatId, y.TgChatMessageId);
        return result;
    }

    public int GetHashCode(TgMessageRaw obj) {
        return HashCode.Combine(obj.TgChatId, obj.TgChatMessageId);
    }
}

public class TgMessageComparer : IEqualityComparer<TgMessage> {
    private Serilog.ILogger log = Log.ForContext<TgMessageComparer>();

    public bool Equals(TgMessage x, TgMessage y) {
        bool result = x.TgChatId == y.TgChatId && x.TgMessageId == y.TgMessageId;
        //log.Verbose("Equals={@result}. x.ChatId = {@xChatId}, x.MsgId = {@xMsgId}, y.ChatId = {@yChatId}, y.MsgId = {@yMsgId}", result, x.TgChatId, x.TgChatMessageId, y.TgChatId, y.TgChatMessageId);
        return result;
    }

    public int GetHashCode(TgMessage obj) {
        return HashCode.Combine(obj.TgChatId, obj.TgMessageId);
    }
}

public class ExchangeMessage {

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    [Required]
    public long SoanxMessageId { get; set; }
    [Required]
    public DateTime CreatedDateUTC { get; set; }

}
