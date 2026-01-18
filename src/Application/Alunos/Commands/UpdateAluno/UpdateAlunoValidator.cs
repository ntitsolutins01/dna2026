using DnaBrasilApi.Domain.Validation;

namespace DnaBrasilApi.Application.Alunos.Commands.UpdateAluno;

public class UpdateAlunoValidator : AbstractValidator<UpdateAlunoCommand>
{

    public UpdateAlunoValidator()
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
    }
}
