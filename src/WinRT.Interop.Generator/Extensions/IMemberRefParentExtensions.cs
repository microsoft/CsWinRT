// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="IMemberRefParent"/> type.
/// </summary>
internal static class IMemberRefParentExtensions
{
    extension(IMemberRefParent memberRefParent)
    {
        /// <summary>
        /// Constructs a reference to a constructor for the target parent member.
        /// </summary>
        /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns>The constructed reference.</returns>
        public MemberReference CreateConstructorReference(CorLibTypeFactory corLibTypeFactory, params TypeSignature[] parameterTypes)
        {
            return memberRefParent.CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: corLibTypeFactory.Void,
                parameterTypes: parameterTypes));
        }
    }
}
