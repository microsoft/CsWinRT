#pragma once

#include <functional>
#include <set>
#include <filesystem>
#include <iostream>
#include <regex>
#include <concurrent_unordered_map.h>
#include <concurrent_unordered_set.h>

#define INSPECTABLE_METHOD_COUNT 6

namespace cswinrt
{
    using namespace winmd::reader;

    static const struct
    {
        char const* csharp;
        char const* dotnet;
    }
    type_mappings[] =
    {
        {"bool", "Boolean"},
        {"char", "Char"},
        {"sbyte", "SByte"},
        {"byte", "Byte"},
        {"short", "Int16"},
        {"ushort", "UInt16"},
        {"int", "Int32"},
        {"uint", "UInt32"},
        {"long", "Int64"},
        {"ulong", "UInt64"},
        {"float", "Float"},
        {"double", "Double"},
        {"string", "String"},
    };

    static concurrency::concurrent_unordered_set<generic_type_instantiation> generic_type_instances;
    generic_type_instance ConvertGenericTypeInstanceToConcreteType(writer& w, const generic_type_instance& generic_instance);

    auto to_csharp_type(fundamental_type type)
    {
        return type_mappings[(int)type].csharp;
    }

    auto to_dotnet_type(fundamental_type type)
    {
        return type_mappings[(int)type].dotnet;
    }

    auto get_delegate_type_suffix(fundamental_type type)
    {
        if (type == fundamental_type::String)
        {
            return "String";
        }
        return type_mappings[(int)type].dotnet;
    }

    static uint32_t get_vmethod_index(TypeDef const& type, MethodDef const& method)
    {
        uint32_t const vtable_base = type.MethodList().first.index();
        uint32_t const vtable_index = method.index() - vtable_base;
        return vtable_index;
    }

    static std::string get_vmethod_name(writer& w, TypeDef const& type, MethodDef const& method)
    {
        return w.write_temp("%_%", method.Name(), get_vmethod_index(type, method));
    }

    std::string internal_accessibility() 
    {
        return (settings.internal || settings.embedded) ? "internal" : "public";
    }

    bool is_type_blittable(type_semantics const& semantics, bool for_array = false)
    {
        return call(semantics,
            [&](object_type)
            {
                return false;
            },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                    case category::enum_type:
                        return !for_array;
                    case category::struct_type:
                        if (auto mapping = get_mapped_type(type.TypeNamespace(), type.TypeName()))
                        {
                            return !mapping->requires_marshaling;
                        }

                        for (auto&& field : type.FieldList())
                        {
                            if (!is_type_blittable(get_type_semantics(field.Signature().Type())))
                            {
                                return false;
                            }
                        }
                        return true;
                    default:
                        return false;
                }
            },
            [&](generic_type_instance const& /*type*/)
            {
                return false;
            },
            [&](fundamental_type const& type)
            {
                return (type != fundamental_type::String) && 
                    (type != fundamental_type::Char) &&
                    (type != fundamental_type::Boolean);
            },
            [&](auto&&)
            {
                return true;
            });
    }

    bool is_value_type(type_semantics const& semantics)
    {
        return call(semantics,
            [&](object_type)
            {
                return false;
            },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                    case category::enum_type:
                        return true;
                    case category::struct_type:
                        if (auto mapping = get_mapped_type(type.TypeNamespace(), type.TypeName()))
                        {
                            return true;
                        }

                        for (auto&& field : type.FieldList())
                        {
                            if (!is_value_type(get_type_semantics(field.Signature().Type())))
                            {
                                return false;
                            }
                        }
                        return true;
                    default:
                        return false;
                }
            },
            [&](generic_type_instance const& /*type*/)
            {
                return false;
            },
            [&](fundamental_type const& type)
            {
                return (type != fundamental_type::String);
            },
            [&](auto&&)
            {
                return true;
            });
    }

    // This checks for interfaces that have derived generic interfaces
    // to handle the scenario where the class implementing that interface
    // can be trimmed and we need to handle potential calls to the
    // generic interface methods while not using IDIC.
    bool has_derived_generic_interface(TypeDef const& type)
    {
        if (is_exclusive_to(type))
        {
            return false;
        }

        bool found = false;

        // Looking at derived interfaces and not the interface / type itself.
        auto impls = type.InterfaceImpl();
        for (auto&& impl : impls)
        {
            call(get_type_semantics(impl.Interface()),
                [&](type_definition const& type)
                {
                    found |= has_derived_generic_interface(type);
                },
                [&](generic_type_instance const&)
                {
                    found = true;
                },
                [](auto)
                {
                });

            if (found)
            {
                return true;
            }
        }

        return false;
    }

    void write_fundamental_type(writer& w, fundamental_type type)
    {
        w.write(to_csharp_type(type));
    }

    void write_fundamental_non_projected_type(writer& w, fundamental_type type)
    {
        w.write(to_dotnet_type(type));
    }

    void write_projection_type(writer& w, type_semantics const& semantics);
    void write_projection_type_for_name_type(writer& w, type_semantics const& semantics, typedef_name_type const& nameType);
    void write_guid(writer& w, TypeDef const& type, bool lowerCase);

    void write_generic_type_name_base(writer& w, uint32_t index)
    {
        write_projection_type(w, w.get_generic_arg_scope(index).first);
    }

    void write_generic_type_name(writer& w, uint32_t index)
    {
        w.write_generic_type_name_custom ?
            w.write_generic_type_name_custom(w, index) :
            write_generic_type_name_base(w, index);
    }

    template<typename TAction, typename TResult = std::invoke_result_t<TAction, type_definition>>
    TResult for_typedef(writer& w, type_semantics const& semantics, TAction action)
    {
        return call(semantics,
            [&](type_definition const& type)
            {
                return action(type);
            },
            [&](generic_type_instance const& type)
            {
                auto guard{ w.push_generic_args(type) };
                return action(type.generic_type);
            },
            #pragma warning(disable:4702)
            [](auto)
            {
                throw_invalid("type definition expected");
                return TResult();
            });
    }

    void write_typedef_name(writer& w, type_definition const& type, typedef_name_type const& nameType = typedef_name_type::Projected, bool forceWriteNamespace = false)
    {
        bool authoredType = settings.component && settings.filter.includes(type);
        auto typeNamespace = type.TypeNamespace();
        auto typeName = type.TypeName();

        if (nameType == typedef_name_type::NonProjected)
        {
            w.write("%.%", typeNamespace, typeName);
            return;
        }

        if (auto proj = get_mapped_type(typeNamespace, typeName))
        {
            typeNamespace = proj->mapped_namespace;
            typeName = proj->mapped_name;
        }

        // Exclusive interfaces for authored types only exist in the CCW impl namepsace.
        // For the default interface, if the projected name is requested, use the class type.
        // For other exclusive interfaces, if the projected name is requested, use the CCW type.
        bool use_exclusive_to_type = false;
        TypeDef exclusive_to_type;
        typedef_name_type name_type_to_write = nameType;
        if (authoredType && is_exclusive_to(type) && name_type_to_write == typedef_name_type::Projected)
        {
            auto exclusive_to_attr = get_attribute(type, "Windows.Foundation.Metadata", "ExclusiveToAttribute");
            auto sig = exclusive_to_attr.Value();
            auto const& fixed_args = sig.FixedArgs();
            XLANG_ASSERT(fixed_args.size() == 1);
            auto sys_type = std::get<ElemSig::SystemType>(std::get<ElemSig>(fixed_args[0].value).value);
            exclusive_to_type = type.get_cache().find_required(sys_type.name);

            try
            {
                if (auto default_interface = get_default_interface(exclusive_to_type))
                {
                    for_typedef(w, get_type_semantics(default_interface), [&](auto&& interface_type)
                        {
                            use_exclusive_to_type = (type == interface_type);
                        });
                }
            }
            catch (const std::invalid_argument&)
            {
            }

            if (!use_exclusive_to_type)
            {
                name_type_to_write = typedef_name_type::CCW;
            }
        }

        if (forceWriteNamespace || 
            (typeNamespace != w._current_namespace) ||
            (name_type_to_write == typedef_name_type::Projected && (w._in_abi_namespace || w._in_abi_impl_namespace)) ||
            (name_type_to_write == typedef_name_type::ABI && !w._in_abi_namespace) ||
            (name_type_to_write == typedef_name_type::CCW && authoredType && !w._in_abi_impl_namespace) ||
            (name_type_to_write == typedef_name_type::CCW && !authoredType && (w._in_abi_namespace || w._in_abi_impl_namespace)))
        {
            w.write("global::");
            if (name_type_to_write == typedef_name_type::ABI || name_type_to_write == typedef_name_type::StaticAbiClass)
            {
                w.write("ABI.");
            }
            else if (authoredType && name_type_to_write == typedef_name_type::CCW)
            {
                w.write("ABI.Impl.");
            }

            w.write("%.", typeNamespace);
        }

        if (use_exclusive_to_type)
        {
            w.write("@", exclusive_to_type.TypeName());
        }
        else if (name_type_to_write == typedef_name_type::StaticAbiClass)
        {
            w.write("@%", typeName, "Methods");
        }
        else
        {
            w.write("@", typeName);
        }
    }

    void write_type_params(writer& w, TypeDef const& type)
    {
        if (distance(type.GenericParam()) == 0)
        {
            return;
        }
        separator s{ w };
        uint32_t index = 0;
        w.write("<%>", bind_each([&](writer& w, GenericParam const& /*gp*/)
            { s(); write_generic_type_name(w, index++); }, type.GenericParam()));
    }

    void write_type_name(writer& w, type_semantics const& semantics, typedef_name_type const& nameType = typedef_name_type::Projected, bool forceWriteNamespace = false)
    {
        for_typedef(w, semantics, [&](auto type)
        {
            write_typedef_name(w, type, nameType, forceWriteNamespace);
            write_type_params(w, type);
        });
    }

    auto write_type_name_temp(writer& w, type_semantics const& type, char const* format = "%", typedef_name_type const& nameType = typedef_name_type::Projected)
    {
        return w.write_temp(format, bind<write_type_name>(type, nameType, false));
    }

    void write_static_abi_class_generic_instantiation_type(writer& w, type_semantics const& semantics)
    {
        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
        {
            write_projection_type_for_name_type(w, w.get_generic_arg_scope(index).first, typedef_name_type::Projected);
            w.write(", ");
            write_projection_type_for_name_type(w, w.get_generic_arg_scope(index).first, typedef_name_type::Projected);
            w.write("Abi");
        });

        w.write("%", bind<write_type_name>(semantics, typedef_name_type::StaticAbiClass, false));
    }

    void write_projection_type_for_name_type(writer& w, type_semantics const& semantics, typedef_name_type const& nameType)
    {
        call(semantics,
            [&](object_type) { w.write("object"); },
            [&](guid_type) { w.write("Guid"); },
            [&](type_type) { w.write("Type"); },
            [&](type_definition const& type) { write_typedef_name(w, type, nameType); },
            [&](generic_type_index const& var) { write_generic_type_name(w, var.index); },
            [&](generic_type_instance const& type)
            {
                auto guard{ w.push_generic_args(type) };
                w.write("%<%>",
                    bind<write_projection_type_for_name_type>(type.generic_type, nameType),
                    bind_list<write_projection_type_for_name_type>(", ", type.generic_args, nameType));
            },
            [&](generic_type_param const& param) { w.write(param.Name()); },
            [&](fundamental_type const& type)
            { 
                if (nameType == typedef_name_type::NonProjected)
                {
                    write_fundamental_non_projected_type(w, type);
                }
                else
                {
                    write_fundamental_type(w, type);
                }
            });
    }

    void write_projection_type(writer& w, type_semantics const& semantics)
    {
        write_projection_type_for_name_type(w, semantics, typedef_name_type::Projected);
    }

    void write_projection_ccw_type(writer& w, type_semantics const& semantics)
    {
        write_projection_type_for_name_type(w, semantics, typedef_name_type::CCW);
    }

    std::string get_generic_instantiation_class_type_name(writer& w, type_definition const& type)
    {
        separator s{ w };
        uint32_t index = 0;
        auto generic_instantiation_class_name = escape_type_name_for_identifier(w.write_temp(
            "%_%",
            bind<write_typedef_name>(type, typedef_name_type::NonProjected, true),
            bind_each([&](writer& w, GenericParam const& /*gp*/)
            {
                s();
                write_projection_type_for_name_type(w, w.get_generic_arg(index++), typedef_name_type::NonProjected);
            }, type.GenericParam())));
        return generic_instantiation_class_name;
    }

    bool is_keyword(std::string_view str)
    {
        static constexpr std::string_view keywords[] =
        {
            "abstract",  "as",       "base",     "bool",       "break",     "byte",
            "case",      "catch",    "char",     "checked",    "class",     "const",
            "continue",  "decimal",  "default",  "delegate",   "do",        "double",
            "else",      "enum",     "event",    "explicit",   "extern",    "false",
            "finally",   "fixed",    "float",    "for",        "foreach",   "goto",
            "if",        "implicit", "in",       "int",        "interface", "internal",
            "is",        "lock",     "long",     "namespace",  "new",       "null",
            "object",    "operator", "out",      "override",   "params",    "private",
            "protected", "public",   "readonly", "ref",        "return",    "sbyte",
            "sealed",    "short",    "sizeof",   "stackalloc", "static",    "string",
            "struct",    "switch",   "this",     "throw",      "true",      "try",
            "typeof",    "uint",     "ulong",    "unchecked",  "unsafe",    "ushort",
            "using",     "virtual",  "void",     "volatile",   "while"
        };
#if 0
        assert(std::is_sorted(std::begin(keywords), std::end(keywords)));
#endif
        return std::binary_search(std::begin(keywords), std::end(keywords), str);
    }

    void write_escaped_identifier(writer& w, std::string_view identifier)
    {
        if (is_keyword(identifier))
        {
            w.write("@");
        }
        w.write(identifier);
    }

    void write_parameter_name(writer& w, method_signature::param_t const& param)
    {
        write_escaped_identifier(w, param.first.Name());
    }

    void write_parameter_name_with_modifier(writer& w, method_signature::param_t const& param)
    {
        switch (get_param_category(param))
        {
        case param_category::ref:
            w.write("in ");
            break;
        case param_category::out:
        case param_category::receive_array:
            w.write("out ");
            break;
        default:
            break;
        }
        write_parameter_name(w, param);
    }

    void write_projection_parameter_type(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::in:
            w.write("%", bind<write_projection_type>(semantics));
            break;
        case param_category::ref:
            w.write("in %", bind<write_projection_type>(semantics));
            break;
        case param_category::out:
            w.write("out %", bind<write_projection_type>(semantics));
            break;
        case param_category::pass_array:
        case param_category::fill_array:
            w.write("%[]", bind<write_projection_type>(semantics));
            break;
        case param_category::receive_array:
            w.write("out %[]", bind<write_projection_type>(semantics));
            break;
        }
    }

    void write_projected_signature(writer& w, TypeSig const& type_sig)
    {
        write_projection_type(w, get_type_semantics(type_sig));
        if(type_sig.is_szarray()) w.write("[]");
    };

    void write_projection_return_type(writer& w, method_signature const& signature)
    {
        if (auto return_sig = signature.return_signature())
        {
            write_projected_signature(w, return_sig.Type());
        }
        else
        {
            w.write("void");
        }
    }

    void write_projection_parameter(writer& w, method_signature::param_t const& param)
    {
        w.write("% %",
            bind<write_projection_parameter_type>(param),
            bind<write_parameter_name>(param));
    }

    void write_event_source_type_name(writer& w, type_semantics const& eventTypeSemantics)
    {
        auto eventTypeCode = w.write_temp("%", bind<write_type_name>(eventTypeSemantics, typedef_name_type::Projected, false));
        std::string eventTypeName = "_EventSource_" + eventTypeCode;
        w.write("%", escape_type_name_for_identifier(eventTypeName));
    }

    MethodDef get_event_invoke_method(TypeDef const& eventType)
    {
        for (auto&& method : eventType.MethodList())
        {
            if (method.Name() == "Invoke")
            {
                return method;
            }
        }
        throw_invalid("Event type must have an Invoke method");
    }

    method_signature get_event_invoke_method_signature(TypeDef const& eventType)
    {
        return method_signature(get_event_invoke_method(eventType));
    }

    void write_event_invoke_params(writer& w, method_signature const& methodSig)
    {
        w.write("%", bind_list<write_projection_parameter>(", ", methodSig.params()));
    }

    void write_event_invoke_return(writer& w, method_signature const& methodSig)
    {
        if (methodSig.return_signature())
        {
            w.write("return ");
        }
    }

    void write_event_invoke_return_default(writer& w, method_signature const& methodSig)
    {
        if (!methodSig.return_signature())
        {
            return;
        }
        auto&& semantics = get_type_semantics(methodSig.return_signature().Type());
        w.write("default(%)", bind<write_projection_type>(semantics));
    }

    void write_event_out_defaults(writer& w, method_signature const& methodSig)
    {
        for (auto&& param : methodSig.params())
        {
            if (get_param_category(param) == param_category::out || get_param_category(param) == param_category::receive_array)
            {
                w.write("\n% = default(%);", bind<write_parameter_name>(param), bind<write_projection_type>(get_type_semantics(param.second->Type())));
            }
        }
    }

    void write_event_invoke_args(writer& w, method_signature const& methodSig)
    {
        w.write("%", bind_list<write_parameter_name_with_modifier>(", ", methodSig.params()));
    }

    void write_abi_type(writer& w, type_semantics const& semantics)
    {
        call(semantics,
            [&](object_type) { w.write("IntPtr"); },
            [&](guid_type) { w.write("Guid"); },
            [&](type_type) { throw_invalid("System.Type not implemented"); },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                    case category::enum_type:
                        write_type_name(w, type);
                        break;

                    case category::struct_type:
                        write_type_name(w, type, !is_type_blittable(semantics) ? typedef_name_type::ABI : typedef_name_type::Projected);
                        break;

                    default:
                        w.write("IntPtr");
                        break;
                };
            },
            [&](generic_type_index const& var)
            {
                write_generic_type_name(w, var.index);
            },
            [&](generic_type_instance const&)
            {
                w.write("IntPtr");
            },
            [&](generic_type_param const& param)
            {
                w.write(param.Name());
            },
            [&](fundamental_type type)
            {
                if (type == fundamental_type::String)
                {
                    w.write("IntPtr");
                }
                else
                {
                    if (type == fundamental_type::Boolean)
                    {
                        type = fundamental_type::UInt8;
                    }
                    if (type == fundamental_type::Char)
                    {
                        type = fundamental_type::UInt16;
                    }
                    write_fundamental_type(w, type);
                }
            });
    }

    void write_abi_parameter(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());
        auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
        switch (get_param_category(param))
        {
        case param_category::in:
            w.write(", % %", bind<write_abi_type>(semantics), param_name);
            break;
        case param_category::ref:
            w.write(settings.netstandard_compat ? ", in % %" : ", %* %", bind<write_abi_type>(semantics), param_name);
            break;
        case param_category::out:
            w.write(settings.netstandard_compat ? ", out % %" : ", %* %", bind<write_abi_type>(semantics), param_name);
            break;
        case param_category::pass_array:
        case param_category::fill_array:
            w.write(", int __%Size, IntPtr %", param_name, param_name);
            break;
        case param_category::receive_array:
            w.write(settings.netstandard_compat ? ", out int __%Size, out IntPtr %" : ", int* __%Size, IntPtr* %", param_name, param_name);
            break;
        }
    }
    
    void write_abi_parameter_type(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());
        switch (get_param_category(param))
        {
        case param_category::in:
            w.write(", %", bind<write_abi_type>(semantics));
            break;
        case param_category::ref:
            w.write(", in %", bind<write_abi_type>(semantics));
            break;
        case param_category::out:
            w.write(", out %", bind<write_abi_type>(semantics));
            break;
        case param_category::pass_array:
        case param_category::fill_array:
            w.write(", int, IntPtr");
            break;
        case param_category::receive_array:
            w.write(", out int, out IntPtr");
            break;
        }
    } 

    void write_abi_parameter_type_pointer(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());
        switch (get_param_category(param))
        {
        case param_category::in:
            w.write(", %", bind<write_abi_type>(semantics));
            break;
        case param_category::ref:
            w.write(", %*", bind<write_abi_type>(semantics));
            break;
        case param_category::out:
            w.write(", %*", bind<write_abi_type>(semantics));
            break;
        case param_category::pass_array:
        case param_category::fill_array:
            w.write(", int, IntPtr");
            break;
        case param_category::receive_array:
            w.write(", int*, IntPtr*");
            break;
        }
    }

    void write_abi_return(writer& w, method_signature const& signature)
    {
        if (auto return_sig = signature.return_signature())
        {
            auto semantics = get_type_semantics(return_sig.Type());
            auto return_param = w.write_temp("%", bind<write_escaped_identifier>(signature.return_param_name()));
            if (settings.netstandard_compat)
            {
                return_sig.Type().is_szarray() ?
                    w.write(", out int __%Size, out IntPtr %", signature.return_param_name(), return_param) :
                    w.write(", out % %", bind<write_abi_type>(semantics), return_param);
            }
            else
            {
                return_sig.Type().is_szarray() ?
                    w.write(", int* __%Size, IntPtr* %", signature.return_param_name(), return_param) :
                    w.write(", %* %", bind<write_abi_type>(semantics), return_param);
            }
        }
    }

    void write_abi_return_type(writer& w, method_signature const& signature)
    {
        if (auto return_sig = signature.return_signature())
        {
            auto semantics = get_type_semantics(return_sig.Type());
            return_sig.Type().is_szarray() ?
                w.write(", out int, out IntPtr") :
                w.write(", out %", bind<write_abi_type>(semantics));
        }
    }

    void write_abi_return_type_pointer(writer& w, method_signature const& signature)
    {
        if (auto return_sig = signature.return_signature())
        {
            auto semantics = get_type_semantics(return_sig.Type());
            return_sig.Type().is_szarray() ?
                w.write(", int*, IntPtr*") :
                w.write(", %*", bind<write_abi_type>(semantics));
        }
    }

    void write_abi_parameters(writer& w, method_signature const& signature)
    {
        w.write("void* thisPtr");
        for (auto&& param : signature.params())
        {
            write_abi_parameter(w, param);
        }
        write_abi_return(w, signature);
    }

    void write_abi_parameters_without_return(writer& w, method_signature const& signature)
    {
        w.write("IntPtr thisPtr");
        for (auto&& param : signature.params())
        {
            write_abi_parameter(w, param);
        }
    }

    void write_abi_parameter_types(writer& w, method_signature const& signature)
    {
        w.write("IntPtr");
        for (auto&& param : signature.params())
        {
            write_abi_parameter_type(w, param);
        }
        write_abi_return_type(w, signature);
    }

    void write_abi_parameter_types_without_return(writer& w, method_signature const& signature)
    {
        w.write("IntPtr");
        for (auto&& param : signature.params())
        {
            write_abi_parameter_type(w, param);
        }
    }

    void write_abi_parameter_types_pointer(writer& w, method_signature const& signature)
    {
        w.write("void*");
        for (auto&& param : signature.params())
        {
            write_abi_parameter_type_pointer(w, param);
        }
        write_abi_return_type_pointer(w, signature);
    }

    void write_abi_parameter_types_pointer_without_return(writer& w, method_signature const& signature)
    {
        w.write("void*");
        for (auto&& param : signature.params())
        {
            write_abi_parameter_type_pointer(w, param);
        }
    }

    void write_abi_delegate_parameter_types_pointer(writer& w, MethodDef const& method)
    {
        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
        {
            write_projection_type_for_name_type(w, w.get_generic_arg_scope(index).first, typedef_name_type::Projected);
            w.write("Abi");
        });

        w.write("delegate* unmanaged[Stdcall]<%, int>",
            bind<write_abi_parameter_types_pointer>(method_signature(method)));
    }

    bool abi_signature_has_generic_parameters(writer& w, method_signature const& signature)
    {
        bool signature_has_generic_parameters{};

        writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
            signature_has_generic_parameters = true;
        });

        auto _ = w.write_temp("%", bind<write_abi_parameters>(signature));
        return signature_has_generic_parameters;
    }

    bool abi_signature_without_return_has_generic_parameters(writer& w, method_signature const& signature)
    {
        bool signature_has_generic_parameters{};

        writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
            signature_has_generic_parameters = true;
        });

        auto _ = w.write_temp("%", bind<write_abi_parameters_without_return>(signature));
        return signature_has_generic_parameters;
    }

    bool projected_signature_has_generic_parameters(writer& w, method_signature const& signature)
    {
        bool signature_has_generic_parameters{};

        writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
            signature_has_generic_parameters = true;
        });

        auto _ = w.write_temp("%%",
            bind_list<write_projection_parameter_type>(", ", signature.params()),
            bind<write_projection_return_type>(signature));
        return signature_has_generic_parameters;
    }

    template<typename write_params>
    void write_event_params(writer& w, row_base<Event>::value_type const& evt, write_params params)
    {
        method_signature add_sig{ std::get<0>(get_event_methods(evt)) };
        auto semantics = get_type_semantics(add_sig.params().at(0).second->Type());

        if (auto td = std::get_if<type_definition>(&semantics))
        {
            method_signature invoke_sig{ get_delegate_invoke(*td) };
            if (invoke_sig.params().size() > 0)
            {
                params(w, invoke_sig);
            }
        }
        else if (auto gti = std::get_if<generic_type_instance>(&semantics))
        {
            auto guard{ w.push_generic_args(*gti) };
            method_signature invoke_sig{ get_delegate_invoke(gti->generic_type) };
            params(w, invoke_sig);
        }
    }

    void write_event_param_types(writer& w, row_base<Event>::value_type const& evt)
    {
        auto write_params = [](writer& w, method_signature const& invoke_sig)
        {
            w.write("<%>", bind_list<write_projection_parameter_type>(", ", invoke_sig.params()));
        };
        write_event_params(w, evt, write_params);
    }

    void write_delegate_abi_call(writer& w, TypeDef const& type, std::string_view call, std::string_view name)
    {
        w.write("%%.%(%)",
            bind<write_typedef_name>(type, typedef_name_type::ABI, false),
            bind<write_type_params>(type),
            call, name);
    }

    void write_object_marshal_from_abi(writer& w, type_semantics const& param_type, TypeDef const& type, std::string_view name, bool is_boxed = false)
    {
        switch (get_category(type))
        {
        case category::enum_type:
        {
            if (is_boxed)
            {
                w.write("(%)", bind<write_type_name>(type, typedef_name_type::Projected, false));
            }
            w.write("%", name);
            return;
        }
        case category::delegate_type:
        {
            write_delegate_abi_call(w, type, "FromAbi", name);
            return;
        }
        case category::struct_type:
        {
            if (is_type_blittable(param_type))
            {
                w.write("%", name);
            }
            else
            {
                w.write("%.FromAbi(%)", bind<write_type_name>(param_type, typedef_name_type::ABI, true), name);
            }
            return;
        }
        case category::interface_type:
        {
            w.write("MarshalInterface<%>.FromAbi(%)",
                bind<write_type_name>(type, typedef_name_type::Projected, false),
                name);
            return;
        }
        case category::class_type:
        {
            w.write("%.FromAbi(%)",
                bind<write_projection_type>(param_type),
                name);
            return;
        }
        }
    }

    void write_fundamental_marshal_to_abi(writer& w, fundamental_type type, std::string_view name)
    {
        switch (type)
        {
        case fundamental_type::String:
            w.write("%.Handle", name);
            break;
        case fundamental_type::Boolean:
            w.write("(byte)(% ? 1 : 0)", name);
            break;
        case fundamental_type::Char:
            w.write("(ushort)%", name);
            break;
        default:
            w.write("%", name);
            break;
        }
    }

    void write_fundamental_marshal_from_abi(writer& w, fundamental_type type, std::string_view name, bool is_boxed = false)
    {
        if (type == fundamental_type::String)
        {
            w.write(R"(MarshalString.FromAbi(%))", name);
        }
        else if (type == fundamental_type::Boolean)
        {
            w.write(is_boxed ? "((byte)(object)% != 0)" : "(% != 0)", name);
        }
        else if (type == fundamental_type::Char)
        {
            w.write(is_boxed ? "(char)(ushort)(object)%" : "(char)%", name);
        }
        else if (is_boxed)
        {
            w.write("(%)(object)%", bind<write_fundamental_type>(type), name);
        }
        else
        {
            w.write("%", name);
        }
    }

    void write_class_modifiers(writer& w, TypeDef const& type)
    {
        if (is_static(type))
        {
            w.write("static ");
            return;
        }

        if (type.Flags().Sealed())
        {
            w.write("sealed ");
        }
    }

    void write_objref_type_name(writer& w, type_semantics const& ifaceTypeSemantics);

    bool is_manually_generated_iface(TypeDef const& ifaceType);

    void write_abi_static_method_call(writer& w, type_semantics const& iface, MethodDef const& method, std::string const& targetObjRef)
    {
        method_signature signature{ method };
        w.write("%.%(%%%)", bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, true),
            method.Name(),
            targetObjRef,
            signature.has_params() ? ", " : "",
            bind_list<write_parameter_name_with_modifier>(", ", signature.params()));
    }

    void write_abi_get_property_static_method_call(writer& w, type_semantics const& iface, Property const& prop, std::string const& targetObjRef)
    {
        w.write("%.get_%(%)",
            bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, true),
            prop.Name(),
            targetObjRef);
    }

    void write_abi_set_property_static_method_call(writer& w, type_semantics const& iface, Property const& prop, std::string const& targetObjRef)
    {
        w.write("%.set_%(%, value)",
            bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, true),
            prop.Name(),
            targetObjRef);
    }

    void write_abi_event_source_static_method_call(writer& w, type_semantics const& iface, Event const& evt, bool isSubscribeCall, std::string const& targetObjRef, bool is_static_event = false)
    {
        w.write("%.Get_%2(%, %).%(value)",
            bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, true),
            evt.Name(),
            targetObjRef,
            bind([&](writer& w) {
                    if (is_static_event)
                    {
                        w.write("%", targetObjRef);
                    }
                    else
                    {
                        w.write("(IWinRTObject)this");
                    }
                }),
            isSubscribeCall ? "Subscribe" : "Unsubscribe");
    }

    void write_method(writer& w, method_signature signature, std::string_view method_name,
        std::string_view return_type, std::string_view method_target,
        std::string_view access_spec = ""sv, std::string_view method_spec = ""sv,
        std::string_view platform_attribute = ""sv,
        std::optional<std::pair<type_semantics, MethodDef>> paramsForStaticMethodCall = {})
    {
        w.write(R"(
%%%% %(%) => %;
)",
            platform_attribute,
            access_spec,
            method_spec,
            return_type,
            method_name,
            bind_list<write_projection_parameter>(", ", signature.params()),
            bind([&](writer& w) {
                    if (paramsForStaticMethodCall.has_value())
                    {
                        w.write("%", bind<write_abi_static_method_call>(paramsForStaticMethodCall.value().first, paramsForStaticMethodCall.value().second,
                            w.write_temp("%", method_target)));
                    }
                    else
                    {
                        w.write("%.%(%)", method_target,
                            method_name,
                            bind_list<write_parameter_name_with_modifier>(", ", signature.params()));
                    }
                }));
    }

    void write_explicitly_implemented_method_for_abi(writer& w, MethodDef const& method,
        std::string_view return_type, TypeDef const& method_interface, std::string_view method_target)
    {
        // In authoring scenarios, exclusive interfaces don't exist, so use the CCW impl type.
        bool implement_ccw_interface = does_abi_interface_implement_ccw_interface(method_interface);

        method_signature signature{ method };
        w.write(R"(
% %.%(%) => %.%(%);
)",
            return_type,
            bind<write_type_name>(method_interface, implement_ccw_interface ? typedef_name_type::CCW : typedef_name_type::Projected, false),
            method.Name(),
            bind_list<write_projection_parameter>(", ", signature.params()),
            method_target,
            method.Name(),
            bind_list<write_parameter_name_with_modifier>(", ", signature.params())
        );
    }

    auto method_signature_equal(writer& w, MethodDef const& first, MethodDef const& second)
    {
        method_signature signature_first{ first };
        method_signature signature_second{ second };

        if (size(signature_first.params()) != size(signature_second.params()))
        {
            return false;
        }

        auto first_method_return_type = w.write_temp("%", bind<write_projection_return_type>(signature_first));
        auto second_method_return_type = w.write_temp("%", bind<write_projection_return_type>(signature_second));
        if (first_method_return_type != second_method_return_type)
        {
            return false;
        }

        auto first_method_parameters = w.write_temp("%", bind_list<write_projection_parameter>(", ", signature_first.params()));
        auto second_method_parameters = w.write_temp("%", bind_list<write_projection_parameter>(", ", signature_second.params()));
        return first_method_parameters == second_method_parameters;
    }

    void write_non_projected_type(writer& w, TypeDef const& type)
    {
        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
        {
            write_projection_type_for_name_type(w, w.get_generic_arg_scope(index).first, typedef_name_type::NonProjected);
        });

        w.write("%", bind<write_type_name>(type, typedef_name_type::NonProjected, false));
    }

    auto is_implemented_as_private_method(writer& w, TypeDef const& class_type, MethodDef const& interface_method)
    {
        auto interface_method_name = w.write_temp(
            "%.%",
            bind<write_non_projected_type>(interface_method.Parent()),
            interface_method.Name());
        for (auto&& class_method : class_type.MethodList())
        {
            if (class_method.Flags().Access() == MemberAccess::Private &&
                class_method.Name() == interface_method_name &&
                method_signature_equal(w, class_method, interface_method))
            {
                return true;
            }
        }

        return false;
    }

    auto is_implemented_as_private_mapped_interface(writer& w, TypeDef const& class_type, TypeDef const& interface_type)
    {
        // Assume as long as one member of the custom mapped interface is implemented as a private member,
        // that the entire interface is implemented as an explicit implementation.
        if (size(interface_type.MethodList()) != 0)
        {
            return is_implemented_as_private_method(w, class_type, interface_type.MethodList().first);
        }

        if (size(interface_type.PropertyList()) != 0)
        {
            auto [getter, _] = get_property_methods(interface_type.PropertyList().first);
            return is_implemented_as_private_method(w, class_type, getter);
        }

        if (size(interface_type.EventList()) != 0)
        {
            auto [add, _] = get_event_methods(interface_type.EventList().first);
            return is_implemented_as_private_method(w, class_type, add);
        }

        return false;
    }

    void write_class_method(writer& w, MethodDef const& method, TypeDef const& class_type, 
        bool is_overridable, bool is_protected, std::string_view interface_member, std::string_view platform_attribute,
        std::optional<type_semantics> call_static_method)
    {
        if (method.SpecialName())
        {
            return;
        }

        auto access_spec = is_protected || is_overridable ? "protected " : "public ";
        std::string method_spec = "";

        // If this interface is overridable but the type is sealed, don't mark the member as virtual.
        // The C# compiler errors out about declaring a virtual member in a sealed class.
        if (is_overridable && !class_type.Flags().Sealed())
        {
            // All overridable methods in the WinRT type system have protected visibility.
            access_spec = "protected ";
            method_spec = "virtual ";
        }

        method_signature signature{ method };
        auto raw_return_type = w.write_temp("%", [&](writer& w) {
            write_projection_return_type(w, signature);
        });
        auto return_type = raw_return_type;
        if (method.Name() == "ToString")
        {
            method_spec += "new ";
            if (signature.params().empty())
            {
                if (auto ret = signature.return_signature())
                {
                    auto semantics = get_type_semantics(ret.Type());
                    if (auto ft = std::get_if<fundamental_type>(&semantics))
                    {
                        if (*ft == fundamental_type::String)
                        {
                            method_spec = "override ";
                            return_type = "string";
                        }
                    }
                }
            }
        }

        bool method_return_matches;
        if (is_object_equals_method(method, &method_return_matches) ||
            is_object_hashcode_method(method, &method_return_matches))
        {
            if (method_return_matches)
            {
                method_spec = "override ";
            }
            else
            {
                method_spec += "new ";
            }
        }

        bool is_private = is_implemented_as_private_method(w, class_type, method);
        auto static_method_params = call_static_method.has_value() ? std::optional(std::pair(call_static_method.value(), method)) : std::nullopt;
        if (!is_private)
        {
            write_method(w, signature, method.Name(), return_type, 
                static_method_params.has_value() ? w.write_temp("%", bind<write_objref_type_name>(static_method_params.value().first)) : interface_member, 
                access_spec, method_spec, platform_attribute, static_method_params);
        }

        // If overridable or private, we need to generate the explcit method
        if (is_overridable || is_private)
        {
            w.write(R"(
%% %.%(%) => %;)",
                platform_attribute,
                bind<write_projection_return_type>(signature),
                bind<write_type_name>(method.Parent(), typedef_name_type::CCW, false),
                method.Name(),
                bind_list<write_projection_parameter>(", ", signature.params()),
                bind([&](writer& w)
                {
                    if (is_private)
                    {
                        if (static_method_params.has_value())
                        {
                            w.write("%", bind<write_abi_static_method_call>(static_method_params.value().first, static_method_params.value().second,
                                w.write_temp("%", bind<write_objref_type_name>(static_method_params.value().first))));
                        }
                        else
                        {
                            w.write("%.%", interface_member, method.Name());
                            w.write("(%)", bind_list<write_parameter_name_with_modifier>(", ", signature.params()));
                        }
                    }
                    else
                    {
                        w.write(method.Name());
                        w.write("(%)", bind_list<write_parameter_name_with_modifier>(", ", signature.params()));
                    }
                }));
        }
    }

    void write_property(writer& w, std::string_view external_prop_name, std::string_view prop_name,
        std::string_view prop_type, std::string_view getter_target, std::string_view setter_target,
        std::string_view access_spec = ""sv, std::string_view method_spec = ""sv,
        std::string_view getter_platform = ""sv, std::string_view setter_platform = ""sv,
        std::optional<std::pair<type_semantics, Property>> const& params_for_static_getter = {}, std::optional<std::pair<type_semantics, Property>> const& params_for_static_setter = {})
    {
        if (setter_target.empty() && !params_for_static_setter.has_value())
        {
            w.write(R"(
%%%% % => %;
)",
                getter_platform,
                access_spec,
                method_spec,
                prop_type,
                external_prop_name,
                bind([&](writer& w) {
                    if (params_for_static_getter.has_value())
                    {
                        w.write("%", bind<write_abi_get_property_static_method_call>(params_for_static_getter.value().first, params_for_static_getter.value().second,
                            w.write_temp("%", getter_target)));
                    }
                    else
                    {
                        w.write("%.%", getter_target, prop_name);
                    }
                    }));
            return;
        }

        std::string_view property_platform = ""sv;
        if (getter_platform == setter_platform)
        {
            property_platform = getter_platform;
            getter_platform = ""sv;
            setter_platform = ""sv;
        }

        w.write(R"(
%%%% %
{
%get => %;
%set => %;
}
)",
            property_platform,
            access_spec,
            method_spec,
            prop_type,
            external_prop_name,
            getter_platform, 
            bind([&](writer& w) {
                if (params_for_static_getter.has_value())
                {
                    w.write("%", bind<write_abi_get_property_static_method_call>(params_for_static_getter.value().first, params_for_static_getter.value().second,
                        w.write_temp("%", getter_target)));
                }
                else
                {
                    w.write("%.%", getter_target, prop_name);
                }
                }),
            setter_platform,
            bind([&](writer& w) {
                if (params_for_static_setter.has_value())
                {
                    w.write("%", bind<write_abi_set_property_static_method_call>(params_for_static_setter.value().first, params_for_static_setter.value().second,
                        w.write_temp("%", setter_target)));
                }
                else
                {
                    w.write("%.% = value", setter_target, prop_name);
                }
            }));
    }

    std::string write_as_cast(writer& w, TypeDef const& iface, bool as_abi)
    {
        if (settings.netstandard_compat)
        {
            return w.write_temp(as_abi ? "As<%>()" : "AsInternal(new InterfaceTag<%>())",
                bind<write_type_name>(iface, as_abi ? typedef_name_type::ABI : typedef_name_type::Projected, false));
        }
        else
        {
            return w.write_temp("((%)(IWinRTObject)this)", bind<write_type_name>(iface, typedef_name_type::Projected, false));
        }
    }

    void write_lazy_interface_type_name(writer& w, type_semantics const& ifaceTypeSemantics)
    {
        auto interfaceTypeCode = w.write_temp("%", bind<write_type_name>(ifaceTypeSemantics, typedef_name_type::Projected, true));
        std::string interfaceTypeName = "_lazy_" + interfaceTypeCode;
        w.write("%", escape_type_name_for_identifier(interfaceTypeName));
    }

    void write_lazy_interface_initialization(writer& w, TypeDef const& type)
    {
        int numLazyInterfaces = 0;
        auto lazyInterfaces = w.write_temp("%", [&](writer& w) 
        {
            for (auto&& ii : type.InterfaceImpl())
            {
                if (has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute"))
                {
                    continue;
                }

                for_typedef(w, get_type_semantics(ii.Interface()), [&](auto interface_type)
                {
                    if (!is_manually_generated_iface(interface_type) && !settings.netstandard_compat)
                    {
                        return;
                    }

                    numLazyInterfaces++;
                    auto lazy_interface_name = w.write_temp("%", bind<write_lazy_interface_type_name>(interface_type)); 
                    auto interface_name = write_type_name_temp(w, interface_type);
                    auto interface_abi_name = write_type_name_temp(w, interface_type, "%", typedef_name_type::ABI);

                    auto interface_init_code = settings.netstandard_compat
                        ? w.write_temp(R"(new %(GetReferenceForQI()))",
                            interface_abi_name)
                        : w.write_temp(R"((%)(object)new SingleInterfaceOptimizedObject(typeof(%), _inner ?? ((IWinRTObject)this).NativeObject))",
                            interface_name,
                            interface_name);

                    w.write(R"(
private volatile % %;
private % Make_%()
{
    global::System.Threading.Interlocked.CompareExchange(ref %, %, null);
    return %;
}
)",
                        interface_name,
                        lazy_interface_name,
                        interface_name,
                        lazy_interface_name,
                        lazy_interface_name,
                        interface_init_code,
                        lazy_interface_name);
                });
            }
        });

        if (numLazyInterfaces != 0)
        {
            w.write(R"(%)", lazyInterfaces);
        }
    }

    std::string write_explicit_name(writer& w, TypeDef const& iface, std::string_view name)
    {
        // In authoring scenarios, exclusive interfaces don't exist, so use the CCW impl type.
        bool implement_ccw_interface = does_abi_interface_implement_ccw_interface(iface);

        return w.write_temp("%.%", write_type_name_temp(w, iface, "%", implement_ccw_interface ? typedef_name_type::CCW : typedef_name_type::Projected), name);
    }

    std::string write_prop_type(writer& w, Property const& prop)
    {
        return w.write_temp("%", bind<write_projected_signature>(prop.Type().Type()));
    }

    void write_explicitly_implemented_property_for_abi(writer& w, Property const& prop, TypeDef const& iface, bool as_abi)
    {
        auto prop_target = write_as_cast(w, iface, as_abi);
        auto [getter, setter] = get_property_methods(prop);
        auto getter_target = getter ? prop_target : "";
        auto setter_target = setter ? prop_target : "";
        write_property(w, write_explicit_name(w, iface, prop.Name()), prop.Name(),
            write_prop_type(w, prop), getter_target, setter_target);
    }

    void write_event(writer& w, std::string_view external_event_name, Event const& event, std::string_view event_target,
        std::string_view access_spec = ""sv, std::string_view method_spec = ""sv, std::string_view platform_attribute = ""sv,
        std::optional<std::tuple<type_semantics, Event, bool>> paramsForStaticMethodCall = {})
    {
        auto event_type = w.write_temp("%", bind<write_type_name>(get_type_semantics(event.EventType()), typedef_name_type::Projected, false));

        // Microsoft.UI.Xaml.Input.ICommand has a lower-fidelity type mapping where the type of the event handler doesn't project one-to-one
        // so we need to hard-code mapping the event handler from the mapped WinRT type to the correct .NET type.
        if (event.Name() == "CanExecuteChanged" && event_type == "global::System.EventHandler<object>")
        {
            auto parent_type = w.write_temp("%", bind<write_type_name>(event.Parent(), typedef_name_type::NonProjected, true));
            if (parent_type == "Microsoft.UI.Xaml.Input.ICommand" || parent_type == "Windows.UI.Xaml.Input.ICommand")
            {
                event_type = "global::System.EventHandler";
            }
        }
        w.write(R"(
%%%event % %
{
add => %;
remove => %;
}
)",
            platform_attribute,
            access_spec,
            method_spec,
            event_type,
            external_event_name,
            bind([&](writer& w) {
                    if (paramsForStaticMethodCall.has_value())
                    {
                        auto&& [iface_type_semantics, _, is_static] = paramsForStaticMethodCall.value();
                        w.write("%", bind<write_abi_event_source_static_method_call>(iface_type_semantics, event, true,
                            w.write_temp("%", event_target), is_static));
                    }
                    else
                    {
                        w.write("%.% += value", event_target, event.Name());
                    }
                }),
            bind([&](writer& w) {
                    if (paramsForStaticMethodCall.has_value())
                    {
                        auto&& [iface_type_semantics, _, is_static] = paramsForStaticMethodCall.value();
                        w.write("%", bind<write_abi_event_source_static_method_call>(iface_type_semantics, event, false,
                            w.write_temp("%", event_target), is_static));
                    }
                    else
                    {
                        w.write("%.% -= value", event_target, event.Name());
                    }
                }));
    }

    void write_explicitly_implemented_event_for_abi(writer& w, Event const& evt, TypeDef const& iface, bool as_abi)
    {
        write_event(w, write_explicit_name(w, iface, evt.Name()), evt, write_as_cast(w, iface, as_abi));
    }

    void write_class_event(writer& w, Event const& event, TypeDef const& class_type, bool is_overridable, bool is_protected, std::string_view interface_member, std::string_view platform_attribute = ""sv, std::optional<type_semantics> call_static_method = {})
    {
        auto visibility = "public ";

        if (is_protected)
        {
            visibility = "protected ";
        }

        if (is_overridable)
        {
            visibility = "protected virtual ";
        }

        auto [add, _] = get_event_methods(event);
        bool is_private = is_implemented_as_private_method(w, class_type, add);
        if (!is_private)
        {
            write_event(w, event.Name(), event,
                call_static_method.has_value() ? w.write_temp("%", bind<write_objref_type_name>(call_static_method.value())) : interface_member, 
                visibility, ""sv, platform_attribute, call_static_method.has_value() ? std::optional(std::tuple(call_static_method.value(), event, false)) : std::nullopt);
        }

        // If overridable or private, we need to generate the explicit event
        if (is_overridable || is_private)
        {
            write_event(
                w, 
                w.write_temp("%.%", bind<write_type_name>(event.Parent(), typedef_name_type::CCW, false), event.Name()),
                event,
                is_private ? interface_member : "this",
                ""sv,
                ""sv,
                platform_attribute,
                std::nullopt);
        }
    }

    auto write_custom_attribute_args(writer& w, CustomAttribute const& attribute, CustomAttributeSig const& signature)
    {
        auto write_fixed_arg = [&](writer & w, FixedArgSig arg)
        {
            if (std::holds_alternative<std::vector<ElemSig>>(arg.value))
            {
                throw_invalid("ElemSig list unexpected");
            }
            auto&& arg_value = std::get<ElemSig>(arg.value);

            call(arg_value.value,
                [&](ElemSig::SystemType system_type)
                {
                    auto arg_type = attribute.get_cache().find_required(system_type.name);
                    if (is_static(arg_type))
                    {
                        w.write("typeof(%)", bind<write_projection_type>(arg_type));
                    }
                    else
                    {
                        w.write("typeof(%)", bind<write_projection_ccw_type>(arg_type));
                    }
                },
                [&](ElemSig::EnumValue enum_value)
                {
                    if (enum_value.type.m_typedef.TypeName() == "AttributeTargets")
                    {
                        std::vector<std::string> values;
                        auto value = std::get<uint32_t>(enum_value.value);
                        if (value == 4294967295)
                        {
                            values.emplace_back("All");
                        }
                        else
                        {
                            static struct
                            {
                                uint32_t value;
                                char const* name;
                            }
                            attribute_target_enums[] =
                            {
                                { 1, "Delegate" },
                                { 2, "Enum" },
                                { 4, "Event" },
                                { 8, "Field" },
                                { 16, "Interface" },
                                { 64, "Method" },
                                { 128, "Parameter" },
                                { 256, "Property" },
                                { 512, "Class" },   // "RuntimeClass"
                                { 1024, "Struct" },
                                { 2048, "All" },    // "InterfaceImpl"
                                { 8192, "Struct" }, // "ApiContract"
                            };
                            for (auto&& target_enum : attribute_target_enums)
                            {
                                if (value & target_enum.value)
                                {
                                    values.emplace_back(target_enum.name);
                                }
                            }
                        }
                        w.write("%",
                            bind_list([](writer& w, auto&& value) { w.write("global::System.AttributeTargets.%", value); },
                                " | ", values));
                    }
                    else for (auto field : enum_value.type.m_typedef.FieldList())
                    {
                        if (field.Name() == "value__") continue;
                        auto field_value = field.Constant().Value();
                        if (std::visit([&](auto&& v) { return Constant::constant_type{ v } == field_value; }, enum_value.value))
                        {
                            w.write("%.%",
                                bind<write_projection_type>(enum_value.type.m_typedef),
                                field.Name());
                        }
                    }
                },
                [&](std::string_view type_name)
                {
                    bool previous_char_escape = false;
                    std::string sanitized_type_name;
                    sanitized_type_name.reserve(type_name.length() * 2);
                    for (const auto& c : type_name)
                    {
                        if (c == '\\' && !previous_char_escape)
                        {
                            previous_char_escape = true;
                            continue;
                        }

                        // We only handle the following escape characters for now.
                        if (previous_char_escape && c != '\\' && c != '\'' && c != '"')
                        {
                            sanitized_type_name += '\\';
                        }
                        previous_char_escape = false;

                        sanitized_type_name += c;
                        if (c == '"')
                        {
                            sanitized_type_name += c;
                        }
                    }
                    w.write("^@\"%\"", sanitized_type_name);
                },
                [&](auto&&)
                {
                    if (auto uint32_value = std::get_if<uint32_t>(&arg_value.value))
                    {
                        w.write("%u", *uint32_value);
                    }
                    else if (auto int32_value = std::get_if<int32_t>(&arg_value.value))
                    {
                        w.write(*int32_value);
                    }
                    else if (auto uint64_value = std::get_if<uint64_t>(&arg_value.value))
                    {
                        w.write(*uint64_value);
                    }
                    else if (auto int64_value = std::get_if<int64_t>(&arg_value.value))
                    {
                        w.write(*int64_value);
                    }
                    else if (auto bool_value = std::get_if<bool>(&arg_value.value))
                    {
                        w.write(*bool_value ? "true" : "false");
                    }
                    else if (auto char_value = std::get_if<char16_t>(&arg_value.value))
                    {
                        w.write(*char_value);
                    }
                    else if (auto uint8_value = std::get_if<uint8_t>(&arg_value.value))
                    {
                        w.write(*uint8_value);
                    }
                    else if (auto int8_value = std::get_if<int8_t>(&arg_value.value))
                    {
                        w.write(*int8_value);
                    }
                    else if (auto uint16_value = std::get_if<uint16_t>(&arg_value.value))
                    {
                        w.write(*uint16_value);
                    }
                    else if (auto int16_value = std::get_if<int16_t>(&arg_value.value))
                    {
                        w.write(*int16_value);
                    }
                    else if (auto float_value = std::get_if<float>(&arg_value.value))
                    {
                        w.write_printf("f", *float_value);
                    }
                    else if (auto double_value = std::get_if<double>(&arg_value.value))
                    {
                        w.write_printf("f", *double_value);
                    }
                });
            };

        std::vector<std::string> params;
        for (auto&& arg : signature.FixedArgs())
        {
            params.push_back(w.write_temp("%", bind(write_fixed_arg, arg)));
        }
        for (auto&& arg : signature.NamedArgs())
        {
            params.push_back(w.write_temp("% = %", arg.name, bind(write_fixed_arg, arg.value)));
        }

        return params;
    }

    std::string get_platform(writer& w, CustomAttributeSig const& signature, std::vector<std::string> const& params)
    {
        if (settings.netstandard_compat)
        {
            return {};
        }
        auto& arg0 = signature.FixedArgs()[0];
        auto& elem = std::get<ElemSig>(arg0.value);
        std::string_view contract_name;
        if (auto system_type = std::get_if<ElemSig::SystemType>(&elem.value))
        {
            contract_name = system_type->name;
        }
        else
        {
            contract_name = std::get<std::string_view>(elem.value);
        }
        auto contract_version = std::stoul(params[1]) >> 16;
        auto& contract_platform = get_contract_platform(contract_name, contract_version);
        if (contract_platform.empty())
        {
            return {};
        }
        auto platform = std::string(contract_platform);
        if (w._check_platform)
        {
            if (platform <= w._platform)
            {
                return {};
            }
            if (w._platform.empty())
            {
                w._platform = platform;
            }
        }
        return "\"Windows" + platform + "\"";
    }

    std::string get_platform(writer& w, std::pair<CustomAttribute, CustomAttribute> const& custom_attributes)
    {
        std::map<std::string, std::vector<std::string>> attributes;
        for (auto&& attribute : custom_attributes)
        {
            auto [attribute_namespace, attribute_name] = attribute.TypeNamespaceAndName();
            attribute_name = attribute_name.substr(0, attribute_name.length() - "Attribute"sv.length());
            auto signature = attribute.Value();
            auto params = write_custom_attribute_args(w, attribute, signature);
            // ContractVersion attribute ==> SupportedOSPlatform attribute
            if (attribute_name == "ContractVersion" && signature.FixedArgs().size() == 2)
            {
                return get_platform(w, signature, params);
            }
        }
        return {};
    }

    void write_platform_attribute(writer& w, std::pair<CustomAttribute, CustomAttribute> const& custom_attributes)
    {
        auto platform = get_platform(w, custom_attributes);
        if (platform.empty())
        {
            return;
        }
        w.write("[global::System.Runtime.Versioning.SupportedOSPlatform(%)]\n", platform);
    }

    std::string write_platform_attribute_temp(writer& w, TypeDef const& type)
    {
        return w.write_temp("%", bind<write_platform_attribute>(type.CustomAttribute()));
    }

    void write_custom_attributes(writer& w, std::pair<CustomAttribute, CustomAttribute> const& custom_attributes, bool enable_platform_attrib)
    {
        std::map<std::string, std::vector<std::string>> attributes;
        bool allow_multiple = false;
        for (auto&& attribute : custom_attributes)
        {
            auto [attribute_namespace, attribute_name] = attribute.TypeNamespaceAndName();
            attribute_name = attribute_name.substr(0, attribute_name.length() - "Attribute"sv.length());
            // GCPressure, Guid, Flags, ProjectionInternal are handled separately
            if (attribute_name == "GCPressure" || attribute_name == "Guid" || 
                attribute_name == "Flags" || attribute_name == "ProjectionInternal") continue;
            auto attribute_full = (attribute_name == "AttributeUsage") ? "System.AttributeUsage" :
                w.write_temp("%.%", attribute_namespace, attribute_name);
            auto signature = attribute.Value();
            auto params = write_custom_attribute_args(w, attribute, signature);
            // ContractVersion attribute ==> SupportedOSPlatform attribute
            if (enable_platform_attrib && attribute_name == "ContractVersion" && signature.FixedArgs().size() == 2)
            {
                auto platform = get_platform(w, signature, params);
                if (!platform.empty())
                {
                    attributes["System.Runtime.Versioning.SupportedOSPlatform"].push_back(platform);
                }
            }
            // Skip metadata attributes without a projection
            if (attribute_namespace == "Windows.Foundation.Metadata")
            {
                if (attribute_name == "AllowMultiple")
                {
                    allow_multiple = true;
                }
                if (attribute_name != "DefaultOverload" && attribute_name != "Overload" && 
                    attribute_name != "AttributeUsage" && attribute_name != "ContractVersion" &&
                    attribute_name != "Experimental")
                {
                    continue;
                }
            }
            attributes[attribute_full] = std::move(params);
        }
        if (auto&& usage = attributes.find("System.AttributeUsage"); usage != attributes.end())
        {
            usage->second.push_back(w.write_temp("AllowMultiple = %", allow_multiple ? "true" : "false"));
        }

        for (auto&& attribute : attributes)
        {
            w.write("[global::");
            w.write(attribute.first);
            if (!attribute.second.empty())
            {
                w.write("(%)", bind_list(", ", attribute.second));
            }
            w.write("]\n");
        }
    }

    void write_type_custom_attributes(writer& w, TypeDef const& type, bool enable_platform_attrib)
    {
        write_custom_attributes(w, type.CustomAttribute(), enable_platform_attrib);
    }

    struct attributed_type
    {
        TypeDef type;
        bool activatable{};
        bool statics{};
        bool composable{};
        bool visible{};
    };
    static auto get_attributed_types(writer& w, TypeDef const& type)
    {
        auto get_system_type = [&](auto&& signature) -> TypeDef
        {
            for (auto&& arg : signature.FixedArgs())
            {
                if (auto type_param = std::get_if<ElemSig::SystemType>(&std::get<ElemSig>(arg.value).value))
                {
                    return type.get_cache().find_required(type_param->name);
                }
            }

            return {};
        };

        std::map<std::string, attributed_type> result;

        for (auto&& attribute : type.CustomAttribute())
        {
            auto attribute_name = attribute.TypeNamespaceAndName();

            if (attribute_name.first != "Windows.Foundation.Metadata")
            {
                continue;
            }

            auto signature = attribute.Value();
            attributed_type info;

            if (attribute_name.second == "ActivatableAttribute")
            {
                info.type = get_system_type(signature);
                info.activatable = true;
            }
            else if (attribute_name.second == "StaticAttribute")
            {
                info.type = get_system_type(signature);
                info.statics = true;
            }
            else if (attribute_name.second == "ComposableAttribute")
            {
                info.type = get_system_type(signature);
                info.composable = true;

                for (auto&& arg : signature.FixedArgs())
                {
                    if (auto visibility = std::get_if<ElemSig::EnumValue>(&std::get<ElemSig>(arg.value).value))
                    {
                        info.visible = std::get<int32_t>(visibility->value) == 2;
                        break;
                    }
                }
            }
            else
            {
                continue;
            }

            std::string name;

            if (info.type)
            {
                name = w.write_temp("%", info.type.TypeName());
            }

            result[name] = std::move(info);
        }

        return result;
    }

    void write_class_static_cache_definition(writer& w, TypeDef const& staticsType, TypeDef const& classType)
    {
        if (staticsType)
        {
            auto factory_class_name = settings.netstandard_compat ?
                w.write_temp("BaseFactory<%.Vftbl>", bind<write_type_name>(staticsType, typedef_name_type::ABI, true)) :
                w.write_temp("BaseFactory");

            auto statics_type_name = staticsType.TypeName();
            w.write(R"(
private static % _% = new %("%.%", %.IID);
)",
                factory_class_name,
                statics_type_name,
                factory_class_name,
                classType.TypeNamespace(),
                classType.TypeName(),
                bind<write_type_name>(staticsType, typedef_name_type::StaticAbiClass, true));
        }
    }

    void write_activation_factory_objref_definition(writer& w, TypeDef const& classType)
    {
        auto objrefname = w.write_temp("%", bind<write_objref_type_name>(classType));
        w.write(R"(
private static volatile IObjectReference __%;
private static IObjectReference %
{
    get
    { 
        var factory = __%;
        if (factory != null && factory.IsInCurrentContext)
        {
            return factory;
        }
        else
        {
            return __% = ActivationFactory.Get("%.%");
        }
    }
}
)",
            objrefname,
            objrefname,
            objrefname,
            objrefname,
            classType.TypeNamespace(),
            classType.TypeName());
    }

    void write_static_objref_definition(writer& w, TypeDef const& staticsType, TypeDef const& classType)
    {
        if (settings.netstandard_compat)
        {
            auto vftblType = w.write_temp("%.Vftbl", bind<write_type_name>(staticsType, typedef_name_type::ABI, true));
            auto objrefname = w.write_temp("%", bind<write_objref_type_name>(staticsType));
            w.write(R"(
private static volatile ObjectReference<%> __%;
private static ObjectReference<%> Make__%()
{
    global::System.Threading.Interlocked.CompareExchange(ref __%, ActivationFactory.Get<%>("%.%", %.IID), null);
    return __%;
}
private static ObjectReference<%> % => __% ?? Make__%();
)",
                vftblType,
                objrefname,
                vftblType,
                objrefname,
                objrefname,
                vftblType,
                classType.TypeNamespace(),
                classType.TypeName(),
                bind<write_type_name>(staticsType, typedef_name_type::StaticAbiClass, true),
                objrefname,
                vftblType,
                objrefname,
                objrefname,
                objrefname);
        }
        else
        {
            auto objrefname = w.write_temp("%", bind<write_objref_type_name>(staticsType));
            w.write(R"(
private static volatile IObjectReference __%;
private static IObjectReference %
{
    get
    { 
        var factory = __%;
        if (factory != null && factory.IsInCurrentContext)
        {
            return factory;
        }
        else
        {
            return __% = ActivationFactory.Get("%.%", %.IID);
        }
    }
}
)",
                objrefname,
                objrefname,
                objrefname,
                objrefname,
                classType.TypeNamespace(),
                classType.TypeName(),
                bind<write_type_name>(staticsType, typedef_name_type::StaticAbiClass, true));
        }
    }

    template<auto method_writer>
    void write_static_abi_class_raw(writer& w, TypeDef const& factory_type)
    {
        w.write(R"(
private static class _%
{%}
)",
            bind<write_type_name>(factory_type, typedef_name_type::StaticAbiClass, false),
            bind_each([&](writer& w, MethodDef const& method)
            {
                method_writer(w, factory_type, method);
            }, factory_type.MethodList()));
    }

    void write_static_composing_factory_method(writer& w, TypeDef const& iface, MethodDef const& method);

    void write_static_abi_method_with_raw_return_type(writer& w, TypeDef const& iface, MethodDef const& method);

    static std::string get_default_interface_name(writer& w, TypeDef const& type, bool abiNamespace = true, bool forceCCW = false)
    {
        return w.write_temp("%", bind<write_type_name>(get_type_semantics(get_default_interface(type)), abiNamespace ? typedef_name_type::ABI : forceCCW ? typedef_name_type::CCW : typedef_name_type::Projected, false));
    }

    void write_factory_constructors(writer& w, TypeDef const& factory_type, TypeDef const& class_type)
    {
        auto default_interface_name = get_default_interface_name(w, class_type);
        if (factory_type)
        {
            write_static_abi_class_raw<write_static_abi_method_with_raw_return_type>(w, factory_type);
            write_static_objref_definition(w, factory_type, class_type);
            auto cache_object = w.write_temp("%", bind<write_objref_type_name>(factory_type));

            auto gc_pressure_amount = get_gc_pressure_amount(class_type);
            auto platform_attribute = write_platform_attribute_temp(w, factory_type);
            for (auto&& method : factory_type.MethodList())
            {
                method_signature signature{ method };
                if (settings.netstandard_compat)
                {
                    w.write(R"(
%public %(%) : this(((Func<%>)(() => {
IntPtr ptr = (_%.%(%%%));
try
{
return %(ComWrappersSupport.GetObjectReferenceForInterface(ptr));
}
finally
{
MarshalInspectable<object>.DisposeAbi(ptr);
}
}))())
{
)",
                        platform_attribute,
                        class_type.TypeName(),
                        bind_list<write_projection_parameter>(", ", signature.params()),
                        default_interface_name,
                        bind<write_type_name>(factory_type, typedef_name_type::StaticAbiClass, false),
                        method.Name(),
                        cache_object,
                        signature.has_params() ? ", " : "",
                        bind_list<write_parameter_name_with_modifier>(", ", signature.params()),
                        "new " + default_interface_name
                    );
                }
                else
                {
                    auto default_type_semantics = get_type_semantics(get_default_interface(class_type));
                    auto default_interface_typedef = for_typedef(w, default_type_semantics, [&](auto&& iface) { return iface; });
                    auto is_manually_gen_default_interface = is_manually_generated_iface(default_interface_typedef);

                    bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(class_type.Extends()));

                    w.write(R"(
%public %(%) %
{ 
IntPtr ptr = (_%.%(%%%)); 
try 
{ 
_inner = ComWrappersSupport.GetObjectReferenceForInterface(ptr, %.IID, false); 
%
} 
finally 
{ 
MarshalInspectable<object>.DisposeAbi(ptr); 
}
)",
                        platform_attribute,
                        class_type.TypeName(),
                        bind_list<write_projection_parameter>(", ", signature.params()),
                        has_base_type ? ":base(global::WinRT.DerivedComposed.Instance)" : "",
                        bind<write_type_name>(factory_type, typedef_name_type::StaticAbiClass, false),
                        method.Name(),
                        cache_object,
                        signature.has_params() ? ", " : "",
                        bind_list<write_parameter_name_with_modifier>(", ", signature.params()),
                        bind<write_type_name>(default_type_semantics, typedef_name_type::StaticAbiClass, true),
                        bind([&](writer& w)
                        {
                            if (is_manually_gen_default_interface)
                            {
                                auto projected_default_interface_name = get_default_interface_name(w, class_type, false);
                                w.write("_defaultLazy = new Lazy<%>(() => (%)new SingleInterfaceOptimizedObject(typeof(%), _inner));", projected_default_interface_name, projected_default_interface_name, projected_default_interface_name);
                            }
                        }));
                }
                w.write(R"(
ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
%
%}
)",
                    settings.netstandard_compat ? "" : "ComWrappersHelper.Init(_inner, false);",
                    [&](writer& w)
                    {
                        if (!gc_pressure_amount || settings.netstandard_compat) return;
                        w.write("GC.AddMemoryPressure(%);\n", gc_pressure_amount);
                    });
            }
        }
        else
        {
            write_activation_factory_objref_definition(w, class_type);
            auto objrefname = w.write_temp("%", bind<write_objref_type_name>(class_type));

            if (settings.netstandard_compat)
            {
                w.write(R"(
public %() : this(new %(global::ABI.WinRT.Interop.IActivationFactoryMethods.ActivateInstanceUnsafe(%)))
{
ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
}
)",
                    class_type.TypeName(),
                    default_interface_name,
                    objrefname);
            }
            else
            {
                bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(class_type.Extends()));
                auto default_type_semantics = get_type_semantics(get_default_interface(class_type));
                auto default_interface_typedef = for_typedef(w, default_type_semantics, [&](auto&& iface) { return iface; });
                auto is_manually_gen_default_interface = is_manually_generated_iface(default_interface_typedef);

                w.write(R"(
public %() %
{
_inner = global::ABI.WinRT.Interop.IActivationFactoryMethods.ActivateInstanceUnsafe(%, %.IID);
ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
ComWrappersHelper.Init(_inner, false);
%
}
)",
                    class_type.TypeName(),
                    has_base_type ? ":base(global::WinRT.DerivedComposed.Instance)" : "",
                    objrefname,
                    bind<write_type_name>(default_type_semantics, typedef_name_type::StaticAbiClass, true),
                    bind([&](writer& w)
                    {
                        if (is_manually_gen_default_interface)
                        {
                            w.write("_defaultLazy = new Lazy<%>(() => (%)new SingleInterfaceOptimizedObject(typeof(%), _inner));", default_interface_name, default_interface_name, default_interface_name);
                        }
                    }));
            }
        }
    }

    bool is_manually_generated_iface(TypeDef const& ifaceType)
    {
        if (ifaceType.TypeNamespace() == "Microsoft.UI.Xaml.Interop" && 
            (ifaceType.TypeName() == "IBindableVector" || ifaceType.TypeName() == "IBindableIterable"))
        {
            return true;
        }

        return false;
    }

    void write_objref_type_name(writer& w, type_semantics const& ifaceTypeSemantics)
    {
        auto objRefTypeCode = w.write_temp("%", bind<write_type_name>(ifaceTypeSemantics, typedef_name_type::Projected, true));
        std::string objRefTypeName = "_objRef_" + objRefTypeCode;
        w.write("%", escape_type_name_for_identifier(objRefTypeName));
    }

    void write_class_objrefs_definition(writer& w, TypeDef const& classType, bool replaceDefaultByInner)
    {
        for (auto&& ii : classType.InterfaceImpl())
        {
            auto semantics = get_type_semantics(ii.Interface());
            for_typedef(w, semantics, [&](TypeDef ifaceType)
                {
                    if (is_manually_generated_iface(ifaceType))
                    {
                        return;
                    }
                    if (is_fast_abi_class(classType) && is_exclusive_to(ifaceType) && !is_default_interface(ii)) // fast abi non default interface
                    {
                        return;
                    }
    
                    auto objrefname = bind<write_objref_type_name>(semantics);
                    bool useInner = replaceDefaultByInner && has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute") && distance(ifaceType.GenericParam()) == 0;

                    if (!useInner)
                    {
                        w.write(R"(
private volatile IObjectReference __%;
private IObjectReference Make__%()
{
)",
                        objrefname,
                        objrefname);

                        if (!classType.Flags().Sealed() && is_fast_abi_class(classType) && is_exclusive_to(ifaceType) && is_default_interface(ii))
                        {
                            w.write(R"(global::System.Threading.Interlocked.CompareExchange(ref __%, GetDefaultInterfaceObjRef(%), null);)", objrefname, get_class_hierarchy_index(classType));
                        } 
                        else
                        {
                            if (distance(ifaceType.GenericParam()) != 0)
                            {
                                auto generic_instantiation_class_name = get_generic_instantiation_class_type_name(w, ifaceType);

                                generic_type_instance generic_instantiation;
                                generic_instantiation.generic_type = ifaceType;
                                for (int idx = 0; idx < distance(ifaceType.GenericParam()); idx++)
                                {
                                    generic_instantiation.generic_args.push_back(w.get_generic_arg(idx));
                                }

                                generic_type_instances.insert(
                                    generic_type_instantiation
                                    {
                                        generic_instantiation,
                                        generic_instantiation_class_name
                                    });

                                w.write("_ = global::WinRT.GenericTypeInstantiations.%.EnsureInitialized();\n", generic_instantiation_class_name);
                            }

                            w.write(R"(global::System.Threading.Interlocked.CompareExchange(ref __%, ((IWinRTObject)this).NativeObject.As<IUnknownVftbl>(%.IID), null);)",
                                objrefname,
                                bind<write_type_name>(semantics, typedef_name_type::StaticAbiClass, true)
                            );
                        }

                        w.write(R"(
return __%;
}
private IObjectReference % => __% ?? Make__%();
)",
                        objrefname,
                        objrefname,
                        objrefname,
                        objrefname);
                    }
                    else
                    {
                        w.write(R"(private IObjectReference % => _inner;)", objrefname);
                    }
                });
        }
    }

    void write_composable_constructors(writer& w, TypeDef const& composable_type, TypeDef const& class_type, std::string_view visibility)
    {
        write_static_abi_class_raw<write_static_composing_factory_method>(w, composable_type);

        write_static_objref_definition(w, composable_type, class_type);
        auto cache_object = bind<write_objref_type_name>(composable_type);

        auto default_interface_name = get_default_interface_name(w, class_type, false);
        auto default_interface_abi_name = get_default_interface_name(w, class_type);
        auto default_type_semantics = get_type_semantics(get_default_interface(class_type));
        auto default_interface_typedef = for_typedef(w, default_type_semantics, [&](auto&& iface) { return iface; });
        auto is_manually_gen_default_interface = is_manually_generated_iface(default_interface_typedef);

        for (auto&& method : composable_type.MethodList())
        {
            method_signature signature{ method };
            bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(class_type.Extends()));
            auto params_without_objects = signature.params();
            params_without_objects.pop_back();
            params_without_objects.pop_back();

            if (settings.netstandard_compat)
            {
                w.write(R"(
% %(%)%
{
object baseInspectable = this.GetType() != typeof(%) ? this : null;
IntPtr composed = _%.%(%, %%baseInspectable, out IntPtr ptr);
using IObjectReference composedRef = ObjectReference<IUnknownVftbl>.Attach(ref composed);
try
{
_inner = ComWrappersSupport.GetObjectReferenceForInterface(ptr);
var defaultInterface = new %(_inner);
_defaultLazy = new Lazy<%>(() => defaultInterface);
ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
}
finally
{
MarshalInspectable<object>.DisposeAbi(ptr);
}
}
)",
                    visibility,
                    class_type.TypeName(),
                    bind_list<write_projection_parameter>(", ", params_without_objects),
                    has_base_type ? ":base(global::WinRT.DerivedComposed.Instance)" : "",
                    bind<write_type_name>(class_type,  typedef_name_type::Projected, false),
                    bind<write_type_name>(composable_type, typedef_name_type::StaticAbiClass, false),
                    method.Name(),
                    cache_object,
                    bind_list<write_parameter_name_with_modifier>(", ", params_without_objects),
                    [&](writer& w) {w.write("%", params_without_objects.empty() ? " " : ", "); },
                    default_interface_abi_name,
                    default_interface_abi_name);
            }
            else
            {
                auto platform_attribute = write_platform_attribute_temp(w, composable_type);

                w.write(R"(
%% %(%)%
{
bool isAggregation = this.GetType() != typeof(%);
object baseInspectable = isAggregation ? this : null;
IntPtr composed = _%.%(%, %%baseInspectable, out IntPtr inner);
try
{
ComWrappersHelper.Init(isAggregation, this, composed, inner, %.IID, out _inner);
%
}
finally
{
Marshal.Release(inner);   
}
}
)",
                    platform_attribute, 
                    visibility,
                    class_type.TypeName(),
                    bind_list<write_projection_parameter>(", ", params_without_objects),
                    has_base_type ? ":base(global::WinRT.DerivedComposed.Instance)" : "",
                    bind<write_type_name>(class_type,  typedef_name_type::Projected, false),
                    bind<write_type_name>(composable_type, typedef_name_type::StaticAbiClass, false),
                    method.Name(),
                    cache_object,
                    bind_list<write_parameter_name_with_modifier>(", ", params_without_objects),
                    [&](writer& w) {w.write("%", params_without_objects.empty() ? " " : ", "); },
                    bind<write_type_name>(default_type_semantics, typedef_name_type::StaticAbiClass, true),
                    bind([&](writer& w)
                        {
                            if (is_manually_gen_default_interface)
                            {
                                w.write("_defaultLazy = new Lazy<%>(() => (%)new SingleInterfaceOptimizedObject(typeof(%), _inner));", default_interface_name, default_interface_name, default_interface_name);
                            }
                        }));
            }
        }
    }

    void write_static_factory_method(writer& w, MethodDef const& method, std::string_view method_target, std::string_view platform_attribute = ""sv)
    {
        if (method.SpecialName())
        {
            return;
        }
        method_signature signature{ method };
        auto return_type = w.write_temp("%", [&](writer& w) {
            write_projection_return_type(w, signature);
            });
        write_method(w, signature, method.Name(), return_type, method_target, "public "sv, ""sv, platform_attribute, std::nullopt);
    }

    void write_static_method(writer& w, MethodDef const& method, std::string_view method_target, std::string_view platform_attribute = ""sv)
    {
        if (method.SpecialName())
        {
            return;
        }
        method_signature signature{ method };
        auto return_type = w.write_temp("%", [&](writer& w) {
            write_projection_return_type(w, signature);
        });
        write_method(w, signature, method.Name(), return_type, method_target, "public "sv, "static "sv, platform_attribute, std::optional(std::pair(method.Parent(), method)));
    }

    void write_static_factory_property(writer& w, Property const& prop, std::string_view prop_target, std::string_view platform_attribute = ""sv)
    {
        auto [getter, setter] = get_property_methods(prop);
        auto getter_target = getter ? prop_target : "";
        auto setter_target = setter ? prop_target : "";
        write_property(w, prop.Name(), prop.Name(), write_prop_type(w, prop),
            getter_target, setter_target, "public "sv, ""sv, platform_attribute, platform_attribute,
            std::nullopt,
            std::nullopt);
    }

    void write_static_factory_event(writer& w, Event const& event, std::string_view event_target, std::string_view platform_attribute = ""sv)
    {
        write_event(w, event.Name(), event, event_target, "public "sv, ""sv, platform_attribute, std::nullopt);
    }

    void write_static_event(writer& w, Event const& event, std::string_view event_target, std::string_view platform_attribute = ""sv)
    {
        write_event(w, event.Name(), event, event_target, "public "sv, "static "sv, platform_attribute, std::optional(std::tuple(event.Parent(), event, true)));
    }

    void write_static_members(writer& w, TypeDef const& class_type)
    {
        std::map<std::string, std::tuple<std::string, std::string, std::string, std::string, std::string, std::optional<std::pair<TypeDef, Property>>, std::optional<std::pair<TypeDef, Property>>>> properties;

        for (auto&& [interface_name, factory] : get_attributed_types(w, class_type))
        {
            if (factory.statics)
            {
                write_static_objref_definition(w, factory.type, class_type);
                auto cache_object = w.write_temp("%", bind<write_objref_type_name>(factory.type));

                auto platform_attribute = write_platform_attribute_temp(w, factory.type);
                w.write_each<write_static_method>(factory.type.MethodList(), cache_object, platform_attribute);
                w.write_each<write_static_event>(factory.type.EventList(), cache_object, platform_attribute);

                // Merge property getters/setters, since such may be defined across interfaces
                for (auto&& prop : factory.type.PropertyList())
                {
                    auto [getter, setter] = get_property_methods(prop);
                    auto prop_type = write_prop_type(w, prop);

                    auto [prop_targets, inserted] = properties.try_emplace(std::string(prop.Name()),
                        prop_type,
                        getter ? cache_object : "",
                        getter ? platform_attribute : "",
                        setter ? cache_object : "",
                        setter ? platform_attribute : "",
                        !getter ? std::nullopt : std::optional(std::pair(prop.Parent(), prop)),
                        !setter ? std::nullopt : std::optional(std::pair(prop.Parent(), prop))
                    );
                    if (!inserted)
                    {
                        auto& [property_type, getter_target, getter_platform, setter_target, setter_platform, getter_prop, setter_prop] = prop_targets->second;
                        XLANG_ASSERT(property_type == prop_type);
                        if (getter)
                        {
                            XLANG_ASSERT(getter_target.empty());
                            getter_target = cache_object;
                            getter_platform = platform_attribute;
                            getter_prop = std::optional(std::pair(prop.Parent(), prop));
                        }
                        if (setter)
                        {
                            XLANG_ASSERT(setter_target.empty());
                            setter_target = cache_object;
                            setter_platform = platform_attribute;
                            setter_prop = std::optional(std::pair(prop.Parent(), prop));
                        }
                        XLANG_ASSERT(!getter_target.empty() || !setter_target.empty());
                    }
                }
            }
        }

        // Write properties with merged accessors
        for (auto& [prop_name, prop_data] : properties)
        {
            auto& [prop_type, getter_target, getter_platform, setter_target, setter_platform, getter_prop, setter_prop] = prop_data;
            write_property(w, prop_name, prop_name, prop_type,
                getter_target, setter_target, "public "sv, "static "sv, getter_platform, setter_platform,
                getter_prop,
                setter_prop);
        }
    }

    void write_attributed_types(writer& w, TypeDef const& type)
    {
        bool factory_written{};

        for (auto&& [interface_name, factory] : get_attributed_types(w, type))
        {
            if (factory.activatable)
            {
                write_factory_constructors(w, factory.type, type);
            }
            else if (factory.composable)
            {
                write_composable_constructors(w, factory.type, type, factory.visible ? "public"sv : "protected"sv);
            }
            else if (factory.statics)
            {
                if (!factory_written)
                {
                    factory_written = true;

                    bool has_base_factory{};
                    auto extends = type.Extends();
                    while(!has_base_factory)
                    {
                        auto base_semantics = get_type_semantics(extends);
                        if (std::holds_alternative<object_type>(base_semantics))
                        {
                            break;
                        }
                        for_typedef(w, base_semantics, [&](auto base_type)
                        {
                            for (auto&& [_, base_factory] : get_attributed_types(w, base_type))
                            {
                                if (base_factory.statics)
                                {
                                    has_base_factory = true;
                                    break;
                                }
                            }
                            extends = base_type.Extends();
                        });
                    }

                     w.write(R"(
public static %I As<I>() => ActivationFactory.Get("%.%").AsInterface<I>();
)",
                        has_base_factory ? "new " : "",
                        type.TypeNamespace(),
                        type.TypeName());
                }
            }
        }

        write_static_members(w, type);
    }

    void write_nongeneric_enumerable_members(writer& w, std::string_view target)
    {
        w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => %.GetEnumerator();
)",
            target);
    }

    void write_enumerable_members_using_static_abi_methods(writer& w, bool include_nongeneric, bool emit_explicit, std::string const& objref_name)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IEnumerable<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        auto abiClass = w.write_temp("global::ABI.System.Collections.Generic.IEnumerableMethods<%>", element);

        w.write(R"(
%IEnumerator<%> %GetEnumerator() => %.GetEnumerator(%);
)",
visibility, element, self, abiClass, objref_name);

        if (!include_nongeneric) return;

        if (emit_explicit)
        {
            w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => %.GetEnumerator(%);
)", abiClass, objref_name);
        }
        else
        {
            w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)");
        }
    }

    void write_enumerable_members_using_idic(writer& w, std::string_view target, bool include_nongeneric, bool emit_explicit)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IEnumerable<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%IEnumerator<%> %GetEnumerator() => %.GetEnumerator();
)",         
            visibility, element, self,  target);

        if (!include_nongeneric) return;

        if (emit_explicit)
        {
            w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => %.GetEnumerator();
)", target);
        }
        else
        {
            w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)");
        }
    }

    void write_enumerator_members(writer& w, std::string_view target, bool emit_explicit)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IEnumerator<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";

        w.write(R"(
%bool %MoveNext() => %.MoveNext();
%void %Reset() => %.Reset();
%void %Dispose() => %.Dispose();
%% %Current => %.Current;
object IEnumerator.Current => Current;
)", 
            visibility, self, target, 
            visibility, self, target, 
            visibility, self, target, 
            visibility, element, self, target);
    }

    void write_enumerator_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string const& objref_name)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IEnumerator<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        auto abiClass = w.write_temp("global::ABI.System.Collections.Generic.IEnumeratorMethods<%>", element);

        w.write(R"(
%bool %MoveNext() => %.MoveNext(%);
%void %Reset() => %.Reset(%);
%void %Dispose() => %.Dispose(%);
%% %Current => %.get_Current(%);
object IEnumerator.Current => Current;
)",
visibility, self, abiClass, objref_name,
visibility, self, abiClass, objref_name,
visibility, self, abiClass, objref_name,
visibility, element, self, abiClass, objref_name);
    }

    void write_readonlydictionary_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string const& objref_name)
    {
        auto key = w.write_temp("%", bind<write_generic_type_name>(0));
        auto value = w.write_temp("%", bind<write_generic_type_name>(1));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyDictionary<%, %>.", key, value) : "";
        auto ireadonlycollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyCollection<global::System.Collections.Generic.KeyValuePair<%, %>>.", key, value) : "";
        auto visibility = emit_explicit ? "" : "public ";
        auto abiClass = w.write_temp("global::ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<%, %>", key, value);
        auto enumerableObjRefName = std::regex_replace(objref_name, std::regex("IDictionary"), "IEnumerable_global__System_Collections_Generic_KeyValuePair") + "_";

        w.write(R"(
%IEnumerable<%> %Keys => %.get_Keys(%);
%IEnumerable<%> %Values => %.get_Values(%);
%int %Count => %.get_Count(%);
%% %this[% key] => %.Indexer_Get(%, key);
%bool %ContainsKey(% key) => %.ContainsKey(%, key);
%bool %TryGetValue(% key, out % value) => %.TryGetValue(%, key, out value);
)",
visibility, key, self, abiClass, objref_name,
visibility, value, self, abiClass, objref_name,
visibility, ireadonlycollection, abiClass, objref_name,
visibility, value, self, key, abiClass, objref_name,
visibility, self, key, abiClass, objref_name,
visibility, self, key, value, abiClass, objref_name);
    }

    void write_readonlydictionary_members_using_idic(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
    {
        auto key = w.write_temp("%", bind<write_generic_type_name>(0));
        auto value = w.write_temp("%", bind<write_generic_type_name>(1));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyDictionary<%, %>.", key, value) : "";
        auto ireadonlycollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyCollection<global::System.Collections.Generic.KeyValuePair<%, %>>.", key, value ) : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%IEnumerable<%> %Keys => %.Keys;
%IEnumerable<%> %Values => %.Values;
%int %Count => %.Count;
%% %this[% key] => %[key];
%bool %ContainsKey(% key) => %.ContainsKey(key);
%bool %TryGetValue(% key, out % value) => %.TryGetValue(key, out value);
)", 
            visibility, key, self, target, 
            visibility, value, self, target, 
            visibility, ireadonlycollection, target,
            visibility, value, self, key, target,
            visibility, self, key, target,
            visibility, self, key, value, target);
        
        if (!include_enumerable) return;
        auto enumerable_type = emit_explicit ? w.write_temp("IEnumerable<KeyValuePair<%, %>>.", key, value) : "";
        w.write(R"(
%IEnumerator<KeyValuePair<%, %>> %GetEnumerator() => %.GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)",
            visibility, key, value, enumerable_type, target);
    }

    void write_dictionary_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string const& objref_name)
    {
        auto key = w.write_temp("%", bind<write_generic_type_name>(0));
        auto value = w.write_temp("%", bind<write_generic_type_name>(1));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IDictionary<%, %>.", key, value) : "";
        auto icollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<%, %>>.", key, value) : "";
        auto visibility = emit_explicit ? "" : "public ";
        auto abiClass = w.write_temp("global::ABI.System.Collections.Generic.IDictionaryMethods<%, %>", key, value);
        auto enumerableObjRefName = std::regex_replace(objref_name, std::regex("IDictionary"), "IEnumerable_global__System_Collections_Generic_KeyValuePair") + "_";

        w.write(R"(
%ICollection<%> %Keys => %.get_Keys(%);
%ICollection<%> %Values => %.get_Values(%);
%int %Count => %.get_Count(%);
%bool %IsReadOnly => %.get_IsReadOnly(%);
%% %this[% key] 
{
get => %.Indexer_Get(%, null, key);
set => %.Indexer_Set(%, key, value);
}
%void %Add(% key, % value) => %.Add(%, key, value);
%bool %ContainsKey(% key) => %.ContainsKey(%, key);
%bool %Remove(% key) => %.Remove(%, key);
%bool %TryGetValue(% key, out % value) => %.TryGetValue(%, null, key, out value);
%void %Add(KeyValuePair<%, %> item) => %.Add(%, item);
%void %Clear() => %.Clear(%);
%bool %Contains(KeyValuePair<%, %> item) => %.Contains(%, null, item);
%void %CopyTo(KeyValuePair<%, %>[] array, int arrayIndex) => %.CopyTo(%, %, array, arrayIndex);
bool ICollection<KeyValuePair<%, %>>.Remove(KeyValuePair<%, %> item) => %.Remove(%, item);
)",
visibility, key, self, abiClass, objref_name, //Keys
visibility, value, self, abiClass, objref_name, // Values
visibility, icollection, abiClass, objref_name, // Count
visibility, icollection, abiClass, objref_name, // IsReadOnly
visibility, value, self, key, abiClass, objref_name, abiClass, objref_name, // Indexer
visibility, self, key, value, abiClass, objref_name,
visibility, self, key, abiClass, objref_name,
visibility, self, key, abiClass, objref_name,
visibility, self, key, value, abiClass, objref_name,
visibility, icollection, key, value, abiClass, objref_name,
visibility, icollection, abiClass, objref_name,
visibility, icollection, key, value, abiClass, objref_name,
visibility, icollection, key, value, abiClass, objref_name, enumerableObjRefName,
key, value, key, value, abiClass, objref_name);
    }

    void write_dictionary_members_using_idic(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
    {
        auto key = w.write_temp("%", bind<write_generic_type_name>(0));
        auto value = w.write_temp("%", bind<write_generic_type_name>(1));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IDictionary<%, %>.", key, value) : "";
        auto icollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<%, %>>.", key, value ) : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%ICollection<%> %Keys => %.Keys;
%ICollection<%> %Values => %.Values;
%int %Count => %.Count;
%bool %IsReadOnly => %.IsReadOnly;
%% %this[% key] 
{
get => %[key];
set => %[key] = value;
}
%void %Add(% key, % value) => %.Add(key, value);
%bool %ContainsKey(% key) => %.ContainsKey(key);
%bool %Remove(% key) => %.Remove(key);
%bool %TryGetValue(% key, out % value) => %.TryGetValue(key, out value);
%void %Add(KeyValuePair<%, %> item) => %.Add(item);
%void %Clear() => %.Clear();
%bool %Contains(KeyValuePair<%, %> item) => %.Contains(item);
%void %CopyTo(KeyValuePair<%, %>[] array, int arrayIndex) => %.CopyTo(array, arrayIndex);
bool ICollection<KeyValuePair<%, %>>.Remove(KeyValuePair<%, %> item) => %.Remove(item);
)", 
            visibility, key, self, target, 
            visibility, value, self, target, 
            visibility, icollection, target, 
            visibility, icollection, target, 
            visibility, value, self, key, target, target, 
            visibility, self, key, value, target, 
            visibility, self, key, target, 
            visibility, self, key, target, 
            visibility, self, key, value, target,
            visibility, icollection, key, value, target,
            visibility, icollection, target,
            visibility, icollection, key, value, target,
            visibility, icollection, key, value, target,
            key, value, key, value, target);
        
        if (!include_enumerable) return;
        auto enumerable_type = emit_explicit ? w.write_temp("IEnumerable<KeyValuePair<%, %>>.", key, value) : "";
        w.write(R"(
%IEnumerator<KeyValuePair<%, %>> %GetEnumerator() => %.GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)",
            visibility, key, value, enumerable_type, target);
    }

    void write_readonlylist_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string const& objref_name)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyList<%>.", element) : "";
        auto ireadonlycollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyCollection<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        auto abiClass = w.write_temp("global::ABI.System.Collections.Generic.IReadOnlyListMethods<%>", element);
        auto objRefName = w.write_temp("%", objref_name);

        w.write(R"(
%int %Count => %.get_Count(%);
%
%% %this[int index] => %.Indexer_Get(%, index);
)",
visibility, ireadonlycollection, abiClass, objRefName,
!emit_explicit ? R"([global::System.Runtime.CompilerServices.IndexerName("ReadOnlyListItem")])" : "",
visibility, element, self, abiClass, objRefName);

    }

    void write_readonlylist_members_using_idic(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyList<%>.", element) : "";
        auto ireadonlycollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.IReadOnlyCollection<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%int %Count => %.Count;
%
%% %this[int index] => %[index];
)",
            visibility, ireadonlycollection, target,
            !emit_explicit ? R"([global::System.Runtime.CompilerServices.IndexerName("ReadOnlyListItem")])" : "",
            visibility, element, self, target);
        
        if (!include_enumerable) return;
        auto enumerable_type = emit_explicit ? w.write_temp("IEnumerable<%>.", element) : "";
        w.write(R"(
%IEnumerator<%> %GetEnumerator() => %.GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)",
            visibility, element, enumerable_type, target);
    }

    void write_nongeneric_list_members(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
    {
        auto self = emit_explicit ? "global::System.Collections.IList." : "";
        auto icollection = emit_explicit ? "global::System.Collections.ICollection." : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%int %Count => %.Count;
%bool %IsSynchronized => %.IsSynchronized;
%object %SyncRoot => %.SyncRoot;
%void %CopyTo(Array array, int index) => %.CopyTo(array, index);
%
%object %this[int index]
{
get => %[index];
set => %[index] = value;
}
%bool %IsFixedSize => %.IsFixedSize;
%bool %IsReadOnly => %.IsReadOnly;
%int %Add(object value) => %.Add(value);
%void %Clear() => %.Clear();
%bool %Contains(object value) => %.Contains(value);
%int %IndexOf(object value) => %.IndexOf(value);
%void %Insert(int index, object value) => %.Insert(index, value);
%void %Remove(object value) => %.Remove(value);
%void %RemoveAt(int index) => %.RemoveAt(index);
)", 
            visibility, icollection, target,
            visibility, icollection, target,
            visibility, icollection, target,
            visibility, icollection, target,
            !emit_explicit ? R"([global::System.Runtime.CompilerServices.IndexerName("NonGenericListItem")])" : "",
            visibility, self,
            target,
            target,
            visibility, self, target,
            visibility, self, target,
            visibility, self, target,
            visibility, self, target,
            visibility, self, target,
            visibility, self, target,
            visibility, self, target,
            visibility, self, target, 
            visibility, self, target);
        
        if (!include_enumerable) return;
        w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => %.GetEnumerator();
)",
            target);
    }

    void write_list_members_using_idic(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IList<%>.", element) : "";
        auto icollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.ICollection<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        
        w.write(R"(
%int %Count => %.Count;
%bool %IsReadOnly => %.IsReadOnly;
%
%% %this[int index] 
{
get => %[index];
set => %[index] = value;
}
%int %IndexOf(% item) => %.IndexOf(item);
%void %Insert(int index, % item) => %.Insert(index, item);
%void %RemoveAt(int index) => %.RemoveAt(index);
%void %Add(% item) => %.Add(item);
%void %Clear() => %.Clear();
%bool %Contains(% item) => %.Contains(item);
%void %CopyTo(%[] array, int arrayIndex) => %.CopyTo(array, arrayIndex);
%bool %Remove(% item) => %.Remove(item);
)",
visibility, icollection, target,
visibility, icollection, target,
!emit_explicit ? R"([global::System.Runtime.CompilerServices.IndexerName("ListItem")])" : "",
visibility, element, self, target, target,
visibility, self, element, target,
visibility, self, element, target,
visibility, self, target,
visibility, icollection, element, target,
visibility, icollection, target,
visibility, icollection, element, target,
visibility, icollection, element, target,
visibility, icollection, element, target);

        if (!include_enumerable) return;
        auto enumerable_type = emit_explicit ? w.write_temp("IEnumerable<%>.", element) : "";
        w.write(R"(
%IEnumerator<%> %GetEnumerator() => %.GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)",
visibility, element, enumerable_type, target);
    }

    void write_list_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string objref_name)
    {
        auto element = w.write_temp("%", bind<write_generic_type_name>(0));
        auto self = emit_explicit ? w.write_temp("global::System.Collections.Generic.IList<%>.", element) : "";
        auto icollection = emit_explicit ? w.write_temp("global::System.Collections.Generic.ICollection<%>.", element) : "";
        auto visibility = emit_explicit ? "" : "public ";
        auto abiClass = w.write_temp("global::ABI.System.Collections.Generic.IListMethods<%>", element);
        auto objRefName = w.write_temp("%", objref_name);
        w.write(R"(
%int %Count => %.get_Count(%);
%bool %IsReadOnly => %.get_IsReadOnly(%);
%
%% %this[int index] 
{
get => %.Indexer_Get(%, index);
set => %.Indexer_Set(%, index, value);
}
%int %IndexOf(% item) => %.IndexOf(%, item);
%void %Insert(int index, % item) => %.Insert(%, index, item);
%void %RemoveAt(int index) => %.RemoveAt(%, index);
%void %Add(% item) => %.Add(%, item);
%void %Clear() => %.Clear(%);
%bool %Contains(% item) => %.Contains(%, item);
%void %CopyTo(%[] array, int arrayIndex) => %.CopyTo(%, array, arrayIndex);
%bool %Remove(% item) => %.Remove(%, item);
)", 
            visibility, icollection, abiClass, objref_name, //Count
            visibility, icollection, abiClass, objref_name, //IsReadOnly
            !emit_explicit ? R"([global::System.Runtime.CompilerServices.IndexerName("ListItem")])" : "", //Indexer
            visibility, element, self, abiClass, objref_name, abiClass, objref_name, //Indexer
            visibility, self, element, abiClass, objref_name, //IndexOf
            visibility, self, element, abiClass, objref_name, //Insert
            visibility, self, abiClass, objref_name, //RemoveAt
            visibility, icollection, element, abiClass, objref_name, //Add
            visibility, icollection, abiClass, objref_name, //Clear
            visibility, icollection, element, abiClass, objref_name, //Contains
            visibility, icollection, element, abiClass, objref_name, //CopyTo
            visibility, icollection, element, abiClass, objref_name); //Remove
    }

    void write_idisposable_members(writer& w, std::string_view target, bool emit_explicit)
    {
        auto self = emit_explicit ? "global::System.IDisposable." : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%void %Dispose() => %.Dispose();
)",
            visibility, self, target);
    }

    void write_idisposable_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string objref_name)
    {
        auto self = emit_explicit ? "global::System.IDisposable." : "";
        auto visibility = emit_explicit ? "" : "public ";
        w.write(R"(
%void %Dispose() => global::ABI.System.IDisposableMethods.Dispose(%);
)",
visibility, self, objref_name);
    }

    void write_notify_data_error_info_members_using_idic(writer& w, std::string_view target, bool emit_explicit)
    {
        auto self = emit_explicit ? "global::System.ComponentModel.INotifyDataErrorInfo." : "";
        auto visibility = emit_explicit ? "" : "public ";

        w.write(R"(
%global::System.Collections.IEnumerable %GetErrors(string propertyName) => %.GetErrors(propertyName);

%event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> %ErrorsChanged
{
add => %.ErrorsChanged += value;
remove => %.ErrorsChanged -= value;
}
%bool %HasErrors {get => %.HasErrors; }
)", 
    visibility, self, target,
    visibility, self, target, target,
    visibility, self, target);
    }

    void write_notify_data_error_info_members_using_static_abi_methods(writer& w, bool emit_explicit, std::string objref_name)
    {
        auto self = emit_explicit ? "global::System.ComponentModel.INotifyDataErrorInfo." : "";
        auto visibility = emit_explicit ? "" : "public ";

        w.write(R"(
%global::System.Collections.IEnumerable %GetErrors(string propertyName) => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.GetErrors(%, propertyName);

%event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> %ErrorsChanged
{
add => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.Get_ErrorsChanged2(%, this).Subscribe(value);
remove => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.Get_ErrorsChanged2(%, this).Unsubscribe(value);
}
%bool %HasErrors {get => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.get_HasErrors(%); }
)",
visibility, self, objref_name,
visibility, self, objref_name, objref_name,
visibility, self, objref_name);
    }

    void write_custom_mapped_type_members(writer& w, std::string_view target, mapped_type const& mapping, bool is_private, bool call_static_abi_methods, std::string objref_name)
    {
        if (mapping.abi_name == "IIterable`1") 
        {
            if (call_static_abi_methods)
            {
                write_enumerable_members_using_static_abi_methods(w, true, is_private, objref_name);
            }
            else
            {
                write_enumerable_members_using_idic(w, target, true, is_private);
            }
        }
        else if (mapping.abi_name == "IIterator`1") 
        {
            if (call_static_abi_methods)
            {
                write_enumerator_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_enumerator_members(w, target, is_private);
            }
        }
        else if (mapping.abi_name == "IMapView`2") 
        {
            
            if (call_static_abi_methods)
            {
                write_readonlydictionary_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_readonlydictionary_members_using_idic(w, target, false, is_private);
            }
        }
        else if (mapping.abi_name == "IMap`2") 
        {
            if (call_static_abi_methods)
            {
                write_dictionary_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_dictionary_members_using_idic(w, target, false, is_private);
            }
        }
        else if (mapping.abi_name == "IVectorView`1")
        {
            if (call_static_abi_methods)
            {
                write_readonlylist_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_readonlylist_members_using_idic(w, target, false, is_private);
            }
        }
        else if (mapping.abi_name == "IVector`1")
        {
            if (call_static_abi_methods)
            {
                write_list_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_list_members_using_idic(w, target, false, is_private);
            }
        }
        else if (mapping.mapped_namespace == "System.Collections" && mapping.mapped_name == "IEnumerable")
        {
            write_nongeneric_enumerable_members(w, target);
        }
        else if (mapping.mapped_namespace == "System.Collections" && mapping.mapped_name == "IList")
        {
            write_nongeneric_list_members(w, target, false, is_private);
        }
        else if (mapping.mapped_namespace == "System" && mapping.mapped_name == "IDisposable")
        {
            if (call_static_abi_methods)
            {
                write_idisposable_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_idisposable_members(w, target, is_private);
            }
        }
        else if (mapping.mapped_namespace == "System.ComponentModel" && mapping.mapped_name == "INotifyDataErrorInfo")
        {
            if (call_static_abi_methods)
            {
                write_notify_data_error_info_members_using_static_abi_methods(w, is_private, objref_name);
            }
            else
            {
                write_notify_data_error_info_members_using_idic(w, target, is_private);
            }
        }
    }

    std::pair<TypeDef, bool> find_property_interface(writer& w, TypeDef const& setter_iface, std::string_view prop_name)
    {
        TypeDef getter_iface;

        auto search_interface = [&](TypeDef const& type)
        {
            for (auto&& prop : type.PropertyList())
            {
                if (prop.Name() == prop_name)
                {
                    getter_iface = type;
                    return true;
                }
            }
            return false;
        };

        std::function<bool(TypeDef const&)> search_interfaces = [&](TypeDef const& type)
        {
            for (auto&& iface : type.InterfaceImpl())
            {
                auto semantics = get_type_semantics(iface.Interface());
                if (for_typedef(w, semantics, [&](auto&& type)
                    {
                        return (setter_iface != type) && (search_interface(type) || search_interfaces(type));
                    })) {
                    return true;
                }
            }
            return false;
        };

        std::function<bool(TypeDef const&)> search_interfaces_from_attributes = [&](TypeDef const& type)
        {
            for (auto&& [interface_name, factory] : get_attributed_types(w, type))
            {
                if (factory.statics && factory.type && (search_interface(factory.type) || search_interfaces(factory.type)))
                {
                    return true;
                }
            }

            return false;
        };


        // first search base interfaces for property getter
        if (search_interfaces(setter_iface))
        {
            return { getter_iface, true };
        }

        // then search peer exclusive-to interfaces and their bases
        if (auto exclusive_to_attr = get_attribute(setter_iface, "Windows.Foundation.Metadata", "ExclusiveToAttribute"))
        {
            auto sig = exclusive_to_attr.Value();
            auto const& fixed_args = sig.FixedArgs();
            XLANG_ASSERT(fixed_args.size() == 1);
            auto sys_type = std::get<ElemSig::SystemType>(std::get<ElemSig>(fixed_args[0].value).value);
            auto exclusive_to_type = setter_iface.get_cache().find_required(sys_type.name);
            if (search_interfaces(exclusive_to_type))
            {
                return { getter_iface, false };
            }

            if (search_interfaces_from_attributes(exclusive_to_type))
            {
                return { getter_iface, false };
            }
        }

        throw_invalid("Could not find property getter interface");
    }



    void write_class_members(writer& w, TypeDef const& type, bool wrapper_type, bool is_interface_impl_type)
    {
        std::set<TypeDef> writtenInterfaces;
        std::map<std::string, std::tuple<std::string, std::string, std::string, std::string, std::string, bool, bool, bool, std::optional<std::pair<type_semantics, Property>>, std::optional<std::pair<type_semantics, Property>>>> properties;
        auto fast_abi_class_val = get_fast_abi_class_for_class(type);

        auto write_class_interface = [&](TypeDef const& interface_type, bool is_default_interface, bool is_overridable_interface, bool is_protected_interface, type_semantics semantics)
        {
            // When writing derived interfaces of interfaces, we can sometimes encounter duplicate interfaces.
            // To prevent writing them multiple times, we catch them here.
            if (writtenInterfaces.find(interface_type) != writtenInterfaces.end())
            {
                return;
            }
            writtenInterfaces.insert(interface_type);

            auto interface_name = write_type_name_temp(w, interface_type);
            auto interface_abi_name = write_type_name_temp(w, interface_type, "%", typedef_name_type::ABI);

            auto static_iface_target = w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::StaticAbiClass, true));
            auto target = wrapper_type ? write_type_name_temp(w, interface_type, "((%) _comp)") :
                (is_default_interface ? "_default" : write_type_name_temp(w, interface_type, "AsInternal(new InterfaceTag<%>())"));

            auto is_fast_abi_iface = fast_abi_class_val.has_value() && is_exclusive_to(interface_type) && !settings.netstandard_compat;
            auto semantics_for_abi_call = is_fast_abi_iface ? get_default_iface_as_type_sem(type) : semantics;

            if (!is_default_interface && !wrapper_type)
            {
                if (settings.netstandard_compat || is_manually_generated_iface(interface_type))
                {
                    w.write(R"(
private % AsInternal(InterfaceTag<%> _) => % ?? Make_%();
)",
                        interface_name,
                        interface_name,
                        bind<write_lazy_interface_type_name>(interface_type),
                        bind<write_lazy_interface_type_name>(interface_type));
                }
            }

            bool call_static_method = !(settings.netstandard_compat || wrapper_type || is_manually_generated_iface(interface_type));

            if (auto mapping = get_mapped_type(interface_type.TypeNamespace(), interface_type.TypeName()); mapping && mapping->has_custom_members_output)
            {
                bool is_private = is_implemented_as_private_mapped_interface(w, type, interface_type);
                auto objref_name = w.write_temp("%", bind<write_objref_type_name>(semantics));
                write_custom_mapped_type_members(w, target, *mapping, is_private, call_static_method, objref_name);
                return;
            }

            auto platform_attribute = write_platform_attribute_temp(w, interface_type);

            w.write_each<write_class_method>(interface_type.MethodList(), type, is_overridable_interface, is_protected_interface, target, platform_attribute, call_static_method ? std::optional(semantics_for_abi_call) : std::nullopt);
            w.write_each<write_class_event>(interface_type.EventList(), type, is_overridable_interface, is_protected_interface, target, platform_attribute, call_static_method ? std::optional(semantics_for_abi_call) : std::nullopt);

            // Merge property getters/setters, since such may be defined across interfaces
            for (auto&& prop : interface_type.PropertyList())
            {
                MethodDef getter, setter;
                std::tie(getter, setter) = get_property_methods(prop);
                auto prop_type = write_prop_type(w, prop);
                auto is_private = getter && is_implemented_as_private_method(w, type, getter);  // for explicitly implemented interfaces, assume there is always a get.
                auto property_name = is_private ? w.write_temp("%.%", interface_name, prop.Name()) : std::string(prop.Name());
                auto [prop_targets, inserted] = properties.try_emplace(property_name,
                    prop_type,
                    getter ? target : "",
                    getter ? platform_attribute : "",
                    setter ? target : "",
                    setter ? platform_attribute : "",
                    is_overridable_interface,
                    !is_protected_interface && !is_overridable_interface, // By default, an overridable member is protected.
                    is_private,
                    call_static_method && getter ? std::optional(std::pair(semantics_for_abi_call, prop)) : std::nullopt,
                    call_static_method && setter ? std::optional(std::pair(semantics_for_abi_call, prop)) : std::nullopt
                );
                if (!inserted)
                {
                    auto& [property_type, getter_target, getter_platform, setter_target, setter_platform, is_overridable, is_public, _, getter_prop, setter_prop] = prop_targets->second;
                    XLANG_ASSERT(property_type == prop_type);
                    if (getter)
                    {
                        XLANG_ASSERT(getter_target.empty());
                        getter_target = target;
                        getter_platform = platform_attribute;
                        getter_prop = call_static_method ? std::optional(std::pair(semantics_for_abi_call, prop)) : std::nullopt;
                    }
                    if (setter)
                    {
                        XLANG_ASSERT(setter_target.empty());
                        setter_target = target;
                        setter_platform = platform_attribute;
                        setter_prop = call_static_method ? std::optional(std::pair(semantics_for_abi_call, prop)) : std::nullopt;
                    }
                    is_overridable |= is_overridable_interface;
                    is_public |= !is_overridable_interface && !is_protected_interface;
                    XLANG_ASSERT(!getter_target.empty() || !setter_target.empty());
                }
                // If this interface is overridable or private then we need to emit an explicit implementation of the property for that interface.
                if (is_overridable_interface || is_private)
                {
                    w.write("\n%% %.% {%%}",
                        platform_attribute,
                        prop_type,
                        bind<write_type_name>(interface_type, typedef_name_type::CCW, false),
                        prop.Name(),
                        bind([&](writer& w)
                        {
                            bool base_getter{};
                            std::string base_getter_platform_attribute{};
                            TypeDef getter_property_iface;
                            if (!getter)
                            {
                                auto property_interface = find_property_interface(w, interface_type, prop.Name());
                                base_getter = property_interface.second;
                                getter_property_iface = property_interface.first;
                                base_getter_platform_attribute = write_platform_attribute_temp(w, property_interface.first);

                            }
                            if (getter || base_getter)
                            {
                                w.write("%get => %; ", base_getter_platform_attribute, bind([&](writer& w) {
                                    if (call_static_method)
                                    {
                                        auto iface = base_getter ? getter_property_iface : prop.Parent();
                                        w.write("%", bind<write_abi_get_property_static_method_call>(iface, prop,
                                            w.write_temp("%", bind<write_objref_type_name>(iface))));
                                    }
                                    else
                                    {
                                        w.write("%%", is_private ? target + "." : "", prop.Name());
                                    }
                                    }));
                            }
                        }),
                        bind([&](writer& w)
                        {
                            if (setter)
                            {
                                w.write("set => %;", bind([&](writer& w) {
                                    if (call_static_method)
                                    {
                                        w.write("%", bind<write_abi_set_property_static_method_call>(prop.Parent(), prop,
                                            w.write_temp("%", bind<write_objref_type_name>(prop.Parent()))));
                                    }
                                    else
                                    {
                                        w.write("%% = value", is_private ? target + "." : "", prop.Name());
                                    }
                                    }));
                            }
                        }));
                }
            }
        };

        if (is_interface_impl_type)
        {
            write_class_interface(type, false, false, false, type);
        }

        std::function<void(TypeDef const&)> write_class_interfaces = [&](TypeDef const& type)
        {
            for (auto&& ii : type.InterfaceImpl())
            {
                auto is_default_interface = has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute");
                auto is_overridable_interface = has_attribute(ii, "Windows.Foundation.Metadata", "OverridableAttribute");
                auto is_protected_interface = has_attribute(ii, "Windows.Foundation.Metadata", "ProtectedAttribute");
                auto semantics = get_type_semantics(ii.Interface());

                for_typedef(w, semantics, [&](auto&& type)
                {
                    write_class_interface(type, is_default_interface, is_overridable_interface, is_protected_interface, is_interface_impl_type ? type : semantics);
                    if (is_interface_impl_type)
                    {
                        write_class_interfaces(type);
                    }
                });
            }
        };

        for_typedef(w, type, [&](auto type)
        {
            write_class_interfaces(type);
        });

        // Write properties with merged accessors
        for (auto& [prop_name, prop_data] : properties)
        {
            auto& [prop_type, getter_target, getter_platform, setter_target, setter_platform, is_overridable, is_public, is_private, getter_prop, setter_prop] = prop_data;
            if (is_private) continue;
            std::string_view access_spec = is_public ? "public "sv : "protected "sv;
            std::string_view method_spec = is_overridable ? "virtual "sv : ""sv;
            write_property(w, prop_name, prop_name, prop_type, 
                getter_prop.has_value() ? w.write_temp("%", bind<write_objref_type_name>(getter_prop.value().first)) : getter_target,
                setter_prop.has_value() ? w.write_temp("%", bind<write_objref_type_name>(setter_prop.value().first)) : setter_target,
                access_spec, method_spec, getter_platform, setter_platform, getter_prop, setter_prop);
        }
    }

    void write_guid_signature(writer& w, type_semantics const& semantics)
    {
        call(semantics,
            [&](guid_type)
            {
                w.write("g16");
            },
            [&](object_type)
            {
                w.write("cinterface(IInspectable)");
            },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                case category::enum_type:
                {
                    w.write("enum(%;%)",
                        bind<write_type_name>(type, typedef_name_type::NonProjected, true),
                        is_flags_enum(type) ? "u4" : "i4");
                    break;
                }
                case category::struct_type:
                {
                    w.write("struct(%;%)",
                        bind<write_type_name>(type, typedef_name_type::NonProjected, true),
                        bind_list([](writer& w, Field const& field)
                        {
                            write_guid_signature(w, get_type_semantics(field.Signature().Type()));
                        }, ";", type.FieldList())
                    );
                    break;
                }
                case category::delegate_type:
                {
                    w.write("delegate({%})", bind<write_guid>(type, true));
                    break;
                }
                case category::interface_type:
                {
                    w.write("{%}", bind<write_guid>(type, true));
                    break;
                }
                case category::class_type:
                {
                    if (auto default_interface = get_default_interface(type))
                    {
                        w.write("rc(%;%)",
                            bind<write_type_name>(type, typedef_name_type::NonProjected, true),
                            bind<write_guid_signature>(get_type_semantics(default_interface)));
                    }
                    else
                    {
                        w.write("{%}", bind<write_guid>(type, true));
                    }
                    break;
                }
                }
            },
            [&](generic_type_instance const& type)
            {
                w.write("pinterface({%};%)",
                    bind<write_guid>(type.generic_type, true),
                    bind_list([](writer& w, type_semantics const& genericType)
                    {
                        write_guid_signature(w, genericType);
                    }, ";", type.generic_args)
                );
            },
            [&](fundamental_type const& type)
            {
                w.write("%", get_fundamental_type_guid_signature(type));
            },
            [&](auto const&) {});
    }

    void write_winrt_attribute(writer& w, TypeDef const& type)
    {
        std::filesystem::path db_path(type.get_database().path());
        if (get_category(type) == category::struct_type)
        {
            w.write(R"([global::WinRT.WindowsRuntimeType("%", "%")])",
                db_path.stem().string(),
                bind<write_guid_signature>(type));
        }
        else
        {
            w.write(R"([global::WinRT.WindowsRuntimeType("%")])",
                db_path.stem().string());
        }
    }

    void write_winrt_metadata_attribute(writer& w, TypeDef const& type)
    {
        std::filesystem::path db_path(type.get_database().path());
        w.write(R"([WindowsRuntimeMetadata("%")])",
            db_path.stem().string());
    }

    void write_winrt_helper_type_attribute(writer& w, TypeDef const& type)
    {
        if (get_category(type) == category::struct_type && is_type_blittable(type))
        {
            w.write(R"([global::WinRT.WindowsRuntimeHelperType])");
            return;
        }

        w.write(R"([global::WinRT.WindowsRuntimeHelperType(typeof(%%))])",
            bind<write_typedef_name>(type, typedef_name_type::ABI, false),
            bind([&](writer& w)
            {
                if (distance(type.GenericParam()) == 0)
                {
                    return;
                }

                // Writes out the generic definition without the types.
                separator s{ w };
                w.write("<%>", bind_each([&](writer& /*w*/, GenericParam const& /*gp*/){ s(); }, type.GenericParam()));
            }));
    }

    // The WinRTExposedType attribute decides the interfaces that are
    // placed on the vtable during CCW generation.
    // 
    // Given structs and delegates can be boxed and passed as an object,
    // we write this attribute to represent the interfaces they implement.
    // 
    // For enums, they can also be boxed, but there are only two types of
    // enums (int and uint) allowing us to handle this directly during CCW
    // generation.
    // 
    // For projection classes, they are typically just unwrapped to get the
    // native object when passed across the ABI.  The exception to this is
    // unsealed classes which are extended by consumers in C# code.
    // For these classes, we generate the attribute using the source generator
    // and put it on the class extending it and also includes the
    // override interfaces from the unsealed base class.
    // 
    // For classes authored using our component authoring support, we
    // generate the attribute on the Impl namespace metadata classes using the
    // source generator rather than here as it allows us to better handle 
    // covariant interfaces.  But for factory classes, we generate them here.
    bool should_write_winrt_exposed_type_attribute(TypeDef const& type, bool isFactory)
    {
        if (settings.netstandard_compat)
        {
            return false;
        }

        return isFactory || 
            get_category(type) == category::struct_type ||
            get_category(type) == category::enum_type ||
            (get_category(type) == category::delegate_type && distance(type.GenericParam()) == 0);
    }

    void write_winrt_exposed_type_attribute(writer& w, TypeDef const& type, bool isFactory)
    {
        if (should_write_winrt_exposed_type_attribute(type, isFactory))
        {
            if (get_category(type) == category::struct_type)
            {
                w.write(R"([global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<%, %>))])",
                    bind<write_type_name>(type, typedef_name_type::Projected, false),
                    bind<write_type_name>(type, is_type_blittable(type) ? typedef_name_type::Projected : typedef_name_type::ABI, false));
            }
            else if (get_category(type) == category::enum_type)
            {
                w.write(R"([global::WinRT.WinRTExposedType(typeof(global::WinRT.EnumTypeDetails<%>))])",
                    bind<write_type_name>(type, typedef_name_type::Projected, false));
            }
            else
            {
                w.write(R"([global::WinRT.WinRTExposedType(typeof(%%WinRTTypeDetails))])", 
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    isFactory ? "ServerActivationFactory" : "");
            }
        }
    }

    void write_winrt_exposed_type_class(writer& w, TypeDef const& type, bool isFactory)
    {
        if (should_write_winrt_exposed_type_attribute(type, isFactory))
        {
            if (get_category(type) == category::class_type && isFactory)
            {
                w.write(R"(
internal sealed class %ServerActivationFactoryWinRTTypeDetails : global::WinRT.IWinRTExposedTypeDetails
{
public global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
{
return new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[]
{
%
};
}
}
)",
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    bind([&](writer& w)
                    {
                        w.write(R"(
new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
{
IID = global::ABI.WinRT.Interop.IActivationFactoryMethods.IID,
Vtable = global::ABI.WinRT.Interop.IActivationFactoryMethods.AbiToProjectionVftablePtr
},
)");

                        for (auto&& [interface_name, factory] : get_attributed_types(w, type))
                        {
                            if ((factory.activatable || factory.statics) && factory.type)
                            {
                                w.write(R"(
new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
{
IID = %.IID,
Vtable = %.AbiToProjectionVftablePtr
},
)",
                                    bind<write_type_name>(factory.type, typedef_name_type::StaticAbiClass, false),
                                    // These are exclusive internal interfaces, so just use the ABI class to get the ptr.
                                    bind<write_type_name>(factory.type, typedef_name_type::ABI, false)
                                );
                            }
                        }
                    })
                );
            }
            else if (get_category(type) == category::delegate_type)
            {
                w.write(R"(
internal sealed class %WinRTTypeDetails : global::WinRT.DelegateTypeDetails<%>
{
public override ComWrappers.ComInterfaceEntry GetDelegateInterface()
{
return new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
{
IID = %.IID,
Vtable = %.AbiToProjectionVftablePtr
};
}
}
)",
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    bind<write_type_name>(type, typedef_name_type::Projected, false),
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    bind<write_type_name>(type, typedef_name_type::ABI, false)
                );
            }
        }
    }

    auto get_invoke_info(writer& w, MethodDef const& method, uint32_t const& abi_methods_start_index = INSPECTABLE_METHOD_COUNT)
    {
        TypeDef const& type = method.Parent();
        return std::pair{
            w.write_temp("(*(delegate* unmanaged[MemberFunction]<%, int>**)ThisPtr)[%]",
                bind<write_abi_parameter_types_pointer>(method_signature { method }),
                get_vmethod_index(type, method) + abi_methods_start_index /* number of methods in IInspectable + previous methods if fastabi*/),
            false
        };
    };

    void write_static_class(writer& w, TypeDef const& type)
    {
        w.write(R"(%%% static class %
{
%}
)",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, true),
            internal_accessibility(),
            bind<write_type_name>(type, typedef_name_type::Projected, false),
            bind<write_attributed_types>(type)
        );
    }

    // Checks if any of the generic args in the concrete type is an non instantiated type.
    // i.e. It is in an generic interface and uses the generic to define the type.
    bool has_generic_param_in_concrete_type(generic_type_instance const& type)
    {
        for (size_t idx = 0; idx < type.generic_args.size(); idx++)
        {
            auto& generic_arg_semantic = type.generic_args[idx];
            if (auto gtp = std::get_if<generic_type_param>(&generic_arg_semantic))
            {
                return true;
            }
        }

        return false;
    }

    void write_ensure_generic_type_initialized_for_instance(writer& w, generic_type_instance const& instance, bool assign_to_variable = false)
    {
        if (settings.netstandard_compat)
        {
            return;
        }

        auto concrete_generic_type = ConvertGenericTypeInstanceToConcreteType(w, instance);
        if (has_generic_param_in_concrete_type(concrete_generic_type))
        {
            return;
        }

        auto guard{ w.push_generic_args(concrete_generic_type) };
        auto generic_instantiation_class_name = get_generic_instantiation_class_type_name(w, concrete_generic_type.generic_type);
        generic_type_instances.insert(
            generic_type_instantiation
            {
                concrete_generic_type,
                generic_instantiation_class_name
            });

        if (assign_to_variable)
        {
            w.write("private static readonly bool initialized = global::WinRT.GenericTypeInstantiations.%.EnsureInitialized();", generic_instantiation_class_name);
        }
        else
        {
            w.write("_ = global::WinRT.GenericTypeInstantiations.%.EnsureInitialized();", generic_instantiation_class_name);
        }
    }

    void write_ensure_generic_type_initialized(writer& w, cswinrt::type_semantics const& semantics, bool assign_to_variable = false)
    {
        call(semantics,
            [&](generic_type_instance const& instance)
            {
                write_ensure_generic_type_initialized_for_instance(w, instance, assign_to_variable);
            },
            [&](auto const&) {});
    }

    void write_event_source_generic_args(writer& w, cswinrt::type_semantics eventTypeSemantics);

    void write_event_source_ctor(writer& w, Event const& evt, int index, uint32_t const& abi_methods_start_index = 6)
    {
        if (for_typedef(w, get_type_semantics(evt.EventType()), [&](TypeDef const& eventType)
            {
                if ((eventType.TypeNamespace() == "Windows.Foundation" || eventType.TypeNamespace() == "System") && eventType.TypeName() == "EventHandler`1")
                {
                    auto [add, remove] = get_event_methods(evt);
                    w.write(R"( new global::ABI.WinRT.Interop.EventHandlerEventSource%(_obj,
%,
%,
%))",
bind<write_type_params>(eventType),
get_invoke_info(w, add, abi_methods_start_index).first,
get_invoke_info(w, remove, abi_methods_start_index).first,
index);
                    return true;
                }
                return false;
            }))
        {
            return;
        }

        auto [add, remove] = get_event_methods(evt);
        w.write(R"(
new %%(_obj,
%,
%,
%))",
            bind<write_event_source_type_name>(get_type_semantics(evt.EventType())),
            bind<write_event_source_generic_args>(get_type_semantics(evt.EventType())),
            get_invoke_info(w, add, abi_methods_start_index).first,
            get_invoke_info(w, remove, abi_methods_start_index).first,
            index);
    }

    void write_event_sources(writer& w, TypeDef const& type)
    {
        for (auto&& evt : type.EventList())
        {
            w.write(R"(
private global::ABI.WinRT.Interop.EventSource<%> _%;)",
bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
evt.Name());
        }
    }

    void write_event_source_table(writer& w, Event const& evt)
    {
        w.write(R"(
private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, global::ABI.WinRT.Interop.EventSource<%>> _%_;
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, global::ABI.WinRT.Interop.EventSource<%>> Make%Table()
{
    %
    global::System.Threading.Interlocked.CompareExchange(ref _%_, new(), null);
    return _%_;
}
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, global::ABI.WinRT.Interop.EventSource<%>> _% => _%_ ?? Make%Table();
)",
            bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
            evt.Name(),
            bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
            evt.Name(),
            bind([&](writer& w)
            {
                call(get_type_semantics(evt.EventType()),
                    [&](generic_type_instance const& instance)
                    {
                        // EventHandler`1 types don't get their unique EventSource class as they share them based on generic.
                        // Due to this, we perform the initialization of the instantiation class here.
                        // Others will perform them in their event source class.
                        if ((instance.generic_type.TypeNamespace() == "Windows.Foundation" || instance.generic_type.TypeNamespace() == "System") && 
                            instance.generic_type.TypeName() == "EventHandler`1")
                        {
                            write_ensure_generic_type_initialized_for_instance(w, instance, false);
                        }
                    },
                    [&](auto const&) {});
            }),
            evt.Name(),
            evt.Name(),
            bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
            evt.Name(),
            evt.Name(),
            evt.Name());
    }

    void write_interface_member_signatures(writer& w, TypeDef const& type)
    {
        for (auto&& method : type.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }

            method_signature signature{ method };
            w.write(R"(
%% %(%);)",
                bind<write_custom_attributes>(method.CustomAttribute(), false),
                bind<write_projection_return_type>(signature),
                method.Name(),
                bind_list<write_projection_parameter>(", ", signature.params())
            );
        }

        for (auto&& prop : type.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);
            // "new" required if overriding a getter in a base interface
            auto new_keyword = (!getter && setter && find_property_interface(w, type, prop.Name()).second) ? "new " : "";
            w.write(R"(
%% % {%% })",
                new_keyword,
                write_prop_type(w, prop),
                prop.Name(),
                getter || setter ? " get;" : "",
                setter ? " set;" : ""
            );
        }

        for (auto&& evt : type.EventList())
        {
            w.write(R"(
event % %;)",
                bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
                evt.Name());
        }
    }

    struct abi_marshaler
    {
        std::string param_name;
        int param_index;
        param_category category;
        bool is_return;
        std::string param_type;
        std::string local_type;
        std::string marshaler_type;
        bool is_value_type;
        bool is_pinnable;
        bool marshal_by_object_reference_value;
        bool has_generic_instantiation;
        std::vector<generic_type_instance> generic_instantiations;
        std::string interface_guid;
        std::string interface_init_rcw_helper;

        bool is_out() const
        {
            return (category == param_category::out) ||
                (category == param_category::receive_array);
        }

        bool is_ref() const
        {
            return (category == param_category::fill_array);
        }

        bool is_generic() const
        {
            return param_index > -1;
        }

        bool is_array() const
        {
            return category >= param_category::pass_array;
        }

        bool is_object_in() const
        {
            return ((category == param_category::in) || (category == param_category::ref)) &&
                marshaler_type.empty() && local_type == "IntPtr";
        }

        // We pass using in for .NET Standard.  Outside of .NET Standard,
        // we want our function pointers to be blittable and be able to disable
        // runtime marshaling, so we use ptrs with the managed function calling the
        // function pointer marking the parameter as in.
        bool is_const_ref() const
        {
            return category == param_category::ref;
        }

        bool is_marshal_by_object_reference_value() const
        {
            return marshal_by_object_reference_value;
        }

        std::string get_marshaler_local(writer& w) const
        {
            return w.write_temp("__%", param_name);
        }

        std::string get_param_local(writer& w) const
        {
            if (!is_generic())
            {
                return is_array() ?
                    w.write_temp("(__%_length, __%_data)",
                        param_name, param_name) :
                    get_marshaler_local(w);
            }
            return is_array() ?
                w.write_temp("(__params[%], __params[%])",
                    param_index, param_index + 1) :
                w.write_temp("__params[%]", param_index);
        }

        void write_locals(writer& w) const
        {
            if (is_pinnable)
                return;

            if (is_object_in() || local_type.empty())
                return;

            if (is_out())
            {
                w.write("% __% = default;\n",
                    local_type,
                    param_name);
                return;
            }

            w.write("using WindowsRuntimeObjectReferenceValue % = %.ConvertToUnmanaged(%)",
                get_marshaler_local(w),
                marshaler_type,
                param_name);
        }

        void write_locals2(writer& w) const
        {
            if (is_pinnable)
                return;

            if (is_generic())
            {
                if (!is_out() && !marshaler_type.empty())
                {
                    w.write("% __% = default;\n", local_type, param_name);
                }
                return;
            }

            if (is_object_in() || local_type.empty())
                return;

            if (!is_array() || !is_out())
            {
                w.write("% __% = default;\n",
                    local_type,
                    param_name);
            }

            if (is_array())
            {
                w.write("int __%_length = default;\n", param_name);
                w.write("IntPtr __%_data = default;\n", param_name);
            }
        }

        void write_create(writer& w, std::string_view source) const
        {
            w.write("%.CreateMarshaler%%(%)",
                marshaler_type,
                is_array() ? "Array" : "",
                is_marshal_by_object_reference_value() ? "2" : "",
                source);
        }

        auto get_escaped_param_name(writer& w) const
        {
            return w.write_temp("%", bind<write_escaped_identifier>(param_name));
        }

        void write_assignments(writer& w) const
        {
            if (is_pinnable || is_object_in() || is_out() || local_type.empty())
                return;

            if (!settings.netstandard_compat &&
                has_generic_instantiation)
            {
                for (auto&& generic_instantiation : generic_instantiations)
                {
                    auto guard{ w.push_generic_args(generic_instantiation) };
                    auto generic_instantiation_class_name = get_generic_instantiation_class_type_name(w, generic_instantiation.generic_type);
                    if (get_category(generic_instantiation.generic_type) == category::delegate_type)
                    {
                        generic_type_instances.insert(
                            generic_type_instantiation
                            {
                                generic_instantiation,
                                generic_instantiation_class_name
                            });

                        w.write("_ = global::WinRT.GenericTypeInstantiations.%.EnsureInitialized();\n", generic_instantiation_class_name);
                    }
                }
            }

            w.write("% = %.CreateMarshaler%%(%%%);\n",
                get_marshaler_local(w),
                marshaler_type,
                is_array() ? "Array" : "",
                is_marshal_by_object_reference_value() ? "2" : "",
                bind<write_escaped_identifier>(param_name),
                interface_guid != "" ? ", " : "",
                interface_guid);

            if (is_generic() || is_array() || (is_const_ref() && !marshaler_type.empty()))
            {
                w.write("%% = %.GetAbi%(%);\n",
                    is_const_ref() && !marshaler_type.empty() ? "var __" : "",
                    get_param_local(w),
                    is_marshal_by_object_reference_value() ? "MarshalInspectable<object>" : marshaler_type,
                    is_array() ? "Array" : "",
                    get_marshaler_local(w));
            }
        }

        bool write_pinnable(writer& w) const
        {
            if (!is_pinnable)
                return false;
            w.write("%.Pinnable __% = new(%);\n", marshaler_type, param_name,
                bind<write_escaped_identifier>(param_name));
            return true;
        }

        void write_fixed_expression(writer& w, bool& write_delimiter) const
        {
            if (!is_pinnable)
                return;
            if (write_delimiter)
            {
                w.write(", ");
            }
            w.write("___% = __%", param_name, param_name);
            write_delimiter = true;
        }

        void write_marshal_to_abi(writer& w, std::string_view source = "") const
        {
            if (!is_generic())
            {
                if (is_array())
                {
                    w.write("%__%_length, %__%_data",
                        is_out() ? (settings.netstandard_compat ? "out " : "&") : "", param_name,
                        is_out() ? (settings.netstandard_compat ? "out " : "&") : "", param_name);
                    return;
                }

                if (is_out())
                {
                    w.write("&__%", param_name);
                    return;
                }

                if (is_object_in())
                {
                    w.write("%%.GetThisPtrUnsafe()", source, bind<write_escaped_identifier>(param_name));
                    return;
                }

                if (marshaler_type.empty())
                {
                    if (param_type == "bool")
                    {
                        w.write("(byte)(%% ? 1 : 0)",
                            source, bind<write_escaped_identifier>(param_name));
                        return;
                    }
                    if (param_type == "char")
                    {
                        w.write("(ushort)%%",
                            source, bind<write_escaped_identifier>(param_name));
                        return;
                    }
                    w.write("%%%",
                        category == param_category::ref ? "_" : "",
                        source, bind<write_escaped_identifier>(param_name));
                    return;
                }
            }

            if (is_array())
            {
                w.write("__%_length, __%_data",
                    param_name,
                    param_name);
                return;
            }

            if (marshaler_type.empty())
            {
                write_escaped_identifier(w, param_name);
                return;
            }

            if (is_const_ref())
            {
                w.write("&____%", param_name);
                return;
            }

            w.write("%.GetThisPtrUnsafe()",
                get_marshaler_local(w));
        }

        void write_from_abi(writer& w, std::string_view source) const
        {
            auto param_cast = is_generic() ?
                w.write_temp("(%)", param_type) : "";

            if (marshaler_type.empty())
            {
                if (local_type == "IntPtr" && param_type != "IntPtr")
                {
                    w.write("%.FromAbi(%)", param_type, source);
                    return;
                }
                if (param_type == "bool")
                {
                    w.write(is_generic() ? "(byte)% != 0" : "% != 0", source);
                    return;
                }
                if (param_type == "char")
                {
                    w.write(is_generic() ? "(char)(ushort)%" : "(char)%", source);
                    return;
                }
                w.write("%%", param_cast, source);
                return;
            }

            w.write("%.ConvertToManaged(null, %)",
                marshaler_type,
                source);
        }

        void write_from_managed(writer& w, std::string_view source) const
        {
            auto param_cast = is_generic() ?
                w.write_temp("(%)", param_type) : "";

            if (marshaler_type.empty())
            {
                if (local_type == "IntPtr")
                {
                    w.write("%.FromManaged(%)", param_type, source);
                    return;
                }
                if (param_type == "bool")
                {
                    w.write("(byte)(% ? 1 : 0)", source);
                    return;
                }
                if (param_type == "char")
                {
                    w.write("(ushort)%", source);
                    return;
                }
                w.write("%%", param_cast, source);
                return;
            }

            w.write("%.FromManaged%(%)",
                marshaler_type,
                is_array() ? "Array" : "",
                source);
        }

        void write_marshal_from_abi(writer& w) const
        {
            if (!is_ref() && (!is_out() || local_type.empty()))
                return;
            if (is_ref())
            {
                if (!starts_with(marshaler_type, "MarshalBlittable"))
                {
                    w.write("%.CopyAbiArray(%, (__%_length, __%_data));\n",
                        marshaler_type,
                        bind<write_escaped_identifier>(param_name),
                        param_name,
                        param_name);
                }
                return;
            }

            if (!settings.netstandard_compat && 
                has_generic_instantiation)
            {
                // If we have an InitRcwHelper call for the RCW impl class, we leave it to that to instantiate the generic interfaces
                // instead of doing them here.
                if (interface_init_rcw_helper != "")
                {
                    w.write(interface_init_rcw_helper);
                }
                else
                {
                    for (auto&& generic_instantiation : generic_instantiations)
                    {
                        auto guard{ w.push_generic_args(generic_instantiation) };
                        auto generic_instantiation_class_name = get_generic_instantiation_class_type_name(w, generic_instantiation.generic_type);
                        if (!starts_with(generic_instantiation_class_name, "Windows_Foundation_IReference"))
                        {
                            generic_type_instances.insert(
                                generic_type_instantiation
                                {
                                    generic_instantiation,
                                    generic_instantiation_class_name
                                });

                            w.write("_ = global::WinRT.GenericTypeInstantiations.%.EnsureInitialized();\n", generic_instantiation_class_name);
                        }
                    }
                }
            }

            is_return ?
                w.write("return ") :
                w.write("% = ", bind<write_escaped_identifier>(param_name));
            write_from_abi(w, get_param_local(w));
            w.write(";\n");
        }

        void write_dispose(writer& w) const
        {
            if (is_pinnable || is_object_in() || local_type.empty())
                return;

            if (marshaler_type.empty())
            {
                if (is_out() && (local_type == "IntPtr" && param_type != "IntPtr"))
                {
                    w.write("MarshalInspectable<object>.DisposeAbi(%);\n", get_marshaler_local(w));
                }
                return;
            }

            if (is_out())
            {
                w.write("%.DisposeAbi%(%);\n",
                    marshaler_type,
                    is_array() ? "Array" : "",
                    get_param_local(w));
            }
            else
            {
                w.write("%.DisposeMarshaler%(%);\n",
                    is_marshal_by_object_reference_value() ? "MarshalInspectable<object>" : marshaler_type,
                    is_array() ? "Array" : "",
                    get_marshaler_local(w));
            }
        }
    };

    void set_abi_marshaler(writer& w, TypeSig const& type_sig, abi_marshaler& m, std::string_view prop_name = "", bool is_generic_instantiation_class = false)
    {
        auto semantics = get_type_semantics(type_sig);
        m.param_type = w.write_temp("%", bind<write_projection_type>(semantics));
        m.is_value_type = is_value_type(semantics);

        auto get_abi_type = [&]()
        {
            auto abi_type = w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::ABI, false));
            if (abi_type != prop_name)
            {
                return abi_type;
            }
            return w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::ABI, true));
        };

        auto set_simple_marshaler_type = [&](abi_marshaler& m, TypeDef const& type)
        {
            if (m.is_array())
            {
                m.marshaler_type = is_type_blittable(semantics, true) ? "MarshalBlittable" : "MarshalNonBlittable";
                m.marshaler_type += "<" + m.param_type + ">";
                m.local_type = m.marshaler_type + ".MarshalerArray";
            }
            else if (!is_type_blittable(type))
            {
                m.marshaler_type = get_abi_type();
                m.local_type = m.marshaler_type;
                if (!m.is_out()) m.local_type += ".Marshaler";

                auto abi_type = w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::ABI, true));
                if (m.marshaler_type == "global::ABI.System.Type")
                {
                    m.is_pinnable = (m.category == param_category::in);
                }
            }
        };

        auto set_typedef_marshaler = [&](abi_marshaler& m, TypeDef const& type)
        {
            switch (get_category(type))
            {
            case category::enum_type:
                break;
            case category::struct_type:
                set_simple_marshaler_type(m, type);
                break;
            case category::interface_type:
                m.marshaler_type = "MarshalInterface<" + m.param_type + ">";
                if (m.is_array())
                {
                    m.local_type = w.write_temp("MarshalInterfaceHelper<%>.MarshalerArray", m.param_type);
                }
                else
                {
                    m.marshal_by_object_reference_value = true;
                    m.local_type = m.is_out() ? "IntPtr" : "ObjectReferenceValue";
                    if (settings.netstandard_compat)
                    {
                        m.interface_guid = w.write_temp("GuidGenerator.GetIID(typeof(%).GetHelperType())", bind<write_type_name>(semantics, typedef_name_type::Projected, false));
                    }
                    else if (type.TypeNamespace() == "Windows.Foundation" && type.TypeName() == "IReference`1")
                    {
                        m.interface_guid = w.write_temp("%.PIID", bind<write_type_name>(semantics, typedef_name_type::ABI, false));
                    }
                    else
                    {
                        m.interface_guid = w.write_temp("%.IID", bind<write_type_name>(type, typedef_name_type::StaticAbiClass, true));
                    }
                }

                // Make sure this isn't being called for a generic instance
                // that was already processed.
                if (!m.has_generic_instantiation)
                {
                    for (auto&& iface : type.InterfaceImpl())
                    {
                        auto ifaceSemantics = get_type_semantics(iface.Interface());
                        call(ifaceSemantics,
                            [&](generic_type_instance const& generic)
                            {
                                m.has_generic_instantiation = true;
                                m.generic_instantiations.emplace_back(generic);
                            },
                            [&](auto) { });
                    }

                    if (has_derived_generic_interface(type))
                    {
                        m.interface_init_rcw_helper = w.write_temp("%.InitRcwHelper();\n", bind<write_type_name>(type, typedef_name_type::StaticAbiClass, true));
                    }
                }
                break;
            case category::class_type:
                m.marshaler_type = w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::ABI, true));
                if (m.is_array())
                {
                    m.local_type = w.write_temp("MarshalInterfaceHelper<%>.MarshalerArray", m.param_type);
                }
                else
                {
                    m.marshal_by_object_reference_value = true;
                    m.local_type = m.is_out() ? "IntPtr" : "ObjectReferenceValue";
                }
                break;
            case category::delegate_type:
                m.marshaler_type = get_abi_type();
                if (m.is_array())
                {
                    m.local_type = w.write_temp("MarshalInterfaceHelper<%>.MarshalerArray", m.param_type);
                }
                else
                {
                    m.marshal_by_object_reference_value = true;
                    m.local_type = m.is_out() ? "IntPtr" : "ObjectReferenceValue";
                }
                break;
            }
        };

        std::function<void()> set_type_semantics_marshaler = [&]()
        {
            call(semantics,
                [&](object_type)
                {
                    m.marshaler_type = "MarshalInspectable<object>";
                    if (m.is_array())
                    {
                        m.local_type = "MarshalInterfaceHelper<object>.MarshalerArray";
                    }
                    else
                    {
                        m.marshal_by_object_reference_value = true;
                        m.local_type = m.is_out() ? "IntPtr" : "ObjectReferenceValue";
                    }
                },
                [&](type_definition const& type)
                {
                    set_typedef_marshaler(m, type);
                },
                [&](generic_type_index const& var)
                {
                    if (is_generic_instantiation_class)
                    {
                        semantics = w.get_generic_arg(var.index);
                        set_type_semantics_marshaler();
                    }
                    else
                    {
                        m.param_type = w.write_temp("%", bind<write_projection_type>(semantics));
                        m.marshaler_type = w.write_temp("Marshaler<%>", m.param_type);
                        // In our non netstandard projection, this should only occur in our generic instantiated 
                        // static method classes which takes the ABI type as a generic.
                        m.local_type = !settings.netstandard_compat && m.is_out() ? w.write_temp("%Abi", m.param_type) : "object";
                    }
                },
                [&](generic_type_instance const& type)
                {
                    auto type_name = w.write_temp("%", bind<write_projection_type>(semantics));

                    bool is_generic_type_param = false;
                    auto generic_instantiation = type;
                    // If this is a generic instantiation class for which we are writing the marshaler for, then
                    // the generics we have in type aren't going to be the actual generic types but index references
                    // to them in a separate vector in the writer.  Due to that, we replace the generic indexes
                    // with the actual types so that we can use them later outside of this context.
                    if (is_generic_instantiation_class)
                    {
                        for (size_t idx = 0; idx < type.generic_args.size(); idx++)
                        {
                            auto& generic_arg_semantic = type.generic_args[idx];
                            if (auto gti = std::get_if<generic_type_index>(&generic_arg_semantic))
                            {
                                generic_instantiation.generic_args[idx] = w.get_generic_arg_scope(gti->index).first;
                            }
                        }
                    }
                    else
                    {
                        // Make sure we are not including declarations of generic interfaces themselves
                        // but rather when they are instantiated.
                        for (size_t idx = 0; idx < type.generic_args.size(); idx++)
                        {
                            auto& generic_arg_semantic = type.generic_args[idx];
                            if (auto gti = std::get_if<generic_type_index>(&generic_arg_semantic))
                            {
                                auto scope_semantics = w.get_generic_arg_scope(gti->index).first;
                                if (auto gtp = std::get_if<generic_type_param>(&scope_semantics))
                                {
                                    is_generic_type_param = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (!is_generic_type_param)
                    {
                        m.generic_instantiations.emplace_back(generic_instantiation);
                        m.has_generic_instantiation = true;
                    }

                    auto guard{ w.push_generic_args(type) };
                    set_typedef_marshaler(m, type.generic_type);
                },
                [&](fundamental_type type)
                {
                    if (type == fundamental_type::String)
                    {
                        if (m.is_array())
                        {
                            m.marshaler_type = "MarshalString";
                            m.local_type = "MarshalString.MarshalerArray";
                        }
                        else
                        {
                            m.marshaler_type = "MarshalString";
                            m.local_type = m.is_out() ? "IntPtr" : "MarshalString";
                            m.is_pinnable = (m.category == param_category::in);
                        }
                    }
                },
                [&](auto const&) {});
        };
        set_type_semantics_marshaler();

        if (m.is_out() && m.local_type.empty())
        {
            m.local_type = w.write_temp("%", bind<write_abi_type>(semantics));
        }

        if (m.is_array() && m.marshaler_type.empty())
        {
            if (m.is_generic())
            {
                m.marshaler_type = w.write_temp("Marshaler<%>", m.param_type);
                m.local_type = "object";
            }
            else
            {
                m.marshaler_type = is_type_blittable(semantics, true) ? "MarshalBlittable" : "MarshalNonBlittable";
                m.marshaler_type += "<" + m.param_type + ">";
                m.local_type = m.marshaler_type + ".MarshalerArray";
            }
        }
    }

    auto get_abi_marshalers(writer& w, method_signature const& signature, bool is_generic, std::string_view prop_name = "", bool raw_return_type = false, bool is_generic_instantiation_class = false)
    {
        std::vector<abi_marshaler> marshalers;
        int param_index = 1;

        for (auto&& param : signature.params())
        {
            abi_marshaler m{
                std::string(param.first.Name()),
                is_generic ? param_index : -1,
                get_param_category(param)
            };
            param_index += m.is_array() ? 2 : 1;
            set_abi_marshaler(w, param.second->Type(), m, prop_name, is_generic_instantiation_class);
            marshalers.push_back(std::move(m));
        }

        if (auto ret = signature.return_signature())
        {
            abi_marshaler m{
                   "retval",
                   is_generic ? param_index : -1,
                   ret.Type().is_szarray() && !raw_return_type ? param_category::receive_array : param_category::out,
                   true
            };
            param_index += m.is_array() ? 2 : 1;
            if (!raw_return_type)
            {
                set_abi_marshaler(w, ret.Type(), m, prop_name, is_generic_instantiation_class);
            }
            else
            {
                m.param_type = w.write_temp("%", bind<write_abi_type>(get_type_semantics(ret.Type())));
                m.local_type = m.param_type;
                m.is_value_type = true;
            }
            marshalers.push_back(std::move(m));
        }

        return marshalers;
    }

    void write_abi_method_call_marshalers(writer& w, std::string_view invoke_target, std::string_view /*invoke_objref*/, bool /*is_generic*/, std::vector<abi_marshaler> const& marshalers, bool has_noexcept_attr = false)
    {
        auto write_abi_invoke = [&](writer& w)
        {
            // Write out initial using statements marshaling the applicable parameters.
            bool have_pinnables{};
            for (auto&& m : marshalers)
            {
                have_pinnables |= m.is_pinnable;
            }

            // Write out of the fixed expression.
            w.write("%",
                bind_each([&](writer& w, abi_marshaler const& m)
                {
                    if (m.is_const_ref() && m.marshaler_type.empty())
                    {
                        w.write("fixed(%* _% = &%)\n",
                            m.param_type,
                            m.param_name,
                            m.param_name);
                    }
                }, marshalers));

            if (have_pinnables)
            {
                bool write_delimiter{};
                w.write("fixed(void* %)\n{\n",
                    bind_each([&](writer& w, abi_marshaler const& m)
                    {
                        m.write_fixed_expression(w, write_delimiter);
                    }, marshalers));

                // TODO: Write out the marshalers for the pinned expressions.
            }

            if (!has_noexcept_attr)
            {
                w.write("RestrictedErrorInfo.ThrowExceptionForHR(%(ThisPtr%));\n",
                    invoke_target,
                    bind_each([](writer& w, abi_marshaler const& m)
                    {
                        w.write(", ");
                        m.write_marshal_to_abi(w);
                    }, marshalers));
            }
            else {
                w.write("%(ThisPtr%);\n",
                    invoke_target,
                    bind_each([](writer& w, abi_marshaler const& m)
                        {
                            w.write(", ");
                            m.write_marshal_to_abi(w);
                        }, marshalers));
            }
            for (auto&& m : marshalers)
            {
                m.write_marshal_from_abi(w);
            }
            if (have_pinnables)
            {
                w.write("}\n");
            }
        };

        w.write("\n");
        for (auto&& m : marshalers)
        {
            m.write_locals(w);
        }

        bool have_disposers = std::find_if(marshalers.begin(), marshalers.end(), [](abi_marshaler const& m)
        {
            return !m.marshaler_type.empty() && !m.is_pinnable;
        }) != marshalers.end();

        if (!have_disposers)
        {
            write_abi_invoke(w);
            return;
        }

        w.write(R"(try
{
%%}
finally
{
%}
)",
            bind_each([](writer& w, abi_marshaler const& m)
            {
                m.write_assignments(w);
            }, marshalers),
            bind(write_abi_invoke),
            bind_each([](writer& w, abi_marshaler const& m)
            {
                m.write_dispose(w);
            }, marshalers)
        );
    }

    void write_abi_method_call(writer& w, method_signature signature, std::string_view invoke_target, std::string_view invoke_objref, bool is_generic, bool raw_return_type = false, bool has_noexcept_attr = false, bool is_generic_instantiation_class = false)
    {
        write_abi_method_call_marshalers(w, invoke_target, invoke_objref, is_generic, get_abi_marshalers(w, signature, is_generic, "", raw_return_type, is_generic_instantiation_class), has_noexcept_attr);
    }

    void write_static_abi_method_with_raw_return_type(writer& w, TypeDef const& iface, MethodDef const& method)
    {
        if (is_special(method))
        {
            return;
        }

        bool generic_type = distance(iface.GenericParam()) > 0;
        auto init_call_variables = [&](writer& w)
        {
            if (generic_type)
            {
                w.write("\nvar _obj = (ObjectReference<%.Vftbl>)_genericObj;", bind<write_type_name>(iface, typedef_name_type::ABI, false));
            }
            w.write("\nvar ThisPtr = _obj.ThisPtr;\n");   
        };
        auto write_raw_return_type = [](writer& w, method_signature const& sig)
        {
            if (auto return_sig = sig.return_signature())
            {
                write_abi_type(w, get_type_semantics(return_sig.Type()));
            }
            else
            {
                w.write("void");
            }
        };

        method_signature signature{ method };
        auto [invoke_target, is_generic] = get_invoke_info(w, method);
        auto objRef = generic_type ? "_genericObj" : "_obj";
        w.write(R"(
public static unsafe % %(% %%%)
{%%}
)",
            bind(write_raw_return_type, signature),
            method.Name(),
            settings.netstandard_compat ? w.write_temp("ObjectReference<%.Vftbl>", bind<write_type_name>(iface, typedef_name_type::ABI, true)) : "IObjectReference",
            objRef,
            signature.has_params() ? ", " : "",
            bind_list<write_projection_parameter>(", ", signature.params()),
            bind(init_call_variables),
            bind<write_abi_method_call>(signature, invoke_target, objRef, is_generic, true, is_noexcept(method), false));
    }


    void write_static_composing_factory_method(writer& w, TypeDef const& iface, MethodDef const& method)
    {
        if (is_special(method))
        {
            return;
        }

        bool generic_type = distance(iface.GenericParam()) > 0;
        auto init_call_variables = [&](writer& w)
        {
            if (generic_type)
            {
                w.write("\nvar _obj = (ObjectReference<%.Vftbl>)_genericObj;", bind<write_type_name>(iface, typedef_name_type::ABI, false));
            }
            w.write("\nvar ThisPtr = _obj.ThisPtr;\n");   
        };
        auto write_composable_constructor_params = [&](writer& w, method_signature const& method_sig)
        {
            auto const& params = method_sig.params();
            // We need to special case the last parameter
            separator s{ w };
            for (size_t i = 0; i < params.size() - 1; i++)
            {
                s();
                write_projection_parameter(w, params[i]);
            }

            // The innerIterface parameter is always an out IntPtr.
            XLANG_ASSERT(get_param_category(params[params.size() - 1]) == param_category::out);

            s();
            w.write("out IntPtr %",
                bind<write_parameter_name>(params[params.size() - 1]));
        };

        auto write_raw_return_type = [](writer& w, method_signature const& sig)
        {
            if (auto return_sig = sig.return_signature())
            {
                write_abi_type(w, get_type_semantics(return_sig.Type()));
            }
            else
            {
                w.write("void");
            }
        };

        method_signature signature{ method };
        auto [invoke_target, is_generic] = get_invoke_info(w, method);

        auto abi_marshalers = get_abi_marshalers(w, signature, is_generic, "", true);
        // The last abi marshaler is the return value and the second-to-last one
        // is the inner object (which is the return value we want).
        size_t inner_inspectable_index = abi_marshalers.size() - 2;
        abi_marshaler const& inner_inspectable_ref = abi_marshalers[inner_inspectable_index];
        abi_marshalers[inner_inspectable_index] = {
            inner_inspectable_ref.param_name,
            inner_inspectable_ref.param_index,
            inner_inspectable_ref.category,
            inner_inspectable_ref.is_return,
            "IntPtr",
            "IntPtr",
            {},
            true
        };

        auto objRef = generic_type ? "_genericObj" : "_obj";
        w.write(R"(
public static unsafe % %(% %%%)
{%%}
)",
            bind(write_raw_return_type, signature),
            method.Name(),
            settings.netstandard_compat ? w.write_temp("ObjectReference<%.Vftbl>", bind<write_type_name>(iface, typedef_name_type::ABI, true)) : "IObjectReference",
            objRef,
            signature.has_params() ? ", " : "",
            bind(write_composable_constructor_params, signature),
            bind(init_call_variables),
            bind<write_abi_method_call_marshalers>(invoke_target, objRef, is_generic, abi_marshalers, is_noexcept(method)));
    }

    void write_interface_members_netstandard(writer& w, TypeDef const& type)
    {
        for (auto&& method : type.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }
            method_signature signature{ method };
            auto [invoke_target, is_generic] = get_invoke_info(w, method);
            w.write(R"(
public unsafe %% %(%)
{
%}
)",
            method.Name() == "ToString"sv ? "override " : "",
            bind<write_projection_return_type>(signature),
            method.Name(),
            bind_list<write_projection_parameter>(", ", signature.params()),
            bind<write_abi_method_call>(signature, invoke_target, "_obj", is_generic, false, is_noexcept(method), false));
        }

        for (auto&& prop : type.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);
            w.write(R"(
%unsafe % %%
{
)",
settings.netstandard_compat ? "public " : "",
write_prop_type(w, prop),
bind([&](writer& w)
    {
        if (!settings.netstandard_compat)
        {
            w.write("%.", bind<write_type_name>(type, typedef_name_type::CCW, false));
        }
    }),
                prop.Name());

            if (getter)
            {
                auto [invoke_target, is_generic] = get_invoke_info(w, getter);
                auto signature = method_signature(getter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                w.write(R"(get
{%}
)",
bind<write_abi_method_call_marshalers>(invoke_target, "_obj", is_generic, marshalers, is_noexcept(prop)));
            }
            if (setter)
            {
                if (!getter)
                {
                    auto getter_interface = write_type_name_temp(w,
                        find_property_interface(w, type, prop.Name()).first,
                        "%",
                        settings.netstandard_compat ? typedef_name_type::ABI : typedef_name_type::CCW);
                    auto getter_cast = settings.netstandard_compat ? "As<%>()"s : "((%)(IWinRTObject)this)"s;
                    w.write("get{ return " + getter_cast + ".%; }\n", getter_interface, prop.Name());
                }
                auto [invoke_target, is_generic] = get_invoke_info(w, setter);
                auto signature = method_signature(setter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                marshalers[0].param_name = "value";
                w.write(R"(set
{%}
)",
bind<write_abi_method_call_marshalers>(invoke_target, "_obj", is_generic, marshalers, is_noexcept(prop)));
            }
            w.write("}\n");
        }

        int index = 0;
        for (auto&& evt : type.EventList())
        {
            auto semantics = get_type_semantics(evt.EventType());
            auto event_source = w.write_temp(settings.netstandard_compat ? "_%" : "Get_%2()", evt.Name());
            w.write(R"(
%event % %%
{
add => %.Subscribe(value);
remove => %.Unsubscribe(value);
}
)",

                settings.netstandard_compat ? "public " : "",
        bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
        bind([&](writer& w)
            {
                if (!settings.netstandard_compat)
                {
                    w.write("%.", bind<write_type_name>(type, typedef_name_type::CCW, false));
                }
            }),
        evt.Name(),
                event_source,
                event_source);
            index++;
        }
    }

    void write_interface_members(writer& w, TypeDef const& type)
    {
        if (is_exclusive_to(type) && !settings.idic_exclusiveto)
        {
            return;
        }

        // In authoring scenarios, exclusive interfaces don't exist, so use the CCW impl type.
        bool implement_ccw_interface = does_abi_interface_implement_ccw_interface(type);

        auto init_call_variables = [&](writer& w)
        {
            w.write("\nvar _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(%).TypeHandle);",
                bind<write_type_name>(type, typedef_name_type::CCW, false));
        };

        for (auto&& method : type.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }
            method_signature signature{ method };
            auto [invoke_target, is_generic] = get_invoke_info(w, method);
            w.write(R"(
unsafe % %%(%)
{%
%%;
}
)",
                bind<write_projection_return_type>(signature),
                bind([&](writer& w)
                {
                    w.write("%.", bind<write_type_name>(type, implement_ccw_interface ? typedef_name_type::CCW : typedef_name_type::Projected, false));
                }),
                method.Name(),
                bind_list<write_projection_parameter>(", ", signature.params()),
                bind(init_call_variables),
                signature.return_signature() ? "return " : "",
                bind<write_abi_static_method_call>(type, method, "_obj")
            );
        }

        for (auto&& prop : type.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);
            w.write(R"(
unsafe % %%
{
)",
write_prop_type(w, prop),
bind([&](writer& w)
    {
        w.write("%.", bind<write_type_name>(type, implement_ccw_interface ? typedef_name_type::CCW : typedef_name_type::Projected, false));
    }),
    prop.Name());

            if (getter)
            {
                auto [invoke_target, is_generic] = get_invoke_info(w, getter);
                auto signature = method_signature(getter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                w.write(R"(get
{%
return %;
}
)",
                    bind(init_call_variables),
                    bind<write_abi_get_property_static_method_call>(type, prop, "_obj")
                );
            }
            if (setter)
            {
                if (!getter)
                {
                    auto getter_interface = write_type_name_temp(w,
                        find_property_interface(w, type, prop.Name()).first,
                        "%",
                        typedef_name_type::CCW);
                    auto getter_cast = "((%)(IWinRTObject)this)"s;
                    w.write("get{ return " + getter_cast + ".%; }\n", getter_interface, prop.Name());
                }
                auto [invoke_target, is_generic] = get_invoke_info(w, setter);
                auto signature = method_signature(setter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                marshalers[0].param_name = "value";
                w.write(R"(set
{%
%;
}
)",
                    bind(init_call_variables),
                    bind<write_abi_set_property_static_method_call>(type, prop, "_obj")
                );
            }
            w.write("}\n");
        }

        int index = 0;
        for (auto&& evt : type.EventList())
        {
            auto semantics = get_type_semantics(evt.EventType());
            auto event_source = w.write_temp("Get_%2()", evt.Name());
            w.write(R"(
event % %%
{
add 
{%
%;
}
remove
{%
%;
}
}
)",

                bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
                bind([&](writer& w)
                {
                    w.write("%.", bind<write_type_name>(type, implement_ccw_interface ? typedef_name_type::CCW : typedef_name_type::Projected, false));
                }),
                evt.Name(),
                bind(init_call_variables),
                bind<write_abi_event_source_static_method_call>(type, evt, true, "_obj", false),
                bind(init_call_variables),
                bind<write_abi_event_source_static_method_call>(type, evt, false, "_obj", false));
            index++;
        }
    }

    void write_generic_method_delegate_variable(writer& w, MethodDef const& method, method_signature const& signature, bool is_parameter_variable = false)
    {
        w.write("%delegate*<IObjectReference, %%%> %%%",
                is_parameter_variable ? "" : "internal unsafe volatile static ",
                bind_list<write_projection_parameter_type>(", ", signature.params()),
                signature.has_params() ? ", " : "",
                bind<write_projection_return_type>(signature),
                is_parameter_variable ? "" : "_",
                method.Name(),
                is_parameter_variable ? "" : ";");
    }

    void write_static_abi_class_members(writer& w, TypeDef const& iface, uint32_t const& abi_methods_start_index = 6, bool is_generic_method_instantiation_class = false)
    {
        bool generic_type = distance(iface.GenericParam()) > 0;
        auto init_call_variables = [&](writer& w)
        {
            w.write(R"(
using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
void* ThisPtr = thisValue.GetThisPtrUnsafe();
)");

            // w.write("\nusing WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();\n");   
        };

        // We make the static ABI classes public for override interfaces to expose
        // the vftbl ptr.  But we don't need the actual RCW implementation for it
        // to be public and in the API surface.  So we use this to determine whether
        // to make them internal.
        bool isExclusiveInterface = is_exclusive_to(iface);

        bool writeEnsureRcwMethodsInitialized = false;

        for (auto&& method : iface.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }

            method_signature signature{ method };

            auto [invoke_target, is_generic] = get_invoke_info(w, method, abi_methods_start_index);
            w.write(R"(
[MethodImpl(MethodImplOptions.NoInlining)]
public static unsafe % %(WindowsRuntimeObjectReference thisReference%%)
{%}
)",
                bind<write_projection_return_type>(signature),
                method.Name(),
                signature.has_params() ? ", " : "",
                bind_list<write_projection_parameter>(", ", signature.params()),
                bind([&](writer& w) {
                    init_call_variables(w);
                    write_abi_method_call(w, signature, invoke_target, "_obj", is_generic, false, is_noexcept(method));
                }));
        }

        for (auto&& prop : iface.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);

            if (getter)
            {
                auto signature = method_signature(getter);
                bool signature_has_only_generic_return =
                    !settings.netstandard_compat &&
                    abi_signature_has_generic_parameters(w, signature) &&
                    !abi_signature_without_return_has_generic_parameters(w, signature);

                if (is_generic_method_instantiation_class && !signature_has_only_generic_return)
                {
                    // These methods are handled by the main static methods class.
                    continue;
                }

                bool projected_signature_has_generic =
                    !settings.netstandard_compat &&
                    projected_signature_has_generic_parameters(w, signature);

                auto [invoke_target, is_generic] = get_invoke_info(w, getter, abi_methods_start_index);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                w.write(R"(% static unsafe % get_%(% %)
{%}
)",
                    isExclusiveInterface ? "internal" : "public",
                    write_prop_type(w, prop),
                    prop.Name(),
                    settings.netstandard_compat ? w.write_temp("ObjectReference<%.Vftbl>", bind<write_type_name>(iface, typedef_name_type::ABI, true)) : "IObjectReference",
                    generic_type ? "_genericObj" : "_obj",
                    bind([&](writer& w) {
                        if (signature_has_only_generic_return && !is_generic_method_instantiation_class)
                        {
                            w.write("\nreturn _get_%(_genericObj);\n", prop.Name());
                        }
                        else if (projected_signature_has_generic && !is_generic_method_instantiation_class)
                        {
                            writeEnsureRcwMethodsInitialized = true;
                            w.write(R"(
if (!RuntimeFeature.IsDynamicCodeCompiled)
{
    _EnsureRcwMethodsInitialized();
    return _%(_genericObj);
}

return get_%Fallback(_genericObj);

[MethodImpl(MethodImplOptions.NoInlining)]
static % get_%Fallback(IObjectReference _genericObj)
{
    if (_% != null)
    {
        return _%(_genericObj);
    }
    else
    {
    %
    }
}
)",
                                getter.Name(),
                                prop.Name(),
                                write_prop_type(w, prop),
                                prop.Name(),
                                getter.Name(),
                                getter.Name(),
                                bind([&](writer& w) {
                                    init_call_variables(w);
                                    write_abi_method_call_marshalers(w, invoke_target, "_obj", is_generic, marshalers, is_noexcept(prop));
                                }));
                        }
                        else
                        {
                            init_call_variables(w);
                            write_abi_method_call_marshalers(w, invoke_target, "_obj", is_generic, marshalers, is_noexcept(prop));
                        }
                    }));
            }
            if (setter && !is_generic_method_instantiation_class)
            {
                auto [invoke_target, is_generic] = get_invoke_info(w, setter, abi_methods_start_index);
                auto signature = method_signature(setter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                marshalers[0].param_name = "value";

                bool projected_signature_has_generic =
                    !settings.netstandard_compat &&
                    projected_signature_has_generic_parameters(w, signature);

                w.write(R"(% static unsafe void set_%(% %, % value)
{%}
)",                 
                    isExclusiveInterface ? "internal" : "public",
                    prop.Name(),
                    settings.netstandard_compat ? w.write_temp("ObjectReference<%.Vftbl>", bind<write_type_name>(iface, typedef_name_type::ABI, true)) : "IObjectReference",
                    generic_type ? "_genericObj" : "_obj",
                    write_prop_type(w, prop),
                    bind([&](writer& w) {
                        if (projected_signature_has_generic && !is_generic_method_instantiation_class)
                        {
                            writeEnsureRcwMethodsInitialized = true;
                            w.write(R"(
if (!RuntimeFeature.IsDynamicCodeCompiled)
{
    _EnsureRcwMethodsInitialized();
    _%(_genericObj, value);
}
else
{
    set_%Fallback(_genericObj, value);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void set_%Fallback(IObjectReference _genericObj, % value)
{
    if (_% != null)
    {
        _%(_genericObj, value);
    }
    else
    {
    %
    }
}
)",
                                setter.Name(),
                                prop.Name(),
                                prop.Name(),
                                write_prop_type(w, prop),
                                setter.Name(),
                                setter.Name(),
                                bind([&](writer& w) {
                                    init_call_variables(w);
                                    write_abi_method_call_marshalers(w, invoke_target, "_obj", is_generic, marshalers, is_noexcept(prop));
                                }));
                        }
                        else
                        {
                            init_call_variables(w);
                            write_abi_method_call_marshalers(w, invoke_target, "_obj", is_generic, marshalers, is_noexcept(prop));
                        }
                    }));
            }
            w.write("\n");
        }

        if (writeEnsureRcwMethodsInitialized)
        {
            w.write(R"(
[MethodImpl(MethodImplOptions.AggressiveInlining)]
internal static unsafe void _EnsureRcwMethodsInitialized()
{
if (RuntimeFeature.IsDynamicCodeCompiled)
{
return;
}
if (!_RcwHelperInitialized)
{
static void ThrowNotInitialized()
{
throw new NotImplementedException(
$"Type '{typeof(%)}' was called without initializing the RCW methods using '@Methods.InitRcwHelper'. " +
$"If using 'IDynamicInterfaceCastable' support to do a dynamic cast to this interface, ensure the 'InitRcwHelper' method is called.");
}

ThrowNotInitialized();
}
}
)",
    bind<write_type_name>(iface, typedef_name_type::Projected, true),
    iface.TypeName());
        }

        if (is_generic_method_instantiation_class)
        {
            // Events are handled by the main static methods class as they won't
            // have generic return.
            return;
        }

        int index = 0;
        for (auto&& evt : iface.EventList())
        {
                    w.write(R"(%
%

% static unsafe global::ABI.WinRT.Interop.EventSource<%> Get_%2(% %, object _thisObj)
{
return _%.GetValue(_thisObj, (key) =>
{
%
return %;
});
}
)",
                        bind<write_event_source_table>(evt),
                        isExclusiveInterface ? "" : w.write_temp(R"(public static unsafe (Action<%>, Action<%>) Get_%(% %, object _thisObj)
{
var eventSource = Get_%2(%, _thisObj);
return (eventSource.Subscribe, eventSource.Unsubscribe);
})", bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
     bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
     evt.Name(),
     settings.netstandard_compat ? w.write_temp("ObjectReference<%.Vftbl>", bind<write_type_name>(iface, typedef_name_type::ABI, true)) : "IObjectReference",
    generic_type ? "_genericObj" : "_obj",
    evt.Name(),
    generic_type ? "_genericObj" : "_obj"),
                        isExclusiveInterface ? "internal" : "public",
                        bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
                        evt.Name(),
                        settings.netstandard_compat ? w.write_temp("ObjectReference<%.Vftbl>", bind<write_type_name>(iface, typedef_name_type::ABI, true)) : "IObjectReference",
                        generic_type ? "_genericObj" : "_obj",
                        evt.Name(),
                        bind(init_call_variables),
                        bind<write_event_source_ctor>(evt, index, abi_methods_start_index)
                    );
            index++;
        }
    }

    struct required_interface
    {
        std::string members;
        std::string helper_wrapper;
        std::string adapter;
    };

    void write_required_interface_members_for_abi_type(writer& w, TypeDef const& type, 
        std::map<std::string, required_interface>& required_interfaces, bool emit_mapped_type_helpers)
    {
        auto write_required_interface = [&](TypeDef const& iface)
        {
            auto interface_name = write_type_name_temp(w, iface);
            if (required_interfaces.find(interface_name) != required_interfaces.end())
            {
                // We've already written this required interface, so don't write it again.
                return;
            }

            if (auto mapping = get_mapped_type(iface.TypeNamespace(), iface.TypeName()))
            {
                auto remove_enumerable = [&](std::string generic_enumerable = "")
                {
                    required_interfaces["global::System.Collections.IEnumerable"] = {};
                    if(generic_enumerable.empty()) return;
                    required_interfaces[std::move(generic_enumerable)] = {};
                };

                bool mapping_written = true;
                if (mapping->abi_name == "IIterable`1") // IEnumerable`1
                {
                    auto element = w.write_temp("%", bind<write_generic_type_name>(0));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_enumerable_members_using_idic>(
                            emit_mapped_type_helpers ? "_iterableToEnumerable"
                            : w.write_temp("((global::System.Collections.Generic.IEnumerable<%>)(IWinRTObject)this)", element),
                            true,
                            !emit_mapped_type_helpers)),
                        w.write_temp("ABI.System.Collections.Generic.IEnumerable<%>", element),
                        "_iterableToEnumerable"
                    };
                    remove_enumerable();
                }
                else if (mapping->abi_name == "IIterator`1") // IEnumerator`1
                {
                    auto element = w.write_temp("%", bind<write_generic_type_name>(0));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_enumerator_members>(
                            emit_mapped_type_helpers ? "_iteratorToEnumerator"
                            : w.write_temp("((global::System.Collections.Generic.IEnumerator<%>)(IWinRTObject)this)", element),
                            !emit_mapped_type_helpers
                        )),
                        w.write_temp("ABI.System.Collections.Generic.IEnumerator<%>", element),
                        "_iteratorToEnumerator"
                    };
                }
                else if (mapping->abi_name == "IMapView`2") // IReadOnlyDictionary`2
                {
                    auto key = w.write_temp("%", bind<write_generic_type_name>(0));
                    auto value = w.write_temp("%", bind<write_generic_type_name>(1));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_readonlydictionary_members_using_idic>(
                            emit_mapped_type_helpers ? "_mapViewToReadOnlyDictionary"
                            : w.write_temp("((global::System.Collections.Generic.IReadOnlyDictionary<%, %>)(IWinRTObject)this)", key, value),
                            true,
                            !emit_mapped_type_helpers)),
                        w.write_temp("ABI.System.Collections.Generic.IReadOnlyDictionary<%, %>", key, value),
                        "_mapViewToReadOnlyDictionary"
                    };
                    remove_enumerable(w.write_temp("global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<%, %>>", key, value));
                }
                else if (mapping->abi_name == "IMap`2") // IDictionary<TKey, TValue> 
                {
                    auto key = w.write_temp("%", bind<write_generic_type_name>(0));
                    auto value = w.write_temp("%", bind<write_generic_type_name>(1));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_dictionary_members_using_idic>(
                            emit_mapped_type_helpers ? "_mapToDictionary"
                            : w.write_temp("((global::System.Collections.Generic.IDictionary<%, %>)(IWinRTObject)this)", key, value),
                            true,
                            !emit_mapped_type_helpers)),
                        w.write_temp("ABI.System.Collections.Generic.IDictionary<%, %>", key, value),
                        "_mapToDictionary"
                    };
                    remove_enumerable(w.write_temp("global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<%, %>>", key, value));
                }
                else if (mapping->abi_name == "IVectorView`1") // IReadOnlyList`1
                {
                    auto element = w.write_temp("%", bind<write_generic_type_name>(0));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_readonlylist_members_using_idic>(
                            emit_mapped_type_helpers ? "_vectorViewToReadOnlyList"
                            : w.write_temp("((global::System.Collections.Generic.IReadOnlyList<%>)(IWinRTObject)this)", element),
                            true,
                            !emit_mapped_type_helpers)),
                        w.write_temp("ABI.System.Collections.Generic.IReadOnlyList<%>", element),
                        "_vectorViewToReadOnlyList"
                    };
                    remove_enumerable(w.write_temp("global::System.Collections.Generic.IEnumerable<%>", element));
                }
                else if (mapping->abi_name == "IVector`1") // IList`1
                {
                    auto element = w.write_temp("%", bind<write_generic_type_name>(0));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_list_members_using_idic>(
                            emit_mapped_type_helpers ? "_vectorToList"
                            : w.write_temp("((global::System.Collections.Generic.IList<%>)(IWinRTObject)this)", element),
                            true,
                            !emit_mapped_type_helpers
                            )),
                        w.write_temp("ABI.System.Collections.Generic.IList<%>", element),
                        "_vectorToList"
                    };
                    remove_enumerable(w.write_temp("global::System.Collections.Generic.IEnumerable<%>", element));
                }
                else if (mapping->abi_name == "IBindableIterable") // IEnumerable
                {
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_nongeneric_enumerable_members>(
                            emit_mapped_type_helpers ? "_bindableIterableToEnumerable"
                            : "((global::System.Collections.IEnumerable)(IWinRTObject)this)")),
                        "ABI.System.Collections.IEnumerable",
                        "_bindableIterableToEnumerable"
                    };
                }
                else if (mapping->abi_name == "IBindableVector") // IList
                {
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_nongeneric_list_members>(
                            emit_mapped_type_helpers ? "_bindableVectorToList"
                            : "((global::System.Collections.IList)(IWinRTObject)this)",
                            true,
                            !emit_mapped_type_helpers)),
                        "ABI.System.Collections.IList",
                        "_bindableVectorToList"
                    };
                    remove_enumerable();
                }
                else if (mapping->mapped_name == "IDisposable")
                {
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_idisposable_members>(emit_mapped_type_helpers ? "As<global::ABI.System.IDisposable>()" : "((global::System.IDisposable)(IWinRTObject)this)",
                            !emit_mapped_type_helpers))
                    };
                }
                else if (mapping->mapped_name == "INotifyDataErrorInfo")
                {
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_notify_data_error_info_members_using_idic>(emit_mapped_type_helpers ? "As<global::ABI.System.ComponentModel.INotifyDataErrorInfo>()" : "((global::System.ComponentModel.INotifyDataErrorInfo)(IWinRTObject)this)",
                            !emit_mapped_type_helpers))
                    };
                }
                else
                {
                    mapping_written = false;
                }

                if (mapping_written)
                {
                    return;
                }
            }

            auto methods = w.write_temp("%",
            [&](writer& w)
            {
                for (auto&& method : iface.MethodList())
                {
                    if (!method.SpecialName())
                    {
                        std::string method_target;
                        if (settings.netstandard_compat)
                        {
                            method_target = w.write_temp("As<%>()", bind<write_type_name>(iface, typedef_name_type::ABI, false));
                        }
                        else
                        {
                            method_target = w.write_temp("((%)(IWinRTObject)this)", bind<write_type_name>(iface, typedef_name_type::Projected, false));
                        }
                        auto return_type = w.write_temp("%", bind<write_projection_return_type>(method_signature{ method }));
                        write_explicitly_implemented_method_for_abi(w, method, return_type, iface, method_target);
                    }
                }
                w.write_each<write_explicitly_implemented_property_for_abi>(iface.PropertyList(), iface, true);
                w.write_each<write_explicitly_implemented_event_for_abi>(iface.EventList(), iface, true);
            });
            required_interfaces[std::move(interface_name)] = { methods };
        };
        
        for (auto&& iface : type.InterfaceImpl())
        {
            for_typedef(w, get_type_semantics(iface.Interface()), [&](auto type)
            {
                if (has_attribute(iface, "Windows.Foundation.Metadata", "OverridableAttribute") || !is_exclusive_to(type))
                {
                    write_required_interface(type);
                    write_required_interface_members_for_abi_type(w, type, required_interfaces, emit_mapped_type_helpers);
                }
            });
        }
    }

    void write_guid(writer& w, TypeDef const& type, bool lowerCase)
    {
        auto attribute = get_attribute(type, "Windows.Foundation.Metadata", "GuidAttribute");
        if (!attribute)
        {
            throw_invalid("'Windows.Foundation.Metadata.GuidAttribute' attribute for type '", type.TypeNamespace(), ".", type.TypeName(), "' not found");
        }

        auto args = attribute.Value().FixedArgs();

        using std::get;

        auto get_arg = [&](decltype(args)::size_type index) { return get<ElemSig>(args[index].value).value; };

        w.write_printf(
            lowerCase ?
            R"(%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x)" :
            R"(%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X)",
            get<uint32_t>(get_arg(0)),
            get<uint16_t>(get_arg(1)),
            get<uint16_t>(get_arg(2)),
            get<uint8_t>(get_arg(3)),
            get<uint8_t>(get_arg(4)),
            get<uint8_t>(get_arg(5)),
            get<uint8_t>(get_arg(6)),
            get<uint8_t>(get_arg(7)),
            get<uint8_t>(get_arg(8)),
            get<uint8_t>(get_arg(9)),
            get<uint8_t>(get_arg(10)));
    }

    void write_guid_attribute(writer& w, TypeDef const& type)
    {
        auto fully_qualify_guid = (type.TypeNamespace() == "Windows.Foundation.Metadata");

        w.write(R"([%("%")])",
            fully_qualify_guid ? "global::System.Runtime.InteropServices.Guid" : "Guid",
            bind<write_guid>(type, false));
    }

    void write_guid_bytes(writer& w, TypeDef const& type)
    {
        auto attribute = get_attribute(type, "Windows.Foundation.Metadata", "GuidAttribute");
        if (!attribute)
        {
            throw_invalid("'Windows.Foundation.Metadata.GuidAttribute' attribute for type '", type.TypeNamespace(), ".", type.TypeName(), "' not found");
        }

        auto args = attribute.Value().FixedArgs();

        using std::get;

        auto get_arg = [&](decltype(args)::size_type index) { return get<ElemSig>(args[index].value).value; };

        w.write_printf(R"(0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X, 0x%X)",
            (get<uint32_t>(get_arg(0)) >> 0) & 0xFF,
            (get<uint32_t>(get_arg(0)) >> 8) & 0xFF,
            (get<uint32_t>(get_arg(0)) >> 16) & 0xFF,
            (get<uint32_t>(get_arg(0)) >> 24) & 0xFF,
            (get<uint16_t>(get_arg(1)) >> 0) & 0xFF,
            (get<uint16_t>(get_arg(1)) >> 8) & 0xFF,
            (get<uint16_t>(get_arg(2)) >> 0) & 0xFF,
            (get<uint16_t>(get_arg(2)) >> 8) & 0xFF,
            get<uint8_t>(get_arg(3)),
            get<uint8_t>(get_arg(4)),
            get<uint8_t>(get_arg(5)),
            get<uint8_t>(get_arg(6)),
            get<uint8_t>(get_arg(7)),
            get<uint8_t>(get_arg(8)),
            get<uint8_t>(get_arg(9)),
            get<uint8_t>(get_arg(10)));
    }

    void write_type_inheritance(writer& w, TypeDef const& type, type_semantics base_semantics, bool add_custom_qi, bool include_exclusive_interface)
    {
        auto delimiter{ " : " };
        auto write_delimiter = [&]()
        {
            w.write(delimiter);
            delimiter = ", ";
        };

        if (!std::holds_alternative<object_type>(base_semantics))
        {
            write_delimiter();
            write_projection_type(w, base_semantics);
        }

        for (auto&& iface : type.InterfaceImpl())
        {
            for_typedef(w, get_type_semantics(iface.Interface()), [&](auto type)
            {
                if (has_attribute(iface, "Windows.Foundation.Metadata", "OverridableAttribute") || !is_exclusive_to(type) || include_exclusive_interface)
                {
                    write_delimiter();
                    w.write("%", bind<write_type_name>(type, typedef_name_type::CCW, false));
                }
            });
        }

        if (add_custom_qi)
        {
            write_delimiter();
            w.write("global::System.Runtime.InteropServices.ICustomQueryInterface");
        }
    }
    
    std::string get_vmethod_delegate_type(writer& w, MethodDef const& method, std::string)
    {
        method_signature signature{ method };
        if (is_special(method))
        {
            bool getter = starts_with(method.Name(), "get_");
            bool setter = starts_with(method.Name(), "put_");
            if (getter || setter)
            {
                std::string suffix{};
                auto prop_type = getter ? signature.return_signature().Type() : signature.params()[0].second->Type();
                if (prop_type.is_szarray())
                {
                    return "";
                }
                call(get_type_semantics(prop_type),
                    [&](guid_type) { suffix = "Guid"; },
                    [&](type_type) { throw_invalid("System.Type not implemented"); },
                    [&](fundamental_type const& type) { suffix = get_delegate_type_suffix(type); },
                    [&](generic_type_index const& /*var*/) {},
                    [&](type_definition const& /*type*/) {},
                    [&](auto) { suffix = "Object"; });
                if (!suffix.empty())
                {
                    return w.write_temp("%_PropertyAs%", (getter ? "_get" : "_put"), suffix);
                }
            }
            else if (starts_with(method.Name(), "add_"))
            {
                return "_add_EventHandler";
            }
            else if (starts_with(method.Name(), "remove_"))
            {
                return "_remove_EventHandler";
            }
        }
        return "";
    }


    struct generic_abi_param
    {
        std::string abi_type;
        std::string generic_param;
        std::string param_name;
    };

    std::pair<std::string, std::string> get_generic_abi_type(writer& w, type_semantics semantics)
    {
        std::string generic_param{};
        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
        {
            auto generic_type_name = w.write_temp("%", bind<write_generic_type_name_base>(index));
            generic_param = generic_type_name + "Abi";
            w.write("Marshaler<%>.AbiType", generic_type_name);
        });
        auto generic_abi_type = w.write_temp("%", bind<write_abi_type>(semantics));
        return {generic_abi_type, generic_param};
    }

    auto get_generic_abi_types(writer& w, method_signature const& signature)
    {
        std::vector<std::pair<std::string, std::string>> generic_abi_types;
        auto add_generic_abi_type = [&](TypeSig sig, bool byref)
        {
            auto const [generic_abi_type, generic_param] = get_generic_abi_type(w, get_type_semantics(sig));
            generic_abi_types.push_back({w.write_temp(!generic_param.empty() ? "%%" : "typeof(%)%",
                generic_abi_type, byref ? (settings.netstandard_compat ? ".MakeByRefType()" : ".MakePointerType()") : ""), generic_param });
        };

        auto add_array_param = [&](param_category category)
        {
            XLANG_ASSERT(category > param_category::out);
            switch (category)
            {
            case param_category::pass_array:
                generic_abi_types.push_back({ "typeof(int)", "" });
                generic_abi_types.push_back({ "typeof(IntPtr)", "" });
                break;
            case param_category::fill_array:
                generic_abi_types.push_back({ "typeof(int)", "" });
                generic_abi_types.push_back({ "typeof(IntPtr).MakeByRefType()", "" });
                break;
            case param_category::receive_array:
                generic_abi_types.push_back({ "typeof(int).MakeByRefType()", "" });
                generic_abi_types.push_back({ "typeof(IntPtr).MakeByRefType()", "" });
                break;
            }
        };

        for (auto&& param : signature.params())
        {
            param_category category = get_param_category(param);
            if (category <= param_category::out)
            {
                add_generic_abi_type(param.second->Type(), category == param_category::out);
            }
            else
            {
                add_array_param(category);
            }
        }
        if (signature.return_signature())
        {
            if (!signature.return_signature().Type().is_szarray())
            {
                add_generic_abi_type(signature.return_signature().Type(), true);
            }
            else
            {
                add_array_param(param_category::receive_array);
            }
        }
        return generic_abi_types;
    }

    void write_abi_signature(writer& w, MethodDef const& method)
    {
        bool is_generic = distance(method.GenericParam()) > 0;
        method_signature signature{ method };
        auto generic_abi_types = get_generic_abi_types(w, signature);
        bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
            [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();

        if (!is_generic && !have_generic_params)
        {
            w.write("(%)", bind<write_abi_parameters>(signature));
            return;
        }
        w.write("(void* thisPtr");
        int index = 0;
        for (auto&& param : signature.params())
        {
            auto generic_type = generic_abi_types[index++].second;
            auto param_cat = get_param_category(param);
            if (!generic_type.empty() && (param_cat <= param_category::out))
            {
                std::string_view param_prefix = "";
                std::string_view param_suffix = "";
                
                if (param_cat == param_category::ref)
                {
                    if (settings.netstandard_compat)
                    {
                        param_prefix = "in ";
                    }
                    else
                    {
                        param_suffix = "*";
                    }
                }

                if (param_cat == param_category::out)
                {
                    if (settings.netstandard_compat)
                    {
                        param_prefix = "out ";
                    }
                    else
                    {
                        param_suffix = "*";
                    }
                }

                w.write(", %%% %",
                    param_prefix,
                    generic_type,
                    param_suffix,
                    bind<write_parameter_name>(param));
            }
            else
            {
                write_abi_parameter(w, param);
            }
        }
        if (auto return_sig = signature.return_signature())
        {
            auto generic_type = generic_abi_types[index++].second;
            if (!return_sig.Type().is_szarray() && !generic_type.empty())
            {
                if (settings.netstandard_compat)
                {
                    w.write(", out % %", generic_type, 
                        bind<write_escaped_identifier>(signature.return_param_name()));
                }
                else
                {
                    w.write(", %* %", generic_type, 
                        bind<write_escaped_identifier>(signature.return_param_name()));
                }
            }
            else
            {
                write_abi_return(w, signature);
            }
        }
        w.write(")");
    }

    struct managed_marshaler
    {
        std::string param_name;
        param_category category;
        std::string param_type;
        std::string local_type;
        std::string marshaler_type;
        bool abi_boxed;
        bool use_pointers;

        bool is_out() const
        {
            return (category == param_category::out) ||
                (category == param_category::receive_array);
        }

        bool is_ref() const
        {
            return (category == param_category::fill_array);
        }

        bool is_array() const
        {
            return category >= param_category::pass_array;
        }

        std::string get_param_local(writer& w) const
        {
            return w.write_temp("__%", param_name);
        }

        void write_local(writer& w) const
        {
            if ((category == param_category::in) || (category == param_category::pass_array))
                return;
            if (category == param_category::fill_array)
            {
                w.write("% __% = %.FromAbiArray((__%Size, %));\n",
                    local_type,
                    param_name,
                    marshaler_type,
                    param_name, bind<write_escaped_identifier>(param_name));
                return;
            }
            if (category == param_category::ref)
            {
                if (!use_pointers)
                {
                    w.write("var __% = %;\n",
                        param_name,
                        bind<write_escaped_identifier>(param_name));
                }
                return;
            }
            std::string_view out_local_type;
            if (param_type == "bool")
            {
                out_local_type = is_array() ? "bool[]" : "bool";
            }
            else if (param_type == "char")
            {
                out_local_type = is_array() ? "char[]" : "char";
            }
            else
            {
                out_local_type = local_type;
            }
            w.write("% __% = default;\n",
                out_local_type,
                param_name);
        }

        void write_out_initialize(writer& w) const
        {
            XLANG_ASSERT(is_out());
            if (!use_pointers)
            {
                w.write("% = default;\n", bind<write_escaped_identifier>(param_name));
                if (is_array())
                {
                    w.write("__%Size = default;\n", param_name);
                }
            }
            else
            {
                w.write("*% = default;\n", bind<write_escaped_identifier>(param_name));
                if (is_array())
                {
                    w.write("*__%Size = default;\n", param_name);
                }
            }
        }

        void write_marshal_to_managed(writer& w) const
        {
            if(is_out() || is_ref())
            {
                w.write("% __%", is_out() ? "out" : "", param_name);
            }
            else if (marshaler_type.empty())
            {
                std::string_view format_string;
                if (param_type == "bool")
                {
                    format_string = "% != 0";
                } 
                else if (param_type == "char")
                {
                    format_string = "(char)%";
                }
                else if (category == param_category::ref)
                {
                    if (!use_pointers)
                    {
                        format_string = "in __%";
                    }
                    else
                    {
                        format_string = "*%";
                    }
                }
                else
                {
                    format_string = "%";
                }
                w.write(format_string, bind<write_escaped_identifier>(param_name));
            }
            else if (is_array())
            {
                w.write("%.FromAbiArray((__%Size, %))",
                    marshaler_type,
                    param_name, bind<write_escaped_identifier>(param_name));
            }
            else
            {
                w.write("%.FromAbi(%%)",
                    marshaler_type,
                    category == param_category::ref ? use_pointers ? "*" : "__" : "",
                    bind<write_escaped_identifier>(param_name));
            }
        }

        void write_marshal_from_managed(writer& w) const
        {
            if (!is_ref() && (!is_out() || local_type.empty()))
                return;
            auto param_local = get_param_local(w);
            if (is_ref())
            {
                w.write("%.CopyManagedArray(%, %);\n",
                    marshaler_type,
                    param_local,
                    bind<write_escaped_identifier>(param_name));
                return;
            }
            if (!use_pointers)
            {
                is_array() ?
                    w.write("(__%Size, %) = ", param_name, bind<write_escaped_identifier>(param_name)) :
                    w.write("% = ", bind<write_escaped_identifier>(param_name));
            }
            else
            {
                is_array() ?
                    w.write("(*__%Size, *%) = ", param_name, bind<write_escaped_identifier>(param_name)) :
                    w.write("*% = ", bind<write_escaped_identifier>(param_name));
            }
            if (marshaler_type.empty())
            {
                if (local_type == "IntPtr")
                {
                    w.write("%.FromManaged(%);",
                        param_type,
                        param_local);
                }
                else
                {
                    if (param_type == "bool")
                    {
                        w.write("(byte)(% ? 1 : 0);", param_local);
                    }
                    else if (param_type == "char")
                    {
                        w.write("(ushort)%;", param_local);
                    }
                    else
                    {
                        w.write("%;", param_local);
                    }
                }
            }
            else
            {
                w.write("%%.FromManaged%(%);",
                    abi_boxed && !is_array() ?
                        w.write_temp("(%)", param_type) : "",
                    marshaler_type,
                    is_array() ? "Array" : "",
                    param_local);
            }
            w.write("\n");
        }
    };

    auto get_managed_marshalers(writer& w, method_signature const& signature, bool /*is_generic*/, bool is_generic_instantiation_class)
    {
        std::vector<managed_marshaler> marshalers;
        concurrency::concurrent_unordered_set<generic_type_instantiation> generic_instantiations;

        std::function<void(writer&, type_semantics const&, managed_marshaler&)> set_marshaler = 
            [&](writer& w, type_semantics const& semantics, managed_marshaler& m)
        {
            m.param_type = w.write_temp("%", bind<write_projection_type>(semantics));

            auto get_abi_type = [&]()
            {
                return w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::ABI, true));
            };

            auto set_typedef_marshaler = [&](TypeDef const& type)
            {
                switch (get_category(type))
                {
                case category::enum_type:
                    break;
                case category::struct_type:
                    if (!is_type_blittable(type))
                    {
                        if (!m.is_array())
                        {
                            m.marshaler_type = get_abi_type();
                        }
                        m.local_type = m.param_type;
                    }
                    break;
                case category::interface_type:
                    m.marshaler_type = w.write_temp("MarshalInterface<%>", m.param_type);
                    m.local_type = m.param_type;
                    break;
                case category::class_type:
                    m.marshaler_type = get_abi_type();
                    m.local_type = m.param_type;
                    break;
                case category::delegate_type:
                    m.marshaler_type = get_abi_type();
                    m.local_type = m.param_type;
                    break;
                }
            };

            call(semantics,
                [&](object_type const&)
                {
                    m.marshaler_type = "MarshalInspectable<object>";
                    m.local_type = "object";
                },
                [&](type_definition const& type)
                {
                    set_typedef_marshaler(type);
                },
                [&](generic_type_index const& var)
                {
                    if (is_generic_instantiation_class)
                    {
                        set_marshaler(w, w.get_generic_arg(var.index), m);
                    }
                    else
                    {
                        m.param_type = get_generic_abi_type(w, semantics).second;
                        m.local_type = w.write_temp("%", bind<write_projection_type>(semantics));
                        m.marshaler_type = w.write_temp("Marshaler<%>", m.local_type);
                        m.abi_boxed = true;
                    }
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    set_typedef_marshaler(type.generic_type);

                    if (!settings.netstandard_compat)
                    {
                        auto generic_instantiation_class_name = get_generic_instantiation_class_type_name(w, type.generic_type);
                        if (!starts_with(generic_instantiation_class_name, "Windows_Foundation_IReference"))
                        {
                            auto concrete_type = ConvertGenericTypeInstanceToConcreteType(w, type);

                            bool has_generic_type_param = false;
                            for (size_t idx = 0; idx < concrete_type.generic_args.size(); idx++)
                            {
                                auto& generic_arg_semantic = concrete_type.generic_args[idx];
                                if (auto gtp = std::get_if<generic_type_param>(&generic_arg_semantic))
                                {
                                    has_generic_type_param = true;
                                    break;
                                }
                            }

                            if (!has_generic_type_param)
                            {
                                generic_instantiations.insert(
                                    generic_type_instantiation
                                    {
                                        concrete_type,
                                        generic_instantiation_class_name
                                    });
                            }
                        }
                    }
                },
                [&](fundamental_type type)
                {
                    if (type == fundamental_type::String)
                    {
                        m.marshaler_type = "MarshalString";
                        m.local_type = m.is_out() ? "string" : "";
                    }
                },
                [&](auto const&) {});

            if ((m.is_out() || (m.category == param_category::ref)) && m.local_type.empty())
            {
                m.local_type = w.write_temp("%", bind<write_abi_type>(semantics));
            }
            if (m.is_array())
            {
                if (m.marshaler_type.empty())
                {
                    m.marshaler_type = is_type_blittable(semantics, true) ? "MarshalBlittable" : "MarshalNonBlittable";
                    m.marshaler_type += "<" + m.param_type + ">";
                }
                m.local_type = (m.local_type.empty() ? m.param_type : m.local_type) + "[]";
            }
            m.use_pointers = !settings.netstandard_compat;
        };

        for (auto&& param : signature.params())
        {
            managed_marshaler m{
                std::string(param.first.Name())
            };
            m.category = get_param_category(param);
            set_marshaler(w, get_type_semantics(param.second->Type()), m);
            marshalers.push_back(std::move(m));
        }

        if (auto ret = signature.return_signature())
        {
            managed_marshaler m{
                std::string(signature.return_param_name()),
                ret.Type().is_szarray() ? param_category::receive_array : param_category::out
            };
            set_marshaler(w, get_type_semantics(ret.Type()), m);
            return std::tuple{ marshalers, m, generic_instantiations };
        }

        return std::tuple{ marshalers, managed_marshaler{}, generic_instantiations };
    }

    void write_managed_method_call(writer& w, method_signature signature, std::string invoke_expression_format, bool is_generic_instantiation_class = false)
    {
        auto generic_abi_types = get_generic_abi_types(w, signature);
        bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
            [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();
        auto managed_marshalers = get_managed_marshalers(w, signature, have_generic_params, is_generic_instantiation_class);
        auto marshalers = std::get<0>(managed_marshalers);
        auto return_marshaler = std::get<1>(managed_marshalers);
        auto generic_instantiations = std::get<2>(managed_marshalers);
        auto return_sig = signature.return_signature();
        
        w.write(
R"(
%
%
try
{
%
%%
}
catch (Exception __exception__)
{
return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
}
return 0;)",
            [&](writer& w) {
                if (!return_sig) return;
                return_marshaler.write_local(w);
            },
            [&](writer& w) {
                w.write(bind_each([](writer& w, managed_marshaler const& m)
                {
                    if (m.is_out())
                    {
                        m.write_out_initialize(w);
                    }
                }, marshalers));
                if (return_sig)
                {
                    return_marshaler.write_out_initialize(w);
                }
                w.write(bind_each([](writer& w, managed_marshaler const& m)
                {
                    m.write_local(w);
                }, marshalers));
            },
            [&](writer& w)
            {
                if (return_sig)
                {
                    w.write("__% = ", return_marshaler.param_name);
                }

                w.write(R"(%;)",
                    bind([&](writer& w)
                    {
                        w.write(invoke_expression_format,
                            bind_list([](writer& w, managed_marshaler const& m)
                                {
                                    m.write_marshal_to_managed(w);
                                }, ", ", marshalers));
                    }));
            },
            bind_each([](writer& w, managed_marshaler const& m)
            { 
                m.write_marshal_from_managed(w);
            }, marshalers),
            [&](writer& w) {
                if (!return_sig) return;
                return_marshaler.write_marshal_from_managed(w);
            });
    }

    void write_method_abi_invoke(writer& w, MethodDef const& method)
    {
        if (method.SpecialName()) return;

        method_signature signature{ method };
        auto return_sig = signature.return_signature();
        auto type_name = write_type_name_temp(w, method.Parent());
        auto vmethod_name = get_vmethod_name(w, method.Parent(), method);

        auto generic_abi_types = get_generic_abi_types(w, signature);

        w.write(R"(
[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvMemberFunction) })]
private static unsafe int Do_Abi_%%
{
%
})",
vmethod_name,
bind<write_abi_signature>(method),
bind<write_managed_method_call>(
    signature,
    w.write_temp("ComInterfaceDispatch.GetInstance<%>((ComInterfaceDispatch*)thisPtr).%%",
        type_name,
        method.Name(),
        "(%)"), 
        false));
    }

    void write_method_abi_invoke_helper(writer& w, MethodDef const& method)
    {
        if (method.SpecialName()) return;

        method_signature signature{ method };
        auto return_sig = signature.return_signature();
        auto type_name = write_type_name_temp(w, method.Parent());
        auto vmethod_name = get_vmethod_name(w, method.Parent(), method);

        w.write(R"(
public static % Do_Abi_%(IntPtr thisPtr%%)
{
%global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr).%(%);
})",
bind<write_projection_return_type>(signature),
vmethod_name,
signature.has_params() ? ", " : "",
bind_list<write_projection_parameter>(", ", signature.params()),
return_sig ? "return " : "",
type_name,
method.Name(),
bind_list<write_parameter_name_with_modifier>(", ", signature.params()));
    }

    void write_property_abi_invoke(writer& w, Property const& prop)
    {
        auto [getter, setter] = get_property_methods(prop);
        auto type_name = write_type_name_temp(w, prop.Parent());
        auto generic_type = distance(prop.Parent().GenericParam()) > 0;
        if (setter)
        {
            method_signature setter_sig{ setter };
            auto vmethod_name = get_vmethod_name(w, setter.Parent(), setter);

            auto generic_abi_types = get_generic_abi_types(w, setter_sig);
            bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
                [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();

            // WinRT properties can't be indexers.
            XLANG_ASSERT(setter_sig.params().size() == 1);

        w.write(
            R"(
%
private static unsafe int Do_Abi_%%
{
%
})",
            !settings.netstandard_compat && !generic_type ? "[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]" : "",
            vmethod_name,
            bind<write_abi_signature>(setter),
            bind<write_managed_method_call>(
                setter_sig,
                w.write_temp("global::WinRT.ComWrappersSupport.FindObject<%>(%).% = %",
                    type_name,
                    have_generic_params ? "new IntPtr(thisPtr)" : "thisPtr",
                    prop.Name(),
                    "%"),
                false));
        }

        if (getter)
        {
            method_signature getter_sig{ getter };
            auto vmethod_name = get_vmethod_name(w, getter.Parent(), getter);

            auto generic_abi_types = get_generic_abi_types(w, getter_sig);
            bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
                [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();

            // WinRT properties can't be indexers.
            XLANG_ASSERT(getter_sig.params().size() == 0);
            w.write(
                R"(
%
private static unsafe int Do_Abi_%%
{
%
})",
                !settings.netstandard_compat && !generic_type ? "[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]" : "",
                vmethod_name,
                bind<write_abi_signature>(getter),
                bind<write_managed_method_call>(
                    getter_sig,
                    w.write_temp("global::WinRT.ComWrappersSupport.FindObject<%>(%).%%",
                        type_name,
                        have_generic_params ? "new IntPtr(thisPtr)" : "thisPtr",
                        prop.Name(),
                        "%"),
                    false));
        }
    }

    void write_property_abi_invoke_helper(writer& w, Property const& prop)
    {
        auto [getter, setter] = get_property_methods(prop);
        auto type_name = write_type_name_temp(w, prop.Parent());
        if (setter)
        {
            method_signature setter_sig{ setter };
            auto vmethod_name = get_vmethod_name(w, setter.Parent(), setter);

            // WinRT properties can't be indexers.
            XLANG_ASSERT(setter_sig.params().size() == 1);

            w.write(R"(
public static void Do_Abi_%(IntPtr thisPtr, %)
{
global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr).% = %;
})",
vmethod_name,
bind_list<write_projection_parameter>(", ", setter_sig.params()),
type_name,
prop.Name(),
bind_list<write_parameter_name_with_modifier>(", ", setter_sig.params()));
        }

        if (getter)
        {
            method_signature getter_sig{ getter };
            auto vmethod_name = get_vmethod_name(w, getter.Parent(), getter);

            // WinRT properties can't be indexers.
            XLANG_ASSERT(getter_sig.params().size() == 0);
            w.write(R"(
public static % Do_Abi_%(IntPtr thisPtr)
{
return global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr).%;
})",
bind<write_projection_return_type>(getter_sig),
vmethod_name,
type_name,
prop.Name());
        }
    }

    void write_event_abi_invoke(writer& w, Event const& evt)
    {
        auto type_name = write_type_name_temp(w, evt.Parent());
        auto generic_type = distance(evt.Parent().GenericParam()) > 0;
        auto semantics = get_type_semantics(evt.EventType());
        auto [add_method, remove_method] = get_event_methods(evt);
        auto add_signature = method_signature{ add_method };

        auto handler_parameter_name = add_signature.params().back().first.Name();
        auto add_handler_event_token_name = add_signature.return_param_name();
        auto remove_handler_event_token_name = method_signature{ remove_method }.params().back().first.Name();

        w.write(R"(
private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<%, global::WinRT.EventRegistrationTokenTable<%>> _%_tokenTables;
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<%, global::WinRT.EventRegistrationTokenTable<%>> Make%Table()
{
    global::System.Threading.Interlocked.CompareExchange(ref _%_tokenTables, new(), null);
    return _%_tokenTables;
}
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<%, global::WinRT.EventRegistrationTokenTable<%>> _%_TokenTables => _%_tokenTables ?? Make%Table();
)",
            type_name,
            bind<write_type_name>(semantics, typedef_name_type::Projected, false),
            evt.Name(),
            type_name,
            bind<write_type_name>(semantics, typedef_name_type::Projected, false),
            evt.Name(),
            evt.Name(),
            evt.Name(),
            type_name,
            bind<write_type_name>(semantics, typedef_name_type::Projected, false),
            evt.Name(),
            evt.Name(),
            evt.Name());

        w.write(
            R"(
%
private static unsafe int Do_Abi_%%
{
%
%% = default;
try
{
var __this = global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr);
var __handler = %.FromAbi(%);
%% = _%_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
__this.% += __handler;
return 0;
}
catch (Exception __ex)
{
return __ex.HResult;
}
})",
            !settings.netstandard_compat && !generic_type ? "[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]" : "",
            get_vmethod_name(w, add_method.Parent(), add_method),
            bind<write_abi_signature>(add_method),
            bind([&](writer& w) {
                if (!settings.netstandard_compat && !generic_type)
                {
                    write_ensure_generic_type_initialized(w, semantics, false);
                }
            }),
            settings.netstandard_compat ? "" : "*",
            add_handler_event_token_name,
            type_name,
            bind<write_type_name>(semantics, typedef_name_type::ABI, false),
            handler_parameter_name,
            settings.netstandard_compat ?  "" : "*",
            add_handler_event_token_name,
            evt.Name(),
            evt.Name());
        w.write(
    R"(
%
private static unsafe int Do_Abi_%%
{
try
{
var __this = global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr);
if(__this != null && _%_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(%, out var __handler))
{
__this.% -= __handler;
}
return 0;
}
catch (Exception __ex)
{
return __ex.HResult;
}
})",
            !settings.netstandard_compat && !generic_type ? "[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]" : "",
            get_vmethod_name(w, remove_method.Parent(), remove_method),
            bind<write_abi_signature>(remove_method),
            type_name,
            evt.Name(),
            remove_handler_event_token_name,
            evt.Name());
    }

    void write_event_abi_invoke_helper(writer& w, Event const& evt)
    {
        auto type_name = write_type_name_temp(w, evt.Parent());
        auto semantics = get_type_semantics(evt.EventType());
        auto [add_method, remove_method] = get_event_methods(evt);
        auto add_signature = method_signature{ add_method };

        w.write(R"(
private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<%, global::WinRT.EventRegistrationTokenTable<%>> _%_tokenTables;
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<%, global::WinRT.EventRegistrationTokenTable<%>> Make%TokenTable()
{
    global::System.Threading.Interlocked.CompareExchange(ref _%_tokenTables, new(), null);
    return _%_tokenTables;
}
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<%, global::WinRT.EventRegistrationTokenTable<%>> _%_TokenTables => _%_tokenTables ?? Make%TokenTable();
)",
        type_name,
        bind<write_type_name>(semantics, typedef_name_type::Projected, false),
        evt.Name(),
        type_name,
        bind<write_type_name>(semantics, typedef_name_type::Projected, false),
        evt.Name(),
        evt.Name(),
        evt.Name(),
        type_name,
        bind<write_type_name>(semantics, typedef_name_type::Projected, false),
        evt.Name(),
        evt.Name(),
        evt.Name());

        w.write(R"(
public static global::WinRT.EventRegistrationToken Do_Abi_%(IntPtr thisPtr, % handler)
{
var __this = global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr);
var token = _%_TokenTables.GetOrCreateValue(__this).AddEventHandler(handler);
__this.% += handler;
return token;
}
)",
            get_vmethod_name(w, add_method.Parent(), add_method),
            bind<write_projection_parameter_type>(add_signature.params().back()),
            type_name,
            evt.Name(),
            evt.Name());
        w.write(R"(
public static void Do_Abi_%(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
{
var __this = global::WinRT.ComWrappersSupport.FindObject<%>(thisPtr);
if(__this != null && _%_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
{
__this.% -= __handler;
}
}
)",
            get_vmethod_name(w, remove_method.Parent(), remove_method),
            type_name,
            evt.Name(),
            evt.Name());
    }

    void get_vtable_members(
        writer& w, 
        TypeDef const& type,
        std::vector<std::string>& nongeneric_delegates,
        std::vector<std::string>& method_marshals_to_abi,
        std::vector<std::string>& method_marshals_to_projection,
        std::vector<std::string>& vtable_delegates,
        std::vector<std::string>& method_create_function_pointers_to_projection,
        std::vector<std::string>& type_declarations,
        std::vector<std::string>& vtable_members)
    {
        auto nongenerics_class = w.write_temp("%_Delegates", bind<write_typedef_name>(type, typedef_name_type::ABI, false));
        auto methods = type.MethodList();
        auto is_generic = distance(type.GenericParam()) > 0;
        for (auto& method : methods)
        {
            bool signature_has_generic_parameters{};

            auto generic_abi_types = get_generic_abi_types(w, method_signature{ method });
            bool have_generic_type_parameters = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
                [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();

            auto vmethod_name = get_vmethod_name(w, type, method);
            auto delegate_type = get_vmethod_delegate_type(w, method, vmethod_name);
            std::string vtable_field_type;
            bool function_pointer = false;
            if (vtable_field_type == "")
            {
                delegate_type = nongenerics_class + "." + vmethod_name;
                writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
                    signature_has_generic_parameters = true;
                });

                auto delegate_definition = w.write_temp("public unsafe delegate int %(%);\n",
                    vmethod_name,
                    bind<write_abi_parameters>(method_signature{ method }));

                if (signature_has_generic_parameters)
                {
                    delegate_type = vtable_field_type = "global::System.Delegate";
                }
                else
                {
                    if (settings.netstandard_compat || is_generic)
                    {
                        nongeneric_delegates.push_back(delegate_definition);
                    }

                    vtable_field_type = w.write_temp("delegate* unmanaged[Stdcall]<%, int>", bind<write_abi_parameter_types>(method_signature{ method }));
                    function_pointer = true;
                }
            }
            else
            {
                // We're a well-known delegate type, but we still need to get the function pointer type.
                vtable_field_type = w.write_temp("delegate* unmanaged[Stdcall]<%, int>", bind<write_abi_parameter_types>(method_signature{ method }));
                function_pointer = true;
            }
            if (!function_pointer)
            {
                vtable_members.emplace_back(w.write_temp("public % %;", vtable_field_type, vmethod_name));
            }
            else if (settings.netstandard_compat || is_generic)
            {
                // Work around https://github.com/dotnet/runtime/issues/37295
                vtable_members.emplace_back(w.write_temp(R"(
private void* _%;
public % % { get => (%)_%; set => _%=(void*)value; }
)",
                    vmethod_name, vtable_field_type, vmethod_name, vtable_field_type, vmethod_name, vmethod_name));
            }
            else
            {
                // Work around C# compiler's lack of support for UnmanagedCallersOnly
                vtable_members.emplace_back(w.write_temp(R"(
private delegate* unmanaged[Stdcall]<%, int> _%;
public % % { get => (%)_%; set => _%=(delegate* unmanaged[Stdcall]<%, int>)value; }
)",
                    bind<write_abi_parameter_types_pointer>(method_signature{ method }), vmethod_name,
                    vtable_field_type, vmethod_name, vtable_field_type, vmethod_name, vmethod_name,
                    bind<write_abi_parameter_types_pointer>(method_signature{ method })));
            }
            uint32_t const delegate_cache_index = method.index() - methods.first.index();
            uint32_t const vtable_index = delegate_cache_index + 6;
            if (is_generic)
            {
                method_marshals_to_abi.emplace_back(signature_has_generic_parameters ?
                    w.write_temp("% = Marshal.GetDelegateForFunctionPointer(vftbl[%], %_Type);\n",
                        vmethod_name, vtable_index, vmethod_name) :
                    w.write_temp("% = (%)(vftbl[%]);\n",
                        vmethod_name, vtable_field_type, vtable_index)
                );

                method_marshals_to_projection.emplace_back(signature_has_generic_parameters ?
                    w.write_temp("nativeVftbl[%] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.%);\n",
                        vtable_index, vmethod_name) :
                    w.write_temp("nativeVftbl[%] = (IntPtr)AbiToProjectionVftable._%;", vtable_index, vmethod_name)
                );

                if (have_generic_type_parameters)
                {
                    auto create_delegate = 
                        w.write_temp(R"(global::System.Delegate.CreateDelegate(%, typeof(%).GetMethod("Do_Abi_%", BindingFlags.NonPublic | BindingFlags.Static)))",
                            !signature_has_generic_parameters ? w.write_temp("typeof(%)", vtable_field_type) : vmethod_name + "_Type",
                            bind<write_static_abi_class_generic_instantiation_type>(type),
                            vmethod_name);
                    vtable_delegates.emplace_back(create_delegate);

                    method_create_function_pointers_to_projection.emplace_back(
                        w.write_temp(R"(% = %%)",
                            vmethod_name,
                            !signature_has_generic_parameters ? w.write_temp("(%)", vtable_field_type) : "",
                            create_delegate));

                    if (signature_has_generic_parameters)
                    {
                        type_declarations.emplace_back(w.write_temp("Type %_Type = %(new Type[]{ typeof(void*), %typeof(int) });\n",
                            vmethod_name,
                            settings.netstandard_compat ? "global::WinRT.Projections.GetAbiDelegateType" : "Expression.GetDelegateType",
                            bind_each([&](writer& w, auto&& pair)
                            {
                                w.write("%, ", pair.first);
                            }, generic_abi_types)));
                    }
                }
                else
                {
                    auto create_delegate = w.write_temp("new %(Do_Abi_%)",
                        delegate_type,
                        vmethod_name);
                    vtable_delegates.emplace_back(create_delegate);
                    method_create_function_pointers_to_projection.emplace_back(
                        w.write_temp("_% = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[%] = %)",
                            vmethod_name,
                            delegate_cache_index,
                            create_delegate));
                }
            }
            else if (settings.netstandard_compat)
            {
                auto create_delegate = w.write_temp("new %(Do_Abi_%)",
                    delegate_type,
                    vmethod_name);
                vtable_delegates.emplace_back(create_delegate);
                method_create_function_pointers_to_projection.emplace_back(
                    w.write_temp("_% = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[%] = %)",
                        vmethod_name,
                        delegate_cache_index,
                        create_delegate));
            }
            else
            {
                // Work around C# compiler's lack of support for UnmanagedCallersOnly
                method_create_function_pointers_to_projection.emplace_back(
                    w.write_temp("_% = &Do_Abi_%",
                        vmethod_name, vmethod_name)
                );
            }
        }
    }

    void write_vtable(writer& w, TypeDef const& type, std::string const& type_name,
        std::string const& nongenerics_class,
        std::vector<std::string>& nongeneric_delegates)
    {
        auto methods = type.MethodList();
        auto is_generic = distance(type.GenericParam()) > 0;
        std::vector<std::string> method_marshals_to_abi;
        std::vector<std::string> method_marshals_to_projection;
        std::vector<std::string> method_create_delegates_to_projection;

        w.write(R"(%
public struct Vftbl
{
internal IInspectable.Vftbl IInspectableVftbl;
%%%%%%
})",
            bind<write_guid_attribute>(type),
            bind_each([&](writer& w, MethodDef const& method)
            {
                bool signature_has_generic_parameters{};

                auto generic_abi_types = get_generic_abi_types(w, method_signature{ method });
                bool have_generic_type_parameters = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
                    [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();

                auto vmethod_name = get_vmethod_name(w, type, method);
                auto delegate_type = get_vmethod_delegate_type(w, method, vmethod_name);
                std::string vtable_field_type;
                bool function_pointer = false;
                if(vtable_field_type == "")
                {
                    delegate_type = nongenerics_class + "." + vmethod_name;
                    writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
                        signature_has_generic_parameters = true;
                    });
                    auto delegate_definition = w.write_temp("public unsafe delegate int %(%);\n",
                        vmethod_name,
                        bind<write_abi_parameters>(method_signature{ method }));
                    if (signature_has_generic_parameters)
                    {
                        delegate_type = vtable_field_type = "global::System.Delegate";
                    }
                    else
                    {
                        if (settings.netstandard_compat || is_generic)
                        {
                            nongeneric_delegates.push_back(delegate_definition);
                        }

                        vtable_field_type = w.write_temp("delegate* unmanaged[Stdcall]<%, int>", bind<write_abi_parameter_types>(method_signature{ method }));
                        function_pointer = true;
                    }
                }
                else
                {
                    // We're a well-known delegate type, but we still need to get the function pointer type.
                    vtable_field_type = w.write_temp("delegate* unmanaged[Stdcall]<%, int>", bind<write_abi_parameter_types>(method_signature{ method }));
                    function_pointer = true;
                }
                if (!function_pointer)
                {
                    w.write("public % %;", vtable_field_type, vmethod_name);
                }
                else if (settings.netstandard_compat || is_generic)
                {
                    // Work around https://github.com/dotnet/runtime/issues/37295
                    w.write("private void* _%;\n", vmethod_name);
                    w.write("public % % { get => (%)_%; set => _%=(void*)value; }\n",
                        vtable_field_type, vmethod_name, vtable_field_type, vmethod_name, vmethod_name);
                }
                else
                {
                    // Work around C# compiler's lack of support for UnmanagedCallersOnly
                    w.write("private delegate* unmanaged[Stdcall]<%, int> _%;\n", bind<write_abi_parameter_types_pointer>(method_signature{ method }), vmethod_name);
                    w.write("public % % { get => (%)_%; set => _%=(delegate* unmanaged[Stdcall]<%, int>)value; }\n",
                        vtable_field_type, vmethod_name, vtable_field_type, vmethod_name, vmethod_name,
                        bind<write_abi_parameter_types_pointer>(method_signature{ method }));
                }
                uint32_t const delegate_cache_index = method.index() - methods.first.index();
                uint32_t const vtable_index = delegate_cache_index + 6;
                if (is_generic)
                {
                    method_marshals_to_abi.emplace_back(signature_has_generic_parameters ?
                        w.write_temp("% = Marshal.GetDelegateForFunctionPointer(vftbl[%], %_Type);\n",
                            vmethod_name, vtable_index, vmethod_name) :
                        w.write_temp("% = (%)(vftbl[%]);\n",
                            vmethod_name, vtable_field_type, vtable_index)
                        );

                    method_marshals_to_projection.emplace_back(signature_has_generic_parameters ?
                        w.write_temp("nativeVftbl[%] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.%);\n",
                            vtable_index, vmethod_name) :
                        w.write_temp("nativeVftbl[%] = (IntPtr)AbiToProjectionVftable._%;", vtable_index, vmethod_name)
                        );

                    if (have_generic_type_parameters)
                    {
                        method_create_delegates_to_projection.emplace_back(
                            w.write_temp(R"(% = %global::System.Delegate.CreateDelegate(%, typeof(Vftbl).GetMethod("Do_Abi_%", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(%)))",
                                vmethod_name,
                                !signature_has_generic_parameters ? w.write_temp("(%)", vtable_field_type) : "",
                                !signature_has_generic_parameters ? w.write_temp("typeof(%)", vtable_field_type) : vmethod_name + "_Type",
                                vmethod_name,
                                bind([&](writer& w, method_signature const& sig)
                                    {
                                        separator s{ w };
                                        auto write_abi_type = [&](writer& w, type_semantics type)
                                        {
                                            auto const [generic_abi_type, generic_type_parameter] = get_generic_abi_type(w, type);
                                            if (!generic_type_parameter.empty())
                                            {
                                                s();
                                                w.write(generic_abi_type);
                                            }
                                        };
                                        for (auto&& param : sig.params())
                                        {
                                            write_abi_type(w, get_type_semantics(param.second->Type()));
                                        }
                                        if (sig.return_signature())
                                        {
                                            write_abi_type(w, get_type_semantics(sig.return_signature().Type()));
                                        }
                                    }, method_signature{ method })));
                    }
                    else
                    {
                        method_create_delegates_to_projection.emplace_back(
                            w.write_temp("_% = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[%] = new %(Do_Abi_%))",
                                vmethod_name,
                                delegate_cache_index,
                                delegate_type,
                                vmethod_name));
                    }
                }
                else if (settings.netstandard_compat)
                {
                    method_create_delegates_to_projection.emplace_back(
                        w.write_temp("_% = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[%] = new %(Do_Abi_%))",
                            vmethod_name,
                            delegate_cache_index,
                            delegate_type,
                            vmethod_name));
                }
                else
                {
                    // Work around C# compiler's lack of support for UnmanagedCallersOnly
                    method_create_delegates_to_projection.emplace_back(
                        w.write_temp("_% = &Do_Abi_%",
                            vmethod_name, vmethod_name)
                    );
                }
            }, methods),
            [&](writer& w)
            {
                if (!is_generic) return;
                w.write("public static readonly Guid PIID = GuidGenerator.CreateIID(typeof(%));\n", type_name);
                w.write(R"(%
internal unsafe Vftbl(IntPtr thisPtr) : this()
{
var vftblPtr = *(void***)thisPtr;
var vftbl = (IntPtr*)vftblPtr;
IInspectableVftbl = *(IInspectable.Vftbl*)vftblPtr;
%}
)",
                    bind_each([&](writer& w, MethodDef const& method)
                    {
                        auto vmethod_name = get_vmethod_name(w, type, method);

                        if (abi_signature_has_generic_parameters(w, method_signature{ method }))
                        {
                            auto generic_abi_types = get_generic_abi_types(w, method_signature{ method });

                            w.write("public static readonly Type %_Type = %(new Type[]{ typeof(void*), %typeof(int) });\n",
                                vmethod_name,
                                settings.netstandard_compat ? "global::WinRT.Projections.GetAbiDelegateType" : "Expression.GetDelegateType",
                                bind_each([&](writer& w, auto&& pair)
                                {
                                    w.write("%, ", pair.first);
                                }, generic_abi_types));
                        }
                    }, methods),
                    bind_each(method_marshals_to_abi)
                );
            },
            bind([&](writer& w)
            {
                if (is_generic)
                {
                    w.write(R"(
private static readonly Vftbl AbiToProjectionVftable;
public static readonly IntPtr AbiToProjectionVftablePtr;
private static Delegate[] DelegateCache = new Delegate[%];
static unsafe Vftbl()
{
AbiToProjectionVftable = new Vftbl
{
IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
%
};
var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * %);
%
AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
}
)",
                        std::to_string(distance(methods)),
                        bind_list(",\n", method_create_delegates_to_projection),
                        std::to_string(distance(methods)),
                        bind([&](writer& w)
                            {
                                if (!is_generic)
                                {
                                    w.write("Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);");
                                }
                                else
                                {
                                    w.write("Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);\n");
                                    w.write("%", bind_each(method_marshals_to_projection));
                                }
                            }));
                }
                else
                {
                        w.write(R"(
public static readonly IntPtr AbiToProjectionVftablePtr;
%
static unsafe Vftbl()
{
AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * %);
(*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
{
IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
%
};
}
)",
                            bind([&](writer& w)
                            {   
                                if (settings.netstandard_compat)
                                {
                                    w.write("private static Delegate[] DelegateCache = new Delegate[%];", std::to_string(distance(methods)));
                                }
                            }),
                            std::to_string(distance(methods)),
                            bind_list(",\n", method_create_delegates_to_projection)
                            );
                    }
                }),
                bind_each<write_method_abi_invoke>(methods),
                bind_each<write_property_abi_invoke>(type.PropertyList()),
                bind_each<write_event_abi_invoke>(type.EventList())
        );
    }

    void write_base_constructor_dispatch_netstandard(writer& w, type_semantics type)
    {
        std::string base_default_interface_name;
        call(type,
            [&](object_type) {},
            [&](type_definition const& def)
            {
                base_default_interface_name = get_default_interface_name(w, def);
            },
            [&](generic_type_instance const& inst)
            {
                auto guard{ w.push_generic_args(inst) };
                base_default_interface_name = get_default_interface_name(w, inst.generic_type);
            },
                [](auto)
            {
                throw_invalid("Invalid base class type.");
            });

        if (!std::holds_alternative<object_type>(type))
        {
            w.write(R"(
    : base(ifc.As<%>())
)",
                base_default_interface_name);
        }
    }

    void write_base_constructor_dispatch(writer& w, type_semantics type)
    {
        if (!std::holds_alternative<object_type>(type))
        {
            w.write(R"(
    : base(global::WinRT.DerivedComposed.Instance)
)");
        }
    }

    void write_authoring_metadata_type(writer& w, TypeDef const& type)
    {
        w.write("%%%%internal class % {}\n",
            bind<write_winrt_attribute>(type),
            bind<write_winrt_exposed_type_attribute>(type, false),
            [&](writer& w)
            {
                auto category = get_category(type);
                if (category == category::delegate_type || category == category::struct_type)
                {
                    write_winrt_helper_type_attribute(w, type);
                }
            },
            bind<write_type_custom_attributes>(type, false),
            bind<write_type_name>(type, typedef_name_type::CCW, false));
    }

    void write_contract(writer& w, TypeDef const& type)
    {
        auto type_name = write_type_name_temp(w, type);
        w.write(R"(%% enum %
{
}
)",
            bind<write_type_custom_attributes>(type, false),
            internal_accessibility(),
            type_name);
    }

    void write_attribute(writer& w, TypeDef const& type)
    {
        auto type_name = write_type_name_temp(w, type);

        w.write(R"(%%% sealed class %: Attribute
{
%}
)",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, true),
            internal_accessibility(),
            type_name,
            [&](writer& w)
            {
                auto methods = type.MethodList();
                for (auto&& method : methods)
                {
                    if (method.Name() != ".ctor") continue;
                    method_signature signature{ method };
                    w.write("public %(%){}\n",
                        type_name,
                        bind_list<write_projection_parameter>(", ", signature.params()));
                }
                for (auto&& field : type.FieldList())
                {
                    w.write("public % %;\n",
                        bind<write_projection_type>(get_type_semantics(field.Signature().Type())),
                        field.Name());
                }
            });
    }

    void write_interface(writer& w, TypeDef const& type)
    {
        XLANG_ASSERT(get_category(type) == category::interface_type);
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::Projected);

        w.write(R"(
%
%
%% interface %%
{%
}
)",
            // Interface
            bind<write_winrt_metadata_attribute>(type),
            bind<write_guid_attribute>(type),
            bind<write_type_custom_attributes>(type, false),
            (is_exclusive_to(type) && !settings.public_exclusiveto) ? "internal" : "public",
            type_name,
            bind<write_type_inheritance>(type, object_type{}, false, false),
            bind<write_interface_member_signatures>(type)
        );
    }

    void write_generic_interface_impl_class(writer& w, TypeDef const& iface)
    {
        w.write(R"(
internal sealed class @Impl% : %, IWinRTObject
{
private IObjectReference _inner;

internal @Impl(IObjectReference _inner)
{
this._inner = _inner;
}

public static @Impl% CreateRcw(IInspectable obj) => new(obj.ObjRef);

%

IObjectReference IWinRTObject.NativeObject => _inner;

bool IWinRTObject.HasUnwrappableNativeObject => true;

private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
{
global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
return _queryInterfaceCache;
}
global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
{
global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
return _additionalTypeData;
}
global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();

%
}
)",
            iface.TypeName(),
            bind<write_type_params>(iface),
            bind<write_type_name>(iface, typedef_name_type::Projected, false),
            iface.TypeName(),
            iface.TypeName(),
            bind<write_type_params>(iface),
            [&](writer& w)
            {
                bool hasDerivedGenericInterfaces = distance(iface.GenericParam()) == 0 && has_derived_generic_interface(iface);
                std::set<std::string> writtenInterfaces;
                std::function<void(writer&, type_semantics const&)> write_objref_defintion = [&](writer& w, type_semantics const& ifaceTypeSemantics)
                {
                    auto objrefname = w.write_temp("%", bind<write_objref_type_name>(ifaceTypeSemantics));

                    // When writing derived interfaces of interfaces, we can sometimes encounter duplicate interfaces.
                    // To prevent writing them multiple times, we catch them here.
                    if (writtenInterfaces.find(objrefname) != writtenInterfaces.end())
                    {
                        return;
                    }
                    writtenInterfaces.insert(objrefname);

                    w.write(R"(
private volatile IObjectReference __%;
private IObjectReference Make__%()
{
%
global::System.Threading.Interlocked.CompareExchange(ref __%, ((IWinRTObject)this).NativeObject.As<IUnknownVftbl>(%.IID), null);
return __%;
}
private IObjectReference % => __% ?? Make__%();
)",
                        objrefname,
                        objrefname,
                        [&](writer& w) {
                            // We initialize the generic interface instantiation class if the respective interface for this
                            // impl class is not generic but has derived generic interfaces.  This is because in those cases
                            // we know the specific generic instantiations and can initialize them here rather than earlier.
                            // By deferring it to here, we are able to trim friendly allowing to trim unused interfaces.
                            if (hasDerivedGenericInterfaces)
                            {
                                write_ensure_generic_type_initialized(w, ifaceTypeSemantics);
                            }
                        },
                        objrefname,
                        bind<write_type_name>(ifaceTypeSemantics, typedef_name_type::StaticAbiClass, false),
                        objrefname,
                        objrefname,
                        objrefname,
                        objrefname);

                    for_typedef(w, ifaceTypeSemantics, [&](auto type)
                    {
                        for (auto&& ii : type.InterfaceImpl())
                        {
                            write_objref_defintion(w, get_type_semantics(ii.Interface()));
                        }
                    });
                };

                write_objref_defintion(w, iface);
            },
            bind<write_class_members>(iface, false, true));
    }

    void write_static_abi_classes(writer& w, TypeDef const& iface)
    {
        auto fast_abi_class_val = get_fast_abi_class_for_interface(iface);
        if (fast_abi_class_val.has_value())
        {
            if (fast_abi_class_val.value().contains_other_interface(iface))
            {
                return;
            }
        }

        w.write(R"(
% static class %
{
%
}
)",
        (is_exclusive_to(iface) && !settings.public_exclusiveto) ? "internal" : "public",
        bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
        [&](writer& w) {
            if (!fast_abi_class_val.has_value() || (!fast_abi_class_val.value().contains_other_interface(iface) && !interfaces_equal(fast_abi_class_val.value().default_interface, iface))) {
                write_static_abi_class_members(w, iface, INSPECTABLE_METHOD_COUNT, false);
                return;
            }
            auto abi_methods_start_index = INSPECTABLE_METHOD_COUNT;
            write_static_abi_class_members(w, fast_abi_class_val.value().default_interface, abi_methods_start_index, false);
            abi_methods_start_index += distance(fast_abi_class_val.value().default_interface.MethodList()) + get_class_hierarchy_index(fast_abi_class_val.value().class_type);
            for (auto&& other_iface : fast_abi_class_val.value().other_interfaces)
            {
                write_static_abi_class_members(w, other_iface, abi_methods_start_index, false);
                abi_methods_start_index += distance(other_iface.MethodList());
            }
        });
    }

    void write_static_abi_classes2(writer& w, TypeDef const& iface)
    {
        auto fast_abi_class_val = get_fast_abi_class_for_interface(iface);
        if (fast_abi_class_val.has_value())
        {
            if (fast_abi_class_val.value().contains_other_interface(iface))
            {
                return;
            }
        }

        if (settings.netstandard_compat)
        {
            w.write(R"(% static class %
{
internal static global::System.Guid IID { get; } = new Guid(new byte[] { % });

%
}
)",
                (is_exclusive_to(iface) || is_projection_internal(iface)) ? "internal" : internal_accessibility(),
                bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                bind<write_guid_bytes>(iface),
                [&](writer& w) {
                    if (!fast_abi_class_val.has_value() || (!fast_abi_class_val.value().contains_other_interface(iface) && !interfaces_equal(fast_abi_class_val.value().default_interface, iface))) {
                        write_static_abi_class_members(w, iface, INSPECTABLE_METHOD_COUNT);
                        return;
                    }
                    auto abi_methods_start_index = INSPECTABLE_METHOD_COUNT;
                    write_static_abi_class_members(w, fast_abi_class_val.value().default_interface, abi_methods_start_index);
                    abi_methods_start_index += distance(fast_abi_class_val.value().default_interface.MethodList()) + get_class_hierarchy_index(fast_abi_class_val.value().class_type);
                    for (auto&& other_iface : fast_abi_class_val.value().other_interfaces)
                    {
                        write_static_abi_class_members(w, other_iface, abi_methods_start_index);
                        abi_methods_start_index += distance(other_iface.MethodList());
                    }
                });
        }
        else
        {
            // Derived classes need access to the vftbl ptr if they are extending unsealed types.
            auto write_vftable_ptr = !is_exclusive_to(iface) || settings.public_exclusiveto;
            if (!write_vftable_ptr)
            {
                // Also write for both overridable interfaces and for default interfaces of authored types.
                auto exclusive_to_type = get_exclusive_to_type(iface);
                auto authored_component_interface = settings.component && settings.filter.includes(iface);
                if (!exclusive_to_type.Flags().Sealed() || authored_component_interface)
                {
                    for (auto&& iface_impl : exclusive_to_type.InterfaceImpl())
                    {
                        for_typedef(w, get_type_semantics(iface_impl.Interface()), [&](auto interface_type)
                        {
                            if (iface == interface_type &&
                                (is_overridable(iface_impl) || 
                                 (authored_component_interface && is_default_interface(iface_impl))))
                            {
                                write_vftable_ptr = true;
                            }
                        });

                        if (write_vftable_ptr)
                        {
                            break;
                        }
                    }
                }
            }

            bool is_generic = distance(iface.GenericParam()) > 0;

            w.write(R"(% static class %
{
%
%
%
}
)", 
            ((is_exclusive_to(iface) && !write_vftable_ptr)|| is_projection_internal(iface)) ? "internal" : internal_accessibility(),
            bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
            [&](writer& w) {
                if (is_generic)
                {
                    w.write(R"(
internal volatile static bool _RcwHelperInitialized;
unsafe static @Methods()
{
ComWrappersSupport.RegisterHelperType(typeof(%), typeof(%));
if (RuntimeFeature.IsDynamicCodeCompiled)
{
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Generic instantiations might not be available in AOT scenarios.")]
#endif
    [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "ABI types never have constructors.")]
    [UnconditionalSuppressMessage("Trimming", "IL2081", Justification = "ABI types never have constructors.")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void @MethodsFallback()
    {
        if (!_RcwHelperInitialized)
        {
            var ensureInitializedFallback = (Func<bool>)typeof(@Methods<%>).MakeGenericType(%).
            GetMethod("InitRcwHelperFallback", BindingFlags.NonPublic | BindingFlags.Static).
            CreateDelegate(typeof(Func<bool>));
            ensureInitializedFallback();
        }
    }

    @MethodsFallback();
}
}
)",
                    iface.TypeName(),
                    bind<write_type_name>(iface, typedef_name_type::Projected, false),
                    bind<write_type_name>(iface, typedef_name_type::ABI, false),
                    iface.TypeName(),
                    iface.TypeName(),
                    bind([&](writer& w)
                    {
                        for (auto i = 0; i < (distance(iface.GenericParam()) * 2) - 1; i++)
                        {
                            w.write(",");
                        }
                    }),
                    bind([&](writer& w)
                    {
                        for (auto i = 0; i < distance(iface.GenericParam()); i++)
                        {
                            if (i != 0)
                            {
                                w.write(", ");
                            }

                            w.write("typeof(%), Marshaler<%>.AbiType",
                                bind<write_generic_type_name>(i),
                                bind<write_generic_type_name>(i));
                        }
                    }),
                    iface.TypeName());
                }
                // If the interface inherits other generic interfaces, but itself isn't generic,
                // we need to handle the case where the class implementing the interface can be trimmed
                // and we end up relying on IDIC casts to access the generic interfaces.  But on AOT,
                // this doesn't work, which means we have a fallback helper impl class for the interface
                // that we instead create.
                else if (has_derived_generic_interface(iface))
                {
                    w.write(R"(
private volatile static bool _RcwHelperInitialized;
public static bool InitRcwHelper()
{
if (_RcwHelperInitialized)
{
return true;
}

global::WinRT.ComWrappersSupport.RegisterTypedRcwFactory(typeof(%), @Impl.CreateRcw);
_RcwHelperInitialized = true;
return true;
}
)",
                        bind<write_type_name>(iface, typedef_name_type::Projected, false),
                        iface.TypeName());
                }
            },
            [&](writer& w) {
                if (!fast_abi_class_val.has_value() || (!fast_abi_class_val.value().contains_other_interface(iface) && !interfaces_equal(fast_abi_class_val.value().default_interface, iface))) {
                    write_static_abi_class_members(w, iface, INSPECTABLE_METHOD_COUNT, false);
                    return;
                }
                auto abi_methods_start_index = INSPECTABLE_METHOD_COUNT;
                write_static_abi_class_members(w, fast_abi_class_val.value().default_interface, abi_methods_start_index, false);
                abi_methods_start_index += distance(fast_abi_class_val.value().default_interface.MethodList()) + get_class_hierarchy_index(fast_abi_class_val.value().class_type);
                for (auto&& other_iface : fast_abi_class_val.value().other_interfaces)
                {
                    write_static_abi_class_members(w, other_iface, abi_methods_start_index, false);
                    abi_methods_start_index += distance(other_iface.MethodList());
                }
            },
            [&](writer& w) {
                if (is_generic)
                {
                        w.write(R"(
public static global::System.Guid IID => GuidGenerator.CreateIID(typeof(%));
)",
                        bind<write_type_name>(iface, typedef_name_type::ABI, false));
                }
                else
                {
                    w.write(R"(
public static ref readonly global::System.Guid IID
{
[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
get
{
global::System.ReadOnlySpan<byte> data = new byte[] { % };
return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(data));
}
}
)",
                        bind<write_guid_bytes>(iface));
                }

                if (write_vftable_ptr)
                {
                    if (is_generic)
                    {
                        w.write(R"(
private static global::System.IntPtr abiToProjectionVftablePtr;
public static global::System.IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

internal static bool TryInitCCWVtable(global::System.IntPtr ptr)
{
return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, global::System.IntPtr.Zero) == global::System.IntPtr.Zero;
}

%
%
%
)",
                        bind_each<write_method_abi_invoke_helper>(iface.MethodList()),
                        bind_each<write_property_abi_invoke_helper>(iface.PropertyList()),
                        bind_each<write_event_abi_invoke_helper>(iface.EventList()));
                    }
                    else
                    {
                        w.write(R"(
public static global::System.IntPtr AbiToProjectionVftablePtr => %.AbiToProjectionVftablePtr;
)",
                            bind<write_type_name>(iface, typedef_name_type::ABI, false));
                    }
                }
            });

            if (is_generic)
            {
                int32_t num_methods = distance(iface.MethodList());
                uint32_t index = 0;

                std::vector<std::string> nongeneric_delegates;
                std::vector<std::string> method_marshals_to_abi;
                std::vector<std::string> method_marshals_to_projection;
                std::vector<std::string> vtable_delegates;
                std::vector<std::string> method_create_function_pointers_to_projection;
                std::vector<std::string> type_declarations;
                std::vector<std::string> vtable_members;
                get_vtable_members(
                    w,
                    iface,
                    nongeneric_delegates,
                    method_marshals_to_abi,
                    method_marshals_to_projection,
                    vtable_delegates,
                    method_create_function_pointers_to_projection,
                    type_declarations,
                    vtable_members);

                w.write(R"(
% static class %%
{
public unsafe static bool InitRcwHelper(%)
{
if (%._RcwHelperInitialized)
{
return true;
}

%
global::WinRT.ComWrappersSupport.RegisterTypedRcwFactory(typeof(%), @Impl%.CreateRcw);
%._RcwHelperInitialized = true;
return true;
}

private unsafe static bool InitRcwHelperFallback()
{
return InitRcwHelper(%);
}


%

public static unsafe bool InitCcw(
%
)
{
if (%.AbiToProjectionVftablePtr != default)
{
return false;
}

var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * %));
*(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
%

if (!%.TryInitCCWVtable(abiToProjectionVftablePtr))
{
NativeMemory.Free((void*)abiToProjectionVftablePtr);
return false;
}

return true;
}

private static global::System.Delegate[] DelegateCache;

#if NET8_0_OR_GREATER
[RequiresDynamicCode("The necessary marshalling code or generic instantiations might not be available.")]
#endif
internal static unsafe void InitFallbackCCWVtable()
{
%
DelegateCache = new global::System.Delegate[]
{
%
};

var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * %));
*(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
%

if (!%.TryInitCCWVtable(abiToProjectionVftablePtr))
{
NativeMemory.Free((void*)abiToProjectionVftablePtr);
}
}

%
%
%
}

)",
                    ((is_exclusive_to(iface) && !write_vftable_ptr) || is_projection_internal(iface)) ? "internal" : internal_accessibility(),
                    bind([&](writer& w) {
                        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
                        {
                            write_projection_type_for_name_type(w, w.get_generic_arg_scope(index).first, typedef_name_type::Projected);
                            w.write(", ");
                            write_projection_type_for_name_type(w, w.get_generic_arg_scope(index).first, typedef_name_type::Projected);
                            w.write("Abi");
                        });

                        w.write("%", bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false));
                    }),
                    bind_each([&](writer& w, GenericParam const& /*gp*/)
                    {
                        w.write(" where ");
                        write_generic_type_name(w, index++);
                        w.write("Abi : unmanaged");
                    }, iface.GenericParam()),
                    [&](writer& w) {
                        bool write_delimiter = false;
                        for (auto& method : iface.MethodList())
                        {
                            method_signature signature{ method };
                            if (!settings.netstandard_compat && 
                                !(is_special(method) && (starts_with(method.Name(), "add_") || starts_with(method.Name(), "remove_"))) &&
                                projected_signature_has_generic_parameters(w, signature))
                            {
                                if (write_delimiter)
                                {
                                    w.write(",\n");
                                }

                                write_generic_method_delegate_variable(w, method, signature, true);
                                write_delimiter = true;
                            }
                        }
                    },
                    bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                    bind_each([&](writer& w, MethodDef const& method)
                    {
                        method_signature signature{ method };
                        if (!settings.netstandard_compat && 
                            !(is_special(method) && (starts_with(method.Name(), "add_") || starts_with(method.Name(), "remove_"))) &&
                            projected_signature_has_generic_parameters(w, signature))
                        {
                            w.write("%._% = %;\n",
                                bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                                method.Name(),
                                method.Name());
                        }
                    }, iface.MethodList()),
                    bind<write_type_name>(iface, typedef_name_type::Projected, false),
                    iface.TypeName(),
                    bind<write_type_params>(iface),
                    bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                    [&](writer& w) {
                        bool write_delimiter = false;
                        for (auto& method : iface.MethodList())
                        {
                            method_signature signature{ method };
                            if (!settings.netstandard_compat &&
                                !(is_special(method) && (starts_with(method.Name(), "add_") || starts_with(method.Name(), "remove_"))) &&
                                projected_signature_has_generic_parameters(w, signature))
                            {
                                if (write_delimiter)
                                {
                                    w.write(",\n");
                                }

                                bool signature_has_only_generic_return =
                                    !settings.netstandard_compat &&
                                    abi_signature_has_generic_parameters(w, signature) &&
                                    !abi_signature_without_return_has_generic_parameters(w, signature);
                                if (signature_has_only_generic_return)
                                {
                                    w.write("&%", method.Name());
                                }
                                else
                                {
                                    w.write("null");
                                }

                                write_delimiter = true;
                            }
                        }
                    },
                    [&](writer& w) {
                        if (!fast_abi_class_val.has_value() || (!fast_abi_class_val.value().contains_other_interface(iface) && !interfaces_equal(fast_abi_class_val.value().default_interface, iface))) {
                            write_static_abi_class_members(w, iface, INSPECTABLE_METHOD_COUNT, true);
                            return;
                        }
                        auto abi_methods_start_index = INSPECTABLE_METHOD_COUNT;
                        write_static_abi_class_members(w, fast_abi_class_val.value().default_interface, abi_methods_start_index, true);
                        abi_methods_start_index += distance(fast_abi_class_val.value().default_interface.MethodList()) + get_class_hierarchy_index(fast_abi_class_val.value().class_type);
                        for (auto&& other_iface : fast_abi_class_val.value().other_interfaces)
                        {
                            write_static_abi_class_members(w, other_iface, abi_methods_start_index, true);
                            abi_methods_start_index += distance(other_iface.MethodList());
                        }
                    },
                    bind_list([&](writer& w, MethodDef const& method)
                    {
                        w.write("% %",
                            bind<write_abi_delegate_parameter_types_pointer>(method),
                            method.Name());
                    }, ",\n", iface.MethodList()),
                    bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                    num_methods,
                    bind_list([&](writer& w, MethodDef const& method)
                    {
                        auto method_index = get_vmethod_index(method.Parent(), method);
                        auto method_name = method.Name();
                        w.write("((%*)abiToProjectionVftablePtr)[%] = %;",
                            bind<write_abi_delegate_parameter_types_pointer>(method),
                            method_index + 6 /* number of entries in IInspectable */,
                            method_name);
                    }, "\n", iface.MethodList()),
                    bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                    bind_list(",\n", type_declarations),
                    bind_list(",\n", vtable_delegates),
                    num_methods,
                    bind_list([&](writer& w, MethodDef const& method)
                    {
                        auto method_index = get_vmethod_index(method.Parent(), method);
                        w.write("((IntPtr*)abiToProjectionVftablePtr)[%] = Marshal.GetFunctionPointerForDelegate(DelegateCache[%]);",
                            method_index + 6 /* number of entries in IInspectable */,
                            method_index);
                    }, "\n", iface.MethodList()),
                    bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false),
                    bind_each<write_method_abi_invoke>(iface.MethodList()),
                    bind_each<write_property_abi_invoke>(iface.PropertyList()),
                    bind_each<write_event_abi_invoke>(iface.EventList())
                );

                write_generic_interface_impl_class(w, iface);
            }
            else if (has_derived_generic_interface(iface))
            {
                write_generic_interface_impl_class(w, iface);
            }
        }
    }

    void write_interface_vftbl(writer& w, TypeDef const& type)
    {
        w.write(R"(
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct %Vftbl
{
public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
public delegate* unmanaged[MemberFunction]<void*, uint> Release;
public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int> GetIids;
public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;
public delegate* unmanaged[MemberFunction]<void*, int*, int> GetTrustLevel;
%
}
)",
            type.TypeName(),
			bind_each([&](writer& w, MethodDef const& method)
            {
                auto vmethod_name = get_vmethod_name(w, type, method);
                w.write("public delegate* unmanaged[MemberFunction]<%, int> %;\n",
                    bind<write_abi_parameter_types_pointer>(method_signature{ method }),
                    vmethod_name);
            }, type.MethodList()));
    }

    void write_interface_impl(writer& w, TypeDef const& type)
    {
		w.write(R"(
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
public static unsafe class %Impl
{
[FixedAddressValueType]
private static readonly %Vftbl Vftbl;

static %Impl()
{
    *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;
    %
}

public static ref readonly global::System.Guid IID
{
[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
get
{
global::System.ReadOnlySpan<byte> data = new byte[] { % };
return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(data));
}
}

public static nint Vtable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => (nint)Unsafe.AsPointer(in Vftbl);
}

%
%
%
}
)",
        type.TypeName(),
		type.TypeName(),
        // static ctor
        type.TypeName(),
        bind_each([&](writer& w, MethodDef const& method)
        {
            auto vmethod_name = get_vmethod_name(w, type, method);
            w.write("Vftbl.% = &Do_Abi_%;\n",
                vmethod_name,
                vmethod_name);
        }, type.MethodList()),
        // IID
        bind<write_guid_bytes>(type),
        // Vtable functions
        bind_each<write_method_abi_invoke>(type.MethodList()),
        bind_each<write_property_abi_invoke>(type.PropertyList()),
        bind_each<write_event_abi_invoke>(type.EventList())
);
    }

    void write_interface_idic_impl(writer& w, TypeDef const& type)
    {
        w.write(R"(
[DynamicInterfaceCastableImplementation]
file interface % : %
{
%
}
)",
            type.TypeName(),
            bind<write_type_name>(type, typedef_name_type::Projected, false),
            bind<write_interface_members>(type));
    }

    bool write_abi_interface(writer& w, TypeDef const& type)
    {
        bool is_generic = distance(type.GenericParam()) > 0;
        XLANG_ASSERT(get_category(type) == category::interface_type);
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::ABI);

        bool shouldOmitCcwCodegen = false;

        // For exclusive interfaces which aren't overridable interfaces that are implemented by unsealed types,
        // we do not need any of the Do_Abi functions or the vtable logic as we will not create CCWs for them.
        // But we are still keeping the interface itself for any helper type lookup that may happen for like GUID lookup.
        // We avoid this path if we want to generate IDIC implementations for them though.
        if (!is_generic &&
            (is_exclusive_to(type) && !settings.public_exclusiveto) &&
            // check for !authored type
            !(settings.component && settings.filter.includes(type)))
        {
            bool hasOverridableAttribute = false;
            auto exclusive_to_type = get_exclusive_to_type(type);
            for (auto&& iface : exclusive_to_type.InterfaceImpl())
            {
                for_typedef(w, get_type_semantics(iface.Interface()), [&](auto interface_type)
                {
                    if (type == interface_type && is_overridable(iface))
                    {
                        hasOverridableAttribute = true;
                    }
                });

                if (hasOverridableAttribute)
                {
                    break;
                }
            }

            if (!hasOverridableAttribute)
            {
                // Under normal conditions, we would stop here and just emit a minimal amount of code.
                // However, if IDIC is requested, just continue normally, but omit the CCW generation.
                // We know we can safely omit that because it wouldn't have normally be generated.
                if (settings.idic_exclusiveto)
                {
                    shouldOmitCcwCodegen = true;
                }
                else
                {
                    // Otherwise, just write the minimal non-IDIC interface
                    w.write(R"(%
internal interface % : %
{
}
)",
                    bind<write_guid_attribute>(type),
                    type_name,
                    bind<write_type_name>(type, typedef_name_type::CCW, false));
                    
                    return true;
                }
            }
        }

        auto nongenerics_class = w.write_temp("%_Delegates", bind<write_typedef_name>(type, typedef_name_type::ABI, false));

        std::string nongeneric_delegates = "";

        std::map<std::string, required_interface> required_interfaces;
        write_required_interface_members_for_abi_type(w, type, required_interfaces, false);

        w.write(R"(%
%
internal unsafe interface % : %
{
%%%%}
)",
            // Interface abi implementation
            is_exclusive_to(type) && !settings.idic_exclusiveto ? "" : "[DynamicInterfaceCastableImplementation]",
            bind<write_guid_attribute>(type),
            type_name,
            bind<write_type_name>(type, does_abi_interface_implement_ccw_interface(type) ? typedef_name_type::CCW : typedef_name_type::Projected, false),
            // Vftbl
            bind([&](writer& w)
            {
                if (shouldOmitCcwCodegen)
                {
                    return;
                }

                auto methods = type.MethodList();
                if (is_generic)
                {
                    w.write(R"(
public static readonly Guid PIID = %.IID;
public static readonly IntPtr AbiToProjectionVftablePtr;
static unsafe @()
{
if (RuntimeFeature.IsDynamicCodeCompiled)
{
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Generic instantiations might not be available in AOT scenarios.")]
#endif
    [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "ABI types never have constructors.")]
    [UnconditionalSuppressMessage("Trimming", "IL2081", Justification = "ABI types never have constructors.")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void @Fallback()
    {
        if (%.AbiToProjectionVftablePtr == default)
        {
            var initFallbackCCWVtable = (Action)typeof(@Methods<%>).MakeGenericType(%).
            GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
            CreateDelegate(typeof(Action));
            initFallbackCCWVtable();
        }
    }

    @Fallback();
}

AbiToProjectionVftablePtr = %.AbiToProjectionVftablePtr;
}

%
public unsafe struct Vftbl
{
internal IInspectable.Vftbl IInspectableVftbl;

public static readonly IntPtr AbiToProjectionVftablePtr = %.AbiToProjectionVftablePtr;

public static readonly Guid PIID = %.IID;
}
)",
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        type.TypeName(),
                        type.TypeName(),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        type.TypeName(),
                        bind([&](writer& w)
                        {
                            for (auto i = 0; i < (distance(type.GenericParam()) * 2) - 1; i++)
                            {
                                w.write(",");
                            }
                        }),
                        bind([&](writer& w)
                        {
                            for (auto i = 0; i < distance(type.GenericParam()); i++)
                            {
                                if (i != 0)
                                {
                                    w.write(", ");
                                }

                                w.write("typeof(%), Marshaler<%>.AbiType",
                                    bind<write_generic_type_name>(i),
                                    bind<write_generic_type_name>(i));
                            }
                        }),
                        type.TypeName(),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        bind<write_guid_attribute>(type),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false));

                    nongeneric_delegates = 
                        w.write_temp("%",
                            bind_each([&](writer& w, MethodDef const& method) {
                                bool signature_has_generic_parameters = false;
                                writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
                                    signature_has_generic_parameters = true;
                                });

                                auto delegate_definition = w.write_temp("public unsafe delegate int %(%);\n",
                                    get_vmethod_name(w, type, method),
                                    bind<write_abi_parameters>(method_signature{ method }));
                                if (!signature_has_generic_parameters)
                                {
                                    w.write(delegate_definition);
                                }
                            }, methods));
                }
                else
                {
                    w.write(R"(
public static readonly IntPtr AbiToProjectionVftablePtr;
static unsafe @()
{
AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(@), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * %);
*(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
%
}
%%%
)",
                    type.TypeName(),
                    type.TypeName(),
                    distance(methods),
                    bind_list([&](writer& w, MethodDef const& method)
                    {
                        auto method_index = get_vmethod_index(method.Parent(), method);
                        auto method_name = method.Name();
                        w.write("((delegate* unmanaged[Stdcall]<%, int>*)AbiToProjectionVftablePtr)[%] = &Do_Abi_%_%;",
                            bind<write_abi_parameter_types_pointer>(method_signature{ method }),
                            method_index + 6 /* number of entries in IInspectable */,
                            method_name,
                            method_index);
                    }, "\n", methods),
                    bind_each<write_method_abi_invoke>(methods),
                    bind_each<write_property_abi_invoke>(type.PropertyList()),
                    bind_each<write_event_abi_invoke>(type.EventList()));
                }
            }),
            bind<write_interface_members>(type),
            "",
            [&](writer& w) {
                if (!is_exclusive_to(type) || settings.idic_exclusiveto)
                {
                    for (auto required_interface : required_interfaces)
                    {
                        w.write("%", required_interface.second.members);
                    }
                }
            }
        );

        if (nongeneric_delegates.length() != 0)
        {
            w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal static class %
{
%}
)",
nongenerics_class,
nongeneric_delegates);
        }
        w.write("\n");

        return true;
    }

    void write_custom_query_interface_impl(writer& w, TypeDef const& type)
    {

        bool has_base_class = !std::holds_alternative<object_type>(get_type_semantics(type.Extends()));
        separator s{ w, " || " };
        w.write(R"(
%bool IsOverridableInterface(Guid iid) => %%;

global::System.Runtime.InteropServices.CustomQueryInterfaceResult global::System.Runtime.InteropServices.ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
{
ppv = IntPtr.Zero;
if (IsOverridableInterface(iid) || global::WinRT.Interop.IID.IID_IInspectable == iid || global::WinRT.Interop.IID.IID_IWeakReferenceSource == iid)
{
return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
}

if (%.TryAs(iid, out ppv) >= 0)
{
return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.Handled;
}

return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
})",
            bind([&](writer& w)
            {
                auto visibility = "protected ";
                auto overridable = "virtual ";
                if (has_base_class)
                {
                    overridable = "override ";
                }
                else if (type.Flags().Sealed())
                {
                    visibility = "private ";
                    overridable = "";
                }
                w.write(visibility);
                w.write(overridable);
            }),
            bind_each([&](writer& w, InterfaceImpl const& iface)
            {
                if (has_attribute(iface, "Windows.Foundation.Metadata", "OverridableAttribute"))
                {
                    s();
                    settings.netstandard_compat ?
                        w.write("GuidGenerator.GetIID(typeof(%)) == iid",
                            bind<write_type_name>(get_type_semantics(iface.Interface()), typedef_name_type::ABI, false)) :
                        w.write("%.IID == iid",
                            bind<write_type_name>(get_type_semantics(iface.Interface()), typedef_name_type::StaticAbiClass, true));
                }
            }, type.InterfaceImpl()),
            bind([&](writer& w)
            {
                if (has_base_class)
                {
                    s();
                    w.write("base.IsOverridableInterface(iid)");
                }
                if (s.first)
                {
                    w.write("false");
                }
            }),
            settings.netstandard_compat ? "GetReferenceForQI()" : "((IWinRTObject)this).NativeObject");
    }

    void write_wrapper_class(writer& w, TypeDef const& type)
    {
        if (is_static(type))
        {
            return;
        }

        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::CCW);
        auto wrapped_type_name = write_type_name_temp(w, type, "%", typedef_name_type::Projected);
        auto default_interface_name = get_default_interface_name(w, type, false, true);

        if (settings.netstandard_compat)
        {
            auto base_semantics = get_type_semantics(type.Extends());
            auto from_abi_new = !std::holds_alternative<object_type>(base_semantics) ? "new " : "";

            // Fallback path for .NET Standard, with all interfaces of the user defined type implemented
            // on the authoring metadata type as well. This is because on this target, CsWinRT will go
            // through the list of implemented interfaces to construct the CCW and do other things. In
            // theory, the type could be abstract and without actually providing an implementation of the
            // interfaces it's declaring, but given this is a fallback path for backwards compatibility,
            // it's simpler to just keep the existing code without any changes, to minimize risk.
            w.write(R"(%%[global::WinRT.ProjectedRuntimeClass(typeof(%))]
%internal % partial class %%
{
public %(% comp)
{
_comp = comp;
}
public static implicit operator %(% comp)
{
return comp._comp;
}
public static implicit operator %(% comp)
{
return new %(comp);
}
public static %% FromAbi(IntPtr thisPtr)
{
if (thisPtr == IntPtr.Zero) return null;
return MarshalInspectable<%>.FromAbi(thisPtr);
}
%
private readonly % _comp;
}
)",
                bind<write_winrt_attribute>(type),
                bind<write_winrt_helper_type_attribute>(type),
                default_interface_name,
                bind<write_type_custom_attributes>(type, false),
                bind<write_class_modifiers>(type),
                type_name,
                bind<write_type_inheritance>(type, base_semantics, false, true),
                type_name,
                wrapped_type_name,
                wrapped_type_name,
                type_name,
                type_name,
                wrapped_type_name,
                type_name,
                from_abi_new,
                wrapped_type_name,
                wrapped_type_name,
                bind<write_class_members>(type, true, false),
                wrapped_type_name);
        }
        else
        {
            // This type can be empty, as it is only used for metadata lookup, but not as implementation.
            // On modern .NET, we use [WinRTExposedType] to get all implemented interfaces for the vtable.
            w.write(R"(%%[global::WinRT.ProjectedRuntimeClass(typeof(%))]
%internal % partial class %
{
public static % FromAbi(IntPtr thisPtr)
{
if (thisPtr == IntPtr.Zero) return null;
return MarshalInspectable<%>.FromAbi(thisPtr);
}
}
)",
                bind<write_winrt_attribute>(type),
                bind<write_winrt_helper_type_attribute>(type),
                default_interface_name,
                bind<write_type_custom_attributes>(type, false),
                bind<write_class_modifiers>(type),
                type_name,
                wrapped_type_name,
                wrapped_type_name);
        }
    }

    void write_class_netstandard(writer& w, TypeDef const& type)
    {
        if (settings.component)
        {
            write_wrapper_class(w, type);
            return;
        }

        if (is_static(type))
        {
            write_static_class(w, type);
            return;
        }

        auto type_name = write_type_name_temp(w, type);
        auto default_interface_name = get_default_interface_name(w, type, false);
        auto default_interface_abi_name = get_default_interface_name(w, type, true);
        auto base_semantics = get_type_semantics(type.Extends());
        auto derived_new = std::holds_alternative<object_type>(base_semantics) ? "" : "new ";

        auto gc_pressure_amount = get_gc_pressure_amount(type);

        w.write(R"(%%[global::WinRT.ProjectedRuntimeClass(nameof(_default))]
%% %class %%, IEquatable<%>
{
public %IntPtr ThisPtr => _default.ThisPtr;

private readonly IObjectReference _inner = null;
private readonly Lazy<%> _defaultLazy;
%

private % _default => _defaultLazy.Value;
%
public static %% FromAbi(IntPtr thisPtr)
{
if (thisPtr == IntPtr.Zero) return null;
return MarshalInspectable<%>.FromAbi(thisPtr);
}

% %(% ifc)%
{
_defaultLazy = new Lazy<%>(() => ifc);
%}
%

public static bool operator ==(% x, % y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
public static bool operator !=(% x, % y) => !(x == y);
%
%

private struct InterfaceTag<I>{};

private % AsInternal(InterfaceTag<%> _) => _default;
%%
}
)",
            bind<write_winrt_attribute>(type),
            bind<write_winrt_helper_type_attribute>(type),
            bind<write_type_custom_attributes>(type, false),
            internal_accessibility(),
            bind<write_class_modifiers>(type),
            type_name,
            bind<write_type_inheritance>(type, base_semantics, true, false),
            type_name,
            derived_new,
            default_interface_abi_name,
            bind<write_lazy_interface_initialization>(type),
            default_interface_abi_name,
            bind<write_attributed_types>(type),
            derived_new,
            type_name,
            type_name,
            type.Flags().Sealed() ? "internal" : "protected internal",
            type_name,
            default_interface_abi_name,
            bind<write_base_constructor_dispatch_netstandard>(base_semantics),
            default_interface_abi_name,
            [&](writer& w)
            {
                if (!gc_pressure_amount) return;
                w.write("GC.AddMemoryPressure(%);\n", gc_pressure_amount);
            },
            [&](writer& w)
            {
                if (!gc_pressure_amount) return;
                w.write(R"(~%()
{
GC.RemoveMemoryPressure(%);
}
)", 
                    type_name,
                    gc_pressure_amount);
            },
            type_name,
            type_name,
            type_name,
            type_name,
            bind([&](writer& w)
            {
                bool return_type_matches = false;
                if (!has_class_equals_method(type, &return_type_matches))
                {
                    w.write("public bool Equals(% other) => this == other;\n", type_name);
                }
                // Even though there is an equals method defined, it doesn't match the signature for IEquatable
                // so we define an explicitly implemented one.
                else if (!return_type_matches)
                {
                    w.write("bool IEquatable<%>.Equals(% other) => this == other;\n", type_name, type_name);
                }

                if (!has_object_equals_method(type))
                {
                    w.write("public override bool Equals(object obj) => obj is % that && this == that;\n", type_name);
                }

                if (!has_object_hashcode_method(type))
                {
                    w.write("public override int GetHashCode() => ThisPtr.GetHashCode();\n");
                }
            }),
            bind([&](writer& w)
            {
                bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(type.Extends()));

                if (!type.Flags().Sealed())
                {
                    w.write(R"(
protected %(global::WinRT.DerivedComposed _)%
{
_defaultLazy = new Lazy<%>(() => GetDefaultReference<%.Vftbl>());
})",
                        type.TypeName(),
                        has_base_type ? ":base(_)" : "",
                        default_interface_abi_name,
                        default_interface_abi_name);
                }

                std::string_view access_spec = "protected ";
                std::string_view override_spec = has_base_type ? "override " : "virtual ";

                if (type.Flags().Sealed() && !has_base_type)
                {
                    access_spec = "private ";
                    override_spec = " ";
                }

                w.write(R"(
%%IObjectReference GetDefaultReference<T>() => _default.AsInterface<T>();)",
                    access_spec,
                    override_spec);

                w.write(R"(
%%IObjectReference GetReferenceForQI() => _inner ?? _default.ObjRef;)",
                    access_spec,
                    override_spec);
            }),
            default_interface_name,
            default_interface_name,
            bind<write_class_members>(type, false, false),
            bind<write_custom_query_interface_impl>(type));
    }

    
    void write_class(writer& w, TypeDef const& type)
    {
        writer::write_platform_guard guard{ w };

        if (settings.component)
        {
            write_wrapper_class(w, type);
            return;
        }

        if (is_static(type))
        {
            write_static_class(w, type);
            return;
        }

        auto type_namespace = type.TypeNamespace();
        auto type_name = write_type_name_temp(w, type);
        auto default_interface_name = get_default_interface_name(w, type, false);
        auto base_semantics = get_type_semantics(type.Extends());
        auto derived_new = std::holds_alternative<object_type>(base_semantics) ? "" : "new ";

        auto gc_pressure_amount = get_gc_pressure_amount(type);

        auto default_interface_typedef = for_typedef(w, get_type_semantics(get_default_interface(type)), [&](auto&& iface) { return iface; });
        auto is_manually_gen_default_interface = is_manually_generated_iface(default_interface_typedef);

        w.write(R"(%
%
[global::ABI.%.%RcwFactory]
[global::WinRT.ProjectedRuntimeClass(typeof(%))]
%% %class %%, IWinRTObject, IEquatable<%>
{
private IntPtr ThisPtr => _inner == null ? (((IWinRTObject)this).NativeObject).ThisPtr : _inner.ThisPtr;

private readonly IObjectReference _inner = null;
%
%

%
%
%
public static %% FromAbi(IntPtr thisPtr)
{
if (thisPtr == IntPtr.Zero) return null;
return MarshalInspectable<%>.FromAbi(thisPtr);
}

% %(IObjectReference objRef)%
{
_inner = objRef.As(%.IID);
%
%}
%

public static bool operator ==(% x, % y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
public static bool operator !=(% x, % y) => !(x == y);
%
%

private struct InterfaceTag<I>{};
%
%%
}
)",
            bind<write_winrt_attribute>(type),
            bind<write_winrt_helper_type_attribute>(type),
            type_namespace,
            type.TypeName(),
            default_interface_name,
            bind<write_type_custom_attributes>(type, true),
            internal_accessibility(),
            bind<write_class_modifiers>(type),
            type_name,
            bind<write_type_inheritance>(type, base_semantics, true, false),
            type_name,
            bind<write_lazy_interface_initialization>(type),
            bind([&](writer& w)
                {
                    if (is_manually_gen_default_interface)
                    {
                        w.write("private readonly Lazy<%> _defaultLazy;", default_interface_name);
                    }
                }),
            bind<write_class_objrefs_definition>(type, type.Flags().Sealed()),
            bind([&](writer& w)
                {
                    if (is_manually_gen_default_interface)
                    {
                        w.write("private % _default => %;", default_interface_name, "_defaultLazy.Value");
                    }
                }),
            bind<write_attributed_types>(type),
            // FromAbi
            derived_new,
            type_name,
            type_name,
            // ObjectReference constructor
            type.Flags().Sealed() ? "internal" : "protected internal",
            type_name,
            bind<write_base_constructor_dispatch>(base_semantics),
            bind<write_type_name>(get_type_semantics(get_default_interface(type)), typedef_name_type::StaticAbiClass, true),
            bind([&](writer& w)
                {
                    if (is_manually_gen_default_interface)
                    {
                        w.write("_defaultLazy = new Lazy<%>(() => (%)new SingleInterfaceOptimizedObject(typeof(%), _inner));", default_interface_name, default_interface_name, default_interface_name);
                    }
                }),
            [&](writer& w)
            {
                if (!gc_pressure_amount) return;
                w.write("GC.AddMemoryPressure(%);\n", gc_pressure_amount);
            },
            [&](writer& w)
            {
                if (!gc_pressure_amount) return;
                w.write(R"(~%()
{
GC.RemoveMemoryPressure(%);
}
)",
                    type_name,
                    gc_pressure_amount);
            },
            // Equality operators
            type_name,
            type_name,
            type_name,
            type_name,
            bind([&](writer& w)
            {
                bool return_type_matches = false;
                if (!has_class_equals_method(type, &return_type_matches))
                {
                    w.write("public bool Equals(% other) => this == other;\n", type_name);
                }
                // Even though there is an equals method defined, it doesn't match the signature for IEquatable
                // so we define an explicitly implemented one.
                else if (!return_type_matches)
                {
                    w.write("bool IEquatable<%>.Equals(% other) => this == other;\n", type_name, type_name);
                }

                if (!has_object_equals_method(type))
                {
                    w.write("public override bool Equals(object obj) => obj is % that && this == that;\n", type_name);
                }

                if (!has_object_hashcode_method(type))
                {
                    w.write("public override int GetHashCode() => ThisPtr.GetHashCode();\n");
                }
            }),
            bind([&](writer& w)
            {
                if (is_fast_abi_class(type))
                {
                    int hierarchy_index = get_class_hierarchy_index(type);
                    if (!type.Flags().Sealed() || hierarchy_index > 0)
                    {
                        auto default_interface = get_default_interface(type);
                        auto default_iface_method_count = 0;
                        for_typedef(w, get_type_semantics(default_interface), [&](TypeDef iface)
                            {
                                default_iface_method_count = distance(iface.MethodList());
                            });

                        w.write(R"(
protected unsafe % IObjectReference GetDefaultInterfaceObjRef(int hierarchyIndex)
{
if (hierarchyIndex < %)
{
    return ((IWinRTObject)this).NativeObject.AsKnownPtr((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr>**)_inner.ThisPtr)[% + hierarchyIndex](_inner.ThisPtr));
}
return _inner;
})",
                            hierarchy_index == 0 ? "virtual" : "override",
                            hierarchy_index,
                            INSPECTABLE_METHOD_COUNT + default_iface_method_count);
                    }
                }
                bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(type.Extends()));
                if (!type.Flags().Sealed())
                {
                    w.write(R"(
protected %(global::WinRT.DerivedComposed _)%
{
%
})",
                        type.TypeName(),
                        has_base_type ? ":base(_)" : "",
                        bind([&](writer& w)
                        {
                            if (is_manually_gen_default_interface)
                            {
                                w.write("_defaultLazy = new Lazy<%>(() => (%)new IInspectable(((IWinRTObject)this).NativeObject));", default_interface_name, default_interface_name);
                            }
                        }));
                    w.write(R"(
bool IWinRTObject.HasUnwrappableNativeObject => this.GetType() == typeof(%);)",
                        type.TypeName());
                }
                else
                {
                    w.write(R"(
bool IWinRTObject.HasUnwrappableNativeObject => true;)");
                }

                w.write(R"(
IObjectReference IWinRTObject.NativeObject => _inner;)");
                if (!has_base_type)
                { 
                w.write(R"(
private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
{
    global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null); 
    return _queryInterfaceCache;
}
global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
{
    global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null); 
    return _additionalTypeData;
}
global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();)");
                }
            }),
            bind([&](writer& w) 
                {
                    if (is_manually_gen_default_interface)
                    {
                        w.write("private % AsInternal(InterfaceTag<%> _) => _default;", default_interface_name, default_interface_name);
                    }
                }),
            bind<write_class_members>(type, false, false),
            bind<write_custom_query_interface_impl>(type));
    }

    void write_winrt_implementation_type_rcw_factory_attribute_type(writer& w, TypeDef const& type)
    {
        if (settings.netstandard_compat)
        {
            return;
        }

        if (settings.component)
        {
            return;
        }

        if (is_static(type))
        {
            return;
        }

        w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class %RcwFactoryAttribute : global::WinRT.WinRTImplementationTypeRcwFactoryAttribute
{
    public override object CreateInstance(global::WinRT.IInspectable inspectable)
        => new %(inspectable.ObjRef);
}
)", type.TypeName(), 
    bind<write_type_name>(type, typedef_name_type::Projected, false));
    }

    void write_abi_class(writer& w, TypeDef const& type)
    {
        if (is_static(type))
        {
            return;
        }

        auto abi_type_name = write_type_name_temp(w, type, "%", typedef_name_type::ABI);
        auto projected_type_name = write_type_name_temp(w, type);
        auto ccw_type_name = write_type_name_temp(w, type, "%", typedef_name_type::CCW);
        auto default_interface_abi_name = get_default_interface_name(w, type, true);

        w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
% struct %
{
%
public static IntPtr GetAbi(IObjectReference value) => value is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(value);
public static % FromAbi(IntPtr thisPtr) => %.FromAbi(thisPtr);
public static IntPtr FromManaged(% obj) => obj is null ? IntPtr.Zero : CreateMarshaler2(obj).Detach();
public static unsafe MarshalInterfaceHelper<%>.MarshalerArray CreateMarshalerArray(%[] array) => MarshalInterfaceHelper<%>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));
public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<%>.GetAbiArray(box);
public static unsafe %[] FromAbiArray(object box) => MarshalInterfaceHelper<%>.FromAbiArray(box, FromAbi);
public static void CopyAbiArray(%[] array, object box) => MarshalInterfaceHelper<%>.CopyAbiArray(array, box, FromAbi);
public static (int length, IntPtr data) FromManagedArray(%[] array) => MarshalInterfaceHelper<%>.FromManagedArray(array, (o) => FromManaged(o));
public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable<object>.DisposeMarshaler(value);
public static void DisposeMarshalerArray(MarshalInterfaceHelper<%>.MarshalerArray array) => MarshalInterfaceHelper<%>.DisposeMarshalerArray(array);
public static void DisposeAbi(IntPtr abi) => MarshalInspectable<object>.DisposeAbi(abi);
public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);
}
)",
            internal_accessibility(),
            abi_type_name,
            bind([&](writer& w)
            {
                bool is_exclusive_to_default = false;
                for_typedef(w, get_type_semantics(get_default_interface(type)), [&](auto&& type)
                {
                    is_exclusive_to_default = is_exclusive_to(type);
                });

                auto is_generic = distance(type.GenericParam()) > 0;
                std::string iid;
                if (settings.netstandard_compat)
                {
                    iid = is_generic ? w.write_temp("GuidGenerator.GetIID(%.Vftbl)", default_interface_abi_name)
                        : w.write_temp("GuidGenerator.GetIID(typeof(%).GetHelperType())", bind<write_type_name>(get_type_semantics(get_default_interface(type)), typedef_name_type::CCW, false));
                }
                else
                {
                    iid = w.write_temp("%.IID", bind<write_type_name>(get_type_semantics(get_default_interface(type)), typedef_name_type::StaticAbiClass, true));
                }

                if (is_exclusive_to_default)
                {
                    w.write(R"(
public static IObjectReference CreateMarshaler(% obj) => obj is null ? null : MarshalInspectable<%>.CreateMarshaler<%>(obj, %);
public static ObjectReferenceValue CreateMarshaler2(% obj) => MarshalInspectable<object>.CreateMarshaler2(obj, %);)",
                        projected_type_name,
                        projected_type_name,
                        is_generic ? w.write_temp("%.Vftbl", default_interface_abi_name) : w.write_temp("IUnknownVftbl"),
                        iid,
                        projected_type_name,
                        iid);
                }
                else
                {
                    auto default_interface_name = get_default_interface_name(w, type, false);
                    w.write(R"(
public static IObjectReference CreateMarshaler(% obj) => obj is null ? null : MarshalInterface<%>.CreateMarshaler(obj);
public static ObjectReferenceValue CreateMarshaler2(% obj) => MarshalInterface<%>.CreateMarshaler2(obj, %);)",
                        projected_type_name,
                        default_interface_name,
                        projected_type_name,
                        default_interface_name,
                        iid);
                }
            }),
            projected_type_name,
            ccw_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name,
            projected_type_name);
    }

    void write_delegate(writer& w, TypeDef const& type)
    {
        if (settings.component)
        {
            write_authoring_metadata_type(w, type);
            return;
        }

        method_signature signature{ get_delegate_invoke(type) };
        w.write(R"(%%%%% delegate % %(%);
)",
            bind<write_winrt_attribute>(type),
            bind<write_winrt_helper_type_attribute>(type),
            bind<write_winrt_exposed_type_attribute>(type, false),
            bind<write_type_custom_attributes>(type, false),
            internal_accessibility(),
            bind<write_projection_return_type>(signature),
            bind<write_type_name>(type, typedef_name_type::Projected, false),
            bind_list<write_projection_parameter>(", ", signature.params()));
    }

    void write_abi_delegate(writer& w, TypeDef const& type)
    {
        auto method = get_delegate_invoke(type);
        method_signature signature{ method };
        auto type_name = write_type_name_temp(w, type);
        auto type_params = w.write_temp("%", bind<write_type_params>(type));
        auto is_generic = distance(type.GenericParam()) > 0;
        auto generic_abi_types = get_generic_abi_types(w, signature);
        bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
            [](auto&& pair){ return !pair.second.empty(); }) != generic_abi_types.end();

        auto write_delegate = [&](writer& w, bool is_abi_helper_method)
        {
            std::string invoke;
            if (settings.netstandard_compat)
            {
                invoke = w.write_temp(R"(
global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(%, (% invoke) =>
{
%
}))",
                    !is_abi_helper_method && have_generic_params ? "new IntPtr(thisPtr)" : "thisPtr",
                    type_name,
                    bind([&](writer& w)
                    {
                        if (signature.return_signature())
                        {
                            w.write("return invoke(%);", "%");
                        }
                        else
                        {
                            w.write("invoke(%);");
                        }
                    }));
            }
            else
            {
                invoke = w.write_temp(R"(
global::WinRT.ComWrappersSupport.FindObject<%>(%).Invoke(%)
)",
                    type_name,
                    !is_abi_helper_method && have_generic_params ? "new IntPtr(thisPtr)" : "thisPtr",
                    "%");
            }

            if (is_abi_helper_method)
            {
                w.write(invoke, bind_list<write_parameter_name_with_modifier>(", ", signature.params()));
                w.write(";");
            }
            else
            {
                write_managed_method_call(w, signature, invoke);
            }
        };

        w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
%
% static class @%
{%
%
%
public static %IntPtr AbiToProjectionVftablePtr;

static unsafe @()
{
%
}
%
public static unsafe IObjectReference CreateMarshaler(% managedDelegate) => 
managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, IID);

public static unsafe ObjectReferenceValue CreateMarshaler2(% managedDelegate) => 
MarshalDelegate.CreateMarshaler2(managedDelegate, IID);

public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<%>.GetAbi(value);

public static unsafe % FromAbi(IntPtr nativeDelegate)
{
return MarshalDelegate.FromAbi<%>(nativeDelegate);
}

public static % CreateRcw(IntPtr ptr)
{
return new %(new NativeDelegateWrapper(ComWrappersSupport.GetObjectReferenceForInterface<IUnknownVftbl>(ptr, IID)).Invoke);
}

#if !NET
[global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
private sealed class NativeDelegateWrapper
#else
private sealed class NativeDelegateWrapper : IWinRTObject
#endif
{
private readonly ObjectReference<global::WinRT.Interop.IUnknownVftbl> _nativeDelegate;

public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IUnknownVftbl> nativeDelegate)
{
_nativeDelegate = nativeDelegate;
}

#if NET
IObjectReference IWinRTObject.NativeObject => _nativeDelegate;
bool IWinRTObject.HasUnwrappableNativeObject => true;
private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
{
    global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null); 
    return _queryInterfaceCache;
}
global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
{
    global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null); 
    return _additionalTypeData;
}
global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();
#endif

public unsafe % Invoke(%)
{%{
IntPtr ThisPtr = _nativeDelegate.ThisPtr;
%%}
}
}

public static IntPtr FromManaged(% managedDelegate) => CreateMarshaler2(managedDelegate).Detach();

public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<%>.DisposeMarshaler(value);

public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<%>.DisposeAbi(abi);

public static unsafe MarshalInterfaceHelper<%>.MarshalerArray CreateMarshalerArray(%[] array) => MarshalInterfaceHelper<%>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));
public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<%>.GetAbiArray(box);
public static unsafe %[] FromAbiArray(object box) => MarshalInterfaceHelper<%>.FromAbiArray(box, FromAbi);
public static void CopyAbiArray(%[] array, object box) => MarshalInterfaceHelper<%>.CopyAbiArray(array, box, FromAbi);
public static (int length, IntPtr data) FromManagedArray(%[] array) => MarshalInterfaceHelper<%>.FromManagedArray(array, (o) => FromManaged(o));
public static void DisposeMarshalerArray(MarshalInterfaceHelper<%>.MarshalerArray array) => MarshalInterfaceHelper<%>.DisposeMarshalerArray(array);
public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);

%
private static unsafe int Do_Abi_Invoke%
{
%
}
}

)",
            bind<write_guid_attribute>(type),
            internal_accessibility(),
            type.TypeName(),
            type_params,
            [&](writer& w) {
                if (!type_params.empty())
                {
                    // Generating both PIID and IID for backcompat consistency and to have a common property to rely on
                    // similar to what we do in the manual projections.
                    w.write(R"(
public static readonly global::System.Guid PIID = GuidGenerator.CreateIID(typeof(%));
public static global::System.Guid IID => PIID;
)",
                        type_name);
                }
                else
                {
                    if (settings.netstandard_compat)
                    {
                        w.write(R"(
public static global::System.Guid IID{ get; } = new Guid(new byte[]{ % });
)",
                            bind<write_guid_bytes>(type));
                    }
                    else
                    {
                        w.write(R"(
public static ref readonly global::System.Guid IID
{
[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
get
{
global::System.ReadOnlySpan<byte> data = new byte[] { % };
return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(data));
}
}
)",
                            bind<write_guid_bytes>(type));
                    }
                }
            },
            [&](writer& w) {
                if (!have_generic_params)
                {
                    // For invoke handlers that don't have generic params but within a generic handler,
                    // we handle them separately via its own delegate class.
                    if (!is_generic && settings.netstandard_compat)
                    {
                        w.write("private unsafe delegate int Abi_Invoke(%);\n",
                            bind<write_abi_parameters>(signature));
                    }
                    return;
                }
                w.write("private static readonly Type Abi_Invoke_Type;");
            },
            settings.netstandard_compat ? "private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;" : "",
            settings.netstandard_compat || !is_generic ? "readonly " : "",
            // class constructor
            type.TypeName(),
            [&](writer& w) 
            {
                if (settings.netstandard_compat)
                {
                    if (!is_generic)
                    {
                        w.write("\nAbiInvokeDelegate = new Abi_Invoke(Do_Abi_Invoke);");
                    }
                    else if (!have_generic_params)
                    {
                        w.write("\nAbiInvokeDelegate = new @_Delegates.Invoke(Do_Abi_Invoke);",
                            type.TypeName());
                    }
                    else
                    {
                        w.write(R"(Abi_Invoke_Type = global::WinRT.Projections.GetAbiDelegateType(new Type[] { typeof(void*), %typeof(int) });
)",
                            bind_each([&](writer& w, auto&& pair)
                            {
                                w.write("%, ", pair.first);
                            }, generic_abi_types));

                        w.write("\nAbiInvokeDelegate = global::System.Delegate.CreateDelegate(Abi_Invoke_Type, typeof(@%).GetMethod(nameof(Do_Abi_Invoke), BindingFlags.Static | BindingFlags.NonPublic)%);",
                            type.TypeName(),
                            type_params,
                            [&](writer& w) {
                                if (!have_generic_params) return;
                                w.write(".MakeGenericMethod(new Type[]{ % })\n",
                                    [&](writer& w) {
                                        int count = 0;
                                        for (auto&& pair : generic_abi_types)
                                        {
                                            if (pair.second.empty()) continue;
                                            w.write(count++ == 0 ? "" : ", ");
                                            w.write(pair.first);
                                        }
                                    });
                            });
                    }

                    w.write(R"(
AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
{
IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)
};
var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(@%), sizeof(IntPtr) * 4);
Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
AbiToProjectionVftablePtr = nativeVftbl;
)",
                        type.TypeName(),
                        type_params);
                }
                else if (!is_generic)
                {
                    w.write(R"(
AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(@), sizeof(IntPtr) * 4);
*(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
((delegate* unmanaged[Stdcall]<%, int>*)AbiToProjectionVftablePtr)[3] = &Do_Abi_Invoke;
)",
                        type.TypeName(),
                        bind<write_abi_parameter_types_pointer>(signature));
                }
                else
                {
                    w.write(R"(
if (!RuntimeFeature.IsDynamicCodeCompiled)
{
    AbiToProjectionVftablePtr = %.AbiToProjectionVftablePtr;
}
else
{
#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Generic instantiations might not be available in AOT scenarios.")]
#endif
    [MethodImpl(MethodImplOptions.NoInlining)]
    static global::System.Delegate InitializeAbiToProjectionVftablePtrFallback(%)
    {
%

global::System.Delegate abiInvokeDelegate = null;
if (%.AbiToProjectionVftablePtr == default)
{
    abiInvokeDelegate = %;
    AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(@%), sizeof(IntPtr) * 4);
    *(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
    ((IntPtr*)AbiToProjectionVftablePtr)[3] = Marshal.GetFunctionPointerForDelegate(abiInvokeDelegate);
}
else
{
    AbiToProjectionVftablePtr = %.AbiToProjectionVftablePtr;
}
return abiInvokeDelegate;
    }

    AbiInvokeDelegate = InitializeAbiToProjectionVftablePtrFallback(%);
}
)",
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        !have_generic_params ? "" : "ref Type abiInvokeType",
                        !have_generic_params ? "" :
                            w.write_temp(R"( 
if (%.AbiToProjectionVftablePtr == default || %._Invoke == default)
{
abiInvokeType = Expression.GetDelegateType(new Type[] { typeof(void*), %typeof(int) });
}
)",
                                bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                                bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                                bind_each([&](writer& w, auto&& pair)
                                {
                                    w.write("%, ", pair.first);
                                }, generic_abi_types)),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        [&](writer& w)
                        {
                            if (have_generic_params)
                            {
                                w.write("global::System.Delegate.CreateDelegate(Abi_Invoke_Type, typeof(@%).GetMethod(nameof(Do_Abi_Invoke), BindingFlags.Static | BindingFlags.NonPublic)%)",
                                    type.TypeName(),
                                    type_params,
                                    [&](writer& w)
                                    {
                                        if (!have_generic_params) return;
                                        w.write(".MakeGenericMethod(new Type[]{ % })\n",
                                            [&](writer& w) {
                                                int count = 0;
                                                for (auto&& pair : generic_abi_types)
                                                {
                                                    if (pair.second.empty()) continue;
                                                    w.write(count++ == 0 ? "" : ", ");
                                                    w.write(pair.first);
                                                }
                                            });
                                    });
                            }
                            else
                            {
                                w.write("new @_Delegates.Invoke(Do_Abi_Invoke)", type.TypeName());
                            }
                        },
                        type.TypeName(),
                        type_params,
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        !have_generic_params ? "" : "ref Abi_Invoke_Type");
                }

                w.write("global::WinRT.ComWrappersSupport.RegisterDelegateFactory(typeof(%), CreateRcw);", bind<write_type_name>(type, typedef_name_type::Projected, false));
            },
            settings.netstandard_compat || is_generic ? "\npublic static global::System.Delegate AbiInvokeDelegate { get; }\n" : "",
            // CreateMarshaler
            type_name,
            type_name,
            // GetAbi
            type_name,
            // FromAbi
            type_name,
            type_name,
            type_name,
            type_name,
            // NativeDelegateWrapper.Invoke
            bind<write_projection_return_type>(signature),
            bind_list<write_projection_parameter>(", ", signature.params()),
            [&](writer& w) {
                if (!settings.netstandard_compat && have_generic_params)
                {
                    w.write(R"(
if (!RuntimeFeature.IsDynamicCodeCompiled)
{
    %._Invoke(_nativeDelegate, %);
    return;
}

if (%._Invoke != null)
{
%._Invoke(_nativeDelegate, %);
}
else
)",
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        bind_list<write_parameter_name>(", ", signature.params()),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                        bind_list<write_parameter_name>(", ", signature.params()));
                }
            },
            bind([&](writer& w)
            {
                if (have_generic_params || settings.netstandard_compat)
                {
                    if (have_generic_params)
                    {
                        w.write("var abiInvoke = Marshal.GetDelegateForFunctionPointer((IntPtr)(*(void***)_nativeDelegate.ThisPtr)[3], Abi_Invoke_Type);");
                    }
                    else
                    {
                        w.write("var abiInvoke = Marshal.GetDelegateForFunctionPointer<%>((IntPtr)(*(void***)_nativeDelegate.ThisPtr)[3]);",
                            is_generic ? w.write_temp("@_Delegates.Invoke", type.TypeName()) : "Abi_Invoke");
                    }
                }
                else
                {
                    w.write("var abiInvoke = (delegate* unmanaged[Stdcall]<%, int>)(*(void***)_nativeDelegate.ThisPtr)[3];",
                        bind<write_abi_parameter_types_pointer>(signature));
                }
            }),
            bind<write_abi_method_call>(signature, "abiInvoke", "_nativeDelegate", have_generic_params, false, is_noexcept(method), false),
            // FromManaged
            type_name,
            // DisposeMarshaler
            type_name,
            // DisposeAbi
            type_name,
            // Array marshalers
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            // Do_Abi_Invoke
            !is_generic && !settings.netstandard_compat ? "\n[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]" : "",
            [&](writer& w) {
                if (!is_generic)
                {
                    w.write("(%)", bind<write_abi_parameters>(signature));
                    return;
                }
                if (have_generic_params)
                {
                    w.write("<");
                    int count = 0;
                    for (auto&& pair : generic_abi_types)
                    {
                        if (pair.second.empty()) continue;
                        w.write(count++ == 0 ? "" : ", ");
                        w.write(pair.second);
                    }
                    w.write(">");
                    w.write("(void* thisPtr");
                }
                else
                {
                    w.write("(IntPtr thisPtr");
                }
                int index = 0;
                for (auto&& param : signature.params())
                {
                    auto generic_type = generic_abi_types[index++].second;
                    auto param_cat = get_param_category(param);
                    if (!generic_type.empty() && (param_cat <= param_category::out))
                    {
                        if (settings.netstandard_compat)
                        {
                            w.write(", %% %",
                                param_cat == param_category::out ? "out " : "",
                                generic_type,
                                bind<write_parameter_name>(param));
                        }
                        else
                        {
                            w.write(", %% %",
                                generic_type,
                                param_cat == param_category::out ? "* " : "",
                                bind<write_parameter_name>(param));
                        }
                    }
                    else
                    {
                        write_abi_parameter(w, param);
                    }
                }
                if (auto return_sig = signature.return_signature())
                {
                    auto generic_type = generic_abi_types[index++].second;
                    if (!return_sig.Type().is_szarray() && !generic_type.empty())
                    {
                        if (settings.netstandard_compat)
                        {
                            w.write(", out % %", generic_type, signature.return_param_name());
                        }
                        else
                        {
                            w.write(", %* %", generic_type, signature.return_param_name());
                        }
                    }
                    else
                    {
                        write_abi_return(w, signature);
                    }
                }
                w.write(")");
            },
            bind(write_delegate, false));

            if (is_generic)
            {
                if (!have_generic_params)
                {
                    w.write(R"(
internal static class @_Delegates
{
public unsafe delegate int Invoke%;
}
)",
                        type.TypeName(),
                        bind<write_abi_signature>(method));
                }

                if (settings.netstandard_compat)
                {
                    return;
                }

                int index = 0;
                std::string vtableAlloc = settings.netstandard_compat ? 
                    "Marshal.AllocCoTaskMem((sizeof(IUnknownVftbl) + sizeof(IntPtr)))" : 
                    "(IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IUnknownVftbl) + sizeof(IntPtr)))";
                std::string vtableFree = settings.netstandard_compat ? 
                    "Marshal.FreeCoTaskMem(abiToProjectionVftablePtr);" : 
                    "NativeMemory.Free((void*)abiToProjectionVftablePtr);";

                w.write(R"(
internal static class %
{
%
private static IntPtr abiToProjectionVftablePtr;
internal static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

static @Methods()
{
ComWrappersSupport.RegisterHelperType(typeof(%), typeof(%));
}

internal static bool TryInitCCWVtable(IntPtr ptr)
{
bool success = global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
if (success)
{
%.AbiToProjectionVftablePtr = abiToProjectionVftablePtr;
global::WinRT.ComWrappersSupport.RegisterComInterfaceEntries(
typeof(%),
global::WinRT.DelegateTypeDetails<%>.GetExposedInterfaces(
new ComWrappers.ComInterfaceEntry 
{
IID = %.PIID,
Vtable = abiToProjectionVftablePtr 
}
)
);
}
return success;
}
}

% static class %%
{
%

public static unsafe bool InitCcw(% invoke)
{
if (%.AbiToProjectionVftablePtr != default)
{
return false;
}

var abiToProjectionVftablePtr = %;
*(IUnknownVftbl*)abiToProjectionVftablePtr = IUnknownVftbl.AbiToProjectionVftbl;
((%*)abiToProjectionVftablePtr)[3] = invoke;

if (!%.TryInitCCWVtable(abiToProjectionVftablePtr))
{
%
return false;
}

return true;
}

public static % Abi_Invoke(IntPtr thisPtr%%)
{
%
}
}
)",
                    bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                    !have_generic_params ? "" :
                        w.write_temp("internal unsafe volatile static delegate*<IObjectReference, %%%> _Invoke;",
                            bind_list<write_projection_parameter_type>(", ", signature.params()),
                            signature.has_params() ? ", " : "",
                            bind<write_projection_return_type>(signature)),
                    type.TypeName(),
                    bind<write_type_name>(type, typedef_name_type::Projected, false),
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    bind<write_type_name>(type, typedef_name_type::Projected, false),
                    bind<write_type_name>(type, typedef_name_type::Projected, false),
                    bind<write_type_name>(type, typedef_name_type::ABI, false),
                    internal_accessibility(),
                    bind<write_static_abi_class_generic_instantiation_type>(type),
                    bind_each([&](writer& w, GenericParam const& /*gp*/)
                    {
                            w.write(" where ");
                            write_generic_type_name(w, index++);
                            w.write("Abi : unmanaged");
                    }, type.GenericParam()),
                    !have_generic_params ? "" : 
                        w.write_temp(R"(
public unsafe static bool InitRcwHelper(delegate*<IObjectReference, %%%> invoke)
{
if (%._Invoke == null)
{
%._Invoke = invoke;
}
return true;
}
)",
                            bind_list<write_projection_parameter_type>(", ", signature.params()),
                            signature.has_params() ? ", " : "",
                            bind<write_projection_return_type>(signature),
                            bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                            bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false)),
                    bind<write_abi_delegate_parameter_types_pointer>(method),
                    bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                    vtableAlloc,
                    bind<write_abi_delegate_parameter_types_pointer>(method),
                    bind<write_type_name>(type, typedef_name_type::StaticAbiClass, false),
                    vtableFree,
                    bind<write_projection_return_type>(signature),
                    signature.has_params() ? ", " : "",
                    bind_list<write_projection_parameter>(", ", signature.params()),
                    bind(write_delegate, true));
            }
    }

    void write_constant(writer& w, Constant const& value)
    {
        switch (value.Type())
        {
        case ConstantType::Int32:
            w.write_printf("%#0x", value.ValueInt32());
            break;
        case ConstantType::UInt32:
            w.write_printf("%#0x", value.ValueUInt32());
            break;
        }
    }

    void write_enum(writer& w, TypeDef const& type)
    {
        if (settings.component)
        {
            write_authoring_metadata_type(w, type);
            return;
        }

        if (is_flags_enum(type))
        {
            w.write("[FlagsAttribute]\n");
        }

        auto enum_underlying_type = is_flags_enum(type) ? "uint" : "int";

        w.write(R"(%%%% enum % : %
{
)", 
        bind<write_winrt_attribute>(type),
        bind<write_winrt_exposed_type_attribute>(type, false),
        bind<write_type_custom_attributes>(type, true),
        (settings.internal || settings.embedded) ? (settings.public_enums ? "public" : "internal") : "public",
        bind<write_type_name>(type, typedef_name_type::Projected, false), enum_underlying_type);
        {
            for (auto&& field : type.FieldList())
            {
                if (auto constant = field.Constant())
                {
                    w.write("%% = unchecked((%)%),\n", 
                        bind<write_platform_attribute>(field.CustomAttribute()),
                        field.Name(), enum_underlying_type, bind<write_constant>(constant));
                }
            }
        }
        w.write("}\n");
    }

    void write_struct(writer& w, TypeDef const& type)
    {
        if (settings.component)
        {
            write_authoring_metadata_type(w, type);
            return;
        }

        auto name = w.write_temp("%", bind<write_type_name>(type, typedef_name_type::Projected, false));

        struct field_info
        {
            std::string type;
            std::string name;
            bool is_interface;
        };
        std::vector<field_info> fields;
        for (auto&& field : type.FieldList())
        {
            auto semantics = get_type_semantics(field.Signature().Type());
            field_info field_info{};
            field_info.type = w.write_temp("%", [&](writer& w){ write_projection_type(w, semantics); });
            field_info.name = field.Name();
            if (auto td = std::get_if<type_definition>(&semantics))
            {
                field_info.is_interface = get_category(*td) == category::interface_type;
            }
            else if (auto gti = std::get_if<generic_type_instance>(&semantics))
            {
                field_info.is_interface = get_category(gti->generic_type) == category::interface_type;
            }
            fields.emplace_back(field_info);
        }

        w.write(R"(%%%%% struct %: IEquatable<%>
{
%
public %(%)
{
%
}

public static bool operator ==(% x, % y) => %;
public static bool operator !=(% x, % y) => !(x == y);
public bool Equals(% other) => this == other;
public override bool Equals(object obj) => obj is % that && this == that;
public override int GetHashCode() => %;
}
)",
            // struct
            bind<write_winrt_attribute>(type),
            bind<write_winrt_helper_type_attribute>(type),
            bind<write_winrt_exposed_type_attribute>(type, false),
            bind<write_type_custom_attributes>(type, true),
            internal_accessibility(),
            name,
            name,
            bind_each([](writer& w, auto&& field)
            {
                w.write("public % %;\n", field.type, field.name);
            }, fields),
            // ctor
            name,
            bind_list([](writer& w, auto&& field)
            {
                w.write("% _%", field.type, field.name);
            }, ", ", fields),
            bind_each([](writer& w, auto&& field)
            {
                w.write("% = _%; ", field.name, field.name);
            }, fields),
            // ==
            name,
            name,
            bind_list([](writer& w, auto&& field)
            {
                w.write("x.% == y.%", 
                    field.name, field.name);
            }, " && ", fields),
            // !=, Equals
            name,
            name,
            name,
            name,
            // GetHashCode
            bind_list([](writer& w, auto&& field)
            {
                w.write("%.GetHashCode()", field.name);
            }, " ^ ", fields)
        );
    }

    void write_abi_struct(writer& w, TypeDef const& type)
    {
        if (is_type_blittable(type))
        {
            return;
        }

        w.write("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n% struct %\n{\n", 
            internal_accessibility(),
            bind<write_type_name>(type, typedef_name_type::ABI, false));

        for (auto&& field : type.FieldList())
        {
            w.write("public ");
            write_abi_type(w, get_type_semantics(field.Signature().Type()));
            w.write(" %;\n", field.Name());
        }

        auto projected_type = w.write_temp("%", bind<write_projection_type>(type));
        auto abi_type = w.write_temp("%", bind<write_type_name>(type, typedef_name_type::ABI, false));

        std::vector<abi_marshaler> marshalers;
        for (auto&& field : type.FieldList())
        {
            abi_marshaler m{ std::string(field.Name()), -1 };
            set_abi_marshaler(w, field.Signature().Type(), m);
            marshalers.push_back(std::move(m));
        }

        // blittable: (no marshaler) value type requiring no marshaling/disposing 
        // marshalable: (marshaler, is_value_type) value type requiring only marshaling, no disposing
        // disposable: (marshaler, !is_value_type) ref type requiring marshaling and disposing
        bool have_disposers = std::find_if(marshalers.begin(), marshalers.end(), [](abi_marshaler const& m)
        {
            return !m.is_value_type;
        }) != marshalers.end();

        w.write(R"(
public struct Marshaler
{
%public % __abi;
)",
            bind_each([](writer& w, abi_marshaler const& m)
            {
                if (m.marshaler_type.empty()) return;
                w.write("public % _%;\n", m.local_type, m.param_name);
            }, marshalers),
            abi_type);
        if (have_disposers)
        {
            w.write(R"(public void Dispose()
{
%}
)",
                bind_each([](writer& w, abi_marshaler const& m)
                {
                    if (m.is_value_type) return;
                    w.write("%.DisposeMarshaler(_%);\n",
                        m.is_marshal_by_object_reference_value() ? "MarshalInspectable<object>" : m.marshaler_type,
                        m.param_name);
                }, marshalers));
        }
        w.write("}\n");

        w.write(R"(
public static Marshaler CreateMarshaler(% arg)
{
var m = new Marshaler();)",
            projected_type);
        if (have_disposers)
        {
            w.write(R"(
bool success = false;
try
{)");
        }
        for (auto&& m : marshalers)
        {
            if (m.marshaler_type.empty()) continue;
            w.write("\nm._% = ", m.param_name);
            m.write_create(w, "arg." + m.get_escaped_param_name(w));
            w.write(";");
        }
        w.write(R"(
m.__abi = new %()
{
%};
%
return m;)",
            abi_type,
            [&](writer& w)
            {
                int count = 0;
                for (auto&& m : marshalers)
                {
                    w.write(count++ == 0 ? "" : ", ");
                    if (m.marshaler_type.empty())
                    {
                        std::string format;
                        if (m.param_type == "bool")
                        {
                            format = "% = (byte)(arg.% ? 1 : 0)\n";
                        }
                        else if (m.param_type == "char")
                        {
                            format = "% = (ushort)arg.%\n";
                        }
                        else
                        {
                            format = "% = arg.%\n";
                        }
                        w.write(format,
                            m.get_escaped_param_name(w),
                            m.get_escaped_param_name(w));
                        continue;
                    }
                    w.write("% = %.GetAbi(m._%)\n",
                        m.get_escaped_param_name(w),
                        m.is_marshal_by_object_reference_value() ? "MarshalInspectable<object>" : m.marshaler_type,
                        m.param_name);
                }
            },
            have_disposers ? "success = true;" : "");
        if (have_disposers)
        {
            w.write(R"(
}
finally
{
if (!success)
{
m.Dispose();
}
}
)");
        }
        w.write("}\n");

        w.write(R"(
public static % GetAbi(Marshaler m) => m.__abi;
)",
            abi_type);

        w.write(R"(
public static % FromAbi(% arg)
{
return new %()
{
%};
}
)",
            projected_type,
            abi_type,
            projected_type,
            [&](writer& w)
            {
                int count = 0;
                for (auto&& m : marshalers)
                {
                    w.write(count++ == 0 ? "" : ", ");
                    if (m.marshaler_type.empty())
                    {
                        std::string format;
                        if (m.param_type == "bool")
                        {
                            format = "% = arg.% != 0\n";
                        }
                        else if (m.param_type == "char")
                        {
                            format = "% = (char)arg.%\n";
                        }
                        else
                        {
                            format = "% = arg.%\n";
                        }
                        w.write(format,
                            m.get_escaped_param_name(w),
                            m.get_escaped_param_name(w));
                        continue;
                    }
                    w.write("% = %\n",
                        m.get_escaped_param_name(w),
                        [&](writer& w) {m.write_from_abi(w, "arg." + m.get_escaped_param_name(w)); });
                }
            });

        w.write(R"(
public static % FromManaged(% arg)
{
return new %()
{
%};
}
)",
            abi_type,
            projected_type,
            abi_type,
            [&](writer& w)
            {
                int count = 0;
                for (auto&& m : marshalers)
                {
                    w.write(count++ == 0 ? "" : ", ");
                    if (m.marshaler_type.empty())
                    {
                        std::string format;
                        if (m.param_type == "bool")
                        {
                            format = "% = (byte)(arg.% ? 1 : 0)\n";
                        }
                        else if (m.param_type == "char")
                        {
                            format = "% = (ushort)arg.%\n";
                        }
                        else
                        {
                            format = "% = arg.%\n";
                        }
                        w.write(format,
                            m.get_escaped_param_name(w),
                            m.get_escaped_param_name(w));
                        continue;
                    }
                    w.write("% = %\n",
                        m.get_escaped_param_name(w), [&](writer& w) {
                            m.write_from_managed(w, "arg." + m.get_escaped_param_name(w)); });
                }
            });

        w.write(R"(
public static unsafe void CopyAbi(Marshaler arg, IntPtr dest) => 
    *(%*)dest.ToPointer() = GetAbi(arg);
)",
            abi_type);

        w.write(R"(
public static unsafe void CopyManaged(% arg, IntPtr dest) =>
    *(%*)dest.ToPointer() = FromManaged(arg);
)",
            projected_type,
            abi_type);
    
      w.write(R"(
public static void DisposeMarshaler(Marshaler m) %
)",
            have_disposers ? "=> m.Dispose();" : "{}");

        w.write(R"(
public static void DisposeAbi(% abi)
{
%}
}
)",
            abi_type,
            bind_each([](writer& w, abi_marshaler const& m)
            {
                if (m.is_value_type) return;
                w.write("%.DisposeAbi(abi.%);\n",
                    m.marshaler_type,
                    m.param_name);
            }, marshalers));
    }

    void write_factory_class_inheritance(writer& w, TypeDef const& type)
    {
        auto delimiter{ ", " };
        auto write_delimiter = [&]()
        {
            w.write(delimiter);
        };

        for (auto&& [interface_name, factory] : get_attributed_types(w, type))
        {
            if ((factory.activatable || factory.statics) && factory.type)
            {
                write_delimiter();
                w.write("%", bind<write_type_name>(factory.type, typedef_name_type::CCW, false));
            }
        }
    }

    void write_factory_activatable_method(writer& w, MethodDef const& method, std::string_view activatable_type)
    {
        method_signature signature{ method };
        w.write(R"(
public % %(%) => new %(%);
)",
activatable_type,
method.Name(),
bind_list<write_projection_parameter>(", ", signature.params()),
activatable_type,
bind_list<write_parameter_name_with_modifier>(", ", signature.params())
);
    }

    void write_factory_class_members(writer& w, TypeDef const& type)
    {
        auto delimiter{ ", " };
        auto write_delimiter = [&]()
        {
            w.write(delimiter);
        };

        auto projected_type_name = write_type_name_temp(w, type, "%", typedef_name_type::Projected);
        for (auto&& [interface_name, factory] : get_attributed_types(w, type))
        {
            if (factory.type)
            {
                if (factory.activatable)
                {
                w.write_each<write_factory_activatable_method>(factory.type.MethodList(), projected_type_name);
                }
                else if (factory.statics)
                {
                w.write_each<write_static_factory_method>(factory.type.MethodList(), projected_type_name, ""sv);
                w.write_each<write_static_factory_property>(factory.type.PropertyList(), projected_type_name, ""sv);
                w.write_each<write_static_factory_event>(factory.type.EventList(), projected_type_name, ""sv);
                }
            }
        }
    }


    void write_factory_class(writer& w, TypeDef const& type)
    {
        auto factory_type_name = write_type_name_temp(w, type, "%ServerActivationFactory", typedef_name_type::CCW);
        auto is_activatable = !is_static(type) && has_default_constructor(type);
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::Projected);

        // If the type is activatable, we implement IActivationFactory by creating an
        // instance and marshalling it to IntPtr. Otherwise, we just throw an exception.
        auto activate_instance_body = is_activatable
            ? w.write_temp(R"(% comp = new %();

    return MarshalInspectable<%>.FromManaged(comp);)", type_name, type_name, type_name)
            : "throw new NotImplementedException();";

        w.write(R"(
%
internal sealed class % : global::WinRT.Interop.IActivationFactory%
{

static %()
{
RuntimeHelpers.RunClassConstructor(typeof(%).TypeHandle);
}

public static IntPtr Make()
{
    return MarshalInspectable<%>.CreateMarshaler2(_factory, IID.IID_IActivationFactory).Detach();
}

static readonly % _factory = new %();

public IntPtr ActivateInstance()
{
    %
}

%
}
)",
bind<write_winrt_exposed_type_attribute>(type, true),
factory_type_name,
bind<write_factory_class_inheritance>(type),
factory_type_name,
type_name,
factory_type_name,
factory_type_name,
factory_type_name,
activate_instance_body,
bind<write_factory_class_members>(type)
);
    }

    void write_module_activation_factory(writer& w, std::set<TypeDef> const& types)
    {
        w.write(R"(
using System;
namespace WinRT
{
% static partial class Module
{
public static unsafe IntPtr GetActivationFactory(% runtimeClassId)
{
switch (runtimeClassId)
{
%
default:
    return %;
}
}
%
%
}
}
)",
    internal_accessibility(),
    // We want to leverage C# support for switching on a ReadOnlySpan<char>, which allows the exported
    // 'DllGetActivationFactory' method to avoid the temporary allocation of the string instance for the
    // input runtime class name. Because that's a C# 11 feature, we only enable this on .NET 7 and above.
    // If that's the TFM in use, we generate this method using ReadOnlySpan<char>, and then a string overload
    // just calling it. Otherwise, we switch on the string parameter, and generate a dummy ReadOnlySpan<char>
    // overload just allocating and calling that. We always need both exports to make sure that hosting
    // scenarios also work, since those will be looking up the string overload via reflection.
    settings.netstandard_compat ? "string" : "ReadOnlySpan<char>",
bind_each([](writer& w, TypeDef const& type)
    {
        w.write(R"(
case "%.%":
    return %ServerActivationFactory.Make();
)",
type.TypeNamespace(),
type.TypeName(),
bind<write_type_name>(type, typedef_name_type::CCW, true)
);
    },
    types
        ),
    settings.partial_factory ? "GetActivationFactoryPartial(runtimeClassId)" : "IntPtr.Zero",
    settings.netstandard_compat ? R"(
public static IntPtr GetActivationFactory(ReadOnlySpan<char> runtimeClassId)
{
    return GetActivationFactory(runtimeClassId.ToString());
}
)" : R"(
public static IntPtr GetActivationFactory(string runtimeClassId)
{
    return GetActivationFactory(runtimeClassId.AsSpan());
}
)",
bind([&](writer& w) {
        if (settings.partial_factory)
        {
            if (!settings.netstandard_compat)
            {
                w.write("private static partial IntPtr GetActivationFactoryPartial(ReadOnlySpan<char> runtimeClassId);");
            }
            else
            {
                w.write("private static partial IntPtr GetActivationFactoryPartial(string runtimeClassId);");
            }
        }
    })
);
    }

    void write_event_source_generic_args(writer& w, cswinrt::type_semantics eventTypeSemantics)
    {
        if (!std::holds_alternative<generic_type_instance>(eventTypeSemantics))
        {
            return;
        }
        for_typedef(w, eventTypeSemantics, [&](TypeDef const&)
            {
                std::vector<std::string> genericArgs;
                auto arg_count = std::get<generic_type_instance>(eventTypeSemantics).generic_args.size();
                for (int i = 0; i < (int) arg_count; ++i )
                {
                    auto semantics = w.get_generic_arg_scope(i).first;
                    if (std::holds_alternative<generic_type_param>(semantics))
                    {
                        genericArgs.push_back(w.write_temp("%", bind<write_generic_type_name>(i)));
                    }
                }                
                if (genericArgs.size() == 0)
                {
                    return;
                }
                w.write("<%>", bind_list([](writer& w, auto&& value)
                    {
                        w.write(value);
                    }, ", "sv, genericArgs));
            });
    }

    void write_event_source_subclass(writer& w, cswinrt::type_semantics eventTypeSemantics)
    {
        auto genericInstantiationInitialization = w.write_temp("%", bind<write_ensure_generic_type_initialized>(eventTypeSemantics, true));

        auto abiTypeName = w.write_temp("%", bind<write_type_name>(eventTypeSemantics, typedef_name_type::ABI, true));
        for_typedef(w, eventTypeSemantics, [&](TypeDef const& eventType)
            {
                if ((eventType.TypeNamespace() == "Windows.Foundation" || eventType.TypeNamespace() == "System") && eventType.TypeName() == "EventHandler`1")
                {
                    return;
                }

                auto eventTypeCode = w.write_temp("%", bind<write_type_name>(eventType, typedef_name_type::Projected, false));
                auto invokeMethodSig = get_event_invoke_method_signature(eventType);
                w.write(R"(
internal sealed unsafe class %% : global::ABI.WinRT.Interop.EventSource<%>
{
%

internal %(IObjectReference obj,
delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, %WinRT.EventRegistrationToken%, int> addHandler,
delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler, int index) : base(obj, addHandler, removeHandler, index)
{
%
}

protected override ObjectReferenceValue CreateMarshaler(% handler) =>
%.CreateMarshaler2(handler);

protected override global::ABI.WinRT.Interop.EventSourceState<%> CreateEventSourceState() =>
new EventState(ObjectReference.ThisPtr, Index);

%
private sealed class EventState : global::ABI.WinRT.Interop.EventSourceState<%>
{
public EventState(System.IntPtr obj, int index)
: base(obj, index)
{
}

protected override % GetEventInvoke()
{
return (%) =>
{
var targetDelegate = TargetDelegate;
if (targetDelegate is null)
{%
return %;
}
%targetDelegate.Invoke(%);
};
}
}
}
)",
bind<write_event_source_type_name>(eventTypeSemantics),
bind<write_event_source_generic_args>(eventTypeSemantics),
eventTypeCode, // EventSource<%>
genericInstantiationInitialization,
bind<write_event_source_type_name>(eventTypeSemantics),
settings.netstandard_compat ? "out " : "",
settings.netstandard_compat ? "" : "*",
genericInstantiationInitialization == "" ? "" : "_ = initialized;",
eventTypeCode, // % handler
abiTypeName,
eventTypeCode, // EventSourceState<%>
settings.netstandard_compat ? "" : "[global::WinRT.WinRTExposedType]",
eventTypeCode, // EventSourceState<%>
eventTypeCode, // % GetEventInvoke()
bind<write_event_invoke_params>(invokeMethodSig),
bind<write_event_out_defaults>(invokeMethodSig),
bind<write_event_invoke_return_default>(invokeMethodSig),
bind<write_event_invoke_return>(invokeMethodSig),
bind<write_event_invoke_args>(invokeMethodSig));
});
    }

    void write_temp_class_event_source_subclass(writer& w, TypeDef const& classType, concurrency::concurrent_unordered_map<std::string, std::string>& typeNameToDefinitionMap)
    {
        for (auto&& ii : classType.InterfaceImpl())
        {
            for_typedef(w, get_type_semantics(ii.Interface()), [&](TypeDef const& interfaceType)
                {
                    for (auto&& eventObj : interfaceType.EventList())
                    {
                        auto&& eventTypeSemantics = get_type_semantics(eventObj.EventType());
                        auto&& eventTypeCode = w.write_temp("%", bind<write_type_name>(eventTypeSemantics, typedef_name_type::Projected, false));
                        auto&& eventClass = w.write_temp("%", bind<write_event_source_subclass>(eventTypeSemantics));
                        typeNameToDefinitionMap[eventTypeCode] = eventClass;
                    }
                });
        }
    }

    void write_temp_interface_event_source_subclass(writer& w, TypeDef const& interfaceType, concurrency::concurrent_unordered_map<std::string, std::string>& typeNameToDefinitionMap)
    {
        for (auto&& eventObj : interfaceType.EventList())
        {
            auto&& eventTypeSemantics = get_type_semantics(eventObj.EventType());
            auto&& eventTypeCode = w.write_temp("%", bind<write_type_name>(eventTypeSemantics, typedef_name_type::Projected, false));
            auto&& eventClass = w.write_temp("%", bind<write_event_source_subclass>(eventTypeSemantics));
            typeNameToDefinitionMap[eventTypeCode] = eventClass;
        }
    }

    void add_base_type_entry(TypeDef const& classType, concurrency::concurrent_unordered_map<std::string, std::string>& typeNameToBaseTypeMap)
    {
        writer w("");
        auto base_type = get_type_semantics(classType.Extends());
        bool has_base_type = !std::holds_alternative<object_type>(base_type);
        if (has_base_type)
        {
            int numChars = (int)strlen("global::");
            auto&& typeName = w.write_temp("%", bind<write_type_name>(classType, typedef_name_type::Projected, true)).substr(numChars);
            auto&& baseTypeName = w.write_temp("%", bind<write_type_name>(base_type, typedef_name_type::Projected, true)).substr(numChars);
            typeNameToBaseTypeMap[typeName] = baseTypeName;
        }
    }

    void add_metadata_type_entry(TypeDef const& type, concurrency::concurrent_unordered_map<std::string, std::string>& authoredTypeNameToMetadataTypeNameMap)
    {
        if (settings.component)
        {
            if ((get_category(type) == category::class_type && is_static(type)) || 
                (get_category(type) == category::interface_type && is_exclusive_to(type)))
            {
                return;
            }

            writer w("");
            auto&& typeName = w.write_temp("%", bind<write_type_name>(type, typedef_name_type::Projected, true));
            auto&& metadataTypeName = w.write_temp("%", bind<write_type_name>(type, typedef_name_type::CCW, true));
            authoredTypeNameToMetadataTypeNameMap[typeName] = metadataTypeName;
        }
    }

    // Checking for if this is an ABI delegate that will need to be code generated or
    // whether it is one that we manually already added in Projections.cs such as for
    // classes where the ABI type is IntPtr and we can handle generically.
    bool is_abi_delegate_required_for_type(type_semantics const& semantics)
    {
        return call(semantics,
            [&](guid_type) 
            {
                return true;
            },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                case category::enum_type:
                case category::struct_type:
                    return true;
                default:
                    return false;
                }
            },
            [&](fundamental_type type)
            {
                return type != fundamental_type::String;
            },
            [](auto)
            {
                return false;
            });
    }

    void add_abi_delegates_for_type(std::string_view typeNamespace, std::string_view typeName, std::vector<type_semantics> generics, concurrency::concurrent_unordered_set<generic_abi_delegate>& abiDelegateEntries)
    {
        writer w;
        if (typeNamespace == "Windows.Foundation" || typeNamespace == "Windows.Foundation.Collections")
        {
            if (typeName == "IIterator`1")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_Current_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_Current_%(void* thisPtr, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "IKeyValuePair`2")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_Key_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_Key_%(IntPtr thisPtr, %* __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(IntPtr), typeof(%*), typeof(int) }", abiType)
                        });
                }

                if (is_abi_delegate_required_for_type(generics[1]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[1]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_Value_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_Value_%(IntPtr thisPtr, %* __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(IntPtr), typeof(%*), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "IMapView`2")
            {
                if (is_abi_delegate_required_for_type(generics[0]) || is_abi_delegate_required_for_type(generics[1]))
                {
                    auto keyAbiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedKeyAbiType = escape_type_name_for_identifier(keyAbiType);

                    auto valueAbiType = w.write_temp("%", bind<write_abi_type>(generics[1]));
                    auto escapedValueAbiType = escape_type_name_for_identifier(valueAbiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_lookup_%_%", escapedKeyAbiType, escapedValueAbiType),
                            w.write_temp("internal unsafe delegate int _lookup_%_%(void* thisPtr, % key, out % value);", escapedKeyAbiType, escapedValueAbiType, keyAbiType, valueAbiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(%).MakeByRefType(), typeof(int) }", keyAbiType, valueAbiType)
                        });

                    if (is_abi_delegate_required_for_type(generics[0]))
                    {
                        abiDelegateEntries.insert(generic_abi_delegate
                            {
                                w.write_temp("_has_key_%", escapedKeyAbiType),
                                w.write_temp("internal unsafe delegate int _has_key_%(void* thisPtr, % key, out byte found);", escapedKeyAbiType, keyAbiType),
                                w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(byte).MakeByRefType(), typeof(int) }", keyAbiType)
                            });
                    }
                }
            }
            else if (typeName == "IMap`2")
            {
                if (is_abi_delegate_required_for_type(generics[0]) || is_abi_delegate_required_for_type(generics[1]))
                {
                    auto keyAbiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedKeyAbiType = escape_type_name_for_identifier(keyAbiType);

                    auto valueAbiType = w.write_temp("%", bind<write_abi_type>(generics[1]));
                    auto escapedValueAbiType = escape_type_name_for_identifier(valueAbiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_lookup_%_%", escapedKeyAbiType, escapedValueAbiType),
                            w.write_temp("internal unsafe delegate int _lookup_%_%(void* thisPtr, % key, out % value);", escapedKeyAbiType, escapedValueAbiType, keyAbiType, valueAbiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(%).MakeByRefType(), typeof(int) }", keyAbiType, valueAbiType)
                        });

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_insert_%_%", escapedKeyAbiType, escapedValueAbiType),
                            w.write_temp("internal unsafe delegate int _insert_%_%(void* thisPtr, % key, % value, out byte replaced);", escapedKeyAbiType, escapedValueAbiType, keyAbiType, valueAbiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(%), typeof(byte).MakeByRefType(), typeof(int) }", keyAbiType, valueAbiType)
                        });

                    if (is_abi_delegate_required_for_type(generics[0]))
                    {
                        abiDelegateEntries.insert(generic_abi_delegate
                            {
                                w.write_temp("_has_key_%", escapedKeyAbiType),
                                w.write_temp("internal unsafe delegate int _has_key_%(void* thisPtr, % key, out byte found);", escapedKeyAbiType, keyAbiType),
                                w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(byte).MakeByRefType(), typeof(int) }", keyAbiType)
                            });

                        abiDelegateEntries.insert(generic_abi_delegate
                            {
                                w.write_temp("_remove_%", escapedKeyAbiType),
                                w.write_temp("internal unsafe delegate int _remove_%(void* thisPtr, % key);", escapedKeyAbiType, keyAbiType),
                                w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(int) }", keyAbiType)
                            });
                    }
                }
            }
            else if (typeName == "IVectorView`1")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_at_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_at_%(void* thisPtr, uint index, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(uint), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_index_of_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _index_of_%(void* thisPtr, % value, out uint index, out byte found);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }", abiType)
                        });

                    // GetEnumerator in IVectorView
                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_Current_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_Current_%(void* thisPtr, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "IVector`1")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_at_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_at_%(void* thisPtr, uint index, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(uint), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_index_of_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _index_of_%(void* thisPtr, % value, out uint index, out byte found);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }", abiType)
                        });

                    // SetAt / InsertAt
                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_set_at_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _set_at_%(void* thisPtr, uint index, % value);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(uint), typeof(%), typeof(int) }", abiType)
                        });

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_append_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _append_%(void* thisPtr, % value);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(int) }", abiType)
                        });

                    // GetEnumerator in IVector
                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_Current_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_Current_%(void* thisPtr, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "EventHandler`1")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_invoke_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _invoke_%(void* thisPtr, IntPtr sender, % args);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(IntPtr), typeof(%), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "IReference`1")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_Value_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_Value_%(void* thisPtr, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "IMapChangedEventArgs`1" ||
                typeName == "IAsyncOperation`1" ||
                typeName == "IAsyncOperationWithProgress`2")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_get_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _get_%(void* thisPtr, out % __return_value__);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%).MakeByRefType(), typeof(int) }", abiType)
                        });
                }

                // Add ABI delegates for AsyncOperationProgressHandler as it is referenced in a property of IAsyncOperationWithProgress
                // which isn't handled separately.
                if (typeName == "IAsyncOperationWithProgress`2" && is_abi_delegate_required_for_type(generics[1]))
                {
                    add_abi_delegates_for_type("Windows.Foundation", "AsyncOperationProgressHandler`2", generics, abiDelegateEntries);
                }
            }
            else if (typeName == "TypedEventHandler`2")
            {
                if (is_abi_delegate_required_for_type(generics[0]) || is_abi_delegate_required_for_type(generics[1]))
                {
                    auto senderAbiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedSenderAbiType = escape_type_name_for_identifier(senderAbiType);

                    auto resultAbiType = w.write_temp("%", bind<write_abi_type>(generics[1]));
                    auto escapedResultAbiType = escape_type_name_for_identifier(resultAbiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_invoke_%_%", escapedSenderAbiType, escapedResultAbiType),
                            w.write_temp("internal unsafe delegate int _invoke_%_%(void* thisPtr, % sender, % args);", escapedSenderAbiType, escapedResultAbiType, senderAbiType, resultAbiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(%), typeof(%), typeof(int) }", senderAbiType, resultAbiType)
                        });
                }
            }
            else if (typeName == "AsyncOperationProgressHandler`2")
            {
                if (is_abi_delegate_required_for_type(generics[1]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[1]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_invoke_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _invoke_%(void* thisPtr, IntPtr asyncInfo, % progressInfo);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(IntPtr), typeof(%), typeof(int) }", abiType)
                        });
                }
            }
            else if (typeName == "AsyncActionProgressHandler`1")
            {
                if (is_abi_delegate_required_for_type(generics[0]))
                {
                    auto abiType = w.write_temp("%", bind<write_abi_type>(generics[0]));
                    auto escapedAbiType = escape_type_name_for_identifier(abiType);

                    abiDelegateEntries.insert(generic_abi_delegate
                        {
                            w.write_temp("_invoke_%", escapedAbiType),
                            w.write_temp("internal unsafe delegate int _invoke_%(void* thisPtr, IntPtr asyncInfo, % progressInfo);", escapedAbiType, abiType),
                            w.write_temp("new global::System.Type[] { typeof(void*), typeof(IntPtr), typeof(%), typeof(int) }", abiType)
                        });
                }
            }
        }
    }

    void add_if_generic_type_reference(cswinrt::type_semantics const& typeSemantics, bool isArray, concurrency::concurrent_unordered_set<generic_abi_delegate>& abiDelegateEntries)
    {
        call(typeSemantics,
            [&](generic_type_instance const& type)
            {
                // Handle generics of generics where their delegate entries should also be added.
                for (auto& arg : type.generic_args)
                {
                    // None of the generics should be an array given WinRT doens't support that.
                    add_if_generic_type_reference(arg, false, abiDelegateEntries);
                }

                add_abi_delegates_for_type(type.generic_type.TypeNamespace(), type.generic_type.TypeName(), type.generic_args, abiDelegateEntries);
            },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                    case category::struct_type:
                    {
                        // Struct fields can have IReference generics which we should generate delegates for.
                        for (auto&& field : type.FieldList())
                        {
                            auto fieldType = field.Signature().Type();
                            add_if_generic_type_reference(get_type_semantics(fieldType), fieldType.is_szarray(), abiDelegateEntries);
                        }
                        break;
                    }
                }

                // On the CCW for arrays, we also generate an IVector CCW, so we need to generate the appropriate delegates.
                if (isArray)
                {
                    std::vector<cswinrt::type_semantics> genericArgs = { typeSemantics };
                    add_abi_delegates_for_type("Windows.Foundation.Collections", "IVector`1", genericArgs, abiDelegateEntries);
                }
            },
            [&](auto)
            {
                // On the CCW for arrays, we also generate an IVector CCW, so we need to generate the appropriate delegates.
                if (isArray)
                {
                    std::vector<cswinrt::type_semantics> genericArgs = { typeSemantics };
                    add_abi_delegates_for_type("Windows.Foundation.Collections", "IVector`1", genericArgs, abiDelegateEntries);
                }
            }
        );
    }

    void add_generic_type_references_in_interface_type(TypeDef const& interfaceType, concurrency::concurrent_unordered_set<generic_abi_delegate>& abiDelegateEntries)
    {
        for (auto&& method : interfaceType.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }

            method_signature signature{ method };
            if (signature.has_params())
            {
                for (auto&& param : signature.params())
                {
                    add_if_generic_type_reference(get_type_semantics(param.second->Type()), param.second->Type().is_szarray(), abiDelegateEntries);
                }
            }

            if (auto& returnSignature = signature.return_signature())
            {
                add_if_generic_type_reference(get_type_semantics(returnSignature.Type()), returnSignature.Type().is_szarray(), abiDelegateEntries);
            }
        }

        for (auto&& prop : interfaceType.PropertyList())
        {
            add_if_generic_type_reference(get_type_semantics(prop.Type().Type()), prop.Type().Type().is_szarray(), abiDelegateEntries);
        }

        for (auto&& evt : interfaceType.EventList())
        {
            add_if_generic_type_reference(get_type_semantics(evt.EventType()), false, abiDelegateEntries);
        }
    }

    void add_generic_type_references_in_type(TypeDef const& type, concurrency::concurrent_unordered_set<generic_abi_delegate>& abiDelegateEntries)
    {
        switch (get_category(type))
        {
        case category::delegate_type:
        {
            method_signature signature{ get_delegate_invoke(type) };
            if (signature.has_params())
            {
                for (auto&& param : signature.params())
                {
                    add_if_generic_type_reference(get_type_semantics(param.second->Type()), param.second->Type().is_szarray(), abiDelegateEntries);
                }
            }

            if (auto& returnSignature = signature.return_signature())
            {
                add_if_generic_type_reference(get_type_semantics(returnSignature.Type()), returnSignature.Type().is_szarray(), abiDelegateEntries);
            }

            break;
        }
        case category::interface_type:
            add_generic_type_references_in_interface_type(type, abiDelegateEntries);
            break;
        }
    }

    bool has_generic_type_instantiations()
    {
        return generic_type_instances.size() != 0;
    }

    void write_generic_type_instantiation(writer& w, generic_type_instance instance, std::vector<std::string>& rcwFunctions, std::vector<std::string>& vtableFunctions)
    {
        auto get_invoke_info = [&](MethodDef const& method, bool isDelegate = false)
        {
            return w.write_temp("(*(delegate* unmanaged[Stdcall]<%, int>**)ThisPtr)[%]",
                bind([&](writer& w) {
                    writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
                    {
                        write_abi_type(w, w.get_generic_arg_scope(index).first);
                    });

                    write_abi_parameter_types_pointer(w, method_signature{ method });
                }),
                isDelegate ? 3 : get_vmethod_index(instance.generic_type, method) + INSPECTABLE_METHOD_COUNT);
        };

        if (get_category(instance.generic_type) == category::delegate_type)
        {
            auto method = get_event_invoke_method(instance.generic_type);
            method_signature signature{ method };

            auto guard{ w.push_generic_args(instance) };

            if (abi_signature_has_generic_parameters(w, signature))
            {
                rcwFunctions.emplace_back(method.Name());

                auto invoke_target = get_invoke_info(method, true);
                w.write(R"(
private static unsafe % %(IObjectReference _obj%%)
{
var ThisPtr = _obj.ThisPtr;
%}
)",
                    bind<write_projection_return_type>(signature),
                    method.Name(),
                    signature.has_params() ? ", " : "",
                    bind_list<write_projection_parameter>(", ", signature.params()),
                    bind<write_abi_method_call>(signature, invoke_target, "_obj", false, false, is_noexcept(method), true));
            }

            vtableFunctions.emplace_back(method.Name());

            w.write(R"(
[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
private static unsafe int Do_Abi_Invoke(%)
{
%
}
)",
                [&](writer& w) {
                    writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
                    {
                        write_abi_type(w, w.get_generic_arg_scope(index).first);
                    });

                    write_abi_parameters(w, signature);
                },
                [&](writer& w) {
                    auto invoke = w.write_temp(
                        "%%.Abi_Invoke(thisPtr, %)",
                        bind<write_projection_type_for_name_type>(instance.generic_type, typedef_name_type::StaticAbiClass),
                        [&](writer& w)
                        {
                            writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
                            {
                                write_projection_type(w, w.get_generic_arg_scope(index).first);
                                w.write(", ");
                                write_abi_type(w, w.get_generic_arg_scope(index).first);
                            });

                            write_type_params(w, instance.generic_type);
                        },
                        "%");
                    write_managed_method_call(w, signature, invoke, true);
                });
        }
        else
        {
            for (auto&& method : instance.generic_type.MethodList())
            {
                method_signature signature{ method };

                if (!projected_signature_has_generic_parameters(w, signature))
                {
                    continue;
                }

                // Adding RCW function names here including the synthesized methods for
                // properties so that we have them in the right vtable order even if we
                // don't write them here.
                if (!(is_special(method) && 
                    (starts_with(method.Name(), "add_") ||
                     starts_with(method.Name(), "remove_"))))
                {
                    rcwFunctions.emplace_back(method.Name());
                }

                if (is_special(method))
                {
                    continue;
                }

                auto guard{ w.push_generic_args(instance) };

                auto invoke_target = get_invoke_info(method);
                w.write(R"(
private static unsafe % %(IObjectReference _obj%%)
{
var ThisPtr = _obj.ThisPtr;
%}
)",
                    bind<write_projection_return_type>(signature),
                    method.Name(),
                    signature.has_params() ? ", " : "",
                    bind_list<write_projection_parameter>(", ", signature.params()),
                    bind<write_abi_method_call>(signature, invoke_target, "_obj", false, false, is_noexcept(method), true));
            }

            for (auto&& prop : instance.generic_type.PropertyList())
            {
                auto guard{ w.push_generic_args(instance) };
                auto [getter, setter] = get_property_methods(prop);

                if (getter && projected_signature_has_generic_parameters(w, method_signature{ getter }))
                {
                    auto invoke_target = get_invoke_info(getter);
                    auto signature = method_signature(getter);
                    auto marshalers = get_abi_marshalers(w, signature, false, prop.Name(), false, true);
                    w.write(R"(private static unsafe % %(IObjectReference _obj)
{
var ThisPtr = _obj.ThisPtr;
%}
)",
                        write_prop_type(w, prop),
                        getter.Name(),
                        bind<write_abi_method_call_marshalers>(invoke_target, "_obj", false, marshalers, is_noexcept(prop)));
                }
                if (setter && projected_signature_has_generic_parameters(w, method_signature{ setter }))
                {
                    auto invoke_target = get_invoke_info(setter);
                    auto signature = method_signature(setter);
                    auto marshalers = get_abi_marshalers(w, signature, false, prop.Name(), false, true);
                    marshalers[0].param_name = "value";
                    w.write(R"(private static unsafe void %(IObjectReference _obj, % value)
{
var ThisPtr = _obj.ThisPtr;
%}
)",
                        setter.Name(),
                        write_prop_type(w, prop),
                        bind<write_abi_method_call_marshalers>(invoke_target, "_obj", false, marshalers, is_noexcept(prop)));
                }
                w.write("\n");
            }
        }
    }

    generic_type_instance ConvertGenericTypeInstanceToConcreteType(writer& w, const generic_type_instance& generic_instance)
    {
        generic_type_instance converted = generic_instance;
        for (size_t idx = 0; idx < generic_instance.generic_args.size(); idx++)
        {
            auto& semantics = generic_instance.generic_args[idx];
            if (auto gti = std::get_if<generic_type_index>(&semantics))
            {
                converted.generic_args[idx] = w.get_generic_arg_scope(gti->index).first;
            }
            else if (auto instance  = std::get_if<generic_type_instance>(&semantics))
            {
                converted.generic_args[idx] = ConvertGenericTypeInstanceToConcreteType(w, *instance);
            }
        }

        return converted;
    }

    void write_generic_type_instantiations(writer& w)
    {
        // Go through all the generic types and write instantiation classes for them.
        // While writing them, their implementations might also reference generic types
        // which may also need instantiation classes if one hasn't been generated already.
        concurrency::concurrent_unordered_set<generic_type_instantiation> written_generic_type_instances;
        bool types_written = true;
        while (generic_type_instances.size() != 0 && types_written)
        {
            types_written = false;
            concurrency::concurrent_unordered_set<generic_type_instantiation> current_generic_type_instances = generic_type_instances;
            generic_type_instances = concurrency::concurrent_unordered_set<generic_type_instantiation>();
            for (auto& instance : current_generic_type_instances)
            {
                if (written_generic_type_instances.find(instance) != written_generic_type_instances.end())
                {
                    continue;
                }
                written_generic_type_instances.insert(instance);
                types_written = true;

                std::vector<std::string> rcwFunctions, vtableFunctions;
                w.write(R"(
internal static class %
{
private static bool Initialized { get; } = Init();

public static bool EnsureInitialized() => Initialized;

%

private unsafe static bool Init()
{
%
%
return true;
}
}
)",
                    instance.instantiation_class_name,
                    bind<write_generic_type_instantiation>(instance.instance, rcwFunctions, vtableFunctions),
                    bind([&](writer& w) {
                        auto guard{ w.push_generic_args(instance.instance) };

                        auto genericInstantiationMethodsClass = w.write_temp("%%",
                            bind<write_projection_type_for_name_type>(instance.instance.generic_type, typedef_name_type::StaticAbiClass),
                            [&](writer& w)
                            {
                                writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
                                {
                                    write_projection_type(w, w.get_generic_arg_scope(index).first);
                                    w.write(", ");
                                    write_abi_type(w, w.get_generic_arg_scope(index).first);
                                });

                                write_type_params(w, instance.instance.generic_type);
                            });

                        if (rcwFunctions.size() != 0 || get_category(instance.instance.generic_type) != category::delegate_type)
                        {
                            w.write("%.InitRcwHelper(%);",
                                genericInstantiationMethodsClass,
                                bind_list([](writer& w, std::string const& rcwFunction)
                                {
                                    w.write("&%", rcwFunction);
                                }, ",\n", rcwFunctions));
                        }

                        if (get_category(instance.instance.generic_type) == category::delegate_type)
                        {
                            w.write("\n%.InitCcw(&Do_Abi_Invoke);", genericInstantiationMethodsClass);
                        }
                    }),
                    bind([&](writer& w) {
                        auto guard{ w.push_generic_args(instance.instance) };

                        // Initialize any interfaces implemented by this type as they
                        // can be called by the consumer when using this interface.
                        for (auto&& iface : instance.instance.generic_type.InterfaceImpl())
                        {
                            write_ensure_generic_type_initialized(w, get_type_semantics(iface.Interface()), false);
                        }

                        // Events don't have an implementation in the generic instantiation classes
                        // given they use generated event sources and are shared in a projection.
                        // But this can mean that the instantiation classes for generic events types in
                        // generic interfaces aren't initialized.  This handles those initializations.
                        for (auto&& evt : instance.instance.generic_type.EventList())
                        {
                            write_ensure_generic_type_initialized(w, get_type_semantics(evt.EventType()), false);
                        }
                    }));
            }
        }
    }

    void write_file_header(writer& w)
    {
        w.write(R"(//------------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by cswinrt.exe version %
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
)", VERSION_STRING);
    }
}