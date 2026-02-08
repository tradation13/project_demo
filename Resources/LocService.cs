using Microsoft.Extensions.Localization;
using System.Reflection;

namespace IPTS.Resources
{
    public class LocService
    {
        private readonly IStringLocalizer _sharedLocalizer;
        private readonly IStringLocalizer _systemLocalizer;

        public LocService(IStringLocalizerFactory factory)
        {
            var assemblyName = new AssemblyName(typeof(SharedResource).Assembly.FullName!);

            // 1. ربط المترجم الأول بملف الصفحات (SharedResource)
            _sharedLocalizer = factory.Create("SharedResource", assemblyName.Name!);

            // 2. ربط المترجم الثاني بملف الفاليدشن والسيرفر (SystemResource)
            _systemLocalizer = factory.Create("SystemResource", assemblyName.Name!);
        }

        // --- الوصول لملف الصفحات (Shared) ---
        // هذا الـ Indexer الافتراضي عشان ما تخرب صفحاتك القديمة
        public LocalizedString this[string key] => _sharedLocalizer[key];

        // --- الوصول لملف الفاليدشن (System) ---
        // ميثود مخصصة لجلب النصوص من ملف السيستم يدوياً
        public string GetSystem(string key) => _systemLocalizer[key];

        // ميثود عامة للـ Shared
        public string Get(string key) => _sharedLocalizer[key];
    }
}