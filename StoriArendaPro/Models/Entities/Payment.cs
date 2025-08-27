using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace StoriArendaPro.Models.Entities
{
    [Table("payments")]
    public partial class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("payment_id")]
        public int PaymentId { get; set; }

        [Required]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("order_type")]
        public string OrderType { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(3)]
        [Column("currency")]
        public string Currency { get; set; } = "RUB";

        [StringLength(50)]
        [Column("payment_method")]
        public string? PaymentMethod { get; set; }

        [Required]
        [StringLength(20)]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "ожидает";

        [StringLength(255)]
        [Column("transaction_id")]
        public string? TransactionId { get; set; }

        [Column("payment_data", TypeName = "jsonb")]
        public JsonDocument? PaymentData { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
