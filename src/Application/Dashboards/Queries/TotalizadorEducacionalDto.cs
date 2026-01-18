namespace DnaBrasilApi.Application.Dashboards.Queries;
public class TotalizadorEducacionalDto
{
    public Dictionary<string, decimal>? PercTotalizadorEducacionalMatematica { get; set; } = new();
    public Dictionary<string, decimal>? PercTotalizadorEducacionalPortugues { get; set; } = new();
    public Dictionary<string, decimal>? ValorTotalizadorEducacionalMatematica { get; set; } = new();
    public Dictionary<string, decimal>? ValorTotalizadorEducacionalPortugues { get; set; } = new();
    public Dictionary<string, decimal>? PercentualEducacional { get; set; } = new();

}
