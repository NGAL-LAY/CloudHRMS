using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class DailyAttendanceController : Controller
    {

        private readonly CloudHRMSApplicationDbContext _dbContext;
        public DailyAttendanceController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            BindEmployeeData();
            return View();
        }

        //save entry
        [HttpPost]
        public async Task<IActionResult> Entry(DailyAttendanceViewModel dailyAttendanceViewModel)
        {
            try
            {
                bool daEmployeeExist =await _dbContext.DailyAttendance
                                                    .AnyAsync(da => !da.IsInActive 
                                                                && da.AttendanceDate == dailyAttendanceViewModel.AttendanceDate
                                                                && da.EmployeeId == dailyAttendanceViewModel.EmployeeId);
                if(!daEmployeeExist)
                {
                    DailyAttendanceEntity dailyAttendanceEntity = new DailyAttendanceEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmployeeId = dailyAttendanceViewModel.EmployeeId,
                        AttendanceDate = dailyAttendanceViewModel.AttendanceDate,
                        InTime = dailyAttendanceViewModel.InTime,
                        OutTime = dailyAttendanceViewModel.OutTime,
                        CreatedAt = DateTime.Now,
                    };
                    _dbContext.DailyAttendance.Add(dailyAttendanceEntity);
                    await _dbContext.SaveChangesAsync();
                    TempData["info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Employee is already exist, try another employee";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system"+ex.Message;
                TempData["Status"] = false;
            }
            BindEmployeeData();
            return View(dailyAttendanceViewModel);
        }

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

        //show list
        public async Task<IActionResult> List(string employeeInfo, string attendanceDate)
        {
            IList<DailyAttendanceViewModel> dailyAttendanceViewModels = new List<DailyAttendanceViewModel>();
            //show all data
            var dailyAttendanceQuery = (from da in _dbContext.DailyAttendance
                                        join e in _dbContext.Employee
                                        on da.EmployeeId equals e.Id
                                        where !da.IsInActive && !e.IsInActive
                                        select new DailyAttendanceViewModel
                                        {
                                            Id = da.Id,
                                            EmployeeInfo = e.Code + "/" + e.Name,
                                            AttendanceDate = da.AttendanceDate,
                                            InTime = da.InTime,
                                            OutTime = da.OutTime,
                                        });
            //apply fiters
            dailyAttendanceQuery = ApplyFilters(dailyAttendanceQuery, employeeInfo, attendanceDate);

            TempData["EmployeeInfo"] = employeeInfo;
            TempData["AttendanceDate"] = attendanceDate;
            dailyAttendanceViewModels = await dailyAttendanceQuery.ToListAsync();
            return View(dailyAttendanceViewModels);
        }

        //filter query by search values
        private IQueryable<DailyAttendanceViewModel> ApplyFilters(IQueryable<DailyAttendanceViewModel> query, string employeeInfo, string attendanceDate)
        {
            if (!string.IsNullOrWhiteSpace(employeeInfo))
            {
                query = query.Where(w => w.EmployeeInfo.Contains(employeeInfo));
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
            DailyAttendanceViewModel dailyAttendanceViewModel = _dbContext.DailyAttendance.Where(w => w.Id == Id && !w.IsInActive)
                .Select(s => new DailyAttendanceViewModel
                {
                    Id=s.Id,
                    EmployeeId=s.EmployeeId,
                    AttendanceDate=s.AttendanceDate,
                    InTime=s.InTime,
                    OutTime=s.OutTime,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt,
                }).FirstOrDefault();
            BindEmployeeData();
            return View(dailyAttendanceViewModel);
        }

        // update 
        public IActionResult Update(DailyAttendanceViewModel dailyAttendanceViewModel)
        {
            try
            {
                DailyAttendanceEntity dailyAttendanceEntity = new DailyAttendanceEntity()
                {
                    Id = dailyAttendanceViewModel.Id,
                    EmployeeId = dailyAttendanceViewModel.EmployeeId,
                    AttendanceDate = dailyAttendanceViewModel.AttendanceDate,
                    InTime = dailyAttendanceViewModel.InTime,
                    OutTime = dailyAttendanceViewModel.OutTime,
                    CreatedAt = dailyAttendanceViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.DailyAttendance.Entry(dailyAttendanceEntity).State = EntityState.Modified;
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully update when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system"+ex.Message;
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //delete record
        [HttpPost]
        public IActionResult Delete(List<string> selectedIds)
        {
            try
            {
                var itemsToDelete = _dbContext.DailyAttendance.Where(da => selectedIds.Contains(da.Id)).ToList();
                foreach (var dailyAttendance in itemsToDelete)
                {
                    dailyAttendance.IsInActive = true;
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
        public async Task<IActionResult> ExportToExcel([FromBody] List<DailyAttendanceViewModel> dailyAttendanceData)
        {
            if (dailyAttendanceData == null || !dailyAttendanceData.Any())
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
                worksheet.Cells[1, 3].Value = "AttendanceDate";
                worksheet.Cells[1, 4].Value = "InTime";
                worksheet.Cells[1, 5].Value = "OutTime";

                // Populate data
                int row = 2;
                foreach (var record in dailyAttendanceData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.EmployeeInfo;
                    worksheet.Cells[row, 3].Value = record.AttendanceDate;
                    worksheet.Cells[row, 4].Value = record.InTime;
                    worksheet.Cells[row, 5].Value = record.OutTime;
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
