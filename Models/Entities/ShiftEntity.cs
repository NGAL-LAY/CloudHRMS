using System.ComponentModel.DataAnnotations.Schema;

namespace CloudHRMS.Models.Entities
{
    [Table("Shift")]
    public class ShiftEntity:BaseEntity
    {
        public string Name { get; set; }
        public TimeSpan InTime { get; set; }
        public TimeSpan OutTime { get; set; }
        public int LateAfter { get; set; }
        public int EarlyOutBefore { get; set; }
        //set foreign key
        public string AttendancePolicyId { get; set; }
        [ForeignKey(nameof(AttendancePolicyId))]
        public virtual AttendancePolicyEntity AttendancePolicy { get; set; }
    }
}
