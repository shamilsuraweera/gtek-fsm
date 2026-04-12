using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Infrastructure.Persistence;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GTEK.FSM.Backend.Api.Authentication;

public interface ILocalAuthService
{
    Task<LocalAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<LocalAuthResult> RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default);
}

public interface ILocalAuthBootstrapService
{
    Task EnsureBootstrappedAsync(CancellationToken cancellationToken = default);
}

public sealed class LocalAuthService : ILocalAuthService
{
    private readonly GtekFsmDbContext dbContext;
    private readonly ILocalPasswordHasher passwordHasher;
    private readonly IJwtTokenIssuer tokenIssuer;

    public LocalAuthService(
        GtekFsmDbContext dbContext,
        ILocalPasswordHasher passwordHasher,
        IJwtTokenIssuer tokenIssuer)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.tokenIssuer = tokenIssuer;
    }

    public async Task<LocalAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var credential = await this.dbContext.LocalCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (credential is null || !this.passwordHasher.VerifyPassword(credential.PasswordHash, request.Password ?? string.Empty))
        {
            return LocalAuthResult.Fail(StatusCodes.Status401Unauthorized, "AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
        }

        var user = await this.dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == credential.UserId && x.TenantId == credential.TenantId, cancellationToken);

        if (user is null)
        {
            return LocalAuthResult.Fail(StatusCodes.Status401Unauthorized, "AUTH_IDENTITY_NOT_FOUND", "The local identity is no longer available.");
        }

        var tenant = await this.dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == credential.TenantId, cancellationToken);

        if (tenant is null)
        {
            return LocalAuthResult.Fail(StatusCodes.Status401Unauthorized, "AUTH_TENANT_NOT_FOUND", "The local tenant is no longer available.");
        }

        return LocalAuthResult.Success(this.tokenIssuer.Issue(user, tenant, normalizedEmail, credential.Role));
    }

    public async Task<LocalAuthResult> RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailTaken = await this.dbContext.LocalCredentials
            .AsNoTracking()
            .AnyAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
        {
            return LocalAuthResult.Fail(StatusCodes.Status409Conflict, "AUTH_EMAIL_ALREADY_EXISTS", "That email address is already registered.");
        }

        var tenantCode = string.IsNullOrWhiteSpace(request.TenantCode)
            ? LocalAuthBootstrapConstants.BaselineTenantCode
            : request.TenantCode.Trim();

        var tenant = await this.dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Code == tenantCode, cancellationToken);

        if (tenant is null)
        {
            return LocalAuthResult.Fail(StatusCodes.Status400BadRequest, "AUTH_TENANT_NOT_FOUND", "The requested tenant code is not available.");
        }

        var user = new User(
            Guid.NewGuid(),
            tenant.Id,
            $"local:{normalizedEmail}",
            request.DisplayName ?? string.Empty);

        var credential = new LocalCredential(
            user.Id,
            tenant.Id,
            normalizedEmail,
            this.passwordHasher.HashPassword(request.Password ?? string.Empty),
            LocalAuthBootstrapConstants.CustomerRole);

        await this.dbContext.Users.AddAsync(user, cancellationToken);
        await this.dbContext.LocalCredentials.AddAsync(credential, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        return LocalAuthResult.Success(this.tokenIssuer.Issue(user, tenant, normalizedEmail, credential.Role));
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}

public sealed class LocalAuthBootstrapService : ILocalAuthBootstrapService
{
    private readonly GtekFsmDbContext dbContext;
    private readonly ILocalPasswordHasher passwordHasher;

    public LocalAuthBootstrapService(GtekFsmDbContext dbContext, ILocalPasswordHasher passwordHasher)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
    }

    public async Task EnsureBootstrappedAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await this.dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == LocalAuthBootstrapConstants.BaselineTenantId, cancellationToken);

        if (tenant is null)
        {
            tenant = new Tenant(
                LocalAuthBootstrapConstants.BaselineTenantId,
                LocalAuthBootstrapConstants.BaselineTenantCode,
                LocalAuthBootstrapConstants.BaselineTenantName);

            await this.dbContext.Tenants.AddAsync(tenant, cancellationToken);
        }

        foreach (var account in LocalAuthBootstrapConstants.Accounts)
        {
            var user = await this.dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == account.UserId, cancellationToken);

            if (user is null)
            {
                user = new User(account.UserId, tenant.Id, account.ExternalIdentity, account.DisplayName);
                await this.dbContext.Users.AddAsync(user, cancellationToken);
            }
            else
            {
                user.Rename(account.DisplayName);
            }

            var credential = await this.dbContext.LocalCredentials
                .FirstOrDefaultAsync(x => x.UserId == account.UserId, cancellationToken);

            if (credential is null)
            {
                credential = new LocalCredential(
                    account.UserId,
                    tenant.Id,
                    account.Email,
                    this.passwordHasher.HashPassword(account.Password),
                    account.Role);

                await this.dbContext.LocalCredentials.AddAsync(credential, cancellationToken);
                continue;
            }

            credential.UpdateEmail(account.Email);
            credential.UpdateRole(account.Role);
            credential.UpdatePasswordHash(this.passwordHasher.HashPassword(account.Password));
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}

