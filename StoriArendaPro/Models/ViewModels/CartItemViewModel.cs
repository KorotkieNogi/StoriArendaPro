using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class CartItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int RentalPriceId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1")]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public string RentalType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше 0")]
        public decimal Subtotal { get; set; }

        // Для отображения
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int RentalDays { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}
