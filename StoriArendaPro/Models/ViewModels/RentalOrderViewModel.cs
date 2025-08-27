using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class RentalOrderViewModel
    {
        public int RentalOrderId { get; set; }
        public string OrderNumber { get; set; }

        [Required(ErrorMessage = "Дата начала аренды обязательна")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Дата окончания аренды обязательна")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Адрес доставки обязателен")]
        public string DeliveryAddress { get; set; }

        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<RentalOrderItemViewModel> Items { get; set; }
        public int RentalDays { get; set; }
        public bool CanCancel => Status == "ожидает" || Status == "подтверждено";
    }
}
