using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Numerics;

namespace CloudHRMS.Controllers
{
    public class ShiftController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;
        public ShiftController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            BindAttendancePolicyData();
            return View();
        }

        //save shift data
        [HttpPost]
        public async Task<IActionResult> Entry(ShiftViewModel shiftViewModel)
        {
            try
            {
                //check is exist or not
                bool shiftExist = await _dbContext.Shift
                                                    .AnyAsync(s => !s.IsInActive && s.Name == shiftViewModel.Name);
                if (!shiftExist)
                {
                    ShiftEntity shiftEntity = new ShiftEntity()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = shiftViewModel.Name,
                        InTime = shiftViewModel.InTime,
                        OutTime = shiftViewModel.OutTime,
                        LateAfter = shiftViewModel.LateAfter,
                        EarlyOutBefore = shiftViewModel.EarlyOutBefore,
                        AttendancePolicyId = shiftViewModel.AttendancePolicyId,
                        CreatedAt = DateTime.Now,
                    };
                    _dbContext.Shift.Add(shiftEntity);
                    _dbContext.SaveChanges();
                    TempData["info"] = "Successfully save when the record save to the system";
                    TempData["Status"] = true;
                    return RedirectToAction("Entry");
                }
                else
                {
                    TempData["info"] = "Shift is already exist, try another shift";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system"+ex.Message;
                TempData["Status"] = false;
            }
            BindAttendancePolicyData();
            return View(shiftViewModel);
        }

        //bind data from Shift
        public void BindAttendancePolicyData()
        {
            IList<AttendancePolicyViewModel> attendancePolicies = _dbContext.AttendancePolicy.Where(w => !w.IsInActive)
                .Select(s => new AttendancePolicyViewModel
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList();
            ViewBag.AttendancePolicies = attendancePolicies; // set up shift data to viewBag for UI
        }

        //converting input to timespan format
        public TimeSpan InputToTimespan(TimeSpan InputValue)
        {
            string strInput = InputValue.ToString();
            int intIndexOfDot = strInput.IndexOf('.');
            string inputToTimespan = (intIndexOfDot != -1) ? strInput.Substring(0, intIndexOfDot) : "0";

            return new TimeSpan(int.Parse(inputToTimespan),0,0);
        }

        //show record
        public async Task<IActionResult> List(string searchTerm = "")
        {
            //check is associated with another that is enable or not check box for delete
            // Get the IDs of Shifts that are referenced in both ShiftAssign and AttendanceMaster
            var shiftIdsFromTwoTbl = _dbContext.ShiftAssign
                                                .Where(sa => sa.ShiftId != null && !sa.IsInActive)
                                                .Select(sa => sa.ShiftId)
                                                .Intersect(
                                                    _dbContext.AttendanceMaster
                                                        .Where(am => am.ShiftId != null && !am.IsInActive)
                                                        .Select(am => am.ShiftId)
                                                )
                                                .Distinct()
                                                .ToList();
            // Find shifts that are not associated with any records in ShiftAssign or AttendanceMaster
            var associatedShiftIds = _dbContext.Shift
                                            .Where(s => !s.IsInActive && shiftIdsFromTwoTbl.Contains(s.Id))
                                            .Select(s => s.Id)
                                            .ToList();
            ViewBag.AssociatedShiftIds = associatedShiftIds;

            //change DataModel to ViewModel
            IList<ShiftViewModel> shifts = new List<ShiftViewModel>();
            //show all data
            var shiftQuery = (from s in _dbContext.Shift
                              join ap in _dbContext.AttendancePolicy
                              on s.AttendancePolicyId equals ap.Id
                              where !s.IsInActive
                              select new ShiftViewModel
                              {
                                  Id = s.Id,
                                  Name = s.Name,
                                  InTime = s.InTime,
                                  OutTime = s.OutTime,
                                  LateAfter = s.LateAfter,
                                  EarlyOutBefore = s.EarlyOutBefore,
                                  AttendancePolicyInfo = ap.Name,
                              });

            if (!string.IsNullOrEmpty(searchTerm))
            {
                //show search data
                shiftQuery = shiftQuery.Where(w => w.Name.Contains(searchTerm));
            }
            TempData["SearchTerm"] = searchTerm;
            shifts = await shiftQuery.ToListAsync();
            return View(shifts);
        }

        //to edit
        public IActionResult Edit(string Id) 
        { 
            ShiftViewModel shiftViewModel = _dbContext.Shift.Where(w => w.Id == Id && !w.IsInActive)
               .Select(s => new ShiftViewModel
               {
                   Id = s.Id,
                   Name = s.Name,
                   InTime = s.InTime,
                   OutTime = s.OutTime,
                   LateAfter = s.LateAfter,
                   EarlyOutBefore= s.EarlyOutBefore,
                   AttendancePolicyId = s.AttendancePolicyId,
                   CreatedOn = s.CreatedAt,
                   UpdatedOn = s.ModifiedAt,
               }).FirstOrDefault();
            BindAttendancePolicyData();
            return View(shiftViewModel);
        }

        //update
        public IActionResult Update(ShiftViewModel shiftViewModel) 
        {
            try
            {
                ShiftEntity shiftEntity = new ShiftEntity()
                {
                    Id = shiftViewModel.Id,
                    Name = shiftViewModel.Name,
                    InTime = shiftViewModel.InTime,
                    OutTime = shiftViewModel.OutTime,
                    LateAfter = shiftViewModel.LateAfter,
                    EarlyOutBefore = shiftViewModel.EarlyOutBefore,
                    AttendancePolicyId = shiftViewModel.AttendancePolicyId,
                    CreatedAt = shiftViewModel.CreatedOn,
                    ModifiedAt = DateTime.Now,
                };
                _dbContext.Shift.Update(shiftEntity);
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

        //delete
        [HttpPost]
        public ActionResult Delete(List<string> selectedIds)
        {
            try
            {
                //delete by id
                if (selectedIds != null && selectedIds.Any())
                {
                    var itemsToDelete = _dbContext.Shift.Where(p => selectedIds.Contains(p.Id)).ToList();
                    foreach (var shift in itemsToDelete)
                    {
                        shift.IsInActive = true;
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
        public IActionResult ExportToExcel([FromBody] List<ShiftViewModel> shiftData)
        {
            if (shiftData == null || !shiftData.Any())
            {
                return BadRequest("No data to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Shift Data");

                // Add headers
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "InTime";
                worksheet.Cells[1, 4].Value = "OutTime";
                worksheet.Cells[1, 5].Value = "LateAfter(Hours)";
                worksheet.Cells[1, 6].Value = "EarlyOutBefore(Hours)";
                worksheet.Cells[1, 7].Value = "AttendancePolicyInfo";

                // Populate data
                int row = 2;
                foreach (var record in shiftData)
                {
                    worksheet.Cells[row, 1].Value = row - 1;
                    worksheet.Cells[row, 2].Value = record.Name;
                    worksheet.Cells[row, 3].Value = record.InTime;
                    worksheet.Cells[row, 4].Value = record.OutTime;
                    worksheet.Cells[row, 5].Value = record.LateAfter;
                    worksheet.Cells[row, 6].Value = record.EarlyOutBefore;
                    worksheet.Cells[row, 7].Value = record.AttendancePolicyInfo;
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
