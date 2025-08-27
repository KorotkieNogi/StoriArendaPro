namespace StoriArendaPro.Models.ViewModels
{
    public class PaymentResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}
