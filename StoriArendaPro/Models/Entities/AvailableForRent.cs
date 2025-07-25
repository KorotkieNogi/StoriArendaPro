using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class AvailableForRent
{
    public int? ProductId { get; set; }

    public string? Name { get; set; }

    public decimal? PricePerDay { get; set; }

    public decimal? PricePerWeek { get; set; }

    public decimal? PricePerMonth { get; set; }

    public int? AvailableQuantity { get; set; }
}
