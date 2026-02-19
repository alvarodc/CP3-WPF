namespace CardPass3.WPF.Data.Models;

public class TimeAttendance
{
    public int IdTimeAttendance { get; set; }
    public string? Name { get; set; }
    public string? TimeAttendanceDescription { get; set; }
    public bool Deleted { get; set; }
}

public class TimeAttendanceSlot
{
    public int IdTimeAttendanceSlot { get; set; }
    public int TimeAttendanceIdTimeAttendance { get; set; }
    public string? Description { get; set; }
    public int Weekday { get; set; }
    public int? TimeslotNumber { get; set; }
    public string TimeBegin { get; set; } = string.Empty;
    public string TimeEnd { get; set; } = string.Empty;
}
