namespace RouteCardProcess.Model.DTOs.WeeklyReport
{
    public class IdleReportModel
    {
        public string Name { get; set; }             // Operator full name
        public string Department { get; set; }       // Dept from MSTLossOrder
        public DateTime CDATE { get; set; }          // OperatorEndTime
        public string PRO_ODER { get; set; }         // Production Order No
        public string OPRTION_NO { get; set; }       // Operation No
        public string WORK_CENTER { get; set; }      // Work Center
        public int Operator { get; set; }            // Operator Id
        public string TYP_Ideal_code { get; set; }   // Type Idle Code
        public string TYP_Ideal_Text { get; set; }   // Type Idle Desc
        public string Ideal_Code { get; set; }       // Idle Code from transaction
        public string Ideal_Reason { get; set; }     // Idle Reason
        public int Ideal_time { get; set; }          // Minutes
    }
}
