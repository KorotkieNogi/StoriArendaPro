using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class SalePrice
{
    public int SalePriceId { get; set; }

    public int? ProductId { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public string? Currency { get; set; }

    public bool? IsOnSale { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Product? Product { get; set; }
}
