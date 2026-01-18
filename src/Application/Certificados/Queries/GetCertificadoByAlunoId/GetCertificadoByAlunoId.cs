//using DnaBrasilApi.Application.Common.Interfaces;

//namespace DnaBrasilApi.Application.Certificados.Queries.GetCertificadoById;

//public record GetCertificadoByAlunoIdQuery : IRequest<CertificadoDto>
//{
//    public required int AlunoId { get; init; }
//}

//public class GetCertificadoByAlunoIdQueryHandler : IRequestHandler<GetCertificadoByAlunoIdQuery, CertificadoDto>
//{
//    private readonly IApplicationDbContext _context;
//    private readonly IMapper _mapper;

//    public GetCertificadoByAlunoIdQueryHandler(IApplicationDbContext context, IMapper mapper)
//    {
//        _context = context;
//        _mapper = mapper;
//    }

//    public async Task<CertificadoDto> Handle(GetCertificadoByAlunoIdQuery request, CancellationToken cancellationToken)
//    {
//        var result = await _context.Certificados
//            .Where(x => x.AlunoId == request.AlunoId)
//            .AsNoTracking()
//            .ProjectTo<CertificadoDto>(_mapper.ConfigurationProvider)
//            .FirstOrDefaultAsync(cancellationToken);

//        return result! == null ? throw new ArgumentNullException(nameof(result)) : result;
//    }
//}
