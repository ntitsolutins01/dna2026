using DnaBrasilApi.Application.Alunos.Queries;
using DnaBrasilApi.Application.Laudos.Commands.CreateLaudo;
using DnaBrasilApi.Application.Laudos.Commands.UpdateLaudo;
using DnaBrasilApi.Application.Laudos.Queries;
using DnaBrasilApi.Application.Laudos.Queries.GetConsumoAlimentarById;
using DnaBrasilApi.Application.Laudos.Queries.GetConsumosAlimentaresAll;
using DnaBrasilApi.Application.Laudos.Queries.GetLaudoByAluno;
using DnaBrasilApi.Application.Mobiles.Queries.GetAlunoImageById;
using DnaBrasilApi.Application.Mobiles.Queries.GetAlunoMobileById;
using DnaBrasilApi.Application.Mobiles.Queries.GetAlunoQrCodeById;
using DnaBrasilApi.Domain.Entities;
using DnaBrasilApi.Domain.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DnaBrasilApi.Web.Endpoints;

/// <summary>
/// Api de Mobiles
/// </summary>
public class Mobiles : EndpointGroupBase
{
    #region MapEndpoints

    /// <summary>
    /// Mapeamento dos Endpoints
    /// </summary>
    /// <param name="app">Objeto usado para configurar as rotas e os http pipelines</param>
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            //.RequireAuthorization()
            .MapGet(GetAlunoMobileByIdQuery, "Aluno/{id}")
            .MapGet(GetAlunoQrCodeById, "Aluno/{id}/QrCode")
            .MapGet(GetAlunoImageById, "Aluno/{id}/Image")
            .MapPost(PostTipoLaudoByAlunoId, "Aluno/{id}/{tipoLaudoId}/{tipoLaudo}");
    }
    #endregion

    #region Main Methods


    #endregion

    #region Get Methods
    public async Task<AlunoMobileDto> GetAlunoMobileByIdQuery(ISender sender, int id)
    {
        return await sender.Send(new GetAlunoMobileByIdQuery() { Id = id });
    }
    public async Task<AlunoByteDto> GetAlunoQrCodeById(ISender sender, int id)
    {
        return await sender.Send(new GetAlunoQrCodeByIdQuery() { Id = id });
    }
    public async Task<AlunoByteDto> GetAlunoImageById(ISender sender, int id)
    {
        return await sender.Send(new GetAlunoImageByIdQuery() { Id = id });
    }
    public async Task<bool> PostTipoLaudoByAlunoId(ISender sender, int id, int tipoLaudoId, int tipoLaudo)
    {
        var result = new TipoLaudoMobileDto();

        LaudoDto? laudo = await sender.Send(new GetLaudoByAlunoQuery(id));

        switch ((EnumTipoLaudo)tipoLaudo)
        {
            case EnumTipoLaudo.SaudeBucal:
                switch (laudo)
                {
                    case null:
                        {
                            var command = new CreateLaudoCommand() { AlunoId = id, SaudeBucalId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                    default:
                        {
                            var command = new UpdateLaudoCommand() { Id = laudo.Id, AlunoId = id, SaudeBucalId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                }
                break;
            case EnumTipoLaudo.Vocacional:
                switch (laudo)
                {
                    case null:
                        {
                            var command = new CreateLaudoCommand() { AlunoId = id, VocacionalId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                    default:
                        {
                            var command = new UpdateLaudoCommand() { Id = laudo.Id, AlunoId = id, VocacionalId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                }
                break;
            case EnumTipoLaudo.QualidadeVida:
                switch (laudo)
                {
                    case null:
                        {
                            var command = new CreateLaudoCommand() { AlunoId = id, QualidadeDeVidaId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                    default:
                        {
                            var command = new UpdateLaudoCommand() { Id = laudo.Id, AlunoId = id, QualidadeDeVidaId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                }
                break;
            case EnumTipoLaudo.ConsumoAlimentar:
                switch (laudo)
                {
                    case null:
                        {
                            var command = new CreateLaudoCommand() { AlunoId = id, ConsumoAlimentarId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                    default:
                        {
                            var command = new UpdateLaudoCommand() { Id = laudo.Id, AlunoId = id, ConsumoAlimentarId = tipoLaudoId, };
                            await sender.Send(command);
                            break;
                        }
                }
                break;
            default:
                return false;
        }

        return true;

    }


    #endregion
}
