using System;

/// <summary>The server refused a request or couldn't be reached. <see cref="Code"/> is the error code from Server/README.md.</summary>
public class ApiException : Exception
{
    /// <summary>The server couldn't be reached at all.</summary>
    public const string ConnectionError = "connection_error";

    /// <summary>The server doesn't know this device's guest (see <see cref="IApiClient.SignInAsync"/>).</summary>
    public const string AccountNotFound = "account_not_found";

    public ApiException(string code, string message)
        : base($"{code}: {message}")
    {
        Code = code;
    }

    public string Code { get; }
}
