#include "pch.h"
#include "settings.h"
#include "strings.h"
#include "helpers.h"
#include "type_writers.h"
#include "code_writers.h"
#include <concurrent_unordered_map.h>
#include <concurrent_unordered_set.h>

namespace cswinrt
{
    using namespace std::literals;
    using namespace std::experimental::filesystem;
    using namespace winmd::reader;

    inline auto get_start_time()
    {
        return std::chrono::high_resolution_clock::now();
    }

    inline auto get_elapsed_time(std::chrono::time_point<std::chrono::high_resolution_clock> const& start)
    {
        return std::chrono::duration_cast<std::chrono::duration<int64_t, std::milli>>(std::chrono::high_resolution_clock::now() - start).count();
    }

    settings_type settings;

    struct usage_exception {};

    static constexpr option options[]
    {
        { "input", 0, option::no_max, "<spec>", "Windows metadata to include in projection" },
        { "output", 0, 1, "<path>", "Location of generated projection" },
        { "include", 0, option::no_max, "<prefix>", "One or more prefixes to include in projection" },
        { "exclude", 0, option::no_max, "<prefix>", "One or more prefixes to exclude from projection" },
        { "addition_exclude", 0, option::no_max, "<prefix>", "One or more namespace prefixes to exclude from the projection additions" },
        { "target", 0, 1, "<net8.0|net7.0|net6.0|netstandard2.0>", "Target TFM for projection. Omit for compatibility with .NET 6." },
        { "component", 0, 0, {}, "Generate component projection." },
        { "verbose", 0, 0, {}, "Show detailed progress information" },
        { "internal", 0, 0, {}, "Generates a private projection."},
        { "embedded", 0, 0, {}, "Generates an embedded projection."},
        { "public_enums", 0, 0, {}, "Used with embedded option to generate enums as public"},
        { "public_exclusiveto", 0, 0, {}, "Make exclusiveto interfaces public in the projection (default is internal)"},
        { "abstract_class", 0, 0, {}, "Enable an abstract version of a component's class"},
        { "idic_exclusiveto", 0, 0, {}, "Make exclusiveto interfaces support IDynamicInterfaceCastable (IDIC) for RCW scenarios (default is false)"},
        { "help", 0, option::no_max, {}, "Show detailed help" },
        { "?", 0, option::no_max, {}, {} },
    };

