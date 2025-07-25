using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class AvailableForSale
{
    public int? ProductId { get; set; }

    public string? Name { get; set; }

    public decimal? Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int? AvailableQuantity { get; set; }
}
