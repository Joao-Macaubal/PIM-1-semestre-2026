using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using asc.Data;
using asc.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace asc.Pages
{
    public class DashboardClienteModel : PageModel // 👈 Nome alterado para evitar conflito com o compilador
    {
        private readonly AppDbContext _context;

        public DashboardClienteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string TipoImovel { get; set; } = string.Empty;

        [BindProperty]
        public decimal AreaDisponivel { get; set; }

        [BindProperty]
        public string Cidade { get; set; } = string.Empty;

        [BindProperty]
        public string Endereco { get; set; } = string.Empty;

        [BindProperty]
        public decimal ConsumoMedioMensal { get; set; }

        public string ClienteNome { get; set; } = "Cliente";
        public Imovel? ImovelAtual { get; set; }
        public Projeto? ProjetoAtual { get; set; }
        public string? MensagemErro { get; set; }
        public string? MensagemSucesso { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Recupera o identificador guardado no cookie (atualmente vindo como o nome do usuário)
            var identificadorCookie = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(identificadorCookie))
            {
                return RedirectToPage("/Login");
            }

            Console.WriteLine($"=========================================");
            Console.WriteLine($"[DEBUG-GET] Carregando painel para o identificador: '{identificadorCookie}'");
            Console.WriteLine($"=========================================");

            // 2. 🛡️ A CORREÇÃO: Busca o cliente aceitando correspondência por Nome ou por Documento
            var cliente = await _context.Clientes
                .Include(c => c.Imoveis)
                    .ThenInclude(i => i.Projeto)
                .FirstOrDefaultAsync(c => c.Nome == identificadorCookie || c.Documento == identificadorCookie);

            if (cliente != null)
            {
                ClienteNome = cliente.Nome;

                // Se a lista de imóveis existir, pega o mais recente cadastrado no MySQL
                if (cliente.Imoveis != null && cliente.Imoveis.Any())
                {
                    ImovelAtual = cliente.Imoveis.OrderByDescending(i => i.Id).FirstOrDefault();

                    if (ImovelAtual != null)
                    {
                        ProjetoAtual = ImovelAtual.Projeto;
                        Console.WriteLine($"[DEBUG-GET] Sucesso! Imóvel ID {ImovelAtual.Id} carregado para a dashboard.");
                    }
                }
                else
                {
                    Console.WriteLine("[DEBUG-GET] Este cliente ainda não possui nenhum imóvel cadastrado.");
                }
            }
            else
            {
                Console.WriteLine("[DEBUG-GET] X Nenhhum cadastro correspondente localizado no banco.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostEnviarSolicitacaoAsync()
        {
            // 1. Recupera o valor guardado no cookie (que atualmente está vindo como o nome 'asd')
            var identificadorCookie = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(identificadorCookie))
            {
                return RedirectToPage("/Login");
            }

            // 2. 🛡️ BUSCA O CPF REAL: Como o cookie guardou o nome, localizamos o cliente pelo Nome no MySQL
            var clienteNoBanco = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Nome == identificadorCookie || c.Documento == identificadorCookie);

            if (clienteNoBanco == null)
            {
                Console.WriteLine($"[ERRO-PORTAL] Sessão inválida. Nenhum cliente achado para: '{identificadorCookie}'");
                return RedirectToPage("/Login");
            }

            // Agora temos o CPF numérico exato salvo no banco (Ex: '00000000001')
            string documentoClienteReal = clienteNoBanco.Documento;

            // 3. Validação dos campos técnicos obrigatórios
            if (string.IsNullOrEmpty(TipoImovel) || AreaDisponivel <= 0 || string.IsNullOrEmpty(Cidade) || string.IsNullOrEmpty(Endereco) || ConsumoMedioMensal <= 0)
            {
                MensagemErro = "Todos os campos técnicos obrigatórios devem ser preenchidos corretamente.";
                await OnGetAsync();
                return Page();
            }

            try
            {
                // 4. Monta a entidade apontando estritamente para o CPF numérico correto
                var novoImovel = new Imovel
                {
                    ClienteDocumento = documentoClienteReal, // 👈 Agora vai o '00000000001' perfeito!
                    TipoImovel = TipoImovel,
                    AreaDisponivel = AreaDisponivel,
                    Cidade = Cidade,
                    Endereco = Endereco,
                    ConsumoMedioMensal = ConsumoMedioMensal,
                    CriadoEm = DateTime.Now
                };

                _context.Add(novoImovel);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = "Nova solicitação registrada com sucesso! Nossa engenharia iniciará a triagem de viabilidade.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=========================================");
                Console.WriteLine($"[ERRO CRÍTICO] Falha ao persistir Imóvel no MySQL:");
                Console.WriteLine($"Identificador do Cookie: '{identificadorCookie}'");
                Console.WriteLine($"CPF Real Localizado: '{documentoClienteReal}'");
                Console.WriteLine($"Mensagem: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                Console.WriteLine("=========================================");

                MensagemErro = "Erro interno ao salvar os dados técnicos.";
                await OnGetAsync();
                return Page();
            }
        }
    }
}