namespace Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public ServiceResult(bool success = false, string? message = null, object? data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public ServiceResult AddError(string error)
        {
            Errors.Add(error);
            return this;
        }
        
        public static ServiceResult SuccessResult(string? message = "Thao tác thành công", object? data = null)
        {
            return new ServiceResult(true, message, data);
        }
        
        public static ServiceResult ErrorResult(string? message = "Có lỗi xảy ra", object? data = null)
        {
            return new ServiceResult(false, message, data);
        }
    }

    public class ServiceResult<T>
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public T Data { get; private set; }
        public List<string> Errors { get; private set; } = new List<string>();

        private ServiceResult(bool success, string message, T data)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static ServiceResult<T> SuccessResult(T data, string message = "Operation completed successfully")
        {
            return new ServiceResult<T>(true, message, data);
        }

        public static ServiceResult<T> ErrorResult(string message, T defaultValue = default)
        {
            return new ServiceResult<T>(false, message, defaultValue);
        }

        public ServiceResult<T> AddError(string error)
        {
            Errors.Add(error);
            return this;
        }
    }
}
