#:package ConsoleAppFramework@5.6.2
#:package Microsoft.CodeAnalysis.CSharp@5.0.0

using ConsoleAppFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

ConsoleApp.Run(args, Run);

/// <summary>
/// Prints the public API surface of the WinRT.Runtime project.
/// </summary>
/// <param name="output">The output .cs file path to write the API surface to.</param>
/// <param name="public">When set, filters out private implementation detail types/members (marked with CSWINRT3001).</param>
/// <param name="skipProjected">When set, filters out projected WinRT types (marked with [WindowsRuntimeMetadata]).</param>
static void Run([Argument] string output, bool @public = false, bool skipProjected = false)
{
    string runtimeDir = Path.GetFullPath(Path.Combine("src", "WinRT.Runtime2"));

    if (!Directory.Exists(runtimeDir))
    {
        Console.Error.WriteLine($"Error: Directory not found: {runtimeDir}");
        Console.Error.WriteLine("Run this script from the repository root.");
        Environment.Exit(1);
    }

    // Find all .cs files excluding bin/obj directories
    char sep = Path.DirectorySeparatorChar;
    var csFiles = Directory.GetFiles(runtimeDir, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}obj{sep}"))
        .ToList();

    ConsoleApp.Log($"Parsing {csFiles.Count} C# files from {runtimeDir}");

    var parseOpts = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
    var trees = csFiles
        .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOpts, path: f))
        .ToList();

    // Collect all public types keyed by "namespace.TypeName`arity"
    var typeMap = new Dictionary<string, TypeInfo>();

    // First pass: collect names of all types marked as private implementation details.
    // This is used to filter their attribute usages when --public is set.
    var privateImplTypeNames = new HashSet<string>(StringComparer.Ordinal);
    foreach (var tree in trees)
    {
        CollectPrivateImplTypeNames(tree.GetCompilationUnitRoot(), privateImplTypeNames);
    }

    // Second pass: also include types that derive from private impl detail types
    // (e.g. internal ComWrappersMarshallerAttribute types that inherit from the base with CSWINRT3001)
    foreach (var tree in trees)
    {
        CollectDerivedPrivateImplTypeNames(tree.GetCompilationUnitRoot(), privateImplTypeNames);
    }

    foreach (var tree in trees)
    {
        CollectFromNode(tree.GetCompilationUnitRoot(), "", typeMap);
    }

    // Format output
    var sb = new StringBuilder();
    bool shouldIncludeType(TypeInfo t) =>
        (!@public || !t.IsPrivateImplDetail) &&
        (!skipProjected || !t.IsProjectedType);

    var byNs = typeMap.Values
        .Where(shouldIncludeType)
        .GroupBy(t => t.Namespace)
        .Where(g => g.Any(shouldIncludeType))
        .OrderBy(g => g.Key, StringComparer.Ordinal);

    bool firstNs = true;
    foreach (var nsGroup in byNs)
    {
        if (!firstNs) sb.AppendLine();
        firstNs = false;

        sb.AppendLine($"namespace {nsGroup.Key}");
        sb.AppendLine("{");

        var types = nsGroup
            .Where(shouldIncludeType)
            .OrderBy(t => GetTypeSortKey(t), StringComparer.Ordinal)
            .ToList();

        for (int i = 0; i < types.Count; i++)
        {
            if (i > 0) sb.AppendLine();
            WriteType(sb, types[i], "    ", @public, privateImplTypeNames, skipProjected);
        }

        sb.AppendLine("}");
    }

    string result = Regexes.HResultAlias.Replace(sb.ToString(), "int").TrimEnd();
    File.WriteAllText(output, result + Environment.NewLine);
    ConsoleApp.Log($"Wrote {typeMap.Count} types to {output}");
}

#region Collection

/// <summary>
/// First pass: collect the simple names of ALL types (including non-public) with CSWINRT3001.
/// </summary>
static void CollectPrivateImplTypeNames(SyntaxNode node, HashSet<string> names)
{
    switch (node)
    {
        case CompilationUnitSyntax cu:
            foreach (var m in cu.Members) CollectPrivateImplTypeNames(m, names);
            break;
        case FileScopedNamespaceDeclarationSyntax fsns:
            foreach (var m in fsns.Members) CollectPrivateImplTypeNames(m, names);
            break;
        case NamespaceDeclarationSyntax nsd:
            foreach (var m in nsd.Members) CollectPrivateImplTypeNames(m, names);
            break;
        case BaseTypeDeclarationSyntax btd:
            if (HasCsWinRT3001(btd.AttributeLists))
            {
                string id = btd.Identifier.Text;
                names.Add(id);
                if (id.EndsWith("Attribute"))
                    names.Add(id[..^"Attribute".Length]);
            }
            // Recurse into nested types
            if (btd is TypeDeclarationSyntax td2)
                foreach (var m in td2.Members)
                    CollectPrivateImplTypeNames(m, names);
            break;
        case DelegateDeclarationSyntax dd:
            if (HasCsWinRT3001(dd.AttributeLists))
            {
                string id = dd.Identifier.Text;
                names.Add(id);
                if (id.EndsWith("Attribute"))
                    names.Add(id[..^"Attribute".Length]);
            }
            break;
    }
}

