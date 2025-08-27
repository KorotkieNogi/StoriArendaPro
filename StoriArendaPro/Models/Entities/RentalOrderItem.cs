using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;


public partial class RentalOrderItem
{
    public int RentalOrderItemId { get; set; }

    public int? RentalOrderId { get; set; }

    public int? RentalPriceId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string RentalType { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual RentalPrice? RentalPrice { get; set; }

    public virtual RentalOrder? RentalOrder { get; set; }
}
