// Models/ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}