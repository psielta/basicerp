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
                return null;

            return await _context.Users
                .Include(u => u.Memberships)
                    .ThenInclude(m => m.Organization)
                .Include(u => u.Accounts)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> ValidatePasswordAsync(Guid userId, string password)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ProviderId == "local");

            if (account == null || string.IsNullOrEmpty(account.Password))
                return false;

            return _passwordHasher.VerifyPassword(password, account.Password);
        }

        public async Task<string> GenerateAndSendOtpAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Usuário não encontrado");

            // Gerar código OTP de 6 dígitos
            var otpCode = GenerateOtpCode();

            // Debug
            System.Diagnostics.Debug.WriteLine($"OTP gerado: '{otpCode}' para usuário {user.Email}");

            // Armazenar OTP no metadata do usuário com expiração
            var metadata = string.IsNullOrEmpty(user.Metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(user.Metadata);

            metadata["otp"] = otpCode;
            metadata["otp_expires"] = DateTime.UtcNow.AddMinutes(5).ToString("O");

            user.Metadata = JsonSerializer.Serialize(metadata);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Debug: verificar o que foi salvo
            System.Diagnostics.Debug.WriteLine($"Metadata salvo: {user.Metadata}");

            // Enviar email
            await _emailService.SendOtpEmailAsync(user.Email, user.Name, otpCode);

            return otpCode;
        }

        public async Task<bool> ValidateOtpAsync(Guid userId, string otpCode)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("ValidateOtpAsync: Usuário não encontrado");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"ValidateOtpAsync: Metadata do usuário: {user.Metadata}");
                System.Diagnostics.Debug.WriteLine($"ValidateOtpAsync: Código recebido: '{otpCode}'");

                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(user.Metadata);

                if (!metadata.ContainsKey("otp") || !metadata.ContainsKey("otp_expires"))
                {
                    System.Diagnostics.Debug.WriteLine("ValidateOtpAsync: Metadata não contém otp ou otp_expires");
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

                System.Diagnostics.Debug.WriteLine($"ValidateOtpAsync: OTP armazenado: '{storedOtp}'");
                System.Diagnostics.Debug.WriteLine($"ValidateOtpAsync: Expira em: {expiresStr}");

                if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(expiresStr))
                {
                    System.Diagnostics.Debug.WriteLine("ValidateOtpAsync: OTP ou data de expiração está vazio");
                    return false;
                }

                // Usar DateTime.Parse com DateTimeStyles.RoundtripKind para preservar UTC
                var expires = DateTime.Parse(expiresStr, null, System.Globalization.DateTimeStyles.RoundtripKind);

                if (DateTime.UtcNow > expires)
                {
                    System.Diagnostics.Debug.WriteLine($"ValidateOtpAsync: OTP expirado. Agora: {DateTime.UtcNow}, Expira: {expires}");
                    // OTP expirado - limpar
                    ClearOtp(user);
                    return false;
                }

                // Comparar códigos (trim para remover espaços)
                var match = storedOtp.Trim() == otpCode.Trim();
                System.Diagnostics.Debug.WriteLine($"ValidateOtpAsync: Comparação '{storedOtp.Trim()}' == '{otpCode.Trim()}': {match}");

                if (match)
                {
                    // OTP válido - limpar após uso
                    ClearOtp(user);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Log do erro para debug
                System.Diagnostics.Debug.WriteLine($"Erro ao validar OTP: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<ClaimsIdentity> CreateIdentityAsync(User user, Guid organizationId)
        {
            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == organizationId);
            if (membership == null)
                throw new InvalidOperationException("Usuário não tem acesso a esta organização");

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
        }

        public void SignOut(HttpContextBase httpContext)
        {
            var authManager = httpContext.GetOwinContext().Authentication;

            // Invalidar sessão no banco
            var claimsPrincipal = httpContext.User as ClaimsPrincipal;
            var sessionToken = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "SessionToken")?.Value;
            if (!string.IsNullOrEmpty(sessionToken))
            {
                _sessionService.InvalidateSession(sessionToken);
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
            var user = await ValidateUserByEmailAsync(email);
            if (user == null)
                return null; // Don't reveal if user exists

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.ProviderId == "local");

            if (account == null)
                return null; // User doesn't have a password account

            // Generate secure reset token
            var token = GenerateSecureToken();
            account.ResetToken = token;
            account.ResetTokenExpires = DateTime.UtcNow.AddHours(24);
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send reset email
            if (string.IsNullOrEmpty(baseUrl))
            {
                // Fallback para configuração no Web.config
                baseUrl = System.Configuration.ConfigurationManager.AppSettings["BaseUrl"] ?? "http://localhost:44318";
            }
            var resetUrl = $"{baseUrl}/Auth/ResetPassword?token={token}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetUrl);

            return token;
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ResetToken == token);

            if (account == null)
                return false;

            if (account.ResetTokenExpires == null || account.ResetTokenExpires < DateTime.UtcNow)
                return false;

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
                return false;

            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ResetToken == token);

            if (account == null || account.ResetTokenExpires == null || account.ResetTokenExpires < DateTime.UtcNow)
                return false;

            // Update password
            account.Password = _passwordHasher.HashPassword(newPassword);
            account.ResetToken = null;
            account.ResetTokenExpires = null;
            account.LastPasswordChange = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send confirmation email
            await _emailService.SendPasswordChangedEmailAsync(account.User.Email, account.User.Name);

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.ProviderId == "local");

            if (account == null)
                return false;

            // Validate current password
            if (!_passwordHasher.VerifyPassword(currentPassword, account.Password))
                return false;

            // Update password
            account.Password = _passwordHasher.HashPassword(newPassword);
            account.LastPasswordChange = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send confirmation email
            await _emailService.SendPasswordChangedEmailAsync(account.User.Email, account.User.Name);

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
                return false;

            session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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