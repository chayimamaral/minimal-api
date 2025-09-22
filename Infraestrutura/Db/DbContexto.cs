using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Entidades;

namespace minimal_api.Infraestrutura.Db
{
    public class DbContexto : DbContext
    {

        private readonly IConfiguration _configurationAppSettings;
        // public DbContexto(IConfiguration configurationAppSettings)
        // {
        //     _configurationAppSettings = configurationAppSettings;

        // }
        public DbContexto(DbContextOptions<DbContexto> options) : base(options) { }

        public DbSet<Administrador> Administradores { get; set; } = default!;
        public DbSet<Veiculo> Veiculos { get; set; } = default!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Administrador>().HasData(
                new Administrador
                {
                    Id = 1,
                    Email = "adm@teste.com",
                    Senha = "123456",
                    Perfil = "Adm"
                });

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {

                var stringConexao = _configurationAppSettings.GetConnectionString("PostgreSqlConnection")?.ToString();
                if (!string.IsNullOrEmpty(stringConexao))
                {
                    optionsBuilder.UseNpgsql(stringConexao);
                }
                throw new Exception("String de conexão nula");
            }
        }
    }
}