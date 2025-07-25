using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using StoriArendaPro.Models.Entities;

namespace StoriArendaPro;

public partial class StoriArendaProContext : DbContext
{
    public StoriArendaProContext()
    {
    }

    public StoriArendaProContext(DbContextOptions<StoriArendaProContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AvailableForRent> AvailableForRents { get; set; }

    public virtual DbSet<AvailableForSale> AvailableForSales { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    //Не используется
    //public virtual DbSet<RentalOrder> RentalOrders { get; set; }

    //Не используется
    //public virtual DbSet<RentalOrderItem> RentalOrderItems { get; set; }

    public virtual DbSet<RentalPrice> RentalPrices { get; set; }

    public virtual DbSet<RentalReport> RentalReports { get; set; }

    //Не используется
    //public virtual DbSet<SaleOrder> SaleOrders { get; set; }

    //Не используется
    //public virtual DbSet<SaleOrderItem> SaleOrderItems { get; set; }

    public virtual DbSet<SalePrice> SalePrices { get; set; }

    public virtual DbSet<SalesReport> SalesReports { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=stori_arenda_pro;Username=postgres;Password=12873465Tam");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailableForRent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("available_for_rent");

            entity.Property(e => e.AvailableQuantity).HasColumnName("available_quantity");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PricePerDay)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_day");
            entity.Property(e => e.PricePerMonth)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_month");
            entity.Property(e => e.PricePerWeek)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_week");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
        });

        modelBuilder.Entity<AvailableForSale>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("available_for_sale");

