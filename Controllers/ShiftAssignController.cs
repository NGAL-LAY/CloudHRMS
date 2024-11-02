using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class ShiftAssignController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;
        public ShiftAssignController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Entry()
        {
            BindEmployeeData();
            BindShiftData();
            return View();
        }

        // save
        [HttpPost]
        public async Task<IActionResult> Entry(ShiftAssignViewModel shiftAssignViewModel) 
        {
            try
            {
                //check is exist or not
                bool saExist = await _dbContext.ShiftAssign
                                                    .AnyAsync(sa => !sa.IsInActive 
                                                    && sa.EmployeeId == shiftAssignViewModel.EmployeeId
                                                    && sa.ShiftId == shiftAssignViewModel.ShiftId
                                                    && sa.ToDate.Date >= shiftAssignViewModel.ToDate.Date);

                if (!saExist)
                {
                    ShiftAssignEntity shiftAssignEntity = new ShiftAssignEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmployeeId = shiftAssignViewModel.EmployeeId,
                        ShiftId = shiftAssignViewModel.ShiftId,
                        FromDate = shiftAssignViewModel.FromDate,
                        ToDate = shiftAssignViewModel.ToDate,
                        CreatedAt = DateTime.Now,
                    };
                    _dbContext.ShiftAssign.Add(shiftAssignEntity);
                    _dbContext.SaveChanges();
                    TempData["info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Shift Assign is already exist, try another";
                    TempData["Status"] = false;
                }
                
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system"+ex.Message;
                TempData["Status"] = false;
            }
            BindEmployeeData();
            BindShiftData(); 
            return View(shiftAssignViewModel); 
        }

        //bind employee data
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

        //bind shift data
        public void BindShiftData()
        {
            IList<ShiftViewModel> shifts = _dbContext.Shift.Where(w => !w.IsInActive)
                .Select(s => new ShiftViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                }).ToList();
            ViewBag.Shifts = shifts;
        }

        //show list
        public async Task<IActionResult> List(string fromDate = "", string toDate = "")
        {
            //check is associated with employee that is enable or not check box for delete
            var associatedPositionIds = _dbContext.Employee
                                        .Where(e => e.PositionId != null && !e.IsInActive)
                                        .Join(_dbContext.Position.Where(p => !p.IsInActive),
                                              e => e.PositionId,
                                              p => p.Id,
                                              (e, p) => e.PositionId)
                                        .Distinct()
                                        .ToList();
            ViewBag.AssociatedPositionIds = associatedPositionIds;

            //change DataModel to ViewModel
            IList<ShiftAssignViewModel> shiftAssigns = new List<ShiftAssignViewModel>();
             var shiftAssignQuery = (from sa in _dbContext.ShiftAssign
                                     join e in _dbContext.Employee
                                                                  on sa.EmployeeId equals e.Id
                                     join s in _dbContext.Shift
                                     on sa.ShiftId equals s.Id
                                     where !sa.IsInActive && !e.IsInActive && !s.IsInActive
                                     select new ShiftAssignViewModel
                                     {
                                         Id = sa.Id,
                                         EmployeeInfo = e.Code + "/" + e.Name,
                                         ShiftName = s.Name,
                                         FromDate = sa.FromDate,
                                         ToDate = sa.ToDate,
                                     });

            if (DateTime.TryParse(fromDate, out DateTime parsedFromDate))
            {
                shiftAssignQuery = shiftAssignQuery.Where(w => w.FromDate.Date >= parsedFromDate);
            }
            if (DateTime.TryParse(toDate, out DateTime parsedToDate))
            {
                shiftAssignQuery = shiftAssignQuery.Where(w => w.ToDate.Date <= parsedToDate);
            }
            TempData["FromDate"] = fromDate;
            TempData["ToDate"] = toDate;
            shiftAssigns = await shiftAssignQuery.ToListAsync();
            return View(shiftAssigns);
        }

        //edit
        public IActionResult Edit(string Id)
        {
            ShiftAssignViewModel shiftAssignViewModel = _dbContext.ShiftAssign.Where(w => w.Id == Id && !w.IsInActive)
                .Select(s=> new ShiftAssignViewModel
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    ShiftId = s.ShiftId,
                    FromDate = s.FromDate,
                    ToDate = s.ToDate,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt,
                }).FirstOrDefault();
            BindEmployeeData();
            BindShiftData();
            return View(shiftAssignViewModel);
        }

        //update 
        public IActionResult Update(ShiftAssignViewModel shiftAssignViewModel)
        {
            try
            {
                ShiftAssignEntity shiftAssignEntity = new ShiftAssignEntity()
                {
                    Id = shiftAssignViewModel.Id,
                    EmployeeId = shiftAssignViewModel.EmployeeId,
                    ShiftId = shiftAssignViewModel.ShiftId,
                    FromDate = shiftAssignViewModel.FromDate,
                    ToDate = shiftAssignViewModel.ToDate,
                    CreatedAt = shiftAssignViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.ShiftAssign.Entry(shiftAssignEntity).State = EntityState.Modified;
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully update when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system"+ex.Message ;
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
                var itemsToDelete = _dbContext.ShiftAssign.Where(sa => selectedIds.Contains(sa.Id)).ToList();
                foreach (var shiftAssign in itemsToDelete)
                {
                    shiftAssign.IsInActive = true;
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
        public async Task<IActionResult> ExportToExcel([FromBody] List<ShiftAssignViewModel> shiftAssignData)
        {
            if (shiftAssignData == null || !shiftAssignData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Shift Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "EmployeeInfo";
                worksheet.Cells[1, 3].Value = "ShiftName";
                worksheet.Cells[1, 4].Value = "FromDate";
                worksheet.Cells[1, 5].Value = "ToDate";

                // Populate data
                int row = 2;
                foreach (var record in shiftAssignData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.EmployeeInfo;
                    worksheet.Cells[row, 3].Value = record.ShiftName;
                    worksheet.Cells[row, 4].Value = record.FromDate;
                    worksheet.Cells[row, 5].Value = record.ToDate;
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
