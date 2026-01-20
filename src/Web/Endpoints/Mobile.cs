using DnaBrasilApi.Application.Alunos.Queries;
using DnaBrasilApi.Application.Mobiles.Queries.GetAlunoImageById;
using DnaBrasilApi.Application.Mobiles.Queries.GetAlunoMobileById;
using DnaBrasilApi.Application.Mobiles.Queries.GetAlunoQrCodeById;

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
            .MapGet(GetAlunoImageById, "Aluno/{id}/Image");
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


    #endregion
}
