namespace GTEK.FSM.Backend.Application.Identity;

public sealed class TokenClaimsValidationResult
{
    private readonly List<TokenClaimsValidationIssue> issues = new();

    public TokenClaimsPayload? Payload { get; private set; }

    public bool IsValid => this.issues.Count == 0 && this.Payload is not null;

    public IReadOnlyList<TokenClaimsValidationIssue> Issues => this.issues;

    internal void SetPayload(TokenClaimsPayload payload)
    {
        this.Payload = payload;
    }

    internal void AddIssue(string claim, string code, string message)
    {
        this.issues.Add(new TokenClaimsValidationIssue(claim, code, message));
    }
}
