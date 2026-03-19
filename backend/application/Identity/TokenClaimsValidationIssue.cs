namespace GTEK.FSM.Backend.Application.Identity;

public sealed record TokenClaimsValidationIssue(
    string Claim,
    string Code,
    string Message);
