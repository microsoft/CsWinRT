// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WellKnownErrorCodes"/>
internal static class WellKnownErrorCodes
{
    /// <summary>Operation successful.</summary>
    public const HRESULT S_OK = unchecked((int)0x00000000);

    /// <summary>Operation successful (false).</summary>
    public const HRESULT S_FALSE = unchecked((int)0x00000001);

    /// <summary>Operation aborted.</summary>
    public const HRESULT E_ABORT = unchecked((int)0x80004004);

    /// <summary>No such interface supported.</summary>
    public const HRESULT E_NOINTERFACE = unchecked((int)0x80004002);

    /// <summary>Pointer that is not valid.</summary>
    public const HRESULT E_POINTER = unchecked((int)0x80004003);

    /// <summary>Class not registered.</summary>
    public const HRESULT REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

    /// <summary>Type mismatch.</summary>
    public const HRESULT TYPE_E_TYPEMISMATCH = unchecked((int)0x80028CA0);

    /// <summary>Numeric overflow.</summary>
    public const HRESULT DISP_E_OVERFLOW = unchecked((int)0x8002000A);

    /// <summary>A concurrent or interleaved operation changed the state of the object, invalidating this operation.</summary>
    public const HRESULT E_CHANGED_STATE = unchecked((int)0x8000000C);

    /// <summary>The operation attempted to access data outside the valid range.</summary>
    public const HRESULT E_BOUNDS = unchecked((int)0x8000000B);

    /// <summary>.NET: Object has been disposed.</summary>
    public const HRESULT COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);

    /// <summary>.NET: The operation was canceled.</summary>
    public const HRESULT COR_E_OPERATIONCANCELED = unchecked((int)0x8013153B);

    /// <summary>.NET: Argument is out of range.</summary>
    public const HRESULT COR_E_ARGUMENTOUTOFRANGE = unchecked((int)0x80131502);

    /// <summary>.NET: Index was outside the bounds of the array.</summary>
    public const HRESULT COR_E_INDEXOUTOFRANGE = unchecked((int)0x80131508);

    /// <summary>.NET: The operation timed out.</summary>
    public const HRESULT COR_E_TIMEOUT = unchecked((int)0x80131505);

    /// <summary>.NET: Operation is invalid in the current state.</summary>
    public const HRESULT COR_E_INVALIDOPERATION = unchecked((int)0x80131509);

    /// <summary>The object has been closed.</summary>
    public const HRESULT RO_E_CLOSED = unchecked((int)0x80000013);

    /// <summary>Illegal state change.</summary>
    public const HRESULT E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000D);

    /// <summary>Method call is invalid in the current state.</summary>
    public const HRESULT E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);

    /// <summary>Delegate assignment is not allowed.</summary>
    public const HRESULT E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);

    /// <summary>The process has no package identity.</summary>
    public const HRESULT APPMODEL_ERROR_NO_PACKAGE = unchecked((int)0x80073D54);

    /// <summary>XAML parsing failed.</summary>
    public const HRESULT E_XAMLPARSEFAILED = unchecked((int)0x802B000A);

    /// <summary>Layout cycle detected.</summary>
    public const HRESULT E_LAYOUTCYCLE = unchecked((int)0x802B0014);

    /// <summary>Element is not enabled.</summary>
    public const HRESULT E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);

    /// <summary>Element is not available.</summary>
    public const HRESULT E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);

    /// <summary>Invalid window handle.</summary>
    public const HRESULT ERROR_INVALID_WINDOW_HANDLE = unchecked((int)0x80070578);

    /// <summary>Not implemented.</summary>
    public const HRESULT E_NOTIMPL = unchecked((int)0x80004001);

    /// <summary>Access denied.</summary>
    public const HRESULT E_ACCESSDENIED = unchecked((int)0x80070005);

    /// <summary>One or more arguments are invalid.</summary>
    public const HRESULT E_INVALIDARG = unchecked((int)0x80070057);

    /// <summary>Out of memory.</summary>
    public const HRESULT E_OUTOFMEMORY = unchecked((int)0x8007000E);

    /// <summary>The request is not supported.</summary>
    public const HRESULT E_NOTSUPPORTED = unchecked((int)0x80070032);

    /// <summary>Arithmetic operation resulted in an overflow.</summary>
    public const HRESULT ERROR_ARITHMETIC_OVERFLOW = unchecked((int)0x80070216);

    /// <summary>Filename or extension is too long.</summary>
    public const HRESULT ERROR_FILENAME_EXCED_RANGE = unchecked((int)0x800700CE);

    /// <summary>The system cannot find the file specified.</summary>
    public const HRESULT ERROR_FILE_NOT_FOUND = unchecked((int)0x80070002);

    /// <summary>Reached the end of the file.</summary>
    public const HRESULT ERROR_HANDLE_EOF = unchecked((int)0x80070026);

    /// <summary>The system cannot find the path specified.</summary>
    public const HRESULT ERROR_PATH_NOT_FOUND = unchecked((int)0x80070003);

    /// <summary>Stack overflow.</summary>
    public const HRESULT ERROR_STACK_OVERFLOW = unchecked((int)0x800703E9);

    /// <summary>Invalid image format.</summary>
    public const HRESULT ERROR_BAD_FORMAT = unchecked((int)0x8007000B);

    /// <summary>The operation was canceled.</summary>
    public const HRESULT ERROR_CANCELLED = unchecked((int)0x800704C7);

    /// <summary>This operation returned because the timeout period expired.</summary>
    public const HRESULT ERROR_TIMEOUT = unchecked((int)0x800705B4);

    /// <summary>The object invoked has disconnected from its clients.</summary>
    public const HRESULT RPC_E_DISCONNECTED = unchecked((int)0x80010108);

    /// <summary>The RPC server is unavailable.</summary>
    public const HRESULT RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);

    /// <summary>Cannot execute script.</summary>
    public const HRESULT JSCRIPT_E_CANTEXECUTE = unchecked((int)0x89020001);
}