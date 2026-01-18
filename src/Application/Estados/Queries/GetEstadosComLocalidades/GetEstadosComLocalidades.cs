using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Estados.Queries.GetEstadosComLocalidades;

/// <summary>
/// Query para buscar Estados que possuem Municípios com Localidades vinculadas
/// </summary>
public record GetEstadosComLocalidadesQuery : IRequest<List<EstadoDto>>;

public class GetEstadosComLocalidadesQueryHandler : IRequestHandler<GetEstadosComLocalidadesQuery, List<EstadoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEstadosComLocalidadesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EstadoDto>> Handle(GetEstadosComLocalidadesQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Estados
            .Where(e => e.Municipios.Any(m => _context.Localidades.Any(l => l.MunicipioId == m.Id && l.Status)))
            .AsNoTracking()
            .ProjectTo<EstadoDto>(_mapper.ConfigurationProvider)
            .OrderBy(t => t.Nome)
            .ToListAsync(cancellationToken);

        return result == null ? throw new ArgumentNullException(nameof(result)) : result;
    }
}
