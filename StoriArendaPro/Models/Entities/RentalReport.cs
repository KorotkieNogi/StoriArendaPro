using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class RentalReport
{
    public int? ProductId { get; set; }

    public string? Name { get; set; }

    public string? Category { get; set; }

    public long? TotalRentals { get; set; }

    public long? TotalUnits { get; set; }

    public decimal? TotalRevenue { get; set; }

    public decimal? AvgRentalDays { get; set; }
}
