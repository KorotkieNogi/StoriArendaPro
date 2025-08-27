using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.Entities
{
    [Table("chat_messages")]
    public partial class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("message_id")]
        public int MessageId { get; set; }

        [Required]
        [Column("chat_id")]
        public int ChatId { get; set; }

        [Required]
        [Column("sender_id")]
        public int SenderId { get; set; }

        [Required]
        [Column("message_text")]
        public string MessageText { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ChatId")]
        public virtual SupportChat SupportChat { get; set; }

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }
    }
}
