using CloudHRMS.DAO;
using CloudHRMS.Models.ViewModels;
using CloudHRMS.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CloudHRMS.Controllers
{
    public class PositionController : Controller
    {
        //db variable
        private readonly CloudHRMSApplicationDbContext _dbContext;

        public PositionController(CloudHRMSApplicationDbContext dbContext)
        {
            this._dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Entry(PositionViewModel positionViewModel)
        {
            try
            {
                // check position duplicate or not
                bool pCodeExists = await _dbContext.Position
                                                .AnyAsync(p => !p.IsInActive && p.Code == positionViewModel.Code);
                if (!pCodeExists)
                {
                    // change ViewModel to Entity data
                    PositionEntity positionEntity = new PositionEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = positionViewModel.Code,
                        Name = positionViewModel.Name,
                        Level = positionViewModel.Level,
                        CreatedAt = DateTime.Now
                    };
                    _dbContext.Position.Add(positionEntity);// change Entity to dbContext
                    await _dbContext.SaveChangesAsync(); // save to DB
                    TempData["Info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    // return clear form data
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["Info"] = "Position Code is already exist, try another code";
                    TempData["Status"] = false;
                }
                
            }
            catch (Exception ex)
            {
                TempData["Info"] = "Error occur when the record save to the system" + ex.Message;
                TempData["Status"] = false;
            }
            return View(positionViewModel);
        }

        //show list
        public async Task<IActionResult> List(string searchTerm="")
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
            IList<PositionViewModel> positions = new List<PositionViewModel>();
            //show all data
            var positionQuery = _dbContext.Position.Where(w => !w.IsInActive)
                                                    .Select(s => new PositionViewModel
                                                    {
                                                        Id = s.Id,
                                                        Code = s.Code,
                                                        Name = s.Name,
                                                        Level = s.Level
                                                    });
            if (!string.IsNullOrEmpty(searchTerm))
            {
                //show search data
                positionQuery = positionQuery.Where(p => p.Code.Contains(searchTerm) || p.Name.Contains(searchTerm));
            }
            
            TempData["SearchTerm"] = searchTerm;
            positions = await positionQuery.ToListAsync();
            return View(positions);// change PositionViewModel to View 
        }

        //edit data
        public IActionResult Edit(string id)
        {
            PositionViewModel positionView = _dbContext.Position.Where(w => w.Id == id && !w.IsInActive).Select(s => new PositionViewModel
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Level = s.Level,
                    CreatedOn = s.CreatedAt,
                    UpdatedOn = s.ModifiedAt
                }).FirstOrDefault();
            return View(positionView);
        }

        //update
        [HttpPost]
        public IActionResult Update(PositionViewModel positionViewModel)
        {
            try
            {
                //change viewModel to Entity
                PositionEntity positionEntity = new PositionEntity()
                {
                    Id = positionViewModel.Id,
                    Code = positionViewModel.Code,
                    Name = positionViewModel.Name,
                    Level = positionViewModel.Level,
                    ModifiedAt = DateTime.Now,
                    CreatedAt = positionViewModel.CreatedOn
                };
                //update by entity
                _dbContext.Position.Entry(positionEntity).State = EntityState.Modified;
                //_dbContext.Position.Update(positionEntity);
                Console.WriteLine("Console Results",_dbContext);
                //update in db
                _dbContext.SaveChanges();
                TempData["info"] = "Successfully update when the record update to the system";
                TempData["Status"] = true;
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record update to the system"+ ex.Message;
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
                if(selectedIds != null && selectedIds.Any())
                {
                    var itemsToDelete = _dbContext.Position.Where(p => selectedIds.Contains(p.Id)).ToList();
                    foreach (var position in itemsToDelete)
                    {
                        position.IsInActive = true;
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
                TempData["Status"] = false;
            }
            return RedirectToAction("List");
        }

        //export to excel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromBody] List<PositionViewModel> positionData)
        {
            if (positionData == null || !positionData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Position Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Code";
                worksheet.Cells[1, 3].Value = "Name";
                worksheet.Cells[1, 4].Value = "Level";

                // Populate data
                int row = 2;
                foreach (var record in positionData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.Code;
                    worksheet.Cells[row, 3].Value = record.Name;
                    worksheet.Cells[row, 4].Value = record.Level;
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
