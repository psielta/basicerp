using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace EntityFrameworkProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Session> Sessions { get; set; }

        public DbSet<ProductTemplate> ProductTemplates { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
        public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductTemplateCategory> ProductTemplateCategories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<StockLocation> StockLocations { get; set; }
        public DbSet<StockBalance> StockBalances { get; set; }
        public DbSet<StockLedger> StockLedgers { get; set; }
        public DbSet<StockReservation> StockReservations { get; set; }
        public DbSet<StockOutboxEvent> StockOutboxEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("citext");

            ConfigureOrganization(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureMembership(modelBuilder);
            ConfigureAccount(modelBuilder);
            ConfigureSession(modelBuilder);

            ConfigureProductTemplate(modelBuilder);
            ConfigureProductVariant(modelBuilder);
            ConfigureProductAttribute(modelBuilder);
            ConfigureProductAttributeValue(modelBuilder);
            ConfigureProductVariantAttributeValue(modelBuilder);
            ConfigureCategory(modelBuilder);
            ConfigureProductTemplateCategory(modelBuilder);
            ConfigureProductImage(modelBuilder);
            ConfigureStockLocation(modelBuilder);
            ConfigureStockBalance(modelBuilder);
            ConfigureStockLedger(modelBuilder);
            ConfigureStockReservation(modelBuilder);
            ConfigureStockOutboxEvent(modelBuilder);
        }

        private static void ConfigureOrganization(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("organization");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasColumnName("slug")
                    .HasColumnType("citext");

                entity.Property(e => e.Logo)
                    .HasColumnName("logo")
                    .HasColumnType("text");

                entity.Property(e => e.Metadata)
                    .IsRequired()
                    .HasColumnName("metadata")
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasIndex(e => e.Slug)
                    .IsUnique()
                    .HasName("organization_slug_unique");
            });
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("email")
                    .HasColumnType("citext");

                entity.Property(e => e.EmailVerified)
                    .IsRequired()
                    .HasColumnName("email_verified")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.Image)
                    .HasColumnName("image")
                    .HasColumnType("text");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasColumnName("role")
                    .HasColumnType("text")
                    .HasDefaultValue("user");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Metadata)
                    .IsRequired()
                    .HasColumnName("metadata")
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb");

                entity.Property(e => e.TwoFactorEnabled)
                    .IsRequired()
                    .HasColumnName("two_factor_enabled")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasName("user_email_unique");
            });
        }

        private static void ConfigureMembership(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Membership>(entity =>
            {
                entity.ToTable("memberships");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasColumnName("role")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.TeamId)
                    .HasColumnName("team_id")
                    .HasColumnType("uuid");

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Memberships)
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("memberships_organization_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Memberships)
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("memberships_user_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.UserId })
                    .IsUnique()
                    .HasName("memberships_org_user_unique");

                entity.HasIndex(e => e.UserId)
                    .HasName("memberships_user_id_idx");

                entity.HasIndex(e => e.OrganizationId)
                    .HasName("memberships_organization_id_idx");
            });
        }

        private static void ConfigureAccount(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("account");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.AccountId)
                    .IsRequired()
                    .HasColumnName("account_id")
                    .HasColumnType("text");

                entity.Property(e => e.ProviderId)
                    .IsRequired()
                    .HasColumnName("provider_id")
                    .HasColumnType("text");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.AccessToken)
                    .HasColumnName("access_token")
                    .HasColumnType("text");

                entity.Property(e => e.RefreshToken)
                    .HasColumnName("refresh_token")
                    .HasColumnType("text");

                entity.Property(e => e.IdToken)
                    .HasColumnName("id_token")
                    .HasColumnType("text");

                entity.Property(e => e.AccessTokenExpiresAt)
                    .HasColumnName("access_token_expires_at")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.RefreshTokenExpiresAt)
                    .HasColumnName("refresh_token_expires_at")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Scope)
                    .HasColumnName("scope")
                    .HasColumnType("text");

                entity.Property(e => e.Password)
                    .HasColumnName("password")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Accounts)
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("account_user_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ProviderId, e.AccountId })
                    .IsUnique()
                    .HasName("account_provider_account_unique");

                entity.HasIndex(e => e.UserId)
                    .HasName("account_user_id_idx");
            });
        }

        private static void ConfigureSession(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("session");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.ExpiresAt)
                    .IsRequired()
                    .HasColumnName("expires_at")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasColumnName("token")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.IpAddress)
                    .HasColumnName("ip_address")
                    .HasColumnType("inet");

                entity.Property(e => e.UserAgent)
                    .HasColumnName("user_agent")
                    .HasColumnType("text");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.ActiveOrganizationId)
                    .HasColumnName("active_organization_id")
                    .HasColumnType("uuid");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("session_user_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ActiveOrganization)
                    .WithMany(o => o.ActiveSessions)
                    .HasForeignKey(e => e.ActiveOrganizationId)
                    .HasConstraintName("session_active_org_fk")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Token)
                    .IsUnique()
                    .HasName("session_token_unique");

                entity.HasIndex(e => e.UserId)
                    .HasName("session_user_id_idx");

                entity.HasIndex(e => e.ExpiresAt)
                    .HasName("session_expires_at_idx");
            });
        }

        private static void ConfigureProductTemplate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductTemplate>(entity =>
            {
                entity.ToTable("product_template");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasColumnName("slug")
                    .HasColumnType("citext");

                entity.Property(e => e.ProductType)
                    .IsRequired()
                    .HasColumnName("product_type")
                    .HasColumnType("smallint")
                    .HasDefaultValue((short)0);

                entity.Property(e => e.Brand)
                    .HasColumnName("brand")
                    .HasColumnType("text");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("text");

                entity.Property(e => e.WarrantyMonths)
                    .HasColumnName("warranty_months")
                    .HasColumnType("integer");

                entity.Property(e => e.IsService)
                    .IsRequired()
                    .HasColumnName("is_service")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.IsRental)
                    .IsRequired()
                    .HasColumnName("is_rental")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.HasDelivery)
                    .IsRequired()
                    .HasColumnName("has_delivery")
                    .HasColumnType("boolean")
                    .HasDefaultValue(true);

                entity.Property(e => e.Ncm)
                    .HasColumnName("ncm")
                    .HasColumnType("text");

                entity.Property(e => e.Nbs)
                    .HasColumnName("nbs")
                    .HasColumnType("text");

                entity.Property(e => e.FreightMode)
                    .HasColumnName("freight_mode")
                    .HasColumnType("smallint");

                entity.Property(e => e.AggregatorCode)
                    .HasColumnName("aggregator_code")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedByUserId)
                    .HasColumnName("created_by_user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.DeletedAt)
                    .HasColumnName("deleted_at")
                    .HasColumnType("timestamp with time zone");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("product_template_organization_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .HasConstraintName("product_template_created_by_user_fk")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                    .IsUnique()
                    .HasName("ux_product_template_org_slug");

                entity.HasIndex(e => new { e.OrganizationId, e.AggregatorCode })
                    .IsUnique()
                    .HasFilter("aggregator_code IS NOT NULL")
                    .HasName("ux_product_template_org_aggregator");

                entity.HasIndex(e => e.OrganizationId)
                    .HasName("ix_product_template_org");
            });
        }

        private static void ConfigureProductVariant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.ToTable("product_variant");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.ProductTemplateId)
                    .IsRequired()
                    .HasColumnName("product_template_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Sku)
                    .IsRequired()
                    .HasColumnName("sku")
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("text");

                entity.Property(e => e.Cost)
                    .HasColumnName("cost")
                    .HasColumnType("numeric(15,2)");

                entity.Property(e => e.Weight)
                    .HasColumnName("weight")
                    .HasColumnType("numeric(12,4)");

                entity.Property(e => e.Height)
                    .HasColumnName("height")
                    .HasColumnType("numeric(12,2)");

                entity.Property(e => e.Width)
                    .HasColumnName("width")
                    .HasColumnType("numeric(12,2)");

                entity.Property(e => e.Length)
                    .HasColumnName("length")
                    .HasColumnType("numeric(12,2)");

                entity.Property(e => e.Barcode)
                    .HasColumnName("barcode")
                    .HasColumnType("text");

                entity.Property(e => e.RawVariationDescription)
                    .HasColumnName("raw_variation_description")
                    .HasColumnType("text");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasColumnName("is_active")
                    .HasColumnType("boolean")
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.DeletedAt)
                    .HasColumnName("deleted_at")
                    .HasColumnType("timestamp with time zone");

                entity.HasOne(e => e.ProductTemplate)
                    .WithMany(t => t.Variants)
                    .HasForeignKey(e => e.ProductTemplateId)
                    .HasConstraintName("product_variant_template_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("product_variant_organization_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.Sku })
                    .IsUnique()
                    .HasName("ux_product_variant_org_sku");

                entity.HasIndex(e => e.ProductTemplateId)
                    .HasName("ix_product_variant_product_template_id");
            });
        }

        private static void ConfigureProductAttribute(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductAttribute>(entity =>
            {
                entity.ToTable("product_attribute");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasColumnName("code")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("product_attribute_organization_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.Code })
                    .IsUnique()
                    .HasName("ux_product_attribute_org_code");

                entity.HasIndex(e => e.OrganizationId)
                    .HasName("ix_product_attribute_org");
            });
        }

        private static void ConfigureProductAttributeValue(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductAttributeValue>(entity =>
            {
                entity.ToTable("product_attribute_value");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.AttributeId)
                    .IsRequired()
                    .HasColumnName("attribute_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasColumnName("value")
                    .HasColumnType("text");

                entity.Property(e => e.SortOrder)
                    .IsRequired()
                    .HasColumnName("sort_order")
                    .HasColumnType("integer")
                    .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.Attribute)
                    .WithMany(a => a.Values)
                    .HasForeignKey(e => e.AttributeId)
                    .HasConstraintName("product_attribute_value_attribute_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.AttributeId, e.Value })
                    .IsUnique()
                    .HasName("ux_product_attribute_value");

                entity.HasIndex(e => e.AttributeId)
                    .HasName("ix_product_attribute_value_attribute_id");
            });
        }

        private static void ConfigureProductVariantAttributeValue(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
            {
                entity.ToTable("product_variant_attribute_value");

                entity.HasKey(e => new { e.VariantId, e.AttributeValueId });

                entity.Property(e => e.VariantId)
                    .HasColumnName("variant_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.AttributeValueId)
                    .HasColumnName("attribute_value_id")
                    .HasColumnType("uuid");

                entity.HasOne(e => e.Variant)
                    .WithMany(v => v.AttributeValues)
                    .HasForeignKey(e => e.VariantId)
                    .HasConstraintName("product_variant_attribute_value_variant_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AttributeValue)
                    .WithMany(v => v.VariantValues)
                    .HasForeignKey(e => e.AttributeValueId)
                    .HasConstraintName("product_variant_attribute_value_value_fk")
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("category");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Slug)
                    .IsRequired()
                    .HasColumnName("slug")
                    .HasColumnType("citext");

                entity.Property(e => e.Path)
                    .HasColumnName("path")
                    .HasColumnType("text");

                entity.Property(e => e.ParentId)
                    .HasColumnName("parent_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("category_organization_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Parent)
                    .WithMany(c => c.Children)
                    .HasForeignKey(e => e.ParentId)
                    .HasConstraintName("category_parent_fk")
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.OrganizationId, e.Slug })
                    .IsUnique()
                    .HasName("ux_category_org_slug");

                entity.HasIndex(e => new { e.OrganizationId, e.ParentId })
                    .HasName("ix_category_org_parent");

                entity.HasIndex(e => e.OrganizationId)
                    .HasName("ix_category_org");
            });
        }

        private static void ConfigureProductTemplateCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductTemplateCategory>(entity =>
            {
                entity.ToTable("product_template_category");

                entity.HasKey(e => new { e.ProductTemplateId, e.CategoryId });

                entity.Property(e => e.ProductTemplateId)
                    .HasColumnName("product_template_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.CategoryId)
                    .HasColumnName("category_id")
                    .HasColumnType("uuid");

                entity.HasOne(e => e.ProductTemplate)
                    .WithMany(t => t.Categories)
                    .HasForeignKey(e => e.ProductTemplateId)
                    .HasConstraintName("product_template_category_template_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.ProductTemplates)
                    .HasForeignKey(e => e.CategoryId)
                    .HasConstraintName("product_template_category_category_fk")
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureProductImage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.ToTable("product_image");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.ProductTemplateId)
                    .IsRequired()
                    .HasColumnName("product_template_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.VariantId)
                    .HasColumnName("variant_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasColumnName("url")
                    .HasColumnType("text");

                entity.Property(e => e.AltText)
                    .HasColumnName("alt_text")
                    .HasColumnType("text");

                entity.Property(e => e.SortOrder)
                    .IsRequired()
                    .HasColumnName("sort_order")
                    .HasColumnType("integer")
                    .HasDefaultValue(0);

                entity.Property(e => e.IsMain)
                    .IsRequired()
                    .HasColumnName("is_main")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.ProductTemplate)
                    .WithMany(t => t.Images)
                    .HasForeignKey(e => e.ProductTemplateId)
                    .HasConstraintName("product_image_template_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Variant)
                    .WithMany(v => v.Images)
                    .HasForeignKey(e => e.VariantId)
                    .HasConstraintName("product_image_variant_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductTemplateId)
                    .HasName("ix_product_image_template");

                entity.HasIndex(e => e.VariantId)
                    .HasName("ix_product_image_variant");

                entity.HasIndex(e => e.ProductTemplateId)
                    .IsUnique()
                    .HasFilter("is_main = true AND variant_id IS NULL")
                    .HasName("ux_product_image_main_template");
            });
        }

        private static void ConfigureStockLocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockLocation>(entity =>
            {
                entity.ToTable("stock_location");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasColumnName("code")
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("text");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("text");

                entity.Property(e => e.IsVirtual)
                    .IsRequired()
                    .HasColumnName("is_virtual")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.IsDefault)
                    .IsRequired()
                    .HasColumnName("is_default")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasColumnName("is_active")
                    .HasColumnType("boolean")
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("stock_location_org_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.Code })
                    .IsUnique()
                    .HasName("ux_stock_location_org_code");

                entity.HasIndex(e => e.OrganizationId)
                    .HasFilter("is_default = true")
                    .IsUnique()
                    .HasName("ux_stock_location_org_default");
            });
        }

        private static void ConfigureStockBalance(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockBalance>(entity =>
            {
                entity.ToTable("stock_balance");

                entity.HasKey(e => new { e.OrganizationId, e.LocationId, e.VariantId });

                entity.Property(e => e.OrganizationId)
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.LocationId)
                    .HasColumnName("location_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.VariantId)
                    .HasColumnName("variant_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.OnHand)
                    .IsRequired()
                    .HasColumnName("on_hand")
                    .HasColumnType("numeric(18,3)")
                    .HasDefaultValue(0);

                entity.Property(e => e.Reserved)
                    .IsRequired()
                    .HasColumnName("reserved")
                    .HasColumnType("numeric(18,3)")
                    .HasDefaultValue(0);

                entity.Property(e => e.LastMovementAt)
                    .IsRequired()
                    .HasColumnName("last_movement_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("stock_balance_org_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Location)
                    .WithMany()
                    .HasForeignKey(e => e.LocationId)
                    .HasConstraintName("stock_balance_location_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Variant)
                    .WithMany()
                    .HasForeignKey(e => e.VariantId)
                    .HasConstraintName("stock_balance_variant_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.VariantId)
                    .HasName("ix_stock_balance_variant");

                entity.HasIndex(e => e.LocationId)
                    .HasName("ix_stock_balance_location");

                entity.HasIndex(e => new { e.OrganizationId, e.VariantId })
                    .HasName("ix_stock_balance_org_variant");

                entity.HasCheckConstraint("stock_balance_non_negative", "on_hand >= 0 AND reserved >= 0 AND on_hand >= reserved");
            });
        }

        private static void ConfigureStockLedger(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockLedger>(entity =>
            {
                entity.ToTable("stock_ledger");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.LocationId)
                    .IsRequired()
                    .HasColumnName("location_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.VariantId)
                    .IsRequired()
                    .HasColumnName("variant_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.DeltaOnHand)
                    .HasColumnName("delta_on_hand")
                    .HasColumnType("numeric(18,3)")
                    .HasDefaultValue(0);

                entity.Property(e => e.DeltaReserved)
                    .HasColumnName("delta_reserved")
                    .HasColumnType("numeric(18,3)")
                    .HasDefaultValue(0);

                entity.Property(e => e.MovementType)
                    .IsRequired()
                    .HasColumnName("movement_type")
                    .HasColumnType("smallint");

                entity.Property(e => e.Reason)
                    .HasColumnName("reason")
                    .HasColumnType("text");

                entity.Property(e => e.SourceType)
                    .HasColumnName("source_type")
                    .HasColumnType("text");

                entity.Property(e => e.SourceId)
                    .HasColumnName("source_id")
                    .HasColumnType("text");

                entity.Property(e => e.SourceLine)
                    .HasColumnName("source_line")
                    .HasColumnType("text");

                entity.Property(e => e.DeduplicationKey)
                    .IsRequired()
                    .HasColumnName("deduplication_key")
                    .HasColumnType("text");

                entity.Property(e => e.OccurredAt)
                    .IsRequired()
                    .HasColumnName("occurred_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Metadata)
                    .IsRequired()
                    .HasColumnName("metadata")
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("stock_ledger_org_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Location)
                    .WithMany()
                    .HasForeignKey(e => e.LocationId)
                    .HasConstraintName("stock_ledger_location_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Variant)
                    .WithMany()
                    .HasForeignKey(e => e.VariantId)
                    .HasConstraintName("stock_ledger_variant_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.DeduplicationKey })
                    .IsUnique()
                    .HasName("ux_stock_ledger_org_dedup");

                entity.HasIndex(e => new { e.VariantId, e.OccurredAt })
                    .HasName("ix_stock_ledger_variant_date");

                entity.HasIndex(e => new { e.LocationId, e.OccurredAt })
                    .HasName("ix_stock_ledger_location_date");

                entity.HasIndex(e => new { e.OrganizationId, e.OccurredAt })
                    .HasName("ix_stock_ledger_org_date");

                entity.HasCheckConstraint("stock_ledger_non_zero_delta", "delta_on_hand <> 0 OR delta_reserved <> 0");
            });
        }

        private static void ConfigureStockReservation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.ToTable("stock_reservation");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .IsRequired()
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.LocationId)
                    .IsRequired()
                    .HasColumnName("location_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.VariantId)
                    .IsRequired()
                    .HasColumnName("variant_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasColumnName("quantity")
                    .HasColumnType("numeric(18,3)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("smallint");

                entity.Property(e => e.ReservedAt)
                    .IsRequired()
                    .HasColumnName("reserved_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnName("expires_at")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.SourceType)
                    .HasColumnName("source_type")
                    .HasColumnType("text");

                entity.Property(e => e.SourceId)
                    .HasColumnName("source_id")
                    .HasColumnType("text");

                entity.Property(e => e.SourceLine)
                    .HasColumnName("source_line")
                    .HasColumnType("text");

                entity.Property(e => e.DeduplicationKey)
                    .IsRequired()
                    .HasColumnName("deduplication_key")
                    .HasColumnType("text");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Metadata)
                    .IsRequired()
                    .HasColumnName("metadata")
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("stock_reservation_org_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Location)
                    .WithMany()
                    .HasForeignKey(e => e.LocationId)
                    .HasConstraintName("stock_reservation_location_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Variant)
                    .WithMany()
                    .HasForeignKey(e => e.VariantId)
                    .HasConstraintName("stock_reservation_variant_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.DeduplicationKey })
                    .IsUnique()
                    .HasName("ux_stock_reservation_org_dedup");

                entity.HasIndex(e => new { e.VariantId, e.Status })
                    .HasName("ix_stock_reservation_variant_status");

                entity.HasIndex(e => new { e.SourceType, e.SourceId })
                    .HasName("ix_stock_reservation_source");

                entity.HasCheckConstraint("stock_reservation_quantity_positive", "quantity > 0");
            });
        }

        private static void ConfigureStockOutboxEvent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockOutboxEvent>(entity =>
            {
                entity.ToTable("stock_outbox_event");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()");

                entity.Property(e => e.OrganizationId)
                    .HasColumnName("organization_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.AggregateType)
                    .IsRequired()
                    .HasColumnName("aggregate_type")
                    .HasColumnType("text");

                entity.Property(e => e.AggregateId)
                    .HasColumnName("aggregate_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasColumnName("event_type")
                    .HasColumnType("text");

                entity.Property(e => e.Payload)
                    .IsRequired()
                    .HasColumnName("payload")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Topic)
                    .HasColumnName("topic")
                    .HasColumnType("text");

                entity.Property(e => e.Key)
                    .HasColumnName("key")
                    .HasColumnType("text");

                entity.Property(e => e.OccurredAt)
                    .IsRequired()
                    .HasColumnName("occurred_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.PublishedAt)
                    .HasColumnName("published_at")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasColumnType("smallint")
                    .HasDefaultValue((short)0);

                entity.Property(e => e.Error)
                    .HasColumnName("error")
                    .HasColumnType("text");

                entity.Property(e => e.TraceId)
                    .HasColumnName("trace_id")
                    .HasColumnType("text");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .HasConstraintName("stock_outbox_event_org_fk")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.Status, e.CreatedAt })
                    .HasName("ix_stock_outbox_status_created");

                entity.HasIndex(e => e.OrganizationId)
                    .HasName("ix_stock_outbox_org");
            });
        }
    }
}
