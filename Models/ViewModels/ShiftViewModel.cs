using CloudHRMS.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudHRMS.Models.ViewModels
{
    public class ShiftViewModel
    {
        public string Id { get; set; } //for edit
        public string Name { get; set; }
        public TimeSpan InTime { get; set; }
        public TimeSpan OutTime { get; set; }
        public int LateAfter { get; set; }
        public int EarlyOutBefore { get; set; }
        //foreign key
        public string AttendancePolicyId { get; set; }
        public string AttendancePolicyInfo { get; set; }
        //audit purpose
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
