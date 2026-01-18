using DnaBrasilApi.Application.Common.Exceptions;
using DnaBrasilApi.Application.Common.Interfaces;
using FluentValidation.Results;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ValidationException = DnaBrasilApi.Application.Common.Exceptions.ValidationException;

namespace DnaBrasilApi.Application.Alunos.Services;

/// <summary>
/// Serviço para validação de duplicidade de alunos 
/// </summary>
public interface IAlunoValidationService
{
    /// <summary>
    /// Valida se já existe aluno com 2 ou mais critérios iguais (Nome, Data Nascimento, CPF)
    /// com regras especiais para homônimos (verifica nome da mãe, depois pai)
    /// </summary>
    /// <param name="alunoIdParaIgnorar">ID do aluno a ser ignorado na validação (usado no Update para não detectar o próprio aluno)</param>
    Task ValidarDuplicidadeAsync(
        string nome, 
        string dtNascimento, 
        string cpf, 
        string? nomeMae, 
        string? nomePai, 
        CancellationToken cancellationToken,
        int? alunoIdParaIgnorar = null);
}

public partial class AlunoValidationService : IAlunoValidationService
{
    private readonly IApplicationDbContext _context;

    public AlunoValidationService(IApplicationDbContext context)
    {
        _context = context;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    public async Task ValidarDuplicidadeAsync(
        string nome, 
        string dtNascimento, 
        string cpf, 
        string? nomeMae, 
        string? nomePai, 
        CancellationToken cancellationToken,
        int? alunoIdParaIgnorar = null)
    {
        
        var nomeNormalizado = NormalizarNome(nome);
        var cpfNormalizado = NormalizarCpf(cpf);
        
       
        if (!DateTime.TryParseExact(
            dtNascimento, 
            "dd/MM/yyyy", 
            CultureInfo.CreateSpecificCulture("pt-BR"),
            DateTimeStyles.None,
            out var dataNascimento))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(dtNascimento), 
                    $"Data de nascimento inválida: '{dtNascimento}'. Use o formato dd/MM/yyyy.")
            });
        }
        
        var nomeMaeNormalizado = NormalizarNome(nomeMae);
        var nomePaiNormalizado = NormalizarNome(nomePai);

       
        var alunosSimilares = await _context.Alunos
            .Where(a => 
                // Ignora o próprio aluno no caso de Update
                (!alunoIdParaIgnorar.HasValue || a.Id != alunoIdParaIgnorar.Value) &&
                (a.Nome.ToLower().Trim() == nomeNormalizado ||
                 a.DtNascimento.Date == dataNascimento.Date ||
                 (!string.IsNullOrEmpty(a.Cpf) && 
                  a.Cpf.Replace(".", "").Replace("-", "") == cpfNormalizado))
            )
            .Select(a => new { 
                a.Id, 
                a.Nome, 
                a.DtNascimento, 
                a.Cpf,
                a.NomeMae,
                a.NomePai
            })
            .ToListAsync(cancellationToken);

        // Normalização em memória 
        var alunosComparacao = alunosSimilares
            .Select(a => new AlunoComparacaoDto
            {
                Id = a.Id,
                Nome = a.Nome,
                NomeNormalizado = NormalizarNome(a.Nome),
                DataNascimento = a.DtNascimento,
                CpfNormalizado = NormalizarCpf(a.Cpf),
                NomeMaeNormalizado = NormalizarNome(a.NomeMae),
                NomePaiNormalizado = NormalizarNome(a.NomePai)
            })
            .ToList();
        
        foreach (var aluno in alunosComparacao)
        {
            var criteriosDetalhados = new List<string>();
            bool nomeIgual = false;
            bool dataIgual = false;
            bool cpfIgual = false;

            // Critério 1: Nome
            if (aluno.NomeNormalizado == nomeNormalizado)
            {
                nomeIgual = true;
                criteriosDetalhados.Add("nome");
            }

            // Critério 2: Data de Nascimento
            if (aluno.DataNascimento.Date == dataNascimento.Date)
            {
                dataIgual = true;
                criteriosDetalhados.Add("data de nascimento");
            }

            // Critério 3: CPF
            if (!string.IsNullOrEmpty(aluno.CpfNormalizado) && 
                aluno.CpfNormalizado == cpfNormalizado)
            {
                cpfIgual = true;
                criteriosDetalhados.Add("CPF");
            }

            // Conta critérios iguais
            int criteriosIguais = (nomeIgual ? 1 : 0) + (dataIgual ? 1 : 0) + (cpfIgual ? 1 : 0);

            // Se menos de 2 critérios, não é duplicata
            if (criteriosIguais < 2)
                continue;

            // ===== REGRA ESPECIAL: Nome + Data (pode ser homônimo) =====
            if (nomeIgual && dataIgual && !cpfIgual)
            {
                // 1ª Verificação: NOME DA MÃE
                bool nomeMaeDisponivelCommand = !string.IsNullOrEmpty(nomeMaeNormalizado);
                bool nomeMaeDisponivelBanco = !string.IsNullOrEmpty(aluno.NomeMaeNormalizado);

                if (nomeMaeDisponivelCommand && nomeMaeDisponivelBanco)
                {
                    if (nomeMaeNormalizado == aluno.NomeMaeNormalizado)
                    {
                        throw new ValidationException(new[]
                        {
                            new ValidationFailure("Duplicidade", 
                                $"Já existe um aluno cadastrado com {string.Join(" e ", criteriosDetalhados)} idênticos " +
                                $"e mesmo nome da mãe (ID: {aluno.Id}). " +
                                $"Nome: {aluno.Nome}, Data Nascimento: {aluno.DataNascimento:dd/MM/yyyy}. " +
                                $"Verifique se não é um cadastro duplicado.")
                        });
                    }
                    else
                    {
                        continue;
                    }
                }

                // 2ª Verificação: SE NÃO TIVER NOME DA MÃE NO COMMAND OU NO BANCO, VERIFICA PELO NOME DO PAI
                bool nomePaiDisponivelCommand = !string.IsNullOrEmpty(nomePaiNormalizado);
                bool nomePaiDisponivelBanco = !string.IsNullOrEmpty(aluno.NomePaiNormalizado);

                if (nomePaiDisponivelCommand && nomePaiDisponivelBanco)
                {
                    if (nomePaiNormalizado == aluno.NomePaiNormalizado)
                    {
                        throw new ValidationException(new[]
                        {
                            new ValidationFailure("Duplicidade", 
                                $"Já existe um aluno cadastrado com {string.Join(" e ", criteriosDetalhados)} idênticos " +
                                $"e mesmo nome do pai (ID: {aluno.Id}). " +
                                $"Nome: {aluno.Nome}, Data Nascimento: {aluno.DataNascimento:dd/MM/yyyy}. " +
                                $"Verifique se não é um cadastro duplicado.")
                        });
                    }
                    else
                    {
                        continue;
                    }
                }

                continue;
            }
  
            if (cpfIgual)
            {
                var criteriosTexto = string.Join(" e ", criteriosDetalhados);
                throw new ValidationException(new[]
                {
                    new ValidationFailure("Duplicidade", 
                        $"Já existe um aluno cadastrado com {criteriosTexto} idênticos (ID: {aluno.Id}). " +
                        $"Nome: {aluno.Nome}, Data Nascimento: {aluno.DataNascimento:dd/MM/yyyy}. " +
                        $"Verifique se não é um cadastro duplicado.")
                });
            }
        }
    }

    /// <summary>
    /// DTO para comparação de alunos com dados já normalizados
    /// </summary>
    private sealed class AlunoComparacaoDto
    {
        public int Id { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string NomeNormalizado { get; init; } = string.Empty;
        public DateTime DataNascimento { get; init; }
        public string CpfNormalizado { get; init; } = string.Empty;
        public string NomeMaeNormalizado { get; init; } = string.Empty;
        public string NomePaiNormalizado { get; init; } = string.Empty;
    }

    /// <summary>
    /// Normaliza nome para comparação (minúsculas, sem espaços extras, sem acentos)
    /// </summary>
    private string NormalizarNome(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        // Converte para minúsculas
        var nomeNormalizado = nome.Trim().ToLowerInvariant();
        
        // Remove espaços múltiplos (qualquer quantidade) usando Regex gerado
        nomeNormalizado = MultipleSpacesRegex().Replace(nomeNormalizado, " ");

        // Remove acentuação
        return RemoverAcentos(nomeNormalizado);
    }

    /// <summary>
    /// Remove acentuação de uma string para normalização
    /// </summary>
    private string RemoverAcentos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        // Normaliza para FormD (decomposição canônica)
        var textoNormalizado = texto.Normalize(NormalizationForm.FormD);
 
        var resultado = new StringBuilder();

        // Remove caracteres de marcação diacrítica (acentos)
        foreach (var c in textoNormalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(c);
            }
        }
        
        // Retorna normalizado para FormC (composição canônica)
        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Normaliza CPF removendo formatação
    /// </summary>
    private string NormalizarCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        return cpf.Replace(".", "").Replace("-", "").Trim();
    }
}
