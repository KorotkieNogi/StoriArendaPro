namespace StoriArendaPro.Models.Entities
{
    public class RentalRequest
    {
        public int RequestId { get; set; }

        public int? UserId { get; set; }

        public int? RentalPpriceId { get; set; }

        public TimeSpan CallTime { get; set; }

        public DateOnly CallDate { get; set; }

        public string? Status { get; set; }

        public string? AdminNotes { get; set; }

        public string? CommentClient { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual RentalPrice? RentalPrice { get; set; }

        public virtual User? User { get; set; }
    }
}
