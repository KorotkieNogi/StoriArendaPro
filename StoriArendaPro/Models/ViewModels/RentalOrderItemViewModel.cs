using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class RentalOrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Тип аренды обязателен")]
        public string RentalType { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Сумма обязательна")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
        public decimal Subtotal { get; set; }

        public int RentalDays { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}
