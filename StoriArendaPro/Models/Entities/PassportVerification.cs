using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.Entities
{
    [Table("passport_verification")]
    public partial class PassportVerification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("verification_id")]
        public int VerificationId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(4)]
        [Column("passport_seria")]
        public string PassportSeria { get; set; }

        [Required]
        [StringLength(6)]
        [Column("passport_number")]
        public string PassportNumber { get; set; }

        [Column("issued_by")]
        public string? IssuedBy { get; set; }

        [Column("issue_date")]
        public DateTime? IssueDate { get; set; }

        [Required]
        [Column("propiska")]
        public string Propiska { get; set; }

        [Required]
        [Column("place_live")]
        public string PlaceLive { get; set; }

        [Column("passport_photo_front")]
        public string? PassportPhotoFront { get; set; }

        [Column("passport_photo_back")]
        public string? PassportPhotoBack { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "ожидает рассмотрения";

        [Column("admin_notes")]
        public string? AdminNotes { get; set; }

        [Column("verified_by")]
        public int? VerifiedBy { get; set; }

        [Column("verified_at")]
        public DateTime? VerifiedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("VerifiedBy")]
        public virtual User? Verifier { get; set; }
    }
}
