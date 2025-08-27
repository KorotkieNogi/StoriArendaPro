using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class RentalRequestViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; } // Для отображения

        [Required(ErrorMessage = "Укажите дату")]
        [Display(Name = "Дата звонка")]
        public DateOnly CallDate { get; set; }

        [Required(ErrorMessage = "Укажите время")]
        [Display(Name = "Время звонка")]
        public TimeSpan CallTime { get; set; }

        [Display(Name = "Комментарий")]
        public string? Comment { get; set; }
    }
}
