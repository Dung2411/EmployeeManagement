namespace EmployeeManagement.Models
{
    public class EmployeeStatisticsViewModel
    {
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalMale { get; set; }
        public int TotalFemale { get; set; }
    }
}
