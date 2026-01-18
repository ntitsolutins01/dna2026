using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorConsumoAlimentarAlunos;
//[Authorize]
public record GetTotalizadorConsumoAlimentarAlunosQuery : IRequest<TotalizadorConsumoAlimentarDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class
    GetTotalizadorConsumoAlimentarAlunosQueryHandler : IRequestHandler<GetTotalizadorConsumoAlimentarAlunosQuery,
    TotalizadorConsumoAlimentarDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorConsumoAlimentarAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorConsumoAlimentarDto> Handle(GetTotalizadorConsumoAlimentarAlunosQuery request,
        CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .AsNoTracking()
            .Select(a => new { a.Id, a.Sexo });

        // Laudos com ConsumoAlimentar (CSV) + sexo do aluno
        var laudosBase = (
        from l in _context.Laudos.AsNoTracking()
        where l.ConsumoAlimentar != null
        join a in alunosFiltrados on l.Aluno.Id equals a.Id
        select new
        {
            a.Sexo,
            Csv = l.ConsumoAlimentar!.Respostas
        }
        ).ToList();

        // Lookup de Respostas: RespostaId -> Peso (somente Quadrante 1)
        var respostaPesoQ1 = _context.Respostas.AsNoTracking()
            .Where(r => r.Questionario.Quadrante == 1)
            .Select(r => new { r.Id, Peso = (decimal)r.ValorPesoResposta })
            .ToDictionary(x => x.Id, x => x.Peso);

        // Métricas (TipoLaudo 8, Quadrante 1)
        var metricasQ1 = _context.TextosLaudos.AsNoTracking()
            .Where(t => t.TipoLaudo.Id == 8 && t.Quadrante == 1)
            .Select(t => new { t.PontoInicial, t.PontoFinal, t.Aviso })
            .ToList();

        // Dicionários de saída
        var categoriasConsumoAlimentar = new[] { "HabitosSaudaveis", "HabitosNaoSaudaveis", "HabitosSatisfatorios", "BonsHabitosAlimentares" };
        
        var totalConsumoAlimentar = categoriasConsumoAlimentar.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorConsumoAlimentarMasculino = categoriasConsumoAlimentar.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorConsumoAlimentarFeminino = categoriasConsumoAlimentar.ToDictionary(k => k, _ => 0m);

        // Processa cada laudo
        foreach (var l in laudosBase)
        {
            if (string.IsNullOrWhiteSpace(l.Csv)) continue;

            decimal somaQ1 = 0m;

            foreach (var tok in l.Csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(tok.Trim(), out var id)) continue;
                if (respostaPesoQ1.TryGetValue(id, out var peso)) somaQ1 += peso;
            }

            // Enquadra na métrica
            var met = metricasQ1.FirstOrDefault(m => somaQ1 >= m.PontoInicial && somaQ1 <= m.PontoFinal);
            if (met == null) continue;

            var label = met.Aviso?.Split('.')[0]?.Trim(); // segue sua regra original
            if (string.IsNullOrEmpty(label) || !totalConsumoAlimentar.ContainsKey(label)) continue;

            totalConsumoAlimentar[label] += 1;
            if (l.Sexo == "M") dictTotalizadorConsumoAlimentarMasculino[label] += 1; else dictTotalizadorConsumoAlimentarFeminino[label] += 1;
        }

        // Percentuais
        static Dictionary<string, decimal> Percentuais(Dictionary<string, decimal> src)
        {
            var sum = src.Values.Sum();
            return sum == 0
                ? src.Keys.ToDictionary(k => k, _ => 0m)
                : src.ToDictionary(kv => kv.Key, kv => Math.Round(100m * kv.Value / sum, 2));
        }

        var percTotalizadorConsumoAlimentarMasculino = Percentuais(dictTotalizadorConsumoAlimentarMasculino);
        var percTotalizadorConsumoAlimentarFeminino = Percentuais(dictTotalizadorConsumoAlimentarFeminino);
        var percConsumoAlimentar = Percentuais(totalConsumoAlimentar);

        var result = new TotalizadorConsumoAlimentarDto
        {
            ValorTotalizadorConsumoAlimentarMasculino = dictTotalizadorConsumoAlimentarMasculino,
            ValorTotalizadorConsumoAlimentarFeminino = dictTotalizadorConsumoAlimentarFeminino,
            PercTotalizadorConsumoAlimentarMasculino = percTotalizadorConsumoAlimentarMasculino,
            PercTotalizadorConsumoAlimentarFeminino = percTotalizadorConsumoAlimentarFeminino,
            PercentualConsumoAlimentar = percConsumoAlimentar
        };

        return Task.FromResult(result);
    }

}
