namespace AspectUtils.Desktop;

public sealed class OperationOutcome
{
    public bool Success { get; init; } = true;
    public string Summary { get; init; } = "Done.";

    public static OperationOutcome Ok(string summary) => new() { Success = true, Summary = summary };

    public static OperationOutcome Fail(string summary) => new() { Success = false, Summary = summary };
}
