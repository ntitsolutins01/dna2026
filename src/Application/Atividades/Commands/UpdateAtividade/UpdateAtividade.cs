using System.Globalization;
using DnaBrasilApi.Application.Common.Interfaces;

namespace DnaBrasilApi.Application.Atividades.Commands.UpdateAtividade;

public record UpdateAtividadeCommand : IRequest<bool>
{
    public required int Id { get; init; }
    public required string Turma { get; init; }
    public required string HrInicial { get; init; }
    public required string HrFinal { get; init; }
    public required int QuantidadeAluno { get; init; }
    public required string DiasSemana { get; init; }
    public required int EstruturaId { get; set; }
    public required int LinhaAcaoId { get; set; }
    public required int CategoriaId { get; set; }
    public required int ModalidadeId { get; set; }
    public required int ProfissionalId { get; set; }
    public bool Status { get; set; }

}

public class UpdateAtividadeCommandHandler : IRequestHandler<UpdateAtividadeCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateAtividadeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateAtividadeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Atividades
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Turma = request.Turma;
        entity.HrInicial = TimeSpan.Parse(request.HrInicial, new CultureInfo("en-US"));
        entity.HrFinal = TimeSpan.Parse(request.HrFinal, new CultureInfo("en-US"));
        entity.QuantidadeAluno = request.QuantidadeAluno;
        entity.DiasSemana = request.DiasSemana;
        entity.EstruturaId = request.EstruturaId;
        entity.LinhaAcaoId = request.LinhaAcaoId;
        entity.CategoriaId = request.CategoriaId;
        entity.ModalidadeId = request.ModalidadeId;
        entity.ProfissionalId = request.ProfissionalId;
        entity.Status = request.Status;

        var result = await _context.SaveChangesAsync(cancellationToken);

        return result == 1;//true
    }
}
