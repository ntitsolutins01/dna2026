using System.Globalization;
using DnaBrasilApi.Application.Alunos.Services;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Alunos.Commands.UpdateAluno;

public record UpdateAlunoCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string? AspNetUserId { get; init; }
    public string? Nome { get; init; }
    public string? Email { get; init; }
    public string? Sexo { get; init; }
    public string? DtNascimento { get; init; }
    public string? NomeMae { get; init; }
    public string? NomePai { get; init; }
    public string? Cpf { get; init; }
    public string? Telefone { get; init; }
    public string? Celular { get; init; }
    public string? Cep { get; init; }
    public string? Endereco { get; init; }
    public string? Numero { get; init; }
    public string? Bairro { get; init; }
    public string? RedeSocial { get; init; }
    public string? NomeFoto { get; init; }
    public byte[]? ByteImage { get; init; }
    public byte[]? QrCode { get; set; }
    public bool Status { get; init; }
    public bool Habilitado { get; init; }
    public int? MunicipioId { get; init; }
    public int? LocalidadeId { get; init; }
    public int? DeficienciaId { get; init; }
    public int? LinhaAcaoId { get; init; }
    public int? ParceiroId { get; init; }
    public string? Etnia { get; set; }
    public int? ProfissionalId { get; set; }
    public string? ModalidadesIds { get; init; }
    public int? SerieId { get; init; }
}

