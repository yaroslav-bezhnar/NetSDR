﻿namespace NetSDR.Client.Models;

public readonly struct ConnectionResult(bool isSuccess, string? message = null)
{
    #region properties

    public bool IsSuccess { get; } = isSuccess;
    public string? Message { get; } = message;

    #endregion

    #region methods

    public override string ToString() => IsSuccess ? "Success" : $"Error: {Message ?? "Unknown"}";

    #endregion
}