/// <summary>
/// Second pass: find types that derive from known private impl detail types.
/// </summary>
static void CollectDerivedPrivateImplTypeNames(SyntaxNode node, HashSet<string> names)
{
    switch (node)
    {
        case CompilationUnitSyntax cu:
            foreach (var m in cu.Members) CollectDerivedPrivateImplTypeNames(m, names);
            break;
        case FileScopedNamespaceDeclarationSyntax fsns:
            foreach (var m in fsns.Members) CollectDerivedPrivateImplTypeNames(m, names);
            break;
        case NamespaceDeclarationSyntax nsd:
            foreach (var m in nsd.Members) CollectDerivedPrivateImplTypeNames(m, names);
            break;
        case TypeDeclarationSyntax td:
            if (td.BaseList != null)
            {
                foreach (var bt in td.BaseList.Types)
                {
                    string baseSimpleName = GetSimpleNameFromDotted(bt.Type.ToString());
                    if (names.Contains(baseSimpleName) || names.Contains(baseSimpleName + "Attribute"))
                    {
                        string id = td.Identifier.Text;
                        names.Add(id);
                        if (id.EndsWith("Attribute"))
                            names.Add(id[..^"Attribute".Length]);
                    }
                }
            }
            // Recurse into nested types
            foreach (var m in td.Members)
                CollectDerivedPrivateImplTypeNames(m, names);
            break;
    }
}

static void CollectFromNode(SyntaxNode node, string ns, Dictionary<string, TypeInfo> typeMap)
{
    switch (node)
    {
        case CompilationUnitSyntax cu:
            foreach (var m in cu.Members) CollectFromNode(m, ns, typeMap);
            break;

        case FileScopedNamespaceDeclarationSyntax fsns:
            foreach (var m in fsns.Members) CollectFromNode(m, fsns.Name.ToString(), typeMap);
            break;

        case NamespaceDeclarationSyntax nsd:
            string nsName = ns.Length > 0 ? $"{ns}.{nsd.Name}" : nsd.Name.ToString();
            foreach (var m in nsd.Members) CollectFromNode(m, nsName, typeMap);
            break;

        case TypeDeclarationSyntax td:
            CollectType(td, ns, typeMap, isNested: false);
            break;

        case EnumDeclarationSyntax ed:
            CollectEnum(ed, ns, typeMap, isNested: false);
            break;

        case DelegateDeclarationSyntax dd:
            CollectDelegate(dd, ns, typeMap, isNested: false);
            break;
    }
}

static void CollectType(TypeDeclarationSyntax td, string ns, Dictionary<string, TypeInfo> typeMap, bool isNested)
{
    if (!IsPublicApiType(td, isNested)) return;

    string kind = td.Kind() switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.InterfaceDeclaration => "interface",
        SyntaxKind.RecordDeclaration => "record class",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => "class"
    };

    bool isInterface = td.IsKind(SyntaxKind.InterfaceDeclaration);
    int arity = td.TypeParameterList?.Parameters.Count ?? 0;
    string key = $"{ns}.{td.Identifier.Text}`{arity}";

    if (!typeMap.TryGetValue(key, out var info))
    {
        info = new TypeInfo
        {
            Namespace = ns,
            Identifier = td.Identifier.Text,
            Kind = kind,
            IsInterface = isInterface,
        };
        typeMap[key] = info;
    }

    // Merge type parameter list (take first non-null)
    if (info.TypeParameterListText == null && td.TypeParameterList != null)
        info.TypeParameterListText = NormalizeWhitespace(td.TypeParameterList.ToString());

    // Merge modifiers (skip unsafe and partial)
    foreach (var mod in td.Modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        info.Modifiers.Add(mod.Text);
    }

    // Merge attributes (deduplicated by normalized text, MethodImpl filtered)
    foreach (var attrText in CollectAttributeTexts(td.AttributeLists))
    {
        if (!info.AttributeTexts.Contains(attrText))
            info.AttributeTexts.Add(attrText);
    }

    // Check private impl detail
    if (HasCsWinRT3001(td.AttributeLists))
        info.IsPrivateImplDetail = true;

    // Check projected type
    if (HasWindowsRuntimeMetadata(td.AttributeLists))
        info.IsProjectedType = true;

    // Merge base types
    if (td.BaseList != null)
    {
        foreach (var bt in td.BaseList.Types)
        {
            string btText = bt.Type.ToString();
            if (!info.BaseTypes.Contains(btText))
                info.BaseTypes.Add(btText);
        }
    }

    // Merge constraints
    foreach (var cc in td.ConstraintClauses)
    {
        string ccText = NormalizeWhitespace(cc.ToString());
        if (!info.Constraints.Contains(ccText))
            info.Constraints.Add(ccText);
    }

    // Collect members
    foreach (var member in td.Members)
    {
        switch (member)
        {
            case TypeDeclarationSyntax nestedTd:
                CollectNestedType(nestedTd, ns, info);
                break;
            case EnumDeclarationSyntax nestedEd:
                CollectNestedEnum(nestedEd, info);
                break;
            case DelegateDeclarationSyntax nestedDd:
                CollectNestedDelegate(nestedDd, info);
                break;
            default:
                if (!IsPublicApiMember(member, isInterface)) break;
                var entry = FormatMemberEntry(member, isInterface);
                if (entry != null && !info.MemberDecls.Contains(entry.Declaration))
                {
                    info.Members.Add(entry);
                    info.MemberDecls.Add(entry.Declaration);
                }
                break;
        }
    }
}

