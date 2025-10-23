// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

[GeneratedComClass]

internal sealed partial class ManagedExceptionErrorInfo : IErrorInfo
{
    private readonly Exception _exception;

    public ManagedExceptionErrorInfo(Exception ex)
    {
        _exception = ex;
    }

    public Guid GetGuid()
    {
        return default;
    }

    public string? GetSource()
    {
        return _exception.Source;
    }

    public string? GetDescription()
    {
        string message = _exception.Message;
        if (string.IsNullOrEmpty(message))
        {
            Type exceptionType = _exception.GetType();
            message = exceptionType.Name;
        }
        return message;
    }

    public string? GetHelpFile()
    {
        return _exception.HelpLink;
    }

    public string GetHelpFileContent()
    {
        return string.Empty;
    }
}