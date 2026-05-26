using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asc.Models
{
    [Table("Clientes")]
    public class Cliente
    {
        [Key]
        [Required]
        [StringLength(18)]
        public string Documento { get; set; } = string.Empty; // CPF ou CNPJ

        [Required]
        [StringLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Telefone { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string SenhaHash { get; set; } = string.Empty; // Senha criptografada

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        [Required]
        public string Role { get; set; } = "Cliente";

        
        public List<Imovel> Imoveis { get; set; } = new List<Imovel>();
    }
}