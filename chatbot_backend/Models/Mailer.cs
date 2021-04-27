using System;
using MailKit.Net.Smtp;
using MimeKit;

namespace chatbot_backend.Models {
    public static class Mailer {
        public static void Send(string body, string toMail) {
            string smtpMail = Environment.GetEnvironmentVariable("SENDER_MAIL");
            string smtpPW = Environment.GetEnvironmentVariable("SENDER_PW");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Mathias Gammelgaard", smtpMail));
            message.To.Add(new MailboxAddress("Mathias Gammelgaard", toMail));
            message.Subject = "How you doin'?";

            message.Body = new TextPart("html") {
                Text = body
            };

            using (var client = new SmtpClient()) {
                client.Connect("smtp.gmail.com", 465, true);
                client.Authenticate(smtpMail, smtpPW);

                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}
