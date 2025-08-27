using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class PassportDataViewModel
    {
        [Required(ErrorMessage = "Серия паспорта обязательна")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Серия паспорта должна содержать 4 цифры")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Серия паспорта должна содержать только цифры")]
        public string PassportSeria { get; set; }

        [Required(ErrorMessage = "Номер паспорта обязателен")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Номер паспорта должен содержать 6 цифр")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Номер паспорта должен содержать только цифры")]
        public string PassportNumber { get; set; }

        [Display(Name = "Кем выдан")]
        public string IssuedBy { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата выдачи")]
        public DateTime? IssueDate { get; set; }

        [Required(ErrorMessage = "Прописка обязательна")]
        [Display(Name = "Прописка")]
        public string Propiska { get; set; }

        [Required(ErrorMessage = "Место проживания обязательно")]
        [Display(Name = "Место проживания")]
        public string PlaceLive { get; set; }

        // Для загрузки фотографий
        [Display(Name = "Фото паспорта (лицевая сторона)")]
        public IFormFile PassportPhotoFront { get; set; }

        [Display(Name = "Фото паспорта (обратная сторона)")]
        public IFormFile PassportPhotoBack { get; set; }
    }
}
