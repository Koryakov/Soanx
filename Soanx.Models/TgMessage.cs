
using Soanx.TelegramModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soanx.RepositoryModels;
    
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