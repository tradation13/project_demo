using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Services
{
    public class MedicalCaseTestService : BaseService<MedicalCaseTest>
    {
        public MedicalCaseTestService(ApplicationDbContext context, IMapper mapper)
            : base(context, mapper)
        {
        }

        // جلب كل اختبارات حالة صحية
        public async Task<List<MedicalCaseTest>> GetTestsForCaseAsync(int medicalCaseId)
        {
            return await GetAllAsync(q =>
                q.Where(t => t.MedicalCaseId == medicalCaseId)
                 .Include(t => t.Test)
                 .ThenInclude(t => t.TestGroup)
                 .OrderBy(t => t.Test.Name)
            );
        }

        // إضافة اختبار (تستخدم AddAsync من BaseService)
        // تحديث نتيجة اختبار
        public async Task<bool> UpdateTestResultAsync(int testId, string result)
        {
            var test = await GetByIdAsync<int>(testId);
            if (test == null) return false;
            test.Result = result;
            _dbSet.Update(test);
            await _context.SaveChangesAsync();
            return true;
        }

        // حذف اختبار (تستخدم DeleteAsync من BaseService)
    }
}
