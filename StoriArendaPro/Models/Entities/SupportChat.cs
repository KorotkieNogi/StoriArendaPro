using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.Entities
{
    [Table("support_chat")]
    public partial class SupportChat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("chat_id")]
        public int ChatId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("admin_id")]
        public int? AdminId { get; set; }

        [StringLength(255)]
        [Column("subject")]
        public string? Subject { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "открыто";

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("AdminId")]
        public virtual User? Admin { get; set; }

        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}