static void CollectEnum(EnumDeclarationSyntax ed, string ns, Dictionary<string, TypeInfo> typeMap, bool isNested)
{
    if (!IsPublicApiType(ed, isNested)) return;

    string key = $"{ns}.{ed.Identifier.Text}`0";

    if (!typeMap.TryGetValue(key, out var info))
    {
        info = new TypeInfo
        {
            Namespace = ns,
            Identifier = ed.Identifier.Text,
            Kind = "enum",
        };
        typeMap[key] = info;
    }

    // Merge modifiers
    foreach (var mod in ed.Modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        info.Modifiers.Add(mod.Text);
    }

    // Merge attributes
    foreach (var attrText in CollectAttributeTexts(ed.AttributeLists))
    {
        if (!info.AttributeTexts.Contains(attrText))
            info.AttributeTexts.Add(attrText);
    }

    // Check private impl detail
    if (HasCsWinRT3001(ed.AttributeLists))
        info.IsPrivateImplDetail = true;

    // Check projected type (or contract enum)
    if (HasWindowsRuntimeMetadata(ed.AttributeLists) || HasContractVersionAttribute(ed.AttributeLists))
        info.IsProjectedType = true;

    if (ed.BaseList != null && info.EnumBaseType == null)
        info.EnumBaseType = ed.BaseList.Types.First().Type.ToString();

    // Collect enum members
    foreach (var em in ed.Members)
    {
        var emSb = new StringBuilder();

        // Enum member attributes
        foreach (var al in em.AttributeLists)
            emSb.AppendLine(NormalizeWhitespace(al.ToString()));

        emSb.Append(em.Identifier.Text);
        if (em.EqualsValue != null)
            emSb.Append($" = {em.EqualsValue.Value}");
        emSb.Append(',');

        string emText = emSb.ToString();
        if (!info.EnumMembers.Contains(emText))
            info.EnumMembers.Add(emText);
    }
}

static void CollectDelegate(DelegateDeclarationSyntax dd, string ns, Dictionary<string, TypeInfo> typeMap, bool isNested)
{
    if (!IsPublicApiType(dd, isNested)) return;

    int arity = dd.TypeParameterList?.Parameters.Count ?? 0;
    string key = $"{ns}.{dd.Identifier.Text}`{arity}";

    if (typeMap.ContainsKey(key)) return; // Delegates shouldn't be partial

    var info = new TypeInfo
    {
        Namespace = ns,
        Identifier = dd.Identifier.Text,
        Kind = "delegate",
    };
    typeMap[key] = info;

    // Modifiers
    foreach (var mod in dd.Modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        info.Modifiers.Add(mod.Text);
    }

    // Attributes
    foreach (var attrText in CollectAttributeTexts(dd.AttributeLists))
    {
        info.AttributeTexts.Add(attrText);
    }

    // Check private impl detail
    if (HasCsWinRT3001(dd.AttributeLists))
        info.IsPrivateImplDetail = true;

    // Check projected type
    if (HasWindowsRuntimeMetadata(dd.AttributeLists))
        info.IsProjectedType = true;

    // Type parameters
    if (dd.TypeParameterList != null)
        info.TypeParameterListText = NormalizeWhitespace(dd.TypeParameterList.ToString());
    // Constraints
    foreach (var cc in dd.ConstraintClauses)
        info.Constraints.Add(NormalizeWhitespace(cc.ToString()));

    // Store delegate-specific info
    info.DelegateReturnType = dd.ReturnType.ToString();
    info.DelegateParameterList = dd.ParameterList.ToString();
}