    static void print_usage(writer& w)
    {
        static auto printColumns = [](writer& w, std::string_view const& col1, std::string_view const& col2)
        {
            w.write_printf("  %-35s%s\n", col1.data(), col2.data());
        };

        static auto printOption = [](writer& w, option const& opt)
        {
            if(opt.desc.empty())
            {
                return;
            }
            printColumns(w, w.write_temp("-% %", opt.name, opt.arg), opt.desc);
        };

        auto format = R"(
C#/WinRT v%
Copyright (c) Microsoft Corporation. All rights reserved.

  cswinrt.exe [options...]

Options:
     
%  ^@<path>                            Response file containing command line options

Where <spec> is one or more of:

  path                               Path to winmd file or recursively scanned folder
  local                              Local ^%WinDir^%\System32\WinMetadata folder
  sdk[+]                             Current version of Windows SDK [with extensions]
  10.0.12345.0[+]                    Specific version of Windows SDK [with extensions]
)";
        w.write(format, VERSION_STRING, bind_each(printOption, options));
    }

    void process_args(int const argc, char** argv)
    {
        reader args{ argc, argv, options };

        if (!args || args.exists("help") || args.exists("?"))
        {
            throw usage_exception{};
        }


        settings.verbose = args.exists("verbose");
        auto target = args.value("target");
        if (!target.empty() && target != "netstandard2.0" && !starts_with(target, "net5.0") && !starts_with(target, "net6.0") && !starts_with(target, "net7.0") && !starts_with(target, "net8.0"))
        {
            throw usage_exception();
        }
        settings.netstandard_compat = target == "netstandard2.0";
        settings.net7_0_or_greater = starts_with(target, "net7.0") || starts_with(target, "net8.0");
        settings.component = args.exists("component");
        settings.internal = args.exists("internal");
        settings.embedded = args.exists("embedded");
        settings.public_enums = args.exists("public_enums");
        settings.public_exclusiveto = args.exists("public_exclusiveto");
        settings.abstract_class = args.exists("abstract_class");
        settings.idic_exclusiveto = args.exists("idic_exclusiveto");
        settings.input = args.files("input", database::is_database);
        
		printf("abstract_class value: %d\n", settings.abstract_class); //TODO: remove this line later, it's just for testing

        for (auto && include : args.values("include"))
        {
            settings.include.insert(include);
        }

        for (auto && exclude : args.values("exclude"))
        {
            settings.exclude.insert(exclude);
        }

        for (auto&& addition_exclude : args.values("addition_exclude"))
        {
            settings.addition_exclude.insert(addition_exclude);
        }

        settings.output_folder = std::filesystem::absolute(args.value("output", "output"));
        create_directories(settings.output_folder);
    }

    auto get_files_to_cache()
    {
        std::vector<std::string> files;
        files.insert(files.end(), settings.input.begin(), settings.input.end());
        return files;
    }

    int run(int const argc, char** argv)
    {
        int result{};
        writer w;

        /* Special case the usage exceptions to print CLI options */
        try
        {
            auto start = get_start_time();
            process_args(argc, argv);
            cache c{ get_files_to_cache() };
            settings.filter = { settings.include, settings.exclude };

            // Include all additions for included namespaces by default
            settings.addition_filter = { settings.include, settings.addition_exclude };

            std::set<TypeDef> componentActivatableClasses;
            if (settings.component)
            {
                for (auto&& [ns, members] : c.namespaces())
                {
                    for (auto&& type : members.classes)
                    {
                        if (!settings.filter.includes(type)) { continue; }
                        for (auto&& attribute : type.CustomAttribute())
                        {
                            auto attribute_name = attribute.TypeNamespaceAndName();
                            if (attribute_name.first != "Windows.Foundation.Metadata")
                            {
                                continue;
                            }

                            if (attribute_name.second == "ActivatableAttribute" || attribute_name.second == "StaticAttribute")
                            {
                                componentActivatableClasses.insert(type);
                            }
                        }
                    }
                }
            }

            if (settings.verbose)
            {
                for (auto&& file : settings.input)
                {
                    w.write("input: %\n", file);
                }

                w.write("output: %\n", settings.output_folder.string());
            }

            w.flush_to_console();

            task_group group;

            concurrency::concurrent_unordered_map<std::string, std::string> typeNameToEventDefinitionMap, typeNameToBaseTypeMap, authoredTypeNameToMetadataTypeNameMap;
            concurrency::concurrent_unordered_set<generic_abi_delegate> abiDelegateEntries;
            bool projectionFileWritten = false;
            for (auto&& ns_members : c.namespaces())
            {
                group.add([&ns_members, &componentActivatableClasses, &projectionFileWritten, &typeNameToEventDefinitionMap, &typeNameToBaseTypeMap, &abiDelegateEntries, &authoredTypeNameToMetadataTypeNameMap]
                {
                    auto&& [ns, members] = ns_members;
                    std::string_view currentType = "";
                    try
                    {
                        writer w(ns);
                        writer helperWriter("WinRT");
                        
                        w.write_begin();
                        bool written = false;
                        bool requires_abi = false;
                        for (auto&& [name, type] : members.types)
                        {
                            currentType = name;
                            if (!settings.filter.includes(type)) { continue; }
                            if (get_mapped_type(ns, name))
                            {
                                written = true;
                                continue;
                            }
                            auto guard{ w.push_generic_params(type.GenericParam()) };
                            auto guard1{ helperWriter.push_generic_params(type.GenericParam()) };

                            bool type_requires_abi = true;
                            switch (get_category(type))
                            {
                            case category::class_type:
                                if (is_attribute_type(type))
                                {
                                    write_attribute(w, type);
                                }
                                else
                                {
                                    if (settings.netstandard_compat)
                                    {
                                        write_class_netstandard(w, type);
                                    }
                                    else
                                    {
                                        write_class(w, type);
                                        add_base_type_entry(type, typeNameToBaseTypeMap);
                                        add_metadata_type_entry(type, authoredTypeNameToMetadataTypeNameMap);
                                    }
                                    if (settings.component && componentActivatableClasses.count(type) == 1)
                                    {
                                        write_factory_class(w, type);
                                    }
                                }

                                write_temp_class_event_source_subclass(helperWriter, type, typeNameToEventDefinitionMap);
                                break;
                            case category::delegate_type:
                                write_delegate(w, type);
                                add_metadata_type_entry(type, authoredTypeNameToMetadataTypeNameMap);
                                break;
                            case category::enum_type:
                                write_enum(w, type);
                                add_metadata_type_entry(type, authoredTypeNameToMetadataTypeNameMap);
                                type_requires_abi = false;
                                break;
                            case category::interface_type:
                                write_interface(w, type);
                                write_temp_interface_event_source_subclass(helperWriter, type, typeNameToEventDefinitionMap);
                                add_metadata_type_entry(type, authoredTypeNameToMetadataTypeNameMap);
                                break;
                            case category::struct_type:
                                if (is_api_contract_type(type))
                                {
                                    write_contract(w, type);
                                }
                                else
                                {
                                    write_struct(w, type);
                                    add_metadata_type_entry(type, authoredTypeNameToMetadataTypeNameMap);
                                    type_requires_abi = !is_type_blittable(type);
                                }
                                break;
                            }

                            add_generic_type_references_in_type(type, abiDelegateEntries);
                            written = true;
                            requires_abi = requires_abi || type_requires_abi;
                        }
                        currentType = "";
                        if (written)
                        {
                            w.write_end();
                            if (requires_abi)
                            {
                                w.write_begin_abi();
                                for (auto&& [name, type] : members.types)
                                {
                                    currentType = name;
                                    if (!settings.filter.includes(type)) { continue; }
                                    if (get_mapped_type(ns, name)) continue;
                                    if (is_api_contract_type(type)) { continue; }
                                    if (is_attribute_type(type)) { continue; }
                                    auto guard{ w.push_generic_params(type.GenericParam()) };

                                    switch (get_category(type))
                                    {
                                    case category::class_type:
                                        write_abi_class(w, type);
                                        if (settings.component && componentActivatableClasses.count(type) == 1)
                                        {
                                            write_winrt_exposed_type_class(w, type, true);
                                        }
                                        write_winrt_implementation_type_rcw_factory_attribute_type(w, type);
                                        break;
                                    case category::delegate_type:
                                        write_abi_delegate(w, type);
                                        write_winrt_exposed_type_class(w, type, false);
                                        break;
                                    case category::interface_type:
                                        if (settings.netstandard_compat)
                                        {
                                            write_static_abi_classes(w, type);
                                            write_abi_interface_netstandard(w, type);
                                        }
                                        else
                                        {
                                            write_static_abi_classes(w, type);
                                            write_abi_interface(w, type);
                                        }
                                        break;
                                    case category::struct_type:
                                        if (!is_type_blittable(type))
                                        {
                                            write_abi_struct(w, type);
                                        }
                                        break;
                                    }
                                }
                                w.write_end_abi();
                            }
                            currentType = "";

                            // Custom additions to namespaces
                            for (auto addition : strings::additions)
                            {
                                if (ns == addition.name && settings.addition_filter.includes(ns))
                                {
                                    w.write(addition.value);
                                }
                            }

                            auto filename = w.write_temp("%.cs", ns);
                            w.flush_to_file(settings.output_folder / filename);
                            projectionFileWritten = true;
                        }
                    }
                    catch (std::exception const& e)
                    {
                        writer console;
                        console.write("error: '%' when processing %%%\n", e.what(), ns, currentType.empty() ? "" : ".", currentType);
                        console.flush_to_console_error();
                        throw;
                    }
                });
            }
            
            if(settings.component)
            {
                group.add([&componentActivatableClasses, &projectionFileWritten]
                {
                    writer wm;
                    write_file_header(wm);
                    write_module_activation_factory(wm, componentActivatableClasses);
                    wm.flush_to_file(settings.output_folder / (std::string("WinRT_Module") + ".cs"));
                    projectionFileWritten = true;
                });
            }

            group.get();

            writer eventHelperWriter("WinRT");
            write_file_header(eventHelperWriter);
            eventHelperWriter.write("using System;\nnamespace WinRT\n{\n%\n}", bind([&](writer& w) {
                for (auto&& [key, value] : typeNameToEventDefinitionMap)
                {
                    w.write("%", value);
                }
            }));
            eventHelperWriter.flush_to_file(settings.output_folder / "WinRTEventHelpers.cs");

            if (!typeNameToBaseTypeMap.empty())
            {
                writer baseTypeWriter("WinRT");
                write_file_header(baseTypeWriter);
                baseTypeWriter.write(R"(namespace WinRT
{
internal static class ProjectionTypesInitializer
{
internal static readonly System.Collections.Generic.Dictionary<string, string> TypeNameToBaseTypeNameMapping = new System.Collections.Generic.Dictionary<string, string>(%, System.StringComparer.Ordinal)
{
%
};

[System.Runtime.CompilerServices.ModuleInitializer]
internal static void InitalizeProjectionTypes()
{
ComWrappersSupport.RegisterProjectionTypeBaseTypeMapping(TypeNameToBaseTypeNameMapping);
}
}
})",
typeNameToBaseTypeMap.size(),
bind([&](writer& w) {
                        for (auto&& [key, value] : typeNameToBaseTypeMap)
                        {
                            w.write(R"(["%"] = "%",)", key, value);
                            w.write("\n");
                        }
    }));
                baseTypeWriter.flush_to_file(settings.output_folder / "WinRTBaseTypeMappingHelper.cs");
            }

            if (!abiDelegateEntries.empty() && settings.netstandard_compat)
            {
                writer baseTypeWriter("WinRT");
                write_file_header(baseTypeWriter);
                baseTypeWriter.write(R"(
using System;

namespace WinRT
{
internal static class AbiDelegatesInitializer
{

[System.Runtime.CompilerServices.ModuleInitializer]
internal static void InitalizeAbiDelegates()
{
%
}

%
}
})",
                bind([&](writer& w) {
                    for (auto&& entry : abiDelegateEntries)
                    {
                        w.write("Projections.RegisterAbiDelegate(%, typeof(%));\n", entry.abi_delegate_types, entry.abi_delegate_name);
                    }

                    if (settings.filter.includes("Windows.Foundation.AsyncStatus"))
                    {
                        w.write("Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(IntPtr), typeof(global::Windows.Foundation.AsyncStatus), typeof(int) }, typeof(_invoke_IntPtr_AsyncStatus));\n");
                    }
                }),
                bind([&](writer& w) {
                    for (auto&& entry : abiDelegateEntries)
                    {
                        w.write("%\n", entry.abi_delegate_declaration);
                    }

                    if (settings.filter.includes("Windows.Foundation.AsyncStatus"))
                    {
                        w.write("internal unsafe delegate int _invoke_IntPtr_AsyncStatus(void* thisPtr, IntPtr asyncInfo, global::Windows.Foundation.AsyncStatus asyncStatus);\n");
                    }
                }));
                baseTypeWriter.flush_to_file(settings.output_folder / "WinRTAbiDelegateInitializer.cs");
            }

            if (!settings.netstandard_compat && has_generic_type_instantiations())
            {
                writer genericTypeInstantiationWriter("WinRT.GenericTypeInstantiations");
                write_file_header(genericTypeInstantiationWriter);
                genericTypeInstantiationWriter.write(R"(
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinRT.GenericTypeInstantiations
{
%
})",
                bind<write_generic_type_instantiations>());
                genericTypeInstantiationWriter.flush_to_file(settings.output_folder / "WinRTGenericTypeInstantiations.cs");
            }

            if (!authoredTypeNameToMetadataTypeNameMap.empty() && settings.component)
            {
                writer metadataMappingTypeWriter("WinRT");
                write_file_header(metadataMappingTypeWriter);
                metadataMappingTypeWriter.write(R"(
using System;

namespace WinRT
{
internal static class AuthoringMetadataTypeInitializer
{

private static Type GetMetadataTypeMapping(Type type)
{
return type switch
{
%
_ => null
};
}

[System.Runtime.CompilerServices.ModuleInitializer]
internal static void InitializeAuthoringTypeMapping()
{
ComWrappersSupport.RegisterAuthoringMetadataTypeLookup(new Func<Type, Type>(GetMetadataTypeMapping));
}
}
})",
                bind([&](writer& w) {
                        for (auto&& [key, value] : authoredTypeNameToMetadataTypeNameMap)
                        {
                            w.write(R"(Type _ when type == typeof(%) => typeof(%),)", key, value);
                            w.write("\n");
                        }
                }));
            metadataMappingTypeWriter.flush_to_file(settings.output_folder / "AuthoringMetadataTypeMappingHelper.cs");
        }

            if (projectionFileWritten)
            {
                for (auto&& string : strings::base)
                {
                    if (std::string(string.name) == "ComInteropHelpers" && !settings.filter.includes("Windows"))
                    {
                        continue;
                    }
                    writer ws;
                    write_file_header(ws);
                    ws.write(string.value);
                    ws.flush_to_file(settings.output_folder / (std::string(string.name) + ".cs"));
                }
            }

            if (settings.verbose)
            {
                w.write("time: %ms\n", get_elapsed_time(start));
            }
        }
        catch (usage_exception const&)
        {
            result = 1;
            print_usage(w);
        }
        catch (std::exception const& e)
        {
            w.write(" error: %\n", e.what());
            result = 1;
            w.flush_to_console_error();
            return result;
        }

        w.flush_to_console();
        return result;
    }
}

int main(int const argc, char** argv)
{
    return cswinrt::run(argc, argv);
}
