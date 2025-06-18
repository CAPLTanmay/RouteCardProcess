namespace RouteCardProcess.Model.Entities
{
    public class MstWorkCenterRequest
    {
        public string Plant { get; set; }
        public string WorkCenter { get; set; }
        public string WorkCenterDesc { get; set; }
        public string Dept { get; set; }
        public string? FullWorkCenter { get; set; }
    }

    public class DeptRequest
    {
        public string Dept { get; set; }
    }
}
