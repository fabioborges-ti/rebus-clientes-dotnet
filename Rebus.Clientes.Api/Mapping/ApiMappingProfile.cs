using AutoMapper;
using Rebus.Clientes.Api.Models;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Api.Mapping;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<CreateClienteRequest, ClienteWriteDto>();
        CreateMap<UpdateClienteRequest, ClienteWriteDto>();
        CreateMap<ClienteDto, ClienteResponse>();
        CreateMap<OperacaoStatusDto, OperacaoStatusResponse>()
            .ForMember(d => d.Tipo, o => o.MapFrom(s => s.Tipo.ToString()))
            .ForMember(d => d.Estado, o => o.MapFrom(s => s.Estado.ToString()));
    }
}
