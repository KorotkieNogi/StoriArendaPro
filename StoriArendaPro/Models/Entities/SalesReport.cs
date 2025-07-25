using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class SalesReport
{
    public int? ProductId { get; set; }

    public string? Name { get; set; }

    public string? Category { get; set; }

    public long? TotalSold { get; set; }

    public long? TotalQuantity { get; set; }

    public decimal? TotalRevenue { get; set; }
}
