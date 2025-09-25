using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enums;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;

public class Startup
{
  public Startup(IConfiguration configuration)
  {
    Configuration = configuration;
    key = Configuration?.GetSection("Jwt")?.ToString() ?? "";
  }

  private string key = "";
  public IConfiguration? Configuration { get; set; } = default!;

  public void ConfigureServices(IServiceCollection services)
  {
    #region Builder

    // var keySection = Configuration.GetSection("Jwt");
    // var key = keySection.Value != null ? keySection.Value.ToString() : string.Empty;

    services.AddAuthentication(option =>
    {
      option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
      option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
      //Isso só faz sentido quando você usa um IdentityServer ou outro provedor de tokens externo.
      //Mas neste caso, o próprio sistema gera o JWT → não precisa de Authority.
      //options.Authority = "https://localhost:7280/";

      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
      };
    });

    services.AddAuthorization();

    services.AddScoped<IAdministradorServico, AdministradorServico>();
    services.AddScoped<IVeiculoServico, VeiculoServico>();

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token no seguinte formato: Bearer SEU_TOKEN_AQUI"
      });

      options.AddSecurityRequirement(new OpenApiSecurityRequirement
      {
    {
          new OpenApiSecurityScheme
          {
              Reference = new OpenApiReference
              {
                  Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
              },
              Scheme = "oauth2",
              Name = "Bearer",
              In = ParameterLocation.Header,
          },
          new string[] {}
    }
      });

      //options.OperationFilter<AddRequiredHeaderParameter>();

    });

    services.AddDbContext<DbContexto>(options =>
    {
      options.UseNpgsql(Configuration.GetConnectionString("PostgreSqlConnection"),
      npgsqlOptions =>
      {
        npgsqlOptions.EnableRetryOnFailure(
              maxRetryCount: 5,
              maxRetryDelay: TimeSpan.FromSeconds(10),
              errorCodesToAdd: null);
      });
    });

    #endregion

  }
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    //app.UseCors();

    _ = app.UseEndpoints(endpoints =>
    {


      #region Home
      endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
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
    new Claim(ClaimTypes.Role, administrador.Perfil)
  };
        var token = new JwtSecurityToken(
          claims: claims,
          expires: DateTime.Now.AddDays(1),
          signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
      }

      //login administrador
      endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
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
      }).AllowAnonymous().WithTags("Administrador");

      //incluir administrador
      endpoints.MapPost("/administrador", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
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
      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Administrador");

      //obter todos administradores
      endpoints.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
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

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Administrador");


      //obter administrador por id
      endpoints.MapGet("/administrador/{id}", ([FromRoute] int? id, IAdministradorServico administradorServico) =>
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

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Administrador");

      //deletar administrador
      endpoints.MapDelete("/administrador/{id}", ([FromRoute] int? id, IAdministradorServico administradorServico) =>
      {
        var administrador = administradorServico.BuscarPorId(id ?? 0);

        if (administrador == null)
        {
          return Results.NotFound();
        }

        administradorServico.Deletar(administrador);

        return Results.NoContent();

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Administrador");

      //atualizar administrador
      endpoints.MapPut("/administradores/{id}", ([FromRoute] int? id, [FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
      {

        var administrador = administradorServico.BuscarPorId(id ?? 0);

        if (administrador == null) return Results.NotFound();

        administrador.Email = administradorDTO.Email;
        administrador.Senha = administradorDTO.Senha;
        administrador.Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString();

        administradorServico.Atualizar(administrador);

        return Results.Ok(administrador);

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Administrador");


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
      endpoints.MapPost("/veiculo", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
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

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" }).WithTags("Veiculo");

      //obter todos veiculos
      endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
      {
        var veiculos = veiculoServico.ObterTodos(pagina ?? 1);

        return Results.Ok(veiculos);

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" }).WithTags("Veiculo");

      //obter veiculo por id
      endpoints.MapGet("/veiculo/{id}", ([FromRoute] int? id, IVeiculoServico veiculoServico) =>
      {
        var veiculo = veiculoServico.BuscarPorId(id ?? 0);

        if (veiculo == null)
        {
          return Results.NotFound();
        }
        return Results.Ok(veiculo);

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" }).WithTags("Veiculo");

      //atualizar veiculo
      endpoints.MapPut("/veiculos/{id}", ([FromRoute] int? id, [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
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

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Veiculo");

      //deletar veiculo
      endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int? id, IVeiculoServico veiculoServico) =>
      {
        var veiculo = veiculoServico.BuscarPorId(id ?? 0);

        if (veiculo == null)
        {
          return Results.NotFound();
        }

        veiculoServico.Deletar(veiculo);

        return Results.NoContent();

      }).RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" }).WithTags("Veiculo");

      #endregion
    });
  }
}