public interface IJwtTokenIssuer
{
    AuthSessionResponse Issue(User user, Tenant tenant, string email, string role);
}

public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly JwtAuthenticationOptions options;

    public JwtTokenIssuer(IOptions<JwtAuthenticationOptions> options)
    {
        this.options = options.Value;
    }

    public AuthSessionResponse Issue(User user, Tenant tenant, string email, string role)
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(8);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.options.SigningKey.Trim()));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(TokenClaimNames.Subject, user.Id.ToString()),
            new(TokenClaimNames.TenantId, tenant.Id.ToString()),
            new(TokenClaimNames.Role, role),
            new(ClaimTypes.Role, role),
            new(TokenClaimNames.TokenVersion, "1"),
            new(JwtRegisteredClaimNames.Jti, $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{user.Id:N}"),
        };

        var token = new JwtSecurityToken(
            issuer: this.options.Issuer,
            audience: this.options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthSessionResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id.ToString(),
            TenantId = tenant.Id.ToString(),
            TenantCode = tenant.Code,
            DisplayName = user.DisplayName,
            Email = email,
            Role = role,
        };
    }
}

public interface ILocalPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string providedPassword);
}

public sealed class Pbkdf2LocalPasswordHasher : ILocalPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int IterationCount = 100000;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, IterationCount, HashAlgorithmName.SHA256, HashSize);
        return $"pbkdf2-sha256${IterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        var segments = passwordHash.Split('$');
        if (segments.Length != 4 || !string.Equals(segments[0], "pbkdf2-sha256", StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(segments[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(segments[2]);
            var expected = Convert.FromBase64String(segments[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            return false;
        }
    }
}

public static class LocalAuthBootstrapConstants
{
    public static readonly Guid BaselineTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public const string BaselineTenantCode = "REF-BASELINE";
    public const string BaselineTenantName = "Reference Baseline Tenant";
    public const string CustomerRole = "Customer";

    public static readonly IReadOnlyList<BootstrappedLocalAccount> Accounts =
    [
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            "role:Customer",
            "Reference Role - Customer",
            "customer@gtek.local",
            "Customer@123",
            "Customer"),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000003"),
            "role:Worker",
            "Reference Role - Worker",
            "worker@gtek.local",
            "Worker@123",
            "Worker"),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000004"),
            "role:Support",
            "Reference Role - Support",
            "support@gtek.local",
            "Support@123",
            "Support"),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000005"),
            "role:Manager",
            "Reference Role - Manager",
            "manager@gtek.local",
            "Manager@123",
            "Manager"),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000006"),
            "role:Admin",
            "Shamil Suraweera",
            "shamilsuraweera@gmail.com",
            "Admin@123",
            "Admin"),
    ];
}

public sealed record BootstrappedLocalAccount(
    Guid UserId,
    string ExternalIdentity,
    string DisplayName,
    string Email,
    string Password,
    string Role);

public sealed class LocalAuthResult
{
    private LocalAuthResult(bool isSuccess, int statusCode, string? errorCode, string message, AuthSessionResponse? payload)
    {
        this.IsSuccess = isSuccess;
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
        this.Message = message;
        this.Payload = payload;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public string? ErrorCode { get; }

    public string Message { get; }

    public AuthSessionResponse? Payload { get; }

    public static LocalAuthResult Success(AuthSessionResponse payload)
    {
        return new LocalAuthResult(true, StatusCodes.Status200OK, null, "Authentication completed.", payload);
    }

    public static LocalAuthResult Fail(int statusCode, string errorCode, string message)
    {
        return new LocalAuthResult(false, statusCode, errorCode, message, null);
    }
}
