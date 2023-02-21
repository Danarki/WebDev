﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Setup.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Setup.Controllers
{
    [Route("api/SendContact")]
    [ApiController]
    public class APIController : ControllerBase
    {
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

            contactForm.Insert();

            string body = "Message: " + contactForm.Message;

            MailController.SendEmail(
                contactForm.Email,
                contactForm.Subject,
                body
                );

            return Ok("Created Succesfully");
        }
    }
}