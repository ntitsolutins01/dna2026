using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorSaudeBucalAlunos;
//[Authorize]
public record GetTotalizadorSaudeBucalAlunosQuery : IRequest<TotalizadorSaudeBucalDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorSaudeBucalAlunosQueryHandler : IRequestHandler<GetTotalizadorSaudeBucalAlunosQuery, TotalizadorSaudeBucalDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorSaudeBucalAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorSaudeBucalDto> Handle(GetTotalizadorSaudeBucalAlunosQuery request, CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!);

        var laudosSlim = (
            from l in _context.Laudos.AsNoTracking()
            join a in alunosFiltrados.Select(x => new { x.Id, x.Sexo }) on l.Aluno.Id equals a.Id
            where l.SaudeBucal != null
            select new
            {
                a.Sexo,
                RespostasCsv = l.SaudeBucal!.Respostas
            }
            ).ToList();

        if (laudosSlim.Count == 0)
        {
            return Task.FromResult(new TotalizadorSaudeBucalDto
            {
                ValorTotalizadorSaudeBucalMasculino = new() { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 },
                ValorTotalizadorSaudeBucalFeminino = new() { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 },
                PercTotalizadorSaudeBucalMasculino = new() { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 },
                PercTotalizadorSaudeBucalFeminino = new() { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 },
                PercentualSaudeBucal = new() { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 }
            });
        }

        // Parse do CSV uma única vez por laudo
        var laudosParsed = laudosSlim.Select(s => new
        {
            s.Sexo,
            Ids = s.RespostasCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => int.Parse(x))
                                 .ToArray()
        }).ToList();

        // Busca TODAS as respostas e quadrantes
        var todosIds = laudosParsed.SelectMany(x => x.Ids).Distinct().ToList();

        var respostasDict = _context.Respostas
        .AsNoTracking()
        .Where(r => todosIds.Contains(r.Id))
        .Select(r => new { r.Id, r.ValorPesoResposta, r.Questionario.Quadrante })
        .ToList()
        .ToDictionary(r => r.Id, r => (valor: r.ValorPesoResposta, quadrante: r.Quadrante));

        // Métricas em memória
        var metricasQ1 = _context.TextosLaudos
          .AsNoTracking()
          .Where(x => x.TipoLaudo.Id == 5 && x.Quadrante == 1)
          .Select(x => new { x.PontoInicial, x.PontoFinal, x.Aviso })
          .ToList() 
          .Select(x => new
          {
              x.PontoInicial,
              x.PontoFinal,
              Categoria = (x.Aviso ?? string.Empty)
                  .Split(new[] { '.' }, 2, StringSplitOptions.None)[0] 
          })
          .ToList();


        // Contadores
        var totalSaudeBucal = new Dictionary<string, int> { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 };
        var totalizadorMasculino = new Dictionary<string, int> { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 };
        var totalizadorFeminino = new Dictionary<string, int> { ["CUIDADO"] = 0, ["ATENCAO"] = 0, ["MUITOBOM"] = 0 };

        // Processamento em memória: somatório do Quadrante 1 por laudo
        foreach (var lp in laudosParsed)
        {
            decimal somaQ1 = 0;
            foreach (var id in lp.Ids)
            {
                if (respostasDict.TryGetValue(id, out var r) && r.quadrante == 1)
                    somaQ1 += r.valor;
            }

            // Enquadra na métrica
            var met = metricasQ1.FirstOrDefault(m => somaQ1 >= m.PontoInicial && somaQ1 <= m.PontoFinal);
            if (met is null) continue;

            totalSaudeBucal[met.Categoria]++;

            if (lp.Sexo == "M")
                totalizadorMasculino[met.Categoria]++;
            else
                totalizadorFeminino[met.Categoria]++;
        }

        // Percentuais
        static Dictionary<string, decimal> Percentual(Dictionary<string, int> dic)
        {
            var total = dic.Values.Sum();
            if (total == 0)
                return dic.Keys.ToDictionary(k => k, _ => 0m);

            return dic.ToDictionary(
                kv => kv.Key,
                kv => Math.Round(100m * kv.Value / total, 2, MidpointRounding.AwayFromZero)
            );
        }

        var dictTotalizadorSaudeBucalMasculino = totalizadorMasculino.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value);
        var dictTotalizadorSaudeBucalFeminino = totalizadorFeminino.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value);
        var percTotalizadorSaudeBucalMasculino = Percentual(totalizadorMasculino);
        var percTotalizadorSaudeBucalFeminino = Percentual(totalizadorFeminino);
        var percSaudeBucal = Percentual(totalSaudeBucal);

        var result = new TotalizadorSaudeBucalDto
        {
            ValorTotalizadorSaudeBucalMasculino = dictTotalizadorSaudeBucalMasculino,
            ValorTotalizadorSaudeBucalFeminino = dictTotalizadorSaudeBucalFeminino,
            PercTotalizadorSaudeBucalMasculino = percTotalizadorSaudeBucalMasculino,
            PercTotalizadorSaudeBucalFeminino = percTotalizadorSaudeBucalFeminino,
            PercentualSaudeBucal = percSaudeBucal
        };

        return Task.FromResult(result);
    }

}

