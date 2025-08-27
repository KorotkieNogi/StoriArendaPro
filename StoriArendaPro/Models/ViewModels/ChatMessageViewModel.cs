using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class ChatMessageViewModel
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public bool IsAdmin { get; set; }

        [Required(ErrorMessage = "Сообщение не может быть пустым")]
        [StringLength(1000, ErrorMessage = "Сообщение не должно превышать 1000 символов")]
        public string MessageText { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
    }
}
