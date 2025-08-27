// Models/ViewModels/UserProfileViewModel.cs
using StoriArendaPro.Models.Entities;

namespace StoriArendaPro.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<RentalOrder> RentalOrders { get; set; }
        public List<SupportChat> SupportChats { get; set; }
        public List<ShoppingCart> CartItems { get; set; }
        public PassportVerification PassportVerification { get; set; }
        public decimal CartTotal { get; set; }
        public int CartItemsCount { get; set; }
        public bool IsPassportVerified => PassportVerification?.Status == "approved";

        // Статистика
        public int ActiveRentals { get; set; }
        public int CompletedRentals { get; set; }
        public decimal TotalSpent { get; set; }
    }
}