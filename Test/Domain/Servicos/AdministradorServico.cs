using System;
using System.Reflection;
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
            //var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var path = Path.GetFullPath(Path.Combine(assemblyPath, @"../../../")); // Caminho para a raiz do projeto

            var builder = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var connectionString = configuration.GetConnectionString("PostgreSqlConnection");

            var options = new DbContextOptionsBuilder<DbContexto>()
                .UseNpgsql(connectionString)
                .Options;

            return new DbContexto(options);
        }
        [TestMethod]
        public void TestandoBuscaPorId()
        {
            // Arrange
            // Aqui você pode configurar os dados necessários para o teste

            var contexto = CriarContextoDeTeste();
            contexto.Database.ExecuteSqlRaw("TRUNCATE TABLE \"Administradores\" RESTART IDENTITY CASCADE;");


            var adm = new Administrador();

            adm.Id = 1;
            adm.Email = "patriaamadabrasil@teste.com";
            adm.Senha = "123456";
            adm.Perfil = "Adm";
            // Act
            // Aqui você pode chamar o método que deseja testar

            var admServico = new AdministradorServico(contexto);

            admServico.Incluir(adm);

            adm.Email = "foo@teste.com";

            //contexto.Administradores.Add(adm);
            admServico.Atualizar(adm);
            //var admin = admServico.BuscarPorId(1);

            // Assert
            // Aqui você pode verificar se o resultado do método é o esperado

            Assert.AreEqual(1, actual: adm.Id);
        }
    }
}