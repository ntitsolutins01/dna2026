using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorEtniaAlunos;
//[Authorize]
public record GetTotalizadorEtniaAlunosQuery : IRequest<TotalizadorEtniaDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorEtniaAlunosQueryHandler : IRequestHandler<GetTotalizadorEtniaAlunosQuery, TotalizadorEtniaDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorEtniaAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorEtniaDto> Handle(GetTotalizadorEtniaAlunosQuery request, CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!);

        var categoriaEtnias = new List<string> { "PARDA", "BRANCA", "PRETA", "INDIGENA", "AMARELA", "NAODECLARADA" };

        var agregados = alunosFiltrados
            .Where(a => a.Etnia != null && categoriaEtnias.Contains(a.Etnia!))
            .Select(a => new { Etnia = a.Etnia!, a.Sexo })
            .GroupBy(x => new { x.Etnia, x.Sexo })
            .Select(g => new { g.Key.Etnia, g.Key.Sexo, Count = g.Count() })
            .ToList();

        // Totalizadores
        var dictTotalizadorEtniaMasculino = categoriaEtnias.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorEtniaFeminino = categoriaEtnias.ToDictionary(k => k, _ => 0m);
        var valorTotal = categoriaEtnias.ToDictionary(k => k, _ => 0m);

        foreach (var row in agregados)
        {
            if (row.Sexo == "M")
                dictTotalizadorEtniaMasculino[row.Etnia] += row.Count;
            else
                dictTotalizadorEtniaFeminino[row.Etnia] += row.Count;

            valorTotal[row.Etnia] += row.Count;
        }

        // Percentuais
        decimal totalMasculino = dictTotalizadorEtniaMasculino.Values.Sum();
        decimal totalFeminino = dictTotalizadorEtniaFeminino.Values.Sum();
        decimal totalGeralEtnias = valorTotal.Values.Sum();

        static Dictionary<string, decimal> Percentuais(
            IReadOnlyList<string> keys,
            IDictionary<string, decimal> src,
            decimal total)
                {
                    return keys.ToDictionary(
                        k => k,
                        k => total == 0 ? 0m : Math.Round(100m * (src[k] / total), 2, MidpointRounding.AwayFromZero)
                    );
                }

        var percTotalizadorEtniaMasculino = Percentuais(categoriaEtnias, dictTotalizadorEtniaMasculino, totalMasculino);
        var percTotalizadorEtniaFeminino = Percentuais(categoriaEtnias, dictTotalizadorEtniaFeminino, totalFeminino);
        var percEtnia = Percentuais(categoriaEtnias, valorTotal, totalGeralEtnias);

        var result = new TotalizadorEtniaDto
        {
            ValorTotalizadorEtniaMasculino = dictTotalizadorEtniaMasculino,
            ValorTotalizadorEtniaFeminino = dictTotalizadorEtniaFeminino,
            PercTotalizadorEtniaMasculino = percTotalizadorEtniaMasculino,
            PercTotalizadorEtniaFeminino = percTotalizadorEtniaFeminino,
            PercEtnia = percEtnia
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Calcula quantidade de anos passdos com base em duas datas, caso encontre qualquer problema retorna 0 
    /// </summary>
    /// <param name="data">Data inicial</param>
    /// <param name="now">Data final ou deixar nula para data atual</param>
    /// <returns>Retorna inteiro com quantiadde de anos</returns>
    private static int GetIdade(DateTime data, DateTime? now = null)
    {
        // Carrega a data do dia para comparação caso data informada seja nula

        now = ((now == null) ? DateTime.Now : now);

        try
        {
            int YearsOld = (now.Value.Year - data.Year);

            if (now.Value.Month < data.Month || (now.Value.Month == data.Month && now.Value.Day < data.Day))
            {
                YearsOld--;
            }

            return (YearsOld < 0) ? 0 : YearsOld;
        }
        catch
        {
            return 0;
        }
    }
}

