namespace StoriArendaPro.Services.Models
{
    public class YooKassaPaymentResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public YooKassaConfirmation Confirmation { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class YooKassaConfirmation
    {
        public string Type { get; set; }
        public string ConfirmationUrl { get; set; }
    }
}
