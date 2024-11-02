using CloudHRMS.DAO;
using CloudHRMS.Models.Entities;
using CloudHRMS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudHRMS.Controllers
{
    public class LoginController : Controller
    {
        private readonly CloudHRMSApplicationDbContext _dbContext;
        public LoginController(CloudHRMSApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Entry()
        {
            return View();
        }

        //register login user
        [HttpPost]
        public async Task<IActionResult> Entry(LoginViewModel loginViewModel)
        {
            try
            {
                //check exist or not
                bool loginUserExist = await _dbContext.Login
                                            .AnyAsync(l => !l.IsInActive && l.Code == loginViewModel.Code && l.UserName == loginViewModel.UserName);

                //select position to set userRole
                string userRole =await (from p in _dbContext.Position
                                   join e in _dbContext.Employee on p.Id equals e.PositionId
                                   where !e.IsInActive && e.Code == loginViewModel.Code
                                   select p.Name).FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(userRole))
                {
                    if (!loginUserExist)
                    {
                        LoginEntity loginEntity = new LoginEntity()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Code = loginViewModel.Code,
                            UserName = loginViewModel.UserName,
                            Password = loginViewModel.Password,
                            Role = (userRole.ToString().ToLower().Trim() == "manager" ? "admin" : "user"),
                            CreatedAt = DateTime.Now,
                        };
                        _dbContext.Login.Add(loginEntity);
                        await _dbContext.SaveChangesAsync();
                        HttpContext.Session.SetString("IsLoggedIn", "true");
                        // Redirect to a home page 
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        TempData["info"] = "If Manager, try another username! User already exist";
                        TempData["Status"] = false;
                    }
                    
                }
                else
                {
                    TempData["info"] = "Invalid user code, contact your admin";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system" + ex.Message;
                TempData["Status"] = false;
            }
            return View(loginViewModel);
        }

        //show empty view
        public IActionResult UpdatePassword()
        {
            return View();
        }

        [HttpPost]
        //forgot password 
        public async Task<IActionResult> UpdatePassword(LoginViewModel loginViewModel)
        {
            try
            {
                var userToUpdate = await _dbContext.Login.Where(w => w.Code == loginViewModel.Code
                                                               && w.UserName == loginViewModel.UserName
                                                               && !w.IsInActive).FirstOrDefaultAsync();
                if (userToUpdate != null) 
                {
                    userToUpdate.Password = loginViewModel.Password;
                    userToUpdate.ModifiedAt = DateTime.Now;
                    _dbContext.SaveChanges();
                    TempData["info"] = "Successfully password update";
                    TempData["Status"] = true;
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["info"] = "Invalid user! update failed, contact your admin";
                    TempData["Status"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["info"] = "Error occur when the record save to the system" + ex.Message;
                TempData["Status"] = false;
            }
            return View(loginViewModel);
        }

        //show empty view
        public IActionResult Login()
        {
            return View();
        }

        //login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            bool userIsExist = await _dbContext.Login.AnyAsync(w => w.Code == loginViewModel.Code
                                                            && w.UserName == loginViewModel.UserName
                                                            && w.Password == loginViewModel.Password
                                                            && !w.IsInActive);
            //test for another people
            if(loginViewModel.UserName.ToLower().Trim() == "admin" && loginViewModel.Password == "00000000")
            {
                // Set session variable to track login status
                HttpContext.Session.SetString("IsLoggedIn", "true");
                // Redirect to a home page
                return RedirectToAction("Index", "Home");
            }
            else
            {
                //real system flow
                if (userIsExist)
                {
                    // Set session variable to track login status
                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    // Redirect to a home page
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["info"] = "Invalid user or password! login failed, contact your admin";
                    TempData["Status"] = false;
                    return View();
                }
            }
        }

        //logout
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsLoggedIn"); 
            return RedirectToAction("Index", "Home");
        }
    }
}