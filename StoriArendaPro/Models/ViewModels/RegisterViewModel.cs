// Models/ViewModels/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace StoriArendaPro.Models.ViewModels
{
    public class RegisterViewModel
    {
        // Шаг 1: Имя, телефон и email
        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Ваше имя")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Phone(ErrorMessage = "Некорректный формат номера")]
        [Display(Name = "Номер телефона")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        // Шаг 2: Код подтверждения
        [Display(Name = "Код подтверждения")]
        public string VerificationCode { get; set; }

        // Шаг 3: Пароль
        [Required(ErrorMessage = "Обязательное поле")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать не менее 8 символов")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
    ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавные и строчные буквы, цифры и специальные символы")]
        [CustomValidation(typeof(RegisterViewModel), "ValidatePasswordStrength")]
        [CustomValidation(typeof(RegisterViewModel), "ValidateEnglishChars")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите пароль")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        // Для отслеживания текущего шага
        public int CurrentStep { get; set; } = 1;

        [JsonIgnore]
        public bool ShouldValidatePassword => CurrentStep == 3;

        public RegisterViewModel()
        {
            // Инициализация свойств для избежания null reference
            FullName = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            VerificationCode = string.Empty;
        }

        // Кастомная валидация силы пароля
        public static ValidationResult ValidatePasswordStrength(string password, ValidationContext context)
        {
            if (string.IsNullOrEmpty(password))
                return ValidationResult.Success;

            // Запрещенные простые пароли
            var weakPasswords = new[]
            {
                "12345678", "123456789", "1234567890", "qwertyui", "password",
                "abcdefgh", "87654321", "11111111", "00000000", "aaaaaaaa"
            };

            if (weakPasswords.Contains(password.ToLower()))
            {
                return new ValidationResult("Пароль слишком простой. Используйте более сложную комбинацию.");
            }

            return ValidationResult.Success;
        }

        // Кастомная валидация английских символов
        public static ValidationResult ValidateEnglishChars(string password, ValidationContext context)
        {
            if (string.IsNullOrEmpty(password))
                return ValidationResult.Success;

            // Разрешаем только английские буквы, цифры и специальные символы
            var regex = new Regex(@"^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]*$");
            if (!regex.IsMatch(password))
            {
                return new ValidationResult("Пароль может содержать только английские буквы, цифры и специальные символы.");
            }

            return ValidationResult.Success;
        }
    }
}