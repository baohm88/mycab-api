namespace MyCabs.Api.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse(bool success, string? message = null, T? data = default, List<string>? errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
        }

        public static ApiResponse<T> Ok(T data, string? message = null)
            => new ApiResponse<T>(true, message, data);

        public static ApiResponse<T> Fail(List<string> errors, string? message = null)
            => new ApiResponse<T>(false, message, default, errors);

        public static ApiResponse<T> Fail(string error, string? message = null)
            => new ApiResponse<T>(false, message, default, new List<string> { error });
    }
}
