namespace GTEK.FSM.Backend.Application.Categories;

public sealed class CategoryMutationResult
{
    private CategoryMutationResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedCategoryItem? payload)
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

    public QueriedCategoryItem? Payload { get; }

    public static CategoryMutationResult Success(QueriedCategoryItem payload, string message)
    {
        return new CategoryMutationResult(true, message, null, null, payload);
    }

    public static CategoryMutationResult Failure(string message, string errorCode, int statusCode)
    {
        return new CategoryMutationResult(false, message, errorCode, statusCode, null);
    }
}