static void CollectNestedType(TypeDeclarationSyntax td, string parentNs, TypeInfo parent)
{
    if (!IsPublicApiType(td, isNested: true)) return;

    string kind = td.Kind() switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.InterfaceDeclaration => "interface",
        SyntaxKind.RecordDeclaration => "record class",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => "class"
    };

    bool isInterface = td.IsKind(SyntaxKind.InterfaceDeclaration);
    int arity = td.TypeParameterList?.Parameters.Count ?? 0;
    string nestedKey = $"{td.Identifier.Text}`{arity}";

    // Find or create nested type in parent
    var nested = parent.NestedTypes.FirstOrDefault(n => n.Identifier == td.Identifier.Text
        && (n.TypeParameterListText?.Split(',').Length ?? 0) == arity);

    if (nested == null)
    {
        nested = new TypeInfo
        {
            Namespace = parentNs,
            Identifier = td.Identifier.Text,
            Kind = kind,
            IsInterface = isInterface,
        };
        parent.NestedTypes.Add(nested);
    }

    // Merge (same logic as CollectType)
    if (nested.TypeParameterListText == null && td.TypeParameterList != null)
        nested.TypeParameterListText = NormalizeWhitespace(td.TypeParameterList.ToString());

    foreach (var mod in td.Modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        nested.Modifiers.Add(mod.Text);
    }

    foreach (var attrText in CollectAttributeTexts(td.AttributeLists))
    {
        if (!nested.AttributeTexts.Contains(attrText))
            nested.AttributeTexts.Add(attrText);
    }

    if (HasCsWinRT3001(td.AttributeLists))
        nested.IsPrivateImplDetail = true;

    if (HasWindowsRuntimeMetadata(td.AttributeLists))
        nested.IsProjectedType = true;

    if (td.BaseList != null)
    {
        foreach (var bt in td.BaseList.Types)
        {
            string btText = bt.Type.ToString();
            if (!nested.BaseTypes.Contains(btText))
                nested.BaseTypes.Add(btText);
        }
    }

    foreach (var cc in td.ConstraintClauses)
    {
        string ccText = NormalizeWhitespace(cc.ToString());
        if (!nested.Constraints.Contains(ccText))
            nested.Constraints.Add(ccText);
    }

    foreach (var member in td.Members)
    {
        switch (member)
        {
            case TypeDeclarationSyntax nestedTd:
                CollectNestedType(nestedTd, parentNs, nested);
                break;
            case EnumDeclarationSyntax nestedEd:
                CollectNestedEnum(nestedEd, nested);
                break;
            case DelegateDeclarationSyntax nestedDd:
                CollectNestedDelegate(nestedDd, nested);
                break;
            default:
                if (!IsPublicApiMember(member, isInterface)) break;
                var entry = FormatMemberEntry(member, isInterface);
                if (entry != null && !nested.MemberDecls.Contains(entry.Declaration))
                {
                    nested.Members.Add(entry);
                    nested.MemberDecls.Add(entry.Declaration);
                }
                break;
        }
    }
}

static void CollectNestedEnum(EnumDeclarationSyntax ed, TypeInfo parent)
{
    if (!IsPublicApiType(ed, isNested: true)) return;

    var nested = new TypeInfo
    {
        Namespace = parent.Namespace,
        Identifier = ed.Identifier.Text,
        Kind = "enum",
    };

    foreach (var mod in ed.Modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        nested.Modifiers.Add(mod.Text);
    }

    foreach (var attrText in CollectAttributeTexts(ed.AttributeLists))
        nested.AttributeTexts.Add(attrText);

    if (HasCsWinRT3001(ed.AttributeLists))
        nested.IsPrivateImplDetail = true;

    if (HasWindowsRuntimeMetadata(ed.AttributeLists) || HasContractVersionAttribute(ed.AttributeLists))
        nested.IsProjectedType = true;

    if (ed.BaseList != null)
        nested.EnumBaseType = ed.BaseList.Types.First().Type.ToString();

    foreach (var em in ed.Members)
    {
        var emSb = new StringBuilder();
        foreach (var al in em.AttributeLists)
            emSb.AppendLine(NormalizeWhitespace(al.ToString()));
        emSb.Append(em.Identifier.Text);
        if (em.EqualsValue != null)
            emSb.Append($" = {em.EqualsValue.Value}");
        emSb.Append(',');
        nested.EnumMembers.Add(emSb.ToString());
    }

    parent.NestedTypes.Add(nested);
}

static void CollectNestedDelegate(DelegateDeclarationSyntax dd, TypeInfo parent)
{
    if (!IsPublicApiType(dd, isNested: true)) return;

    var nested = new TypeInfo
    {
        Namespace = parent.Namespace,
        Identifier = dd.Identifier.Text,
        Kind = "delegate",
    };

    foreach (var mod in dd.Modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        nested.Modifiers.Add(mod.Text);
    }

    foreach (var attrText in CollectAttributeTexts(dd.AttributeLists))
        nested.AttributeTexts.Add(attrText);

    if (HasCsWinRT3001(dd.AttributeLists))
        nested.IsPrivateImplDetail = true;

    if (HasWindowsRuntimeMetadata(dd.AttributeLists))
        nested.IsProjectedType = true;

    if (dd.TypeParameterList != null)
        nested.TypeParameterListText = NormalizeWhitespace(dd.TypeParameterList.ToString());

    foreach (var cc in dd.ConstraintClauses)
        nested.Constraints.Add(NormalizeWhitespace(cc.ToString()));

    nested.DelegateReturnType = dd.ReturnType.ToString();
    nested.DelegateParameterList = dd.ParameterList.ToString();

    parent.NestedTypes.Add(nested);
}

#endregion

#region Member formatting

static MemberEntry? FormatMemberEntry(MemberDeclarationSyntax member, bool isInterface)
{
    return member switch
    {
        MethodDeclarationSyntax m => FormatMethod(m, isInterface),
        PropertyDeclarationSyntax p => FormatProperty(p, isInterface),
        EventDeclarationSyntax e => FormatEventWithAccessors(e, isInterface),
        EventFieldDeclarationSyntax ef => FormatEventField(ef, isInterface),
        FieldDeclarationSyntax f => FormatField(f),
        ConstructorDeclarationSyntax c => FormatConstructor(c),
        DestructorDeclarationSyntax d => FormatDestructor(d),
        OperatorDeclarationSyntax o => FormatOperator(o),
        ConversionOperatorDeclarationSyntax co => FormatConversion(co),
        IndexerDeclarationSyntax idx => FormatIndexer(idx, isInterface),
        _ => null
    };
}

