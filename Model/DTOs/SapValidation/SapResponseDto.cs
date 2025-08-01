namespace RouteCardProcess.Model.DTOs.SapValidation
{
    public class SapResponseDto
    {
        public bool IsError { get; set; }
        public List<string> Messages { get; set; } = new();
        public string RawResponse { get; set; }
    }
}