            entity.Property(e => e.AvailableQuantity).HasColumnName("available_quantity");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.OldPrice)
                .HasPrecision(10, 2)
                .HasColumnName("old_price");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.HasIndex(e => e.Slug, "categories_slug_key").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsForRent)
                .HasDefaultValue(true)
                .HasColumnName("is_for_rent");
            entity.Property(e => e.IsForSale)
                .HasDefaultValue(false)
                .HasColumnName("is_for_sale");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("inventory_pkey");

            entity.ToTable("inventory");

            entity.HasIndex(e => e.ProductId, "idx_inventory_low_stock").HasFilter("((quantity_for_rent + quantity_for_sale) < low_stock_threshold)");

            entity.HasIndex(e => e.ProductId, "idx_inventory_product");

            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.LowStockThreshold)
                .HasDefaultValue(3)
                .HasColumnName("low_stock_threshold");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QuantityForRent)
                .HasDefaultValue(0)
                .HasColumnName("quantity_for_rent");
            entity.Property(e => e.QuantityForSale)
                .HasDefaultValue(0)
                .HasColumnName("quantity_for_sale");
            entity.Property(e => e.ReservedForRent)
                .HasDefaultValue(0)
                .HasColumnName("reserved_for_rent");
            entity.Property(e => e.ReservedForSale)
                .HasDefaultValue(0)
                .HasColumnName("reserved_for_sale");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("inventory_product_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.ToTable("products");

            entity.HasIndex(e => e.IsActive, "idx_products_active").HasFilter("(is_active = true)");

            entity.HasIndex(e => e.CategoryId, "idx_products_category");

            entity.HasIndex(e => e.Slug, "idx_products_slug");

            entity.HasIndex(e => e.Sku, "products_sku_key").IsUnique();

            entity.HasIndex(e => e.Slug, "products_slug_key").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(255)
                .HasColumnName("image_path");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ShortDescription).HasColumnName("short_description");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("sku");
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .HasColumnName("slug");
            entity.Property(e => e.Specifications)
                .HasColumnType("jsonb")
                .HasColumnName("specifications");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("products_category_id_fkey");
        });

        //modelBuilder.Entity<RentalOrder>(entity =>
        //{
        //    entity.HasKey(e => e.RentalOrderId).HasName("rental_orders_pkey");

        //    entity.ToTable("rental_orders");

        //    entity.HasIndex(e => e.Status, "idx_rental_orders_status");

        //    entity.HasIndex(e => e.UserId, "idx_rental_orders_user");

        //    entity.HasIndex(e => e.OrderNumber, "rental_orders_order_number_key").IsUnique();

        //    entity.Property(e => e.RentalOrderId).HasColumnName("rental_order_id");
        //    entity.Property(e => e.CreatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("created_at");
        //    entity.Property(e => e.DeliveryAddress).HasColumnName("delivery_address");
        //    entity.Property(e => e.EndDate).HasColumnName("end_date");
        //    entity.Property(e => e.Notes).HasColumnName("notes");
        //    entity.Property(e => e.OrderNumber)
        //        .HasMaxLength(20)
        //        .HasColumnName("order_number");
        //    entity.Property(e => e.PaymentStatus)
        //        .HasMaxLength(20)
        //        .HasDefaultValueSql("'unpaid'::character varying")
        //        .HasColumnName("payment_status");
        //    entity.Property(e => e.StartDate).HasColumnName("start_date");
        //    entity.Property(e => e.Status)
        //        .HasMaxLength(20)
        //        .HasDefaultValueSql("'pending'::character varying")
        //        .HasColumnName("status");
        //    entity.Property(e => e.TotalAmount)
        //        .HasPrecision(10, 2)
        //        .HasColumnName("total_amount");
        //    entity.Property(e => e.UpdatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("updated_at");
        //    entity.Property(e => e.UserId).HasColumnName("user_id");

        //    entity.HasOne(d => d.User).WithMany(p => p.RentalOrders)
        //        .HasForeignKey(d => d.UserId)
        //        .HasConstraintName("rental_orders_user_id_fkey");
        //});

        //modelBuilder.Entity<RentalOrderItem>(entity =>
        //{
        //    entity.HasKey(e => e.RentalOrderItemId).HasName("rental_order_items_pkey");

        //    entity.ToTable("rental_order_items");

        //    entity.Property(e => e.RentalOrderItemId).HasColumnName("rental_order_item_id");
        //    entity.Property(e => e.CreatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("created_at");
        //    entity.Property(e => e.ProductId).HasColumnName("product_id");
        //    entity.Property(e => e.Quantity).HasColumnName("quantity");
        //    entity.Property(e => e.RentalOrderId).HasColumnName("rental_order_id");
        //    entity.Property(e => e.RentalType)
        //        .HasMaxLength(10)
        //        .HasColumnName("rental_type");
        //    entity.Property(e => e.Subtotal)
        //        .HasPrecision(10, 2)
        //        .HasColumnName("subtotal");
        //    entity.Property(e => e.UnitPrice)
        //        .HasPrecision(10, 2)
        //        .HasColumnName("unit_price");

        //    entity.HasOne(d => d.Product).WithMany(p => p.RentalOrderItems)
        //        .HasForeignKey(d => d.ProductId)
        //        .HasConstraintName("rental_order_items_product_id_fkey");

        //    entity.HasOne(d => d.RentalOrder).WithMany(p => p.RentalOrderItems)
        //        .HasForeignKey(d => d.RentalOrderId)
        //        .OnDelete(DeleteBehavior.Cascade)
        //        .HasConstraintName("rental_order_items_rental_order_id_fkey");
        //});

        modelBuilder.Entity<RentalPrice>(entity =>
        {
            entity.HasKey(e => e.RentalPriceId).HasName("rental_prices_pkey");

            entity.ToTable("rental_prices");

            entity.HasIndex(e => e.ProductId, "idx_rental_prices_product");

            entity.Property(e => e.RentalPriceId).HasColumnName("rental_price_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Deposit)
                .HasPrecision(10, 2)
                .HasColumnName("deposit");
            entity.Property(e => e.MinRentalDays)
                .HasDefaultValue(1)
                .HasColumnName("min_rental_days");
            entity.Property(e => e.PricePerDay)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_day");
            entity.Property(e => e.PricePerMonth)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_month");
            entity.Property(e => e.PricePerWeek)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_week");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithMany(p => p.RentalPrices)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("rental_prices_product_id_fkey");
        });

        modelBuilder.Entity<RentalReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("rental_report");

            entity.Property(e => e.AvgRentalDays).HasColumnName("avg_rental_days");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TotalRentals).HasColumnName("total_rentals");
            entity.Property(e => e.TotalRevenue).HasColumnName("total_revenue");
            entity.Property(e => e.TotalUnits).HasColumnName("total_units");
        });

        modelBuilder.Entity<RentalRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("request_id_pkey");

            entity.ToTable("rental_orders");

            entity.HasIndex(e => e.UserId, "idx_rental_requests_user");

            entity.HasIndex(e => e.RentalPpriceId, "idx_rental_requests_rental_price");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.CallDate).HasColumnName("call_date");
            entity.Property(e => e.CallTime).HasColumnName("call_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'В ожидании'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.AdminNotes).HasColumnName("admin_notes");
            entity.Property(e => e.CommentClient).HasColumnName("comment_client");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RentalPpriceId).HasColumnName("rental_price_id");

            entity.HasOne(d => d.RentalPrice).WithMany(p => p.RentalRequests)
                .HasForeignKey(d => d.RentalPpriceId)
                .HasConstraintName("rental_requests_rental_price_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.RentalRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("rental_requests_user_id_fkey");
        });

        //modelBuilder.Entity<SaleOrder>(entity =>
        //{
        //    entity.HasKey(e => e.SaleOrderId).HasName("sale_orders_pkey");

        //    entity.ToTable("sale_orders");

        //    entity.HasIndex(e => e.Status, "idx_sale_orders_status");

        //    entity.HasIndex(e => e.UserId, "idx_sale_orders_user");

        //    entity.HasIndex(e => e.OrderNumber, "sale_orders_order_number_key").IsUnique();

        //    entity.Property(e => e.SaleOrderId).HasColumnName("sale_order_id");
        //    entity.Property(e => e.ContactPhone)
        //        .HasMaxLength(20)
        //        .HasColumnName("contact_phone");
        //    entity.Property(e => e.DiscountAmount)
        //        .HasPrecision(10, 2)
        //        .HasDefaultValueSql("0")
        //        .HasColumnName("discount_amount");
        //    entity.Property(e => e.Notes).HasColumnName("notes");
        //    entity.Property(e => e.OrderDate)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("order_date");
        //    entity.Property(e => e.OrderNumber)
        //        .HasMaxLength(20)
        //        .HasColumnName("order_number");
        //    entity.Property(e => e.PaymentStatus)
        //        .HasMaxLength(20)
        //        .HasDefaultValueSql("'unpaid'::character varying")
        //        .HasColumnName("payment_status");
        //    entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address");
        //    entity.Property(e => e.Status)
        //        .HasMaxLength(20)
        //        .HasDefaultValueSql("'processing'::character varying")
        //        .HasColumnName("status");
        //    entity.Property(e => e.TotalAmount)
        //        .HasPrecision(10, 2)
        //        .HasColumnName("total_amount");
        //    entity.Property(e => e.UpdatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("updated_at");
        //    entity.Property(e => e.UserId).HasColumnName("user_id");

        //    entity.HasOne(d => d.User).WithMany(p => p.SaleOrders)
        //        .HasForeignKey(d => d.UserId)
        //        .HasConstraintName("sale_orders_user_id_fkey");
        //});

        //modelBuilder.Entity<SaleOrderItem>(entity =>
        //{
        //    entity.HasKey(e => e.SaleOrderItemId).HasName("sale_order_items_pkey");

        //    entity.ToTable("sale_order_items");

        //    entity.Property(e => e.SaleOrderItemId).HasColumnName("sale_order_item_id");
        //    entity.Property(e => e.CreatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("created_at");
        //    entity.Property(e => e.DiscountPercent)
        //        .HasDefaultValue(0)
        //        .HasColumnName("discount_percent");
        //    entity.Property(e => e.ProductId).HasColumnName("product_id");
        //    entity.Property(e => e.Quantity).HasColumnName("quantity");
        //    entity.Property(e => e.SaleOrderId).HasColumnName("sale_order_id");
        //    entity.Property(e => e.Subtotal)
        //        .HasPrecision(10, 2)
        //        .HasColumnName("subtotal");
        //    entity.Property(e => e.UnitPrice)
        //        .HasPrecision(10, 2)
        //        .HasColumnName("unit_price");

        //    entity.HasOne(d => d.Product).WithMany(p => p.SaleOrderItems)
        //        .HasForeignKey(d => d.ProductId)
        //        .HasConstraintName("sale_order_items_product_id_fkey");

        //    entity.HasOne(d => d.SaleOrder).WithMany(p => p.SaleOrderItems)
        //        .HasForeignKey(d => d.SaleOrderId)
        //        .OnDelete(DeleteBehavior.Cascade)
        //        .HasConstraintName("sale_order_items_sale_order_id_fkey");
        //});

        modelBuilder.Entity<SalePrice>(entity =>
        {
            entity.HasKey(e => e.SalePriceId).HasName("sale_prices_pkey");

            entity.ToTable("sale_prices");

            entity.HasIndex(e => e.IsOnSale, "idx_sale_prices_active").HasFilter("(is_on_sale = true)");

            entity.HasIndex(e => e.ProductId, "idx_sale_prices_product");

            entity.Property(e => e.SalePriceId).HasColumnName("sale_price_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'RUB'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.IsOnSale)
                .HasDefaultValue(false)
                .HasColumnName("is_on_sale");
            entity.Property(e => e.OldPrice)
                .HasPrecision(10, 2)
                .HasColumnName("old_price");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithMany(p => p.SalePrices)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sale_prices_product_id_fkey");
        });

        modelBuilder.Entity<SalesReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("sales_report");

            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
            entity.Property(e => e.TotalRevenue).HasColumnName("total_revenue");
            entity.Property(e => e.TotalSold).HasColumnName("total_sold");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsAdmin)
                .HasDefaultValue(false)
                .HasColumnName("is_admin");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
