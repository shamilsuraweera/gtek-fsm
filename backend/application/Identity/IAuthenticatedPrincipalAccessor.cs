namespace GTEK.FSM.Backend.Application.Identity;

/// <summary>
/// Application-level abstraction for reading the current authenticated principal.
/// Implementations may use HTTP, messaging, or other transports.
/// </summary>
public interface IAuthenticatedPrincipalAccessor
{
    AuthenticatedPrincipal? GetCurrent();
}
