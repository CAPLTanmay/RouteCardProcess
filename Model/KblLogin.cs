namespace RouteCardProcess.Model
{
    public class KblLoginRequest
    {
        public string StrLoginId { get; set; }
        public string StrPassword { get; set; }
    }

    public class KblTokenRequest
    {
        public string ClientId { get; set; } = "Kbl@domail";
        public string ClientSecret { get; set; } = "2UL#$qNa6moa5Xg@vjL#KblPun@";
    }

    public class KblEmpInfoRequest
    {
        public string EmpName { get; set; } = "";
        public string EmpId { get; set; }
    }

    public class KblEmpInfoResponse
    {
        public List<KblEmpInfo> EmpInfo { get; set; }
    }

    public class KblEmpInfo
    {
        public string Tktno { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Deptnm { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }
        public string Wsupervisor { get; set; }
        public string ManagerEmailId { get; set; }
        public string ManagerName { get; set; }
        public string Designation { get; set; }
        public string Company { get; set; }
        public string Band { get; set; }
        public string SectorHead { get; set; }
        public string SectorHeadTktNo { get; set; }
        public string SubDept1 { get; set; }
        public string SubDept2 { get; set; }
        public string SubDept3 { get; set; }
    }


}
