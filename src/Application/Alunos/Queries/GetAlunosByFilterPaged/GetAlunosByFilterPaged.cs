using System.Globalization;
using DnaBrasilApi.Application.Common.Dtos;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;
using Microsoft.Extensions.Logging;


namespace DnaBrasilApi.Application.Alunos.Queries.GetAlunosByFilterPaged;

public sealed record GetAlunosByFilterPagedQuery : IRequest<PagedResult<AlunoCarteirinhaDto>>
{
 public AlunosFilterDto? SearchFilter { get; init; }
 public int Page { get; init; } =1;
 public int PageSize { get; init; } =20;
}

public class GetAlunosByFilterPagedQueryHandler : IRequestHandler<GetAlunosByFilterPagedQuery, PagedResult<AlunoCarteirinhaDto>>
{
 private readonly IApplicationDbContext _context;
 private readonly IMapper _mapper;
 private readonly ILogger<GetAlunosByFilterPagedQueryHandler> _logger;

 public GetAlunosByFilterPagedQueryHandler(IApplicationDbContext context, IMapper mapper, ILogger<GetAlunosByFilterPagedQueryHandler> logger)
 {
 _context = context;
 _mapper = mapper;
 _logger = logger;
 }

 public async Task<PagedResult<AlunoCarteirinhaDto>> Handle(GetAlunosByFilterPagedQuery request, CancellationToken cancellationToken)
 {
 var query = _context.Alunos.AsNoTracking();
 var filtered = FilterAlunos(query, request.SearchFilter);

 var total = await filtered.CountAsync(cancellationToken);

 var page = Math.Max(1, request.Page);
 var size = Math.Max(1, request.PageSize);
 var skip = (page -1) * size;

 // query para buscar apenas os dados necessários para carteirinha
 var baseQuery = filtered
 .OrderBy(a => a.Id)
 .Skip(skip)
 .Take(size)
 .Select(a => new
 {
 a.Id,
 a.Nome,
 a.DtNascimento,
 a.Celular,
 a.Cpf,
 a.ByteImage,
 a.QrCode,
 a.Sexo,
 MunicipioNome = a.Municipio != null ? a.Municipio.Nome : null,
 EstadoSigla = a.Municipio != null && a.Municipio.Estado != null ? a.Municipio.Estado.Sigla : null,
 NomeLocalidade = a.Localidade != null ? a.Localidade.Nome : null
 });

 var baseItems = await baseQuery.ToListAsync(cancellationToken);

 var alunoIds = baseItems.Select(x => x.Id).ToList();

 // busca modalidades em aluno_modalidades
 var modQuery = _context.AlunoModalidades
 .AsNoTracking()
 .Where(am => alunoIds.Contains(am.AlunoId) && am.Modalidade != null)
 .Select(am => new { AlunoId = am.AlunoId, ModalidadeNome = am.Modalidade!.Nome });

 var alunoModalidades = await modQuery.ToListAsync(cancellationToken);

 // monta Dto
 var items = baseItems.Select(b =>
 {
 var modalidadesFromAluno = alunoModalidades.Where(am => am.AlunoId == b.Id).Select(am => am.ModalidadeNome).Distinct().ToList();

 var modalidades = modalidadesFromAluno;

 return new AlunoCarteirinhaDto
 {
 Id = b.Id,
 Nome = b.Nome,
 DtNascimento = b.DtNascimento.ToString("dd/MM/yyyy"),
 Celular = b.Celular,
 Cpf = b.Cpf,
 Sexo = b.Sexo == "F" ? "Feminino" : (b.Sexo == "M" ? "Masculino" : "Não Definido"),
 Modalidades = modalidades == null || !modalidades.Any() ? string.Empty : string.Join(", ", modalidades),
 MunicipioEstado = !string.IsNullOrEmpty(b.MunicipioNome) ? b.MunicipioNome + " / " + (b.EstadoSigla ?? string.Empty) : string.Empty,
 NomeLocalidade = b.NomeLocalidade ?? string.Empty,
 ByteImage = b.ByteImage,
 QrCode = b.QrCode
 };
 }).ToList();

 return new PagedResult<AlunoCarteirinhaDto>
 {
 Page = page,
 PageSize = size,
 TotalCount = total,
 Items = items
 };
 }

