using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Dominio.ModelViews
{
    public struct Home
    {
        public readonly string Mensagem { get => "API de cadastro de veículos - Minimal API"; }
        public readonly string Doc { get => "/swagger"; }
    }
}