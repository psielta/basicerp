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
    }
}
