using DnaBrasilApi.Application.Laudos.Queries;
using DnaBrasilApi.Domain.Entities;

namespace DnaBrasilApi.Application.Alunos.Queries;
public class AlunoByteDto
{
    /// <summary>
    /// Matrícula do aluno - Identificador único
    /// </summary>
    public int Id { get; init; }
    public byte[]? ByteImage { get; init; }
    public byte[]? QrCode { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Byte[], AlunoByteDto>();
        }
    }
}