 private IQueryable<Aluno> FilterAlunos(IQueryable<Aluno> alunos, AlunosFilterDto? search)
 {
 if (search is null) return alunos;

 static bool TryParseId(string? s, out int value) { value =0; return !string.IsNullOrWhiteSpace(s) && int.TryParse(s, out value); }

 if (TryParseId(search.LocalidadeId, out var localidadeId))
 alunos = alunos.Where(u => u.Localidade != null && u.Localidade.Id == localidadeId);

 else if (TryParseId(search.MunicipioId, out var municipioId))
 alunos = alunos.Where(u => u.Municipio != null && u.Municipio.Id == municipioId);

 else if (!string.IsNullOrWhiteSpace(search.Estado))
 {
 var estado = search.Estado.Trim();
 alunos = alunos.Where(u => u.Municipio != null && u.Municipio.Estado != null && u.Municipio.Estado.Sigla != null && u.Municipio.Estado.Sigla.Contains(estado));
 }

 else if (TryParseId(search.FomentoId, out var fomentoId))
 alunos = alunos.Where(u => u.Fomento != null && u.Fomento.Id == fomentoId);

 if (TryParseId(search.AlunoId, out var alunoId))
 alunos = alunos.Where(u => u.Id == alunoId);

 if (!string.IsNullOrWhiteSpace(search.Nome))
 {
 var nome = search.Nome.Trim();
 alunos = alunos.Where(u => u.Nome != null && EF.Functions.Like(u.Nome, $"%{nome}%"));
 }

 if (TryParseId(search.ProfissionalId, out var profissionalId))
 alunos = alunos.Where(u => u.Profissional != null && u.Profissional.Id == profissionalId);

 if (TryParseId(search.DeficienciaId, out var defId))
 alunos = alunos.Where(u => _context.Deficiencias.Any(d => d.Id == defId && d.Alunos != null && d.Alunos.Any(a => a.Id == u.Id)));

 if (!string.IsNullOrWhiteSpace(search.Etnia))
 {
 var etnia = search.Etnia.Trim();
 alunos = alunos.Where(u => u.Etnia != null && u.Etnia == etnia);
 }

 if (TryParseId(search.Matricula, out var matricula))
 alunos = alunos.Where(u => u.Id == matricula);

 if (!string.IsNullOrWhiteSpace(search.Sexo))
 {
 var sexo = search.Sexo.Trim();
 alunos = alunos.Where(u => u.Sexo != null && u.Sexo == sexo);
 }

 if (search.PossuiFoto)
 alunos = alunos.Where(u => u.ByteImage != null);

 if (TryParseId(search.SerieId, out var serieId))
 alunos = alunos.Where(u => u.Serie != null && u.Serie.Id == serieId);

 if (!string.IsNullOrWhiteSpace(search.Email))
 {
 var email = search.Email.Trim().ToUpperInvariant();
 alunos = alunos.Where(u => u.Email != null && u.Email.Trim().ToUpper() == email);
 }

 if (!string.IsNullOrWhiteSpace(search.DataNascimento) && DateTime.TryParseExact(search.DataNascimento.Trim(), "dd/MM/yyyy", CultureInfo.CreateSpecificCulture("pt-BR"), DateTimeStyles.None, out var data))
 alunos = alunos.Where(u => u.DtNascimento == data);

 if (!string.IsNullOrWhiteSpace(search.Cpf))
 {
 var cpf = search.Cpf.Trim();
 alunos = alunos.Where(u => u.Cpf != null && u.Cpf == cpf);
 }

 return alunos;
 }
}
