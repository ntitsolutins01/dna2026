using DnaBrasilApi.Application.Estados.Queries;
using DnaBrasilApi.Application.Estados.Queries.GetEstadoByUf;
using DnaBrasilApi.Application.Estados.Queries.GetEstadosAll;
using DnaBrasilApi.Application.Estados.Queries.GetEstadosByMunicipioId;
using DnaBrasilApi.Application.Estados.Queries.GetEstadosComLocalidades;
using DnaBrasilApi.Application.Municipios.Queries;
using DnaBrasilApi.Application.Municipios.Queries.GetMunicipioById;
using DnaBrasilApi.Application.Municipios.Queries.GetMunicipiosByFomentoId;
using DnaBrasilApi.Application.Municipios.Queries.GetMunicipiosByUf;
using DnaBrasilApi.Application.Municipios.Queries.GetMunicipiosByUfComLocalidades;

namespace DnaBrasilApi.Web.Endpoints;

public class DivisoesAdministrativas : EndpointGroupBase
{
    #region MapEndpoints

    /// <summary>
    /// Mapeamento dos Endpoints
    /// </summary>
    /// <param name="app">Objeto usado para configurar as rotas e os http pipelines</param>
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetEstadosAll, "Estados")
            .MapGet(GetEstadosComLocalidades, "Estados/ComLocalidades")
            .MapGet(GetMunicipiosByUf, "Municipios/{uf}")
            .MapGet(GetMunicipiosByUfComLocalidades, "Municipios/{uf}/ComLocalidades")
            .MapGet(GetMunicipioById, "Municipio/{id}")
            .MapGet(GetEstadoByUf, "Estado/{uf}")
            .MapGet(GetMunicipiosByFomentoId, "Municipio/Fomento/{id}")
            .MapGet(GetEstadosByMunicipioId, "Estado/Municipio/{municipioId}");
    }
    #endregion

    #region Get Methods
    /// <summary>
    /// Endpoint que busca todos os Estados
    /// </summary>
    /// <param name="sender">sender</param>
    /// <returns>Retorna uma Divisao Administrativa</returns>
    public async Task<List<EstadoDto>> GetEstadosAll(ISender sender)
    {
        return await sender.Send(new GetEstadosAllQuery());
    }

    /// <summary>
    /// Endpoint que busca Estados cujos Municípios possuem Localidades vinculadas
    /// </summary>
    /// <param name="sender">sender</param>
    /// <returns>Retorna lista de Estados com localidades</returns>
    public async Task<List<EstadoDto>> GetEstadosComLocalidades(ISender sender)
    {
        return await sender.Send(new GetEstadosComLocalidadesQuery());
    }

    /// <summary>
    /// Endpoint que busca todos os Municipios pela Uf
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="uf"></param>
    /// <returns>Retorna uma Divisao Administrativa</returns>
    public async Task<List<MunicipioDto>> GetMunicipiosByUf(ISender sender, string uf)
    {
        return await sender.Send(new GetMunicipioByUfQuery { Uf = uf });
    }

    /// <summary>
    /// Endpoint que busca Municípios de uma UF que possuem Localidades vinculadas
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="uf">Sigla da UF (ex: DF, SP, RJ)</param>
    /// <returns>Retorna lista de Municípios da UF com localidades</returns>
    public async Task<List<MunicipioDto>> GetMunicipiosByUfComLocalidades(ISender sender, string uf)
    {
        return await sender.Send(new GetMunicipiosByUfComLocalidadesQuery { Uf = uf });
    }

    /// <summary>
    /// Endpoint que busca o Estado pela Uf
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="uf"></param>
    /// <returns></returns>
    public async Task<EstadoDto> GetEstadoByUf(ISender sender, string uf)
    {
        return await sender.Send(new GetEstadoByUfQuery() { Uf = uf });
    }

    /// <summary>
    /// Endpoint que busca o Municipio pelo Id
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<MunicipioDto> GetMunicipioById(ISender sender, int id)
    {
        return await sender.Send(new GetMunicipioByIdQuery() { Id = id });
    }

    /// <summary>
    /// Endpoint que busca os Municipios de um fomento
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="id">Id do fomento</param>
    /// <returns>retorna uma lista de municipios</returns>
    public async Task<List<MunicipioDto>> GetMunicipiosByFomentoId(ISender sender, int id)
    {
        return await sender.Send(new GetMunicipiosByFomentoIdQuery { Id = id });
    }

    /// <summary>
    /// Endpoint que s Estados por Municipio
    /// </summary>
    /// <param name="sender">sender</param>
    /// <returns>Retorna uma Divisao Administrativa</returns>
    public async Task<List<EstadoDto>> GetEstadosByMunicipioId(ISender sender, int municipioId)
    {
        return await sender.Send(new GetEstadosByMunicipioIdQuery { MunicipioId = municipioId });
    }

    #endregion
}
