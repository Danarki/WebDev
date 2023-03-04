using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class UserController : Controller
    {
        private readonly WebAppContext _context;
        private IHttpContextAccessor _accessor;

        public UserController(WebAppContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _accessor = accessor;
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.Where(x => x.Email == email).FirstOrDefault<User>();

            if (user == null)
            {
                @ViewData["RegistrationMessage"] = "Email not found in database";
                return View("Index");
            };

            bool verifyHash = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (!verifyHash)
            {
                @ViewData["RegistrationMessage"] = "Email not found in database";
                return View("Index");
            }
            else
            {
                HttpContext.Session.SetInt32("LoggedIn", 1);
                HttpContext.Session.SetInt32("UserID", user.ID);
                return Redirect("/Home");
            }
        }

        [HttpPost]
        public IActionResult Register(string email, string username, string password, string passwordConfirmation)
        {
            if (password != passwordConfirmation)
            {
                @ViewData["RegistrationMessage"] = "Confirmation password was not equal!";
                return View("Index");
            }

            if (_context.Users.Where(x => x.Email == email).FirstOrDefault<User>() != null)
            {
                @ViewData["RegistrationMessage"] = "Email is already in use!";
                return View("Index");
            };

            try
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                var random = new Random();
                var token = new string(Enumerable.Repeat(chars, 16)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                string passHashed = BCrypt.Net.BCrypt.HashPassword(password);
                User user = new User();
                user.Email = email;
                user.Password = passHashed;
                user.Username = username;
                user.VerifyToken = token;
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
                    user.Insert(_context);

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
            if (HttpContext.Session.GetInt32("LoggedIn") == null)
            {
                HttpContext.Session.SetInt32("LoggedIn", 0);
            } else if (HttpContext.Session.GetInt32("LoggedIn").Equals(1))
            {
                return Redirect("/Home");
            }

            return View();
        }
    }
}
