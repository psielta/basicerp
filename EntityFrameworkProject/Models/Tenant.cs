using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("tenants")]
    public class Tenant
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("nome")]
        public string Nome { get; set; }

        [Required]
        [MaxLength(18)]
        [Column("cnpj")]
        public string Cnpj { get; set; }

        [MaxLength(200)]
        [Column("email")]
        public string Email { get; set; }

        [MaxLength(20)]
        [Column("telefone")]
        public string Telefone { get; set; }

        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        [Column("data_criacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }

        public virtual ICollection<Usuario> Usuarios { get; set; }
    }
}
