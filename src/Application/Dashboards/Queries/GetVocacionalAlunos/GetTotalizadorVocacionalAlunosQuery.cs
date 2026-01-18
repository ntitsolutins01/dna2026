using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Application.Common.Models;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetVocacionalAlunos;
//[Authorize]
public record GetTotalizadorVocacionalAlunosQuery : IRequest<VocacionalDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorVocacionalAlunosQueryHandler : IRequestHandler<GetTotalizadorVocacionalAlunosQuery, VocacionalDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorVocacionalAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<VocacionalDto> Handle(GetTotalizadorVocacionalAlunosQuery request, CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .AsNoTracking()
            .Select(a => new { a.Id, a.Sexo });

        // Categorias válidas: usa TextoLaudo Tipo 6
        var labels = _context.TextosLaudos.AsNoTracking()
            .Where(t => t.TipoLaudo.Id == 6)
            .Select(t => t.Aviso)
            .ToList();

        var categoriasVocacional = labels
            .Select(a => (a ?? string.Empty).Split('.')[0].Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var comp = StringComparer.OrdinalIgnoreCase;
        var totalGeral = categoriasVocacional.ToDictionary(k => k, _ => 0m, comp);
        var dictTotalizadorVocacionalMasculino = categoriasVocacional.ToDictionary(k => k, _ => 0m, comp);
        var dictTotalizadorVocacionalFeminino = categoriasVocacional.ToDictionary(k => k, _ => 0m, comp);

        // Query única: Aluno x Laudo (com Vocacional) x Encaminhamento (Tipo 6)
        var agregados = (
            from a in alunosFiltrados
            join l in _context.Laudos.AsNoTracking().Where(l => l.Vocacional != null)
                on a.Id equals l.Aluno.Id
            join e in _context.Encaminhamentos.AsNoTracking()
                    .Where(e => e.TipoLaudo.Id == 6)
                on l.Vocacional!.Encaminhamento!.Id equals e.Id
            select new { a.Sexo, Parametro = e.Parametro }
        )
        .GroupBy(x => new { x.Parametro, x.Sexo })
        .Select(g => new { g.Key.Parametro, g.Key.Sexo, Count = g.Count() })
        .ToList();

        // Preenche dicionários
        foreach (var row in agregados)
        {
            var key = row.Parametro?.Trim();
            if (string.IsNullOrEmpty(key) || !totalGeral.ContainsKey(key)) continue;

            if (row.Sexo == "M") dictTotalizadorVocacionalMasculino[key] += row.Count;
            else dictTotalizadorVocacionalFeminino[key] += row.Count;

            totalGeral[key] += row.Count;
        }

        // Percentuais
        static Dictionary<string, decimal> Percentuais(Dictionary<string, decimal> src)
        {
            var sum = src.Values.Sum();
            return sum == 0
                ? src.Keys.ToDictionary(k => k, _ => 0m)
                : src.ToDictionary(kv => kv.Key, kv => Math.Round(100m * kv.Value / sum, 2));
        }

        var percTotalizadorVocacionalMasculino = Percentuais(dictTotalizadorVocacionalMasculino);
        var percTotalizadorVocacionalFeminino = Percentuais(dictTotalizadorVocacionalFeminino);
        var percVocacional = Percentuais(totalGeral);

        var result = new VocacionalDto
        {
            ValorTotalizadorVocacionalMasculino = dictTotalizadorVocacionalMasculino,
            ValorTotalizadorVocacionalFeminino = dictTotalizadorVocacionalFeminino,
            PercTotalizadorVocacionalMasculino = percTotalizadorVocacionalMasculino,
            PercTotalizadorVocacionalFeminino = percTotalizadorVocacionalFeminino,
            PercentualVocacional = percVocacional
        };

        return Task.FromResult(result);
    }

}

