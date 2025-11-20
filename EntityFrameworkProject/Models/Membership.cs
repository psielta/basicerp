using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("memberships")]
    public class Membership
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("role")]
        public string Role { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("team_id")]
        public Guid? TeamId { get; set; }

        public Organization Organization { get; set; }

        public User User { get; set; }
    }
}

