using StoriArendaPro.Models.Entities;

namespace StoriArendaPro.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int PendingRequests { get; set; }
        public int PendingVerifications { get; set; }
        public int ActiveRentals { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public List<PassportVerification> Verifications { get; set; }
        public List<RentalOrder> RecentOrders { get; set; }
        public List<SupportChat> ActiveChats { get; set; }
    }
}
