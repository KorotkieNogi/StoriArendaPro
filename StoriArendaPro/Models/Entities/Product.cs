using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class Product
{
    public int ProductId { get; set; }

    public int? CategoryId { get; set; }

    public int? TypeProductId { get; set; }

    public string? Sku { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string? Specifications { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual TypeProduct? TypeProduct { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    

    public virtual ICollection<RentalPrice> RentalPrices { get; set; } = new List<RentalPrice>();

    //Не испульзуется
    //public virtual ICollection<SaleOrderItem> SaleOrderItems { get; set; } = new List<SaleOrderItem>();

    public virtual ICollection<SalePrice> SalePrices { get; set; } = new List<SalePrice>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
