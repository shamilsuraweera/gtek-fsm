using System.Text;

using GTEK.FSM.Backend.Api.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GTEK.FSM.Backend.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtOptions = new JwtAuthenticationOptions();
        configuration.GetSection(JwtAuthenticationOptions.SectionName).Bind(jwtOptions);
        jwtOptions.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment() && !environment.IsEnvironment("Local");
                options.SaveToken = false;
                options.IncludeErrorDetails = environment.IsDevelopment() || environment.IsEnvironment("Local");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey.Trim())),
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddApiAuthorizationPolicies();

        return services;
    }
}
