using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using EntityFrameworkProject.Data;
using Microsoft.EntityFrameworkCore;
using WebApplicationBasic.Models.ViewModels;
using WebApplicationBasic.Services;

namespace WebApplicationBasic.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IAuthenticationService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;
        private readonly IImageProcessingService _imageProcessingService;

        public AccountController()
        {
            _authService = DependencyResolver.Current.GetService<IAuthenticationService>();
            _context = DependencyResolver.Current.GetService<ApplicationDbContext>();
            _storageService = DependencyResolver.Current.GetService<IStorageService>();
            _imageProcessingService = DependencyResolver.Current.GetService<IImageProcessingService>();
        }

        // GET: /Account
        public ActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        // GET: /Account/Profile
        public async Task<ActionResult> Profile()
        {
            var user = await _context.Users
                .Include(u => u.Memberships)
                    .ThenInclude(m => m.Organization)
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            if (user == null)
            {
                return HttpNotFound();
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == CurrentUserId && a.ProviderId == "local");

            var model = new AccountProfileViewModel
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                Image = user.Image,
                Role = user.Role,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                HasPassword = account != null,
                LastPasswordChange = account?.LastPasswordChange,
                Organizations = user.Memberships.Select(m => new OrganizationMembershipViewModel
                {
                    OrganizationId = m.OrganizationId,
                    OrganizationName = m.Organization.Name,
                    OrganizationSlug = m.Organization.Slug,
                    Role = m.Role,
                    JoinedAt = m.CreatedAt,
                    IsCurrent = m.OrganizationId == CurrentOrganizationId
                }).ToList()
            };

            return View(model);
        }

        // GET: /Account/Edit
        public async Task<ActionResult> Edit()
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return HttpNotFound();
            }

            var model = new EditProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                ImageUrl = user.Image?.StartsWith("http") == true ? user.Image : null,
                CurrentImage = user.Image
            };

            return View(model);
        }

        // POST: /Account/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Check if email is already taken by another user
            if (model.Email != user.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.Id != CurrentUserId);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Este email já está em uso.");
                    return View(model);
                }

                user.EmailVerified = false; // Reset email verification
            }

            // Process image upload or URL
            string newImageUrl = null;

            // Priority 1: File upload
            if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = System.IO.Path.GetExtension(model.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Apenas arquivos JPG e PNG são permitidos.");
                    return View(model);
                }

                // Validate file size from config
                var maxFileSize = int.Parse(ConfigurationManager.AppSettings["Image:MaxFileSize"] ?? "5242880");
                if (model.ImageFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", "A imagem deve ter no máximo 5MB.");
                    return View(model);
                }

                try
                {
                    // Generate unique filename
                    var fileName = $"profile_{CurrentUserId}_{Guid.NewGuid()}{extension}";

                    // Get image processing configuration
                    var maxWidth = int.Parse(ConfigurationManager.AppSettings["Image:MaxWidth"] ?? "800");
                    var maxHeight = int.Parse(ConfigurationManager.AppSettings["Image:MaxHeight"] ?? "800");
                    var quality = long.Parse(ConfigurationManager.AppSettings["Image:Quality"] ?? "85");

                    // Resize image before upload
                    using (var resizedImageStream = _imageProcessingService.ResizeImage(
                        model.ImageFile.InputStream,
                        maxWidth,
                        maxHeight,
                        quality))
                    {
                        // Upload to MinIO
                        newImageUrl = await _storageService.UploadFileAsync(
                            resizedImageStream,
                            fileName,
                            model.ImageFile.ContentType
                        );
                    }

                    // Delete old image if it exists and is from MinIO
                    if (!string.IsNullOrEmpty(user.Image) && user.Image.Contains("user-profiles"))
                    {
                        await _storageService.DeleteFileAsync(user.Image);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageFile", $"Erro ao fazer upload da imagem: {ex.Message}");
                    return View(model);
                }
            }
            // Priority 2: URL provided
            else if (!string.IsNullOrEmpty(model.ImageUrl))
            {
                newImageUrl = model.ImageUrl;

                // Delete old image if it exists and is from MinIO (user switched to URL)
                if (!string.IsNullOrEmpty(user.Image) && user.Image.Contains("user-profiles"))
                {
                    await _storageService.DeleteFileAsync(user.Image);
                }
            }
            // Priority 3: Keep current image
            else
            {
                newImageUrl = model.CurrentImage;
            }

            user.Name = model.Name;
            user.Email = model.Email;
            user.Image = newImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Perfil atualizado com sucesso!";
            return RedirectToAction("Profile");
        }

        // GET: /Account/ChangePassword
        public async Task<ActionResult> ChangePassword()
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == CurrentUserId && a.ProviderId == "local");

            if (account == null)
            {
                TempData["Warning"] = "Você não possui uma senha configurada. Use login via email.";
                return RedirectToAction("Profile");
            }

            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var success = await _authService.ChangePasswordAsync(CurrentUserId, model.CurrentPassword, model.NewPassword);

            if (success)
            {
                TempData["Success"] = "Senha alterada com sucesso!";
                return RedirectToAction("Profile");
            }
            else
            {
                ModelState.AddModelError("CurrentPassword", "Senha atual incorreta.");
                return View(model);
            }
        }

        // GET: /Account/Security
        public async Task<ActionResult> Security()
        {
            var sessions = await _authService.GetUserSessionsAsync(CurrentUserId);
            var currentSessionToken = (HttpContext.User as System.Security.Claims.ClaimsPrincipal)?.Claims
                .FirstOrDefault(c => c.Type == "SessionToken")?.Value;

            var model = new AccountSecurityViewModel
            {
                Sessions = sessions.Select(s => new SessionViewModel
                {
                    SessionId = s.Id,
                    Token = s.Token,
                    IpAddress = s.IpAddress?.ToString() ?? "",
                    UserAgent = s.UserAgent,
                    CreatedAt = s.CreatedAt,
                    ExpiresAt = s.ExpiresAt,
                    IsCurrent = s.Token == currentSessionToken,
                    OrganizationName = GetOrganizationName(s.ActiveOrganizationId ?? Guid.Empty)
                }).ToList(),
                TwoFactorEnabled = CurrentUser.TwoFactorEnabled
            };

            return View(model);
        }

        // POST: /Account/RevokeSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RevokeSession(string sessionToken)
        {
            var currentSessionToken = (HttpContext.User as System.Security.Claims.ClaimsPrincipal)?.Claims
                .FirstOrDefault(c => c.Type == "SessionToken")?.Value;

            if (sessionToken == currentSessionToken)
            {
                TempData["Error"] = "Você não pode revogar a sessão atual.";
                return RedirectToAction("Security");
            }

            var success = await _authService.RevokeSessionAsync(sessionToken, CurrentUserId);

            if (success)
            {
                TempData["Success"] = "Sessão revogada com sucesso.";
            }
            else
            {
                TempData["Error"] = "Erro ao revogar sessão.";
            }

            return RedirectToAction("Security");
        }

        // POST: /Account/RevokeAllSessions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RevokeAllSessions()
        {
            var currentSessionToken = (HttpContext.User as System.Security.Claims.ClaimsPrincipal)?.Claims
                .FirstOrDefault(c => c.Type == "SessionToken")?.Value;

            var sessions = await _authService.GetUserSessionsAsync(CurrentUserId);

            foreach (var session in sessions.Where(s => s.Token != currentSessionToken))
            {
                await _authService.RevokeSessionAsync(session.Token, CurrentUserId);
            }

            TempData["Success"] = "Todas as outras sessões foram revogadas.";
            return RedirectToAction("Security");
        }

        // GET: /Account/Organizations
        public async Task<ActionResult> Organizations()
        {
            var user = await _context.Users
                .Include(u => u.Memberships)
                    .ThenInclude(m => m.Organization)
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            if (user == null)
            {
                return HttpNotFound();
            }

            var model = new AccountOrganizationsViewModel
            {
                CurrentOrganizationId = CurrentOrganizationId,
                Organizations = user.Memberships.Select(m => new OrganizationDetailViewModel
                {
                    OrganizationId = m.OrganizationId,
                    Name = m.Organization.Name,
                    Slug = m.Organization.Slug,
                    Description = "", // Organization não tem campo Description
                    Logo = m.Organization.Logo,
                    Role = m.Role,
                    JoinedAt = m.CreatedAt,
                    IsCurrent = m.OrganizationId == CurrentOrganizationId,
                    MemberCount = _context.Memberships.Count(mem => mem.OrganizationId == m.OrganizationId)
                }).ToList()
            };

            return View(model);
        }

        private string GetOrganizationName(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                return "N/A";

            return _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Name)
                .FirstOrDefault() ?? "N/A";
        }
    }
}