using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace EntityFrameworkProject.Models
{
    [Table("session")]
    public class Session
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Required]
        [Column("token")]
        public string Token { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("ip_address")]
        public IPAddress IpAddress { get; set; }

        [Column("user_agent")]
        public string UserAgent { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("active_organization_id")]
        public Guid? ActiveOrganizationId { get; set; }

        public User User { get; set; }

        public Organization ActiveOrganization { get; set; }
    }
}

