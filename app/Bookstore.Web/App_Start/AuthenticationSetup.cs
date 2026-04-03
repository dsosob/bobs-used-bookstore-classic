using Microsoft.AspNetCore.Builder;
using System.Security.Claims;
using System.Threading.Tasks;
using BobsBookstoreClassic.Data;
using Bookstore.Domain.Customers;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Web
{
    public static class AuthenticationSetup
    {
        public static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            if (BookstoreConfiguration.GetSetting("Services:Authentication") == "aws")
            {
                ConfigureCognitoAuthentication(services);
            }
            else
            {
                ConfigureLocalAuthentication(services);
            }
        }

        private static void ConfigureLocalAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Authentication/Login";
                    options.LogoutPath = "/Authentication/Logout";
                });
        }

        private static void ConfigureCognitoAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.ClientId = BookstoreConfiguration.GetSetting("Authentication:Cognito:LocalClientId");
                options.MetadataAddress = BookstoreConfiguration.GetSetting("Authentication:Cognito:MetadataAddress");
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "cognito:username",
                    RoleClaimType = "cognito:groups"
                };
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var customerService = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();
                        var identity = (ClaimsIdentity)context.Principal.Identity;

                        var dto = new CreateOrUpdateCustomerDto(
                            identity.GetSub(),
                            identity.Name,
                            identity.FindFirst(y => y.Type.Contains("givenname"))?.Value,
                            identity.FindFirst(y => y.Type.Contains("surname"))?.Value);

                        await customerService.CreateOrUpdateCustomerAsync(dto);
                    }
                };
            });
        }

        public static void UseLocalAuthenticationMiddleware(this Microsoft.AspNetCore.Builder.IApplicationBuilder app)
        {
            if (BookstoreConfiguration.GetSetting("Services:Authentication") != "aws")
            {
                app.UseMiddleware<LocalAuthenticationMiddleware>();
            }
        }
    }
}
