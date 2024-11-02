using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class AttendanceMasterController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;
        public AttendanceMasterController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            BindEmployeeData();
            BindShiftData();
            return View();
        }

        //save entry
        [HttpPost]
        public async Task<IActionResult> Entry(AttendanceMasterViewModel attendanceMasterViewModel)
        {
            try
            {
                bool amEmployeeShiftExist = await _dbContext.AttendanceMaster
                                                        .AnyAsync(am => !am.IsInActive
                                                                       && am.AttendanceDate == attendanceMasterViewModel.AttendanceDate
                                                                       && am.EmployeeId == attendanceMasterViewModel.EmployeeId
                                                                       && am.ShiftId == attendanceMasterViewModel.ShiftId);
                if(!amEmployeeShiftExist)
                {
                    AttendanceMasterEntity attendanceMasterEntity = new AttendanceMasterEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmployeeId = attendanceMasterViewModel.EmployeeId,
                        ShiftId = attendanceMasterViewModel.ShiftId,
                        AttendanceDate = attendanceMasterViewModel.AttendanceDate,
                        InTime = (attendanceMasterViewModel.InTime.ToString() != "") ? attendanceMasterViewModel.InTime : new TimeSpan(),
                        OutTime = (attendanceMasterViewModel.OutTime.ToString() != "") ? attendanceMasterViewModel.OutTime : new TimeSpan(),
                        IsLate = attendanceMasterViewModel.IsLate ? true : false,
                        IsEarlyOut = attendanceMasterViewModel.IsEarlyOut ? true : false,
                        IsLeave = attendanceMasterViewModel.IsLeave ? true : false,
                        CreatedAt = DateTime.Now,
                    };
                    _dbContext.AttendanceMaster.Add(attendanceMasterEntity);
                    await _dbContext.SaveChangesAsync();
                    TempData["info"] = "Successfully Save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Employee and Shift is already exist, try another";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system" + ex.Message;
                TempData["Status"] = false;
            }
            BindEmployeeData();
            BindShiftData();
            return View(attendanceMasterViewModel);
        }
        
        //Bind EmployeeData
        public void BindEmployeeData()
        {
            IList<EmployeeViewModel> employeeData = _dbContext.Employee.Where(w => !w.IsInActive)
            .Select(s => new EmployeeViewModel
             {
                Id = s.Id,
                Code = s.Code+"/"+s.Name,
             }).ToList();
            ViewBag.EmployeeData = employeeData;
        }

        //Bind ShiftData
        public void BindShiftData()
        {
            IList<ShiftViewModel> shifts = _dbContext.Shift.Where(w => !w.IsInActive)
                .Select(s => new ShiftViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                }).ToList();
            ViewBag.ShiftData = shifts;
        }

        //show list
        public async Task<IActionResult> List(string employeeInfo, string shift, string attendanceDate)
        {
            IList<AttendanceMasterViewModel> attendanceMasterViewModels = new List<AttendanceMasterViewModel>();
            //show all data
            var attendanceMasterQuery = (from am in _dbContext.AttendanceMaster
                                         join e in _dbContext.Employee
                                                                            on am.EmployeeId equals e.Id
                                         join s in _dbContext.Shift
                                         on am.ShiftId equals s.Id
                                         where !am.IsInActive && !e.IsInActive && !s.IsInActive
                                         select new AttendanceMasterViewModel
                                         {
                                             Id = am.Id,
                                             EmployeeInfo = e.Code + "/" + e.Name,
                                             ShiftName = s.Name,
                                             AttendanceDate = am.AttendanceDate,
                                             InTime = am.InTime,
                                             OutTime = am.OutTime,
                                             IsLate = am.IsLate,
                                             IsEarlyOut = am.IsEarlyOut,
                                             IsLeave = am.IsLeave,
                                         });
            // Apply filters
            attendanceMasterQuery = ApplyFilters(attendanceMasterQuery,employeeInfo, shift, attendanceDate);

            TempData["EmployeeInfo"] = employeeInfo;
            TempData["Shift"] = shift;
            TempData["AttendanceDate"] = attendanceDate;
            attendanceMasterViewModels = await attendanceMasterQuery.ToListAsync();
            return View(attendanceMasterViewModels);
        }

        //filter query by search values
        private IQueryable<AttendanceMasterViewModel> ApplyFilters(IQueryable<AttendanceMasterViewModel> query, string employeeInfo, string shift, string attendanceDate)
        {
            if (!string.IsNullOrWhiteSpace(employeeInfo))
            {
                query = query.Where(w => w.EmployeeInfo.Contains(employeeInfo));
            }

            if (!string.IsNullOrWhiteSpace(shift))
            {
                query = query.Where(w => w.ShiftName.Contains(shift));
            }

            if (!string.IsNullOrEmpty(attendanceDate) && DateTime.TryParse(attendanceDate, out DateTime parsedDate))
            {
                query = query.Where(w => w.AttendanceDate.Date == parsedDate.Date);
            }

            return query;
        }

        //edit record
        public IActionResult Edit(string Id)
        {
            AttendanceMasterViewModel attendanceMasterViewModel = _dbContext.AttendanceMaster.Where( w => w.Id == Id && !w.IsInActive)
                .Select(s => new AttendanceMasterViewModel
                {
                    Id= s.Id,
                    EmployeeId = s.EmployeeId,
                    ShiftId = s.ShiftId,
                    AttendanceDate = s.AttendanceDate,
                    InTime = s.InTime,
                    OutTime = s.OutTime,
                    IsLate = s.IsLate,
                    IsEarlyOut = s.IsEarlyOut,
                    IsLeave = s.IsLeave,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt,
                }).FirstOrDefault();
            BindEmployeeData();
            BindShiftData();
            return View(attendanceMasterViewModel);
        }

        //update record
        public IActionResult Update(AttendanceMasterViewModel attendanceMasterViewModel)
        {
            try
            {
                AttendanceMasterEntity attendanceMasterEntity = new AttendanceMasterEntity()
                {
                    Id = attendanceMasterViewModel.Id,
                    EmployeeId = attendanceMasterViewModel.EmployeeId,
                    ShiftId = attendanceMasterViewModel.ShiftId,
                    AttendanceDate = attendanceMasterViewModel.AttendanceDate,
                    InTime = (attendanceMasterViewModel.InTime.ToString() != "") ? attendanceMasterViewModel.InTime : new TimeSpan(),
                    OutTime = (attendanceMasterViewModel.OutTime.ToString() != "") ? attendanceMasterViewModel.OutTime : new TimeSpan(),
                    IsLate = attendanceMasterViewModel.IsLate,
                    IsEarlyOut = attendanceMasterViewModel.IsEarlyOut,
                    IsLeave = attendanceMasterViewModel.IsLeave,
                    CreatedAt = attendanceMasterViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.AttendanceMaster.Entry(attendanceMasterEntity).State = EntityState.Modified;
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully update when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system"+ex.Message;
                TempData["Statsu"] = false;
            }
            return RedirectToAction("List");
        }

        //delete record
        [HttpPost]
        public IActionResult Delete(List<string> selectedIds)
        {
            try
            {
                var itemsToDelete = _dbContext.AttendanceMaster.Where(am => selectedIds.Contains(am.Id)).ToList();
                foreach (var attendanceMaster in itemsToDelete)
                {
                    attendanceMaster.IsInActive = true;
                }
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully delete when the recoed delete from the record";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record delete from the system"+ex.Message;
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //export to excel
        [HttpPost]
        public IActionResult ExportToExcel([FromBody] List<AttendanceMasterViewModel> attendanceMasterData)
        {
            if (attendanceMasterData == null || !attendanceMasterData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Payroll Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "EmployeeInfo";
                worksheet.Cells[1, 3].Value = "ShiftName";
                worksheet.Cells[1, 4].Value = "AttendanceDate";
                worksheet.Cells[1, 5].Value = "InTime";
                worksheet.Cells[1, 6].Value = "OutTime";
                worksheet.Cells[1, 7].Value = "IsLate";
                worksheet.Cells[1, 8].Value = "IsEarlyOut";
                worksheet.Cells[1, 9].Value = "IsLeave";

                // Populate data
                int row = 2;
                foreach (var record in attendanceMasterData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.EmployeeInfo;
                    worksheet.Cells[row, 3].Value = record.ShiftName;
                    worksheet.Cells[row, 4].Value = record.AttendanceDate;
                    worksheet.Cells[row, 5].Value = record.InTime;
                    worksheet.Cells[row, 6].Value = record.OutTime;
                    worksheet.Cells[row, 7].Value = record.IsLate;
                    worksheet.Cells[row, 8].Value = record.IsEarlyOut;
                    worksheet.Cells[row, 9].Value = record.IsLeave;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var fileContents = package.GetAsByteArray();
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
        }
    }
}
