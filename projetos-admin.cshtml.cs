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
    [IgnoreAntiforgeryToken]
    public class projetos_adminModel : PageModel
    {
        private readonly AppDbContext _context;

        public projetos_adminModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Projeto> ListaProjetos { get; set; } = new List<Projeto>();
        public int TotalInstalacao { get; set; }
        public int TotalConcessionaria { get; set; }
        public int TotalFinalizados { get; set; }

        [BindProperty]
        public int ProjetoId { get; set; }

        [BindProperty]
        public string NovoStatus { get; set; } = string.Empty;

        [BindProperty]
        public string? NovoDiarioBordo { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Console.WriteLine("\n=========================================");
            Console.WriteLine("[DEBUG-TECNICO] Executando ON GET ASYNC");
            Console.WriteLine("=========================================");

            ListaProjetos = await _context.Set<Projeto>()
                .Include(p => p.Imovel)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            // 🛡️ Ajuste nos contadores para aceitar as variações de encoding antigas
            TotalInstalacao = ListaProjetos.Count(p => p.StatusExecucao.Contains("Instala"));
            TotalConcessionaria = ListaProjetos.Count(p => p.StatusExecucao.Contains("Concessionaria") || p.StatusExecucao.Contains("ConcessionÃ¡ria") || p.StatusExecucao.Contains("Concessionária") || p.StatusExecucao.Contains("CONCESSIONÁ;RIA"));
            TotalFinalizados = ListaProjetos.Count(p => p.StatusExecucao.Contains("Finalizado"));

            Console.WriteLine($"[DEBUG-TECNICO] Projetos carregados com sucesso. Total na lista: {ListaProjetos.Count}");
            return Page();
        }

        public async Task<IActionResult> OnPostAtualizarOrdemAsync()
        {
            Console.WriteLine("\n=========================================");
            Console.WriteLine("[DEBUG-TECNICO] 🚨 O MÉTODO ONPOST FOI DISPARADO NO C#!");
            Console.WriteLine("=========================================");

            // Resgate manual do Form se o model binding do Razor falhar por encoding
            if (ProjetoId == 0 && Request.Form.ContainsKey("ProjetoId"))
            {
                int.TryParse(Request.Form["ProjetoId"], out int idManual);
                ProjetoId = idManual;
            }
            if (string.IsNullOrEmpty(NovoStatus) && Request.Form.ContainsKey("NovoStatus"))
            {
                NovoStatus = Request.Form["NovoStatus"]!;
            }
            if (string.IsNullOrEmpty(NovoDiarioBordo) && Request.Form.ContainsKey("NovoDiarioBordo"))
            {
                NovoDiarioBordo = Request.Form["NovoDiarioBordo"];
            }

            // Mapeia e converte a string corrompida para o INDICE numérico real do ENUM do MySQL
            string indiceEnum = "1";
            if (NovoStatus.Contains("Materiais"))
                indiceEnum = "1";
            else if (NovoStatus.Contains("Instala") || NovoStatus.Contains("InstalaÃ§Ã£o"))
                indiceEnum = "2";
            else if (NovoStatus.Contains("Concessionaria") || NovoStatus.Contains("ConcessionÃ¡ria") || NovoStatus.Contains("Concessionária"))
                indiceEnum = "3";
            else if (NovoStatus.Contains("Finalizado"))
                indiceEnum = "4";

            Console.WriteLine($"[DEBUG-TECNICO] Status recebido: '{NovoStatus}' -> Convertido para Índice ENUM: {indiceEnum}");

            if (ProjetoId <= 0)
            {
                Console.WriteLine("[DEBUG-TECNICO] ❌ Erro de Validação: ProjetoId inválido.");
                return RedirectToPage();
            }

            try
            {
                Console.WriteLine($"[DEBUG-TECNICO] Executando comando SQL direto para o Projeto ID {ProjetoId}...");

                // Converte para inteiro puro para o MySQL receber o ID numérico sem aspas
                int valorNumericoEnum = int.Parse(indiceEnum);

                // Query direta que atualiza a coluna usando a engine do MySQL sem intermediários do EF Core
                string sql = "UPDATE Projetos SET StatusExecucao = {0}, DiarioBordo = {1}, AtualizadoEm = {2} WHERE Id = {3}";

                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(sql, valorNumericoEnum, NovoDiarioBordo, DateTime.Now, ProjetoId);

                if (linhasAfetadas > 0)
                {
                    Console.WriteLine($"[DEBUG-TECNICO] ✅ SUCESSO TOTAL VIA SQL PURO! Status do ENUM atualizado para a posição: {valorNumericoEnum}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG-TECNICO] ❌ ERRO: Nenhuma linha alterada no banco de dados.");
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine("=========================================");
                Console.WriteLine($"[DEBUG-TECNICO] 🔥 EXCEÇÃO CRÍTICA DE BANCO DE DADOS:");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                Console.WriteLine("=========================================");
                return RedirectToPage();
            }
        }
    }
}