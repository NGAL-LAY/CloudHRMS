using CloudHRMS.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudHRMS.Models.ViewModels
{
    public class AttendanceMasterViewModel
    {
        public string Id { get; set; }　//for edit
        //set foreign key
        public string EmployeeId { get; set; }
        public string EmployeeInfo { get; set; }
        public string ShiftId { get; set; }
        public string ShiftName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? InTime { get; set; }
        public TimeSpan? OutTime { get; set; }
        public bool IsLate { get; set; }
        public bool IsEarlyOut { get; set; }
        public bool IsLeave { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
