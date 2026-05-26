using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using asc.Data;
using asc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace asc.Pages
{
    public class DashboardAdminModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardAdminModel(AppDbContext context)
        {
            _context = context;
        }

        // Listas para a tabela e filtros
        public List<Imovel> ListaSolicitacoes { get; set; } = new List<Imovel>();

        // Contadores das métricas superiores
        public int TotalNovas { get; set; }
        public int TotalEmAnalise { get; set; }
        public int TotalPropostas { get; set; }

        // Filtros capturados na URL via GET
        [BindProperty(SupportsGet = true)]
        public string? FiltroCidade { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FiltroTipoImovel { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Console.WriteLine("[DEBUG-ADMIN] Calculando métricas e carregando imóveis...");

            // Query base trazendo o Imovel + Projeto correspondente (Relação 1:1)
            var query = _context.Set<Imovel>()
                .Include(i => i.Projeto)
                .AsQueryable();

            // Executa os filtros caso o usuário tenha digitado algo na tela
            if (!string.IsNullOrEmpty(FiltroCidade))
            {
                query = query.Where(i => i.Cidade.Contains(FiltroCidade));
            }

            if (!string.IsNullOrEmpty(FiltroTipoImovel))
            {
                query = query.Where(i => i.TipoImovel == FiltroTipoImovel);
            }

            // Traz a lista ordenada de forma decrescente para os mais novos aparecerem no topo
            ListaSolicitacoes = await query.OrderByDescending(i => i.Id).ToListAsync();

            // Cálculo das métricas dinâmicas com base nas regras do banco de dados
            // Como não modificamos o banco, dividimos o status entre a existência ou não do projeto vinculado
            TotalNovas = ListaSolicitacoes.Count(i => i.Projeto == null);
            TotalEmAnalise = ListaSolicitacoes.Count(i => i.Projeto != null && i.Projeto.StatusExecucao == "Aguardando Materiais");
            TotalPropostas = ListaSolicitacoes.Count(i => i.Projeto != null && i.Projeto.StatusExecucao != "Aguardando Materiais");

            return Page();
        }

        public async Task<IActionResult> OnPostConverterParaProjetoAsync(int imovelId)
        {
            Console.WriteLine($"[DEBUG-CONVERSÃO] Iniciando criação de projeto para o Imóvel ID: {imovelId}");

            if (imovelId <= 0)
            {
                return RedirectToPage();
            }

            try
            {
                // 1. Verifica se o imóvel existe no MySQL
                var imovelExiste = await _context.Set<Imovel>().AnyAsync(i => i.Id == imovelId);
                if (!imovelExiste)
                {
                    Console.WriteLine("[DEBUG-CONVERSÃO] X Erro: Imóvel não localizado no banco.");
                    return RedirectToPage();
                }

                // 2. Garante a regra estrita de 1:1 do PIM (Verifica se já não criaram um projeto para esse imóvel antes)
                var projetoJaExiste = await _context.Set<Projeto>().AnyAsync(p => p.ImovelId == imovelId);
                if (projetoJaExiste)
                {
                    Console.WriteLine("[DEBUG-CONVERSÃO] X Bloqueado: Relação 1:1 violada. Imóvel já possui projeto ativo.");
                    return RedirectToPage();
                }

                // 3. Cria a nova entidade Projeto respeitando as colunas e os defaults do seu script SQL
                var novoProjeto = new Projeto
                {
                    ImovelId = imovelId,
                    ResponsavelTecnico = "Eng. Gabriel Oliveira", // Default definido no seu banco
                    StatusExecucao = "Aguardando Materiais",       // Status inicial padrão
                    DiarioBordo = "Solicitação comercial aprovada. Projeto fotovoltaico inicial gerado pelo sistema corporativo.",
                    AtualizadoEm = DateTime.Now
                };

                _context.Add(novoProjeto);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DEBUG-CONVERSÃO] É TETRA! Projeto gerado com sucesso para o Imóvel {imovelId}.");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG-CONVERSÃO] X ERRO CRÍTICO ao salvar projeto: {ex.Message}");
                return RedirectToPage();
            }
        }
    }
}