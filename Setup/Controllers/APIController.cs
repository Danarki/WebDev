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
        public async Task<IActionResult> SendContact(string? subject = null, string? message = null, string? email = null)
        {

            ContactForm contactForm = new ContactForm();
            contactForm.Subject = subject;
            contactForm.Message = message;
            contactForm.Email = email;

            ValidationContext context = new ValidationContext(contactForm, serviceProvider: null, items: null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(contactForm, context, results, true);

            if (isValid == false)
            {
                StringBuilder sbrErrors = new StringBuilder();
                foreach (var validationResult in results)
                {
                    sbrErrors.AppendLine(validationResult.ErrorMessage);
                }
                return BadRequest(sbrErrors.ToString());
            }

            contactForm.Insert(_context);

            string body = "Message: " + contactForm.Message;

            MailController.SendEmail(
                contactForm.Email,
                contactForm.Subject,
                body
                );

            return Ok("Created Succesfully");
        }

        [Route("CreateRoom")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateRoom(string lobbyName, int gameId, int ownerId)
        {
            GameRoom room = new GameRoom();
            room.Name = lobbyName;
            room.GameID = gameId;
            room.OwnerID = ownerId;

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            room.OwnerToken = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[new Random(Guid.NewGuid().GetHashCode()).Next(s.Length)]).ToArray());

            room.Insert(_context);

            return Ok(room.ID);
        }

        [Route("RemoveRoom")]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteRoom(int roomID)
        {
            GameRoom gameRoom = _context.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

            if (gameRoom == null)
            {
                return BadRequest("Room not found");
            }

            try
            {
                gameRoom.Delete(_context);
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }

            return Ok("Removed Succesfully");
        }
    }
}
