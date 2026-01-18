using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries;

// Filtro de Pesquisa Principal do DashBoard
public static class DashboardFilterPrincipal
{
    public static IQueryable<Aluno> FiltrarAlunos(IApplicationDbContext context, DashboardDto filtro)
    {
        var alunos = context.Alunos
            .AsNoTracking()
            .Where(a => !a.Convidado);

        // Contrato
        if (!string.IsNullOrWhiteSpace(filtro.FomentoId) &&
            int.TryParse(filtro.FomentoId.Split('-')[0], out var fomentoId))
        {
            var id = Convert.ToInt32(filtro.FomentoId.Split("-")[0]);

            alunos = alunos.Where(u => u.Fomento.Id == id);
        }

        // Localidade do Contrato
        if (!string.IsNullOrWhiteSpace(filtro.LocalidadeId) &&
            int.TryParse(filtro.LocalidadeId.Split('-')[0], out var localidadeId))
        {
            alunos = alunos.Where(u => u.Localidade!.Id == Convert.ToInt32(filtro.LocalidadeId));
        }

        // Estado
        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            alunos = alunos.Where(u => u.Municipio!.Estado!.Sigla!.Contains(filtro.Estado));
        }

        // Município
        if (!string.IsNullOrWhiteSpace(filtro.MunicipioId) &&
            int.TryParse(filtro.MunicipioId.Split('-')[0], out var municipioId))
        {
            alunos = alunos.Where(u => u.Municipio!.Id == Convert.ToInt32(filtro.MunicipioId));
        }

        return alunos;
    }
}
