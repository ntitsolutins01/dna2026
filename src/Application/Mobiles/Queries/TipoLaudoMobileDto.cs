
using DnaBrasilApi.Domain.Enums;

namespace DnaBrasilApi.Application.Alunos.Queries;
public class TipoLaudoMobileDto
{
    
    public int Id { get; set; }
    public int? TipoLaudoId { get; set; }
    public EnumTipoLaudo TipoLaudo { get; set; }
}
