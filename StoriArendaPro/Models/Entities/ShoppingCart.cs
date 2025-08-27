using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.Entities
{
    [Table("shopping_cart")]
    public partial class ShoppingCart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("cart_id")]
        public int CartId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("rental_price_id")]
        public int? RentalPriceId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Column("quantity")]
        public int Quantity { get; set; } = 1;

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [StringLength(30)]
        [Column("rental_type")]
        public string? RentalType { get; set; }

        [Required]
        [Column("unit_price", TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column("subtotal", TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("RentalPriceId")]
        public virtual RentalPrice? RentalPrice { get; set; }
    }
}
