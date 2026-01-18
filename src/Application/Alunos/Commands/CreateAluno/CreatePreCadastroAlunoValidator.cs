using DnaBrasilApi.Domain.Validation;

namespace DnaBrasilApi.Application.Alunos.Commands.CreateAluno;

public class CreatePreCadastroAlunoValidator : AbstractValidator<CreatePreCadastroAlunoCommand>
{
    public CreatePreCadastroAlunoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome não pode ter mais de 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .MaximumLength(100).WithMessage("Email não pode ter mais de 100 caracteres.")
            .Must(ValidationHelpers.EmailValido)
            .WithMessage("Email inválido.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.")
            .Must(ValidationHelpers.CpfValido)
            .WithMessage("CPF inválido.");

        RuleFor(x => x.Celular)
            .NotEmpty().WithMessage("Celular é obrigatório.")
            .Must(ValidationHelpers.CelularValido)
            .WithMessage("Celular inválido. Use (XX) 9XXXX-XXXX.");

        RuleFor(x => x.Telefone)
            .Must(ValidationHelpers.TelefoneFixoValido!)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .WithMessage("Telefone inválido. Use (XX) XXXX-XXXX.");

        RuleFor(x => x.DtNascimento)
            .NotEmpty().WithMessage("Data de nascimento é obrigatória.")
            .Must(ValidationHelpers.DataNascimentoValida)
            .WithMessage("Data de nascimento inválida.");

        RuleFor(x => x.Sexo)
            .NotEmpty().WithMessage("Sexo é obrigatório.")
            .MaximumLength(1).WithMessage("Sexo deve ter apenas 1 caractere.")
            .Must(x => x == "M" || x == "F")
            .WithMessage("Sexo deve ser 'M' ou 'F'.");

        RuleFor(x => x.Etnia)
            .NotEmpty().WithMessage("Etnia é obrigatória.")
            .MaximumLength(12).WithMessage("Etnia não pode ter mais de 12 caracteres.");

        RuleFor(x => x.MunicipioId)
            .GreaterThan(0).WithMessage("Município é obrigatório.");

        RuleFor(x => x.DeficienciaId)
            .GreaterThan(0).WithMessage("Deficiência é obrigatória.");

        RuleFor(x => x.LocalidadeId)
            .GreaterThan(0).WithMessage("Localidade é obrigatória.");

        RuleFor(x => x.AutorizacaoSaida)
            .NotNull().WithMessage("Autorização de saída é obrigatória.");

        RuleFor(x => x.ParticipacaoProgramaCompartilhamentoDados)
            .NotNull().WithMessage("Autorização de participação e compartilhamento de dados é obrigatória.");

        RuleFor(x => x.UtilizacaoImagem)
            .NotNull().WithMessage("Autorização de utilização de imagem é obrigatória.");

        RuleFor(x => x.AutorizacaoConsentimentoAssentimento)
           .NotNull().WithMessage("Concordância com os termos é obrigatória.");

        RuleFor(x => x.Endereco)
            .MaximumLength(200).WithMessage("Endereço não pode ter mais de 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco));

        RuleFor(x => x.Bairro)
            .MaximumLength(50).WithMessage("Bairro não pode ter mais de 50 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Bairro));

        RuleFor(x => x.NomeMae)
            .MaximumLength(150).WithMessage("Nome da mãe não pode ter mais de 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.NomeMae));

        RuleFor(x => x.NomePai)
            .MaximumLength(150).WithMessage("Nome do pai não pode ter mais de 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.NomePai));
    }
}
