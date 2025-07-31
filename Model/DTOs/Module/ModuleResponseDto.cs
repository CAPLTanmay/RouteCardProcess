namespace RouteCardProcess.Model.DTOs.Module
{
    public class ModuleResponseDto
    {
        public string Name { get; set; }
        public string ParentModuleKey { get; set; }
        public string Route { get; set; }
        public bool IsVisibleOnSidebar { get; set; }
        public string ModuleKey { get; set; }
        public string ImageUrl { get; set; }
        public int Order { get; set; }
    }
}
