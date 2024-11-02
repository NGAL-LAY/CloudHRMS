using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;

        public EmployeeController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            BindPositionData();
            BindDepartmentData();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Entry(EmployeeViewModel employeeViewModel) 
        {
            try
            {
                bool eCodeExist = await _dbContext.Employee
                                            .AnyAsync(e => !e.IsInActive && e.Code == employeeViewModel.Code);
                if(!eCodeExist)
                {
                    EmployeeEntity employeeEntity = new EmployeeEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = employeeViewModel.Code,
                        Name = employeeViewModel.Name,
                        Email = employeeViewModel.Email,
                        Phone = employeeViewModel.Phone,
                        Gender = employeeViewModel.Gender,
                        DepartmentId = employeeViewModel.DepartmentId,
                        PositionId = employeeViewModel.PositionId,
                        DOB = employeeViewModel.DOB,
                        DOE = employeeViewModel.DOE,
                        DOR = employeeViewModel.DOR,
                        Address = employeeViewModel.Address,
                        BasicSalary = employeeViewModel.BasicSalary,
                        CreatedAt = DateTime.Now
                    };
                    _dbContext.Employee.Add(employeeEntity);
                    await _dbContext.SaveChangesAsync();
                    TempData["info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Employee Code is already exist, try another employee";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system"+ex.Message;
                TempData["Status"] = false;
            }

            //show UI
            BindPositionData();
            BindDepartmentData();
            return View(employeeViewModel);
        }

        //bind data from Position
        public void BindPositionData()
        {
            IList<PositionViewModel> positions = _dbContext.Position.Where(w => !w.IsInActive)
                .Select(s => new PositionViewModel
                {
                    Id = s.Id,
                    Code = s.Code +"/"+s.Name,
                }).ToList();
            ViewBag.Positions = positions;//set up position data to viewBag for UI
        }

        //bind data from Department
        public void BindDepartmentData()
        {
            IList<DepartmentViewModel> departments = _dbContext.Department.Where(w => !w.IsInActive).
                Select(s => new DepartmentViewModel
                {
                    Id = s.Id,
                    Code = s.Code +"/"+s.Name,
                }).ToList();
            ViewBag.Departments = departments;//set up department data to viewBag for UI
        }

        //show list 
        public async Task<IActionResult> List(string searchTerm = "")
        {
            //check is associated with another that is enable or not check box for delete
            // Get the IDs of Employees that are referenced in ShiftAssign, DailyAttendance, AttendanceMaster and Payroll
            var employeeIdsFromFourTbl = _dbContext.ShiftAssign
                                                .Where(sa => sa.EmployeeId != null && !sa.IsInActive)
                                                .Select(sa => sa.EmployeeId)
                                                .Intersect(
                                                    _dbContext.AttendanceMaster
                                                        .Where(am => am.EmployeeId != null && !am.IsInActive)
                                                        .Select(am => am.EmployeeId)
                                                )
                                                .Intersect(
                                                    _dbContext.DailyAttendance
                                                        .Where(da => da.EmployeeId != null && !da.IsInActive)
                                                        .Select(da => da.EmployeeId)
                                                )
                                                .Intersect(
                                                    _dbContext.Payroll
                                                        .Where(p => p.EmployeeId != null && !p.IsInActive)
                                                        .Select(p => p.EmployeeId)
                                                )
                                                .Distinct()
                                                .ToList();
            // Find employees that are not associated with any records in above four table
            var associatedEmployeeIds = _dbContext.Employee
                                            .Where(e => !e.IsInActive && employeeIdsFromFourTbl.Contains(e.Id))
                                            .Select(e => e.Id)
                                            .ToList();
            ViewBag.AssociatedEmployeeIds = associatedEmployeeIds;

            //data from db set to the data of viewmodel
            IList<EmployeeViewModel> employeeViewModels = new List<EmployeeViewModel>();
            //show all data
            var employeeQuery = (from e in _dbContext.Employee
                                        join d in _dbContext.Department
                                        on e.DepartmentId equals d.Id
                                        join p in _dbContext.Position
                                        on e.PositionId equals p.Id
                                        where !e.IsInActive && !d.IsInActive && !p.IsInActive
                                        select new EmployeeViewModel
                                        {
                                            Id = e.Id,
                                            Code = e.Code,
                                            Name = e.Name,
                                            Email = e.Email,
                                            Phone = e.Phone,
                                            Gender = e.Gender,
                                            DepartmentId = e.DepartmentId,
                                            PositionId = e.PositionId,
                                            DOB = e.DOB,
                                            DOE = e.DOE,
                                            DOR = e.DOR,
                                            Address = e.Address,
                                            BasicSalary = e.BasicSalary,
                                            DepartmentInfo = d.Code + "/" + d.Name,
                                            PositionInfo = p.Code + "/" + p.Name
                                        });
            if (!string.IsNullOrEmpty(searchTerm))
            {
                employeeQuery = employeeQuery.Where(e => e.Code.Contains(searchTerm) || e.Name.Contains(searchTerm));
            }
            employeeViewModels = await employeeQuery.ToListAsync();

            TempData["SearchTerm"] = searchTerm;
            return View(employeeViewModels);
        }

        //edit
        public IActionResult Edit(string Id)
        {
            EmployeeViewModel employeeViewModel = _dbContext.Employee.Where(w=> w.Id == Id && !w.IsInActive).
                Select(s => new EmployeeViewModel
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Email = s.Email,
                    Phone = s.Phone,
                    Gender = s.Gender,
                    DepartmentId = s.DepartmentId,
                    PositionId = s.PositionId,
                    DOB = s.DOB,
                    DOE = s.DOE,
                    DOR= s.DOR,
                    Address = s.Address,
                    BasicSalary = s.BasicSalary,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt,
                }).FirstOrDefault();
            BindPositionData();
            BindDepartmentData();
            return View(employeeViewModel);
        }

        //update 
        public IActionResult Update(EmployeeViewModel employeeViewModel) 
        {
            try
            {
                EmployeeEntity employeeEntity = new EmployeeEntity()
                {
                    Id = employeeViewModel.Id,
                    Code = employeeViewModel.Code,
                    Name = employeeViewModel.Name,
                    Email = employeeViewModel.Email,
                    Phone = employeeViewModel.Phone,
                    Gender = employeeViewModel.Gender,
                    DepartmentId = employeeViewModel.DepartmentId,
                    PositionId = employeeViewModel.PositionId,
                    DOB = employeeViewModel.DOB,
                    DOE = employeeViewModel.DOE,
                    DOR = employeeViewModel.DOR,
                    Address = employeeViewModel.Address,
                    BasicSalary = employeeViewModel.BasicSalary,
                    CreatedAt = employeeViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.Employee.Update(employeeEntity);
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully update when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system";
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //delete 
        [HttpPost]
        public IActionResult Delete(List<string> selectedIds)
        {
            try
            {
                //delete by id
                if (selectedIds != null && selectedIds.Any())
                {
                    var itemsToDelete = _dbContext.Employee.Where(p => selectedIds.Contains(p.Id)).ToList();
                    foreach (var employee in itemsToDelete)
                    {
                        employee.IsInActive = true;
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
                TempData["info"] = "Error occur when the record delete from the system"+ ex.Message;
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //export to excel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] List<EmployeeViewModel> employeeData)
        {
            if (employeeData == null || !employeeData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Employee Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Code";
                worksheet.Cells[1, 3].Value = "Name";
                worksheet.Cells[1, 4].Value = "Email";
                worksheet.Cells[1, 5].Value = "Gender";
                worksheet.Cells[1, 6].Value = "DateOfBirth";
                worksheet.Cells[1, 7].Value = "DateOfEnter";
                worksheet.Cells[1, 8].Value = "DateOfRetired";
                worksheet.Cells[1, 9].Value = "Phone";
                worksheet.Cells[1, 10].Value = "BasicSalary";
                worksheet.Cells[1, 11].Value = "Address";
                worksheet.Cells[1, 12].Value = "PositionInfo";
                worksheet.Cells[1, 13].Value = "DepartmentInfo";

                // Populate data
                int row = 2;
                foreach (var record in employeeData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.Code;
                    worksheet.Cells[row, 3].Value = record.Name;
                    worksheet.Cells[row, 4].Value = record.Email;
                    worksheet.Cells[row, 5].Value = record.Gender;
                    worksheet.Cells[row, 6].Value = record.DOB;
                    worksheet.Cells[row, 7].Value = record.DOE;
                    worksheet.Cells[row, 8].Value = record.DOR;
                    worksheet.Cells[row, 9].Value = record.Phone;
                    worksheet.Cells[row, 10].Value = record.BasicSalary;
                    worksheet.Cells[row, 11].Value = record.Address;
                    worksheet.Cells[row, 12].Value = record.PositionInfo;
                    worksheet.Cells[row, 13].Value = record.DepartmentInfo;
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
