namespace RouteCardProcess.Model.DTOs.Login
{
    public class ShiftDto
    {
        public string ShiftCode { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string ShiftDesc { get; set; }
        public TimeSpan NotificationTime { get; set; }  
        public TimeSpan ShiftStartBufferTime { get; set; }
        
    }
}
