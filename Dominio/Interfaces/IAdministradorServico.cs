using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;

namespace minimal_api.Dominio.Interfaces
{
    public interface IAdministradorServico
    {
        Administrador? Login(LoginDTO loginDTO);
        List<AdministradorModelView> ObterTodos(int? pagina);
        Administrador? BuscarPorId(int id);
        Administrador Incluir(Administrador administrador);
        void Atualizar(Administrador administrador);
        bool Deletar(Administrador administrador);

    }
}