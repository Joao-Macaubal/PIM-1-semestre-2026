using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using asc.Data;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace asc.Pages
{
    [IgnoreAntiforgeryToken]
    public class RecuperarSenhaModel : PageModel
    {
        private readonly AppDbContext _context;

        public RecuperarSenhaModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Documento { get; set; } = string.Empty;

        [BindProperty]
        public string Telefone { get; set; } = string.Empty;

        [BindProperty]
        public string NovaSenha { get; set; } = string.Empty;

        // Propriedade que define qual etapa exibir no HTML
        public bool DadosVerificados { get; set; } = false;

        public string? MensagemErro { get; set; }
        public string? MensagemSucesso { get; set; }

        public void OnGet()
        {
            Console.WriteLine("[DEBUG] Tela de recuperação de senha carregada.");
        }

        public async Task<IActionResult> OnPostAsync(string acao)
        {
            // Limpa formatações básicas
            var docLimpo = Documento.Replace(".", "").Replace("-", "").Replace("/", "").Trim();
            var telLimpo = Telefone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();

            // ========================================================
            // ETAPA 1: VERIFICAÇÃO DOS DADOS
            // ========================================================
            if (acao == "verificar")
            {
                if (string.IsNullOrEmpty(docLimpo) || string.IsNullOrEmpty(telLimpo))
                {
                    MensagemErro = "Insira o documento e o telefone cadastrados.";
                    return Page();
                }

                Console.WriteLine($"[DEBUG] Validando se existe: Doc '{docLimpo}' e Tel '{telLimpo}'");
                var usuarioExiste = await _context.Clientes.AnyAsync(c => c.Documento == docLimpo && c.Telefone == telLimpo);

                if (!usuarioExiste)
                {
                    Console.WriteLine("[DEBUG] X Verificação falhou: Dados inexistentes ou incorretos.");
                    MensagemErro = "Dados inválidos. Nenhum usuário encontrado com essas informações.";
                    return Page();
                }

                Console.WriteLine("[DEBUG] Avançando para Etapa 2: Usuário validado com sucesso.");
                DadosVerificados = true; // Libera os campos de senha no HTML
                return Page();
            }

            // ========================================================
            // ETAPA 2: SALVAR NOVA SENHA
            // ========================================================
            if (acao == "salvar")
            {
                if (string.IsNullOrEmpty(docLimpo) || string.IsNullOrEmpty(NovaSenha))
                {
                    MensagemErro = "A nova senha não pode estar vazia.";
                    DadosVerificados = true; // Mantém na etapa 2
                    return Page();
                }

                Console.WriteLine($"[DEBUG] Buscando usuário '{docLimpo}' para aplicar nova senha...");
                var usuario = await _context.Clientes.FirstOrDefaultAsync(c => c.Documento == docLimpo);

                if (usuario == null)
                {
                    MensagemErro = "Erro crítico ao identificar o usuário. Tente novamente.";
                    return Page();
                }

                // Criptografa e atualiza no banco
                string novoHash = BCrypt.Net.BCrypt.HashPassword(NovaSenha);
                usuario.SenhaHash = novoHash;

                try
                {
                    _context.Clientes.Update(usuario);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("[DEBUG] Sucesso! Senha alterada no MySQL com BCrypt.");
                    MensagemSucesso = "Senha redefinida com sucesso! Redirecionando para o login...";
                    return Page();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] X Erro ao atualizar o banco: {ex.Message}");
                    MensagemErro = "Erro interno ao salvar os dados no MySQL.";
                    DadosVerificados = true;
                    return Page();
                }
            }

            return Page();
        }
    }
}