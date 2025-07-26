namespace NetSDR.Client.Models;

public readonly struct ConnectionResult(bool isSuccess, string? message = null)
{
    public bool IsSuccess { get; } = isSuccess;
    public string? Message { get; } = message;

    public override string ToString() =>
        IsSuccess ? "Success" : $"Error: {Message ?? "Unknown"}";
}