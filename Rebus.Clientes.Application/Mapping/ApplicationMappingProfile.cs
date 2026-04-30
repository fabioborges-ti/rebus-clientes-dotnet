using AutoMapper;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Application.Mapping;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Cliente, ClienteDto>();
    }
}
