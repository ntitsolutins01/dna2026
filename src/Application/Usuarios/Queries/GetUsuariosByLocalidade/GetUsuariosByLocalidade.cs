using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Usuarios.Queries.GetUsuariosByLocalidade;
//[Authorize]
public record GetUsuariosByLocalidadeQuery : IRequest<List<UsuarioDto>>
{
    public required int LocalidadeId { get; init; }
}

public class GetUsuariosByLocalidadeQueryHandler : IRequestHandler<GetUsuariosByLocalidadeQuery, List<UsuarioDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUsuariosByLocalidadeQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<UsuarioDto>> Handle(GetUsuariosByLocalidadeQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Usuarios
            .Where(u => u.Localidade != null && u.Localidade.Id == request.LocalidadeId)
            .AsNoTracking()
            .ProjectTo<UsuarioDto>(_mapper.ConfigurationProvider)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return result!;
    }
}
