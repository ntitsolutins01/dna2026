using System.Buffers.Text;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorDesempenhoAlunos;
//[Authorize]
public record GetTotalizadorDesempenhoAlunosQuery : IRequest<TotalizadorDesempenhoDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorDesempenhoAlunosQueryHandler : IRequestHandler<GetTotalizadorDesempenhoAlunosQuery, TotalizadorDesempenhoDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorDesempenhoAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }


    public Task<TotalizadorDesempenhoDto> Handle(GetTotalizadorDesempenhoAlunosQuery request, CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .AsNoTracking()
            .Select(a => new { a.Id, a.Sexo, a.DtNascimento });

        var hoje = DateTime.Today;

        // Alunos com TalentoEsportivo + idade calculada no SQL
        var baseTalentoEsportivo =
            from l in _context.Laudos.AsNoTracking()
            where l.TalentoEsportivo != null
            join a in alunosFiltrados on l.Aluno.Id equals a.Id
            select new
            {
                a.Sexo,
                Idade = (hoje.Year - a.DtNascimento.Year)
                        - (a.DtNascimento.AddYears(hoje.Year - a.DtNascimento.Year) > hoje ? 1 : 0),
                TE = l.TalentoEsportivo!
            };

        var textos = _context.TextosLaudos.AsNoTracking()
            .Where(t => t.Status && t.TipoLaudo.Id == 4
                    && (t.Aviso == "Excelente" || t.Aviso == "Muito Bom" || t.Aviso == "Bom"));

        // Para cada classificação, compara o valor correspondente com [PontoInicial, PontoFinal]
        var qVelocidade =
            from x in baseTalentoEsportivo
            join t in textos.Where(t => t.Classificacao == "Velocidade")
                on x.Sexo equals t.Sexo
            where t.Idade == x.Idade
               && x.TE.Velocidade >= t.PontoInicial && x.TE.Velocidade <= t.PontoFinal
            select new { x.Sexo, Bucket = "velocidade" };

        var qImpulsao =
            from x in baseTalentoEsportivo
            join t in textos.Where(t => t.Classificacao == "Impulsão")
                on x.Sexo equals t.Sexo
            where t.Idade == x.Idade
               && x.TE.ImpulsaoHorizontal >= t.PontoInicial && x.TE.ImpulsaoHorizontal <= t.PontoFinal
            select new { x.Sexo, Bucket = "forcaExplosiva" };

        var qShuttle =
        from x in baseTalentoEsportivo
        join t in textos.Where(t => t.Classificacao == "Agilidade ou Shuttle run")
            on x.Sexo equals t.Sexo
        where t.Idade == x.Idade
           && x.TE.ShuttleRun >= t.PontoInicial && x.TE.ShuttleRun <= t.PontoFinal
        select new { x.Sexo, Bucket = "shutlleRun" };

        var qFlex =
            from x in baseTalentoEsportivo
            join t in textos.Where(t => t.Classificacao == "Flexibilidade")
                on x.Sexo equals t.Sexo
            where t.Idade == x.Idade
               && x.TE.Flexibilidade >= t.PontoInicial && x.TE.Flexibilidade <= t.PontoFinal
            select new { x.Sexo, Bucket = "flexibilidadeMuscular" };

        var qPreensao =
            from x in baseTalentoEsportivo
            join t in textos.Where(t => t.Classificacao == "Preensão Manual")
                on x.Sexo equals t.Sexo
            where t.Idade == x.Idade
               && x.TE.PreensaoManual >= t.PontoInicial && x.TE.PreensaoManual <= t.PontoFinal
            select new { x.Sexo, Bucket = "forcaMembrosSup" };

        var qVo2 =
            from x in baseTalentoEsportivo
            join t in textos.Where(t => t.Classificacao == "Vo2 Max")
                on x.Sexo equals t.Sexo
            where t.Idade == x.Idade
               && x.TE.Vo2Max >= t.PontoInicial && x.TE.Vo2Max <= t.PontoFinal
            select new { x.Sexo, Bucket = "aptidaoCardio" };

        var qPrancha =
            from x in baseTalentoEsportivo
            join t in textos.Where(t => t.Classificacao == "Prancha (ABD)")
                on x.Sexo equals t.Sexo
            where t.Idade == x.Idade
               && x.TE.Abdominal >= t.PontoInicial && x.TE.Abdominal <= t.PontoFinal
            select new { x.Sexo, Bucket = "prancha" };

        var agregados = qVelocidade
            .Concat(qImpulsao).Concat(qShuttle).Concat(qFlex)
            .Concat(qPreensao).Concat(qVo2).Concat(qPrancha)
            .GroupBy(x => new { x.Bucket, x.Sexo })
            .Select(g => new { g.Key.Bucket, g.Key.Sexo, Count = g.Count() })
            .ToList();


        // Ddicionários com zeros
        var buckets = new[] {
        "velocidade","flexibilidadeMuscular","forcaMembrosSup",
        "forcaExplosiva","aptidaoCardio","shutlleRun","prancha"
    };

        var totalGeralDesempenho = buckets.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorDesempenhoMasculino = buckets.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorDesempenhoFeminino = buckets.ToDictionary(k => k, _ => 0m);

        foreach (var row in agregados)
        {
            if (row.Sexo == "M") dictTotalizadorDesempenhoMasculino[row.Bucket] += row.Count;
            else dictTotalizadorDesempenhoFeminino[row.Bucket] += row.Count;
            totalGeralDesempenho[row.Bucket] += row.Count;
        }

        // Percentuais
        static Dictionary<string, decimal> Percentuais(Dictionary<string, decimal> src)
        {
            var sum = src.Values.Sum();
            return sum == 0
                ? src.Keys.ToDictionary(k => k, _ => 0m)
                : src.ToDictionary(kv => kv.Key, kv => Math.Round(100m * kv.Value / sum, 2));
        }

        var percTotalizadorDesempenhoMasculino = Percentuais(dictTotalizadorDesempenhoMasculino);
        var percTotalizadorDesempenhoFeminino = Percentuais(dictTotalizadorDesempenhoFeminino);
        var percDesempenho = Percentuais(totalGeralDesempenho);

        var result = new TotalizadorDesempenhoDto
        {
            ValorTotalizadorDesempenhoMasculino = dictTotalizadorDesempenhoMasculino,
            ValorTotalizadorDesempenhoFeminino = dictTotalizadorDesempenhoFeminino,
            PercTotalizadorDesempenhoMasculino = percTotalizadorDesempenhoMasculino,
            PercTotalizadorDesempenhoFeminino = percTotalizadorDesempenhoFeminino,
            PercDesempenho = percDesempenho
        };

        return Task.FromResult(result);
    }


    /// <summary>
    /// Calcula quantidade de anos passos com base em duas datas, caso encontre qualquer problema retorna 0 
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

            return YearsOld >= 18 ? 99 : YearsOld < 4 ? 4 : YearsOld;
        }
        catch
        {
            return 0;
        }
    }
}

