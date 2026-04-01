namespace GTEK.FSM.Backend.Application.Categories;

public sealed class CategoriesQueryResult
{
    private CategoriesQueryResult(bool isSuccess, string message, string? errorCode, int? statusCode, IReadOnlyList<QueriedCategoryItem>? payload)
    {
        this.IsSuccess = isSuccess;
        this.Message = message;
        this.ErrorCode = errorCode;
        this.StatusCode = statusCode;
        this.Payload = payload;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public int? StatusCode { get; }

    public IReadOnlyList<QueriedCategoryItem>? Payload { get; }

    public static CategoriesQueryResult Success(IReadOnlyList<QueriedCategoryItem> payload, string message = "Categories retrieved.")
    {
        return new CategoriesQueryResult(true, message, null, null, payload);
    }

    public static CategoriesQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new CategoriesQueryResult(false, message, errorCode, statusCode, null);
    }
}
