using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public int? ProductId { get; set; }

    public int? QuantityForRent { get; set; }

    public int? QuantityForSale { get; set; }

    public int? ReservedForRent { get; set; }

    public int? ReservedForSale { get; set; }

    public int? LowStockThreshold { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Product? Product { get; set; }
}
