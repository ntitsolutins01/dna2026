using System.Globalization;
using DnaBrasilApi.Application.Alunos.Services;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Alunos.Commands.CreateAluno;

public class CreatePreCadastroAlunoCommandHandler : IRequestHandler<CreatePreCadastroAlunoCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IAlunoValidationService _validationService;

    public CreatePreCadastroAlunoCommandHandler(
        IApplicationDbContext context,
        IAlunoValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public async Task<int> Handle(CreatePreCadastroAlunoCommand request, CancellationToken cancellationToken)
    {
        // Valida duplicidade antes de criar
        await _validationService.ValidarDuplicidadeAsync(
            request.Nome,
            request.DtNascimento,
            request.Cpf,
            request.NomeMae,
            request.NomePai,
            cancellationToken);

        // Valida Município
        var municipio = await _context.Municipios.FindAsync(
            new object[] { request.MunicipioId }, cancellationToken);
        Guard.Against.NotFound(request.MunicipioId, municipio);

        // Valida Deficiência
        var deficiencia = await _context.Deficiencias.FindAsync(
            new object[] { request.DeficienciaId }, cancellationToken);
        Guard.Against.NotFound(request.DeficienciaId, deficiencia);

        // Valida Localidade
        var localidade = await _context.Localidades.FindAsync(
            new object[] { request.LocalidadeId }, cancellationToken);
        Guard.Against.NotFound(request.LocalidadeId, localidade);

        // Busca Fomento pela Localidade através da tabela FomentoLocalidades
        var fomento = await _context.FomentoLocalidades
            .Where(fl => fl.LocalidadeId == request.LocalidadeId && fl.Status)
            .Select(fl => fl.Fomento)
            .FirstOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.LocalidadeId, fomento);

        var entity = new Aluno
        {
            Nome = request.Nome,
            Email = request.Email,
            Cpf = request.Cpf,
            Celular = request.Celular,
            Telefone = request.Telefone,
            DtNascimento = DateTime.ParseExact(request.DtNascimento, "dd/MM/yyyy",
                CultureInfo.CreateSpecificCulture("pt-BR")),
            Sexo = request.Sexo,
            Etnia = request.Etnia,
            NomeMae = request.NomeMae,
            NomePai = request.NomePai,
            Endereco = request.Endereco,
            Numero = request.Numero,
            Bairro = request.Bairro,
            Cep = request.Cep,
            Municipio = municipio,
            Localidade = localidade,
            Fomento = fomento!,
            Deficiencia = deficiencia,
            AutorizacaoSaida = request.AutorizacaoSaida,
            ParticipacaoProgramaCompartilhamentoDados = request.ParticipacaoProgramaCompartilhamentoDados,
            UtilizacaoImagem = request.UtilizacaoImagem,
            AutorizacaoConsentimentoAssentimento = request.AutorizacaoConsentimentoAssentimento,
            Status = request.Status,      // true (pré-cadastro ativo)
            Habilitado = request.Habilitado, // false (ainda não habilitado)
            Convidado = false
        };

        _context.Alunos.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
