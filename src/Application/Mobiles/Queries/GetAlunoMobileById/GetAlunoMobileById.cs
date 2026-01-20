using DnaBrasilApi.Application.Alunos.Queries;
using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Mobiles.Queries.GetAlunoMobileById;

public record GetAlunoMobileByIdQuery : IRequest<AlunoMobileDto>
{
    public required int Id { get; init; }
}

public class GetAlunoMobileByIdQueryHandler : IRequestHandler<GetAlunoMobileByIdQuery, AlunoMobileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAlunoMobileByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AlunoMobileDto> Handle(GetAlunoMobileByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.Alunos
            .Where(x => x.Id == request.Id)
            .AsNoTracking()
            .ProjectTo<AlunoMobileDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return result! == null ? throw new ArgumentNullException(nameof(result)) : result;
    }
}
