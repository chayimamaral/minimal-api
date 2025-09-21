using minimal_api.Infraestrutura.Db;
using minimal_api.Dominio.DTOs;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.Servicos; // Add this line if AdministradorServico is here
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

IServiceCollection serviceCollection = builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();

builder.Services.AddDbContext<DbContexto>(options =>
{
  options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection"),
  npgsqlOptions =>
  {
    npgsqlOptions.EnableRetryOnFailure(
          maxRetryCount: 5,
          maxRetryDelay: TimeSpan.FromSeconds(10),
          errorCodesToAdd: null);
  });
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
  if (administradorServico.Login(loginDTO) != null)

    return Results.Ok("Login com sucesso");
  else

    return Results.Unauthorized();
});

app.Run();




