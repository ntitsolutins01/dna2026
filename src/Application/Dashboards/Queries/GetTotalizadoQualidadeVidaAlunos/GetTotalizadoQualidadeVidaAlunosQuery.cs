using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;
using DnaBrasilApi.Domain.Enums;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadoQualidadeVidaAlunos;
//[Authorize]
public record GetTotalizadoQualidadeVidaAlunosQuery : IRequest<TotalizadorQualidadeVidaDto>
{
    public DashboardDto? SearchFilter { get; init; }
}

public class
    GetTotalizadoQualidadeVidaAlunosQueryHandler : IRequestHandler<GetTotalizadoQualidadeVidaAlunosQuery,
    TotalizadorQualidadeVidaDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadoQualidadeVidaAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorQualidadeVidaDto> Handle(GetTotalizadoQualidadeVidaAlunosQuery request,
        CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .AsNoTracking()
            .Select(a => new { a.Id, a.Sexo });

        // Laudos com QualidadeDeVida (CSV) + sexo do aluno (JOIN pela FK)
        var laudos = (
            from l in _context.Laudos.AsNoTracking()
            where l.QualidadeDeVida != null
            join a in alunosFiltrados on l.Aluno.Id equals a.Id
            select new
            {
                a.Sexo,
                Csv = l.QualidadeDeVida!.Encaminhamentos
            }
        ).ToList();

        // Lookup de Encaminhamento: Id -> Parametro
        var encaminhamentoMap = _context.Encaminhamentos.AsNoTracking()
            .Where(e => e.TipoLaudo.Id == (int)EnumTipoLaudo.QualidadeVida)
            .Select(e => new { e.Id, Parametro = e.Parametro })
            .ToDictionary(x => x.Id, x => x.Parametro);

        // Dicionários de saída 
        var categorias = new[]
        {
        "BemEstarFisico","MalEstarFisico","AutoEstima","BaixaAutoEstima",
        "FuncionamentoHarmonico","Conflitos","ContextosFavorecedores","ContextosNaoFavorecedores"
        };

        var totalQualidade = categorias.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorQualidadeMasculino = categorias.ToDictionary(k => k, _ => 0m);
        var dictTotalizadorQualidadeFeminino = categorias.ToDictionary(k => k, _ => 0m);

        // Processamento em memória
        foreach (var l in laudos)
        {
            if (string.IsNullOrWhiteSpace(l.Csv)) continue;

            foreach (var tok in l.Csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(tok.Trim(), out var id)) continue;
                if (!encaminhamentoMap.TryGetValue(id, out var parametro)) continue;

                // Se quiser garantir case/trim:
                parametro = parametro?.Trim();
                if (string.IsNullOrEmpty(parametro) || !totalQualidade.ContainsKey(parametro)) continue;

                totalQualidade[parametro] += 1;
                if (l.Sexo == "M") dictTotalizadorQualidadeMasculino[parametro] += 1; else dictTotalizadorQualidadeFeminino[parametro] += 1;
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

        var percTotalizadorQualidadeVidaMasculino = Percentuais(dictTotalizadorQualidadeMasculino);
        var percTotalizadorQualidadeVidaFeminino = Percentuais(dictTotalizadorQualidadeFeminino);
        var percQualidadeVida = Percentuais(totalQualidade);

        var result = new TotalizadorQualidadeVidaDto
        {
            ValorTotalizadorQualidadeVidaMasculino = dictTotalizadorQualidadeMasculino,
            ValorTotalizadorQualidadeVidaFeminino = dictTotalizadorQualidadeFeminino,
            PercTotalizadorQualidadeVidaMasculino = percTotalizadorQualidadeVidaMasculino,
            PercTotalizadorQualidadeVidaFeminino = percTotalizadorQualidadeVidaFeminino,
            PercentualQualidade = percQualidadeVida
        };

        return Task.FromResult(result);
    }
   
}
