using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class SupportChatViewModel
    {
        public int ChatId { get; set; }

        [Required(ErrorMessage = "Тема обязательна")]
        [StringLength(255, ErrorMessage = "Тема не должна превышать 255 символов")]
        public string Subject { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<ChatMessageViewModel> Messages { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageDate { get; set; }
        public bool HasUnreadMessages { get; set; }
    }
}
