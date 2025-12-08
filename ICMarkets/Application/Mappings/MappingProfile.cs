using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Domain.Entities;

namespace ICMarkets.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<BlockchainData, BlockchainDataDto>();
    }
}
