namespace RouteCardProcess.Model.DTOs.Module
{
    public class ApiResponse<T>
    {
        public T Result { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public List<string> Errors { get; set; }
    }
}
