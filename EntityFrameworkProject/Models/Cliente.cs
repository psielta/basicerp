using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("tenant_id")]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("nome")]
        public string Nome { get; set; }

        [MaxLength(18)]
        [Column("cpf")]
        public string Cpf { get; set; }

        [MaxLength(18)]
        [Column("cnpj")]
        public string Cnpj { get; set; }

        [MaxLength(200)]
        [Column("email")]
        public string Email { get; set; }

        [MaxLength(20)]
        [Column("telefone")]
        public string Telefone { get; set; }

        [MaxLength(20)]
        [Column("celular")]
        public string Celular { get; set; }

        [MaxLength(10)]
        [Column("cep")]
        public string Cep { get; set; }

        [MaxLength(200)]
        [Column("endereco")]
        public string Endereco { get; set; }

        [MaxLength(10)]
        [Column("numero")]
        public string Numero { get; set; }

        [MaxLength(100)]
        [Column("complemento")]
        public string Complemento { get; set; }

        [MaxLength(100)]
        [Column("bairro")]
        public string Bairro { get; set; }

        [MaxLength(100)]
        [Column("cidade")]
        public string Cidade { get; set; }

        [MaxLength(2)]
        [Column("estado")]
        public string Estado { get; set; }

        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        [Column("data_criacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        [Column("data_atualizacao")]
        public DateTime? DataAtualizacao { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }
    }
}
