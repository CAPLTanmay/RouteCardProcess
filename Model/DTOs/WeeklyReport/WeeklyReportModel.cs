namespace RouteCardProcess.Model.DTOs.WeeklyReport
{
    public class WeeklyExceptionReportModel
    {
        public DateTime? ConfDate { get; set; }           // CONF_DATE
        public string? ProOder { get; set; }              // PRO_ODER
        public string? OprtionNo { get; set; }            // OPRTION_NO
        public string? WorkCenter { get; set; }           // WORK_CENTER
        public string? OprtorCode { get; set; }           // Oprtorcode
        public string? Name { get; set; }                 // Name
        public string? Col5 { get; set; }                 // Col5
        public string? Material { get; set; }             // MATERIAL
        public string? Maktx { get; set; }                // Maktx
        public string? OprtionDesc { get; set; }          // OPRTION_DESC
        public string? ExceptionCode { get; set; }        // Exception_Code
        public string? ExceptionDesc { get; set; }        // Exception_Desc
        public string? Remark { get; set; }               // Remark
        public decimal? OprtionQty { get; set; }          // OPRTION_QTY
        public decimal? YieldQty { get; set; }            // YIELD_QTY
        public decimal? ProcesTime { get; set; }          // PROCES_TIME
        public decimal? PlanTime { get; set; }            // plantime
        public decimal? StdMachUp { get; set; }           // STD_MACH_UP
        public decimal? StdLabUp { get; set; }            // STD_lAB_UP
        public decimal? ExcessTime { get; set; }          // ExcessTime
        public string? CompStatus { get; set; }           // comp_Status
    }
}
