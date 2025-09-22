using minimal_api.Infraestrutura.Db;
using minimal_api.Dominio.DTOs;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.Servicos; // Add this line if AdministradorServico is here
using Microsoft.AspNetCore.Mvc;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Entidades; // Add this if Veiculo is in this namespace

#region Builder
var builder = WebApplication.CreateBuilder(args);

//IServiceCollection serviceCollection = builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Adminstradores
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
  if (administradorServico.Login(loginDTO) != null)

    return Results.Ok("Login com sucesso");
  else

    return Results.Unauthorized();
}).WithTags("Administrador");
#endregion

#region Veiculos

static ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
  var validacao = new ErrosDeValidacao
  {
    Mensagens = new List<string>()
  };

  if (veiculoDTO == null)
  {
    validacao.Mensagens.Add("Dados do veículo não podem ser nulos.");
    return validacao;
  }

  if (string.IsNullOrEmpty(veiculoDTO.Nome) || veiculoDTO.Nome.Length < 3)
    validacao.Mensagens.Add("Nome do veículo deve ter pelo menos 3 caracteres.");

  if (string.IsNullOrEmpty(veiculoDTO.Modelo) || veiculoDTO.Modelo.Length < 3)
    validacao.Mensagens.Add("Modelo do veículo deve ter pelo menos 3 caracteres.");

  if (veiculoDTO.Ano < 1886 || veiculoDTO.Ano > DateTime.Now.Year)
    validacao.Mensagens.Add("Ano do veículo deve ser entre 1886 e o ano atual.");

  return validacao;
}

app.MapPost("/veiculo", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
  //var validacao = new ErrosDeValidacao();
  var validacao = validaDTO(veiculoDTO);
  // if (string.IsNullOrEmpty(veiculoDTO.Nome) || veiculoDTO.Nome.Length < 3)
  //   validacao.Mensagens.Add("Nome do veículo deve ter pelo menos 3 caracteres.");

  // if (string.IsNullOrEmpty(veiculoDTO.Modelo) || veiculoDTO.Modelo.Length < 3)
  //   validacao.Mensagens.Add("Modelo do veículo deve ter pelo menos 3 caracteres.");

  // if (veiculoDTO.Ano < 1886 || veiculoDTO.Ano > DateTime.Now.Year)
  //   validacao.Mensagens.Add("Ano do veículo deve ser entre 1886 e o ano atual.");

  if (validacao.Mensagens.Any())
  {
    return Results.BadRequest(validacao);
  }

  var veiculo = new Veiculo
  {
    Nome = veiculoDTO.Nome,
    Modelo = veiculoDTO.Modelo,
    Ano = veiculoDTO.Ano
  };

  veiculoServico.Incluir(veiculo);
  return Results.Created($"/veiculo/{veiculo.Id}", veiculo);

}).WithTags("Veiculo");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{
  var veiculos = veiculoServico.ObterTodos(pagina ?? 1);

  return Results.Ok(veiculos);

}).WithTags("Veiculo");

app.MapGet("/veiculos/{id}", ([FromRoute] int? id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id ?? 0);

  if (veiculo == null)
  {
    return Results.NotFound();
  }
  return Results.Ok(veiculo);

}).WithTags("Veiculo");

app.MapPut("/veiculos/{id}", ([FromRoute] int? id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id ?? 0);

  if (veiculo == null) return Results.NotFound();


  var validacao = new ErrosDeValidacao();
  if (validacao.Mensagens.Count > 0)
  {
    return Results.BadRequest(validacao);
  }

  veiculo.Nome = veiculoDTO.Nome;
  veiculo.Modelo = veiculoDTO.Modelo;
  veiculo.Ano = veiculoDTO.Ano;

  veiculoServico.Atualizar(veiculo);

  return Results.Ok(veiculo);

}).WithTags("Veiculo");

app.MapDelete("/veiculos/{id}", ([FromRoute] int? id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id ?? 0);

  if (veiculo == null)
  {
    return Results.NotFound();
  }

  veiculoServico.Deletar(veiculo);

  return Results.NoContent();

}).WithTags("Veiculo");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
#endregion



