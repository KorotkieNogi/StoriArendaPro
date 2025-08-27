using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class RentalPrice
{
    public int RentalPriceId { get; set; }

    public int? ProductId { get; set; }

    public decimal? PricePerDay { get; set; }

    public decimal? Deposit { get; set; }

    public int? MinRentalDays { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Product? Product { get; set; }

    public virtual ICollection<RentalOrderItem> RentalOrderItems { get; set; } = new List<RentalOrderItem>();

    //public virtual ICollection<RentalRequest> RentalRequests { get; set; } = new List<RentalRequest>();
    public virtual ICollection<ShoppingCart> ShoppingCart { get; set; } = new List<ShoppingCart>();
}
