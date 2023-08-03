using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Soanx.Repository;
using Soanx.TelegramModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.Repositories.Models;

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

public enum TgMessageRawProcessingStatus {
    Unknown = 0,
    NotProcessed = 1,
    Processed = 2,
}

public interface TgMessageUniqueIds {
    public long TgChatId { get; set; }
    public long TgChatMessageId { get; set; }
}

public class TgMessageRaw: TgMessageUniqueIds {

    [Key]
    public long Id { get; set; }
    public TgMessageRawProcessingStatus ProcessingStatus { get; set; }
    /// <summary>
    /// Actual only for storing from events and always "None" for reading previous messages
    /// </summary>
    public UpdateType UpdateType { get; set; }
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
    public bool Equals(TgMessageRaw x, TgMessageRaw y) {
        return x.TgChatId == y.TgChatId && x.TgChatMessageId == y.TgChatMessageId;
    }

    public int GetHashCode(TgMessageRaw obj) {
        return HashCode.Combine(obj.TgChatId, obj.TgChatMessageId);
    }
}
