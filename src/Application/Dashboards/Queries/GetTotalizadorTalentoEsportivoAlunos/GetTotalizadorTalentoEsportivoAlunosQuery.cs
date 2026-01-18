using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;
using MediatR;

namespace DnaBrasilApi.Application.Dashboards.Queries.GetTotalizadorTalentoEsportivoAlunos;
//[Authorize]
public record GetTotalizadorTalentoEsportivoAlunosQuery : IRequest<TotalizadorTalentoDto>
{
    public DashboardDto? SearchFilter { get; init; }

}

public class GetTotalizadorTalentoEsportivoAlunosQueryHandler : IRequestHandler<GetTotalizadorTalentoEsportivoAlunosQuery, TotalizadorTalentoDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTotalizadorTalentoEsportivoAlunosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<TotalizadorTalentoDto> Handle(GetTotalizadorTalentoEsportivoAlunosQuery request, CancellationToken cancellationToken)
    {
        var alunosFiltrados = DashboardFilterPrincipal.FiltrarAlunos(_context, request.SearchFilter!);

        var modalidades = _context.Modalidades
            .Where(x => x.Status == true)
            .ToList();

        var modalidadeIdToNome = modalidades.ToDictionary(m => m.Id, m => m.Nome!);

        var verificaAlunos = alunosFiltrados.Select(x => x.Id);

        Dictionary<string, decimal> dict = new();
        Dictionary<string, decimal> dictTalentoMasculino = new();
        Dictionary<string, decimal> dictTalentoFeminino = new();

        foreach (Modalidade item in modalidades)
        {
            dictTalentoMasculino.Add(item.Nome!, 0);
            dictTalentoFeminino.Add(item.Nome!, 0);
            dict.Add(item.Nome!, 0);
        }

        var laudos = _context.Laudos
            .AsNoTracking()
            .Where(l => l.TalentoEsportivo != null && verificaAlunos.Contains(l.Aluno.Id))
            .Include(l => l.Aluno)
            .Include(l => l.TalentoEsportivo);

        var agrupados = laudos
            .Where(l =>
                l.TalentoEsportivo != null &&
                l.TalentoEsportivo.Encaminhamento != null &&
                l.Modalidade != null &&
                l.Aluno != null &&
                l.Aluno.Sexo != null) 
            .GroupBy(l => new
            {
                ModalidadeId = l.Modalidade!.Id,
                modalidadeIdToNome = l.TalentoEsportivo!.EncaminhamentoTexo,
                Sexo = l.Aluno!.Sexo!
            })
            .Select(g => new
            {
                ModalidadeId = g.Key.ModalidadeId,
                Sexo = g.Key.Sexo,
                Quantidade = g.Count()
            })
            .ToList();

        // Inicializa dicionários
        var dictTotalizadorTalentoMasculino = modalidades.ToDictionary(m => m.Nome!, _ => 0m);
        var dictTotalizadorTalentoFeminino = modalidades.ToDictionary(m => m.Nome!, _ => 0m);
        var totalTalentos = modalidades.ToDictionary(m => m.Nome!, _ => 0m);

        foreach (var item in agrupados)
        {
            if (!modalidadeIdToNome.TryGetValue(item.ModalidadeId, out var nomeMod) || string.IsNullOrEmpty(nomeMod))
                continue;

            if (item.Sexo == "M")
                dictTotalizadorTalentoMasculino[nomeMod] += item.Quantidade;
            else
                dictTotalizadorTalentoFeminino[nomeMod] += item.Quantidade;

            totalTalentos[nomeMod] += item.Quantidade;
        }

        // Percentuais
        static Dictionary<string, decimal> Percentuais(IDictionary<string, decimal> valores)
        {
            var total = valores.Values.Sum();
            if (total == 0) return valores.ToDictionary(kv => kv.Key, kv => 0m);
            return valores.ToDictionary(kv => kv.Key, kv => Math.Round(100m * kv.Value / total, 2));
        }

        var percTotalizadorTalentoMasculino = Percentuais(dictTotalizadorTalentoMasculino);
        var percTotalizadorTalentoFeminino = Percentuais(dictTotalizadorTalentoFeminino);
        var percTalento = Percentuais(totalTalentos);

        var result = new TotalizadorTalentoDto
        {
            ValorTotalizadorTalentoMasculino = dictTotalizadorTalentoMasculino,
            ValorTotalizadorTalentoFeminino = dictTotalizadorTalentoFeminino,
            PercTotalizadorTalentoMasculino = percTotalizadorTalentoMasculino,
            PercTotalizadorTalentoFeminino = percTotalizadorTalentoFeminino,
            PercTalento = percTalento
        };

        return Task.FromResult(result);

    }

}

