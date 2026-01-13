using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;

namespace IPTS.Services
{
    public class UserTypeService(IMapper mapper, ApplicationDbContext context) : BaseService<UserType>(context, mapper)
    {
        //public bool UserTypeExist(string name)
        //{
        //    return _context.UserTypes.Any(x => x.Name == name);
        //}
    }
}
