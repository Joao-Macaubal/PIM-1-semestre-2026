using asc.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. GARANTA QUE ESSA LINHA EXISTE AQUI (Ativa o suporte ao Razor Pages)
builder.Services.AddRazorPages();

// Configuração do MySQL que fizemos antes
// Configuração estável e manual do MySQL no seu PIM
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("ConexaoMysql"),
        new MySqlServerVersion(new Version(8, 0, 0)) // 👈 Versão fixada manualmente (evita o AutoDetect)
    )
);
// Configuração de Autenticação por Cookies
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login"; // Se tentar acessar sem logar, vai pro Login
        options.AccessDeniedPath = "/Login"; // Se cliente tentar acessar tela de Admin, é barrado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Tempo da sessão
    });

var app = builder.Build();

// Configurações de ambiente
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();

// 2. GARANTA QUE ESSA LINHA EXISTE AQUI (Permite ler arquivos da wwwroot)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // 👈 Adicione esta se não tiver
app.UseAuthorization();

// 3. GARANTA QUE ESSA LINHA EXISTE AQUI (Faz o mapeamento das URLs das páginas)
app.MapRazorPages();

app.Run();