using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("user")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; }

        [Column("email_verified")]
        public bool EmailVerified { get; set; }

        [Column("image")]
        public string Image { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("metadata")]
        public string Metadata { get; set; } = "{}";

        [Column("two_factor_enabled")]
        public bool TwoFactorEnabled { get; set; }

        public ICollection<Membership> Memberships { get; set; }

        public ICollection<Account> Accounts { get; set; }

        public ICollection<Session> Sessions { get; set; }
    }
}

