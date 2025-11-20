using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EntityFrameworkProject.Data;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplicationBasic.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _context;
        private const int SessionExpirationHours = 24;

        public SessionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Session CreateSession(Guid userId, Guid? organizationId, HttpRequestBase request)
        {
            // Limpar sessões expiradas do usuário
            CleanExpiredSessions(userId);

            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActiveOrganizationId = organizationId,
                Token = GenerateSecureToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(SessionExpirationHours),
                IpAddress = GetClientIpAddress(request),
                UserAgent = request.UserAgent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            _context.SaveChanges();

            return session;
        }

        public Session GetSession(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            return _context.Sessions
                .Include(s => s.User)
                    .ThenInclude(u => u.Memberships)
                        .ThenInclude(m => m.Organization)
                .Include(s => s.ActiveOrganization)
                .FirstOrDefault(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
        }

        public Session GetSessionWithDetails(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var session = _context.Sessions
                .Include(s => s.User)
                    .ThenInclude(u => u.Memberships)
                        .ThenInclude(m => m.Organization)
                .Include(s => s.ActiveOrganization)
                .FirstOrDefault(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

            if (session != null)
            {
                // Atualizar última atividade
                session.UpdatedAt = DateTime.UtcNow;

                // Sliding expiration - renovar tempo de expiração
                session.ExpiresAt = DateTime.UtcNow.AddHours(SessionExpirationHours);

                _context.SaveChanges();
            }

            return session;
        }

        public void UpdateSessionOrganization(string token, Guid organizationId)
        {
            var session = GetSession(token);
            if (session != null)
            {
                session.ActiveOrganizationId = organizationId;
                session.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void InvalidateSession(string token)
        {
            var session = _context.Sessions.FirstOrDefault(s => s.Token == token);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                _context.SaveChanges();
            }
        }

        public void InvalidateAllUserSessions(Guid userId)
        {
            var sessions = _context.Sessions.Where(s => s.UserId == userId).ToList();
            _context.Sessions.RemoveRange(sessions);
            _context.SaveChanges();
        }

        private void CleanExpiredSessions(Guid userId)
        {
            var expiredSessions = _context.Sessions
                .Where(s => s.UserId == userId && s.ExpiresAt <= DateTime.UtcNow)
                .ToList();

            if (expiredSessions.Any())
            {
                _context.Sessions.RemoveRange(expiredSessions);
                _context.SaveChanges();
            }
        }

        private string GenerateSecureToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }
        }

        private IPAddress GetClientIpAddress(HttpRequestBase request)
        {
            try
            {
                // Verificar headers de proxy
                string[] headers = { "X-Forwarded-For", "X-Real-IP", "CF-Connecting-IP" };

                foreach (var header in headers)
                {
                    var ipString = request.Headers[header];
                    if (!string.IsNullOrEmpty(ipString))
                    {
                        // X-Forwarded-For pode conter múltiplos IPs
                        var ip = ipString.Split(',').FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out var parsedIp))
                        {
                            return parsedIp;
                        }
                    }
                }

                // Fallback para o IP direto
                return IPAddress.Parse(request.UserHostAddress ?? "127.0.0.1");
            }
            catch
            {
                return IPAddress.Parse("127.0.0.1");
            }
        }
    }

    public interface ISessionService
    {
        Session CreateSession(Guid userId, Guid? organizationId, HttpRequestBase request);
        Session GetSession(string token);
        Session GetSessionWithDetails(string token);
        void UpdateSessionOrganization(string token, Guid organizationId);
        void InvalidateSession(string token);
        void InvalidateAllUserSessions(Guid userId);
    }
}