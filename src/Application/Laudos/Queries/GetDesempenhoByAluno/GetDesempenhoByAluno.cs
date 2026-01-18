using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Application.Dashboards.Queries.GetAlunosBySexo;
using DnaBrasilApi.Domain.Entities;
using DnaBrasilApi.Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DnaBrasilApi.Application.Laudos.Queries.GetDesempenhoByAluno;
//[Authorize]
public record GetDesempenhoByAlunoQuery(int id, int? laudoId) : IRequest<DesempenhoDto>;


public class GetDesempenhoByAlunoQueryHandler : IRequestHandler<GetDesempenhoByAlunoQuery, DesempenhoDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDesempenhoByAlunoQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }


    // Só há 3 situações que cai nessa regra: 
    // 1 - Report (Laudo Completo) onde traz todos os laudos do aluno e laudo específico
    // 2 - Details (Detalhamento Laudo) onde traz todos os laudos do aluno e laudo específico (detalhado)
    // 3 - Laudo Resumido onde deve trazer a partir de uma listagem ou aluno específico, apenas o último laudo (nunca considera um laudoId, apenas AlunoId)
    public async Task<DesempenhoDto> Handle(GetDesempenhoByAlunoQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Aluno> aluno = _context.Alunos
            .Include(x => x.Serie)
            .Where(x => x.Id == request.id)
            .AsNoTracking();

        // Se não existir aluno, já retorna um desempenho "zerado"
        if (!await aluno.AnyAsync(cancellationToken))
            return new DesempenhoDto();

        // Base: laudos do aluno
        var laudoQuery = _context.Laudos
            .AsNoTracking()
            .Include(x => x.Aluno)
            .Include(x => x.Vocacional)
                .ThenInclude(v => v!.Encaminhamento)
            .Include(x => x.SaudeBucal)
                .ThenInclude(s => s!.Encaminhamento)
            .Include(x => x.ConsumoAlimentar)
                .ThenInclude(c => c!.Encaminhamento)
            .Include(x => x.QualidadeDeVida)
            .Include(x => x.EducacionalMatematica)
                .ThenInclude(e => e!.Encaminhamento)
            .Include(x => x.EducacionalPortugues)
                .ThenInclude(f => f!.Encaminhamento)
            .Where(x => x.Aluno.Id == request.id);

        // Se veio LaudoId, restringe ao laudo específico
        if (request.laudoId.HasValue)
            laudoQuery = laudoQuery.Where(x => x.Id == request.laudoId.Value);

        try
        {
            // Aqui sim chamamos o cálculo
            var result = await DesempenhoAlunos(aluno, laudoQuery, cancellationToken);
            return result ?? new DesempenhoDto();
        }
        catch (Exception)
        {
            // TODO: logar ex em algum logger seu
            // _logger.Error(ex, "Erro ao calcular desempenho do aluno {Id}", request.id);

            // MAS NÃO QUEBRA A API: devolve um DTO zerado
            return new DesempenhoDto();
        }
    }

    public async Task<DesempenhoDto> DesempenhoAlunos(IQueryable<Aluno> aluno,
        IQueryable<Laudo> laudo, CancellationToken cancellationToken)
    {
        // Se veio laudoId, esse IQueryable já está restrito no Handle.
        // Se NÃO veio, considera apenas o ÚLTIMO laudo do aluno.
        var laudoBase = laudo;

        if (!await laudoBase.AnyAsync(cancellationToken))
            return new DesempenhoDto();

        // Se não tiver laudoId (ou seja: laudo ainda pode ter vários), pega o último:
        if (await laudoBase.CountAsync(cancellationToken) > 1)
        {
            laudoBase = laudoBase
                .OrderByDescending(x => x.Id)
                .Take(1);
        }

        var desempenhoEsportivo = await _context.TextosLaudos
            .Where(x => x.Status && x.TipoLaudo!.Id == 4)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var desempenhoVida = await _context.TextosLaudos
            .Where(x => x.TipoLaudo!.Id == 7)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var desempenhoImc = await _context.TextosLaudos
            .Where(x => x.Status && x.TipoLaudo!.Id == 9)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var desempenhoVocacional = await _context.TextosLaudos
            .Where(x => x.TipoLaudo!.Id == 6)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var desempenhoSaudeBucal = await _context.TextosLaudos
            .Where(x => x.TipoLaudo!.Id == 5)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var desempenhoConsumoAlimentar = await _context.TextosLaudos
            .Where(x => x.TipoLaudo!.Id == 8)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var desempenhoEducacional = await _context.TextosLaudos
            .Where(x => x.TipoLaudo!.Id == 16)
            .Select(s => s.Classificacao)
            .Distinct()
            .ToListAsync();

        var serieAluno = aluno.FirstOrDefault()?.Serie?.Nome ?? "";
        var turmaAluno = aluno.FirstOrDefault()?.Serie?.Turma ?? "";

        Dictionary<string, decimal> dict = new()
        {
            { "velocidade", 0 },
            { "flexibilidadeMuscular", 0 },
            { "forcaMembrosSup", 0 },
            { "forcaExplosiva", 0 },
            { "aptidaoCardio", 0 },

            { "shutlleRun", 0 },
            { "prancha", 0 },
            { "vo2Max", 0 },

            { "imc", 0 }
        };

        Dictionary<string, decimal> dictDesempenhoMasculino = new()
        {
            { "velocidade", 0 },
            { "flexibilidadeMuscular", 0 },
            { "forcaMembrosSup", 0 },
            { "forcaExplosiva", 0 },
            { "aptidaoCardio", 0 },

            { "shutlleRun", 0 },
            { "prancha", 0 },
            { "vo2Max", 0 },

            { "imc", 0 }
        };

        Dictionary<string, decimal> dictDesempenhoFeminino = new()
        {
            { "velocidade", 0 },
            { "flexibilidadeMuscular", 0 },
            { "forcaMembrosSup", 0 },
            { "forcaExplosiva", 0 },
            { "aptidaoCardio", 0 },

            { "shutlleRun", 0 },
            { "prancha", 0 },
            { "vo2Max", 0 },

            { "imc", 0 }
        };

        List<TextoLaudo> textoLaudo = new();

        var verificaAluno = aluno.Select(x => x.Id);

        var laudoEsportivo = laudoBase.Where(x => verificaAluno.Contains(x.Aluno.Id)).Include(i => i.TalentoEsportivo).Where(x => x.TalentoEsportivo != null)
            .Include(a => a.Aluno)
            .AsNoTracking();

        var laudoVida = laudoBase.Where(x => verificaAluno.Contains(x.Aluno.Id)).Include(i => i.QualidadeDeVida).Where(x => x.QualidadeDeVida != null)
            .Include(a => a.Aluno)
            .AsNoTracking();

        var laudoImc = laudoBase.Where(x => verificaAluno.Contains(x.Aluno.Id)).Include(i => i.Saude).Where(x => x.Saude != null)
            .Include(a => a.Aluno)
            .AsNoTracking();

        var laudoVocacional = laudoBase.Where(x => verificaAluno.Contains(x.Aluno.Id)).Include(i => i.Vocacional).Where(x => x.Vocacional != null)
            .Include(a => a.Aluno)
            .AsNoTracking();

        var laudoSaudeBucal = laudoBase.Where(x => verificaAluno.Contains(x.Aluno.Id)).Include(i => i.SaudeBucal).Where(x => x.SaudeBucal != null)
            .Include(a => a.Aluno)
            .AsNoTracking();

        var laudoConsumoAlimentar = laudoBase.Where(x => verificaAluno.Contains(x.Aluno.Id)).Include(i => i.ConsumoAlimentar).Where(x => x.ConsumoAlimentar != null)
            .Include(a => a.Aluno)
            .AsNoTracking();

        var laudoEducacional = laudoBase
            .Where(x => verificaAluno.Contains(x.Aluno.Id) &&
                        (x.EducacionalMatematicaId != null || x.EducacionalPortuguesId != null))
            .Include(x => x.EducacionalMatematica) 
            .Include(x => x.EducacionalPortugues)  
            .Include(x => x.Aluno)
            .AsNoTracking();

        double velocidade = 0;
        double impulsao = 0;
        double shutlleRun = 0;
        double flexibilidadeMuscular = 0;
        double forcaMembrosSup = 0;
        double aptidaoCardio = 0;
        double prancha = 0;

        int imcSaude = 0;

        int saudeBucal = 0;

        int consumoAlimentar = 0;

        DateTimeOffset? dataTalentoEsportivo = null;
        DateTimeOffset? dataSaude = null;
        DateTimeOffset? dataVida = null;
        DateTimeOffset? dataVocacional = null;
        DateTimeOffset? dataConsumoAlimentar = null;
        DateTimeOffset? dataSaudeBucal = null;
        DateTimeOffset? dataMatematica = null;
        DateTimeOffset? dataPortugues = null;

        string avisoVelocidade = "";
        string percentualVelocidade = "";
        string avisoImpulsao = "";
        string percentualImpulsao = "";
        string avisoShutlleRun = "";
        string percentualShutlleRun = "";
        string avisoFlexibilidadeMuscular = "";
        string percentualFlexibilidadeMuscular = "";
        string avisoForcaMembrosSup = "";
        string percentualForcaMembrosSup = "";
        string avisoAptidaoCardio = "";
        string percentualAptidaoCardio = "";
        string avisoPrancha = "";
        string percentualPrancha = "";

        string avisoImc = "";

        string avisoBemEstar = "";
        string avisoAutoestima = "";
        string avisoFamilia = "";
        string avisoContexto = "";

        string avisoSaudeBucal = "";

        string avisoConsumoAlimentar = "";
        
        string avisoPortugues = "";
        string avisoMatematica = "";

        string textoVelocidade = "";
        string textoImpulsao = "";
        string textoShuttleRun = "";
        string textoFlexibilidadeMuscular = "";
        string textoForcaMembrosSup = "";
        string textoAptidaoCardio = "";
        string textoPrancha = "";

        string textoImc = "";

        string textoVocacional = "";

        string textoBemEstar = "";
        string textoAutoestima = "";
        string textoFamilia = "";
        string textoContexto = "";

        string textoSaudeBucal = "";

        string textoConsumoAlimentar = "";

        string textoMatematica = "";
        string textoPortugues = "";

        // -------------------------------------------------- Talento Esportivo ------------------------------------
        var alunoEsportivo = laudoEsportivo.FirstOrDefault();

        if (alunoEsportivo?.TalentoEsportivo != null)
        {
            var idade = GetIdade(alunoEsportivo.Aluno.DtNascimento, DateTime.Now);

            foreach (var desempenho in desempenhoEsportivo)
            {
                textoLaudo = _context.TextosLaudos.Where(x =>
                    x.Status &&
                    x.Classificacao!.Equals(desempenho) &&
                    x.Idade == idade &&
                    (x.Aviso!.Trim() == "Excelente" || x.Aviso!.Trim() == "Muito Bom" || x.Aviso!.Trim() == "Bom" || x.Aviso!.Trim() == "Razoavel" || x.Aviso!.Trim() == "Fraco" || x.Aviso!.Trim() == "Muito fraco") &&
                    x.Sexo == (idade == 99 ? "G" : alunoEsportivo.Aluno.Sexo)).ToList();

                dataTalentoEsportivo = alunoEsportivo.TalentoEsportivo.Created;

                foreach (var item in textoLaudo)
                {
                    switch (item.Classificacao)
                    {
                        // Velocidade
                        case "Velocidade" when
                            alunoEsportivo.TalentoEsportivo.Velocidade >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.Velocidade <= item.PontoFinal:
                            {
                                const string key = "velocidade";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoVelocidade = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoVelocidade = nota;

                                velocidade = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualVelocidade = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }

                        // Impulsão Força Explosiva Membros Inferiores
                        case "Impulsão" when
                            alunoEsportivo.TalentoEsportivo.ImpulsaoHorizontal >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.ImpulsaoHorizontal <= item.PontoFinal:
                            {
                                const string key = "forcaExplosiva";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoImpulsao = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoImpulsao = nota;

                                impulsao = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualImpulsao = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }

                        // Agilidade ou Shuttle run
                        case "Agilidade ou Shuttle run" when
                            alunoEsportivo.TalentoEsportivo.ShuttleRun >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.ShuttleRun <= item.PontoFinal:
                            {
                                const string key = "shutlleRun";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoShuttleRun = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoShutlleRun = nota;

                                shutlleRun = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualShutlleRun = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }

                        // Flexibilidade Muscular
                        case "Flexibilidade" when
                            alunoEsportivo.TalentoEsportivo.Flexibilidade >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.Flexibilidade <= item.PontoFinal:
                            {
                                const string key = "flexibilidadeMuscular";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoFlexibilidadeMuscular = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoFlexibilidadeMuscular = nota;

                                flexibilidadeMuscular = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualFlexibilidadeMuscular = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }

                        // Força de Membros Superiores - Preensão Manual
                        case "Preensão Manual" when
                            alunoEsportivo.TalentoEsportivo.PreensaoManual >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.PreensaoManual <= item.PontoFinal:
                            {
                                const string key = "forcaMembrosSup";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoForcaMembrosSup = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoForcaMembrosSup = nota;

                                forcaMembrosSup = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualForcaMembrosSup = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }

                        // Aptidão Cardiorrespiratória - VO2 Max
                        case "Vo2 Max" when
                            alunoEsportivo.TalentoEsportivo.Vo2Max >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.Vo2Max <= item.PontoFinal:
                            {
                                const string key = "aptidaoCardio";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoAptidaoCardio = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoAptidaoCardio = nota;

                                aptidaoCardio = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualAptidaoCardio = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }

                        // Resistência Abdominal
                        case "Prancha (ABD)" when
                            alunoEsportivo.TalentoEsportivo.Abdominal >= item.PontoInicial &&
                            alunoEsportivo.TalentoEsportivo.Abdominal <= item.PontoFinal:
                            {
                                const string key = "prancha";
                                var nota = item.Aviso.Trim();

                                // Contadores
                                if (alunoEsportivo.Aluno.Sexo == "M")
                                    dictDesempenhoMasculino[key] += 1;
                                else
                                    dictDesempenhoFeminino[key] += 1;

                                dict[key] += 1;

                                textoPrancha = textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == nota)?.Texto ?? "";

                                avisoPrancha = nota;

                                prancha = nota switch
                                {
                                    "Muito Fraco" => 0d,
                                    "Fraco" => 5d,
                                    "Razoavel" or "Razoável" => 7d,
                                    "Bom" => 9d,
                                    "Muito Bom" => 12d,
                                    "Excelente" => 14.28d,
                                    _ => 0d
                                };

                                // Percentual (enum + tabela)
                                var avisoEnum = nota switch
                                {
                                    "Muito Fraco" => EnumMetricaTalentoEsportivo.MuitoFraco,
                                    "Fraco" => EnumMetricaTalentoEsportivo.Fraco,
                                    "Razoavel" or "Razoável" => EnumMetricaTalentoEsportivo.Razoavel,
                                    "Bom" => EnumMetricaTalentoEsportivo.Bom,
                                    "Muito Bom" => EnumMetricaTalentoEsportivo.MuitoBom,
                                    "Excelente" => EnumMetricaTalentoEsportivo.Excelente,
                                    _ => EnumMetricaTalentoEsportivo.MuitoFraco
                                };

                                var sexoChar = idade == 99 ? 'G' : alunoEsportivo.Aluno.Sexo![0];
                                percentualPrancha = PercentuaisMetricaTalentoEsportivo.Calcular(avisoEnum, sexoChar, idade);

                                break;
                            }
                    }
                }
            }
        }

        // -------------------------------------------------- Saude ------------------------------------
        var alunoSaude = laudoImc.FirstOrDefault();
        if (alunoSaude?.Saude != null)
        {
            var idade = GetIdade(alunoSaude.Aluno.DtNascimento, DateTime.Now);

            var imc = GetImc(alunoSaude.Saude.Massa, alunoSaude.Saude.Altura);
            var decimalImc = Convert.ToDecimal(imc);

            var desempenho = desempenhoImc.FirstOrDefault();
            if (desempenho != null)
            {
                textoLaudo = _context.TextosLaudos.Where(x =>
                    x.Status &&
                    x.Classificacao!.Equals(desempenho) &&
                    x.Idade == idade &&
                    (x.Aviso!.Trim() == "ABAIXODONORMAL" || x.Aviso!.Trim() == "NORMAL" || x.Aviso!.Trim() == "OBESIDADE" ||
                     x.Aviso!.Trim() == "SOBREPESO") &&
                    x.Sexo == (idade == 99 ? "G" : alunoSaude.Aluno.Sexo)).ToList();

                dataSaude = alunoSaude.Saude.Created;

                var item = textoLaudo.FirstOrDefault(x => decimalImc >= x.PontoInicial && decimalImc <= x.PontoFinal);

                if (item != null)
                {
                    avisoImc = item.Aviso switch
                    {
                        "ABAIXODONORMAL" => "Abaixo do Normal",
                        "NORMAL" => "Normal",
                        "OBESIDADE" => "Obesidade",
                        "SOBREPESO" => "Sobrepeso",
                        _ => item.Aviso
                    };

                    var nota = item.Aviso;

                    if (alunoSaude.Aluno.Sexo == "M")
                    {
                        if (dictDesempenhoMasculino.ContainsKey("imc"))
                        {
                            dictDesempenhoMasculino["imc"] += 1;
                        }
                    }
                    else
                    {
                        if (dictDesempenhoFeminino.ContainsKey("imc"))
                        {
                            dictDesempenhoFeminino["imc"] += 1;
                        }
                    }

                    if (dict.ContainsKey("imc"))
                    {
                        dict["imc"] += 1;
                    }

                    // Define o texto e o valor de imcSaude em uma única estrutura switch
                    (textoImc, imcSaude) = nota switch
                    {
                        "ABAIXODONORMAL" => (
                            textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "ABAIXODONORMAL")?.Texto ?? "", 50),
                        "NORMAL" => (
                            textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "NORMAL")?.Texto ?? "", 100),
                        "OBESIDADE" => (
                            textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "OBESIDADE")?.Texto ?? "", 25),
                        "SOBREPESO" => (
                            textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "SOBREPESO")?.Texto ?? "", 0),
                        _ => ("", 0)
                    };

                }
            }
        }

        // -------------------------------------------------- Qualidade de Vida ------------------------------------
        int scoreQualidadeVida = 0;

        var alunoVida = laudoVida.FirstOrDefault();

        if (alunoVida?.QualidadeDeVida != null)
        {
            var desempenho = desempenhoVida.FirstOrDefault();
            if (desempenho != null)
            {
                textoLaudo = _context.TextosLaudos
                    .Where(x =>
                        x.Classificacao!.Equals(desempenho) &&
                        (x.Aviso!.Trim().Equals("BemEstarFisico.Bem estar físico") ||
                         x.Aviso.Trim().Equals("MalEstarFisico.Mal estar físico") ||
                         x.Aviso.Trim().Equals("AutoEstima.Autoestima e estabilidade emocional") ||
                         x.Aviso.Trim().Equals("BaixaAutoEstima.Baixa autoestima e dificuldades emocionais") ||
                         x.Aviso.Trim().Equals("FuncionamentoHarmonico.Funcionamento harmônico familiar") ||
                         x.Aviso.Trim().Equals("Conflitos.Conflitos no contexto familiar") ||
                         x.Aviso.Trim().Equals("ContextosFavorecedores.Contextos favorecedores do desenvolvimento") ||
                         x.Aviso.Trim().Equals("ContextosNaoFavorecedores.Contextos não favorecedores do desenvolvimento")))
                    .ToList();

                dataVida = alunoVida.QualidadeDeVida.Created;

                var encaminhamentos = laudo.FirstOrDefault()?.QualidadeDeVida?.Encaminhamentos;

                if (encaminhamentos != null && textoLaudo.Any())
                {
                    foreach (var param in encaminhamentos.Split(','))
                    {
                        var paramNormalized = param switch
                        {
                            "40" => "BEMESTARFISICO.BEM ESTAR FÍSICO",
                            "58" => "MALESTARFISICO.MAL ESTAR FÍSICO",
                            "62" => "AUTOESTIMA.AUTOESTIMA E ESTABILIDADE EMOCIONAL",
                            "64" => "BAIXAAUTOESTIMA.BAIXA AUTOESTIMA E DIFICULDADES EMOCIONAIS",
                            "66" => "FUNCIONAMENTOHARMONICO.FUNCIONAMENTO HARMÔNICO FAMILIAR",
                            "67" => "CONFLITOS.CONFLITOS NO CONTEXTO FAMILIAR",
                            "68" => "CONTEXTOSFAVORECEDORES.CONTEXTOS FAVORECEDORES DO DESENVOLVIMENTO",
                            "77" => "CONTEXTOSNAOFAVORECEDORES.CONTEXTOS NÃO FAVORECEDORES DO DESENVOLVIMENTO",
                            _ => param.Trim()
                        };


                        foreach (var laudoItem in textoLaudo)
                        {
                            var avisoLaudo = laudoItem.Aviso?.Trim().ToUpper();

                            if (avisoLaudo!.Equals(paramNormalized))
                            {
                                var nota = laudoItem.Aviso?.Trim();

                                int qualidadeVida = nota switch
                                {
                                    "BemEstarFisico.Bem estar físico" => 25,
                                    "MalEstarFisico.Mal estar físico" => 0,
                                    "AutoEstima.Autoestima e estabilidade emocional" => 25,
                                    "BaixaAutoEstima.Baixa autoestima e dificuldades emocionais" => 0,
                                    "FuncionamentoHarmonico.Funcionamento harmônico familiar" => 25,
                                    "Conflitos.Conflitos no contexto familiar" => 0,
                                    "ContextosFavorecedores.Contextos favorecedores do desenvolvimento" => 25,
                                    "ContextosNaoFavorecedores.Contextos não favorecedores do desenvolvimento" => 0,
                                    _ => 0
                                };

                                scoreQualidadeVida += qualidadeVida;

                                (textoBemEstar, avisoBemEstar) = nota switch
                                {
                                    "BemEstarFisico.Bem estar físico" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "BemEstarFisico.Bem estar físico")?.Texto ?? "", "Muito Bom"),
                                    "MalEstarFisico.Mal estar físico" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "MalEstarFisico.Mal estar físico")?.Texto ?? "", "Necessita Melhorar"),
                                    _ => (textoBemEstar ?? "", avisoBemEstar ?? "")
                                };

                                (textoAutoestima, avisoAutoestima) = nota switch
                                {
                                    "AutoEstima.Autoestima e estabilidade emocional" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "AutoEstima.Autoestima e estabilidade emocional")?.Texto ?? "", "Muito Bom"),
                                    "BaixaAutoEstima.Baixa autoestima e dificuldades emocionais" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "BaixaAutoEstima.Baixa autoestima e dificuldades emocionais")?.Texto ?? "", "Necessita Melhorar"),
                                    _ => (textoAutoestima ?? "", avisoAutoestima ?? "")
                                };

                                (textoFamilia, avisoFamilia) = nota switch
                                {
                                    "FuncionamentoHarmonico.Funcionamento harmônico familiar" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "FuncionamentoHarmonico.Funcionamento harmônico familiar")?.Texto ?? "", "Muito Bom"),
                                    "Conflitos.Conflitos no contexto familiar" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "Conflitos.Conflitos no contexto familiar")?.Texto ?? "", "Necessita Melhorar"),
                                    _ => (textoFamilia ?? "", avisoFamilia ?? "")
                                };

                                (textoContexto, avisoContexto) = nota switch
                                {
                                    "ContextosFavorecedores.Contextos favorecedores do desenvolvimento" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "ContextosFavorecedores.Contextos favorecedores do desenvolvimento")?.Texto ?? "", "Muito Bom"),
                                    "ContextosNaoFavorecedores.Contextos não favorecedores do desenvolvimento" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "ContextosNaoFavorecedores.Contextos não favorecedores do desenvolvimento")?.Texto ?? "", "Necessita Melhorar"),
                                    _ => (textoContexto ?? "", avisoContexto ?? "")
                                };
                            }
                        }
                    }
                }
            }
        }

        // -------------------------------------------------- Saude Bucal ------------------------------------
        var alunoSaudeBucal = laudoSaudeBucal.FirstOrDefault();

        if (alunoSaudeBucal?.SaudeBucal != null)
        {
            var desempenho = desempenhoSaudeBucal.FirstOrDefault();
            if (desempenho != null)
            {
                textoLaudo = _context.TextosLaudos
                    .Where(x =>
                        x.Classificacao!.Equals(desempenho) &&
                        (x.Aviso!.Trim().Equals("CUIDADO.CUIDADO") ||
                         x.Aviso.Trim().Equals("ATENCAO.ATENÇÃO") ||
                         x.Aviso.Trim().Equals("MUITOBOM.MUITO BOM")))
                    .ToList();

                dataSaudeBucal = alunoSaudeBucal.SaudeBucal.Created;

                var param = laudo.FirstOrDefault()?.SaudeBucal?.Encaminhamento?.Parametro?.Trim();
                var paramNormalized = "";

                if (param != null)
                {
                    (avisoSaudeBucal, paramNormalized) = param switch
                    {
                        "ATENCAO" => ("Atenção", "ATENCAO.ATENÇÃO"),
                        "CUIDADO" => ("Cuidado", "CUIDADO.CUIDADO"),
                        "MUITOBOM" => ("Muito Bom", "MUITOBOM.MUITO BOM"),
                        _ => (param, param)
                    };
                }

                if (textoLaudo.Any())
                {
                    foreach (var laudoItem in textoLaudo)
                    {
                        var avisoLaudo = laudoItem.Aviso?.Trim();

                        if (avisoLaudo!.Equals(paramNormalized))
                        {
                            var nota = laudoItem.Aviso?.Trim();

                            (textoSaudeBucal, saudeBucal) = nota switch
                            {
                                "CUIDADO.CUIDADO" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "CUIDADO.CUIDADO")?.Texto ?? "", 25),
                                "ATENCAO.ATENÇÃO" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "ATENCAO.ATENÇÃO")?.Texto ?? "", 50),
                                "MUITOBOM.MUITO BOM" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "MUITOBOM.MUITO BOM")?.Texto ?? "", 100),
                                _ => ("", 0)
                            };


                            break;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------- Consumo Alimentar ------------------------------------
        var alunoConsumoAlimentar = laudoConsumoAlimentar.FirstOrDefault();

        if (alunoConsumoAlimentar?.ConsumoAlimentar != null)
        {
            var desempenho = desempenhoConsumoAlimentar.FirstOrDefault();
            if (desempenho != null)
            {
                textoLaudo = _context.TextosLaudos
                    .Where(x =>
                        x.Classificacao!.Equals(desempenho) &&
                        (x.Aviso!.Trim().Equals("HabitosNaoSaudaveis.Hábitos não saudáveis") ||
                         x.Aviso.Trim().Equals("HabitosSatisfatorios.Hábitos satisfatórios") ||
                         x.Aviso.Trim().Equals("BonsHabitosAlimentares.Bons Hábitos alimentares") ||
                         x.Aviso.Trim().Equals("HabitosSaudaveis.Hábitos Saudáveis")))
                    .ToList();

                dataConsumoAlimentar = alunoConsumoAlimentar.ConsumoAlimentar.Created;

                var param = laudo.FirstOrDefault()?.ConsumoAlimentar?.Encaminhamento?.Parametro?.Trim();
                var paramNormalized = "";

                if (param != null)
                {
                    (avisoConsumoAlimentar, paramNormalized) = param switch
                    {
                        "HabitosNaoSaudaveis" => ("Hábitos não saudáveis", "HabitosNaoSaudaveis.Hábitos não saudáveis"),
                        "HabitosSatisfatorios" => ("Hábitos satisfatórios", "HabitosSatisfatorios.Hábitos satisfatórios"),
                        "BonsHabitosAlimentares" => ("Bons hábitos alimentares", "BonsHabitosAlimentares.Bons Hábitos alimentares"),
                        "HabitosSaudaveis" => ("Hábitos saudáveis", "HabitosSaudaveis.Hábitos Saudáveis"),
                        _ => (param, param)
                    };
                }

                if (textoLaudo.Any())
                {
                    foreach (var laudoItem in textoLaudo)
                    {
                        var avisoLaudo = laudoItem.Aviso?.Trim();

                        if (avisoLaudo!.Equals(paramNormalized))
                        {
                            var nota = laudoItem.Aviso;

                            (textoConsumoAlimentar, consumoAlimentar) = nota switch
                            {
                                "HabitosNaoSaudaveis.Hábitos não saudáveis" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "HABITOSNAOSAUDAVEIS.HÁBITOS NÃO SAUDÁVEIS")?.Texto ?? "", 20),
                                "HabitosSatisfatorios.Hábitos satisfatórios" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "HABITOSSATISFATORIOS.HÁBITOS SATISFATÓRIOS")?.Texto ?? "", 40),
                                "BonsHabitosAlimentares.Bons Hábitos alimentares" => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "BONSHABITOSALIMENTARES.BONS HÁBITOS ALIMENTARES")?.Texto ?? "", 70),
                                "HabitosSaudaveis.Hábitos Saudáveis " => (textoLaudo.FirstOrDefault(t => t.Aviso.Trim().ToUpper() == "HABITOSSAUDAVEIS.HÁBITOS SAUDÁVEIS")?.Texto ?? "", 100),
                                _ => ("", 0)
                            };

                            break;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------- Vocacional ------------------------------------
        var scoreVocacional = 0;
        if (laudo.Any(x => x.Vocacional != null))
        {
            scoreVocacional = 100;
        }

        var alunoVocacional = laudoVocacional.FirstOrDefault();

        if (alunoVocacional?.Vocacional != null)
        {
            var desempenho = desempenhoVocacional.FirstOrDefault();
            if (desempenho != null)
            {
                textoLaudo = _context.TextosLaudos
                    .Where(x =>
                        x.Classificacao!.Equals(desempenho) &&
                        (x.Aviso!.Trim().Equals("Artistico.Interesse Artistico") ||
                         x.Aviso.Trim().Equals("Empreendedorismo.Interesse Empreendedor") ||
                         x.Aviso.Trim().Equals("CienciasExatasNaturais.Ciências Exatas e Naturais") ||
                         x.Aviso.Trim().Equals("CienciasHumanas.Ciências Humanas") ||
                         x.Aviso.Trim().Equals("CienciasContabeisAdministrativas.Ciências Contábeis e Administrativas") ||
                         x.Aviso.Trim().Equals("TecnologiasAplicadas.Tecnologias Aplicadas")))
                    .ToList();

                dataVocacional = alunoVocacional.Vocacional.Created;

                var param = laudo.FirstOrDefault()?.Vocacional?.Encaminhamento?.Parametro?.Trim();

                var paramNormalized = param switch
                {
                    "Artistico" => "Artistico.Interesse Artistico",
                    "Empreendedorismo" => "Empreendedorismo.Interesse Empreendedor",
                    "CienciasExatasNaturais" => "CienciasExatasNaturais.Ciências Exatas e Naturais",
                    "CienciasHumanas" => "CienciasHumanas.Ciências Humanas",
                    "CienciasContabeisAdministrativas" => "CienciasContabeisAdministrativas.Ciências Contábeis e Administrativas",
                    "TecnologiasAplicadas" => "TecnologiasAplicadas.Tecnologias Aplicadas",
                    _ => param
                };

                if (textoLaudo.Any())
                {
                    foreach (var laudoItem in textoLaudo)
                    {
                        var avisoLaudo = laudoItem.Aviso?.Trim();

                        if (avisoLaudo!.Equals(paramNormalized))
                        {
                            var nota = laudoItem.Aviso;

                            textoVocacional = nota switch
                            {
                                "Artistico.Interesse Artistico" => textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "Artistico.Interesse Artistico")?.Texto ?? "",
                                "Empreendedorismo.Interesse Empreendedor" => textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "Empreendedorismo.Interesse Empreendedor")?.Texto ?? "",
                                "CienciasExatasNaturais.Ciências Exatas e Naturais" => textoLaudo.FirstOrDefault(t => t.Aviso == "CienciasExatasNaturais.Ciências Exatas e Naturais")?.Texto ?? "",
                                "CienciasHumanas.Ciências Humanas" => textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "CienciasHumanas.Ciências Humanas")?.Texto ?? "",
                                "CienciasContabeisAdministrativas.Ciências Contábeis e Administrativas" => textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "CienciasContabeisAdministrativas.Ciências Contábeis e Administrativas")?.Texto ?? "",
                                "TecnologiasAplicadas.Tecnologias Aplicada" => textoLaudo.FirstOrDefault(t => t.Aviso.Trim() == "TecnologiasAplicadas.Tecnologias Aplicada")?.Texto ?? "",
                                _ => ""
                            };

                            break;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------- Educacional ------------------------------------
        var alunoEducacional = laudoEducacional.FirstOrDefault();

        int scoreMatematica = 0, scorePortugues = 0;

        if (alunoEducacional is not null)
        {
            // Matemática
            if (alunoEducacional.EducacionalMatematica is not null)
            {
                dataMatematica = alunoEducacional.EducacionalMatematica.Created;

                var encMatematica = alunoEducacional.EducacionalMatematica.Encaminhamento;
                if (encMatematica is not null)
                {
                    (avisoMatematica, scoreMatematica, textoMatematica) =
                        CalcularMetricaEducacional(encMatematica);
                }
            }

            // Português
            if (alunoEducacional.EducacionalPortugues is not null)
            {
                dataPortugues = alunoEducacional.EducacionalPortugues.Created;

                var encPortugues = alunoEducacional.EducacionalPortugues.Encaminhamento;
                if (encPortugues is not null)
                {
                    (avisoPortugues, scorePortugues, textoPortugues) =
                        CalcularMetricaEducacional(encPortugues);
                }
            }
        }

        // -------------------------------------------------- Calculo Scores ------------------------------------
        var scoreTalentoEsportivo = velocidade + impulsao + shutlleRun + flexibilidadeMuscular
                                    + forcaMembrosSup + aptidaoCardio + prancha;

        var scoreSaude = imcSaude;

        // -------------------------------------------------- Série / Turma ------------------------------------

        return new DesempenhoDto()
        {
            ScoreTalentoEsportivo = Round(scoreTalentoEsportivo),
            ScoreSaude = scoreSaude,
            ScoreVida = scoreQualidadeVida,
            ScoreVocacional = scoreVocacional,
            ScoreSaudeBucal = saudeBucal,
            ScoreConsumoAlimentar = consumoAlimentar,
            ScoreMatematica = scoreMatematica,
            ScorePortugues = scorePortugues,
            ScoreDna = Round(scoreTalentoEsportivo + scoreSaude + scoreVocacional + saudeBucal + consumoAlimentar + scoreQualidadeVida + scoreMatematica + scorePortugues),
            AvisoVelocidade = avisoVelocidade,
            PercentualVelocidade = percentualVelocidade,
            AvisoImpulsao = avisoImpulsao,
            PercentualImpulsao = percentualImpulsao,
            AvisoShuttleRun = avisoShutlleRun,
            PercentualShuttleRun = percentualShutlleRun,
            AvisoFlexibilidadeMuscular = avisoFlexibilidadeMuscular,
            PercentualFlexibilidadeMuscular = percentualFlexibilidadeMuscular,
            AvisoForcaMembrosSup = avisoForcaMembrosSup,
            PercentualForcaMembrosSup = percentualForcaMembrosSup,
            AvisoAptidaoCardio = avisoAptidaoCardio,
            PercentualAptidaoCardio = percentualAptidaoCardio,
            AvisoPrancha = avisoPrancha,
            PercentualPrancha = percentualPrancha,
            AvisoImc = avisoImc,
            AvisoBemEstar = avisoBemEstar,
            AvisoAutoestima = avisoAutoestima,
            AvisoFamilia = avisoFamilia,
            AvisoContexto = avisoContexto,
            AvisoSaudeBucal = avisoSaudeBucal,
            AvisoConsumoAlimentar = avisoConsumoAlimentar,
            AvisoPortugues = avisoPortugues,
            AvisoMatematica = avisoMatematica,
            DataTalentoEsportivo = dataTalentoEsportivo,
            DataSaude = dataSaude,
            DataVida = dataVida,
            DataVocacional = dataVocacional,
            DataSaudeBucal = dataSaudeBucal,
            DataConsumoAlimentar = dataConsumoAlimentar,
            DataMatematica = dataMatematica,
            DataPortugues = dataPortugues,
            TextoVelocidade = textoVelocidade,
            TextoImpulsao = textoImpulsao,
            TextoShuttleRun = textoShuttleRun,
            TextoFlexibilidadeMuscular = textoFlexibilidadeMuscular,
            TextoForcaMembrosSup = textoForcaMembrosSup,
            TextoAptidaoCardio = textoAptidaoCardio,
            TextoPrancha = textoPrancha,
            TextoImc = textoImc,
            TextoVocacional = textoVocacional,
            TextoBemEstar = textoBemEstar,
            TextoAutoestima = textoAutoestima,
            TextoFamilia = textoFamilia,
            TextoContexto = textoContexto,
            TextoConsumoAlimentar = textoConsumoAlimentar,
            TextoSaudeBucal = textoSaudeBucal,
            TextoMatematica = textoMatematica,
            TextoPortugues = textoPortugues,
            SerieAluno = serieAluno,
            TurmaAluno = turmaAluno

        };
    }

    /// <summary>
    /// Calcula quantidade de anos passdos com base em duas datas, caso encontre qualquer problema retorna 0 
    /// </summary>
    /// <param name="data">Data inicial</param>
    /// <param name="now">Data final ou deixar nula para data atual</param>
    /// <returns>Retorna inteiro com quantiadde de anos</returns>
    private static int GetIdade(DateTime data, DateTime? now = null)
    {
        // Carrega a data do dia para comparação caso data informada seja nula

        now = ((now == null) ? DateTime.Now : now);

        try
        {
            int YearsOld = (now.Value.Year - data.Year);

            if (now.Value.Month < data.Month || (now.Value.Month == data.Month && now.Value.Day < data.Day))
            {
                YearsOld--;
            }

            return YearsOld >= 18 ? 99 : YearsOld < 4 ? 4 : YearsOld;
        }
        catch
        {
            return 0;
        }
    }
    private static int Round(double number)
    {
        if (number - (int)number >= 0.5)
        {
            number = (int)number + 1;
        }
        else
        {
            number = (int)number;
        }

        return (int)number;
    }
    private static string GetImc(decimal? massa, decimal? altura)
    {
        try
        {
            var inteiro = massa! * 100 * 100;
            var dividendo = altura * altura;
            var result = Convert.ToDecimal(inteiro) / Convert.ToDecimal(dividendo);

            Double doublVal = Convert.ToDouble(String.Format("{0:0.00}", result));

            return doublVal.ToString();

        }
        catch
        {
            return 0.ToString();
        }
    }

    private static (string aviso, int score, string texto)
    CalcularMetricaEducacional(Encaminhamento enc)
    {
        // Aqui você ajusta as regras de pontuação como quiser (Tabela Encaminhamento. São os Ids)
        var score = enc.Id switch
        {
            96 => 20,
            97 => 35,
            98 => 50,
            _ => 0
        };

        var aviso = enc.Nome ?? string.Empty;
        var texto = enc.Descricao ?? string.Empty;

        return (aviso, score, texto);
    }

    public static class PercentuaisMetricaTalentoEsportivo
    {
        private const int IdadeMin = 7;
        private const int IdadeMax = 17;

        private static readonly Dictionary<char, Dictionary<EnumMetricaTalentoEsportivo, string[]>> Tabela =
            new()
            {
                // Cada array corresponde a idade de 7 a 17
                ['F'] = new()
                {
                    [EnumMetricaTalentoEsportivo.MuitoFraco] = new[] { "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%" }, 
                    [EnumMetricaTalentoEsportivo.Fraco] = new[] { "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%" },
                    [EnumMetricaTalentoEsportivo.Razoavel] = new[] { "30%", "30%", "30%", "30%", "30%", "30%", "30%", "30%", "30%", "30%", "30%" },
                    [EnumMetricaTalentoEsportivo.Bom] = new[] { "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%" },
                    [EnumMetricaTalentoEsportivo.MuitoBom] = new[] { "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%" },
                    [EnumMetricaTalentoEsportivo.Excelente] = new[] { "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%" },
                },
                ['M'] = new()
                {
                    [EnumMetricaTalentoEsportivo.MuitoFraco] = new[] { "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%" },
                    [EnumMetricaTalentoEsportivo.Fraco] = new[] { "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%", "0%" },
                    [EnumMetricaTalentoEsportivo.Razoavel] = new[] { "10%", "10%", "10%", "10%", "10%", "30%", "30%", "30%", "30%", "30%", "30%" },
                    [EnumMetricaTalentoEsportivo.Bom] = new[] { "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%", "50%" },
                    [EnumMetricaTalentoEsportivo.MuitoBom] = new[] { "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%", "75%" },
                    [EnumMetricaTalentoEsportivo.Excelente] = new[] { "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%", "100%" },
                }
            };

        public static string Calcular(EnumMetricaTalentoEsportivo aviso, char sexo, int idade)
        {
            sexo = char.ToUpperInvariant(sexo);
            if (idade < IdadeMin || idade > IdadeMax) return "";

            if (!Tabela.TryGetValue(sexo, out var porAviso)) return "";
            if (!porAviso.TryGetValue(aviso, out var arr)) return "";

            return arr[idade - IdadeMin];
        }
    }

}
