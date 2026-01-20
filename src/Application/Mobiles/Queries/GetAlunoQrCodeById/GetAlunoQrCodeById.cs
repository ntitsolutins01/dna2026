using DnaBrasilApi.Application.Alunos.Queries;
using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Mobiles.Queries.GetAlunoQrCodeById;

public record GetAlunoQrCodeByIdQuery : IRequest<AlunoByteDto>
{
    public required int Id { get; init; }
}

public class GetAlunoQrCodeByIdQueryHandler : IRequestHandler<GetAlunoQrCodeByIdQuery, AlunoByteDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAlunoQrCodeByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AlunoByteDto> Handle(GetAlunoQrCodeByIdQuery request, CancellationToken cancellationToken)
    {
        var result =  _context.Alunos
                .Where(x => x.Id == request.Id)
                .AsNoTracking()
                .Select(s=>s.QrCode)
                .FirstOrDefault()!;

        var aluno = new AlunoByteDto() { Id = request.Id, QrCode = result };

        return await Task.FromResult(aluno);
    }
}
