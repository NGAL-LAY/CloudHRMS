using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;

        public DepartmentController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Entry(DepartmentViewModel departmentViewModel)
        {
            try
            {
                bool dCodeExist = await _dbContext.Department
                                            .AnyAsync(d => !d.IsInActive && d.Code == departmentViewModel.Code);
                if (!dCodeExist) 
                {
                    DepartmentEntity departmentEntity = new DepartmentEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = departmentViewModel.Code,
                        Name = departmentViewModel.Name,
                        ExtensionPhone = departmentViewModel.ExtensionPhone,
                        CreatedAt = DateTime.Now
                    };
                    _dbContext.Department.Add(departmentEntity);
                    _dbContext.SaveChanges();
                    TempData["Info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    // return clear form data
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["Info"] = "Department Code is already exist, try another code";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["Info"] = "Error occur when the record save to the system" + ex.Message;
                TempData["Status"] = false;
            }
            return View(departmentViewModel);
        }

        //show list
        public async Task<IActionResult> List(string searchTerm = "")
        {
            
            //check is associated with employee that is enable or not check box for delete
            var associatedDepartmentIds = _dbContext.Employee
                                        .Where(e => e.DepartmentId != null && !e.IsInActive)
                                        .Join(_dbContext.Department.Where(p => !p.IsInActive),
                                              e => e.DepartmentId,
                                              p => p.Id,
                                              (e, p) => e.DepartmentId)
                                        .Distinct()
                                        .ToList();
            ViewBag.AssociatedDepartmentIds = associatedDepartmentIds;

            //change DataModel to ViewModel
            IList<DepartmentViewModel> departments = new List<DepartmentViewModel>();
            //show all data
            var departmentQuery =  _dbContext.Department.Where(w => !w.IsInActive)
                                                        .Select(s => new DepartmentViewModel
                                                        {
                                                            Id = s.Id,
                                                            Code = s.Code,
                                                            Name = s.Name,
                                                            ExtensionPhone = s.ExtensionPhone
                                                        });
            if (!string.IsNullOrEmpty(searchTerm))
            {
                departmentQuery = departmentQuery.Where(d => d.Code.Contains(searchTerm)||d.Name.Contains(searchTerm));
            }
            TempData["SearchTerm"] = searchTerm;
            departments = await departmentQuery.ToListAsync();
            return View(departments);// change PositionViewModel to View 
        }

        //edit
        public IActionResult Edit(string Id)
        {
            DepartmentViewModel departmentView = _dbContext.Department.Where(w => w.Id == Id && !w.IsInActive).Select(s => new DepartmentViewModel
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                ExtensionPhone = s.ExtensionPhone,
                CreatedOn = s.CreatedAt,
                UpdatedOn = s.ModifiedAt
            }).FirstOrDefault();
            return View(departmentView);
        }

        //update
        [HttpPost]
        public IActionResult Update(DepartmentViewModel departmentViewModel)
        {
            try
            {
                //change viewModel to Entity
                DepartmentEntity departmentEntity = new DepartmentEntity()
                {
                    Id = departmentViewModel.Id,
                    Code = departmentViewModel.Code,
                    Name = departmentViewModel.Name,
                    ExtensionPhone = departmentViewModel.ExtensionPhone,
                    ModifiedAt = DateTime.Now,
                    CreatedAt = departmentViewModel.CreatedOn
                };
                //update in entity
                _dbContext.Department.Entry(departmentEntity).State = EntityState.Modified;
                //update in db
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
        [HttpPost]
        public IActionResult Delete(List<string> selectedIds)
        {
            try
            {
                //delete by id
                if (selectedIds != null && selectedIds.Any())
                {
                    var itemsToDelete = _dbContext.Department.Where(d => selectedIds.Contains(d.Id)).ToList();
                    foreach (var department in itemsToDelete)
                    {
                        department.IsInActive = true;
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
                TempData["info"] = "Error occur when the record delete from the system" + ex.Message;
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //export to excel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] List<DepartmentViewModel> departmentData)
        {
            if (departmentData == null || !departmentData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Department Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Code";
                worksheet.Cells[1, 3].Value = "Name";
                worksheet.Cells[1, 4].Value = "ExtensionPhone";

                // Populate data
                int row = 2;
                foreach (var record in departmentData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.Code;
                    worksheet.Cells[row, 3].Value = record.Name;
                    worksheet.Cells[row, 4].Value = record.ExtensionPhone;
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