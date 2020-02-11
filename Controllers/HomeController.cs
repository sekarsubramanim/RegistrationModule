using _10_02_2020_RegistrationModule.Models;
using _10_02_2020_RegistrationModule.Models.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace _10_02_2020_RegistrationModule.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index([Bind(Exclude = "IsEmailVerified,ActivationCode")] RegTable users)
        {
            bool Status = false;
            string message = "";
            
            if (ModelState.IsValid)
            {
                //Email is already Exist 
                var isExist = IsEmailExist(users.EmailId);
                if (isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email already exist");
                    return View(users);
                }
                
                users.ActivationCode = Guid.NewGuid();
                               
                users.Password = Crypto.Hash(users.Password);

                users.IsEmailVerified = false;

               
                #region Save to Database
                using (RegistrationDBEntities db = new RegistrationDBEntities())
                {
                    db.RegTables.Add(users);
                    db.SaveChanges();

                    //Send Email to User
                    SendVerificationLinkEmail(users.EmailId, users.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link " +
                        " has been sent to your email id:" + users.EmailId;
                    Status = true;
                }
                #endregion
            }
            else
            {
                message = "Invalid Request";
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(users);
        }

        public bool IsEmailExist(string emailId)
        {
            using (RegistrationDBEntities db = new RegistrationDBEntities())
            {
                var data = db.RegTables.Where(d => d.EmailId == emailId).FirstOrDefault();
                return data != null;
            }
        }
        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode)
        {
            var verifyUrl = "/Home/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("dotnetawesome@gmail.com", "Dotnet Awesome");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = "********"; // Replace with actual password
            string subject = "Your account is successfully created!";

            string body = "<br/><br/>We are excited to tell you that your Dotnet Awesome account is" +
                " successfully created. Please click on the below link to verify your account" +
                " <br/><br/><a href='" + link + "'>" + link + "</a> ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
           
        }
    }
}