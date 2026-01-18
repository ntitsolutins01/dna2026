namespace DnaBrasilApi.Application.Alunos.Queries;
public class AlunoCarteirinhaDto
{
    public int Id { get; init; }
    public string? Nome { get; set; }
    public string? DtNascimento { get; set; }
    public string? Celular { get; set; }
    public string? Cpf { get; set; }
    public string? Sexo { get; set; }
    public string? Modalidades { get; set; }
    public string? MunicipioEstado { get; set; }
    public string? NomeLocalidade { get; set; }
    public byte[]? ByteImage { get; set; }
    public byte[]? QrCode { get; set; } = null;
}
