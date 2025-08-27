using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; } // "rental" или "sale"
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";

        [Required(ErrorMessage = "Выберите способ оплаты")]
        public string PaymentMethod { get; set; }

        // Данные банковской карты (для демонстрации, в реальном проекте используйте PCI DSS compliance)
        [CreditCard(ErrorMessage = "Неверный номер карты")]
        public string CardNumber { get; set; }

        [StringLength(5, MinimumLength = 5, ErrorMessage = "Неверный срок действия")]
        public string CardExpiry { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Неверный CVV")]
        public string CardCvv { get; set; }

        public bool SaveCard { get; set; }

        // Альтернативные методы оплаты
        public bool UseYooKassa { get; set; }
        public bool UseSberbank { get; set; }
        public bool UseTinkoff { get; set; }
    }
}
