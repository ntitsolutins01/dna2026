using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Application.Common.Models;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorSaudeSexoAlunos;
//[Authorize]
public record GetTotalizadorSaudeSexoAlunosQuery : IRequest<TotalizadorSexoSaudeDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorSaudeSexoAlunosQueryHandler : IRequestHandler<GetTotalizadorSaudeSexoAlunosQuery, TotalizadorSexoSaudeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorSaudeSexoAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorSexoSaudeDto> Handle(GetTotalizadorSaudeSexoAlunosQuery request, CancellationToken cancellationToken)
    {
        var hoje = DateTime.Today;

        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .AsNoTracking()
            .Select(a => new { a.Id, a.Sexo, a.DtNascimento });

        // Classificação por sexo: (Aluno x Laudo) x MetricaImc
        var classificacaoPorSexo = (
            from l in _context.Laudos.AsNoTracking()
            where l.Saude != null
            join a in alunosFiltrados on l.Aluno.Id equals a.Id
            let alturaM = (double)l.Saude!.Altura! * 0.01d
            let imc = (double)l.Saude!.Massa! / Math.Pow(alturaM, 2d)
            let idade = (hoje.Year - a.DtNascimento.Year)
                        - (a.DtNascimento.AddYears(hoje.Year - a.DtNascimento.Year) > hoje ? 1 : 0)
            // pega a faixa de IMC (uma linha) compatível com idade/sexo e IMC calculado
            from mi in _context.MetricasImc.AsNoTracking()
                .Where(mi =>
                    mi.Idade == idade &&
                    mi.Sexo == (idade == 99 ? "G" : a.Sexo) &&
                    imc >= (double)mi.ValorInicial && imc <= (double)mi.ValorFinal)
                .Take(1)
            select new { a.Sexo, mi.Classificacao }
        )
        .GroupBy(x => new { x.Classificacao, x.Sexo })
        .Select(g => new { g.Key.Classificacao, g.Key.Sexo, Count = g.Count() })
        .ToList();

        // Dicionários de saída
        var buckets = new[] {
        "baixoPeso","acimaPeso","riscoColesterolAlto","riscoHipertensao",
        "resistenciaInsulina","desequilibrioMuscular","indicePositivoSaude"
    };
        var dictTotalizadorSaudeMasculino = buckets.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorSaudeFeminino = buckets.ToDictionary(k => k, _ => 0m);
        var totalBuckets = buckets.ToDictionary(k => k, _ => 0m);

        // Aplica o mapeamento: 
        //    NORMAL -> indicePositivoSaude
        //    ABAIXODONORMAL -> desequilibrioMuscular
        //    SOBREPESO -> resistenciaInsulina
        //    OBESIDADE -> colesterol + hipertensão + resistência
        
        foreach (var row in classificacaoPorSexo)
        {
            void Inc(string key, decimal v)
            {
                if (row.Sexo == "M") dictTotalizadorSaudeMasculino[key] += v; else dictTotalizadorSaudeFeminino[key] += v;
                totalBuckets[key] += v;
            }

            switch (row.Classificacao)
            {
                case "NORMAL":
                    Inc("indicePositivoSaude", row.Count);
                    break;
                case "ABAIXODONORMAL":
                    Inc("desequilibrioMuscular", row.Count);
                    break;
                case "SOBREPESO":
                    Inc("resistenciaInsulina", row.Count);
                    break;
                case "OBESIDADE":
                    Inc("riscoColesterolAlto", row.Count);
                    Inc("riscoHipertensao", row.Count);
                    Inc("resistenciaInsulina", row.Count);
                    break;
            }
        }

        // Percentuais
        static Dictionary<string, decimal> Percentuais(Dictionary<string, decimal> src)
        {
            var sum = src.Values.Sum();
            return sum == 0
                ? src.Keys.ToDictionary(k => k, _ => 0m)
                : src.ToDictionary(kv => kv.Key, kv => Math.Round(100m * kv.Value / sum, 2));
        }

        var percTotalizadorSaudeMasculino = Percentuais(dictTotalizadorSaudeMasculino);
        var percTotalizadorSaudeFeminino = Percentuais(dictTotalizadorSaudeFeminino);

        // PercentualSaude por CLASSIFICAÇÃO (NORMAL/SOBREPESO/…)
        var totalPorClass = classificacaoPorSexo
            .GroupBy(c => c.Classificacao)
            .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.Count));

        var totalGeralClass = totalPorClass.Values.Sum();
        var dict = totalGeralClass == 0
            ? totalPorClass.Keys.ToDictionary(k => k, _ => 0m)
            : totalPorClass.ToDictionary(kv => kv.Key, kv => Math.Round(100m * kv.Value / totalGeralClass, 2));

        var result = new TotalizadorSexoSaudeDto
        {
            ValorTotalizadorSaudeMasculino = dictTotalizadorSaudeMasculino,
            ValorTotalizadorSaudeFeminino = dictTotalizadorSaudeFeminino,
            PercTotalizadorSaudeMasculino = percTotalizadorSaudeMasculino,
            PercTotalizadorSaudeFeminino = percTotalizadorSaudeFeminino,
            PercentualSaude = dict
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

            return YearsOld > 18 ? 99 : YearsOld < 4 ? 4 : YearsOld;
        }
        catch
        {
            return 0;
        }
    }
}

