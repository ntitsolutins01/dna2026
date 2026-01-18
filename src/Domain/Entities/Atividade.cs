namespace DnaBrasilApi.Domain.Entities;

public class Atividade : BaseAuditableEntity
{
    public int EstruturaId { get; set; }
    public required Estrutura Estrutura { get; set; }
    public int LinhaAcaoId { get; set; }
    public required LinhaAcao LinhaAcao { get; set; }
    public int CategoriaId { get; set; }
    public required Categoria Categoria { get; set; }
    public int ModalidadeId { get; set; }
    public required Modalidade Modalidade { get; set; }
    public required string Turma { get; set; }
    public required string DiasSemana { get; set; }
    public TimeSpan HrInicial { get; set; }
    public TimeSpan HrFinal { get; set; }
    public int ProfissionalId { get; set; }
    public required Profissional Profissional { get; set; }
    public int LocalidadeId { get; set; }
    public required Localidade Localidade { get; set; }
    public bool Status { get; set; } = true;
    public required int QuantidadeAluno { get; set; }
    public IList<AtividadeAluno>? AtividadeAlunos { get; set; }
    public IList<AlunoPresenca>? AlunoPresencas { get; set; }
}
