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
                .ForMember(x => x.Name, opt => opt.MapFrom(x => (x.Name).Trim()))
                .ForMember(x => x.Phone, opt => opt.MapFrom(x => x.Phone))
                .ForMember(x => x.Email, opt => opt.MapFrom(x => x.Email))
                .ForMember(x => x.Description, opt => opt.MapFrom(x => x.Description))
                .ReverseMap();

            CreateMap<Repository.Model.Application,     Model.Application>().ReverseMap();
            CreateMap<Repository.Model.MessageTemplate, Model.MessageTemplate>().ReverseMap();
            CreateMap<Repository.Model.Permission,      Model.Permission>().ReverseMap();
            CreateMap<Repository.Model.UserPermission,  Model.UserPermission>().ReverseMap();
            CreateMap<Repository.Model.RolePermission,  Model.RolePermission>().ReverseMap();
            CreateMap<Repository.Model.Role,            Model.Role>().ReverseMap();
            CreateMap<Repository.Model.UserRole,        Model.UserRole>().ReverseMap();
            CreateMap<Repository.Model.Environment,     Model.Environment>().ReverseMap();
        }
    }
}
