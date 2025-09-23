using minimal_api.Infraestrutura.Db;
using minimal_api.Dominio.DTOs;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.Servicos; // Add this line if AdministradorServico is here
using Microsoft.AspNetCore.Mvc;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens; // Add this if Veiculo is in this namespace
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var keySection = builder.Configuration.GetSection("Jwt");
var key = keySection.Value != null ? keySection.Value.ToString() : string.Empty;

builder.Services.AddAuthentication(option =>
{
  option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
  option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer("Bearer", options =>
{
  options.Authority = "https://localhost:7280/";
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateLifetime = true,
    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
  };
});

builder.Services.AddAuthorization();

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

#region Administradores

// na função abaixo, deverá verificar qual o Perfil do administrador (Editor ou Admin) e permitir ou negar o acesso a determinadas rotas
// por exemplo, apenas administradores com Perfil Admin poderão criar, atualizar ou deletar outros administradores
// já administradores com Perfil Editor poderão apenas visualizar a lista de administradores e seus próprios dados

string GerarTokenJwt(Administrador administrador)
{
  if (string.IsNullOrEmpty(key))
    return string.Empty;

  var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
  var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

  var claims = new List<Claim>
  {
    new Claim("Email", administrador.Email),
    new Claim("Perfil", administrador.Perfil),
  };
  var token = new JwtSecurityToken(
    claims: claims,
    expires: DateTime.Now.AddDays(1),
    signingCredentials: credentials
  );
  return new JwtSecurityTokenHandler().WriteToken(token);
}

//login administrador
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
  var adm = administradorServico.Login(loginDTO);
  if (adm != null)
  {
    string token = GerarTokenJwt(adm);

    return Results.Ok(new AdministradorLogado
    {
      Email = adm.Email,
      Perfil = adm.Perfil,
      Token = token
    });
  }
  else
    return Results.Unauthorized();
}).WithTags("Administrador");

//incluir administrador
app.MapPost("/administrador", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
{
  var validacao = new ErrosDeValidacao
  {
    Mensagens = new List<string>()
  };

  if (string.IsNullOrEmpty(administradorDTO.Email) || !administradorDTO.Email.Contains("@"))
    validacao.Mensagens.Add("Email inválido.");

  if (string.IsNullOrEmpty(administradorDTO.Senha) || administradorDTO.Senha.Length < 6)
    validacao.Mensagens.Add("Senha não pode ter menos que 6 caracteres.");

  //if (string.IsNullOrEmpty(administradorDTO.Perfil.ToString()) || administradorDTO.Perfil.ToString().Length < 3)
  if (administradorDTO.Perfil == null || !Enum.IsDefined(typeof(Perfil), administradorDTO.Perfil))
    validacao.Mensagens.Add("Perfil inválido.");

  if (validacao.Mensagens.Any())
  {
    return Results.BadRequest(validacao);
  }

  var administrador = new Administrador
  {
    Email = administradorDTO.Email,
    Senha = administradorDTO.Senha,
    Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
  };

  administradorServico.Incluir(administrador);

  return Results.Created($"/administrador/{administrador.Id}", administrador);
}).RequireAuthorization().WithTags("Administrador");

//obter todos administradores
app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
  var adms = new List<AdministradorModelView>();
  var administradores = administradorServico.ObterTodos(pagina ?? 1);

  foreach (var adm in administradores)
  {
    adms.Add(new AdministradorModelView
    {
      Id = adm.Id,
      Email = adm.Email,
      Perfil = adm.Perfil
    });
  }

  return Results.Ok(adms);

}).RequireAuthorization().WithTags("Administrador");


//obter administrador por id
app.MapGet("/administrador/{id}", ([FromRoute] int? id, IAdministradorServico administradorServico) =>
{
  var administrador = administradorServico.BuscarPorId(id ?? 0);

  var adms = new AdministradorModelView();

  if (administrador == null)
  {
    return Results.NotFound();
  }

  var adm = new AdministradorModelView
  {
    Id = administrador.Id,
    Email = administrador.Email,
    Perfil = administrador.Perfil
  };


  return Results.Ok(adm);

}).RequireAuthorization().WithTags("Administrador");

//deletar administrador
app.MapDelete("/administrador/{id}", ([FromRoute] int? id, IAdministradorServico administradorServico) =>
{
  var administrador = administradorServico.BuscarPorId(id ?? 0);

  if (administrador == null)
  {
    return Results.NotFound();
  }

  administradorServico.Deletar(administrador);

  return Results.NoContent();

}).RequireAuthorization().WithTags("Administrador");

//atualizar administrador
app.MapPut("/administradores/{id}", ([FromRoute] int? id, [FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
{

  var administrador = administradorServico.BuscarPorId(id ?? 0);

  if (administrador == null) return Results.NotFound();

  administrador.Email = administradorDTO.Email;
  administrador.Senha = administradorDTO.Senha;
  administrador.Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString();

  administradorServico.Atualizar(administrador);

  return Results.Ok(administrador);

}).RequireAuthorization().WithTags("Administrador");


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

//incluir veiculo
app.MapPost("/veiculo", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{

  var validacao = validaDTO(veiculoDTO);

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

}).RequireAuthorization().WithTags("Veiculo");

//obter todos veiculos
app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{
  var veiculos = veiculoServico.ObterTodos(pagina ?? 1);

  return Results.Ok(veiculos);

}).RequireAuthorization().WithTags("Veiculo");

//obter veiculo por id
app.MapGet("/veiculo/{id}", ([FromRoute] int? id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id ?? 0);

  if (veiculo == null)
  {
    return Results.NotFound();
  }
  return Results.Ok(veiculo);

}).RequireAuthorization().WithTags("Veiculo");

//atualizar veiculo
app.MapPut("/veiculos/{id}", ([FromRoute] int? id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{

  var veiculo = veiculoServico.BuscarPorId(id ?? 0);

  if (veiculo == null) return Results.NotFound();

  var validacao = validaDTO(veiculoDTO);

  if (validacao.Mensagens.Any())
  {
    return Results.BadRequest(validacao);
  }

  veiculo.Nome = veiculoDTO.Nome;
  veiculo.Modelo = veiculoDTO.Modelo;
  veiculo.Ano = veiculoDTO.Ano;

  veiculoServico.Atualizar(veiculo);

  return Results.Ok(veiculo);

}).RequireAuthorization().WithTags("Veiculo");

//deletar veiculo
app.MapDelete("/veiculos/{id}", ([FromRoute] int? id, IVeiculoServico veiculoServico) =>
{
  var veiculo = veiculoServico.BuscarPorId(id ?? 0);

  if (veiculo == null)
  {
    return Results.NotFound();
  }

  veiculoServico.Deletar(veiculo);

  return Results.NoContent();

}).RequireAuthorization().WithTags("Veiculo");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

IApplicationBuilder applicationBuilder = app.UseAuthentication();
IApplicationBuilder applicationBuilder1 = app.UseAuthorization();

app.Run();
#endregion