public class UpdateAlunoCommandHandler : IRequestHandler<UpdateAlunoCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IAlunoValidationService _validationService;

    public UpdateAlunoCommandHandler(IApplicationDbContext context, IAlunoValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public async Task<bool> Handle(UpdateAlunoCommand request, CancellationToken cancellationToken)
    {
   
        var entity = await _context.Alunos
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        // Usa dados do request OU dados do banco
        var nomeParaValidar = request.Nome ?? entity!.Nome;
        var dtNascimentoParaValidar = request.DtNascimento ?? entity.DtNascimento.ToString("dd/MM/yyyy");
        var cpfParaValidar = request.Cpf ?? entity.Cpf;
        var nomeMaeParaValidar = request.NomeMae ?? entity.NomeMae;
        var nomePaiParaValidar = request.NomePai ?? entity.NomePai;

        // Valida duplicidade SOMENTE se todos os campos críticos existirem
        if (!string.IsNullOrWhiteSpace(nomeParaValidar) && 
            !string.IsNullOrWhiteSpace(dtNascimentoParaValidar) && 
            !string.IsNullOrWhiteSpace(cpfParaValidar))
        {
            await _validationService.ValidarDuplicidadeAsync(
                nomeParaValidar,
                dtNascimentoParaValidar,
                cpfParaValidar,
                nomeMaeParaValidar,
                nomePaiParaValidar,
                cancellationToken,
                alunoIdParaIgnorar: request.Id);
        }

        // Load referenced entities only if ids provided
        Deficiencia? deficiencia = null;
        if (request.DeficienciaId.HasValue && request.DeficienciaId.Value > 0)
        {
            deficiencia = await _context.Deficiencias.FindAsync(new object[] { request.DeficienciaId }, cancellationToken);
            Guard.Against.NotFound(request.DeficienciaId.Value, deficiencia);
        }

        Municipio? municipio = null;
        if (request.MunicipioId.HasValue && request.MunicipioId.Value > 0)
        {
            municipio = await _context.Municipios.FindAsync(new object[] { request.MunicipioId }, cancellationToken);
            Guard.Against.NotFound(request.MunicipioId.Value, municipio);
        }

        Localidade? localidade = null;
        if (request.LocalidadeId.HasValue && request.LocalidadeId.Value > 0)
        {
            localidade = await _context.Localidades.FindAsync(new object[] { request.LocalidadeId }, cancellationToken);
            Guard.Against.NotFound(request.LocalidadeId.Value, localidade);
        }

        Profissional? profissional = null;
        if (request.ProfissionalId.HasValue && request.ProfissionalId.Value > 0)
        {
            profissional = await _context.Profissionais.FindAsync(new object[] { request.ProfissionalId }, cancellationToken);
            Guard.Against.NotFound(request.ProfissionalId.Value, profissional);
        }

        LinhaAcao? linhaAcao = null;
        if (request.LinhaAcaoId.HasValue && request.LinhaAcaoId.Value > 0)
        {
            linhaAcao = await _context.LinhasAcoes.FindAsync(new object[] { request.LinhaAcaoId }, cancellationToken);
            Guard.Against.NotFound(request.LinhaAcaoId.Value, linhaAcao);
        }

        Parceiro? parceiro = null;
        if (request.ParceiroId.HasValue && request.ParceiroId.Value > 0)
        {
            parceiro = await _context.Parceiros.FindAsync(new object[] { request.ParceiroId }, cancellationToken);
            Guard.Against.NotFound(request.ParceiroId.Value, parceiro);
        }

        Serie? serie = null;
        if (request.SerieId.HasValue && request.SerieId.Value > 0)
        {
            serie = await _context.Series.FindAsync(new object[] { request.SerieId }, cancellationToken);
            Guard.Against.NotFound(request.SerieId.Value, serie);
        }

        // Conditional updates (avoid overwriting with null unless intentional)
        if (request.AspNetUserId != null) entity!.AspNetUserId = request.AspNetUserId;
        if (request.Nome != null) entity!.Nome = request.Nome;
        if (request.Email != null) entity!.Email = request.Email;
        if (request.Sexo != null) entity!.Sexo = request.Sexo;
        if (request.Etnia != null) entity!.Etnia = request.Etnia;
        if (!string.IsNullOrWhiteSpace(request.DtNascimento))
        {
            if (DateTime.TryParseExact(request.DtNascimento, "dd/MM/yyyy", CultureInfo.CreateSpecificCulture("pt-BR"), DateTimeStyles.None, out var dt))
            {
                entity!.DtNascimento = dt;
            }
            else
            {
                throw new FormatException("DtNascimento em formato inválido. Use dd/MM/yyyy.");
            }
        }
        entity!.NomeMae = request.NomeMae ?? entity.NomeMae;
        entity.NomePai = request.NomePai ?? entity.NomePai;
        entity.Cpf = request.Cpf ?? entity.Cpf;
        entity.Cep = request.Cep ?? entity.Cep;
        entity.Telefone = request.Telefone ?? entity.Telefone;
        entity.Celular = request.Celular ?? entity.Celular;
        entity.Endereco = request.Endereco ?? entity.Endereco;
        entity.Numero = request.Numero ?? entity.Numero;
        entity.Bairro = request.Bairro ?? entity.Bairro;
        entity.Status = request.Status; // explicit flags
        entity.Habilitado = request.Habilitado;
        entity.NomeFoto = request.NomeFoto ?? entity.NomeFoto;
        if (request.ByteImage != null) entity.ByteImage = request.ByteImage;
        if (request.QrCode != null) entity.QrCode = request.QrCode;

        // Assign navigations if loaded
        if (deficiencia != null) entity.Deficiencia = deficiencia;
        if (linhaAcao != null) entity.LinhaAcao = linhaAcao;
        if (profissional != null) entity.Profissional = profissional;
        if (municipio != null) entity.Municipio = municipio;
        if (localidade != null) entity.Localidade = localidade;
        if (parceiro != null) entity.Parceiro = parceiro;
        if (serie != null) entity.Serie = serie;

        // Modalidades (replace set) - ensure not null string
        var listAlunoModalidades = new List<AlunoModalidade>();
        if (!string.IsNullOrWhiteSpace(request.ModalidadesIds))
        {
            int[] arrIds = request.ModalidadesIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            listAlunoModalidades.AddRange(arrIds.Select(id => new AlunoModalidade { ModalidadeId = id, AlunoId = entity.Id }));
            entity.AlunoModalidades = listAlunoModalidades;
        }

        var result = await _context.SaveChangesAsync(cancellationToken);
        return result >= 1;
    }
}
