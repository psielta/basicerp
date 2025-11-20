using System;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(WebApplicationBasic.Startup))]

namespace WebApplicationBasic
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            // Configurar autenticação por cookies
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Auth/Login"),
                LogoutPath = new PathString("/Auth/Logout"),
                ExpireTimeSpan = TimeSpan.FromHours(24),
                SlidingExpiration = true,
                CookieName = "BasicERP.Auth",
                CookieSecure = CookieSecureOption.SameAsRequest,
                CookieHttpOnly = true,
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = async context =>
                    {
                        // Aqui você pode adicionar validação adicional da sessão
                        // Por exemplo, verificar se a sessão ainda está válida no banco

                        var sessionToken = context.Identity.FindFirst("SessionToken")?.Value;
                        if (!string.IsNullOrEmpty(sessionToken))
                        {
                            // Você pode injetar o SessionService aqui para validar
                            // Por enquanto, vamos apenas continuar
                        }
                    },
                    OnApplyRedirect = context =>
                    {
                        // Customizar redirecionamento se necessário
                        context.Response.Redirect(context.RedirectUri);
                    }
                }
            });
        }
    }
}