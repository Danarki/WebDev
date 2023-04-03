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
                    @TempData["ErrorMessage"] = message;

                    LogItem logItem = new LogItem();

                    logItem.Message = message + " for email: " + email;
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "Login";
                    logItem.Type = "Email";
                    logItem.IsError = false;


                    db.LogItem.Add(logItem);

                    db.SaveChanges();


                    return View("Index");
                }

                if (user.VerifiedAt == null)
                {
                    string body = "Verify your email by copy and pasting this link in your browser: " + Request.Host + "/User/VerifyEmail?token=" +
                                  user.VerifyToken;

                    MailController.SendEmail(
                        user.Email,
                        "Verify your email",
                        body
                    );

                    @TempData["ErrorMessage"] = "User has not been verified. A new email has been sent.";

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

                    @TempData["ErrorTitle"] = "Error ID: " + logItem.ID;
                    @TempData["ErrorMessage"] = message;

                    return View("Index");
                }

                HttpContext.Session.SetInt32("LoggedIn", 1);
                HttpContext.Session.SetInt32("UserID", user.ID);
                HttpContext.Session.SetInt32("Role", (int)user.Role);

                return Redirect("/Home");
            }
        }

        [HttpPost]
        public IActionResult Register(string email, string username, string password, string passwordConfirmation)
        {
            if (password.Length < 16)
            {
                @TempData["ErrorMessage"] = "The password is not long enough! The requirements is longer than 15 characters";
                return View("Index");
            }

            if (password != passwordConfirmation)
            {
                @TempData["ErrorMessage"] = "Confirmation password was not equal!";
                return View("Index");
            }

            using (WebAppContext db = new WebAppContext())
            {
                if (db.Users.Where(x => x.Email == email).FirstOrDefault<User>() != null)
                {
                    @TempData["ErrorMessage"] = "Email is already in use!";
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

                        @TempData["ErrorTitle"] = "Error ID: " + logItem.ID;
                        @TempData["ErrorMessage"] = "A field was not filled correctly, try again!";
                    }
                    else
                    {
                        user.Insert(db);

                        string body = "Verify your email by copy and pasting this link in your browser: " + Request.Host + "/User/VerifyEmail?token=" +
                                      user.VerifyToken;

                        MailController.SendEmail(
                            user.Email,
                            "Verify your email",
                            body
                        );

                        @TempData["ErrorMessage"] = "User has been created, check your email.";
                    }
                }
                catch (Exception e)
                {
                    @TempData["ErrorMessage"] = "A field was not filled correctly, try again!";
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

                string body = "Reset your password by copy and pasting this link to your browser: " + Request.Host + "/User/PasswordReset?token=" + user.PasswordToken;

                MailController.SendEmail(
                    user.Email,
                    "Reset your password",
                    body
                );

                @TempData["ErrorMessage"] = "An email has been sent to reset your password.";

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
                @TempData["ErrorMessage"] = "The passwords you filled are not the same, try again!";

                return Redirect("/User/VerifyEmail?token=" + userToken);
            }

            using (WebAppContext db = new WebAppContext())
            {
                User user = db.Users.Where(x => x.PasswordToken == userToken).FirstOrDefault();

                user.Password = BCrypt.Net.BCrypt.HashPassword(password);

                db.SaveChanges();

                @TempData["ErrorMessage"] = "Password had been reset, please try logging in now.";

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

        [HttpPost]
        public IActionResult UpdatePassword(string currentPassword, string newPassword, string newPasswordConfirmation,
            string userId, string userToken)
        {
            if (newPasswordConfirmation != newPassword)
            {
                @TempData["ErrorMessage"] = "Passwords are not equal!";
                return Redirect("/User/ChangePassword");
            }


            if (newPassword == null || newPassword.Length < 16)
            {
                @TempData["ErrorMessage"] = "New password is not long enough!";
                return Redirect("/User/ChangePassword");
            }


            try
            {
                using (WebAppContext db = new WebAppContext())
                {
                    User user = db.Users.Where(x => x.ID == int.Parse(userId) && x.PasswordToken == userToken).FirstOrDefault();

                    if (user == null)
                    {
                        LogItem logItem = new LogItem();
                        logItem.IsError = true;
                        logItem.Message = "User tried to alter other user's password.";
                        logItem.Source = "API";
                        logItem.Type = "UpdatePassword";
                        logItem.TimeOfOccurence = DateTime.Now;

                        db.SaveChanges();

                        @TempData["ErrorTitle"] = "Error ID: " + logItem.ID;
                        @TempData["ErrorMessage"] = "User not found!";
                        return Redirect("/User/ChangePassword");
                    }

                    string oldPassword = user.Password;
                    user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

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

                        logItem.Message = "Validation failed for password change form with errors:" +
                                          sbrErrors.ToString();
                        logItem.TimeOfOccurence = DateTime.Now;
                        logItem.Source = "API";
                        logItem.Type = "ChangePassword";
                        logItem.IsError = true;

                        db.LogItem.Add(logItem);

                        user.Password = oldPassword;

                        db.SaveChanges();

                        @TempData["ErrorTitle"] = "Error ID: " + logItem.ID;
                        @TempData["ErrorMessage"] = logItem.Message;
                        return Redirect("/User/ChangePassword");
                    }

                    db.SaveChanges();

                    LogItem log = new LogItem();
                    log.IsError = false;
                    log.Message = "User with ID: " + user.ID + " has changed their password";
                    log.Source = "API";
                    log.Type = "UpdatePassword";
                    log.TimeOfOccurence = DateTime.Now;

                    db.SaveChanges();
                }

                @TempData["ErrorMessage"] = "Succesfully changed password, please log in again";
                Logout();
                return Redirect("/");
            }
            catch (Exception ex)
            {
                using (WebAppContext db = new WebAppContext())
                {
                    LogItem log = new LogItem();
                    log.IsError = true;
                    log.Message = ex.InnerException.ToString();
                    log.Source = "API";
                    log.Type = "UpdatePassword";
                    log.TimeOfOccurence = DateTime.Now;

                    db.SaveChanges();

                    @TempData["ErrorTitle"] = "Error ID: " + log.ID;
                    @TempData["ErrorMessage"] = ex.InnerException;
                    return Redirect("/User/ChangePassword");
                }
            }
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

                        @TempData["ErrorMessage"] = "Your email has been verified, please log in now.";
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

        public IActionResult ChangePassword()
        {
            User user = null;
            using (WebAppContext db = new WebAppContext())
            {
                int userID = (int)HttpContext.Session.GetInt32("UserID");
                user = db.Users.Find(userID);

                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                user.PasswordToken = new string(Enumerable.Repeat(chars, 32)
                    .Select(s => s[new Random(BCrypt.Net.BCrypt.GenerateSalt().GetHashCode()).Next(s.Length)]).ToArray());

                db.SaveChanges();
            }

            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.SetInt32("LoggedIn", 0);
            HttpContext.Session.SetInt32("UserID", 0);
            HttpContext.Session.SetInt32("Role", 0);

            return Redirect("/");
        }
    }
}
