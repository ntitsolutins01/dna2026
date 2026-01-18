using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Municipios.Queries.GetMunicipiosByUfComLocalidades;

/// <summary>
/// Query para buscar Municípios de uma UF que possuem Localidades vinculadas
/// </summary>
public record GetMunicipiosByUfComLocalidadesQuery : IRequest<List<MunicipioDto>>
{
    public required string Uf { get; init; }
}

public class GetMunicipiosByUfComLocalidadesQueryHandler : IRequestHandler<GetMunicipiosByUfComLocalidadesQuery, List<MunicipioDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMunicipiosByUfComLocalidadesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<MunicipioDto>> Handle(GetMunicipiosByUfComLocalidadesQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Municipios
            .Where(m => m.Estado!.Sigla == request.Uf 
                && _context.Localidades.Any(l => l.MunicipioId == m.Id && l.Status))
            .AsNoTracking()
            .ProjectTo<MunicipioDto>(_mapper.ConfigurationProvider)
            .OrderBy(t => t.Nome)
            .ToListAsync(cancellationToken);

        return result == null ? throw new ArgumentNullException(nameof(result)) : result;
    }
}
