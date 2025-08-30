using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using StoriArendaPro.Models.Entities;
using Twilio.TwiML.Voice;

namespace StoriArendaPro;

public partial class StoriArendaProContext : IdentityDbContext<User, IdentityRole<int>, int>
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

    public virtual DbSet<TypeProduct> TypeProducts { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    //public virtual DbSet<RentalRequest> RentalRequests { get; set; }
    
    public virtual DbSet<RentalOrder> RentalOrders { get; set; }
    public virtual DbSet<RentalOrderItem> RentalOrderItems { get; set; }

    public virtual DbSet<RentalPrice> RentalPrices { get; set; }

    public virtual DbSet<RentalReport> RentalReports { get; set; }

    //Не используется
    //public virtual DbSet<SaleOrder> SaleOrders { get; set; }

    //Не используется
    //public virtual DbSet<SaleOrderItem> SaleOrderItems { get; set; }

    public virtual DbSet<SalePrice> SalePrices { get; set; }

    public virtual DbSet<SalesReport> SalesReports { get; set; }

    public virtual DbSet<User> Users { get; set; }





    // Новые DbSet свойства для добавленных таблиц
    public virtual DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public virtual DbSet<SupportChat> SupportChats { get; set; }
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }
    public virtual DbSet<PassportVerification> PassportVerifications { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }




    // DbSet для Identity
    public virtual DbSet<IdentityUserClaim<int>> UserClaims { get; set; }
    public virtual DbSet<IdentityUserLogin<int>> UserLogins { get; set; }
    public virtual DbSet<IdentityUserToken<int>> UserTokens { get; set; }
    public virtual DbSet<IdentityUserRole<int>> UserRoles { get; set; }
    public virtual DbSet<IdentityRoleClaim<int>> RoleClaims { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=stori_arenda_pro;Username=postgres;Password=12873465Tam",
                options => options.CommandTimeout(120)).EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .LogTo(Console.WriteLine, LogLevel.Information); ;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // конвертер, который преобразует UTC в Local для timestamp without time zone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Utc ? v.ToLocalTime() : v,
                            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified)));
                }
            }
        }

        modelBuilder.Entity<IdentityRole<int>>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });

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
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .HasColumnName("icon");
        });

        modelBuilder.Entity<TypeProduct>(entity =>
        {
            entity.HasKey(e => e.TypeProductId).HasName("type_product_pkey");

            entity.ToTable("type_product");

            entity.HasIndex(e => e.Slug, "type_product_slug_key").IsUnique();

            entity.Property(e => e.TypeProductId).HasColumnName("type_product_id");
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

            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .HasColumnName("icon");
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

            entity.HasIndex(e => e.TypeProductId, "idx_products_type_product");

            entity.HasIndex(e => e.Slug, "idx_products_slug");

            entity.HasIndex(e => e.Sku, "products_sku_key").IsUnique();

            entity.HasIndex(e => e.Slug, "products_slug_key").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.TypeProductId).HasColumnName("type_product_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");

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

            entity.HasOne(d => d.TypeProduct).WithMany(p => p.Products)
                .HasForeignKey(d => d.TypeProductId)
                .HasConstraintName("products_type_product_id_fkey");
        });


        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_image_pkey");

            entity.ToTable("product_image");

            entity.HasIndex(e => e.Path, "idx_product_image_path");
            entity.HasIndex(e => new { e.ProductId, e.IsMain }, "idx_product_image_main")
                .HasFilter("(is_main = true)");
            entity.HasIndex(e => new { e.ProductId, e.Order }, "idx_product_image_order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Path).HasColumnName("path");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.IsMain)
                .HasDefaultValue(false)
                .HasColumnName("is_main");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("product_image_product_id_fkey");
        });



        modelBuilder.Entity<RentalOrder>(entity =>
        {
            entity.HasKey(e => e.RentalOrderId).HasName("rental_orders_pkey");

            entity.ToTable("rental_orders");

            entity.HasIndex(e => e.Status, "idx_rental_orders_status");

            entity.HasIndex(e => e.UserId, "idx_rental_orders_user");


            entity.Property(e => e.RentalOrderId).HasColumnName("rental_order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryAddress).HasColumnName("delivery_address");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'неоплаченный'::character varying")
                .HasColumnName("payment_status");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ожидает'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RentalOrders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("rental_orders_user_id_fkey");
        });

        modelBuilder.Entity<RentalOrderItem>(entity =>
        {
            entity.HasKey(e => e.RentalOrderItemId).HasName("rental_order_items_pkey");

            entity.ToTable("rental_order_items");

            entity.Property(e => e.RentalOrderItemId).HasColumnName("rental_order_item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.RentalPriceId).HasColumnName("rental_price_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.RentalOrderId).HasColumnName("rental_order_id");
            entity.Property(e => e.RentalType)
                .HasMaxLength(30)
                .HasColumnName("rental_type");
            entity.Property(e => e.Subtotal)
                .HasPrecision(10, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(10, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.RentalPrice).WithMany(p => p.RentalOrderItems)
                .HasForeignKey(d => d.RentalPriceId)
                .HasConstraintName("rental_order_items_rental_price_id_fkey");

            entity.HasOne(d => d.RentalOrder).WithMany(p => p.RentalOrderItems)
                .HasForeignKey(d => d.RentalOrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("rental_order_items_rental_order_id_fkey");
        });

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
                .HasDefaultValue(0.5)
                .HasColumnName("min_rental_days");
            entity.Property(e => e.PricePerDay)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_day");

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

        //modelBuilder.Entity<RentalRequest>(entity =>
        //{
        //    entity.HasKey(e => e.RequestId).HasName("request_id_pkey");

        //    entity.ToTable("rental_orders");

        //    entity.HasIndex(e => e.UserId, "idx_rental_requests_user");

        //    entity.HasIndex(e => e.RentalPpriceId, "idx_rental_requests_rental_price");

        //    entity.Property(e => e.RequestId).HasColumnName("request_id");
        //    entity.Property(e => e.CallDate).HasColumnName("call_date");
        //    entity.Property(e => e.CallTime).HasColumnName("call_time");
        //    entity.Property(e => e.Status)
        //        .HasMaxLength(20)
        //        .HasDefaultValueSql("'В ожидании'::character varying")
        //        .HasColumnName("status");
        //    entity.Property(e => e.AdminNotes).HasColumnName("admin_notes");
        //    entity.Property(e => e.CommentClient).HasColumnName("comment_client");
        //    entity.Property(e => e.CreatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("created_at");
        //    entity.Property(e => e.UpdatedAt)
        //        .HasDefaultValueSql("CURRENT_TIMESTAMP")
        //        .HasColumnType("timestamp without time zone")
        //        .HasColumnName("updated_at");

        //    entity.Property(e => e.UserId).HasColumnName("user_id");
        //    entity.Property(e => e.RentalPpriceId).HasColumnName("rental_price_id");

        //    entity.HasOne(d => d.RentalPrice).WithMany(p => p.RentalRequests)
        //        .HasForeignKey(d => d.RentalPpriceId)
        //        .HasConstraintName("rental_requests_rental_price_id_fkey");

        //    entity.HasOne(d => d.User).WithMany(p => p.RentalRequests)
        //        .HasForeignKey(d => d.UserId)
        //        .HasConstraintName("rental_requests_user_id_fkey");
        //});

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
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("AspNetUsers");

            //Обязательное поле
            entity.HasIndex(e => e.Email, "email_users").IsUnique();

            entity.Property(e => e.Id).HasColumnName("Id");
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
            //Обязательное поле
            entity.Property(e => e.PasswordHash)
                .HasColumnName("passwordhash");
            //Обязательное поле
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phonenumber");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            //Обязательное поле
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.PassportSeria)
                .HasMaxLength(4)
                .HasColumnName("passport_seria");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(6)
                .HasColumnName("passport_number");
            entity.Property(e => e.Propiska)
                .HasColumnName("propiska");
            entity.Property(e => e.PlaceLive)
                .HasColumnName("place_live");



            //Обязательные поля ASP.NET Core Identity
            entity.Property(e => e.UserName)
                .HasColumnName("username");
            entity.Property(e => e.AccessFailedCount)
                .HasColumnName("accessfailedcount");
            entity.Property(e => e.LockoutEnd)
                .HasColumnName("lockoutend");
            entity.Property(e => e.TwoFactorEnabled)
                .HasColumnName("twofactorenabled");
            entity.Property(e => e.PhoneNumberConfirmed)
                .HasColumnName("phonenumberconfirmed");
            entity.Property(e => e.SecurityStamp)
                .HasColumnName("securitystamp");
            entity.Property(e => e.ConcurrencyStamp)
                .HasColumnName("concurrencystamp");
            entity.Property(e => e.NormalizedEmail)
                .HasColumnName("normalizedemail");
            entity.Property(e => e.NormalizedUserName)
                .HasColumnName("normalizedusername");
            entity.Property(e => e.LockoutEnabled)
                .HasColumnName("lockoutenabled");
            entity.Property(e => e.PhoneNumberConfirmed)
                .HasColumnName("phonenumberconfirmed");
            entity.Property(e => e.EmailConfirmed)
                .HasColumnName("emailconfirmed");
            
        });




        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("shopping_cart_pkey");

            entity.ToTable("shopping_cart");

            entity.HasIndex(e => new { e.UserId, e.RentalType }, "IX_shopping_cart_user_rental_type").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_shopping_cart_user");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RentalPriceId).HasColumnName("rental_price_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.RentalType)
                .HasMaxLength(30)
                .HasColumnName("rental_type");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("unit_price");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User)
                .WithMany(u => u.ShoppingCart)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("shopping_cart_user_id_fkey");

            entity.HasOne(d => d.RentalPrice)
                .WithMany(u => u.ShoppingCart)
                .HasForeignKey(d => d.RentalPriceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("shopping_cart_rental_price_id_fkey");
        });

        // Конфигурация SupportChat
        modelBuilder.Entity<SupportChat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("support_chat_pkey");
            entity.ToTable("support_chat");

            // Индексы
            entity.HasIndex(e => e.Status, "idx_support_chat_status");
            entity.HasIndex(e => e.UserId, "idx_support_chat_user");
            entity.HasIndex(e => e.AdminId, "idx_support_chat_admin");

            // Свойства
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("открыто")
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            // Отношение с User (клиент)
            entity.HasOne(d => d.User)
                .WithMany(u => u.UserSupportChats) // Указываем конкретное навигационное свойство
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("support_chat_user_id_fkey");

            // Отношение с Admin (администратор)
            entity.HasOne(d => d.Admin)
                .WithMany(u => u.AdminSupportChats) // Указываем конкретное навигационное свойство
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false) // Делаем отношение необязательным
                .HasConstraintName("support_chat_admin_id_fkey");
        });

        // Конфигурация ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("chat_messages_pkey");

            entity.ToTable("chat_messages");

            entity.HasIndex(e => e.ChatId, "idx_chat_messages_chat");
            entity.HasIndex(e => e.SenderId, "idx_chat_messages_sender");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.SupportChat)
                .WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ChatId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("chat_messages_chat_id_fkey");

            entity.HasOne(d => d.Sender)
                .WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("chat_messages_sender_id_fkey");
        });

        // Конфигурация PassportVerification
        modelBuilder.Entity<PassportVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("passport_verification_pkey");

            entity.ToTable("passport_verification");

            entity.HasIndex(e => e.Status, "idx_passport_verification_status");
            entity.HasIndex(e => e.UserId, "idx_passport_verification_user").IsUnique();

            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PassportSeria)
                .HasMaxLength(4)
                .HasColumnName("passport_seria");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(6)
                .HasColumnName("passport_number");
            entity.Property(e => e.IssuedBy).HasColumnName("issued_by");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");
            entity.Property(e => e.Propiska).HasColumnName("propiska");
            entity.Property(e => e.PlaceLive).HasColumnName("place_live");
            entity.Property(e => e.PassportPhotoFront).HasColumnName("passport_photo_front");
            entity.Property(e => e.PassportPhotoBack).HasColumnName("passport_photo_back");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ожидает рассмотрения")
                .HasColumnName("status");
            entity.Property(e => e.AdminNotes).HasColumnName("admin_notes");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User)
                .WithMany(u => u.PassportVerifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("passport_verification_user_id_fkey");

            
        });

        // Конфигурация Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => new { e.OrderId, e.OrderType }, "idx_payments_order");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderType)
                .HasMaxLength(20)
                .HasColumnName("order_type");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("amount");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("RUB")
                .HasColumnName("currency");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("ожидает")
                .HasColumnName("payment_status");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(255)
                .HasColumnName("transaction_id");
            entity.Property(e => e.PaymentData)
                .HasColumnType("jsonb")
                .HasColumnName("payment_data");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
        });



        // Конфигурация для Identity таблиц
        modelBuilder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.ToTable("AspNetUserClaims");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.ClaimType).HasColumnName("ClaimType");
            entity.Property(e => e.ClaimValue).HasColumnName("ClaimValue");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.ToTable("AspNetUserLogins");
            entity.Property(e => e.LoginProvider).HasColumnName("LoginProvider");
            entity.Property(e => e.ProviderKey).HasColumnName("ProviderKey");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("ProviderDisplayName");
            entity.Property(e => e.UserId).HasColumnName("UserId");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.ToTable("AspNetUserTokens");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.LoginProvider).HasColumnName("LoginProvider");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Value).HasColumnName("Value");
        });

        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.ToTable("AspNetUserRoles");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.RoleId).HasColumnName("RoleId");
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(entity =>
        {
            entity.ToTable("AspNetRoleClaims");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.RoleId).HasColumnName("RoleId");
            entity.Property(e => e.ClaimType).HasColumnName("ClaimType");
            entity.Property(e => e.ClaimValue).HasColumnName("ClaimValue");
        });



        OnModelCreatingPartial(modelBuilder);
    }

    // Обработка закрытия контекста
    public override void Dispose()
    {
        Console.WriteLine("DbContext disposing...");
        base.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        Console.WriteLine("DbContext disposing async...");
        await base.DisposeAsync();
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
