using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class UserController : Controller
    {
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            using (WebAppContext db = new WebAppContext())
            {
                User? user = db.Users.Where(x => x.Email == email).FirstOrDefault<User>();

                if (user == null)
                {
                    string message = "Combination of this username/password is not present in database";
                    @ViewData["RegistrationMessage"] = message;

                    LogItem logItem = new LogItem();

                    logItem.Message = message + " for email: " + email;
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "Login";
                    logItem.Type = "Email";
                    logItem.IsError = true;


                    db.LogItem.Add(logItem);

                    db.SaveChanges();


                    return View("Index");
                }

                bool verifyHash = BCrypt.Net.BCrypt.Verify(password, user.Password);

                if (!verifyHash)
                {
                    string message = "Combination of this username/password is not present in database";

                    LogItem logItem = new LogItem();

                    logItem.Message = message + " for email: " + email;
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "Login";
                    logItem.Type = "Password";
                    logItem.IsError = true;

                    db.LogItem.Add(logItem);

                    db.SaveChanges();

                    @ViewData["ErrorTitle"] = "Error ID: " + logItem.ID;
                    @ViewData["ErrorMessage"] = message;

                    return View("Index");
                }

                HttpContext.Session.SetInt32("LoggedIn", 1);
                HttpContext.Session.SetInt32("UserID", user.ID);
                return Redirect("/Home");
            }
        }

        [HttpPost]
        public IActionResult Register(string email, string username, string password, string passwordConfirmation)
        {
            if (password.Length < 16)
            {
                @ViewData["ErrorMessage"] = "The password is not long enough! The requirements is longer than 15 characters";
                return View("Index");
            }

            if (password != passwordConfirmation)
            {
                @ViewData["ErrorMessage"] = "Confirmation password was not equal!";
                return View("Index");
            }

            using (WebAppContext db = new WebAppContext())
            {
                if (db.Users.Where(x => x.Email == email).FirstOrDefault<User>() != null)
                {
                    @ViewData["ErrorMessage"] = "Email is already in use!";
                    return View("Index");
                }

                try
                {
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                    Random random = new Random();
                    string token = new string(Enumerable.Repeat(chars, 32)
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

                    if (!isValid)
                    {
                        StringBuilder sbrErrors = new StringBuilder();
                        foreach (ValidationResult validationResult in results)
                        {
                            sbrErrors.AppendLine(validationResult.ErrorMessage);
                        }

                        LogItem logItem = new LogItem();

                        logItem.Message = "Validation failed for registration form with errors:" + sbrErrors.ToString();
                        logItem.TimeOfOccurence = DateTime.Now;
                        logItem.Source = "User";
                        logItem.Type = "Register";
                        logItem.IsError = true;

                        db.LogItem.Add(logItem);

                        db.SaveChanges();

                        @ViewData["ErrorTitle"] = "Error ID: " + logItem.ID;
                        @ViewData["ErrorMessage"] = "A field was not filled correctly, try again!";
                    }
                    else
                    {
                        user.Insert(db);

                        string body = "Verify your email: " + Request.Host + "/User/VerifyEmail?token=" +
                                      user.VerifyToken;

                        MailController.SendEmail(
                            user.Email,
                            "Verify your email",
                            body
                        );

                        @ViewData["ErrorMessage"] = "User has been created, check your email.";
                    }
                }
                catch (Exception e)
                {
                    @ViewData["ErrorMessage"] = "A field was not filled correctly, try again!";
                }

                return View("Index");
            }
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            using (WebAppContext db = new WebAppContext())
            {
                User user = db.Users.Where(x => x.Email == email).FirstOrDefault();

                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                Random random = new Random();
                string token = new string(Enumerable.Repeat(chars, 32)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                user.PasswordToken = token;

                db.SaveChanges();

                string body = "Reset your password: " + Request.Host + "/User/PasswordReset?token=" + user.PasswordToken;

                MailController.SendEmail(
                    user.Email,
                    "Reset your password",
                    body
                );

                @ViewData["ErrorMessage"] = "An email has been sent to reset your password.";

                LogItem logItem = new LogItem();

                logItem.Message = "User sent reset password email to email: " + email;
                logItem.TimeOfOccurence = DateTime.Now;
                logItem.Source = "Login";
                logItem.Type = "PasswordResetMail";
                logItem.IsError = false;

                db.LogItem.Add(logItem);

                db.SaveChanges();
            }

            return View("Index");
        }

        [HttpPost]
        public IActionResult ResetPassword(string password, string passwordConfirmation, string userToken)
        {
            if (password != passwordConfirmation)
            {
                @ViewData["ErrorMessage"] = "The passwords you filled are not the same, try again!";

                return Redirect("/User/VerifyEmail?token=" + userToken);
            }

            using (WebAppContext db = new WebAppContext())
            {
                User user = db.Users.Where(x => x.PasswordToken == userToken).FirstOrDefault();

                user.Password = BCrypt.Net.BCrypt.HashPassword(password);

                db.SaveChanges();

                @ViewData["ErrorMessage"] = "Password had been reset, please try logging in now.";

                LogItem logItem = new LogItem();

                logItem.Message = "User succesfully reset password for email: " + user.Email;
                logItem.TimeOfOccurence = DateTime.Now;
                logItem.Source = "Login";
                logItem.Type = "PasswordReset";
                logItem.IsError = false;

                db.LogItem.Add(logItem);

                db.SaveChanges();
            }

            return Redirect("/");
        }

        public IActionResult PasswordReset(string token)
        {
            using (WebAppContext db = new WebAppContext())
            {
                if (db.Users.Where(x => x.PasswordToken == token).FirstOrDefault() == null)
                {
                    return Redirect("/");
                }
            }

            @ViewData["token"] = token;

            return View();
        }

        public IActionResult VerifyEmail(string token)
        {
            if (token != null)
            {
                using (WebAppContext db = new WebAppContext())
                {
                    User user = db.Users.Where(x => x.VerifyToken == token).FirstOrDefault();
                    if (user != null)
                    {
                        user.VerifiedAt = DateTime.Now;

                        db.SaveChanges();

                        @ViewData["ErrorMessage"] = "Your email has been verified, please log in now.";
                    }
                    else
                    {
                        LogItem logItem = new LogItem();
                        
                        logItem.Message = "Access-control failed on email verification with fake token";
                        logItem.TimeOfOccurence = DateTime.Now;
                        logItem.Source = "User";
                        logItem.Type = "VerifyEmail";
                        logItem.IsError = true;

                        db.LogItem.Add(logItem);

                        db.SaveChanges();
                    }
                }
            }

            return Redirect("/User/Index");
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("LoggedIn") == null)
            {
                HttpContext.Session.SetInt32("LoggedIn", 0);
            }
            else if (HttpContext.Session.GetInt32("LoggedIn").Equals(1))
            {
                return Redirect("/Home");
            }

            return View();
        }
    }
}
