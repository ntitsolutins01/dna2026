using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Validation;

namespace DnaBrasilApi.Application.Alunos.Commands.CreateAluno;

public class CreateAlunoValidator : AbstractValidator<CreateAlunoCommand>
{

    public CreateAlunoValidator()
    {

        RuleFor(x => x.Nome)
            .NotEmpty().MaximumLength(150);

        RuleFor(x => x.Email)
           .NotEmpty().WithMessage("Email é obrigatório.")
           .MaximumLength(100).WithMessage("Email não pode ter mais de 100 caracteres.")
           .Must(ValidationHelpers.EmailValido)
           .WithMessage("Email inválido.");

        RuleFor(x => x.Cpf)
            .NotEmpty().Must(ValidationHelpers.CpfValido)
            .WithMessage("CPF inválido.");

        RuleFor(x => x.Celular)
            .NotEmpty().Must(ValidationHelpers.CelularValido)
            .WithMessage("Celular inválido. Use (XX) 9XXXX-XXXX.");

        RuleFor(x => x.Telefone)
            .Must(ValidationHelpers.TelefoneFixoValido)
            .WithMessage("Telefone inválido. Use (XX) XXXX-XXXX.");

        RuleFor(x => x.DtNascimento)
            .NotEmpty().Must(ValidationHelpers.DataNascimentoValida)
            .WithMessage("Data de nascimento inválida.");
        RuleFor(x => x.NomeMae)
            .MaximumLength(150).WithMessage("Nome da mãe não pode ter mais de 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.NomeMae));

        RuleFor(x => x.NomePai)
            .MaximumLength(150).WithMessage("Nome do pai não pode ter mais de 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.NomePai));


        RuleFor(x => x.AutorizacaoConsentimentoAssentimento)
            .Equal(true).WithMessage("Termo de consentimento deve ser aceito.");

    }

}
