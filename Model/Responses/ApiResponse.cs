namespace RouteCardProcess.Model.Responses
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static ApiResponse CreateSuccess(object data = null, string message = "Success") =>
            new ApiResponse { Success = true, Message = message, Data = data };

        public static ApiResponse CreateFail(string message, object data = null) =>
            new ApiResponse { Success = false, Message = message, Data = data };
    }
}
