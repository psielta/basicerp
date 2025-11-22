using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using EntityFrameworkProject.Data;
using Microsoft.EntityFrameworkCore;
using WebApplicationBasic.Models.ViewModels;
using WebApplicationBasic.Services;
using Serilog;

namespace WebApplicationBasic.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly ISessionService _sessionService;
        private readonly ApplicationDbContext _context;

        public AuthController()
        {
            // Obter serviços do container DI
            _authService = DependencyResolver.Current.GetService<IAuthenticationService>();
            _sessionService = DependencyResolver.Current.GetService<ISessionService>();
            _context = DependencyResolver.Current.GetService<ApplicationDbContext>();
        }

        // GET: /Auth/Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            var ipAddress = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                Log.Warning("LOGIN_ATTEMPT_FAILED: ModelState inválido para email {Email} de IP {IpAddress}",
                    model.Email, ipAddress);
                return View(model);
            }

            Log.Information("LOGIN_ATTEMPT: Tentativa de login para email {Email} de IP {IpAddress}",
                model.Email, ipAddress);

            // Validar usuário pelo email
            var user = await _authService.ValidateUserByEmailAsync(model.Email);

            if (user == null)
            {
                Log.Warning("LOGIN_FAILED: Email {Email} não encontrado - IP {IpAddress}",
                    model.Email, ipAddress);
                ModelState.AddModelError("", "Email não encontrado.");
                return View(model);
            }

            // Verificar se o usuário tem organizações
            var organizations = await _authService.GetUserOrganizationsAsync(user.Id);

            if (!organizations.Any())
            {
                Log.Warning("LOGIN_FAILED: Usuário {UserId} ({Email}) sem organizações - IP {IpAddress}",
                    user.Id, model.Email, ipAddress);
                ModelState.AddModelError("", "Usuário não está vinculado a nenhuma organização.");
                return View(model);
            }

            Log.Information("LOGIN_EMAIL_VALIDATED: Usuário {UserId} ({Email}) validado, {OrgCount} organização(ões) encontrada(s)",
                user.Id, model.Email, organizations.Count);

            // Se tem apenas uma organização, pular seleção
            if (organizations.Count == 1)
            {
                var org = organizations.First();
                return RedirectToAction("SelectMethod", new
                {
                    userId = user.Id,
                    organizationId = org.Id,
                    returnUrl = model.ReturnUrl
                });
            }

            // Múltiplas organizações - redirecionar para seleção
            return RedirectToAction("SelectOrganization", new { userId = user.Id, returnUrl = model.ReturnUrl });
        }

        // GET: /Auth/SelectOrganization
        [HttpGet]
        public async Task<ActionResult> SelectOrganization(Guid userId, string returnUrl)
        {
            var user = await _context.Users
                .Include(u => u.Memberships)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var organizations = await _authService.GetUserOrganizationsAsync(userId);

            var model = new SelectOrganizationViewModel
            {
                UserId = userId,
                UserName = user.Name,
                UserEmail = user.Email,
                UserImage = user.Image,
                Organizations = organizations.Select(o =>
                {
                    var membership = user.Memberships?.FirstOrDefault(m => m.OrganizationId == o.Id);
                    return new SelectOrganizationViewModel.OrganizationInfo
                    {
                        Id = o.Id,
                        Name = o.Name,
                        Role = membership?.Role ?? "member",
                        Logo = o.Logo
                    };
                }).ToList(),
                ReturnUrl = returnUrl,
                HasPassword = _context.Accounts.Any(a => a.UserId == userId && a.ProviderId == "local" && !string.IsNullOrEmpty(a.Password))
            };

            return View(model);
        }

        // POST: /Auth/SelectOrganization
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectOrganization(SelectOrganizationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return RedirectToAction("SelectMethod", new
            {
                userId = model.UserId,
                organizationId = model.OrganizationId,
                returnUrl = model.ReturnUrl
            });
        }

        // GET: /Auth/SelectMethod
        [HttpGet]
        public async Task<ActionResult> SelectMethod(Guid userId, Guid organizationId, string returnUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            var organization = await _context.Organizations.FindAsync(organizationId);

            if (user == null || organization == null)
            {
                return RedirectToAction("Login");
            }

            // Verificar se tem senha
            var hasPassword = _context.Accounts.Any(a => a.UserId == userId && a.ProviderId == "local" && !string.IsNullOrEmpty(a.Password));

            ViewBag.UserId = userId;
            ViewBag.OrganizationId = organizationId;
            ViewBag.UserEmail = user.Email;
            ViewBag.OrganizationName = organization.Name;
            ViewBag.HasPassword = hasPassword;
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        // POST: /Auth/LoginWithPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LoginWithPassword(PasswordLoginViewModel model)
        {
            var ipAddress = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                Log.Warning("PASSWORD_LOGIN_FAILED: ModelState inválido para usuário {UserId} de IP {IpAddress}",
                    model.UserId, ipAddress);
                return View(model);
            }

            Log.Information("PASSWORD_LOGIN_ATTEMPT: Tentativa de login com senha para usuário {UserId} de IP {IpAddress}",
                model.UserId, ipAddress);

            // Validar senha
            var isValid = await _authService.ValidatePasswordAsync(model.UserId, model.Password);

            if (!isValid)
            {
                Log.Warning("PASSWORD_LOGIN_FAILED: Senha incorreta para usuário {UserId} de IP {IpAddress}",
                    model.UserId, ipAddress);
                ModelState.AddModelError("", "Senha incorreta.");
                return View(model);
            }

            // Login bem-sucedido
            await CompleteLogin(model.UserId, model.OrganizationId, model.ReturnUrl);

            return RedirectToLocal(model.ReturnUrl);
        }

        // GET: /Auth/LoginWithPassword
        [HttpGet]
        public async Task<ActionResult> LoginWithPassword(Guid userId, Guid organizationId, string returnUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            var organization = await _context.Organizations.FindAsync(organizationId);

            if (user == null || organization == null)
            {
                return RedirectToAction("Login");
            }

            var model = new PasswordLoginViewModel
            {
                UserId = userId,
                OrganizationId = organizationId,
                UserEmail = user.Email,
                UserName = user.Name,
                UserImage = user.Image,
                OrganizationName = organization.Name,
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        // POST: /Auth/SendOTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendOTP(Guid userId, Guid organizationId, string returnUrl)
        {
            var ipAddress = Request.UserHostAddress;

            try
            {
                Log.Information("OTP_SEND_REQUEST: Solicitação de OTP para usuário {UserId} de IP {IpAddress}",
                    userId, ipAddress);

                await _authService.GenerateAndSendOtpAsync(userId);
                TempData["Success"] = "Código enviado para seu email!";

                return RedirectToAction("VerifyOTP", new { userId, organizationId, returnUrl });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OTP_SEND_ERROR: Erro ao enviar OTP para usuário {UserId} de IP {IpAddress}",
                    userId, ipAddress);
                TempData["Error"] = "Erro ao enviar código. Tente novamente.";
                return RedirectToAction("SelectMethod", new { userId, organizationId, returnUrl });
            }
        }

        // GET: /Auth/VerifyOTP
        [HttpGet]
        public async Task<ActionResult> VerifyOTP(Guid userId, Guid organizationId, string returnUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            var organization = await _context.Organizations.FindAsync(organizationId);

            if (user == null || organization == null)
            {
                return RedirectToAction("Login");
            }

            var model = new VerifyOtpViewModel
            {
                UserId = userId,
                OrganizationId = organizationId,
                UserEmail = user.Email,
                UserName = user.Name,
                UserImage = user.Image,
                OrganizationName = organization.Name,
                ReturnUrl = returnUrl,
                CanResend = true,
                ResendInSeconds = 60
            };

            return View(model);
        }

        // POST: /Auth/VerifyOTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyOTP(VerifyOtpViewModel model)
        {
            var ipAddress = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                Log.Warning("OTP_VERIFY_FAILED: ModelState inválido para usuário {UserId} de IP {IpAddress}",
                    model.UserId, ipAddress);
                return View(model);
            }

            Log.Information("OTP_VERIFY_ATTEMPT: Tentativa de verificação OTP para usuário {UserId} de IP {IpAddress}",
                model.UserId, ipAddress);

            // Validar OTP
            var isValid = await _authService.ValidateOtpAsync(model.UserId, model.OtpCode);

            if (!isValid)
            {
                Log.Warning("OTP_VERIFY_FAILED: Código OTP inválido ou expirado para usuário {UserId} de IP {IpAddress}",
                    model.UserId, ipAddress);
                ModelState.AddModelError("", "Código inválido ou expirado.");

                var user = await _context.Users.FindAsync(model.UserId);
                var organization = await _context.Organizations.FindAsync(model.OrganizationId);

                model.UserEmail = user?.Email;
                model.OrganizationName = organization?.Name;

                return View(model);
            }

            // Login bem-sucedido
            await CompleteLogin(model.UserId, model.OrganizationId, model.ReturnUrl);

            return RedirectToLocal(model.ReturnUrl);
        }

        // GET: /Auth/Logout
        public ActionResult Logout()
        {
            var ipAddress = Request.UserHostAddress;
            var claimsPrincipal = User as ClaimsPrincipal;
            var userId = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            Log.Information("LOGOUT_REQUEST: Solicitação de logout para usuário {UserId} de IP {IpAddress}",
                userId, ipAddress);

            _authService.SignOut(HttpContext);
            return RedirectToAction("Login");
        }

        // GET: /Auth/SwitchOrganization
        public ActionResult SwitchOrganization()
        {
            var ipAddress = Request.UserHostAddress;

            // Obter userId do usuário autenticado
            var claimsPrincipal = User as ClaimsPrincipal;
            var userIdClaim = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                Log.Warning("SWITCH_ORG_FAILED: Usuário não autenticado tentando trocar organização de IP {IpAddress}",
                    ipAddress);
                return RedirectToAction("Login");
            }

            Log.Information("SWITCH_ORG_REQUEST: Usuário {UserId} solicitando troca de organização de IP {IpAddress}",
                userId, ipAddress);

            // Fazer logout mantendo o userId
            _authService.SignOut(HttpContext);

            // Redirecionar para seleção de organização
            return RedirectToAction("SelectOrganization", new { userId = userId });
        }

        private async Task CompleteLogin(Guid userId, Guid organizationId, string returnUrl)
        {
            var user = await _context.Users
                .Include(u => u.Memberships)
                    .ThenInclude(m => m.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new InvalidOperationException("Usuário não encontrado");
            }

            // Criar identidade
            var identity = await _authService.CreateIdentityAsync(user, organizationId);

            // Criar sessão no banco
            var session = _sessionService.CreateSession(userId, organizationId, Request);

            // Fazer sign in
            _authService.SignIn(HttpContext, identity, session);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ForgotPasswordViewModel());
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            var ipAddress = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                Log.Warning("FORGOT_PASSWORD_FAILED: ModelState inválido para email {Email} de IP {IpAddress}",
                    model.Email, ipAddress);
                return View(model);
            }

            Log.Information("FORGOT_PASSWORD_REQUEST: Solicitação de reset de senha para email {Email} de IP {IpAddress}",
                model.Email, ipAddress);

            // Always show success message to not reveal if email exists
            var baseUrl = System.Configuration.ConfigurationManager.AppSettings["BaseUrl"];
            var result = await _authService.GeneratePasswordResetTokenAsync(model.Email, baseUrl);

            TempData["Success"] = "Se o email estiver cadastrado, você receberá instruções para redefinir sua senha.";
            return View(new ForgotPasswordViewModel());
        }

        // GET: /Auth/ResetPassword
        [HttpGet]
        public async Task<ActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var isValid = await _authService.ValidateResetTokenAsync(token);
            if (!isValid)
            {
                TempData["Error"] = "Link de redefinição inválido ou expirado.";
                return RedirectToAction("ForgotPassword");
            }

            var user = await _authService.GetUserByResetTokenAsync(token);
            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = user?.Email
            };

            return View(model);
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var ipAddress = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                Log.Warning("RESET_PASSWORD_FAILED: ModelState inválido para token de IP {IpAddress}", ipAddress);
                return View(model);
            }

            Log.Information("RESET_PASSWORD_ATTEMPT: Tentativa de reset de senha de IP {IpAddress}", ipAddress);

            var success = await _authService.ResetPasswordAsync(model.Token, model.Password);

            if (success)
            {
                Log.Information("RESET_PASSWORD_SUCCESS: Senha redefinida com sucesso de IP {IpAddress}", ipAddress);
                TempData["Success"] = "Sua senha foi redefinida com sucesso! Você já pode fazer login.";
                return RedirectToAction("Login");
            }
            else
            {
                Log.Warning("RESET_PASSWORD_FAILED: Token inválido ou expirado de IP {IpAddress}", ipAddress);
                ModelState.AddModelError("", "Link de redefinição inválido ou expirado.");
                return View(model);
            }
        }
    }
}