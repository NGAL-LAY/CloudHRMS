using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class AttendancePolicyController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;
        public AttendancePolicyController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Entry(AttendancePolicyViewModel attendancePolicyViewModel) 
        {
            try
            {
                bool apNameExist = await _dbContext.AttendancePolicy
                                                .AnyAsync(ap => !ap.IsInActive
                                                                && ap.Name == attendancePolicyViewModel.Name);           
                if(!apNameExist)
                {
                    AttendancePolicyEntity attendancePolicyEntity = new AttendancePolicyEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = attendancePolicyViewModel.Name,
                        NumberOfLateTime = attendancePolicyViewModel.NumberOfLateTime,
                        NumberOfEarlyOutTime = attendancePolicyViewModel.NumberOfEarlyOutTime,
                        DeductionInAmount = attendancePolicyViewModel.DeductionInAmount,
                        DeductionInDay = attendancePolicyViewModel.DeductionInDay,
                        CreatedAt = DateTime.Now,
                    };
                    _dbContext.AttendancePolicy.Add(attendancePolicyEntity);
                    await _dbContext.SaveChangesAsync();
                    TempData["info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Attendance policy name is already exist, try another name";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system"+ex.Message;
                TempData["Status"] = false;
            }
            return View(attendancePolicyViewModel);
        }

        //show list
        public async Task<IActionResult> List(string searchTerm = "")
        {
            //check is associated with employee that is enable or not check box for delete
            var associatedAttendancePolicyIds = _dbContext.Shift
                                        .Where(s => s.AttendancePolicyId != null && !s.IsInActive)
                                        .Join(_dbContext.AttendancePolicy.Where(ap => !ap.IsInActive),
                                              s => s.AttendancePolicyId,
                                              ap => ap.Id,
                                              (s, ap) => s.AttendancePolicyId)
                                        .Distinct()
                                        .ToList();
            ViewBag.AssociatedAttendancePolicyIds = associatedAttendancePolicyIds;

            //change DataModel to ViewModel
            IList<AttendancePolicyViewModel> attendancPolicies = new List<AttendancePolicyViewModel>();
            //show all data
            var attendancePolicyQuery = _dbContext.AttendancePolicy.Where(w => !w.IsInActive).
                                                                    Select(s => new AttendancePolicyViewModel
                                                                    {
                                                                        Id = s.Id,
                                                                        Name = s.Name,
                                                                        NumberOfLateTime = s.NumberOfLateTime,
                                                                        NumberOfEarlyOutTime = s.NumberOfEarlyOutTime,
                                                                        DeductionInAmount = s.DeductionInAmount,
                                                                        DeductionInDay = s.DeductionInDay
                                                                    });
            if (!string.IsNullOrWhiteSpace(searchTerm)) 
            {
                //show search data
                attendancePolicyQuery = attendancePolicyQuery.Where(ap => ap.Name.Contains(searchTerm));
            }
            TempData["SearchTerm"] = searchTerm;
            attendancPolicies = await attendancePolicyQuery.ToListAsync();
            return View(attendancPolicies);
        }

        //edit
        public IActionResult Edit(string Id) 
        {
            AttendancePolicyViewModel attendancePolicy = _dbContext.AttendancePolicy.Where(w=> w.Id == Id && !w.IsInActive).
                Select(s => new AttendancePolicyViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    NumberOfLateTime = s.NumberOfLateTime,
                    NumberOfEarlyOutTime= s.NumberOfEarlyOutTime,
                    DeductionInAmount= s.DeductionInAmount,
                    DeductionInDay= s.DeductionInDay,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt,
                }).FirstOrDefault();
            return View(attendancePolicy);
        }

        //update
        public IActionResult Update(AttendancePolicyViewModel attendancePolicyViewModel)
        {
            try
            {
                AttendancePolicyEntity attendancePolicyEntity = new AttendancePolicyEntity()
                {
                    Id = attendancePolicyViewModel.Id,
                    Name = attendancePolicyViewModel.Name,
                    NumberOfLateTime = attendancePolicyViewModel.NumberOfLateTime,
                    NumberOfEarlyOutTime = attendancePolicyViewModel.NumberOfEarlyOutTime,
                    DeductionInAmount = attendancePolicyViewModel.DeductionInAmount,
                    DeductionInDay = attendancePolicyViewModel.DeductionInDay,
                    CreatedAt = attendancePolicyViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.AttendancePolicy.Update(attendancePolicyEntity);
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully update when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system" + ex.Message;
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //delete
        public ActionResult Delete(List<string> selectedIds)
        {
            try
            {
                //delete by id
                if (selectedIds != null && selectedIds.Any())
                {
                    var itemsToDelete = _dbContext.AttendancePolicy.Where(ap => selectedIds.Contains(ap.Id)).ToList();
                    foreach (var attendancePolicy in itemsToDelete)
                    {
                        attendancePolicy.IsInActive = true;
                    }
                    _dbContext.SaveChanges();
                    TempData["info"] = "Successfully delete when the recoed delete from the record";
                    TempData["Status"] = true;
                }
                else
                {
                    TempData["info"] = "Cannot delete because it is associated with another";
                    TempData["Status"] = false;
                    return RedirectToAction("List");
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record delete from the system"+ex.Message;
                TempData["Status"]= false;
            }
            return RedirectToAction("List");
        }

        //export to excel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] List<AttendancePolicyViewModel> attendancePolicyData)
        {
            if (attendancePolicyData == null || !attendancePolicyData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Payroll Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "NumberOfLateTime";
                worksheet.Cells[1, 4].Value = "NumberOfEarlyOutTime";
                worksheet.Cells[1, 5].Value = "DeductionInAmount";
                worksheet.Cells[1, 6].Value = "DeductionInDay";

                // Populate data
                int row = 2;
                foreach (var record in attendancePolicyData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.Name;
                    worksheet.Cells[row, 3].Value = record.NumberOfLateTime;
                    worksheet.Cells[row, 4].Value = record.NumberOfEarlyOutTime;
                    worksheet.Cells[row, 5].Value = record.DeductionInAmount;
                    worksheet.Cells[row, 6].Value = record.DeductionInDay;
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