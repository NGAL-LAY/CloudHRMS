
using CloudHRMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudHRMS.DAO
{
    public class CloudHRMSApplicationDbContext:DbContext
    {
        public CloudHRMSApplicationDbContext(DbContextOptions<CloudHRMSApplicationDbContext> options) : base(options) { }

        public DbSet<PositionEntity> Position { get; set; }
        public DbSet<DepartmentEntity> Department { get; set; }
        public DbSet<EmployeeEntity> Employee { get; set; }
        public DbSet<DailyAttendanceEntity> DailyAttendance { get; set; }
        public DbSet<AttendancePolicyEntity> AttendancePolicy { get; set; }
        public DbSet<ShiftEntity> Shift { get; set; }
        public DbSet<ShiftAssignEntity> ShiftAssign { get; set; }
        public DbSet<AttendanceMasterEntity> AttendanceMaster { get; set; }
        public DbSet<PayrollEntity> Payroll { get; set; }
        public DbSet<LoginEntity> Login { get; set; }
    }
}