static MemberEntry FormatMethod(MethodDeclarationSyntax m, bool isInterface)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(m.AttributeLists);

    // Modifiers
    string mods = FilterModifiers(m.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    // Return type first, then explicit interface specifier, then name
    sb.Append(m.ReturnType + " ");
    if (m.ExplicitInterfaceSpecifier != null)
        sb.Append(m.ExplicitInterfaceSpecifier);
    sb.Append(m.Identifier.Text);
    if (m.TypeParameterList != null)
        sb.Append(NormalizeWhitespace(m.TypeParameterList.ToString()));
    sb.Append(FormatParameterList(m.ParameterList));
    foreach (var cc in m.ConstraintClauses)
        sb.Append(" " + NormalizeWhitespace(cc.ToString()));
    sb.Append(';');

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(m.AttributeLists),
        SortKey = $"6_{m.Identifier.Text}_{m.ParameterList.Parameters.Count:D3}"
    };
}

static MemberEntry FormatProperty(PropertyDeclarationSyntax p, bool isInterface)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(p.AttributeLists);

    string mods = FilterModifiers(p.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append(p.Type + " ");
    if (p.ExplicitInterfaceSpecifier != null)
        sb.Append(p.ExplicitInterfaceSpecifier);
    sb.Append(p.Identifier.Text);
    sb.Append(" " + FormatAccessors(p));

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(p.AttributeLists),
        SortKey = $"3_{p.Identifier.Text}"
    };
}

static MemberEntry FormatEventWithAccessors(EventDeclarationSyntax e, bool isInterface)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(e.AttributeLists);

    string mods = FilterModifiers(e.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append("event ");
    sb.Append(e.Type + " ");
    if (e.ExplicitInterfaceSpecifier != null)
        sb.Append(e.ExplicitInterfaceSpecifier);
    sb.Append(e.Identifier.Text);
    sb.Append(';');

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(e.AttributeLists),
        SortKey = $"5_{e.Identifier.Text}"
    };
}

static MemberEntry? FormatEventField(EventFieldDeclarationSyntax ef, bool isInterface)
{
    if (ef.Declaration.Variables.Count == 0) return null;

    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(ef.AttributeLists);

    string mods = FilterModifiers(ef.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append("event ");
    sb.Append(ef.Declaration.Type + " ");
    sb.Append(string.Join(", ", ef.Declaration.Variables.Select(v => v.Identifier.Text)));
    sb.Append(';');

    string name = ef.Declaration.Variables.First().Identifier.Text;
    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(ef.AttributeLists),
        SortKey = $"5_{name}"
    };
}

static MemberEntry? FormatField(FieldDeclarationSyntax f)
{
    if (f.Declaration.Variables.Count == 0) return null;

    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(f.AttributeLists);

    string mods = FilterModifiers(f.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append(f.Declaration.Type + " ");

    var vars = new List<string>();
    foreach (var v in f.Declaration.Variables)
    {
        string varText = v.Identifier.Text;
        // Include initializer for const fields
        if (v.Initializer != null && f.Modifiers.Any(SyntaxKind.ConstKeyword))
            varText += $" = {v.Initializer.Value}";
        vars.Add(varText);
    }
    sb.Append(string.Join(", ", vars));
    sb.Append(';');

    string name = f.Declaration.Variables.First().Identifier.Text;
    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(f.AttributeLists),
        SortKey = $"2_{name}"
    };
}

static MemberEntry FormatConstructor(ConstructorDeclarationSyntax c)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(c.AttributeLists);

    string mods = FilterModifiers(c.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append(c.Identifier.Text);
    sb.Append(FormatParameterList(c.ParameterList));
    sb.Append(';');

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(c.AttributeLists),
        SortKey = $"1_{c.ParameterList.Parameters.Count:D3}"
    };
}

static MemberEntry FormatDestructor(DestructorDeclarationSyntax d)
{
    return new MemberEntry
    {
        Declaration = $"~{d.Identifier.Text}();",
        IsPrivateImplDetail = false,
        SortKey = "1_destructor"
    };
}

static MemberEntry FormatOperator(OperatorDeclarationSyntax o)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(o.AttributeLists);

    string mods = FilterModifiers(o.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append(o.ReturnType + " ");
    sb.Append("operator ");
    sb.Append(o.OperatorToken.Text);
    sb.Append(FormatParameterList(o.ParameterList));
    sb.Append(';');

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(o.AttributeLists),
        SortKey = $"7_{o.OperatorToken.Text}"
    };
}

static MemberEntry FormatConversion(ConversionOperatorDeclarationSyntax co)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(co.AttributeLists);

    string mods = FilterModifiers(co.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append(co.ImplicitOrExplicitKeyword.Text + " ");
    sb.Append("operator ");
    sb.Append(co.Type);
    sb.Append(FormatParameterList(co.ParameterList));
    sb.Append(';');

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(co.AttributeLists),
        SortKey = $"8_{co.Type}"
    };
}

