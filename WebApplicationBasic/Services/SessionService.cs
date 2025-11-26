using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EntityFrameworkProject.Data;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

            var ipAddress = GetClientIpAddress(request);
            var userAgent = request.UserAgent;

            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActiveOrganizationId = organizationId,
                Token = GenerateSecureToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(SessionExpirationHours),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            _context.SaveChanges();

            Log.Information("SESSION_CREATED: Sessão {SessionToken} criada para usuário {UserId} na organização {OrganizationId} - IP: {IpAddress}, UserAgent: {UserAgent}, Expira em: {ExpiresAt}",
                session.Token, userId, organizationId, ipAddress, userAgent, session.ExpiresAt);

            return session;
        }

        public Session GetSession(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Log.Debug("SESSION_GET_FAILED: Token vazio fornecido");
                return null;
            }

            var session = _context.Sessions
                .Include(s => s.User)
                    .ThenInclude(u => u.Memberships)
                        .ThenInclude(m => m.Organization)
                .Include(s => s.ActiveOrganization)
                .FirstOrDefault(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

            if (session == null)
            {
                Log.Warning("SESSION_GET_FAILED: Sessão não encontrada ou expirada para token {SessionToken}", token);
            }
            else
            {
                Log.Debug("SESSION_GET_SUCCESS: Sessão {SessionToken} recuperada para usuário {UserId}", token, session.UserId);
            }

            return session;
        }

        public Session GetSessionWithDetails(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Log.Debug("SESSION_GET_DETAILS_FAILED: Token vazio fornecido");
                return null;
            }

            var session = _context.Sessions
                .Include(s => s.User)
                    .ThenInclude(u => u.Memberships)
                        .ThenInclude(m => m.Organization)
                .Include(s => s.ActiveOrganization)
                .FirstOrDefault(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

            if (session != null)
            {
                // Sliding expiration: renovar apenas se faltar menos de 50% do tempo de sessão
                // Isso evita escritas no banco a cada request
                var renewalThreshold = TimeSpan.FromHours(SessionExpirationHours / 2.0);
                var timeUntilExpiry = session.ExpiresAt - DateTime.UtcNow;

                if (timeUntilExpiry < renewalThreshold)
                {
                    var oldExpiry = session.ExpiresAt;

                    session.UpdatedAt = DateTime.UtcNow;
                    session.ExpiresAt = DateTime.UtcNow.AddHours(SessionExpirationHours);

                    _context.SaveChanges();

                    Log.Debug("SESSION_RENEWED: Sessão {SessionToken} renovada para usuário {UserId} - Expirava em: {OldExpiry}, Nova expiração: {NewExpiry}",
                        token, session.UserId, oldExpiry, session.ExpiresAt);
                }
            }
            else
            {
                Log.Warning("SESSION_GET_DETAILS_FAILED: Sessão não encontrada ou expirada para token {SessionToken}", token);
            }

            return session;
        }

        public void UpdateSessionOrganization(string token, Guid organizationId)
        {
            var session = GetSession(token);
            if (session != null)
            {
                var oldOrgId = session.ActiveOrganizationId;
                session.ActiveOrganizationId = organizationId;
                session.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();

                Log.Information("SESSION_ORGANIZATION_CHANGED: Sessão {SessionToken} do usuário {UserId} mudou de organização {OldOrganizationId} para {NewOrganizationId}",
                    token, session.UserId, oldOrgId, organizationId);
            }
            else
            {
                Log.Warning("SESSION_ORGANIZATION_CHANGE_FAILED: Sessão não encontrada para token {SessionToken}", token);
            }
        }

        public void InvalidateSession(string token)
        {
            var session = _context.Sessions.FirstOrDefault(s => s.Token == token);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                _context.SaveChanges();

                Log.Information("SESSION_INVALIDATED: Sessão {SessionToken} invalidada para usuário {UserId}",
                    token, session.UserId);
            }
            else
            {
                Log.Debug("SESSION_INVALIDATE_FAILED: Sessão não encontrada para token {SessionToken}", token);
            }
        }

        public void InvalidateAllUserSessions(Guid userId)
        {
            var sessions = _context.Sessions.Where(s => s.UserId == userId).ToList();
            var count = sessions.Count;

            _context.Sessions.RemoveRange(sessions);
            _context.SaveChanges();

            Log.Information("ALL_SESSIONS_INVALIDATED: {Count} sessões invalidadas para usuário {UserId}",
                count, userId);
        }

        private void CleanExpiredSessions(Guid userId)
        {
            var expiredSessions = _context.Sessions
                .Where(s => s.UserId == userId && s.ExpiresAt <= DateTime.UtcNow)
                .ToList();

            if (expiredSessions.Any())
            {
                var count = expiredSessions.Count;
                _context.Sessions.RemoveRange(expiredSessions);
                _context.SaveChanges();

                Log.Debug("EXPIRED_SESSIONS_CLEANED: {Count} sessões expiradas removidas para usuário {UserId}",
                    count, userId);
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