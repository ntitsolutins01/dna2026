using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorDeficienciaAlunos;
//[Authorize]
public record GetTotalizadorDeficienciaAlunosQuery : IRequest<TotalizadorDeficienciaDto>
{
    public DashboardDto? SearchFilter { get; init; }

}
public class GetTotalizadorDeficienciaAlunosQueryHandler : IRequestHandler<GetTotalizadorDeficienciaAlunosQuery, TotalizadorDeficienciaDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorDeficienciaAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorDeficienciaDto> Handle(GetTotalizadorDeficienciaAlunosQuery request, CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .Include(i => i.Deficiencia);

        var deficiencia = _context.Deficiencias
            .Where(d => d.Status)
            .Select(d => d.Nome)
            .ToList();

        var dictTotalizadorDeficienciaMasculino = deficiencia.Distinct().ToDictionary(n => n, _ => 0m);
        var dictTotalizadorDeficienciaFeminino = deficiencia.Distinct().ToDictionary(n => n, _ => 0m);
        var total = deficiencia.Distinct().ToDictionary(n => n, _ => 0m);

        var agregados = alunosFiltrados
            .Where(a => a.Deficiencia != null && a.Deficiencia.Status)
            .Select(a => new { Nome = a.Deficiencia!.Nome!, a.Sexo })
            .GroupBy(x => new { x.Nome, x.Sexo })
            .Select(g => new { g.Key.Nome, g.Key.Sexo, Count = g.Count() })
            .ToList();

        foreach (var row in agregados)
        {
            if (row.Sexo == "M")
                dictTotalizadorDeficienciaMasculino[row.Nome] = dictTotalizadorDeficienciaMasculino.GetValueOrDefault(row.Nome) + row.Count;
            else
                dictTotalizadorDeficienciaFeminino[row.Nome] = dictTotalizadorDeficienciaFeminino.GetValueOrDefault(row.Nome) + row.Count;

            total[row.Nome] = total.GetValueOrDefault(row.Nome) + row.Count;
        }

        // Percentuais
        decimal totalMasculino = dictTotalizadorDeficienciaMasculino.Values.Sum();
        decimal totalFeminino = dictTotalizadorDeficienciaFeminino.Values.Sum();
        decimal totalGeral = total.Values.Sum();

        static Dictionary<string, decimal> Percentuais(Dictionary<string, decimal> src, decimal total)
        => total == 0
            ? src.Keys.ToDictionary(k => k, _ => 0m)
            : src.ToDictionary(kv => kv.Key, kv => Math.Round(100m * (kv.Value / total), 2));

        var percTotalizadorDeficienciaMasculino = Percentuais(dictTotalizadorDeficienciaMasculino, totalMasculino);
        var percTotalizadorDeficienciaFeminino = Percentuais(dictTotalizadorDeficienciaFeminino, totalFeminino);
        var percDeficiencia = Percentuais(total, totalGeral);

        return Task.FromResult(new TotalizadorDeficienciaDto
        {
            ValorTotalizadorDeficienciaMasculino = dictTotalizadorDeficienciaMasculino,
            ValorTotalizadorDeficienciaFeminino = dictTotalizadorDeficienciaFeminino,
            PercTotalizadorDeficienciaMasculino = percTotalizadorDeficienciaMasculino,
            PercTotalizadorDeficienciaFeminino = percTotalizadorDeficienciaFeminino,
            PercDeficiencia = percDeficiencia
        });
    }
}

