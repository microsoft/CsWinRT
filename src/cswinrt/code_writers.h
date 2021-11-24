#pragma once

#include <functional>
#include <set>
#include <filesystem>
#include <iostream>
#include <regex>
#include <concurrent_unordered_map.h>

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

    std::string internal_if_embedded() 
    {
        return (settings.embedded) ? "internal" : "public";
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
            [](auto)
            {
                throw_invalid("type definition expected");
                #pragma warning(disable:4702)
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
        std::regex re(R"-((\ |:|<|>|,|\.))-");
        w.write("%", std::regex_replace(eventTypeName, re, "_"));
    }

    method_signature get_event_invoke_method(TypeDef const& eventType)
    {
        for (auto&& method : eventType.MethodList())
        {
            if (method.Name() == "Invoke")
            {
                return method_signature(method);
            }
        }
        throw_invalid("Event type must have an Invoke method");
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
        w.write("IntPtr thisPtr");
        for (auto&& param : signature.params())
        {
            write_abi_parameter(w, param);
        }
        write_abi_return(w, signature);
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

    void write_abi_parameter_types_pointer(writer& w, method_signature const& signature)
    {
        w.write("IntPtr");
        for (auto&& param : signature.params())
        {
            write_abi_parameter_type_pointer(w, param);
        }
        write_abi_return_type_pointer(w, signature);
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
        w.write("%.Get_%(%, %).%(value)",
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
            isSubscribeCall ? "Item1" : "Item2");
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
                            w.write_temp("%", bind<write_objref_type_name>(paramsForStaticMethodCall.value().first))));
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
        method_signature signature{ method };
        w.write(R"(
% %.%(%) => %.%(%);
)",
            return_type,
            bind<write_type_name>(method_interface, typedef_name_type::CCW, false),
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

        bool is_private = is_implemented_as_private_method(w, class_type, method);
        auto static_method_params = call_static_method.has_value() ? std::optional(std::pair(call_static_method.value(), method)) : std::nullopt;
        if (!is_private)
        {
            write_method(w, signature, method.Name(), return_type, interface_member, access_spec, method_spec, platform_attribute, 
                static_method_params);
        }

        if (is_overridable || !is_exclusive_to(method.Parent()))
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
                            w.write_temp("%", bind<write_objref_type_name>(params_for_static_getter.value().first))));
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
                        w.write_temp("%", bind<write_objref_type_name>(params_for_static_getter.value().first))));
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
                        w.write_temp("%", bind<write_objref_type_name>(params_for_static_setter.value().first))));
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
                    auto interface_name = write_type_name_temp(w, interface_type);
                    auto interface_abi_name = write_type_name_temp(w, interface_type, "%", typedef_name_type::ABI);

                    if (settings.netstandard_compat)
                    {
                        w.write(R"(
{typeof(%), new Lazy<%>(() => new %(GetReferenceForQI()))},)",
                            interface_name,
                            interface_abi_name,
                            interface_abi_name);
                    }
                    else
                    {
                        w.write(R"(
{typeof(%), new Lazy<%>(() => (%)(object)new SingleInterfaceOptimizedObject(typeof(%), _inner ?? ((IWinRTObject)this).NativeObject))},)",
                            interface_name,
                            interface_name,
                            interface_name,
                            interface_name);
                    }
                });
            }
        });

        if (numLazyInterfaces != 0)
        {
            w.write(R"(
_lazyInterfaces = new Dictionary<Type, object>(%)
{%
};
)", numLazyInterfaces, lazyInterfaces);
        }
    }

    std::string write_explicit_name(writer& w, TypeDef const& iface, std::string_view name)
    {
        return w.write_temp("%.%", write_type_name_temp(w, iface, "%", typedef_name_type::CCW), name);
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

        // ICommand has a lower-fidelity type mapping where the type of the event handler doesn't project one-to-one
        // so we need to hard-code mapping the event handler from the mapped WinRT type to the correct .NET type.
        if (event.Name() == "CanExecuteChanged" && event_type == "global::System.EventHandler<object>")
        {
            event_type = "global::System.EventHandler";
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
                            w.write_temp("%", bind<write_objref_type_name>(iface_type_semantics)), is_static));
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
                            w.write_temp("%", bind<write_objref_type_name>(iface_type_semantics)), is_static));
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
            write_event(w, event.Name(), event, interface_member, visibility, ""sv, platform_attribute, call_static_method.has_value() ? std::optional(std::tuple(call_static_method.value(), event, false)) : std::nullopt);
        }

        if (is_overridable || !is_exclusive_to(event.Parent()))
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
                    std::string sanitized_type_name;
                    sanitized_type_name.reserve(type_name.length() * 2);
                    for (const auto& c : type_name)
                    {
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
                    attribute_name != "AttributeUsage" && attribute_name != "ContractVersion")
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

    void write_composing_factory_method(writer& w, MethodDef const& method);

    void write_abi_method_with_raw_return_type(writer& w, MethodDef const& method);

    template<auto method_writer>
    std::string write_factory_cache_object(writer& w, TypeDef const& factory_type, TypeDef const& class_type);

    std::string write_static_cache_object(writer& w, std::string_view cache_type_name, TypeDef const& class_type)
    {
        auto instance =
            w.write_temp(
                "private static readonly _% _instance = new _%();",
                cache_type_name,
                cache_type_name);
        auto factoryAs = w.write_temp("%",
            bind([&](writer& w)
            {
                if (is_static(class_type))
                {
                    w.write("_factory._As");
                }
                else
                {
                    w.write("ActivationFactory<%>.As", class_type.TypeName());
                }
            })
        );

        if (settings.netstandard_compat)
        {            
        auto cache_vftbl_type = w.write_temp("ABI.%.%.Vftbl",
                class_type.TypeNamespace(),
                cache_type_name);
        auto cache_interface =
            w.write_temp(
                R"(%<%>)",
                factoryAs,
                cache_vftbl_type);
            w.write(R"(
internal class _% : ABI.%.%
{
public _%() : base(%()) { }
%
internal static % Instance => _instance;
}
)",
                cache_type_name,
                class_type.TypeNamespace(),
                cache_type_name,
                cache_type_name,
                cache_interface,
                instance,
                cache_type_name);
        }
        else
        {
            w.write(R"(
internal class _% : IWinRTObject
{
private IObjectReference _obj;
public _%()
{
_obj = %(GuidGenerator.GetIID(typeof(%.%).GetHelperType()));
}

%
internal static % Instance => (%)_instance;

IObjectReference IWinRTObject.NativeObject => _obj;
bool IWinRTObject.HasUnwrappableNativeObject => false;
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
}
)",
                cache_type_name,
                cache_type_name,
                factoryAs,
                class_type.TypeNamespace(),
                cache_type_name,
                instance,
                cache_type_name,
                cache_type_name);
        }

        return w.write_temp("_%.Instance", cache_type_name);
    }

    static std::string get_default_interface_name(writer& w, TypeDef const& type, bool abiNamespace = true, bool forceCCW = false)
    {
        return w.write_temp("%", bind<write_type_name>(get_type_semantics(get_default_interface(type)), abiNamespace ? typedef_name_type::ABI : forceCCW ? typedef_name_type::CCW : typedef_name_type::Projected, false));
    }

    void write_factory_constructors(writer& w, TypeDef const& factory_type, TypeDef const& class_type)
    {
        auto default_interface_name = get_default_interface_name(w, class_type);
        if (factory_type)
        {
            auto cache_object = write_factory_cache_object<write_abi_method_with_raw_return_type>(w, factory_type, class_type);
            auto platform_attribute = write_platform_attribute_temp(w, factory_type);

            for (auto&& method : factory_type.MethodList())
            {
                method_signature signature{ method };
                w.write(R"(
%public %(%) : this(((Func<%>)(() => {
IntPtr ptr = (%.%(%));
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
ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
%
}
)",
                    platform_attribute, 
                    class_type.TypeName(),
                    bind_list<write_projection_parameter>(", ", signature.params()),
                    settings.netstandard_compat ? default_interface_name : "IObjectReference",
                    cache_object,
                    method.Name(),
                    bind_list<write_parameter_name_with_modifier>(", ", signature.params()),
                    settings.netstandard_compat ? "new " + default_interface_name : "",
                    settings.netstandard_compat ? "" : "ComWrappersHelper.Init(_inner, false);");
            }
        }
        else
        {
            w.write(R"(
public %() : this(%(ActivationFactory<%>.ActivateInstance<IUnknownVftbl>()))
{
ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
%
}
)",
                class_type.TypeName(),
                settings.netstandard_compat ? "new " + default_interface_name : "",
                class_type.TypeName(),
                settings.netstandard_compat ? "" : "ComWrappersHelper.Init(_inner, false);");
        }
    }

    bool is_manually_generated_iface(TypeDef const& ifaceType)
    {
        if (ifaceType.TypeNamespace() == "Windows.Foundation.Collections" && ifaceType.TypeName() == "IVector`1")
        {
            return false;
        }
        if (auto mapping = get_mapped_type(ifaceType.TypeNamespace(), ifaceType.TypeName()))
        {
            return true;
        }
        return false;
    }

    void write_objref_type_name(writer& w, type_semantics const& ifaceTypeSemantics)
    {
        auto objRefTypeCode = w.write_temp("%", bind<write_type_name>(ifaceTypeSemantics, typedef_name_type::Projected, true));
        std::string objRefTypeName = "_objRef_" + objRefTypeCode;
        std::regex re(R"-((\ |:|<|>|,|\.))-");
        w.write("%", std::regex_replace(objRefTypeName, re, "_"));
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
    
                    auto objrefname = bind<write_objref_type_name>(semantics);

                    w.write(R"(
private volatile IObjectReference __%;
private IObjectReference Make__%()
{
)",
                        objrefname,
                        objrefname);
                   
                    if (replaceDefaultByInner && has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute") && distance(ifaceType.GenericParam()) == 0)
                    {
                        w.write(R"(global::System.Threading.Interlocked.CompareExchange(ref __%, _inner, null);)", objrefname);
                    }
                    else if (distance(ifaceType.GenericParam()) == 0)
                    {

                        w.write(R"(global::System.Threading.Interlocked.CompareExchange(ref __%, ((IWinRTObject)this).NativeObject.As<IUnknownVftbl>(GuidGenerator.GetIID(typeof(%).FindHelperType())), null);)", 
                            objrefname,
                            bind<write_type_name>(semantics, typedef_name_type::Projected, false)
                            ); 
                    }
                    else
                    {
                        w.write(R"(global::System.Threading.Interlocked.CompareExchange(ref __%, (IObjectReference)typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(typeof(%).FindHelperType().FindVftblType()).Invoke(((IWinRTObject)this).NativeObject, null), null);)",
                            objrefname,
                            bind<write_type_name>(semantics, typedef_name_type::Projected, false));
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
                });
        }
    }

    void write_class_static_objrefs_definition(writer& w, std::string const& target, TypeDef const& classType)
    {
        for (auto&& [interface_name, factory] : get_attributed_types(w, classType))
        {
            if (factory.statics)
            {
                auto objrefname = bind<write_objref_type_name>(factory.type);
                w.write(R"(
private static volatile IObjectReference __%;
private static IObjectReference Make__%()
{
    global::System.Threading.Interlocked.CompareExchange(ref __%, %As(GuidGenerator.GetIID(typeof(%).GetHelperType())), null);
    return __%;
}
private static IObjectReference % => __% ?? Make__%();

)",
                    objrefname,
                    objrefname,
                    objrefname,
                    target,
                    bind<write_type_name>(factory.type, typedef_name_type::Projected, false),
                    objrefname,
                    objrefname,
                    objrefname,
                    objrefname);
            }
        }
    }

    void write_composable_constructors(writer& w, TypeDef const& composable_type, TypeDef const& class_type, std::string_view visibility)
    {
        auto cache_object = write_factory_cache_object<write_composing_factory_method>(w, composable_type, class_type);
        auto default_interface_name = get_default_interface_name(w, class_type, false);
        auto default_interface_abi_name = get_default_interface_name(w, class_type);
        auto default_interface_typedef = for_typedef(w, get_type_semantics(get_default_interface(class_type)), [&](auto&& iface) { return iface; });
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
IntPtr composed = %.%(%%baseInspectable, out IntPtr ptr);
using IObjectReference composedRef = ObjectReference<IUnknownVftbl>.Attach(ref composed);
try
{
_inner = ComWrappersSupport.GetObjectReferenceForInterface(ptr);
var defaultInterface = new %(_inner);
_defaultLazy = new Lazy<%>(() => defaultInterface);
%

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
                    cache_object,
                    method.Name(),
                    bind_list<write_parameter_name_with_modifier>(", ", params_without_objects),
                    [&](writer& w) {w.write("%", params_without_objects.empty() ? " " : ", "); },
                    default_interface_abi_name,
                    default_interface_abi_name,
                    bind<write_lazy_interface_initialization>(class_type));
            }
            else
            {
                auto platform_attribute = write_platform_attribute_temp(w, composable_type);

                w.write(R"(
%% %(%)%
{
bool isAggregation = this.GetType() != typeof(%);
object baseInspectable = isAggregation ? this : null;
IntPtr composed = %.%(%%baseInspectable, out IntPtr inner);
try
{
ComWrappersHelper.Init(isAggregation, this, composed, inner, out _inner);
%
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
                    cache_object,
                    method.Name(),
                    bind_list<write_parameter_name_with_modifier>(", ", params_without_objects),
                    [&](writer& w) {w.write("%", params_without_objects.empty() ? " " : ", "); },
                    bind([&](writer& w)
                        {
                            if (is_manually_gen_default_interface)
                            {
                                w.write("_defaultLazy = new Lazy<%>(() => (%)new SingleInterfaceOptimizedObject(typeof(%), _inner));", default_interface_name, default_interface_name, default_interface_name);
                            }
                        }),
                    bind<write_lazy_interface_initialization>(class_type));
            }
        }
    }

    void write_static_method(writer& w, MethodDef const& method, std::string_view method_target, bool factory_class = false, std::string_view platform_attribute = ""sv)
    {
        if (method.SpecialName())
        {
            return;
        }
        method_signature signature{ method };
        auto return_type = w.write_temp("%", [&](writer& w) {
            write_projection_return_type(w, signature);
        });
        write_method(w, signature, method.Name(), return_type, method_target, "public "sv, factory_class ? ""sv : "static "sv, platform_attribute, settings.netstandard_compat || factory_class ? std::nullopt : std::optional(std::pair(method.Parent(), method)));
    }

    void write_static_property(writer& w, Property const& prop, std::string_view prop_target, bool factory_class = false, std::string_view platform_attribute = ""sv)
    {
        auto [getter, setter] = get_property_methods(prop);
        auto getter_target = getter ? prop_target : "";
        auto setter_target = setter ? prop_target : "";
        write_property(w, prop.Name(), prop.Name(), write_prop_type(w, prop),
            getter_target, setter_target, "public "sv, factory_class ? ""sv : "static "sv, platform_attribute, platform_attribute, 
            settings.netstandard_compat || factory_class || !getter ? std::nullopt : std::optional(std::pair(prop.Parent(), prop)), 
            settings.netstandard_compat || factory_class || !setter ? std::nullopt : std::optional(std::pair(prop.Parent(), prop)));
    }

    void write_static_event(writer& w, Event const& event, std::string_view event_target, bool factory_class = false, std::string_view platform_attribute = ""sv)
    {
        write_event(w, event.Name(), event, event_target, "public "sv, factory_class ? ""sv : "static "sv, platform_attribute,
            settings.netstandard_compat || factory_class ? std::nullopt : std::optional(std::tuple(event.Parent(), event, true)));
    }

    void write_static_members(writer& w, TypeDef const& static_type, TypeDef const& class_type)
    {
        auto cache_object = settings.netstandard_compat ? write_static_cache_object(w, static_type.TypeName(), class_type) : "";
        auto platform_attribute = write_platform_attribute_temp(w, static_type);
        w.write_each<write_static_method>(static_type.MethodList(), cache_object, false, platform_attribute);
        w.write_each<write_static_property>(static_type.PropertyList(), cache_object, false, platform_attribute);
        w.write_each<write_static_event>(static_type.EventList(), cache_object, false, platform_attribute);
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

                    if (is_static(type))
                    {
                        w.write(R"(
internal static %BaseActivationFactory _factory = new BaseActivationFactory("%", "%.%");
public static %I As<I>() => _factory.AsInterface<I>();
%
)",
                            has_base_factory ? "new " : "",
                            type.TypeNamespace(),
                            type.TypeNamespace(),
                            type.TypeName(),
                            has_base_factory ? "new " : "",
                            bind([&](writer& w)
                                {
                                    if (!settings.netstandard_compat)
                                    {
                                        write_class_static_objrefs_definition(w, "_factory._", type);
                                    }
                                }));
                    }
                    else
                    {
                        w.write(R"(
public static %I As<I>() => ActivationFactory<%>.AsInterface<I>();
%
)",
                            has_base_factory ? "new " : "",
                            type.TypeName(),
                            bind([&](writer& w)
                                {
                                    if (!settings.netstandard_compat)
                                    {
                                        write_class_static_objrefs_definition(w, 
                                            w.write_temp("ActivationFactory<%>.", type.TypeName()), 
                                            type);
                                    }
                                }));
                    }
                }

                write_static_members(w, factory.type, type);
            }
        }
    }

    void write_nongeneric_enumerable_members(writer& w, std::string_view target)
    {
        w.write(R"(
IEnumerator IEnumerable.GetEnumerator() => %.GetEnumerator();
)",
            target);
    }

    void write_enumerable_members(writer& w, std::string_view target, bool include_nongeneric, bool emit_explicit)
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

    void write_readonlydictionary_members(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
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

    void write_dictionary_members(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
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

    void write_readonlylist_members(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit)
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

    void write_list_members_using_static_abi_methods(writer& w, std::string_view target, bool include_enumerable, bool emit_explicit, std::string objref_name)
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
        
        if (!include_enumerable) return;
        auto enumerable_type = emit_explicit ? w.write_temp("IEnumerable<%>.", element) : "";
        w.write(R"(
%IEnumerator<%> %GetEnumerator() => %.GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
)",
            visibility, element, enumerable_type, target);
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

    void write_notify_data_error_info_members(writer& w, std::string_view target, bool emit_explicit)
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

    void write_custom_mapped_type_members(writer& w, std::string_view target, mapped_type const& mapping, bool is_private, bool call_static_abi_methods, std::string objref_name)
    {
        if (mapping.abi_name == "IIterable`1") 
        {
            write_enumerable_members(w, target, true, is_private);
        }
        else if (mapping.abi_name == "IIterator`1") 
        {
            write_enumerator_members(w, target, is_private);
        }
        else if (mapping.abi_name == "IMapView`2") 
        {
            write_readonlydictionary_members(w, target, false, is_private);
        }
        else if (mapping.abi_name == "IMap`2") 
        {
            write_dictionary_members(w, target, false, is_private);
        }
        else if (mapping.abi_name == "IVectorView`1")
        {
            write_readonlylist_members(w, target, false, is_private);
        }
        else if (mapping.abi_name == "IVector`1")
        {
            if (call_static_abi_methods)
            {
                write_list_members_using_static_abi_methods(w, target, false, is_private, objref_name);
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
            write_idisposable_members(w, target, is_private);
        }
        else if (mapping.mapped_namespace == "System.ComponentModel" && mapping.mapped_name == "INotifyDataErrorInfo")
        {
            write_notify_data_error_info_members(w, target, is_private);
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
        }

        throw_invalid("Could not find property getter interface");
    }

    void write_class_members(writer& w, TypeDef const& type, bool wrapper_type)
    {
        std::map<std::string, std::tuple<std::string, std::string, std::string, std::string, std::string, bool, bool, bool, std::optional<std::pair<type_semantics, Property>>, std::optional<std::pair<type_semantics, Property>>>> properties;
        for (auto&& ii : type.InterfaceImpl())
        {
            auto semantics = get_type_semantics(ii.Interface());

            auto write_class_interface = [&](TypeDef const& interface_type)
            {
                auto interface_name = write_type_name_temp(w, interface_type);
                auto interface_abi_name = write_type_name_temp(w, interface_type, "%", typedef_name_type::ABI);

                auto is_default_interface = has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute");
                auto static_iface_target = w.write_temp("%", bind<write_type_name>(semantics, typedef_name_type::StaticAbiClass, true));
                auto target = wrapper_type ? write_type_name_temp(w, interface_type, "((%) _comp)") :
                        (is_default_interface ? "_default" : write_type_name_temp(w, interface_type, "AsInternal(new InterfaceTag<%>())"));
                if (!is_default_interface && !wrapper_type)
                {
                    if (settings.netstandard_compat)
                    {
                        w.write(R"(
private % AsInternal(InterfaceTag<%> _) => ((Lazy<%>)_lazyInterfaces[typeof(%)]).Value;
)",
                            interface_name,
                            interface_name,
                            interface_abi_name,
                            interface_name);
                    }
                    else if (is_manually_generated_iface(interface_type))
                    {
                        w.write(R"(
private % AsInternal(InterfaceTag<%> _) =>  ((Lazy<%>)_lazyInterfaces[typeof(%)]).Value;
)",
                            interface_name,
                            interface_name,
                            interface_name,
                            interface_name);
                    }
                }

                bool call_static_method = !(settings.netstandard_compat || wrapper_type || is_manually_generated_iface(interface_type));

                if(auto mapping = get_mapped_type(interface_type.TypeNamespace(), interface_type.TypeName()); mapping && mapping->has_custom_members_output)
                {
                    bool is_private = is_implemented_as_private_mapped_interface(w, type, interface_type);
                    auto objref_name = w.write_temp("%", bind<write_objref_type_name>(semantics));
                    write_custom_mapped_type_members(w, target, *mapping, is_private, call_static_method, objref_name);
                    return;
                }

                auto is_overridable_interface = has_attribute(ii, "Windows.Foundation.Metadata", "OverridableAttribute");
                auto is_protected_interface = has_attribute(ii, "Windows.Foundation.Metadata", "ProtectedAttribute");

                auto platform_attribute = write_platform_attribute_temp(w, interface_type);
                
                w.write_each<write_class_method>(interface_type.MethodList(), type, is_overridable_interface, is_protected_interface, target, platform_attribute, call_static_method ? std::optional(semantics) : std::nullopt);
                w.write_each<write_class_event>(interface_type.EventList(), type, is_overridable_interface, is_protected_interface, target, platform_attribute, call_static_method ? std::optional(semantics) : std::nullopt);

                
                // Merge property getters/setters, since such may be defined across interfaces
                for (auto&& prop : interface_type.PropertyList())
                {
                    MethodDef getter, setter;
                    std::tie(getter, setter) = get_property_methods(prop);
                    auto prop_type = write_prop_type(w, prop);
                    auto is_private = getter && is_implemented_as_private_method(w, type, getter);  // for explicitly implemented interfaces, assume there is always a get.
                    auto property_name = is_private ? w.write_temp("%.%", interface_name, prop.Name()) : std::string(prop.Name());
                    auto [prop_targets, inserted]  = properties.try_emplace(property_name,
                        prop_type,
                        getter ? target : "",
                        getter ? platform_attribute : "",
                        setter ? target : "",
                        setter ? platform_attribute : "",
                        is_overridable_interface,
                        !is_protected_interface && !is_overridable_interface, // By default, an overridable member is protected.
                        is_private,
                        call_static_method && getter ? std::optional(std::pair(semantics, prop)) : std::nullopt,
                        call_static_method && setter ? std::optional(std::pair(semantics, prop)) : std::nullopt
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
                            getter_prop = call_static_method ? std::optional(std::pair(semantics, prop)) : std::nullopt;
                        }
                        if (setter)
                        {
                            XLANG_ASSERT(setter_target.empty());
                            setter_target = target;
                            setter_platform = platform_attribute;
                            setter_prop = call_static_method ? std::optional(std::pair(semantics, prop)) : std::nullopt;
                        }
                        is_overridable |= is_overridable_interface;
                        is_public |= !is_overridable_interface && !is_protected_interface;
                        XLANG_ASSERT(!getter_target.empty() || !setter_target.empty());
                    }
                    // If this interface is overridable then we need to emit an explicit implementation of the property for that interface.
                    if (is_overridable_interface || !is_exclusive_to(interface_type))
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
            for_typedef(w, semantics, [&](auto type)
            {
                write_class_interface(type);
            });
        }

        // Write properties with merged accessors
        for (auto& [prop_name, prop_data] : properties)
        {
            auto& [prop_type, getter_target, getter_platform, setter_target, setter_platform, is_overridable, is_public, is_private, getter_prop, setter_prop] = prop_data;
            if (is_private) continue;
            std::string_view access_spec = is_public ? "public "sv : "protected "sv;
            std::string_view method_spec = is_overridable ? "virtual "sv : ""sv;
            write_property(w, prop_name, prop_name, prop_type, getter_target, setter_target, access_spec, method_spec, getter_platform, setter_platform, getter_prop, setter_prop);
        }
    }

    void write_winrt_attribute(writer& w, TypeDef const& type)
    {
        std::filesystem::path db_path(type.get_database().path());
        w.write(R"([global::WinRT.WindowsRuntimeType("%")])",
db_path.stem().string());
    }

    auto get_invoke_info(writer& w, MethodDef const& method)
    {
        TypeDef const& type = method.Parent();
        if (!settings.netstandard_compat && distance(type.GenericParam()) == 0)
        {
            return std::pair{
                w.write_temp("(*(delegate* unmanaged[Stdcall]<%, int>**)ThisPtr)[%]",
                    bind<write_abi_parameter_types>(method_signature { method }),
                    get_vmethod_index(type, method) + 6 /* number of methods in IInspectable */),
                false
            };
        }
        auto vmethod_name = get_vmethod_name(w, type, method);
        return std::pair{
            "_obj.Vftbl." + vmethod_name,
            abi_signature_has_generic_parameters(w, method_signature { method })};
    };

    void write_static_class(writer& w, TypeDef const& type)
    {
        w.write(R"(%%% static class %
{
%})",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, true),
            internal_if_embedded(),
            bind<write_type_name>(type, typedef_name_type::Projected, false),
            bind<write_attributed_types>(type)
        );
    }

    void write_event_source_generic_args(writer& w, cswinrt::type_semantics eventTypeSemantics);

    void write_event_source_ctor(writer& w, Event const& evt, int index)
    {
        if (for_typedef(w, get_type_semantics(evt.EventType()), [&](TypeDef const& eventType)
            {
                if ((eventType.TypeNamespace() == "Windows.Foundation" || eventType.TypeNamespace() == "System") && eventType.TypeName() == "EventHandler`1")
                {
                    auto [add, remove] = get_event_methods(evt);
                    w.write(R"( new EventSource__EventHandler%(_obj,
%,
%,
%))",
bind<write_type_params>(eventType),
get_invoke_info(w, add).first,
get_invoke_info(w, remove).first,
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
            get_invoke_info(w, add).first,
            get_invoke_info(w, remove).first,
            index);
    }

    void write_event_sources(writer& w, TypeDef const& type)
    {
        for (auto&& evt : type.EventList())
        {
            w.write(R"(
private EventSource<%> _%;)",
bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
evt.Name());
        }
    }

    void write_event_source_table(writer& w, Event const& evt)
    {
        w.write(R"(
private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, EventSource<%>> _%_;
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, EventSource<%>> Make%Table()
{
    global::System.Threading.Interlocked.CompareExchange(ref _%_, new(), null);
    return _%_;
}
private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, EventSource<%>> _% => _%_ ?? Make%Table();
)",
            bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
            evt.Name(),
            bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
            evt.Name(),
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
            w.write("%.CreateMarshaler%(%)",
                marshaler_type,
                is_array() ? "Array" : "",
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

            w.write("% = %.CreateMarshaler%(%);\n",
                get_marshaler_local(w),
                marshaler_type,
                is_array() ? "Array" : "",
                bind<write_escaped_identifier>(param_name));

            if (is_generic() || is_array())
            {
                w.write("% = %.GetAbi%(%);\n",
                    get_param_local(w),
                    marshaler_type,
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
                        is_out() ? "out " : "", param_name,
                        is_out() ? "out " : "", param_name);
                    return;
                }

                if (is_out())
                {
                    w.write("%__%", "out ", param_name);
                    return;
                }

                if (is_object_in())
                {
                    w.write("%%.ThisPtr", source, bind<write_escaped_identifier>(param_name));
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
                    w.write("%%",
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

            w.write("%.GetAbi%(%%)",
                marshaler_type,
                is_array() ? "Array" : "",
                is_pinnable ? "ref " : "",
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

            w.write("%.FromAbi%(%)",
                marshaler_type,
                is_array() ? "Array" : "",
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
                    marshaler_type,
                    is_array() ? "Array" : "",
                    get_marshaler_local(w));
            }
        }
    };

    void set_abi_marshaler(writer& w, TypeSig const& type_sig, abi_marshaler& m, std::string_view prop_name = "")
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
                    m.local_type = m.is_out() ? "IntPtr" : "IObjectReference";
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
                    m.local_type = m.is_out() ? "IntPtr" : "IObjectReference";
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
                    m.local_type = m.is_out() ? "IntPtr" : "IObjectReference";
                }
                break;
            }
        };

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
                    m.local_type = m.is_out() ? "IntPtr" : "IObjectReference";
                }
            },
            [&](type_definition const& type)
            {
                set_typedef_marshaler(m, type);
            },
            [&](generic_type_index const& /*var*/)
            {
                m.param_type = w.write_temp("%", bind<write_projection_type>(semantics));
                m.marshaler_type = w.write_temp("Marshaler<%>", m.param_type);
                m.local_type = "object";
            },
            [&](generic_type_instance const& type)
            {
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

    auto get_abi_marshalers(writer& w, method_signature const& signature, bool is_generic, std::string_view prop_name = "", bool raw_return_type = false)
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
            set_abi_marshaler(w, param.second->Type(), m, prop_name);
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
                set_abi_marshaler(w, ret.Type(), m, prop_name);
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

    void write_abi_method_call_marshalers(writer& w, std::string_view invoke_target, bool is_generic, std::vector<abi_marshaler> const& marshalers, bool has_noexcept_attr = false)
    {
        auto write_abi_invoke = [&](writer& w)
        {
            bool have_pinnables{};
            w.write("%",
                bind_each([&](writer& w, abi_marshaler const& m)
                {
                    have_pinnables |= m.write_pinnable(w);
                }, marshalers));
            if (have_pinnables)
            {
                bool write_delimiter{};
                w.write("fixed(void* %)\n{\n",
                    bind_each([&](writer& w, abi_marshaler const& m)
                    {
                        m.write_fixed_expression(w, write_delimiter);
                    }, marshalers));
            }
            if (is_generic)
            {
                w.write("%.DynamicInvokeAbi(__params);\n", invoke_target);
            }
            else if (!has_noexcept_attr)
            {
                w.write("global::WinRT.ExceptionHelpers.ThrowExceptionForHR(%(ThisPtr%));\n",
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
        if (is_generic)
        {
            w.write("var __params = new object[]{ ThisPtr");
            for (auto&& m : marshalers)
            {
                w.write(", ");
                if (m.is_array()) w.write("null, null");
                else if (!m.is_out() && m.marshaler_type.empty()) m.write_marshal_to_abi(w);
                else w.write("null");
            }
            w.write(" };\n");
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

    void write_abi_method_call(writer& w, method_signature signature, std::string_view invoke_target, bool is_generic, bool raw_return_type = false, bool has_noexcept_attr = false)
    {
        write_abi_method_call_marshalers(w, invoke_target, is_generic, get_abi_marshalers(w, signature, is_generic, "", raw_return_type), has_noexcept_attr);
    }

    void write_abi_method_with_raw_return_type(writer& w, MethodDef const& method)
    {
        if (is_special(method))
        {
            return;
        }

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
        w.write(R"(
public unsafe %% %(%)
{%}
)",
            // In the .NET Standard 2.0 code-gen, the fully-projected signature will be available in the base class, so we need to specify new to hide it
            settings.netstandard_compat ? "new " : "", 
            bind(write_raw_return_type, signature),
            method.Name(),
            bind_list<write_projection_parameter>(", ", signature.params()),
            bind<write_abi_method_call>(signature, invoke_target, is_generic, true, is_noexcept(method)));
    }


    void write_composing_factory_method(writer& w, MethodDef const& method)
    {
        if (is_special(method))
        {
            return;
        }

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

        w.write(R"(
public unsafe % %(%)
{%}
)",
            bind(write_raw_return_type, signature),
            method.Name(),
            bind(write_composable_constructor_params, signature),
            bind<write_abi_method_call_marshalers>(invoke_target, is_generic, abi_marshalers, is_noexcept(method)));
    }
    
    template<auto method_writer>
    std::string write_factory_cache_object(writer& w, TypeDef const& factory_type, TypeDef const& class_type)
    {
        std::string_view cache_type_name = factory_type.TypeName();
        if (settings.netstandard_compat)
        {
            auto cache_vftbl_type = w.write_temp("ABI.%.%.Vftbl", class_type.TypeNamespace(), cache_type_name);
            auto cache_interface =
                w.write_temp(
                    R"(ActivationFactory<%>.As<%>)",
                    class_type.TypeName(),
                    cache_vftbl_type);

            w.write(R"(
internal class _% : ABI.%.%
{
public _%() : base(%()) { }
private static _% _instance = new _%();
internal static _% Instance => _instance;
%
}
)",
                cache_type_name,
                class_type.TypeNamespace(),
                cache_type_name,
                cache_type_name,
                cache_interface,
                cache_type_name,
                cache_type_name,
                cache_type_name,
                bind_each<method_writer>(factory_type.MethodList())
                );
        }
        else
        {
            w.write(R"(
internal class _% : IWinRTObject
{
private IObjectReference _obj;
private IntPtr ThisPtr => _obj.ThisPtr;
public _%()
{
_obj = ActivationFactory<%>.As(GuidGenerator.GetIID(typeof(%.%).GetHelperType()));
}

private static _% _instance = new _%();
internal static _% Instance => _instance;

IObjectReference IWinRTObject.NativeObject => _obj;
bool IWinRTObject.HasUnwrappableNativeObject => false;
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
                cache_type_name,
                cache_type_name,
                class_type.TypeName(),
                class_type.TypeNamespace(),
                cache_type_name,
                cache_type_name,
                cache_type_name,
                cache_type_name,
                bind_each<method_writer>(factory_type.MethodList()));
        }

        return w.write_temp("_%.Instance", cache_type_name);
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
            bind<write_abi_method_call>(signature, invoke_target, is_generic, false, is_noexcept(method)));
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
bind<write_abi_method_call_marshalers>(invoke_target, is_generic, marshalers, is_noexcept(prop)));
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
bind<write_abi_method_call_marshalers>(invoke_target, is_generic, marshalers, is_noexcept(prop)));
            }
            w.write("}\n");
        }

        int index = 0;
        for (auto&& evt : type.EventList())
        {
            auto semantics = get_type_semantics(evt.EventType());
            auto event_source = w.write_temp(settings.netstandard_compat ? "_%" : "Get_%()", evt.Name());
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
        if (is_exclusive_to(type))
        {
            return;
        }

        bool generic_type = distance(type.GenericParam()) > 0;

        auto init_call_variables = [&](writer& w)
        {
            if (!settings.netstandard_compat)
            {
                w.write("\nvar _obj = ((%)((IWinRTObject)this).GetObjectReferenceForType(typeof(%).TypeHandle));",
                    generic_type ? "ObjectReference<Vftbl>" : "IObjectReference",
                    bind<write_type_name>(type, typedef_name_type::CCW, false));
            }
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
%unsafe %% %%(%)
{%
%%;
}
)",
                settings.netstandard_compat ? "public " : "",
                (settings.netstandard_compat && method.Name() == "ToString"sv) ? "override " : "",
                bind<write_projection_return_type>(signature),
                bind([&](writer& w)
                    {
                        if (!settings.netstandard_compat)
                        {
                            w.write("%.", bind<write_type_name>(type, typedef_name_type::CCW, false));
                        }
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
                        settings.netstandard_compat ? typedef_name_type::ABI : typedef_name_type::CCW);
                    auto getter_cast = settings.netstandard_compat ? "As<%>()"s : "((%)(IWinRTObject)this)"s;
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
            auto event_source = w.write_temp(settings.netstandard_compat ? "_%" : "Get_%()", evt.Name());
            w.write(R"(
%event % %%
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
                bind(init_call_variables),
                bind<write_abi_event_source_static_method_call>(type, evt, true, "_obj", false),
                bind(init_call_variables),
                bind<write_abi_event_source_static_method_call>(type, evt, false, "_obj", false));
            index++;
        }
    }

    void write_static_abi_class_members(writer& w, TypeDef const& iface)
    {
        bool generic_type = distance(iface.GenericParam()) > 0;

        auto init_call_variables = [&](writer& w)
        {
            if (generic_type)
            {
                w.write("\nvar _obj = (ObjectReference<%.Vftbl>)_genericObj;", bind<write_type_name>(iface, typedef_name_type::ABI, false));
            }
            w.write("\nvar ThisPtr = _obj.ThisPtr;\n");   
        };

        for (auto&& method : iface.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }
            method_signature signature{ method };
            auto [invoke_target, is_generic] = get_invoke_info(w, method);
            w.write(R"(
public static unsafe %% %(IObjectReference %%%)
{%%}
)",
                (settings.netstandard_compat && method.Name() == "ToString"sv) ? "override " : "",
                bind<write_projection_return_type>(signature),
                method.Name(),
                generic_type ? "_genericObj" : "_obj",
                signature.has_params() ? ", " : "",
                bind_list<write_projection_parameter>(", ", signature.params()),
                bind(init_call_variables),
                bind<write_abi_method_call>(signature, invoke_target, is_generic, false, is_noexcept(method)));
        }

        for (auto&& prop : iface.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);

            if (getter)
            {
                auto [invoke_target, is_generic] = get_invoke_info(w, getter);
                auto signature = method_signature(getter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                w.write(R"(public static unsafe % get_%(IObjectReference %)
{%%}
)",
                    write_prop_type(w, prop),
                    prop.Name(),
                    generic_type ? "_genericObj" : "_obj",
                    bind(init_call_variables),
                    bind<write_abi_method_call_marshalers>(invoke_target, is_generic, marshalers, is_noexcept(prop)));
            }
            if (setter)
            {
                auto [invoke_target, is_generic] = get_invoke_info(w, setter);
                auto signature = method_signature(setter);
                auto marshalers = get_abi_marshalers(w, signature, is_generic, prop.Name());
                marshalers[0].param_name = "value";
                w.write(R"(public static unsafe void set_%(IObjectReference %, % value)
{%%}
)",                 
                    prop.Name(),
                    generic_type ? "_genericObj" : "_obj",
                    write_prop_type(w, prop),
                    bind(init_call_variables),
                    bind<write_abi_method_call_marshalers>(invoke_target, is_generic, marshalers, is_noexcept(prop)));
            }
            w.write("\n");
        }

        int index = 0;
        for (auto&& evt : iface.EventList())
        {
                    w.write(R"(%
public static unsafe (Action<%>, Action<%>) Get_%(IObjectReference %, object _thisObj)
{
var eventSource = _%.GetValue(_thisObj, (key) =>
{
%
return %;
});
return (eventSource.Subscribe, eventSource.Unsubscribe);
}
)",
                        bind<write_event_source_table>(evt),
                        bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
                        bind<write_type_name>(get_type_semantics(evt.EventType()), typedef_name_type::Projected, false),
                        evt.Name(),
                        generic_type ? "_genericObj" : "_obj",
                        evt.Name(),
                        bind(init_call_variables),
                        bind<write_event_source_ctor>(evt, index)
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

                if (mapping->abi_name == "IIterable`1") // IEnumerable`1
                {
                    auto element = w.write_temp("%", bind<write_generic_type_name>(0));
                    required_interfaces[std::move(interface_name)] =
                    {
                        w.write_temp("%", bind<write_enumerable_members>(
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
                        w.write_temp("%", bind<write_readonlydictionary_members>(
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
                        w.write_temp("%", bind<write_dictionary_members>(
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
                        w.write_temp("%", bind<write_readonlylist_members>(
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
                return;
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

    void write_guid_attribute(writer& w, TypeDef const& type)
    {
        auto fully_qualify_guid = (type.TypeNamespace() == "Windows.Foundation.Metadata");

        auto attribute = get_attribute(type, "Windows.Foundation.Metadata", "GuidAttribute");
        if (!attribute)
        {
            throw_invalid("'Windows.Foundation.Metadata.GuidAttribute' attribute for type '", type.TypeNamespace(), ".", type.TypeName(), "' not found");
        }

        auto args = attribute.Value().FixedArgs();

        using std::get;

        auto get_arg = [&](decltype(args)::size_type index) { return get<ElemSig>(args[index].value).value; };

        w.write_printf(R"([%s("%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X")])",
            fully_qualify_guid ? "global::System.Runtime.InteropServices.Guid" : "Guid",
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
    
    std::string get_vmethod_delegate_type(writer& w, MethodDef const& method, std::string vmethod_name)
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
                generic_abi_type, byref ? ".MakeByRefType()" : ""), generic_param });
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
        }
        w.write(have_generic_params ? "(void* thisPtr" : "(IntPtr thisPtr");
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
                if (settings.netstandard_compat || have_generic_params)
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

    auto get_managed_marshalers(writer& w, method_signature const& signature, bool is_generic)
    {
        std::vector<managed_marshaler> marshalers;

        auto set_marshaler = [is_generic](writer& w, type_semantics const& semantics, managed_marshaler& m)
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
                [&](generic_type_index const& /*var*/)
                {
                    m.param_type = get_generic_abi_type(w, semantics).second;
                    m.local_type = w.write_temp("%", bind<write_projection_type>(semantics));
                    m.marshaler_type = w.write_temp("Marshaler<%>", m.local_type);
                    m.abi_boxed = true;
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    set_typedef_marshaler(type.generic_type);
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
            m.use_pointers = !settings.netstandard_compat && !is_generic;
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
            return std::pair{ marshalers, m };
        }

        return std::pair{ marshalers, managed_marshaler{} };
    }

    void write_managed_method_call(writer& w, method_signature signature, std::string invoke_expression_format)
    {
        auto generic_abi_types = get_generic_abi_types(w, signature);
        bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
            [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();
        auto managed_marshalers = get_managed_marshalers(w, signature, have_generic_params);
        auto marshalers = managed_marshalers.first;
        auto return_marshaler = managed_marshalers.second;
        auto return_sig = signature.return_signature();
        
        w.write(
R"(%
%
try
{
%
%%
}
catch (Exception __exception__)
{
global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
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

        auto generic_type = distance(method.Parent().GenericParam()) > 0;
        method_signature signature{ method };
        auto return_sig = signature.return_signature();
        auto type_name = write_type_name_temp(w, method.Parent());
        auto vmethod_name = get_vmethod_name(w, method.Parent(), method);

        auto generic_abi_types = get_generic_abi_types(w, signature);
        bool have_generic_params = std::find_if(generic_abi_types.begin(), generic_abi_types.end(),
            [](auto&& pair) { return !pair.second.empty(); }) != generic_abi_types.end();

        w.write(
            R"(
%
private static unsafe int Do_Abi_%%
{
%
})",
            !settings.netstandard_compat && !generic_type ? "[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]" : "",
            vmethod_name,
            bind<write_abi_signature>(method),
            bind<write_managed_method_call>(
                signature,
                w.write_temp("global::WinRT.ComWrappersSupport.FindObject<%>(%).%%",
                    type_name,
                    have_generic_params ? "new IntPtr(thisPtr)" : "thisPtr",
                    method.Name(),
                    "(%)")));
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
                    "%")));
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
                        "%")));
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
                w.write("public static Guid PIID = GuidGenerator.CreateIID(typeof(%));\n", type_name);
                w.write(R"(%
internal unsafe Vftbl(IntPtr thisPtr) : this()
{
var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
var vftbl = (IntPtr*)vftblPtr.Vftbl;
IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
%}
)",
                    bind_each([&](writer& w, MethodDef const& method)
                    {
                        auto vmethod_name = get_vmethod_name(w, type, method);

                        if (abi_signature_has_generic_parameters(w, method_signature{ method }))
                        {
                            auto generic_abi_types = get_generic_abi_types(w, method_signature{ method });

                            w.write("public static readonly Type %_Type = Expression.GetDelegateType(new Type[]{ typeof(void*), %typeof(int) });\n",
                                vmethod_name,
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
var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * %);
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
AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * %);
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
        w.write("%%internal class % {}\n",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, false),
            bind<write_type_name>(type, typedef_name_type::CCW, false));
    }

    void write_contract(writer& w, TypeDef const& type)
    {
        auto type_name = write_type_name_temp(w, type);
        w.write(R"(%public enum %
{
}
)",
            bind<write_type_custom_attributes>(type, false),
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
            internal_if_embedded(),
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
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::CCW);

        w.write(R"(%%
%% interface %%
{%
}
)",
            // Interface
            bind<write_winrt_attribute>(type),
            bind<write_guid_attribute>(type),
            bind<write_type_custom_attributes>(type, false),
            is_exclusive_to(type) || (is_projection_internal(type) || settings.embedded) ? "internal" : "public",
            type_name,
            bind<write_type_inheritance>(type, object_type{}, false, false),
            bind<write_interface_member_signatures>(type)
        );
    }

    bool write_abi_interface_netstandard(writer& w, TypeDef const& type)
    {
        XLANG_ASSERT(get_category(type) == category::interface_type);
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::ABI);
        auto nongenerics_class = w.write_temp("%_Delegates", bind<write_typedef_name>(type, typedef_name_type::ABI, false));
        auto is_generic = distance(type.GenericParam()) > 0;
        std::vector<std::string> nongeneric_delegates;

        std::map<std::string, required_interface> required_interfaces;
        write_required_interface_members_for_abi_type(w, type, required_interfaces, true);

        w.write(R"([global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
%
% unsafe class % : %
{
%
internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr)%
public static implicit operator %(IObjectReference obj) => (obj != null) ? new %(obj) : null;
protected readonly ObjectReference<Vftbl> _obj;
public IObjectReference ObjRef { get => _obj; }
public IntPtr ThisPtr => _obj.ThisPtr;
public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
public A As<A>() => _obj.AsType<A>();
public @(IObjectReference obj) : this(obj.As<Vftbl>()) {}
internal @(ObjectReference<Vftbl> obj)
{
_obj = obj;%
%}
%%%%}
)",
            // Interface abi implementation
            bind<write_guid_attribute>(type),
            internal_if_embedded(),
            type_name,
            bind<write_type_name>(type, typedef_name_type::CCW, false),
            // Vftbl
            bind<write_vtable>(type, type_name, nongenerics_class, nongeneric_delegates),
            // Interface impl
            [&](writer& w) {
                if (!is_generic)
                {
                    w.write(" => ObjectReference<Vftbl>.FromAbi(thisPtr);\n");
                    return;
                }
                w.write(R"(
{
if (thisPtr == IntPtr.Zero)
{
return null;
}
var vftblT = new Vftbl(thisPtr);
return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT);
}
public static Guid PIID = Vftbl.PIID;
)");
            },
            type_name,
            type_name,
            type.TypeName(),
            type.TypeName(),
            [&](writer& w) 
            {
                int index = 0;
                for (auto&& evt : type.EventList())
                {
                    w.write("_% = %;\n", evt.Name(), bind<write_event_source_ctor>(evt, index++));
                }
            },
            [&](writer& w) {
                for (auto required_interface : required_interfaces)
                {
                    if (required_interface.second.helper_wrapper.empty()) 
                        continue;
                    w.write("% = new %.FromAbiHelper(ObjRef);\n", 
                        required_interface.second.adapter,
                        required_interface.second.helper_wrapper);
                }
            },
            [&](writer& w) {
                for (auto required_interface : required_interfaces)
                {
                    if (required_interface.second.helper_wrapper.empty())
                        continue;
                    w.write("%.FromAbiHelper %;\n",
                        required_interface.second.helper_wrapper,
                        required_interface.second.adapter);
                }
            },
            bind<write_interface_members_netstandard>(type),
            bind<write_event_sources>(type),
            [&](writer& w) {
                for (auto required_interface : required_interfaces)
                {
                    w.write("%", required_interface.second.members);
                }
            }
        );

        if (!nongeneric_delegates.empty())
        {
            w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
% static class %
{
%}
)",
                internal_if_embedded(),
                nongenerics_class,
                bind_each(nongeneric_delegates));
        }
        w.write("\n");

        return true;
    }

    void write_static_abi_classes(writer& w, TypeDef const& iface)
    {
        w.write(R"(% static class %
{
%
}
)", 
        is_exclusive_to(iface) ? "internal" : internal_if_embedded(), 
        bind<write_type_name>(iface, typedef_name_type::StaticAbiClass, false), 
        bind<write_static_abi_class_members>(iface));
    }

    bool write_abi_interface(writer& w, TypeDef const& type)
    {

        bool is_generic = distance(type.GenericParam()) > 0;
        XLANG_ASSERT(get_category(type) == category::interface_type);
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::ABI);

        auto nongenerics_class = w.write_temp("%_Delegates", bind<write_typedef_name>(type, typedef_name_type::ABI, false));

        std::vector<std::string> nongeneric_delegates;

        std::map<std::string, required_interface> required_interfaces;
        write_required_interface_members_for_abi_type(w, type, required_interfaces, false);

        w.write(R"(%
%
internal unsafe interface % : %
{
%%%%%}
)",
// Interface abi implementation
            is_exclusive_to(type) ? "" : "[DynamicInterfaceCastableImplementation]",
            bind<write_guid_attribute>(type),
            type_name,
            bind<write_type_name>(type, typedef_name_type::CCW, false),
            [&](writer& w) {
                w.write(distance(type.GenericParam()) > 0 ? "public static Guid PIID = Vftbl.PIID;\n\n" : "");
            },
            // Vftbl
            bind([&](writer& w)
            {
                auto methods = type.MethodList();
                if (is_generic)
                {
                    write_vtable(w, type, type_name, nongenerics_class, nongeneric_delegates);
                }
                else
                {
                    w.write(R"(
public static IntPtr AbiToProjectionVftablePtr;
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
                for (auto required_interface : required_interfaces)
                {
                    w.write("%", required_interface.second.members);
                }
            }
        );

        if (!nongeneric_delegates.empty())
        {
            w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
% static class %
{
%}
)",
internal_if_embedded(),
nongenerics_class,
bind_each(nongeneric_delegates));
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
if (IsOverridableInterface(iid) || typeof(global::WinRT.IInspectable).GUID == iid)
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
                    w.write("GuidGenerator.GetIID(typeof(%)) == iid",
                        bind<write_type_name>(get_type_semantics(iface.Interface()), typedef_name_type::ABI, false));
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
        auto base_semantics = get_type_semantics(type.Extends());
        auto from_abi_new = !std::holds_alternative<object_type>(base_semantics) ? "new " : "";

        w.write(R"(%[global::WinRT.ProjectedRuntimeClass(typeof(%))]
%internal %class %%
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
        bind<write_class_members>(type, true),
        wrapped_type_name);
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

        auto gc_pressure_amount = 0;
        if (auto gc_pressure_attr = get_attribute(type, "Windows.Foundation.Metadata", "GCPressureAttribute"))
        {
            auto sig = gc_pressure_attr.Value();
            auto const& args = sig.NamedArgs();
            auto amount = std::get<int32_t>(std::get<ElemSig::EnumValue>(std::get<ElemSig>(args[0].value.value).value).value);
            gc_pressure_amount = amount == 0 ? 12000 : amount == 1 ? 120000 : 1200000;
        }

        w.write(R"(%[global::WinRT.ProjectedRuntimeClass(nameof(_default))]
%% %class %%, IEquatable<%>
{
public %IntPtr ThisPtr => _default.ThisPtr;

private IObjectReference _inner = null;
private readonly Lazy<%> _defaultLazy;
private readonly Dictionary<Type, object> _lazyInterfaces;

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
%
%}
%

public static bool operator ==(% x, % y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
public static bool operator !=(% x, % y) => !(x == y);
public bool Equals(% other) => this == other;
public override bool Equals(object obj) => obj is % that && this == that;
public override int GetHashCode() => ThisPtr.GetHashCode();
%

private struct InterfaceTag<I>{};

private % AsInternal(InterfaceTag<%> _) => _default;
%%
}
)",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, false),
            internal_if_embedded(),
            bind<write_class_modifiers>(type),
            type_name,
            bind<write_type_inheritance>(type, base_semantics, true, false),
            type_name,
            derived_new,
            default_interface_abi_name,
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
            bind<write_lazy_interface_initialization>(type),
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
            type_name,
            type_name,
            bind([&](writer& w)
            {
                bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(type.Extends()));

                if (!type.Flags().Sealed())
                {
                    w.write(R"(
protected %(global::WinRT.DerivedComposed _)%
{
_defaultLazy = new Lazy<%>(() => GetDefaultReference<%.Vftbl>());
%
})",
                        type.TypeName(),
                        has_base_type ? ":base(_)" : "",
                        default_interface_abi_name,
                        default_interface_abi_name,
                        bind<write_lazy_interface_initialization>(type));
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
            bind<write_class_members>(type, false),
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

        auto type_name = write_type_name_temp(w, type);
        auto default_interface_name = get_default_interface_name(w, type, false);
        auto base_semantics = get_type_semantics(type.Extends());
        auto derived_new = std::holds_alternative<object_type>(base_semantics) ? "" : "new ";

        auto default_interface_typedef = for_typedef(w, get_type_semantics(get_default_interface(type)), [&](auto&& iface) { return iface; });
        auto is_manually_gen_default_interface = is_manually_generated_iface(default_interface_typedef);

        w.write(R"(%
[global::WinRT.ProjectedRuntimeClass(nameof(_default))]
[global::WinRT.ObjectReferenceWrapper(nameof(_inner))]
%% %class %%, IWinRTObject, IEquatable<%>
{
private IntPtr ThisPtr => _inner == null ? (((IWinRTObject)this).NativeObject).ThisPtr : _inner.ThisPtr;

private IObjectReference _inner = null;
%
private readonly Dictionary<Type, object> _lazyInterfaces;

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
_inner = objRef.As(GuidGenerator.GetIID(typeof(%).GetHelperType()));
%
%
}

public static bool operator ==(% x, % y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
public static bool operator !=(% x, % y) => !(x == y);
public bool Equals(% other) => this == other;
public override bool Equals(object obj) => obj is % that && this == that;
public override int GetHashCode() => ThisPtr.GetHashCode();
%

private struct InterfaceTag<I>{};
%
%%
}
)",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, true),
            internal_if_embedded(),
            bind<write_class_modifiers>(type),
            type_name,
            bind<write_type_inheritance>(type, base_semantics, true, false),
            type_name,
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
                    w.write("private % _default => %;", default_interface_name, is_manually_gen_default_interface ? "_defaultLazy.Value" : "null");
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
            default_interface_name,
            bind([&](writer& w)
                {
                    if (is_manually_gen_default_interface)
                    {
                        w.write("_defaultLazy = new Lazy<%>(() => (%)new SingleInterfaceOptimizedObject(typeof(%), _inner));", default_interface_name, default_interface_name, default_interface_name);
                    }
                }),
            bind<write_lazy_interface_initialization>(type),
            // Equality operators
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            type_name,
            bind([&](writer& w)
            {
                bool has_base_type = !std::holds_alternative<object_type>(get_type_semantics(type.Extends()));
                if (!type.Flags().Sealed())
                {
                    w.write(R"(
protected %(global::WinRT.DerivedComposed _)%
{
%
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
                            }),
                        bind<write_lazy_interface_initialization>(type));
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
            bind<write_class_members>(type, false),
            bind<write_custom_query_interface_impl>(type));
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
public static IntPtr FromManaged(% obj) => obj is null ? IntPtr.Zero : CreateMarshaler(obj).GetRef();
public static unsafe MarshalInterfaceHelper<%>.MarshalerArray CreateMarshalerArray(%[] array) => MarshalInterfaceHelper<%>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));
public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<%>.GetAbiArray(box);
public static unsafe %[] FromAbiArray(object box) => MarshalInterfaceHelper<%>.FromAbiArray(box, FromAbi);
public static (int length, IntPtr data) FromManagedArray(%[] array) => MarshalInterfaceHelper<%>.FromManagedArray(array, (o) => FromManaged(o));
public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable<object>.DisposeMarshaler(value);
public static void DisposeMarshalerArray(MarshalInterfaceHelper<%>.MarshalerArray array) => MarshalInterfaceHelper<%>.DisposeMarshalerArray(array);
public static void DisposeAbi(IntPtr abi) => MarshalInspectable<object>.DisposeAbi(abi);
public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);
}
)",
            internal_if_embedded(),
            abi_type_name,
            bind([&](writer& w)
            {
                bool is_exclusive_to_default = false;
                for_typedef(w, get_type_semantics(get_default_interface(type)), [&](auto&& type)
                {
                    is_exclusive_to_default = is_exclusive_to(type);
                });
                if (is_exclusive_to_default)
                {
                    auto default_interface_abi_name = get_default_interface_name(w, type, true);
                    w.write("public static IObjectReference CreateMarshaler(% obj) => obj is null ? null : MarshalInspectable<%>.CreateMarshaler(obj)%;",
                        projected_type_name,
                        projected_type_name,
                        bind([&](writer& w)
                        {
                            if (distance(type.GenericParam()) > 0)
                            {
                                w.write(".As<%.Vftbl>()", default_interface_abi_name);
                            }
                            else
                            {
                                w.write(
                                    ".As<IUnknownVftbl>(GuidGenerator.GetIID(typeof(%).GetHelperType()))",
                                    bind<write_type_name>(get_type_semantics(get_default_interface(type)), typedef_name_type::CCW, false));
                            }
                        }));
                }
                else
                {
                    auto default_interface_name = get_default_interface_name(w, type, false);
                    w.write("public static IObjectReference CreateMarshaler(% obj) => obj is null ? null : MarshalInterface<%>.CreateMarshaler(obj);",
                        projected_type_name,
                        default_interface_name);
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
        w.write(R"(%%% delegate % %(%);
)",
            bind<write_winrt_attribute>(type),
            bind<write_type_custom_attributes>(type, false),
            internal_if_embedded(),
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

        w.write(R"([global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
%
% static class @%
{%
%
private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
public static readonly IntPtr AbiToProjectionVftablePtr;

static unsafe @()
{%
AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
{
IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
Invoke = %
};
var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(@%), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
AbiToProjectionVftablePtr = nativeVftbl;
}
%
public static unsafe IObjectReference CreateMarshaler(% managedDelegate) => 
managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, GuidGenerator.GetIID(typeof(@%)));

public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<%>.GetAbi(value);

public static unsafe % FromAbi(IntPtr nativeDelegate)
{
var abiDelegate = ComWrappersSupport.GetObjectReferenceForInterface<IDelegateVftbl>(nativeDelegate);
return abiDelegate is null ? null : (%)ComWrappersSupport.TryRegisterObjectForInterface(new %(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
}

[global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
#if !NET
private class NativeDelegateWrapper
#else
private class NativeDelegateWrapper : IWinRTObject
#endif
{
private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
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
{
IntPtr ThisPtr = _nativeDelegate.ThisPtr;
%%
}
}

public static IntPtr FromManaged(% managedDelegate) => CreateMarshaler(managedDelegate)?.GetRef() ?? IntPtr.Zero;

public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<%>.DisposeMarshaler(value);

public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<%>.DisposeAbi(abi);
%
private static unsafe int Do_Abi_Invoke%
{
%
}
}

)",
            bind<write_guid_attribute>(type),
            internal_if_embedded(),
            type.TypeName(),
            type_params,
            [&](writer& w) {
                if (type_params.empty()) return;
                w.write(R"(
public static Guid PIID = GuidGenerator.CreateIID(typeof(%));)",
                    type_name
                );
            },
            [&](writer& w) {
                if (!is_generic)
                {
                    if (settings.netstandard_compat)
                    {
                        w.write("private unsafe delegate int Abi_Invoke(%);\n",
                            bind<write_abi_parameters>(signature));
                    }
                    return;
                }
                w.write(R"(private static readonly Type Abi_Invoke_Type = Expression.GetDelegateType(new Type[] { typeof(void*), %typeof(int) });
)",
                    bind_each([&](writer& w, auto&& pair)
                    {
                        w.write("%, ", pair.first);
                    }, generic_abi_types));
            },
            // class constructor
            type.TypeName(),
            [&](writer& w) {
                if (!is_generic)
                {
                    if (settings.netstandard_compat)
                    {
                        w.write("\nAbiInvokeDelegate = new Abi_Invoke(Do_Abi_Invoke);");
                    }
                    return;
                }
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
            },
            bind([&](writer& w)
            {
                if (settings.netstandard_compat || is_generic)
                {
                    w.write("Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)");
                }
                else
                {
                    w.write("(IntPtr)(delegate* unmanaged[Stdcall]<%, int>)&Do_Abi_Invoke", bind<write_abi_parameter_types_pointer>(signature));
                }
            }),
            type.TypeName(),
            type_params,
            settings.netstandard_compat || is_generic ? "\npublic static global::System.Delegate AbiInvokeDelegate { get; }\n" : "",
            // CreateMarshaler
            type_name,
            type.TypeName(),
            type_params,
            // GetAbi
            type_name,
            // FromAbi
            type_name,
            type_name,
            type_name,
            // NativeDelegateWrapper.Invoke
            bind<write_projection_return_type>(signature),
            bind_list<write_projection_parameter>(", ", signature.params()),
            bind([&](writer& w)
            {
                if (is_generic || settings.netstandard_compat)
                {
                    w.write("var abiInvoke = Marshal.GetDelegateForFunctionPointer%(_nativeDelegate.Vftbl.Invoke%);",
                        is_generic ? "" : "<Abi_Invoke>",
                        is_generic ? ", Abi_Invoke_Type" : "");
                }
                else
                {
                    w.write("var abiInvoke = (delegate* unmanaged[Stdcall]<%, int>)(_nativeDelegate.Vftbl.Invoke);",
                        bind<write_abi_parameter_types>(signature));
                }
            }),
            bind<write_abi_method_call>(signature, "abiInvoke", is_generic, false, is_noexcept(method)),
            // FromManaged
            type_name,
            // DisposeMarshaler
            type_name,
            // DisposeAbi
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
                }
                w.write("(void* thisPtr");
                int index = 0;
                for (auto&& param : signature.params())
                {
                    auto generic_type = generic_abi_types[index++].second;
                    auto param_cat = get_param_category(param);
                    if (!generic_type.empty() && (param_cat <= param_category::out))
                    {
                        w.write(", %% %",
                            param_cat == param_category::out ? "out " : "",
                            generic_type,
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
                        w.write(", out % %", generic_type, signature.return_param_name());
                    }
                    else
                    {
                        write_abi_return(w, signature);
                    }
                }
                w.write(")");
            },
            [&](writer& w) {
                if (settings.netstandard_compat)
                {
                    write_managed_method_call(w, signature,
                        w.write_temp(R"(
global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(%, (% invoke) =>
{
    %
}))",
                            is_generic ? "new IntPtr(thisPtr)" : "thisPtr",
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
                            })
                        ));
                }
                else
                {
                    write_managed_method_call(w, signature,
                        w.write_temp(R"(
global::WinRT.ComWrappersSupport.FindObject<%>(%).Invoke(%)
)",
                            type_name,
                            is_generic ? "new IntPtr(thisPtr)" : "thisPtr",
                            "%"
                        ));
                }
            });
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

        w.write(R"(%%% enum % : %
{
)", 
        bind<write_winrt_attribute>(type),
        bind<write_type_custom_attributes>(type, true),
        internal_if_embedded(),
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

        w.write(R"(%%% struct %: IEquatable<%>
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
            bind<write_type_custom_attributes>(type, true),
            internal_if_embedded(),
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
            internal_if_embedded(),
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
                    if(m.is_value_type) return;
                    w.write("%.DisposeMarshaler(_%);\n",
                        m.marshaler_type,
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
Func<bool> dispose = () => { m.Dispose(); return false; };
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
                        m.marshaler_type,
                        m.param_name);
                }
            });
        if (have_disposers)
        {
            w.write(R"(
}
catch (Exception) when (dispose())
{
// Will never execute
return default;
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
public static void DisposeAbi(% abi){ /*todo*/ }
}

)",
            abi_type);
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
                w.write_each<write_static_method>(factory.type.MethodList(), projected_type_name, true, ""sv);
                w.write_each<write_static_property>(factory.type.PropertyList(), projected_type_name, true, ""sv);
                w.write_each<write_static_event>(factory.type.EventList(), projected_type_name, true, ""sv);
                }
            }
        }
    }


    void write_factory_class(writer& w, TypeDef const& type)
    {
        auto factory_type_name = write_type_name_temp(w, type, "%ServerActivationFactory", typedef_name_type::CCW);
        auto base_class = (is_static(type) || !has_default_constructor(type)) ?
            "ComponentActivationFactory" : write_type_name_temp(w, type, "ActivatableComponentActivationFactory<%>", typedef_name_type::Projected);
        auto type_name = write_type_name_temp(w, type, "%", typedef_name_type::Projected);

        w.write(R"(
internal class % : %%
{

static %()
{
RuntimeHelpers.RunClassConstructor(typeof(%).TypeHandle);
}

public static IntPtr Make()
{
using var marshaler = MarshalInspectable<%>.CreateMarshaler(_factory).As<ABI.WinRT.Interop.IActivationFactory.Vftbl>();
return marshaler.GetRef();
}

static readonly % _factory = new %();
public static ObjectReference<I> ActivateInstance<I>()
{
IntPtr instance = _factory.ActivateInstance();
return ObjectReference<IInspectable.Vftbl>.Attach(ref instance).As<I>();
}

%
}
)",
factory_type_name,
base_class,
bind<write_factory_class_inheritance>(type),
factory_type_name,
type_name,
factory_type_name,
factory_type_name,
factory_type_name,
bind<write_factory_class_members>(type)
);
    }

    void write_module_activation_factory(writer& w, std::set<TypeDef> const& types)
    {
        w.write(R"(
using System;
namespace WinRT
{
% static class Module
{
public static unsafe IntPtr GetActivationFactory(String runtimeClassId)
{%
return IntPtr.Zero;
}
}
}
)",
    internal_if_embedded(),
bind_each([](writer& w, TypeDef const& type)
    {
        w.write(R"(

if (runtimeClassId == "%.%")
{
return %ServerActivationFactory.Make();
}
)",
type.TypeNamespace(),
type.TypeName(),
bind<write_type_name>(type, typedef_name_type::CCW, true)
);
    },
    types
        ));
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
        auto abiTypeName = w.write_temp("%", bind<write_type_name>(eventTypeSemantics, typedef_name_type::ABI, true));
        for_typedef(w, eventTypeSemantics, [&](TypeDef const& eventType)
            {
                if ((eventType.TypeNamespace() == "Windows.Foundation" || eventType.TypeNamespace() == "System") && eventType.TypeName() == "EventHandler`1")
                {
                    return;
                }
                auto eventTypeCode = w.write_temp("%", bind<write_type_name>(eventType, typedef_name_type::Projected, false));
                auto invokeMethodSig = get_event_invoke_method(eventType);
                w.write(R"(
internal sealed unsafe class %% : EventSource<%>
{
internal %(IObjectReference obj,
delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler, int index) : base(obj, addHandler, removeHandler, index)
{
}

protected override IObjectReference CreateMarshaler(% del) =>
del is null ? null : %.CreateMarshaler(del);

protected override void DisposeMarshaler(IObjectReference marshaler) =>
%.DisposeMarshaler(marshaler);

protected override System.IntPtr GetAbi(IObjectReference marshaler) =>
marshaler is null ? System.IntPtr.Zero : %.GetAbi(marshaler);

protected override State CreateEventState() =>
new EventState(_obj.ThisPtr, _index);

private sealed class EventState : State
{
public EventState(System.IntPtr obj, int index)
: base(obj, index)
{
}

protected override System.Delegate GetEventInvoke()
{
% invoke = (%) =>
{
var localDel = (%) del;
if (localDel == null)
{%
return %;
}
%localDel.Invoke(%);
};
return invoke;
}
}
}
)",
bind<write_event_source_type_name>(eventTypeSemantics),
bind<write_event_source_generic_args>(eventTypeSemantics),
eventTypeCode, 
bind<write_event_source_type_name>(eventTypeSemantics), 
eventTypeCode,
abiTypeName,
abiTypeName,
abiTypeName,
eventTypeCode,
bind<write_event_invoke_params>(invokeMethodSig),
eventTypeCode,
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
}