static MemberEntry FormatIndexer(IndexerDeclarationSyntax idx, bool isInterface)
{
    var sb = new StringBuilder();
    var attrs = CollectAttributeTexts(idx.AttributeLists);

    string mods = FilterModifiers(idx.Modifiers);
    if (mods.Length > 0) sb.Append(mods + " ");

    sb.Append(idx.Type + " ");
    if (idx.ExplicitInterfaceSpecifier != null)
        sb.Append(idx.ExplicitInterfaceSpecifier);
    sb.Append("this");
    sb.Append(FormatBracketedParameterList(idx.ParameterList));
    sb.Append(" " + FormatAccessors(idx));

    return new MemberEntry
    {
        Attributes = attrs,
        Declaration = sb.ToString(),
        IsPrivateImplDetail = HasCsWinRT3001(idx.AttributeLists),
        SortKey = "4_this"
    };
}

#endregion

#region Output writing

static void WriteType(StringBuilder sb, TypeInfo info, string indent, bool publicOnly, HashSet<string> privateImplTypeNames, bool skipProjected = false)
{
    // Write filtered attributes
    foreach (var attr in info.AttributeTexts)
    {
        if (!ShouldIncludeAttribute(attr, publicOnly, privateImplTypeNames))
            continue;
        sb.AppendLine(indent + attr);
    }

    // Build type header
    var header = new StringBuilder();
    header.Append(indent);

    // Modifiers
    string mods = FormatTypeModifiers(info.Modifiers, info.Kind);
    if (mods.Length > 0) header.Append(mods + " ");

    // Kind
    header.Append(info.Kind + " ");

    // Name + type params
    header.Append(info.Identifier);
    if (info.TypeParameterListText != null)
        header.Append(info.TypeParameterListText);

    // Delegate: print return type, params, constraints on same line
    if (info.Kind == "delegate")
    {
        var delegateHeader = new StringBuilder();
        delegateHeader.Append(indent);
        string delegateMods = FormatTypeModifiers(info.Modifiers, "delegate");
        if (delegateMods.Length > 0) delegateHeader.Append(delegateMods + " ");
        delegateHeader.Append("delegate ");
        delegateHeader.Append(info.DelegateReturnType + " ");
        delegateHeader.Append(info.Identifier);
        if (info.TypeParameterListText != null)
            delegateHeader.Append(info.TypeParameterListText);
        delegateHeader.Append(info.DelegateParameterList);
        foreach (var cc in info.Constraints)
            delegateHeader.Append(" " + cc);
        delegateHeader.Append(';');
        sb.AppendLine(delegateHeader.ToString());
        return;
    }

    // Enum: print base type
    if (info.Kind == "enum")
    {
        if (info.EnumBaseType != null)
            header.Append(" : " + info.EnumBaseType);
        sb.AppendLine(header.ToString());
        sb.AppendLine(indent + "{");
        foreach (var em in info.EnumMembers)
        {
            var lines = em.Split('\n');
            foreach (var line in lines)
            {
                string trimmed = line.TrimEnd('\r');
                if (trimmed.Length > 0)
                    sb.AppendLine(indent + "    " + trimmed);
            }
        }
        sb.AppendLine(indent + "}");
        return;
    }

    // Base types
    if (info.BaseTypes.Count > 0)
        header.Append(" : " + string.Join(", ", info.BaseTypes));

    // Constraints
    foreach (var cc in info.Constraints)
        header.Append(" " + cc);

    sb.AppendLine(header.ToString());
    sb.AppendLine(indent + "{");

    // Members
    var members = info.Members
        .Where(m => !publicOnly || !m.IsPrivateImplDetail)
        .OrderBy(m => m.SortKey, StringComparer.Ordinal)
        .ToList();

    string memberIndent = indent + "    ";
    foreach (var m in members)
    {
        // Write filtered member attributes
        foreach (var attr in m.Attributes)
        {
            if (!ShouldIncludeAttribute(attr, publicOnly, privateImplTypeNames))
                continue;
            sb.AppendLine(memberIndent + attr);
        }

        // Write the declaration
        sb.AppendLine(memberIndent + m.Declaration);
    }

    // Nested types
    var nestedTypes = info.NestedTypes
        .Where(t => (!publicOnly || !t.IsPrivateImplDetail) && (!skipProjected || !t.IsProjectedType))
        .OrderBy(t => GetTypeSortKey(t), StringComparer.Ordinal)
        .ToList();

    if (nestedTypes.Count > 0 && members.Count > 0)
        sb.AppendLine();

    for (int i = 0; i < nestedTypes.Count; i++)
    {
        if (i > 0) sb.AppendLine();
        WriteType(sb, nestedTypes[i], indent + "    ", publicOnly, privateImplTypeNames, skipProjected);
    }

    sb.AppendLine(indent + "}");
}

#endregion

#region Helpers

