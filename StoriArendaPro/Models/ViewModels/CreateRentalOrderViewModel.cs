using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class CreateRentalOrderViewModel
    {
        [Required(ErrorMessage = "Адрес доставки обязателен")]
        public string DeliveryAddress { get; set; }

        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<CartItemViewModel> CartItems { get; set; }
    }
}
