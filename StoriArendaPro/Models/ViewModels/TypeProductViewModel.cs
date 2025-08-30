using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class TypeProductViewModel
    {
        public int TypeProductId { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Название типа")]
        [StringLength(100, ErrorMessage = "Название не должно превышать 100 символов")]
        public string Name { get; set; } = null!;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Иконка")]
        public string? Icon { get; set; }

        [Display(Name = "Для аренды")]
        public bool IsForRent { get; set; } = true;

        [Display(Name = "Для продажи")]
        public bool IsForSale { get; set; } = false;
    }
}
