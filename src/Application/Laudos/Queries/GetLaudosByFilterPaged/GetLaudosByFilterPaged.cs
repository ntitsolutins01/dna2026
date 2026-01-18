using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DnaBrasilApi.Application.Common.Models;
using DnaBrasilApi.Application.Laudos.Queries;
using DnaBrasilApi.Application.Common.Interfaces;
using DnaBrasilApi.Domain.Entities;
using DnaBrasilApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DnaBrasilApi.Application.Common.Dtos;

namespace DnaBrasilApi.Application.Laudos.Queries.GetLaudosByFilterPaged;

public sealed record GetLaudosByFilterPagedQuery : IRequest<PagedResult<LaudoDto>>
{
    public required LaudosFilterDto SearchFilter { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 100;
}

public class GetLaudosByFilterPagedQueryHandler : IRequestHandler<GetLaudosByFilterPagedQuery, PagedResult<LaudoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLaudosByFilterPagedQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<LaudoDto>> Handle(GetLaudosByFilterPagedQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Max(1, request.PageSize);
        var skip = (page - 1) * size;

        var baseQuery = _context.Laudos.AsNoTracking();
        var filtered = ApplyFilters(baseQuery, request.SearchFilter);

        var total = await filtered.CountAsync(ct);

        var ids = await filtered
            .OrderBy(l => l.Aluno.Id)    
            .ThenByDescending(l => l.Id)
            .Select(l => l.Id)
            .Skip(skip)
            .Take(size)
            .ToListAsync(ct);

        var items = await _context.Laudos
            .AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .ProjectTo<LaudoDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        // reordena
        var order = ids.Select((id, idx) => new { id, idx }).ToDictionary(x => x.id, x => x.idx);
        items = items.OrderBy(x => order[x.Id]).ToList();

        return new PagedResult<LaudoDto>
        {
            Page = page,
            PageSize = size,
            TotalCount = total,
            Items = items
        };
    }


    private IQueryable<Laudo> ApplyFilters(IQueryable<Laudo> query, LaudosFilterDto filter)
    {
        if (filter == null) return query;

        if (TryParse(filter.FomentoId, out var fomentoId))
            query = query.Where(l => l.Aluno.Fomento != null && l.Aluno.Fomento.Id == fomentoId);

        if (!string.IsNullOrWhiteSpace(filter.Estado))
        {
            var estado = filter.Estado.Trim();
            query = query.Where(l => l.Aluno.Municipio != null && l.Aluno.Municipio.Estado != null && l.Aluno.Municipio.Estado.Sigla == estado);
        }

        if (TryParse(filter.MunicipioId, out var municipioId))
            query = query.Where(l => l.Aluno.Municipio != null && l.Aluno.Municipio.Id == municipioId);

        if (TryParse(filter.LocalidadeId, out var localidadeId))
            query = query.Where(l => l.Aluno.Localidade != null && l.Aluno.Localidade.Id == localidadeId);

        if (TryParse(filter.AlunoId, out var alunoId))
            query = query.Where(l => l.Aluno.Id == alunoId);

        if (TryParse(filter.TipoLaudoId, out var tipoLaudoId))
        {
            query = tipoLaudoId switch
            {
                (int)EnumTipoLaudo.ConsumoAlimentar => query.Where(l => l.ConsumoAlimentar != null),
                (int)EnumTipoLaudo.QualidadeVida => query.Where(l => l.QualidadeDeVida != null),
                (int)EnumTipoLaudo.Saude => query.Where(l => l.Saude != null),
                (int)EnumTipoLaudo.SaudeBucal => query.Where(l => l.SaudeBucal != null),
                (int)EnumTipoLaudo.TalentoEsportivo => query.Where(l => l.TalentoEsportivo != null),
                (int)EnumTipoLaudo.Vocacional => query.Where(l => l.Vocacional != null),
                _ => query
            };
        }

        if (TryParse(filter.DeficienciaId, out var deficienciaId))
            query = query.Where(l => l.Aluno.Deficiencia != null && l.Aluno.Deficiencia.Id == deficienciaId);

        if (filter.PossuiFoto)
            query = query.Where(l => l.Aluno.ByteImage != null);

        if (filter.Finalizado)
            query = query.Where(l => l.StatusLaudo == "F");

        if (filter.Ordem != null)
            query = query.Where(l => l.Ordem == filter.Ordem);

        return query;
    }

    private static bool TryParse(string? value, out int parsed)
    {
        if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsed))
            return true;
        parsed = 0;
        return false;
    }
}
