using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Laudos.Queries.GetLaudosResumidosByFilter;

public record GetLaudosResumidosByFilterQuery : IRequest<List<LaudoResumidoDto>>
{
    public required LaudosResumidosFilterDto SearchFilter { get; init; }
}

public class GetLaudosResumidosByFilterQueryHandler : IRequestHandler<GetLaudosResumidosByFilterQuery, List<LaudoResumidoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLaudosResumidosByFilterQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<LaudoResumidoDto>> Handle(GetLaudosResumidosByFilterQuery request, CancellationToken cancellationToken)
    {
        // Validação básica do request
        if (request?.SearchFilter is null)
            throw new ArgumentNullException(nameof(request.SearchFilter));

        // Valida e converte a LocalidadeId
        if (!int.TryParse(request.SearchFilter.LocalidadeId, out var localidadeId))
            throw new ArgumentException("LocalidadeId inválido.", nameof(request.SearchFilter.LocalidadeId));

        // Monta a query base
        var laudosQuery = _context.Laudos
            .AsNoTracking()
            .Include(i => i.Aluno.Localidade)
            .Include(i => i.QualidadeDeVida)
            .Include(i => i.ConsumoAlimentar)
            .Include(i => i.TalentoEsportivo)
            .Include(i => i.Saude)
            .Include(i => i.SaudeBucal)
            .Include(i => i.Vocacional)
            .Include(i => i.Modalidade)
            .Include(i => i.EducacionalPortugues)
            .Include(i => i.EducacionalMatematica)
            .Where(x =>
                x.Modalidade != null &&
                x.StatusLaudo == "F" &&
                x.Aluno.Localidade != null &&
                x.Aluno.Localidade.Id == localidadeId);

        // Aplica filtros dinâmicos (datas, sexo, etc.) no IQueryable
        var laudosFiltrados = FilterLaudos(laudosQuery, request.SearchFilter, cancellationToken);

        // Projeta direto para o DTO, ordena e executa no banco
        var result = await laudosFiltrados
            .ProjectTo<LaudoResumidoDto>(_mapper.ConfigurationProvider)
            .OrderBy(o => o.TalentoEsportivo!.EncaminhamentoTexo)
            .ToListAsync(cancellationToken);

        return result;

    }

    private IQueryable<Laudo> FilterLaudos(IQueryable<Laudo> laudos, LaudosResumidosFilterDto search, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(search.FomentoId))
        {
            var fomento = Convert.ToInt32(search.FomentoId);

            laudos = laudos.Where(u => u.Aluno.Fomento!.Id == fomento);
        }

        if (!string.IsNullOrWhiteSpace(search.Estado))
        {
            laudos = laudos.Where(u => u.Aluno.Municipio!.Estado!.Sigla!.Contains(search.Estado));
        }

        if (!string.IsNullOrWhiteSpace(search.MunicipioId))
        {
            laudos = laudos.Where(u => u.Aluno.Municipio!.Id == Convert.ToInt32(search.MunicipioId));
        }

        if (!string.IsNullOrWhiteSpace(search.LocalidadeId))
        {
            laudos = laudos.Where(u => u.Aluno.Localidade!.Id == Convert.ToInt32(search.LocalidadeId));
        }

        if (!string.IsNullOrWhiteSpace(search.AlunoId))
        {
            laudos = laudos.Where(u => u.Aluno.Id == Convert.ToInt32(search.AlunoId));
        }

        if (search.PossuiFoto)
        {
            laudos = laudos.Where(u => u.Aluno.ByteImage != null);
        }

        if (search.Finalizado)
        {
            laudos = laudos.Where(u => u.StatusLaudo == "F");
        }

        return laudos;
    }
}
