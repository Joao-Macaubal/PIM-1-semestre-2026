using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using asc.Data;
using asc.Models;
using System;
using System.Threading.Tasks;

namespace asc.Pages
{
    [IgnoreAntiforgeryToken]
    public class CadastroModel : PageModel
    {
        private readonly AppDbContext _context;

        public CadastroModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Nome { get; set; } = string.Empty;

        [BindProperty]
        public string Documento { get; set; } = string.Empty;

        [BindProperty]
        public string Telefone { get; set; } = string.Empty;

        [BindProperty]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        public string? MensagemErro { get; set; }

        public void OnGet()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("[DEBUG] Tela de Cadastro carregada via GET");
            Console.WriteLine("=========================================");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("[DEBUG] O método OnPostAsync FOI DISPARADO!");
            Console.WriteLine($"[DEBUG] Dados Recebidos -> Nome: '{Nome}', Documento (Bruto): '{Documento}', Telefone: '{Telefone}', Senha Preenchida?: {!string.IsNullOrEmpty(Senha)}");
            Console.WriteLine("=========================================");

            // Força a limpeza de formatações do CPF/CNPJ vindos da máscara do JS
            if (!string.IsNullOrEmpty(Documento))
            {
                Documento = Documento.Replace(".", "").Replace("-", "").Replace("/", "").Trim();
                Console.WriteLine($"[DEBUG] Documento após limpar a máscara do JS: '{Documento}'");
            }

            // 1. Validação básica
            if (string.IsNullOrEmpty(Documento) || string.IsNullOrEmpty(Senha) || string.IsNullOrEmpty(Nome))
            {
                Console.WriteLine("[DEBUG] X VALIDAÇÃO FALHOU: Um ou mais campos obrigatórios chegaram NULOS ou VAZIOS no C#.");
                MensagemErro = "Todos os campos obrigatórios devem ser preenchidos.";
                return Page();
            }

            try
            {
                Console.WriteLine("[DEBUG] Verificando se o documento já existe no MySQL...");
                var clienteExistente = await _context.Clientes.FindAsync(Documento);
                if (clienteExistente != null)
                {
                    Console.WriteLine($"[DEBUG] X CADASTRO BARRADO: O documento {Documento} já existe na tabela Clientes.");
                    MensagemErro = "Este CPF/CNPJ já está cadastrado no sistema.";
                    return Page();
                }

                Console.WriteLine("[DEBUG] Gerando Hash da senha com BCrypt...");
                string senhaCriptografada = BCrypt.Net.BCrypt.HashPassword(Senha);

                Console.WriteLine("[DEBUG] Tentando persistir novo cliente no MySQL...");
                var novoCliente = new Cliente
                {
                    Documento = Documento,
                    Nome = Nome,
                    Telefone = Telefone,
                    SenhaHash = senhaCriptografada,
                    Role = "Cliente"
                };

                _context.Clientes.Add(novoCliente);
                await _context.SaveChangesAsync();

                Console.WriteLine("[DEBUG] É TETRA! Cliente salvo com sucesso. Redirecionando para /Login");
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=========================================");
                Console.WriteLine("[DEBUG] CRÍTICO - EXCEÇÃO DE BANCO DE DADOS:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                Console.WriteLine("=========================================");

                MensagemErro = "Erro interno ao salvar no banco de dados: " + ex.Message;
                return Page();
            }
        }
    }
}