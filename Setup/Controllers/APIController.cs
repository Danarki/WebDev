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

        [Route("SendContact")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContact(string? subject = null, string? message = null, string? email = null)
        {
            ContactForm contactForm = new ContactForm();
            contactForm.Subject = subject;
            contactForm.Message = message;
            contactForm.Email = email;

            ValidationContext context = new ValidationContext(contactForm, serviceProvider: null, items: null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(contactForm, context, results, true);
            
            using (WebAppContext db = new WebAppContext())
            {
                if (!isValid)
                {
                    StringBuilder sbrErrors = new StringBuilder();
                    foreach (ValidationResult validationResult in results)
                    {
                        sbrErrors.AppendLine(validationResult.ErrorMessage);
                    }

                    LogItem logItem = new LogItem();

                    logItem.Message = "Validation failed for contact form with errors:" + sbrErrors.ToString();
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "API";
                    logItem.Type = "SendContact";
                    logItem.IsError = true;

                    db.LogItem.Add(logItem);

                    db.SaveChanges();

                    return BadRequest(sbrErrors.ToString());
                }

                contactForm.Insert(db);

                string body = "Message: " + contactForm.Message;

                MailController.SendEmail(
                    contactForm.Email,
                    contactForm.Subject,
                    body
                );

                return Ok("Created Succesfully");
            }
        }

        [Route("CreateRoom")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(string lobbyName, int ownerId)
        {
            GameRoom room = new GameRoom();
            room.Name = lobbyName;
            room.HasStarted = false;
            room.OwnerID = ownerId;

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            room.OwnerToken = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[new Random(BCrypt.Net.BCrypt.GenerateSalt().GetHashCode()).Next(s.Length)]).ToArray());

            ValidationContext context = new ValidationContext(room, serviceProvider: null, items: null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(room, context, results, true);

            using (WebAppContext db = new WebAppContext())
            {
                if (!isValid)
                {
                    StringBuilder sbrErrors = new StringBuilder();
                    foreach (ValidationResult validationResult in results)
                    {
                        sbrErrors.AppendLine(validationResult.ErrorMessage);
                    }

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
    }
}
