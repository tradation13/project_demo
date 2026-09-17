using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;

namespace IPTS.Services
{
    public class TestParameterService(IMapper mapper, ApplicationDbContext context)
        : BaseService<TestParameter>(context, mapper)
    {
    }
}
