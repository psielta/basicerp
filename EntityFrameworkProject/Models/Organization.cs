using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("organization")]
    public class Organization
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("slug")]
        public string Slug { get; set; }

        [Column("logo")]
        public string Logo { get; set; }

        [Column("metadata")]
        public string Metadata { get; set; } = "{}";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<Membership> Memberships { get; set; }

        public ICollection<Session> ActiveSessions { get; set; }
    }
}

