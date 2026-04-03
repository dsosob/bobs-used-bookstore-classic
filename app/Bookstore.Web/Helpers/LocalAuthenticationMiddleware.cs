using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Bookstore.Domain.Customers;

namespace Bookstore.Web.Helpers
{
    public class LocalAuthenticationMiddleware
    {
        private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";

        private readonly RequestDelegate _next;

        public LocalAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICustomerService customerService)
        {
            if (context.Request.Path.Value.StartsWith("/Authentication/Login"))
            {
                CreateClaimsPrincipal(context);

                await SaveCustomerDetailsAsync(context, customerService);

                context.Response.Cookies.Append("LocalAuthentication", "true", new CookieOptions 
                { 
                    Expires = DateTimeOffset.Now.AddDays(1),
                    HttpOnly = true
                });

                context.Response.Redirect("/");
            }
            else if (context.Request.Cookies.ContainsKey("LocalAuthentication"))
            {
                CreateClaimsPrincipal(context);

                await SaveCustomerDetailsAsync(context, customerService);

                await _next(context);
            }
            else
            {
                await _next(context);
            }
        }

        private void CreateClaimsPrincipal(HttpContext context)
        {
            var identity = new ClaimsIdentity("Application");

            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));

            context.User = new ClaimsPrincipal(identity);
        }

        private async Task SaveCustomerDetailsAsync(HttpContext context, ICustomerService customerService)
        {
            var identity = (ClaimsIdentity)context.User.Identity;

            var dto = new CreateOrUpdateCustomerDto(
                identity.FindFirst("nameidentifier").Value,
                identity.Name,
                identity.FindFirst("given_name").Value,
                identity.FindFirst("family_name").Value);

            await customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}
