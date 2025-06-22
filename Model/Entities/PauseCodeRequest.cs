namespace RouteCardProcess.Model.Entities
{
    public class PauseCodeRequest
    {
        public string Plant { get; set; }
        public string PauseCode { get; set; }
        public string PauseCodeDesc { get; set; }
        public string PauseCodeDescM { get; set; }
        public string? FullPauseDescription {  get; set; }
        public bool? IsActive { get; set; }
    }

    public class DeletePauseCodeRequest
    {
        public string Plant { get; set; }
        public string PauseCode { get; set; }
    }
}
