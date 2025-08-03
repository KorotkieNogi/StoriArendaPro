namespace StoriArendaPro.Models.Entities
{
    public partial class TypeProduct
    {
        public int TypeProductId { get; set; }

        public string Name { get; set; } = null!;

        public string Slug { get; set; } = null!;

        public string? Description { get; set; }

        public bool? IsForRent { get; set; }

        public bool? IsForSale { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
