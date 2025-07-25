using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public bool? IsAdmin { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    //public virtual ICollection<RentalOrder> RentalOrders { get; set; } = new List<RentalOrder>();

    //public virtual ICollection<SaleOrder> SaleOrders { get; set; } = new List<SaleOrder>();

    public virtual ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
}
