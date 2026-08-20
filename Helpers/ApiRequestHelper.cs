using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IPTS.Helpers
{
    public static class ApiRequestHelper
    {
        public static Dictionary<string, string[]> ToErrorDictionary(ModelStateDictionary modelState)
        {
            return modelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message ?? string.Empty : e.ErrorMessage)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .ToArray());
        }

        public static bool IsAjaxOrJsonRequest(HttpRequest request)
        {
            if (string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var accept in request.Headers.Accept)
            {
                if (!string.IsNullOrEmpty(accept) &&
                    accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
