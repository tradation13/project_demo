using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Services
{
    public class MedicalCaseService : BaseService<MedicalCase>
    {
        public MedicalCaseService(ApplicationDbContext context, IMapper mapper)
            : base(context, mapper)
        {
        }

        // جلب كل الحالات الصحية لمريض
        public async Task<List<MedicalCase>> GetCasesForPatientAsync(int patientId, int? DoctorId = null)
        {
            return await GetAllAsync(q =>
                q.Where(mc => DoctorId == null ? mc.PatientId == patientId : mc.DoctorId == DoctorId)
                 .Include(mc => mc.MedicalCaseTests)
                 .OrderByDescending(mc => mc.CreatedAt)
            );
        }

        // جلب حالة صحية مع اختبارات وتفاصيلها
        public async Task<MedicalCase?> GetCaseWithTestsAsync(int caseId)
        {
            return await GetByIdAsync<int>(caseId, q =>
                q.Include(mc => mc.MedicalCaseTests)
                 .ThenInclude(mct => mct.Test)
                 .ThenInclude(t => t.TestGroup)
            );
        }

    }
}
