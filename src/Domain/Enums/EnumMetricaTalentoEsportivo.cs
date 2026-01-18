using System.ComponentModel;

namespace DnaBrasilApi.Domain.Enums;

// Enum para Métrica Tipo de Laudo Talento Esportivo
public enum EnumMetricaTalentoEsportivo
{
    [Description("Muito Fraco")]
    MuitoFraco = 1,
    [Description("Fraco")]
    Fraco = 2,
    [Description("Razoavel")]
    Razoavel = 3,
    [Description("Bom")]
    Bom = 4,
    [Description("Muito Bom")]
    MuitoBom = 5,
    [Description("Excelente")]
    Excelente = 6
}
