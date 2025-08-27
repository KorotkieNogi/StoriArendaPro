using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class User : IdentityUser<int>
{

    public string FullName { get; set; } = null!;

    public bool? IsAdmin { get; set; }

    public bool? IsActive { get; set; }

    public string? PassportSeria { get; set; }

    public string? PassportNumber { get; set; }

    public string? Propiska { get; set; }

    public string? PlaceLive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }




    public virtual ICollection<RentalOrder> RentalOrders { get; set; } = new List<RentalOrder>();

    //public virtual ICollection<SaleOrder> SaleOrders { get; set; } = new List<SaleOrder>();

    //public virtual ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
    public virtual ICollection<ShoppingCart> ShoppingCart { get; set; } = new List<ShoppingCart>();
    // Чаты, где пользователь является клиентом
    public virtual ICollection<SupportChat> UserSupportChats { get; set; }

    // Чаты, где пользователь является администратором
    public virtual ICollection<SupportChat> AdminSupportChats { get; set; }
    public virtual ICollection<PassportVerification> PassportVerifications { get; set; }

}
