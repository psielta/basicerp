using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("account")]
    public class Account
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("account_id")]
        public string AccountId { get; set; }

        [Required]
        [Column("provider_id")]
        public string ProviderId { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("access_token")]
        public string AccessToken { get; set; }

        [Column("refresh_token")]
        public string RefreshToken { get; set; }

        [Column("id_token")]
        public string IdToken { get; set; }

        [Column("access_token_expires_at")]
        public DateTime? AccessTokenExpiresAt { get; set; }

        [Column("refresh_token_expires_at")]
        public DateTime? RefreshTokenExpiresAt { get; set; }

        [Column("scope")]
        public string Scope { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("reset_token")]
        public string ResetToken { get; set; }

        [Column("reset_token_expires")]
        public DateTime? ResetTokenExpires { get; set; }

        [Column("last_password_change")]
        public DateTime? LastPasswordChange { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; }
    }
}

