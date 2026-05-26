using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using asc.Data;
using asc.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace asc.Pages
{
    // 🛡️ Segurança: Só quem tem a Role "Admin" entra aqui
    [Authorize(Roles = "Admin")]
    public class ClientesAdminModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClientesAdminModel(AppDbContext context)
        {
            _context = context;
        }

        // Esta lista vai guardar os clientes que vierem do banco para o HTML ler
        public IList<Cliente> ListaClientes { get; set; } = new List<Cliente>();

        public string? MensagemSucesso { get; set; }
        public string? MensagemErro { get; set; }

        // 1ª FUNÇÃO: Carrega a lista de clientes ao abrir a página
        public async Task<IActionResult> OnGetAsync()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("[DEBUG-ADMIN] Carregando clientes estritamente com Imóveis e Projetos...");
            Console.WriteLine("=========================================");

            try
            {
                // Puxa apenas os dados existentes no banco original
                ListaClientes = await _context.Clientes
                    .Include(c => c.Imoveis)               // Carrega os imóveis vinculados ao CPF/CNPJ
                        .ThenInclude(i => i.Projeto)       // Carrega o projeto de cada imóvel se houver
                    .Where(c => c.Documento != "admin")
                    .ToListAsync();

                Console.WriteLine($"[DEBUG-ADMIN] Sucesso! {ListaClientes.Count} clientes carregados.");
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG-ADMIN] X ERRO CRÍTICO ao buscar dados: {ex.Message}");
                MensagemErro = "Erro interno ao carregar dados relacionais do banco.";
                return Page();
            }
        }
    }
}