static bool IsPublicApiType(MemberDeclarationSyntax decl, bool isNested)
{
    var mods = decl.Modifiers;

    // file-scoped types are never public
    if (mods.Any(SyntaxKind.FileKeyword)) return false;

    if (isNested)
    {
        return mods.Any(SyntaxKind.PublicKeyword) ||
               (mods.Any(SyntaxKind.ProtectedKeyword) && !mods.Any(SyntaxKind.PrivateKeyword));
    }
    else
    {
        return mods.Any(SyntaxKind.PublicKeyword);
    }
}

static bool IsPublicApiMember(MemberDeclarationSyntax member, bool isInterface)
{
    if (isInterface)
    {
        // In interfaces, skip only explicitly private members
        return !member.Modifiers.Any(SyntaxKind.PrivateKeyword);
    }

    var mods = member.Modifiers;

    // Explicit interface implementations (no access modifier, but part of the contract)
    if (member is MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null })
        return true;
    if (member is PropertyDeclarationSyntax { ExplicitInterfaceSpecifier: not null })
        return true;
    if (member is EventDeclarationSyntax { ExplicitInterfaceSpecifier: not null })
        return true;
    if (member is IndexerDeclarationSyntax { ExplicitInterfaceSpecifier: not null })
        return true;

    return mods.Any(SyntaxKind.PublicKeyword) ||
           (mods.Any(SyntaxKind.ProtectedKeyword) && !mods.Any(SyntaxKind.PrivateKeyword));
}

static bool HasCsWinRT3001(SyntaxList<AttributeListSyntax> attributeLists)
{
    foreach (var attrList in attributeLists)
    {
        foreach (var attr in attrList.Attributes)
        {
            string name = attr.Name.ToString();
            if (name is not ("Obsolete" or "ObsoleteAttribute" or "System.Obsolete" or "System.ObsoleteAttribute"))
                continue;

            if (attr.ArgumentList == null) continue;

            foreach (var arg in attr.ArgumentList.Arguments)
            {
                if (arg.NameEquals?.Name.Identifier.ValueText != "DiagnosticId") continue;

                string exprText = arg.Expression.ToString();
                if (exprText.Contains("CSWINRT3001") ||
                    exprText.Contains("PrivateImplementationDetailObsoleteDiagnosticId"))
                    return true;
            }
        }
    }
    return false;
}

static bool HasWindowsRuntimeMetadata(SyntaxList<AttributeListSyntax> attributeLists)
{
    foreach (var attrList in attributeLists)
    {
        foreach (var attr in attrList.Attributes)
        {
            string name = GetAttributeSimpleName(attr.Name);
            if (name is "WindowsRuntimeMetadata" or "WindowsRuntimeMetadataAttribute")
                return true;
        }
    }
    return false;
}

static bool HasContractVersionAttribute(SyntaxList<AttributeListSyntax> attributeLists)
{
    foreach (var attrList in attributeLists)
    {
        foreach (var attr in attrList.Attributes)
        {
            string name = GetAttributeSimpleName(attr.Name);
            if (name is "ContractVersion" or "ContractVersionAttribute")
                return true;
        }
    }
    return false;
}

static string FilterModifiers(SyntaxTokenList modifiers)
{
    var parts = new List<string>();
    foreach (var mod in modifiers)
    {
        if (mod.Text is "unsafe" or "partial") continue;
        parts.Add(mod.Text);
    }
    return string.Join(" ", parts);
}

static string FormatTypeModifiers(HashSet<string> modifiers, string kind)
{
    // Order: access, static, new, abstract/sealed, readonly, ref
    var ordered = new List<string>();
    string[] order = ["public", "protected", "internal", "static", "new", "abstract", "sealed", "readonly", "ref"];

    foreach (var m in order)
    {
        if (modifiers.Contains(m))
            ordered.Add(m);
    }

    // Add any remaining modifiers not in the order list
    foreach (var m in modifiers)
    {
        if (!ordered.Contains(m))
            ordered.Add(m);
    }

    return string.Join(" ", ordered);
}

static string FormatAccessors(BasePropertyDeclarationSyntax prop)
{
    // Expression body -> read-only
    if (prop is PropertyDeclarationSyntax { ExpressionBody: not null })
        return "{ get; }";

    if (prop.AccessorList == null)
        return "{ get; }";

    var parts = new List<string>();
    foreach (var accessor in prop.AccessorList.Accessors)
    {
        // Skip non-public accessors
        if (accessor.Modifiers.Any(SyntaxKind.PrivateKeyword))
            continue;
        if (accessor.Modifiers.Any(SyntaxKind.InternalKeyword) && !accessor.Modifiers.Any(SyntaxKind.ProtectedKeyword))
            continue;

        var accSb = new StringBuilder();

        // Accessor attributes (skip MethodImpl)
        foreach (var al in accessor.AttributeLists)
        {
            if (al.Attributes.Count == 1 && IsMethodImplAttribute(al.Attributes[0]))
                continue;
            accSb.Append(NormalizeWhitespace(al.ToString()) + " ");
        }

        // Accessor modifiers (e.g., "protected")
        foreach (var mod in accessor.Modifiers)
        {
            if (mod.Text == "unsafe") continue;
            accSb.Append(mod.Text + " ");
        }

        accSb.Append(accessor.Keyword.Text);
        accSb.Append(';');
        parts.Add(accSb.ToString());
    }

    if (parts.Count == 0)
        return "{ get; }";

    return "{ " + string.Join(" ", parts) + " }";
}

