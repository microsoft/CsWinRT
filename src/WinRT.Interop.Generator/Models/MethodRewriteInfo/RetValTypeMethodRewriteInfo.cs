// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains info for a target method for two-pass IL generation, for an unmanaged <c>[retval]</c> value.
/// </summary>
/// <see cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod"/>
internal sealed class RetValTypeMethodRewriteInfo : MethodRewriteInfo;
