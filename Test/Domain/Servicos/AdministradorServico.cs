using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;

namespace Test.Domain.Servicos
{
    [TestClass]
    public class AdministradorServicoTest
    {
        private DbContexto CriarContextoDeTeste()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var connectionString = configuration.GetConnectionString("PostgreSqlConnection");

            var options = new DbContextOptionsBuilder<DbContexto>()
                .UseNpgsql(connectionString)
                .Options;

            return new DbContexto(options);
        }
        [TestMethod]
        public void TestandoSalvarAdministrador()
        {
            // Arrange
            // Aqui você pode configurar os dados necessários para o teste

            var contexto = CriarContextoDeTeste();
            contexto.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Administradores\" RESTART IDENTITY CASCADE;");


            var adm = new Administrador();

            adm.Email = "teste@teste.com";
            adm.Senha = "123456";
            adm.Perfil = "Adm";
            // Act
            // Aqui você pode chamar o método que deseja testar

            var admServico = new AdministradorServico(contexto);
            contexto.Administradores.Add(adm);
            admServico.Incluir(adm);

            // Assert
            // Aqui você pode verificar se o resultado do método é o esperado

            Assert.AreEqual(1, admServico.ObterTodos(1).Count);
        }
    }
}