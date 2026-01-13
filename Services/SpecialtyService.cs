using AutoMapper;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.ViewModels;
using IPTS.Models.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace IPTS.Services
{
    public class SpecialtyService(IMapper mapper, ApplicationDbContext context, UserManager<AppUser> userManager) : BaseService<Specialty>(context, mapper)
    {
       
    }
}
