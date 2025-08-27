using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class CreateChatViewModel
    {
        [Required(ErrorMessage = "Тема обязательна")]
        [StringLength(255, ErrorMessage = "Тема не должна превышать 255 символов")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Сообщение обязательно")]
        [StringLength(1000, ErrorMessage = "Сообщение не должно превышать 1000 символов")]
        public string Message { get; set; }
    }
}
