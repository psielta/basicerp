using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using EntityFrameworkProject.Data;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Owin.Security;
using Serilog;

namespace WebApplicationBasic.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionService _sessionService;
        private readonly IEmailService _emailService;

        public AuthenticationService(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ISessionService sessionService,
            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _sessionService = sessionService;
            _emailService = emailService;
        }

        public async Task<User> ValidateUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                Log.Warning("USER_VALIDATION_FAILED: Email vazio fornecido");
                return null;
            }

            var user = await _context.Users
                .Include(u => u.Memberships)
                    .ThenInclude(m => m.Organization)
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                Log.Warning("USER_VALIDATION_FAILED: Usuário não encontrado para email {Email}", email);
            }
            else
            {
                Log.Debug("USER_VALIDATION_SUCCESS: Usuário {UserId} encontrado para email {Email}", user.Id, email);
            }

            return user;
        }

        public async Task<bool> ValidatePasswordAsync(Guid userId, string password)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ProviderId == "local");

            if (account == null || string.IsNullOrEmpty(account.Password))
            {
                Log.Warning("PASSWORD_VALIDATION_FAILED: Conta local não encontrada para usuário {UserId}", userId);
                return false;
            }

            var isValid = _passwordHasher.VerifyPassword(password, account.Password);

            if (isValid)
            {
                Log.Information("PASSWORD_VALIDATION_SUCCESS: Senha validada para usuário {UserId}", userId);
            }
            else
            {
                Log.Warning("PASSWORD_VALIDATION_FAILED: Senha inválida para usuário {UserId}", userId);
            }

            return isValid;
        }

        public async Task<string> GenerateAndSendOtpAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Log.Error("OTP_GENERATION_FAILED: Usuário {UserId} não encontrado", userId);
                throw new InvalidOperationException("Usuário não encontrado");
            }

            // Gerar código OTP de 6 dígitos
            var otpCode = GenerateOtpCode();

            Log.Information("OTP_GENERATED: OTP gerado para usuário {UserId} ({Email})", userId, user.Email);

            // Armazenar OTP no metadata do usuário com expiração
            var metadata = string.IsNullOrEmpty(user.Metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(user.Metadata);

            metadata["otp"] = otpCode;
            metadata["otp_expires"] = DateTime.UtcNow.AddMinutes(5).ToString("O");

            user.Metadata = JsonSerializer.Serialize(metadata);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Debug("OTP_STORED: OTP armazenado no banco para usuário {UserId}", userId);

            // Enviar email
            try
            {
                await _emailService.SendOtpEmailAsync(user.Email, user.Name, otpCode);
                Log.Information("OTP_SENT: Email OTP enviado para {Email}", user.Email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OTP_SEND_FAILED: Falha ao enviar email OTP para {Email}", user.Email);
                throw;
            }

            return otpCode;
        }

        public async Task<bool> ValidateOtpAsync(Guid userId, string otpCode)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Log.Warning("OTP_VALIDATION_FAILED: Usuário {UserId} não encontrado", userId);
                return false;
            }

            try
            {
                Log.Debug("OTP_VALIDATION_ATTEMPT: Validando OTP para usuário {UserId}", userId);

                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(user.Metadata);

                if (!metadata.ContainsKey("otp") || !metadata.ContainsKey("otp_expires"))
                {
                    Log.Warning("OTP_VALIDATION_FAILED: Metadata não contém OTP para usuário {UserId}", userId);
                    return false;
                }

                // Extrair valores corretamente do JsonElement
                string storedOtp = null;
                string expiresStr = null;

                if (metadata["otp"].ValueKind == JsonValueKind.String)
                {
                    storedOtp = metadata["otp"].GetString();
                }

                if (metadata["otp_expires"].ValueKind == JsonValueKind.String)
                {
                    expiresStr = metadata["otp_expires"].GetString();
                }

                if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(expiresStr))
                {
                    Log.Warning("OTP_VALIDATION_FAILED: OTP ou data de expiração vazio para usuário {UserId}", userId);
                    return false;
                }

                // Usar DateTime.Parse com DateTimeStyles.RoundtripKind para preservar UTC
                var expires = DateTime.Parse(expiresStr, null, System.Globalization.DateTimeStyles.RoundtripKind);

                if (DateTime.UtcNow > expires)
                {
                    Log.Warning("OTP_EXPIRED: OTP expirado para usuário {UserId}. Expirou em {ExpiresAt}", userId, expires);
                    // OTP expirado - limpar
                    ClearOtp(user);
                    return false;
                }

                // Comparar códigos (trim para remover espaços)
                var match = storedOtp.Trim() == otpCode.Trim();

                if (match)
                {
                    Log.Information("OTP_VALIDATION_SUCCESS: OTP validado com sucesso para usuário {UserId}", userId);
                    // OTP válido - limpar após uso
                    ClearOtp(user);
                    return true;
                }

                Log.Warning("OTP_VALIDATION_FAILED: Código OTP incorreto para usuário {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OTP_VALIDATION_ERROR: Erro ao validar OTP para usuário {UserId}", userId);
                return false;
            }
        }

        public async Task<ClaimsIdentity> CreateIdentityAsync(User user, Guid organizationId)
        {
            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == organizationId);
            if (membership == null)
            {
                Log.Warning("IDENTITY_CREATION_FAILED: Usuário {UserId} não tem acesso à organização {OrganizationId}",
                    user.Id, organizationId);
                throw new InvalidOperationException("Usuário não tem acesso a esta organização");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("OrganizationId", organizationId.ToString()),
                new Claim("OrganizationName", membership.Organization.Name),
                new Claim(ClaimTypes.Role, membership.Role),
                new Claim("GlobalRole", user.Role ?? "user")
            };

            Log.Information("IDENTITY_CREATED: Identidade criada para usuário {UserId} ({Email}) na organização {OrganizationId} ({OrganizationName}) com role {Role}",
                user.Id, user.Email, organizationId, membership.Organization.Name, membership.Role);

            return new ClaimsIdentity(claims, "ApplicationCookie");
        }

        public void SignIn(HttpContextBase httpContext, ClaimsIdentity identity, Session session)
        {
            var authManager = httpContext.GetOwinContext().Authentication;

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            // Adicionar token da sessão aos claims
            identity.AddClaim(new Claim("SessionToken", session.Token));

            authManager.SignIn(authProperties, identity);

            var userId = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var orgId = identity.Claims.FirstOrDefault(c => c.Type == "OrganizationId")?.Value;
            var orgName = identity.Claims.FirstOrDefault(c => c.Type == "OrganizationName")?.Value;

            Log.Information("LOGIN_SUCCESS: Usuário {UserId} ({Email}) autenticado na organização {OrganizationId} ({OrganizationName}) - SessionToken: {SessionToken}",
                userId, email, orgId, orgName, session.Token);
        }

        public void SignOut(HttpContextBase httpContext)
        {
            var authManager = httpContext.GetOwinContext().Authentication;

            // Invalidar sessão no banco
            var claimsPrincipal = httpContext.User as ClaimsPrincipal;
            var sessionToken = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "SessionToken")?.Value;
            var userId = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                _sessionService.InvalidateSession(sessionToken);
                Log.Information("LOGOUT_SUCCESS: Usuário {UserId} ({Email}) deslogado - SessionToken: {SessionToken}",
                    userId, email, sessionToken);
            }
            else
            {
                Log.Warning("LOGOUT_ATTEMPT: Tentativa de logout sem sessão válida");
            }

            authManager.SignOut("ApplicationCookie");
        }

        public async Task<List<Organization>> GetUserOrganizationsAsync(Guid userId)
        {
            var memberships = await _context.Memberships
                .Include(m => m.Organization)
                .Where(m => m.UserId == userId)
                .ToListAsync();

            return memberships.Select(m => m.Organization).ToList();
        }

        public async Task<bool> CreateLocalAccountAsync(Guid userId, string password)
        {
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ProviderId == "local");

            if (existingAccount != null)
                return false;

            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProviderId = "local",
                AccountId = userId.ToString(),
                Password = _passwordHasher.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateOtpCode()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var random = BitConverter.ToUInt32(bytes, 0);
                return (random % 900000 + 100000).ToString();
            }
        }

        private void ClearOtp(User user)
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(user.Metadata);
                metadata.Remove("otp");
                metadata.Remove("otp_expires");
                user.Metadata = JsonSerializer.Serialize(metadata);
                user.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();
            }
            catch { }
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email, string baseUrl = null)
        {
            Log.Information("PASSWORD_RESET_REQUEST: Solicitação de reset de senha para email {Email}", email);

            var user = await ValidateUserByEmailAsync(email);
            if (user == null)
            {
                Log.Warning("PASSWORD_RESET_FAILED: Usuário não encontrado para email {Email}", email);
                return null; // Don't reveal if user exists
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.ProviderId == "local");

            if (account == null)
            {
                Log.Warning("PASSWORD_RESET_FAILED: Usuário {UserId} não possui conta local", user.Id);
                return null; // User doesn't have a password account
            }

            // Generate secure reset token
            var token = GenerateSecureToken();
            account.ResetToken = token;
            account.ResetTokenExpires = DateTime.UtcNow.AddHours(24);
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("PASSWORD_RESET_TOKEN_GENERATED: Token gerado para usuário {UserId} ({Email}), expira em {ExpiresAt}",
                user.Id, user.Email, account.ResetTokenExpires);

            // Send reset email
            try
            {
                if (string.IsNullOrEmpty(baseUrl))
                {
                    // Fallback para configuração no Web.config
                    baseUrl = System.Configuration.ConfigurationManager.AppSettings["BaseUrl"] ?? "http://localhost:44318";
                }
                var resetUrl = $"{baseUrl}/Auth/ResetPassword?token={token}";
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetUrl);

                Log.Information("PASSWORD_RESET_EMAIL_SENT: Email de reset enviado para {Email}", user.Email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PASSWORD_RESET_EMAIL_FAILED: Falha ao enviar email de reset para {Email}", user.Email);
                throw;
            }

            return token;
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Log.Warning("PASSWORD_RESET_TOKEN_VALIDATION_FAILED: Token vazio fornecido");
                return false;
            }

            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ResetToken == token);

            if (account == null)
            {
                Log.Warning("PASSWORD_RESET_TOKEN_VALIDATION_FAILED: Token não encontrado");
                return false;
            }

            if (account.ResetTokenExpires == null || account.ResetTokenExpires < DateTime.UtcNow)
            {
                Log.Warning("PASSWORD_RESET_TOKEN_EXPIRED: Token expirado para usuário {UserId}", account.UserId);
                return false;
            }

            Log.Debug("PASSWORD_RESET_TOKEN_VALID: Token válido para usuário {UserId}", account.UserId);
            return true;
        }

        public async Task<User> GetUserByResetTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var account = await _context.Accounts
                .Include(a => a.User)
                    .ThenInclude(u => u.Memberships)
                        .ThenInclude(m => m.Organization)
                .FirstOrDefaultAsync(a => a.ResetToken == token);

            if (account == null || account.ResetTokenExpires == null || account.ResetTokenExpires < DateTime.UtcNow)
                return null;

            return account.User;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
            {
                Log.Warning("PASSWORD_RESET_FAILED: Token ou senha vazio fornecido");
                return false;
            }

            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ResetToken == token);

            if (account == null || account.ResetTokenExpires == null || account.ResetTokenExpires < DateTime.UtcNow)
            {
                Log.Warning("PASSWORD_RESET_FAILED: Token inválido ou expirado");
                return false;
            }

            // Update password
            account.Password = _passwordHasher.HashPassword(newPassword);
            account.ResetToken = null;
            account.ResetTokenExpires = null;
            account.LastPasswordChange = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("PASSWORD_RESET_SUCCESS: Senha redefinida para usuário {UserId} ({Email})",
                account.UserId, account.User.Email);

            // Send confirmation email
            try
            {
                await _emailService.SendPasswordChangedEmailAsync(account.User.Email, account.User.Name);
                Log.Information("PASSWORD_RESET_CONFIRMATION_SENT: Email de confirmação enviado para {Email}", account.User.Email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PASSWORD_RESET_CONFIRMATION_FAILED: Falha ao enviar email de confirmação para {Email}", account.User.Email);
                // Não falhar a operação por causa do email
            }

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ProviderId == "local");

            if (account == null)
            {
                Log.Warning("PASSWORD_CHANGE_FAILED: Conta local não encontrada para usuário {UserId}", userId);
                return false;
            }

            // Validate current password
            if (!_passwordHasher.VerifyPassword(currentPassword, account.Password))
            {
                Log.Warning("PASSWORD_CHANGE_FAILED: Senha atual incorreta para usuário {UserId} ({Email})",
                    userId, account.User.Email);
                return false;
            }

            // Update password
            account.Password = _passwordHasher.HashPassword(newPassword);
            account.LastPasswordChange = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("PASSWORD_CHANGE_SUCCESS: Senha alterada para usuário {UserId} ({Email})",
                userId, account.User.Email);

            // Send confirmation email
            try
            {
                await _emailService.SendPasswordChangedEmailAsync(account.User.Email, account.User.Name);
                Log.Information("PASSWORD_CHANGE_CONFIRMATION_SENT: Email de confirmação enviado para {Email}", account.User.Email);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PASSWORD_CHANGE_CONFIRMATION_FAILED: Falha ao enviar email de confirmação para {Email}", account.User.Email);
                // Não falhar a operação por causa do email
            }

            return true;
        }

        public async Task<List<Session>> GetUserSessionsAsync(Guid userId)
        {
            return await _context.Sessions
                .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> RevokeSessionAsync(string sessionToken, Guid userId)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Token == sessionToken && s.UserId == userId);

            if (session == null)
            {
                Log.Warning("SESSION_REVOKE_FAILED: Sessão não encontrada para token {SessionToken} e usuário {UserId}",
                    sessionToken, userId);
                return false;
            }

            session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("SESSION_REVOKED: Sessão {SessionToken} revogada para usuário {UserId}",
                sessionToken, userId);

            return true;
        }

        private string GenerateSecureToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "")
                    .Substring(0, 32);
            }
        }
    }

    public interface IAuthenticationService
    {
        Task<User> ValidateUserByEmailAsync(string email);
        Task<bool> ValidatePasswordAsync(Guid userId, string password);
        Task<string> GenerateAndSendOtpAsync(Guid userId);
        Task<bool> ValidateOtpAsync(Guid userId, string otpCode);
        Task<ClaimsIdentity> CreateIdentityAsync(User user, Guid organizationId);
        void SignIn(HttpContextBase httpContext, ClaimsIdentity identity, Session session);
        void SignOut(HttpContextBase httpContext);
        Task<List<Organization>> GetUserOrganizationsAsync(Guid userId);
        Task<bool> CreateLocalAccountAsync(Guid userId, string password);

        // Password reset methods
        Task<string> GeneratePasswordResetTokenAsync(string email, string baseUrl = null);
        Task<bool> ValidateResetTokenAsync(string token);
        Task<User> GetUserByResetTokenAsync(string token);
        Task<bool> ResetPasswordAsync(string token, string newPassword);

        // Account management methods
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<List<Session>> GetUserSessionsAsync(Guid userId);
        Task<bool> RevokeSessionAsync(string sessionToken, Guid userId);
    }
}