using System.Text;

using GTEK.FSM.Shared.Contracts.Results;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GTEK.FSM.Backend.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<JwtAuthenticationOptions>(configuration.GetSection(JwtAuthenticationOptions.SectionName));

        var jwtOptions = new JwtAuthenticationOptions();
        configuration.GetSection(JwtAuthenticationOptions.SectionName).Bind(jwtOptions);
        jwtOptions.Validate();

        var hubPath = configuration.GetSection("SignalR").GetValue<string>("HubPath") ?? "/hubs/pipeline";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment() && !environment.IsEnvironment("Local");
                options.SaveToken = false;
                options.IncludeErrorDetails = environment.IsDevelopment() || environment.IsEnvironment("Local");

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrWhiteSpace(context.Token))
                        {
                            return Task.CompletedTask;
                        }

                        var accessToken = context.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrWhiteSpace(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments(hubPath, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        // Prevent default plain-text challenge so clients receive standardized envelope payloads.
                        context.HandleResponse();

                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var response = ApiResponse<object>.Fail(
                            message: "Authentication is required.",
                            errorCode: "AUTH_UNAUTHORIZED",
                            traceId: context.HttpContext.TraceIdentifier);

                        await context.Response.WriteAsJsonAsync(response);
                    },
                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var response = ApiResponse<object>.Fail(
                            message: "You do not have permission to access this resource.",
                            errorCode: "AUTH_FORBIDDEN",
                            traceId: context.HttpContext.TraceIdentifier);

                        await context.Response.WriteAsJsonAsync(response);
                    }
                };

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

        return services;
    }
}