static string FormatParameterList(ParameterListSyntax parameterList)
{
    // Normalize multi-line parameter lists to single line
    var parts = new List<string>();
    foreach (var param in parameterList.Parameters)
    {
        parts.Add(NormalizeWhitespace(param.ToFullString()));
    }
    return "(" + string.Join(", ", parts) + ")";
}

static string FormatBracketedParameterList(BracketedParameterListSyntax parameterList)
{
    var parts = new List<string>();
    foreach (var param in parameterList.Parameters)
    {
        parts.Add(NormalizeWhitespace(param.ToFullString()));
    }
    return "[" + string.Join(", ", parts) + "]";
}

static List<string> CollectAttributeTexts(SyntaxList<AttributeListSyntax> attributeLists)
{
    var result = new List<string>();
    foreach (var al in attributeLists)
    {
        // Always skip [MethodImpl(...)] attributes
        if (al.Attributes.Count == 1 && IsMethodImplAttribute(al.Attributes[0]))
            continue;
        result.Add(NormalizeWhitespace(al.ToString()));
    }
    return result;
}

static bool IsMethodImplAttribute(AttributeSyntax attr)
{
    string simpleName = GetAttributeSimpleName(attr.Name);
    return simpleName is "MethodImpl" or "MethodImplAttribute";
}

static bool IsPrivateImplDetailAttribute(string attrText, HashSet<string> privateImplTypeNames)
{
    // Extract the attribute name from a normalized text like "[SomeAttr(...)]"
    string name = ExtractAttributeNameFromText(attrText);
    string simpleName = GetSimpleNameFromDotted(name);
    return privateImplTypeNames.Contains(simpleName) || privateImplTypeNames.Contains(simpleName + "Attribute");
}

static string GetAttributeSimpleName(NameSyntax name)
{
    return name switch
    {
        SimpleNameSyntax simple => simple.Identifier.Text,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
        _ => name.ToString()
    };
}

static string ExtractAttributeNameFromText(string attrText)
{
    // Input: "[Ns.AttrName(args)]" or "[AttrName]" etc.
    int start = attrText.IndexOf('[') + 1;
    if (start <= 0) start = 0;

    // Skip target specifier like "return: "
    int colonIdx = attrText.IndexOf(':', start);
    int parenIdx = attrText.IndexOf('(', start);
    if (colonIdx >= 0 && (parenIdx < 0 || colonIdx < parenIdx))
        start = colonIdx + 1;

    int end = attrText.IndexOf('(', start);
    if (end < 0) end = attrText.IndexOf(']', start);
    if (end < 0) end = attrText.Length;

    return attrText[start..end].Trim();
}

static string GetSimpleNameFromDotted(string dottedName)
{
    int lastDot = dottedName.LastIndexOf('.');
    return lastDot >= 0 ? dottedName[(lastDot + 1)..] : dottedName;
}

static bool ShouldIncludeAttribute(string attrText, bool publicOnly, HashSet<string> privateImplTypeNames)
{
    if (publicOnly && IsPrivateImplDetailAttribute(attrText, privateImplTypeNames))
        return false;
    return true;
}

static string NormalizeWhitespace(string text)
{
    // Trim and normalize internal whitespace (collapse runs of whitespace to single space)
    var result = new StringBuilder();
    bool lastWasSpace = false;
    foreach (char c in text.Trim())
    {
        if (char.IsWhiteSpace(c))
        {
            if (!lastWasSpace)
            {
                result.Append(' ');
                lastWasSpace = true;
            }
        }
        else
        {
            result.Append(c);
            lastWasSpace = false;
        }
    }
    return result.ToString();
}

static string GetTypeSortKey(TypeInfo t)
{
    int kindOrder = t.Kind switch
    {
        "enum" => 1,
        "delegate" => 2,
        "interface" => 3,
        "struct" or "record struct" or "readonly struct" => 4,
        _ => 5 // class, record class
    };
    return $"{kindOrder}_{t.Identifier}";
}

#endregion

#region Data classes

class TypeInfo
{
    public string Namespace = "";
    public string Identifier = "";
    public string Kind = "class";
    public bool IsInterface;
    public bool IsPrivateImplDetail;
    public bool IsProjectedType;
    public string? TypeParameterListText;
    public HashSet<string> Modifiers = new();
    public List<string> AttributeTexts = new();
    public List<string> BaseTypes = new();
    public List<string> Constraints = new();
    public List<MemberEntry> Members = new();
    public HashSet<string> MemberDecls = new(); // For deduplication
    public List<TypeInfo> NestedTypes = new();

    // Enum-specific
    public string? EnumBaseType;
    public List<string> EnumMembers = new();

    // Delegate-specific
    public string? DelegateReturnType;
    public string? DelegateParameterList;
}

class MemberEntry
{
    public List<string> Attributes = new();
    public string Declaration = "";
    public bool IsPrivateImplDetail;
    public string SortKey = "";
}

#endregion

partial class Regexes
{
    [GeneratedRegex(@"\bHRESULT\b")]
    public static partial Regex HResultAlias { get; }
}
