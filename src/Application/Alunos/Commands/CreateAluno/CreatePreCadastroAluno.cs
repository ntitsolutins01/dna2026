using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaBrasilApi.Application.Alunos.Commands.CreateAluno;

public record CreatePreCadastroAlunoCommand : IRequest<int>
{
    public required string Nome { get; init; }
    public required string DtNascimento { get; init; }
    public required string Sexo { get; init; }
    public required string Etnia { get; init; }
    public required int MunicipioId { get; init; }
    public required string Email { get; init; }
    public required string Celular { get; init; }
    public required string Cpf { get; init; }
    public required int DeficienciaId { get; init; }
    public required int LocalidadeId { get; init; }
    public string? Endereco { get; init; }
    public string? Numero { get; init; }
    public string? Bairro { get; init; }
    public string? Cep { get; init; }
    public string? Telefone { get; init; }
    public string? NomeMae { get; init; }
    public string? NomePai { get; init; }
    public required bool AutorizacaoSaida { get; init; }
    public required bool ParticipacaoProgramaCompartilhamentoDados { get; init; }
    public required bool UtilizacaoImagem { get; init; }
    public required bool AutorizacaoConsentimentoAssentimento { get; init; }
    public bool Habilitado => false;
    public bool Status => true;
}




