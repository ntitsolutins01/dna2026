using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Estados.Queries.GetEstadosByMunicipioId;
//[Authorize]
public record GetEstadosByMunicipioIdQuery: IRequest<List<EstadoDto>>
{
    public required int MunicipioId { get; init; }
}

public class GetEstadosByMunicipioIdQueryHandler : IRequestHandler<GetEstadosByMunicipioIdQuery, List<EstadoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEstadosByMunicipioIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EstadoDto>> Handle(GetEstadosByMunicipioIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Estados
            .Where(u => u.Municipios.Any(m => m.Id == request.MunicipioId))
            .AsNoTracking()
            .ProjectTo<EstadoDto>(_mapper.ConfigurationProvider)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return result == null ? throw new ArgumentNullException(nameof(result)) : result;
    }
}
