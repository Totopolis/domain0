using AutoMapper;
using Domain0.Model;
using Domain0.Repository.Model;

namespace Domain0.Service
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<Account, UserProfile>()
                .ForMember(x => x.Id, opt => opt.MapFrom(x => x.Id))
                .ForMember(x => x.Name, opt => opt.MapFrom(x => (x.FirstName + " " + x.MiddleName + " " + x.SecondName).Trim()))
                .ForMember(x => x.Phone, opt => opt.MapFrom(x => x.Phone))
                .ForMember(x => x.Description, opt => opt.MapFrom(x => x.Description));
        }
    }
}
