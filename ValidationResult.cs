namespace ESDEUpdater;

public sealed class ValidationResult
{
    public bool IsSuccess { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public FolderAnalysis? OldAnalysis { get; init; }
    public FolderAnalysis? NewAnalysis { get; init; }

    public static ValidationResult Success(FolderAnalysis oldAnalysis, FolderAnalysis newAnalysis) =>
        new()
        {
            IsSuccess = true,
            OldAnalysis = oldAnalysis,
            NewAnalysis = newAnalysis
        };

    public static ValidationResult Failure(string title, string message, FolderAnalysis? old = null, FolderAnalysis? newAnalysis = null) =>
        new()
        {
            IsSuccess = false,
            Title = title,
            Message = message,
            OldAnalysis = old,
            NewAnalysis = newAnalysis
        };
}
