using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace WebApplicationBasic.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Configuração para o MailHog do docker-compose
            _smtpHost = "localhost";
            _smtpPort = 1025;
            _fromEmail = "noreply@basicerp.com";
            _fromName = "BasicERP";
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var builder = new BodyBuilder();

                if (isHtml)
                    builder.HtmlBody = body;
                else
                    builder.TextBody = body;

                message.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // MailHog não requer autenticação
                    await client.ConnectAsync(_smtpHost, _smtpPort, false);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                // Em produção, você deve logar o erro
                throw new Exception($"Erro ao enviar email: {ex.Message}", ex);
            }
        }

        public async Task SendOtpEmailAsync(string to, string userName, string otpCode)
        {
            var subject = "Seu código de verificação - BasicERP";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; }}
                        .header {{ background-color: #007bff; color: #ffffff; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .otp-code {{ font-size: 32px; font-weight: bold; color: #007bff; text-align: center; padding: 20px; background-color: #f8f9fa; border-radius: 5px; letter-spacing: 5px; }}
                        .footer {{ text-align: center; padding: 20px; color: #666666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>BasicERP</h1>
                        </div>
                        <div class='content'>
                            <p>Olá {userName},</p>
                            <p>Você solicitou um código de verificação para acessar sua conta.</p>
                            <p>Seu código de verificação é:</p>
                            <div class='otp-code'>{otpCode}</div>
                            <p><strong>Este código expira em 5 minutos.</strong></p>
                            <p>Se você não solicitou este código, ignore este email.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 BasicERP. Todos os direitos reservados.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendOtpEmailAsync(string to, string userName, string otpCode);
    }
}