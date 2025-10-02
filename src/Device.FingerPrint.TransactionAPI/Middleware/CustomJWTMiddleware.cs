using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Device.FingerPrint.TransactionAPI.Middleware
{
    public class CustomJWTMiddleware
    {
        private readonly RequestDelegate next;
        private readonly string JWTPassword;
        private readonly bool JWTEnable;

        public CustomJWTMiddleware(RequestDelegate next, bool JWTEnable, string JWTPassword)
        {
            this.next = next;
            this.JWTEnable = JWTEnable;
            this.JWTPassword = JWTPassword;
        }

        public async Task Invoke(HttpContext context)
        {
            var authHeaderParts = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ');
            var token = authHeaderParts?[^1];

            if (JWTEnable && (token == null || !ValidateToken(token)))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (!JWTEnable && token != null && !ValidateToken(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next(context);
        }

        private bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var keyJWT = Encoding.ASCII.GetBytes(JWTPassword);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    IssuerSigningKey = new SymmetricSecurityKey(keyJWT),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    RequireExpirationTime = false,
                }, out _);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
