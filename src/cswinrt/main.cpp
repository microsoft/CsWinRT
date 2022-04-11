#include "pch.h"
#include "settings.h"
#include "strings.h"
#include "helpers.h"
#include "type_writers.h"
#include "code_writers.h"
#include <concurrent_unordered_map.h>

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
        { "target", 0, 1, "<net6.0|net5.0|netstandard2.0>", "Target TFM for projection. Omit for compatibility with newest TFM (net5.0)." },
        { "component", 0, 0, {}, "Generate component projection." },
        { "verbose", 0, 0, {}, "Show detailed progress information" },
        { "internal", 0, 0, {}, "Generate the projection as internal."},
        { "embedded", 0, 0, {}, "Generate the projection as internal."},
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
        if (!target.empty() && target != "netstandard2.0" && !starts_with(target, "net5.0") && !starts_with(target, "net6.0"))
        {
            throw usage_exception();
        }
        settings.netstandard_compat = target == "netstandard2.0";
        settings.component = args.exists("component");
        settings.internal = args.exists("internal");
        settings.embedded = args.exists("embedded");
        settings.input = args.files("input", database::is_database);

        for (auto && include : args.values("include"))
        {
            settings.include.insert(include);
        }

        for (auto && exclude : args.values("exclude"))
        {
            settings.exclude.insert(exclude);
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

            concurrency::concurrent_unordered_map<std::string, std::string> typeNameToDefinitionMap;
            bool projectionFileWritten = false;
            for (auto&& ns_members : c.namespaces())
            {
                group.add([&ns_members, &componentActivatableClasses, &projectionFileWritten, &typeNameToDefinitionMap]
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
                                    }
                                    if (settings.component && componentActivatableClasses.count(type) == 1)
                                    {
                                        write_factory_class(w, type);
                                    }
                                }
                                write_temp_class_event_source_subclass(helperWriter, type, typeNameToDefinitionMap);
                                break;
                            case category::delegate_type:
                                write_delegate(w, type);
                                break;
                            case category::enum_type:
                                write_enum(w, type);
                                type_requires_abi = false;
                                break;
                            case category::interface_type:
                                write_interface(w, type);
                                write_temp_interface_event_source_subclass(helperWriter, type, typeNameToDefinitionMap);
                                break;
                            case category::struct_type:
                                if (is_api_contract_type(type))
                                {
                                    write_contract(w, type);
                                }
                                else
                                {
                                    write_struct(w, type);
                                    type_requires_abi = !is_type_blittable(type);
                                }
                                break;
                            }

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
                                        break;
                                    case category::delegate_type:
                                        write_abi_delegate(w, type);
                                        break;
                                    case category::interface_type:
                                        if (settings.netstandard_compat)
                                        {
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
                                if (ns == addition.name)
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
                        console.flush_to_console();
                        throw;
                    }
                });
            }
            
            if(settings.component)
            {
                group.add([&componentActivatableClasses, &projectionFileWritten]
                {
                    writer wm;
                    wm.write(R"(//------------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by cswinrt.exe version %
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
)", VERSION_STRING);
                    write_module_activation_factory(wm, componentActivatableClasses);
                    wm.flush_to_file(settings.output_folder / (std::string("WinRT_Module") + ".cs"));
                    projectionFileWritten = true;
                });
            }

            group.get();
            writer eventHelperWriter("WinRT");
            eventHelperWriter.write(R"(//------------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by cswinrt.exe version %
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
)", VERSION_STRING);
            eventHelperWriter.write("namespace WinRT\n{\n%\n}", bind([&](writer& w) {
                for (auto&& [key, value] : typeNameToDefinitionMap)
                {
                    w.write("%", value);
                }
            }));
            eventHelperWriter.flush_to_file(settings.output_folder / "WinRTEventHelpers.cs");

            if (projectionFileWritten)
            {
                for (auto&& string : strings::base)
                {
                    if (std::string(string.name) == "ComInteropHelpers" && !settings.filter.includes("Windows"))
                    {
                        continue;
                    }
                    writer ws;
                    ws.write(R"(//------------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by cswinrt.exe version %
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
)", VERSION_STRING);
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
        }

        w.flush_to_console();
        return result;
    }
}

int main(int const argc, char** argv)
{
    return cswinrt::run(argc, argv);
}