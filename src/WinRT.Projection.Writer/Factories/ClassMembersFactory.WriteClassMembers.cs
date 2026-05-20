// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class ClassMembersFactory
{
    /// <summary>
    /// Emits all instance members (methods, properties, events) inherited from implemented interfaces.
    /// In reference-projection mode, type declarations and per-interface objref getters are
    /// emitted, but non-mapped instance method/property/event bodies are emitted as <c>=> throw null;</c> stubs.
    /// </summary>
    public static void WriteClassMembers(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        HashSet<string> writtenMethods = [];

        // For properties: track per-name accessor presence so we can merge get/set across interfaces.
        // Use insertion-order Dictionary so the per-class property emission order matches the
        // .winmd metadata definition order order).
        Dictionary<string, PropertyAccessorState> propertyState = [];
        HashSet<string> writtenEvents = [];
        HashSet<TypeDefinition> writtenInterfaces = [];

        // interface inside WriteInterfaceMembersRecursive (right before that interface's
        // members), instead of one upfront block. This interleaves the GetInterface() impls
        // with their corresponding interface body, matching truth's per-interface layout.
        WriteInterfaceMembersRecursive(writer, context, type, type, null, writtenMethods, propertyState, writtenEvents, writtenInterfaces);

        // After collecting all properties (with merged accessors), emit them.
        foreach (KeyValuePair<string, PropertyAccessorState> kvp in propertyState)
        {
            PropertyAccessorState s = kvp.Value;

            // For generic-interface properties, emit the UnsafeAccessor static externs above the
            // property declaration. Note: getter and setter use the same accessor name (because
            // C# allows method overloading on parameter list for the static externs).
            if (s.HasGetter && s.GetterIsGeneric && !string.IsNullOrEmpty(s.GetterGenericInteropType))
            {
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "{{kvp.Key}}")]
                    static extern {{s.GetterPropTypeText}} {{s.GetterGenericAccessorName}}([UnsafeAccessorType("{{s.GetterGenericInteropType}}")] object _, WindowsRuntimeObjectReference thisReference);
                    """);
            }

            if (s.HasSetter && s.SetterIsGeneric && !string.IsNullOrEmpty(s.SetterGenericInteropType))
            {
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "{{kvp.Key}}")]
                    static extern void {{s.SetterGenericAccessorName}}([UnsafeAccessorType("{{s.SetterGenericInteropType}}")] object _, WindowsRuntimeObjectReference thisReference, {{s.SetterPropTypeText}} value);
                    """);
            }

            writer.WriteLine();

            // when getter and setter platforms match; otherwise emit per-accessor.
            string getterPlat = s.GetterPlatformAttribute;
            string setterPlat = s.SetterPlatformAttribute;
            string propertyPlat = string.Empty;

            // If both accessor platform attributes are equal, collapse them to a single
            // property-level attribute. For getter-only or setter-only properties only one side
            // is set; compare the relevant side.
            bool bothSidesPresent = s.HasGetter && s.HasSetter;

            if (!bothSidesPresent || getterPlat == setterPlat)
            {
                // Collapse: prefer the populated side (treats both-empty as equal).
                propertyPlat = !string.IsNullOrEmpty(getterPlat) ? getterPlat : setterPlat;
                getterPlat = string.Empty;
                setterPlat = string.Empty;
            }

            writer.WriteIf(!string.IsNullOrEmpty(propertyPlat), propertyPlat);

            writer.Write($"{s.Access}{s.MethodSpec}{s.PropTypeText} {kvp.Key}");

            // For getter-only properties, emit expression body: 'public T Prop => Expr;'
            // For getter+setter or setter-only, use accessor block: 'public T Prop { get => ...; set => ...; }'
            // In ref mode, all property bodies emit '=> throw null;'
            bool getterOnly = s.HasGetter && !s.HasSetter;

            if (getterOnly)
            {
                writer.Write(" => ");

                if (context.Settings.ReferenceProjection)
                {
                    writer.Write("throw null;");
                }
                else if (s.GetterIsGeneric)
                {
                    if (!string.IsNullOrEmpty(s.GetterGenericInteropType))
                    {
                        writer.Write($"{s.GetterGenericAccessorName}(null, {s.GetterObjRef});");
                    }
                    else
                    {
                        writer.Write("throw null!;");
                    }
                }
                else
                {
                    writer.Write($"{s.GetterAbiClass}.{kvp.Key}({s.GetterObjRef});");
                }

                writer.WriteLine();
            }
            else
            {
                writer.WriteLine();
                using (writer.WriteBlock())
                {
                    if (s.HasGetter)
                    {
                        if (!string.IsNullOrEmpty(getterPlat))
                        {
                            writer.Write(getterPlat);
                        }

                        if (context.Settings.ReferenceProjection)
                        {
                            writer.WriteLine("get => throw null;");
                        }
                        else if (s.GetterIsGeneric)
                        {
                            if (!string.IsNullOrEmpty(s.GetterGenericInteropType))
                            {
                                writer.WriteLine($"get => {s.GetterGenericAccessorName}(null, {s.GetterObjRef});");
                            }
                            else
                            {
                                writer.WriteLine("get => throw null!;");
                            }
                        }
                        else
                        {
                            writer.WriteLine($"get => {s.GetterAbiClass}.{kvp.Key}({s.GetterObjRef});");
                        }
                    }

                    if (s.HasSetter)
                    {
                        if (!string.IsNullOrEmpty(setterPlat))
                        {
                            writer.Write(setterPlat);
                        }

                        if (context.Settings.ReferenceProjection)
                        {
                            writer.WriteLine("set => throw null;");
                        }
                        else if (s.SetterIsGeneric)
                        {
                            if (!string.IsNullOrEmpty(s.SetterGenericInteropType))
                            {
                                writer.WriteLine($"set => {s.SetterGenericAccessorName}(null, {s.SetterObjRef}, value);");
                            }
                            else
                            {
                                writer.WriteLine("set => throw null!;");
                            }
                        }
                        else
                        {
                            writer.WriteLine($"set => {s.SetterAbiClass}.{kvp.Key}({s.SetterObjRef}, value);");
                        }
                    }
                }
            }

            // For overridable properties, emit an explicit interface implementation that
            // delegates to the protected property.:
            //   T InterfaceName.PropName { get => PropName; }
            //   T InterfaceName.PropName { set => PropName = value; }
            if (s.IsOverridable && s.OverridableInterface is not null)
            {
                WriteInterfaceTypeNameForCcwCallback iface = WriteInterfaceTypeNameForCcw(context, s.OverridableInterface);
                writer.Write($"{s.PropTypeText} {iface}.{kvp.Key} {{");

                if (s.HasGetter)
                {
                    writer.Write($"get => {kvp.Key}; ");
                }

                if (s.HasSetter)
                {
                    writer.Write($"set => {kvp.Key} = value; ");
                }

                writer.WriteLine("}");
            }
        }

        // GetInterface() / GetDefaultInterface() impls are emitted per-interface inside
        // WriteInterfaceMembersRecursive (matches the original code's per-interface ordering).
    }
}
