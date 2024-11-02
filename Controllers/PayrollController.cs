using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Runtime.InteropServices;

namespace CloudHRMS.Controllers
{
    public class PayrollController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;
        public PayrollController(CloudHRMSApplicationDbContext dbContext)
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
        public async Task<IActionResult> Entry(PayrollViewModel payrollViewModel)
        {
            try
            {
                bool payrollExist = await _dbContext.Payroll
                                                .AnyAsync(p => !p.IsInActive
                                                && p.EmployeeId == payrollViewModel.EmployeeId
                                                && p.ToDate.Date >= payrollViewModel.ToDate.Date);
                if (!payrollExist)
                {
                    PayrollEntity payrollEntity = new PayrollEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmployeeId = payrollViewModel.EmployeeId,
                        FromDate = payrollViewModel.FromDate,
                        ToDate = payrollViewModel.ToDate,
                        IncomeTax = payrollViewModel.IncomeTax,
                        GrossPay = payrollViewModel.GrossPay,
                        NetPay = payrollViewModel.NetPay,
                        Allowlance = payrollViewModel.Allowlance,
                        Deduction = payrollViewModel.Deduction,
                        AttendanceDays = payrollViewModel.AttendanceDays,
                        AttendanceDeduction = payrollViewModel.AttendanceDeduction,
                        CreatedAt = DateTime.Now,
                    };
                    _dbContext.Payroll.Add(payrollEntity);
                    await _dbContext.SaveChangesAsync();
                    TempData["info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Payroll is already exist, try another";
                    TempData["Status"] = false;
                }

            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system" + ex.Message;
                TempData["Status"] = false;
            }
            BindEmployeeData();
            return View(payrollViewModel);
        }

        //bind employeedata
        public void BindEmployeeData()
        {
            IList<EmployeeViewModel> employeeData = _dbContext.Employee.Where(w => !w.IsInActive)
                .Select(s => new EmployeeViewModel
                {
                    Id = s.Id,
                    Code = s.Code + "/" + s.Name,
                }).ToList();
            ViewBag.EmployeeData = employeeData;
        }

        //show list
        public async Task<IActionResult> List(string searchTerm, string fromDate, string toDate)
        {
            IList<PayrollViewModel> payrollViewModel = new List<PayrollViewModel>();
            //show all data
            var payrollQuery = (from p in _dbContext.Payroll
                                join e in _dbContext.Employee
                                on p.EmployeeId equals e.Id
                                where !p.IsInActive
                                select new PayrollViewModel
                                {
                                    Id = p.Id,
                                    EmployeeInfo = e.Code + "/" + e.Name,
                                    FromDate = p.FromDate,
                                    ToDate = p.ToDate,
                                    IncomeTax = p.IncomeTax,
                                    GrossPay = p.GrossPay,
                                    NetPay = p.NetPay,
                                    Allowlance = p.Allowlance,
                                    AttendanceDays = p.AttendanceDays,
                                    Deduction = p.Deduction,
                                    AttendanceDeduction = p.AttendanceDeduction,
                                });

            // Apply filters
            payrollQuery = ApplyFilters(payrollQuery, searchTerm, fromDate, toDate);

            TempData["SearchTerm"] = searchTerm;
            TempData["FromDate"] = fromDate;
            TempData["ToDate"] = toDate;
            payrollViewModel = await payrollQuery.ToListAsync();

            //BindEmployeeData();
            return View(payrollViewModel);
        }

        // filter query by search 
        public IQueryable<PayrollViewModel> ApplyFilters(IQueryable<PayrollViewModel> query, string searchTerm, string fromDate, string toDate)
        {
            // Parse dates safely
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out DateTime parsedFromDate))
            {
                query = query.Where(p => p.FromDate >= parsedFromDate);
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out DateTime parsedToDate))
            {
                query = query.Where(p => p.ToDate <= parsedToDate);
            }

            // Search by employee info
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.EmployeeInfo.Contains(searchTerm));
            }

            return query;
        }

        //edit record
        public IActionResult Edit(string Id)
        {
            PayrollViewModel payrollViewModel = _dbContext.Payroll.Where(w => w.Id == Id && !w.IsInActive)
                .Select(s => new PayrollViewModel
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    FromDate = s.FromDate,
                    ToDate = s.ToDate,
                    IncomeTax = s.IncomeTax,
                    GrossPay = s.GrossPay,
                    NetPay = s.NetPay,
                    Allowlance = s.Allowlance,
                    AttendanceDays = s.AttendanceDays,
                    Deduction = s.Deduction,
                    AttendanceDeduction = s.AttendanceDeduction,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt,
                }).FirstOrDefault();
            BindEmployeeData();
            return View(payrollViewModel);
        }

        //update record
        public IActionResult Update(PayrollViewModel payrollViewModel)
        {
            try
            {
                PayrollEntity payrollEntity = new PayrollEntity()
                {
                    Id = payrollViewModel.Id,
                    EmployeeId = payrollViewModel.EmployeeId,
                    FromDate = payrollViewModel.FromDate,
                    ToDate = payrollViewModel.ToDate,
                    IncomeTax = payrollViewModel.IncomeTax,
                    GrossPay = payrollViewModel.GrossPay,
                    NetPay = payrollViewModel.NetPay,
                    Allowlance = payrollViewModel.Allowlance,
                    Deduction = payrollViewModel.Deduction,
                    AttendanceDays = payrollViewModel.AttendanceDays,
                    AttendanceDeduction = payrollViewModel.AttendanceDeduction,
                    CreatedAt = payrollViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.Payroll.Update(payrollEntity);
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully save when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system" + ex.Message;
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
                var itemsToDelete = _dbContext.Payroll.Where(p => selectedIds.Contains(p.Id)).ToList();
                foreach (var payroll in itemsToDelete)
                {
                    payroll.IsInActive = true;
                }
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully delete when the recoed delete from the record";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["inof"] = "Error occur when the record delete from the system" + ex.Message;
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //export to excel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] List<PayrollViewModel> payrollData)
        {
            if (payrollData == null || !payrollData.Any())
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
                worksheet.Cells[1, 3].Value = "FromDate";
                worksheet.Cells[1, 4].Value = "ToDate";
                worksheet.Cells[1, 5].Value = "IncomeTax";
                worksheet.Cells[1, 6].Value = "GrossPay";
                worksheet.Cells[1, 7].Value = "NetPay";
                worksheet.Cells[1, 8].Value = "Allowlance";
                worksheet.Cells[1, 9].Value = "Deduction";
                worksheet.Cells[1, 10].Value = "AttendanceDays";
                worksheet.Cells[1, 11].Value = "AttendanceDeduction";

                // Populate data
                int row = 2;
                foreach (var record in payrollData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.EmployeeInfo;
                    worksheet.Cells[row, 3].Value = record.FromDate;
                    worksheet.Cells[row, 4].Value = record.ToDate;
                    worksheet.Cells[row, 5].Value = record.IncomeTax;
                    worksheet.Cells[row, 6].Value = record.GrossPay;
                    worksheet.Cells[row, 7].Value = record.NetPay;
                    worksheet.Cells[row, 8].Value = record.Allowlance;
                    worksheet.Cells[row, 9].Value = record.Deduction;
                    worksheet.Cells[row, 10].Value = record.AttendanceDays;
                    worksheet.Cells[row, 11].Value = record.AttendanceDeduction;
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
