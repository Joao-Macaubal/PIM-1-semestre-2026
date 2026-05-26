using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asc.Models // 👈 TEM QUE SER EXATAMENTE ASSIM
{
    [Table("Projetos")]
    public class Projeto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ImovelId { get; set; }

        [StringLength(100)]
        public string ResponsavelTecnico { get; set; } = "Eng. Gabriel Oliveira";

        [Required]
        public string StatusExecucao { get; set; } = "Aguardando Materiais";

        public string? DiarioBordo { get; set; }

        public DateTime AtualizadoEm { get; set; } = DateTime.Now;

        // Propriedade de navegação inversa para o Imóvel
        [ForeignKey("ImovelId")]
        public Imovel? Imovel { get; set; }
    }
}