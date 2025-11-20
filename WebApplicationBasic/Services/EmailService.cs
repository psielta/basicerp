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

        public async Task SendPasswordResetEmailAsync(string to, string userName, string resetUrl)
        {
            var subject = "Redefinir senha - BasicERP";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; }}
                        .header {{ background-color: #212529; color: #ffffff; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .btn {{ display: inline-block; padding: 12px 30px; background-color: #0d6efd; color: #ffffff; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
                        .warning {{ background-color: #fff3cd; border: 1px solid #ffc107; color: #856404; padding: 10px; border-radius: 5px; margin: 20px 0; }}
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
                            <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
                            <p>Para criar uma nova senha, clique no botão abaixo:</p>
                            <div style='text-align: center;'>
                                <a href='{resetUrl}' class='btn'>Redefinir Senha</a>
                            </div>
                            <div class='warning'>
                                <strong>⚠️ Atenção:</strong><br>
                                Este link expira em 24 horas e só pode ser usado uma vez.
                            </div>
                            <p>Se você não solicitou a redefinição de senha, ignore este email e sua senha permanecerá inalterada.</p>
                            <p>Por segurança, este link não funcionará se você não o solicitou.</p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #e0e0e0;'>
                            <p style='font-size: 12px; color: #999;'>Se o botão não funcionar, copie e cole este link no seu navegador:<br>{resetUrl}</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 BasicERP. Todos os direitos reservados.</p>
                            <p>Este é um email automático, por favor não responda.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPasswordChangedEmailAsync(string to, string userName)
        {
            var subject = "Senha alterada com sucesso - BasicERP";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; }}
                        .header {{ background-color: #198754; color: #ffffff; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .success {{ background-color: #d4edda; border: 1px solid #28a745; color: #155724; padding: 10px; border-radius: 5px; margin: 20px 0; }}
                        .warning {{ background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 10px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; color: #666666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✓ Senha Alterada</h1>
                        </div>
                        <div class='content'>
                            <p>Olá {userName},</p>
                            <div class='success'>
                                <strong>✓ Sua senha foi alterada com sucesso!</strong><br>
                                Data e hora: {DateTime.Now:dd/MM/yyyy HH:mm}
                            </div>
                            <p>Sua senha da conta BasicERP foi alterada com sucesso.</p>
                            <div class='warning'>
                                <strong>⚠️ Não foi você?</strong><br>
                                Se você não fez esta alteração, entre em contato conosco imediatamente e altere sua senha.
                            </div>
                            <p>Dicas de segurança:</p>
                            <ul>
                                <li>Use senhas fortes e únicas para cada conta</li>
                                <li>Ative a verificação em duas etapas quando disponível</li>
                                <li>Nunca compartilhe sua senha com outras pessoas</li>
                                <li>Evite usar senhas em computadores públicos</li>
                            </ul>
                        </div>
                        <div class='footer'>
                            <p>© 2024 BasicERP. Todos os direitos reservados.</p>
                            <p>Este é um email automático de confirmação.</p>
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
        Task SendPasswordResetEmailAsync(string to, string userName, string resetUrl);
        Task SendPasswordChangedEmailAsync(string to, string userName);
    }
}