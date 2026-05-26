using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asc.Models // 👈 TEM QUE SER EXATAMENTE ASSIM
{
    [Table("Imoveis")]
    public class Imovel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(18)]
        public string ClienteDocumento { get; set; } = string.Empty;

        [Required]
        public string TipoImovel { get; set; } = "Residencial";

        [Column(TypeName = "decimal(10,2)")]
        public decimal AreaDisponivel { get; set; }

        [Required]
        [StringLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Endereco { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal ConsumoMedioMensal { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        // 🛡️ Propriedade de navegação para o relacionamento 1:1 com Projeto
        public Projeto? Projeto { get; set; }
    }
}