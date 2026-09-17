using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;

namespace IPTS.Services
{
    public class MedicalConditionService(IMapper mapper, ApplicationDbContext context)
        : BaseService<MedicalCondition>(context, mapper)
    {
    }
}
