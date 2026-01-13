using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;

namespace IPTS.Services
{
    public class PatientService(ApplicationDbContext context, IMapper mapper) : BaseService<Patient>(context, mapper)
    {
    }
}
