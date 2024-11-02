using CloudHRMS.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudHRMS.Models.ViewModels
{
    public class ShiftAssignViewModel
    {
        public string Id { get; set; } //for edit
        //set foreign
        public string EmployeeId { get; set; }
        public string EmployeeInfo { get; set; }
        public string ShiftId { get; set; }
        public string ShiftName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        //audit purpose
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
