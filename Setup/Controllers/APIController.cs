using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NuGet.Common;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebDev.Models;

namespace WebDev.Controllers
{
    [Route("api/")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private WebAppContext _context;

        public APIController(WebAppContext context)
        {
            _context = context;
        }

        [Route("test")]
        [HttpPost]
        public async Task<IActionResult> test()
        {
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("Strict-Transport-Security","max-age=15724800");
            return Ok("chefgreg");
        }

        // Function for saving contact form data
        [Route("SendContact")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContact(string? subject = null, string? message = null, string? email = null)
        {
            // set headers
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("Strict-Transport-Security", "max-age=15724800");

            ContactForm contactForm = new ContactForm();
            contactForm.Subject = subject;
            contactForm.Message = message;
            contactForm.Email = email;

            // create validator
            ValidationContext context = new ValidationContext(contactForm, serviceProvider: null, items: null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(contactForm, context, results, true);

            using (WebAppContext db = new WebAppContext())
            {
                // check if validator failed
                if (!isValid)
                {
                    StringBuilder sbrErrors = new StringBuilder();
                    foreach (ValidationResult validationResult in results)
                    {
                        sbrErrors.AppendLine(validationResult.ErrorMessage);
                    }

                    // log errors
                    LogItem logItem = new LogItem();

                    logItem.Message = "Validation failed for contact form with errors:" + sbrErrors.ToString();
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "API";
                    logItem.Type = "SendContact";
                    logItem.IsError = true;

                    db.LogItem.Add(logItem);

                    db.SaveChanges();

                    // return errors in request
                    return BadRequest(sbrErrors.ToString());
                }

                contactForm.Insert(db);

                return Ok("Created Succesfully");
            }
        }

        // function for creating a gameroom
        [Route("CreateRoom")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(string lobbyName, int ownerId)
        {
            // set headers
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("Strict-Transport-Security", "max-age=15724800");

            GameRoom room = new GameRoom();
            room.Name = lobbyName;
            room.HasStarted = false;
            room.OwnerID = ownerId;

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            // create token for authentication
            room.OwnerToken = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[new Random(BCrypt.Net.BCrypt.GenerateSalt().GetHashCode()).Next(s.Length)]).ToArray());

            // create validator
            ValidationContext context = new ValidationContext(room, serviceProvider: null, items: null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(room, context, results, true);

            using (WebAppContext db = new WebAppContext())
            {
                // check if validator has failed
                if (!isValid)
                {
                    StringBuilder sbrErrors = new StringBuilder();
                    foreach (ValidationResult validationResult in results)
                    {
                        sbrErrors.AppendLine(validationResult.ErrorMessage);
                    }

                    // log errors
                    LogItem logItem = new LogItem();

                    logItem.Message = "Validation failed for contact form with errors:" + sbrErrors.ToString();
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "API";
                    logItem.Type = "CreateRoom";
                    logItem.IsError = true;

                    db.LogItem.Add(logItem);

                    db.SaveChanges();

                    return BadRequest(sbrErrors.ToString());
                }

                room.Insert(_context);

                return Ok(room.ID);
            }
        }

        // function for updating users, only used by moderator
        [Route("UpdateUser")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(string id, string username, string email, string role)
        {
            // set headers
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("Strict-Transport-Security", "max-age=15724800");

            try
            {
                using (WebAppContext db = new WebAppContext())
                {
                    User user = db.Users.Where(x => x.ID == int.Parse(id)).FirstOrDefault();

                    user.Username = username;
                    user.Email = email;
                    user.Role = (Role)int.Parse(role);

                    db.SaveChanges();

                    LogItem log = new LogItem();
                    log.IsError = false;
                    log.Message = "User with ID: " + user.ID + " was changed";
                    log.Source = "API";
                    log.Type = "UpdateUser";
                    log.TimeOfOccurence = DateTime.Now;

                    db.SaveChanges();
                }

                return Ok("Succesfully changed user");
            }
            catch (Exception ex)
            {
                using (WebAppContext db = new WebAppContext())
                {
                    LogItem log = new LogItem();
                    log.IsError = true;
                    log.Message = ex.InnerException.ToString();
                    log.Source = "API";
                    log.Type = "UpdateUser";
                    log.TimeOfOccurence = DateTime.Now;

                    db.SaveChanges();


                    return BadRequest(ex.InnerException);
                }
            }
        }
    }
}
