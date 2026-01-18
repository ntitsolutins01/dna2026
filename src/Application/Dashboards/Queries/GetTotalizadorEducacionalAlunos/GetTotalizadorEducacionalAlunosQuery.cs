using System.Linq;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;
using DnaBrasilApi.Domain.Enums;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorEducacionalAlunos;
//[Authorize]
public record GetTotalizadorEducacionalAlunosQuery : IRequest<TotalizadorEducacionalDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorEducacionalAlunosQueryHandler : IRequestHandler<GetTotalizadorEducacionalAlunosQuery, TotalizadorEducacionalDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorEducacionalAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TotalizadorEducacionalDto> Handle(GetTotalizadorEducacionalAlunosQuery request, CancellationToken cancellationToken)
    {
        var categorias = new[] { "DEFASAGEM", "INTERMEDIARIO", "ADEQUADO" };

        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!)
            .AsNoTracking()
            .Select(a => new { a.Id });

        // EncaminhamentoId -> Parametro
        var encaminhamentoMap = await _context.Encaminhamentos.AsNoTracking()
            .Where(e => e.TipoLaudo.Id == (int)EnumTipoLaudo.Educacional) 
            .Select(e => new { e.Id, e.Parametro })
            .ToDictionaryAsync(x => x.Id, x => x.Parametro, cancellationToken);

        // ====== MAIS RECENTE POR ALUNO E POR MATÉRIA ======

        // Último laudo por aluno COM Matemática
        var lastMatByAluno = await (
            from l in _context.Laudos.AsNoTracking()
            join a in alunosFiltrados on l.Aluno.Id equals a.Id
            where l.EducacionalMatematicaId != null
            group l by l.Aluno.Id into g
            select new
            {
                AlunoId = g.Key,
                MatId = g.OrderByDescending(x => x.Id).Select(x => x.EducacionalMatematicaId).FirstOrDefault()
            }
        ).ToListAsync(cancellationToken);

        // Último laudo por aluno COM Português
        var lastPtByAluno = await (
            from l in _context.Laudos.AsNoTracking()
            join a in alunosFiltrados on l.Aluno.Id equals a.Id
            where l.EducacionalPortuguesId != null
            group l by l.Aluno.Id into g
            select new
            {
                AlunoId = g.Key,
                PtId = g.OrderByDescending(x => x.Id).Select(x => x.EducacionalPortuguesId).FirstOrDefault()
            }
        ).ToListAsync(cancellationToken);

        // Junta todos os Educacionais necessários e busca de uma vez
        var educIds = lastMatByAluno.Select(x => x.MatId)
            .Concat(lastPtByAluno.Select(x => x.PtId))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var educacionais = await _context.Educacionais.AsNoTracking()
            .Where(e => educIds.Contains(e.Id))
            .Select(e => new { e.Id, EncId = (int?)e.Encaminhamento!.Id })
            .ToDictionaryAsync(x => x.Id, x => x.EncId, cancellationToken);

        // Dicionários de saída (quantidade de alunos)
        var dictMat = categorias.ToDictionary(k => k, _ => 0m);
        var dictPt = categorias.ToDictionary(k => k, _ => 0m);

        // Conta Matemática
        foreach (var item in lastMatByAluno)
        {
            if (item.MatId is int matId &&
                educacionais.TryGetValue(matId, out var encIdNullable) &&
                encIdNullable is int encId &&
                encaminhamentoMap.TryGetValue(encId, out var param))
            {
                var key = (param ?? "").Trim().ToUpperInvariant();
                if (dictMat.ContainsKey(key)) dictMat[key] += 1;
            }
        }

        // Conta Português
        foreach (var item in lastPtByAluno)
        {
            if (item.PtId is int ptId &&
                educacionais.TryGetValue(ptId, out var encIdNullable) &&
                encIdNullable is int encId &&
                encaminhamentoMap.TryGetValue(encId, out var param))
            {
                var key = (param ?? "").Trim().ToUpperInvariant();
                if (dictPt.ContainsKey(key)) dictPt[key] += 1;
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

        var percMat = Percentuais(dictMat);
        var percPt = Percentuais(dictPt);

        // (Opcional) Geral somando as duas matérias
        var totalGeral = categorias.ToDictionary(k => k, k => dictMat[k] + dictPt[k]);
        var percGeral = Percentuais(totalGeral);

        return new TotalizadorEducacionalDto
        {
            ValorTotalizadorEducacionalMatematica = dictMat,
            ValorTotalizadorEducacionalPortugues = dictPt,
            PercTotalizadorEducacionalMatematica = percMat,
            PercTotalizadorEducacionalPortugues = percPt,
            PercentualEducacional = percGeral
        };
    }


}

