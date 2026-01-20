using DnaBrasilApi.Application.Alunos.Queries;
using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Mobiles.Queries.GetAlunoImageById;

public record GetAlunoImageByIdQuery : IRequest<AlunoByteDto>
{
    public required int Id { get; init; }
}

public class GetAlunoImageByIdQueryHandler : IRequestHandler<GetAlunoImageByIdQuery, AlunoByteDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAlunoImageByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AlunoByteDto> Handle(GetAlunoImageByIdQuery request, CancellationToken cancellationToken)
    {
        var result = _context.Alunos
            .Where(x => x.Id == request.Id)
            .AsNoTracking()
            .Select(s=> s.ByteImage)
            .FirstOrDefault()!;

        var aluno = new AlunoByteDto() { Id = request.Id, ByteImage = result };

        return await Task.FromResult(aluno);
    }
}
