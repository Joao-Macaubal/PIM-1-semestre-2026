using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using asc.Data;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace asc.Pages
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Documento { get; set; } = string.Empty;

        [BindProperty]
        public string Senha { get; set; } = string.Empty;

        public string? MensagemErro { get; set; }

        public void OnGet()
        {
            Console.WriteLine("[DEBUG] Tela de Login carregada via GET.");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("[DEBUG] Tentativa de login iniciada via MySQL puro.");
            Console.WriteLine($"[DEBUG] Usuário/Documento digitado: '{Documento}'");
            Console.WriteLine("=========================================");

            if (string.IsNullOrEmpty(Documento) || string.IsNullOrEmpty(Senha))
            {
                MensagemErro = "Por favor, preencha todos os campos.";
                return Page();
            }

            // 1. Limpa o documento (remove formatações se for CPF/CNPJ)
            // Se for a palavra "admin", o Trim() garante que espaços não quebrem a busca
            var docLimpo = Documento.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

            Console.WriteLine($"[DEBUG] Buscando '{docLimpo}' na tabela Clientes...");
            var usuario = await _context.Clientes.FirstOrDefaultAsync(c => c.Documento == docLimpo);

            // 2. Se não encontrou o usuário no banco
            if (usuario == null)
            {
                Console.WriteLine("[DEBUG] X LOGIN FALHOU: Usuário não encontrado no MySQL.");
                MensagemErro = "Usuário ou senha inválidos.";
                return Page();
            }

            // 3. Verifica a senha criptografada com o BCrypt
            Console.WriteLine("[DEBUG] Usuário encontrado! Validando hash da senha...");
            bool senhaValida = BCrypt.Net.BCrypt.Verify(Senha, usuario.SenhaHash);

            if (!senhaValida)
            {
                Console.WriteLine("[DEBUG] X LOGIN FALHOU: Senha incorreta para o hash do banco.");
                MensagemErro = "Usuário ou senha inválidos.";
                return Page();
            }

            // 4. Captura os dados reais da coluna do banco de dados
            string roleUsuario = usuario.Role;
            string nomeUsuario = usuario.Nome;
            Console.WriteLine($"[DEBUG] Sucesso! Autenticado como: '{nomeUsuario}' | Perfil: '{roleUsuario}'");

            // 5. Criação do Cookie de Sessão
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nomeUsuario),
                new Claim(ClaimTypes.NameIdentifier, usuario.Documento),
                new Claim(ClaimTypes.Role, roleUsuario)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // 6. Redirecionamento baseado na Role do Banco
            if (roleUsuario.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[DEBUG] Redirecionando para o painel do Administrador.");
                return RedirectToPage("/dashboard-admin");
            }

            Console.WriteLine("[DEBUG] Redirecionando para o portal do Cliente.");
            return RedirectToPage("/dashboard-cliente");
        }
    }
}