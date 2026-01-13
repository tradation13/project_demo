using System.Reflection;

namespace IPTS.Helpers
{
    public static class ModelsHelper
    {
        public static object GetObjectViewModel(
     string modelName,
     object? Data = null,
     string? suffix = "ViewModel",
     string? prefix = null,
     string? area = null)
        {
            string projectName = Assembly.GetEntryAssembly()?.GetName().Name
                                 ?? throw new Exception("Project name could not be determined.");

            string typePath = $"{projectName}." +
                              $"{(string.IsNullOrEmpty(area) ? "" : $"Areas.{area}.")}" +
                              "ViewsModels." +
                              $"{(string.IsNullOrEmpty(prefix) ? "" : prefix)}" +
                              $"{modelName}" +
                              $"{(string.IsNullOrEmpty(suffix) ? "" : suffix)}";

            var type = Type.GetType(typePath)
                       ?? throw new Exception($"Class not found: {typePath}");

            var viewModel = Activator.CreateInstance(type)
                            ?? throw new Exception($"Could not create instance of {typePath}");

            // استخدام AutoMapper أو خاصية النسخ اليدوي
           if(Data != null) foreach (var prop in type.GetProperties())
            {
                var userProp = Data.GetType().GetProperty(prop.Name);
                if (userProp != null)
                {
                    var value = userProp.GetValue(Data);
                    prop.SetValue(viewModel, value);
                }
            }

            return viewModel;
        }

    }
}