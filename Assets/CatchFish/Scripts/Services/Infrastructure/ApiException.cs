using System;

/// <summary>The backend refused a request or couldn't be reached. <see cref="Code"/> is the error code from Server/README.md.</summary>
public class ApiException : Exception
{
    public ApiException(string code, string message)
        : base($"{code}: {message}")
    {
        Code = code;
    }

    public string Code { get; }
}
