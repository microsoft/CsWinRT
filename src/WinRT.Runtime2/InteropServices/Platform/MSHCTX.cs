// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-mshctx"/>
internal enum MSHCTX
{
    MSHCTX_LOCAL = 0,
    MSHCTX_NOSHAREDMEM = 1,
    MSHCTX_DIFFERENTMACHINE = 2,
    MSHCTX_INPROC = 3,
    MSHCTX_CROSSCTX = 4,
    MSHCTX_CONTAINER = 5
}
