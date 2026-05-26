using Microsoft.EntityFrameworkCore;
using asc.Models;

namespace asc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Garante que o mapeamento respeite a estrutura que criamos no MySQL
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.Documento);
                entity.Property(e => e.CriadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}