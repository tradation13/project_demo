namespace IPTS.ViewModels
{
    public class ApiResult<T>
    {
        public bool Ok { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public Dictionary<string, string[]>? Errors { get; init; }

        public static ApiResult<T> Success(string code, string message, T? data = default) =>
            new()
            {
                Ok = true,
                Code = code,
                Message = message,
                Data = data
            };

        public static ApiResult<T> Fail(string code, string message, Dictionary<string, string[]>? errors = null, T? data = default) =>
            new()
            {
                Ok = false,
                Code = code,
                Message = message,
                Errors = errors,
                Data = data
            };
    }
}
