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

            CreateMap<Repository.Model.Application,     Model.Application>();
            CreateMap<Repository.Model.MessageTemplate, Model.MessageTemplate>();
            CreateMap<Repository.Model.Permission,      Model.Permission>();
            CreateMap<Repository.Model.UserPermission,  Model.UserPermission>();
            CreateMap<Repository.Model.RolePermission,  Model.RolePermission>();
            CreateMap<Repository.Model.Role,            Model.Role>();
            CreateMap<Repository.Model.UserRole,        Model.UserRole>();
        }
    }
}
