using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class UserController : Controller
    {
        [HttpPost]
        public void Login(string email, string password)
        {

        }

        [HttpPost]
        public IActionResult Register(string email, string username, string password, string passwordConfirmation)
        {
            if (password != passwordConfirmation)
            {
                @ViewData["RegistrationMessage"] = "Confirmation password was not equal!";
                return View("Index");
            }

            try
            {
                string passHashed = BCrypt.Net.BCrypt.HashPassword(password);
                User user = new User();
                user.Email = email;
                user.Password = passHashed;
                user.Username = username;
                user.VerifiedAt = null;
                user.PasswordToken = null;

                ValidationContext context = new ValidationContext(user, serviceProvider: null, items: null);
                List<ValidationResult> results = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(user, context, results, true);

                if (isValid == false)
                {
                    @ViewData["RegistrationMessage"] = "A field was not filled correctly, try again!";
                }
                else
                {
                    user.Insert();

                    @ViewData["RegistrationMessage"] = "User has been created, check your email.";
                }
            }
            catch (Exception e)
            {
                @ViewData["RegistrationMessage"] = "A field was not filled correctly, try again!";
            } 

            return View("Index");
        }

        public IActionResult Index()
        {
            HomeController.WebAppContext.Database.EnsureCreated();
            //@ViewData["LoggedIn"] = true;

            return View();
        }
    }
}
