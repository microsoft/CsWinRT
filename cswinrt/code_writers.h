#pragma once

#include <functional>
#include <set>

namespace cswinrt
{
    using namespace winmd::reader;

    static inline bool starts_with(std::string_view const& value, std::string_view const& match) noexcept
    {
        return 0 == value.compare(0, match.size(), match);
    }

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
        {"WinRT.HString", "WinRT.HString"},
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

    bool is_type_blittable(type_semantics const& semantics)
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
            [&](generic_type_index const& /*type*/)
            {
                return false;
            },
            [&](fundamental_type const& type)
            {
                return (type != fundamental_type::String) && (type != fundamental_type::Boolean);
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

    void write_projection_type(writer& w, type_semantics const& semantics);

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

    void write_typedef_name(writer& w, type_definition const& type, bool abiNamespace = false, bool forceWriteNamespace = false)
    {
        if (forceWriteNamespace || ((type.TypeNamespace() != w._current_namespace) || (abiNamespace != w._in_abi_namespace)))
        {
            if (abiNamespace)
            {
                w.write("ABI.");
            }
            else if (w._in_abi_namespace)
            {
                // E.g. disambiguate 'Foo.Bar' from 'ABI.Foo.Bar' in the 'ABI' namespace
                w.write("global::");
            }

            w.write("%.", type.TypeNamespace());
        }
        w.write("@", type.TypeName());
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

    void write_type_name(writer& w, type_semantics const& semantics, bool abiNamespace = false, bool forceWriteNamespace = false)
    {
        for_typedef(w, semantics, [&](TypeDef const& type)
        {
            write_typedef_name(w, type, abiNamespace, forceWriteNamespace);
            write_type_params(w, type);
        });
    }

    auto write_type_name_temp(writer& w, type_semantics const& type, char const* format = "%", bool abiNamespace = false)
    {
        return w.write_temp(format, bind<write_type_name>(type, abiNamespace, false));
    }

    void write_projection_type(writer& w, type_semantics const& semantics)
    {
        call(semantics,
            [&](object_type) { w.write("IInspectable"); },
            [&](guid_type) { w.write("Guid"); },
            [&](type_definition const& type) { write_typedef_name(w, type); },
            [&](generic_type_index const& var) { write_generic_type_name(w, var.index); },
            [&](generic_type_instance const& type)
            {
                auto guard{ w.push_generic_args(type) };
                w.write("%<%>",
                    bind<write_projection_type>(type.generic_type),
                    bind_list<write_projection_type>(", ", type.generic_args));
            },
            [&](generic_type_param const& param) { w.write(param.Name()); },
            [&](fundamental_type const& type) { write_fundamental_type(w, type); });
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

    void write_parameter_name_with_modifier(writer& w, method_signature::param_t const& param, bool with_modifier = true)
    {
        if (with_modifier)
        {
            switch (get_param_category(param))
            {
            case param_category::out:
                w.write("out ");
                break;
            default:
                break;
            }
        }

        write_escaped_identifier(w, param.first.Name());
    }

    void write_parameter_name(writer& w, method_signature::param_t const& param)
    {
        write_parameter_name_with_modifier(w, param, false);
    }

    void write_projection_parameter_type(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::in:
            w.write("%", bind<write_projection_type>(semantics));
            break;
        case param_category::out:
            w.write("out %", bind<write_projection_type>(semantics));
            break;
        case param_category::pass_array:
            w.write("/*pass_array*/ %[]", bind<write_projection_type>(semantics));
            break;
        case param_category::fill_array:
            w.write("/*fill_array*/ %[]", bind<write_projection_type>(semantics));
            break;
        case param_category::receive_array:
            w.write("/*receive_array*/ %[]", bind<write_projection_type>(semantics));
            break;
        }
    }

    void write_method_return_type(writer& w, method_signature const& signature)
    {
        if (signature.return_signature())
        {
            auto semantics = get_type_semantics(signature.return_signature().Type());
            write_projection_type(w, semantics);
        }
        else
        {
            w.write("void");
        }
    }

    void write_projection_method_parameter(writer& w, method_signature::param_t const& param)
    {
        w.write("% %",
            bind<write_projection_parameter_type>(param),
            bind<write_parameter_name>(param));
    }

    void write_abi_type(writer& w, type_semantics const& semantics)
    {
        call(semantics,
            [&](object_type) { w.write("IntPtr"); },
            [&](guid_type) { w.write("Guid"); },
            [&](type_definition const& type)
            {
                switch (get_category(type))
                {
                    case category::enum_type:
                        write_type_name(w, type);
                        break;

                    case category::struct_type:
                        write_type_name(w, type, !is_type_blittable(semantics));
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
                        type =  fundamental_type::UInt8;
                    }
                    write_fundamental_type(w, type);
                }
            });
    }

    void write_abi_parameter_type(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::in:
            w.write("[In] %", bind<write_abi_type>(semantics));
            break;
        case param_category::out:
            w.write("[Out] out %", bind<write_abi_type>(semantics));
            break;
        case param_category::pass_array:
            w.write("/*pass_array*/ %[]", bind<write_abi_type>(semantics));
            break;
        case param_category::fill_array:
            w.write("/*fill_array*/ %[]", bind<write_abi_type>(semantics));
            break;
        case param_category::receive_array:
            w.write("/*receive_array*/ %[]", bind<write_abi_type>(semantics));
            break;
        }
    }

    void write_abi_parameters(writer& w, method_signature const& signature, bool void_thisptr)
    {
        w.write("% thisPtr", void_thisptr ? "void*" : "IntPtr");

        for (auto&& param : signature.params())
        {
            w.write(", % %", bind<write_abi_parameter_type>(param), bind<write_parameter_name>(param));
        }

        if (signature.return_signature())
        {
            auto semantics = get_type_semantics(signature.return_signature().Type());
            w.write(", [Out] out % %", bind<write_abi_type>(semantics), signature.return_param_name());
        }
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

    void write_delegate_helper_call(writer& w, TypeDef const& type, std::string_view call, std::string_view name)
    {
        w.write("%Helper%.%(%)",
            bind<write_typedef_name>(type, false, false),
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
                w.write("(%)", bind<write_type_name>(type, false, false));
            }
            w.write("%", name);
            return;
        }
        case category::delegate_type:
        {
            write_delegate_helper_call(w, type, "FromAbi", name);
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
                w.write("%.FromAbi(%)", bind<write_type_name>(param_type, true, true), name);
            }
            return;
        }
        case category::interface_type:
        {
            w.write("MarshalInterface<%, %>.FromAbi(%)",
                bind<write_type_name>(type, false, false),
                bind<write_type_name>(type, true, false),
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
        default:
            w.write("%", name);
            break;
        }
    }

    void write_fundamental_marshal_from_abi(writer& w, fundamental_type type, std::string_view name, bool is_boxed = false)
    {
        if (type == fundamental_type::String)
        {
            w.write(R"(new WinRT.HString(%))", name);
        }
        else if (type == fundamental_type::Boolean)
        {
            if (is_boxed)
            {
                w.write("((byte)(object)% != 0)", name);
            }
            else
            {
                w.write("(% != 0)", name);
            }
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

    std::string_view const OwnerMemberName = "_WinRT_Owner";

    void write_event_param_marshaler(writer& w, method_signature::param_t const& param)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
        call(semantics,
            [&](object_type)
            {
                w.write("(IntPtr value) => (obj.ThisPtr == value && % is IInspectable) ? (IInspectable)% : IInspectable.FromAbi(value)", OwnerMemberName, OwnerMemberName);
            },
            [&](type_definition const& type)
            {
                w.write(R"((IntPtr value) => (obj.ThisPtr == value && % is %) ? (%)% : %)",
                    OwnerMemberName,
                    bind<write_projection_type>(semantics),
                    bind<write_projection_type>(semantics),
                    OwnerMemberName,
                    bind<write_object_marshal_from_abi>(semantics, type, "value"sv, true));
            },
            [&](generic_type_index const& var)
            {
                write_type(w.get_generic_arg(var.index));
            },
            [&](generic_type_instance const& type)
            {
                auto guard{ w.push_generic_args(type) };
                w.write(R"((IntPtr value) => (obj.ThisPtr == value && % is %) ? (%)% : %)",
                    OwnerMemberName,
                    bind<write_projection_type>(semantics),
                    bind<write_projection_type>(semantics),
                    OwnerMemberName,
                    bind<write_object_marshal_from_abi>(semantics, type.generic_type, "value"sv, true));
            },
            [&](fundamental_type type)
            {
                w.write(R"((IntPtr value) => %)",
                    bind<write_fundamental_marshal_from_abi>(type, "value"sv, true));
            },
            [](auto) {});
        };
        write_type(get_type_semantics(param.second->Type()));
    }

    void write_event_param_marshalers(writer& w, row_base<Event>::value_type const& evt)
    {
        auto write_params = [&](writer& w, method_signature const& invoke_sig)
        {
            w.write(",\n    %", bind_list<write_event_param_marshaler>(",\n    ", invoke_sig.params()));
        };
        write_event_params(w, evt, write_params);
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

    void write_method(writer& w, method_signature signature, std::string_view method_name, 
        std::string_view return_type, std::string_view method_target,
        std::string_view access_spec = ""sv, std::string_view method_spec = ""sv)
    {
        w.write(R"(
%%% %(%) => %.%(%);
)",
            access_spec,
            method_spec,
            return_type,
            method_name,
            bind_list<write_projection_method_parameter>(", ", signature.params()),
            method_target,
            method_name,
            bind_list<write_parameter_name_with_modifier>(", ", signature.params(), true)
        );
    }

    void write_explicitly_implemented_method(writer& w, MethodDef const& method, 
        std::string_view return_type, TypeDef const& method_interface, std::string_view method_target)
    {
        method_signature signature{ method };
        w.write(R"(
% %.%(%) => %.%(%);
)",
            return_type,
            bind<write_type_name>(method_interface, false, false),
            method.Name(),
            bind_list<write_projection_method_parameter>(", ", signature.params()),
            method_target,
            method.Name(),
            bind_list<write_parameter_name_with_modifier>(", ", signature.params(), true)
        );
    }

    void write_class_method(writer& w, MethodDef const& method, bool is_overridable, bool is_protected, std::string_view interface_member)
    {
        if (method.SpecialName())
        {
            return;
        }

        bool write_explicit_implementation = is_protected || is_overridable;
        auto access_spec = is_protected ? "protected " : "public ";
        std::string method_spec = "";

        if (is_overridable)
        {
            // All overridable methods in the WinRT type system have protected visibility.
            access_spec = "protected ";
            method_spec = "virtual ";
        }

        method_signature signature{ method };
        
        auto raw_return_type = w.write_temp("%", bind([&](writer& w) {
            write_method_return_type(w, signature);
        }));
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
                            write_explicit_implementation = true;
                        }
                    }
                }
            }
        }

        write_method(w, signature, method.Name(), return_type, interface_member, access_spec, method_spec);

        if (write_explicit_implementation)
        {
            write_explicitly_implemented_method(w, method, raw_return_type, method.Parent(), interface_member);
        }
    }

    void write_property(writer& w, std::string_view external_prop_name, std::string_view prop_name, 
        std::string_view prop_type, std::string_view getter_target, std::string_view setter_target, 
        std::string_view access_spec = ""sv, std::string_view method_spec = ""sv)
    {
        if (setter_target.empty())
        {
            w.write(R"(
%%% % => %.%;
)",
                access_spec,
                method_spec,
                prop_type,
                external_prop_name,
                getter_target,
                prop_name);
        }
        else
        {
            w.write(R"(
%%% %
{
get => %.%;
set => %.% = value;
}
)",
                access_spec,
                method_spec,
                prop_type,
                external_prop_name,
                getter_target,
                prop_name,
                setter_target,
                prop_name);
        }
    }

    std::string write_as_cast(writer& w, TypeDef const& iface, bool as_abi)
    {
        return w.write_temp(as_abi ? "As<%>()" : "AsInternal(new InterfaceTag<%>())",
            bind<write_type_name>(iface, as_abi, false));
    }

    std::string write_explicit_name(writer& w, TypeDef const& iface, std::string_view name)
    {
        return w.write_temp("%.%", write_type_name_temp(w, iface), name);
    }

    std::string write_prop_type(writer& w, Property const& prop)
    {
        return w.write_temp("%", bind<write_projection_type>(get_type_semantics(prop.Type().Type())));
    }

    void write_explicitly_implemented_property(writer& w, Property const& prop, TypeDef const& iface, bool as_abi)
    {
        auto prop_target = write_as_cast(w, iface, as_abi);
        auto [getter, setter] = get_property_methods(prop);
        auto getter_target = getter ? prop_target : "";
        auto setter_target = setter ? prop_target : "";
        write_property(w, write_explicit_name(w, iface, prop.Name()), prop.Name(), 
            write_prop_type(w, prop), getter_target, setter_target);
    }

    void write_event(writer& w, std::string_view external_event_name, Event const& event, std::string_view event_target, 
        std::string_view access_spec = ""sv, std::string_view method_spec = ""sv)
    {
        w.write(R"(
%%event WinRT.EventHandler% %
{
add => %.% += value;
remove => %.% -= value;
}
)",
            access_spec,
            method_spec,
            bind<write_event_param_types>(event),
            external_event_name,
            event_target,
            event.Name(),
            event_target,
            event.Name());
    }

    void write_explicitly_implemented_event(writer& w, Event const& evt, TypeDef const& iface, bool as_abi)
    {
        write_event(w, write_explicit_name(w, iface, evt.Name()), evt, write_as_cast(w, iface, as_abi));
    }

    void write_class_event(writer& w, Event const& event, bool is_overridable, bool is_protected, std::string_view interface_member)
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
        write_event(w, event.Name(), event, interface_member, visibility);

        if (is_protected || is_overridable)
        {
            write_explicitly_implemented_event(w, event, event.Parent(), false);
        }
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

    std::string write_cache_object(writer& w, std::string_view cache_type_name, TypeDef const& class_type, bool is_static = false)
    {
        auto cache_interface =
            is_static ?
                w.write_temp(
                    R"((new BaseActivationFactory("%", "%.%"))._As<ABI.%.%.Vftbl>)",
                    class_type.TypeNamespace(),
                    class_type.TypeNamespace(),
                    class_type.TypeName(),
                    class_type.TypeNamespace(),
                    cache_type_name) :
                w.write_temp(
                    R"(ActivationFactory<%>.As<ABI.%.%.Vftbl>)",
                    class_type.TypeName(),
                    class_type.TypeNamespace(),
                    cache_type_name);

        w.write(R"(
internal class _% : ABI.%.%
{
public _%() : base(%()) { }
private static WeakLazy<_%> _instance = new WeakLazy<_%>();
internal static % Instance => _instance.Value;
}
)",
            cache_type_name,
            class_type.TypeNamespace(),
            cache_type_name,
            cache_type_name,
            cache_interface,
            cache_type_name,
            cache_type_name,
            cache_type_name);

        return w.write_temp("_%.Instance", cache_type_name);
    }

    static std::string get_default_interface_name(writer& w, TypeDef const& type, bool abiNamespace = true)
    {
        return w.write_temp("%", bind<write_type_name>(get_type_semantics(get_default_interface(type)), abiNamespace, false));
    }

    void write_factory_constructors(writer& w, TypeDef const& factory_type, TypeDef const& class_type)
    {
        if (factory_type)
        {
            auto cache_object = write_cache_object(w, factory_type.TypeName(), class_type);

            for (auto&& method : factory_type.MethodList())
            {
                method_signature signature{ method };
                w.write(R"(
public %(%) : this(%.%(%)._default) {}
)",
                    class_type.TypeName(),
                    bind_list<write_projection_method_parameter>(", ", signature.params()),
                    cache_object,
                    method.Name(),
                    bind_list<write_parameter_name_with_modifier>(", ", signature.params(), true));
            }
        }
        else
        {
            auto default_interface_name = get_default_interface_name(w, class_type);
            w.write(R"(
public %() : this(new %(ActivationFactory<%>.ActivateInstance<%.Vftbl>())){}
)",
                class_type.TypeName(),
                default_interface_name,
                class_type.TypeName(),
                default_interface_name);
        }
    }

    void write_composable_constructors(writer& w, TypeDef const& composable_type, TypeDef const& class_type)
    {
        auto cache_object = write_cache_object(w, composable_type.TypeName(), class_type);

        for (auto&& method : composable_type.MethodList())
        {
            method_signature signature{ method };
            auto default_interface_name = get_default_interface_name(w, class_type);
            auto params_without_objects = signature.params();
            params_without_objects.pop_back();
            params_without_objects.pop_back();

            w.write(R"(
public %(%) : this(((Func<%>)(() => {
IInspectable baseInspectable = null;
IInspectable innerInspectable;
return %.%(%%baseInspectable, out innerInspectable)._default;
}))()){}
)",
                class_type.TypeName(),
                bind_list<write_projection_method_parameter>(", ", params_without_objects),
                default_interface_name,
                cache_object,
                method.Name(),
                bind_list<write_parameter_name_with_modifier>(", ", params_without_objects, true),
                bind([&](writer& w){w.write("%", params_without_objects.empty() ? " " : ", "); }));
        }
    }

    void write_static_method(writer& w, MethodDef const& method, std::string_view method_target)
    {
        if (method.SpecialName())
        {
            return;
        }
        method_signature signature{ method };
        auto return_type = w.write_temp("%", bind([&](writer& w) {
            write_method_return_type(w, signature);
        }));
        write_method(w, signature, method.Name(), return_type, method_target, "public "sv, "static "sv);
    }

    void write_static_property(writer& w, Property const& prop, std::string_view prop_target)
    {
        auto [getter, setter] = get_property_methods(prop);
        auto getter_target = getter ? prop_target : "";
        auto setter_target = setter ? prop_target : "";
        write_property(w, prop.Name(), prop.Name(), write_prop_type(w, prop), 
            getter_target, setter_target, "public "sv, "static "sv);
    }

    void write_static_event(writer& w, Event const& event, std::string_view event_target)
    {
        write_event(w, event.Name(), event, event_target, "public "sv, "static "sv);
    }

    void write_static_members(writer& w, TypeDef const& static_type, TypeDef const& class_type)
    {
        auto cache_object = write_cache_object(w, static_type.TypeName(), class_type, true);
        w.write_each<write_static_method>(static_type.MethodList(), cache_object);
        w.write_each<write_static_property>(static_type.PropertyList(), cache_object);
        w.write_each<write_static_event>(static_type.EventList(), cache_object);
    }

    void write_attributed_types(writer& w, TypeDef const& type)
    {
        for (auto&& [interface_name, factory] : get_attributed_types(w, type))
        {
            if (factory.activatable)
            {
                write_factory_constructors(w, factory.type, type);
            }
            else if (factory.composable && factory.visible)
            {
                write_composable_constructors(w, factory.type, type);
            }
            else if (factory.statics)
            {
                write_static_members(w, factory.type, type);
            }
        }
    }

    void write_class_members(writer& w, TypeDef const& type)
    {
        std::map<std::string_view, std::tuple<std::string, std::string, std::string, bool, bool>> properties;
        for (auto&& ii : type.InterfaceImpl())
        {
            auto semantics = get_type_semantics(ii.Interface());

            auto write_class_interface = [&](TypeDef const& interface_type)
            {
                auto interface_name = write_type_name_temp(w, interface_type);
                auto interface_abi_name = write_type_name_temp(w, interface_type, "%", true);

                auto is_default_interface = has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute");
                auto target = is_default_interface ? "_default" : write_type_name_temp(w, interface_type, "AsInternal(new InterfaceTag<%>())");
                if (!is_default_interface)
                {
                    w.write(R"(
private % AsInternal(InterfaceTag<%> _) => new %(_default.AsInterface<%.Vftbl>());
)",
                        interface_name,
                        interface_name,
                        interface_abi_name,
                        interface_abi_name);
                }

                auto is_overridable_interface = has_attribute(ii, "Windows.Foundation.Metadata", "OverridableAttribute");
                auto is_protected_interface = has_attribute(ii, "Windows.Foundation.Metadata", "ProtectedAttribute");

                // temporary, to fix ToggleSwitch.OnToggled, etc - overridable/protected logic needs review
                if (type.Flags().Sealed())
                {
                    is_overridable_interface = false;
                    is_protected_interface = false;
                }

                w.write_each<write_class_method>(interface_type.MethodList(), is_overridable_interface, is_protected_interface, target);
                w.write_each<write_class_event>(interface_type.EventList(), is_overridable_interface, is_protected_interface, target);

                // If this interface is overidable but the type is sealed, make the interface act as though it is protected.
                // If we don't do this, then the C# compiler errors out about declaring a virtual member in a sealed class.
                if (is_overridable_interface && type.Flags().Sealed())
                {
                    is_overridable_interface = false;
                    is_protected_interface = true;
                }

                // Merge property getters/setters, since such may be defined across interfaces
                // Since a property has to either be overridable or not, 
                for (auto&& prop : interface_type.PropertyList())
                {
                    auto [getter, setter] = get_property_methods(prop);
                    auto prop_type = write_prop_type(w, prop);
                    auto [prop_targets, inserted]  = properties.try_emplace(prop.Name(),
                        std::move(prop_type),
                        std::move(getter ? target : ""),
                        std::move(setter ? target : ""),
                        is_overridable_interface,
                        !is_protected_interface && !is_overridable_interface // By default, an overridable member is protected.
                        );
                    if (!inserted)
                    {
                        auto& [property_type, getter_target, setter_target, is_overridable, is_public] = prop_targets->second;
                        XLANG_ASSERT(property_type == prop_type);
                        if (getter)
                        {
                            XLANG_ASSERT(getter_target.empty());
                            getter_target = target;
                        }
                        if (setter)
                        {
                            XLANG_ASSERT(setter_target.empty());
                            setter_target = target;
                        }
                        is_overridable |= is_overridable_interface;
                        is_public |= !is_overridable_interface && !is_protected_interface;
                        XLANG_ASSERT(!getter_target.empty() || !setter_target.empty());
                    }

                    // If this interface is overridable or protected then we need to emit an explicit implementation of the property for that interface.
                    if (is_overridable_interface || is_protected_interface)
                    {
                        write_explicitly_implemented_property(w, prop, interface_type, false);
                    }
                }
            };
            for_typedef(w, semantics, [&](TypeDef const& type)
            {
                write_class_interface(type);
            });
        }

        // Write properties with merged accessors
        for (auto& [prop_name, prop_data] : properties)
        {
            auto& [prop_type, getter_target, setter_target, is_overridable, is_public] = prop_data;
            std::string_view access_spec = is_public ? "public "sv : "protected "sv;
            std::string_view method_spec = is_overridable ? "virtual "sv : ""sv;
            write_property(w, prop_name, prop_name, prop_type, getter_target, setter_target, access_spec, method_spec);
        }
    }

    void write_static_class(writer& w, TypeDef const& type)
    {
        w.write(R"(public static class %
{
%})",
            bind<write_type_name>(type, false, false),
            bind<write_attributed_types>(type)
        );
    }

    void write_event_source_ctors(writer& w, TypeDef const& type)
    {
        uint32_t const vtable_base = type.MethodList().first.index();
        for (auto&& evt : type.EventList())
        {
            auto [add, remove] = get_event_methods(evt);
            w.write(R"(

_% =
    new EventSource%(_obj,
    _obj.Vftbl.add_%_%,
    _obj.Vftbl.remove_%_%%);)",
                evt.Name(),
                bind<write_event_param_types>(evt),
                evt.Name(),
                add.index() - vtable_base,
                evt.Name(),
                remove.index() - vtable_base,
                bind<write_event_param_marshalers>(evt));
        }
    }

    void write_event_sources(writer& w, TypeDef const& type)
    {
        for (auto&& evt : type.EventList())
        {
            w.write(R"(
private EventSource% _%;)",
                bind<write_event_param_types>(evt),
                evt.Name());
        }
    }

    void write_generic_type_marshal_from_abi_base(writer& w, std::string_view generic_type, std::string_view name)
    {
        w.write("WinRT.Marshaler<%>.FromAbi(%)",
            generic_type,
            name);
    }

    void write_generic_type_marshal_from_abi(writer& w, std::string_view generic_type, std::string_view name)
    {
        return w.write_generic_type_marshal_from_abi_custom ?
            w.write_generic_type_marshal_from_abi_custom(w, generic_type, name)
            : write_generic_type_marshal_from_abi_base(w, generic_type, name);
    }

    void write_generic_type_marshal_to_abi_base(writer& w, std::string_view generic_type, std::string_view name)
    {
        w.write("(%_Native)WinRT.Marshaler<%>.ToAbi(%)",
            generic_type,
            generic_type,
            name);
    }

    void write_generic_type_marshal_to_abi(writer& w, std::string_view generic_type, std::string_view name)
    {
        return w.write_generic_type_marshal_to_abi_custom ?
            w.write_generic_type_marshal_to_abi_custom(w, generic_type, name)
            : write_generic_type_marshal_to_abi_base(w, generic_type, name);
    }

    void write_marshal_from_abi(writer& w, type_semantics const& semantics, std::string_view name)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
            call(semantics,
                [&](object_type)
                {
                    w.write("IInspectable.FromAbi(%)", name);
                },
                [&](type_definition const& type)
                {
                    write_object_marshal_from_abi(w, semantics, type, name);
                },
                [&](generic_type_index const& var)
                {
                    write_generic_type_marshal_from_abi(w,
                        w.write_temp("%", bind<write_generic_type_name>(var.index)),
                        name);
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_object_marshal_from_abi(w, semantics, type.generic_type, name);
                },
                [&](fundamental_type const& type)
                {
                    write_fundamental_marshal_from_abi(w, type, name);
                },
                [&](auto)
                {
                    w.write("%", name);
                });
        };
        write_type(semantics);
    }

    void write_marshal_to_abi(writer& w, type_semantics const& semantics, std::string_view name);

    void write_object_marshal_to_abi(writer& w, TypeDef const& type, std::string_view name)
    {
        switch (get_category(type))
        {
        case category::enum_type:
            w.write("%", name);
            return;
        case category::delegate_type:
            write_delegate_helper_call(w, type, "ToAbi", name);
            return;
        case category::struct_type:
        {
            if (is_type_blittable(type))
            {
                w.write("%", name);
            }
            else
            {
                w.write("%.ToAbi(%)", bind<write_type_name>(type, true, true), name);
            }
            return;
        }
        case category::interface_type:
        {
            w.write("MarshalInterface<%, %>.ToAbi(%).ThisPtr", bind<write_type_name>(type, false, false), bind<write_type_name>(type, true, false), name);
            return;
        }
        case category::class_type:
        {
            write_marshal_to_abi(w, get_type_semantics(get_default_interface(type)), name);
            return;
        }
        default:
            throw_invalid("Unknown type category.");
        }
    }

    void write_marshal_to_abi(writer& w, type_semantics const& semantics, std::string_view name)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
            call(semantics,
                [&](object_type)
                {
                    w.write("%?.ThisPtr ?? IntPtr.Zero", name);
                },
                [&](type_definition const& type)
                {
                    write_object_marshal_to_abi(w, type, name);
                },
                [&](generic_type_index const& var)
                {
                    write_generic_type_marshal_to_abi(w,
                        w.write_temp("%", bind<write_generic_type_name>(var.index)),
                        name);
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_object_marshal_to_abi(w, type.generic_type, name);
                },
                [&](fundamental_type type)
                {
                    write_fundamental_marshal_to_abi(w, type, name);
                },
                [&](auto)
                {
                    w.write("%", name);
                });
        };
        write_type(semantics);
    }
    void write_param_out_to_abi_local_declare(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
            call(semantics,
                [&](object_type)
                {
                    w.write("IntPtr %_value;\n", bind<write_parameter_name>(param));
                },
                [&](type_definition const& type)
                {
                    switch (get_category(type))
                    {
                    case category::enum_type:
                        break;
                    case category::struct_type:
                        if (!is_type_blittable(type))
                        {
                            w.write("% %_value;\n", bind<write_type_name>(semantics, true, false), bind<write_parameter_name>(param));
                        }
                        break;
                    default:
                        w.write("IntPtr %_value;\n", bind<write_parameter_name>(param));
                        break;
                    }
                },
                [&](generic_type_instance const&)
                {
                    w.write("IntPtr %_value;\n", bind<write_parameter_name>(param));
                },
                [&](fundamental_type type)
                {
                    if (type == fundamental_type::String)
                    {
                        w.write("IntPtr %_value;\n", bind<write_parameter_name>(param));
                    }
                    else if (type == fundamental_type::Boolean)
                    {
                        w.write("byte %_value;\n", bind<write_parameter_name>(param));
                    }
                },
                [&](auto const&) {});
            break;
        case param_category::in:
            //auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            //write_marshal_to_abi(w, get_type_semantics(param.second->Type()), param_name);
            break;
        case param_category::pass_array:
            //w.write("/*pass_array*/ null");
            break;
        case param_category::fill_array:
            //w.write("/*fill_array*/ null");
            break;
        case param_category::receive_array:
            //w.write("/*receive_array*/ null");
            break;
        }
    }

    void write_param_out_to_projection_local_declare(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
            call(semantics,
                [&](object_type)
                {
                    w.write("WinRT.IInspectable %_value;\n", bind<write_parameter_name>(param));
                },
                [&](type_definition const& type)
                {
                    if (!is_type_blittable(type))
                    {
                        w.write("% %_value;\n", bind<write_type_name>(type, false, false), bind<write_parameter_name>(param));
                    }
                },
                [&](generic_type_instance const& generic_type)
                {
                    w.write("% %_value;\n", bind<write_type_name>(generic_type, false, false), bind<write_parameter_name>(param));
                },
                [&](fundamental_type type)
                {
                    if (type == fundamental_type::String)
                    {
                        w.write("WinRT.HString %_value;\n", bind<write_parameter_name>(param));
                    }
                    else if (type == fundamental_type::Boolean)
                    {
                        w.write("bool %_value;\n", bind<write_parameter_name>(param));
                    }
                },
                [&](auto const&) {});
            break;
        case param_category::in:
            //auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            //write_marshal_to_abi(w, get_type_semantics(param.second->Type()), param_name);
            break;
        case param_category::pass_array:
            //w.write("/*pass_array*/ null");
            break;
        case param_category::fill_array:
            //w.write("/*fill_array*/ null");
            break;
        case param_category::receive_array:
            //w.write("/*receive_array*/ null");
            break;
        }
    }

    void write_out_marshal_from_abi(writer& w, type_semantics const& semantics, std::string_view name)
    {
        if (!is_type_blittable(semantics))
        {
            write_marshal_from_abi(w, semantics, name);
        }
    }

    void write_out_marshal_to_abi(writer& w, type_semantics const& semantics, std::string_view name)
    {
        if (!is_type_blittable(semantics))
        {
            write_marshal_to_abi(w, semantics, name);
        }
    }

    void write_param_out_local_marshal_from_abi(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
        {
            auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            auto out_marshal = w.write_temp("%", bind<write_out_marshal_from_abi>(get_type_semantics(param.second->Type()), param_name + "_value"));
            if (!out_marshal.empty())
            {
                w.write("\n% = %;", param_name, out_marshal);
            }
        }
        break;
        case param_category::in:
        {
            //auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            //write_marshal_to_abi(w, get_type_semantics(param.second->Type()), param_name);
        }
        break;
        case param_category::pass_array:
            //w.write("/*pass_array*/ null");
            break;
        case param_category::fill_array:
            //w.write("/*fill_array*/ null");
            break;
        case param_category::receive_array:
            //w.write("/*receive_array*/ null");
            break;
        }
    }

    void write_param_out_local_marshal_to_abi(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
        {
            auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            auto out_marshal = w.write_temp("%", bind<write_out_marshal_to_abi>(get_type_semantics(param.second->Type()), param_name + "_value"));
            if (!out_marshal.empty())
            {
                w.write("\n% = %;", param_name, out_marshal);
            }
        }
        break;
        case param_category::in:
        {
            //auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            //write_marshal_to_abi(w, get_type_semantics(param.second->Type()), param_name);
        }
        break;
        case param_category::pass_array:
            //w.write("/*pass_array*/ null");
            break;
        case param_category::fill_array:
            //w.write("/*fill_array*/ null");
            break;
        case param_category::receive_array:
            //w.write("/*receive_array*/ null");
            break;
        }
    }

    void write_param_marshal_to_abi(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
            w.write("out %%", bind<write_parameter_name>(param),
                bind([&](writer& w)
                {
                    if (!is_type_blittable(get_type_semantics(param.second->Type())))
                    {
                        w.write("_value");
                    }
                }));
            break;
        case param_category::in:
            {
                auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
                write_marshal_to_abi(w, get_type_semantics(param.second->Type()), param_name);
            }
            break;
        case param_category::pass_array:
            w.write("/*pass_array*/ null");
            break;
        case param_category::fill_array:
            w.write("/*fill_array*/ null");
            break;
        case param_category::receive_array:
            w.write("/*receive_array*/ null");
            break;
        }
    }

    static std::string get_vmethod_name(writer& w, TypeDef const& type, MethodDef const& method)
    {
        uint32_t const vtable_base = type.MethodList().first.index();
        uint32_t const vtable_index = method.index() - vtable_base;
        return w.write_temp("%_%", method.Name(), vtable_index);
    }

    std::pair<std::string, bool> find_property_interface(writer& w, TypeDef const& setter_iface, std::string_view prop_name)
    {
        std::string getter_iface;

        auto search_interface = [&](TypeDef const& type)
        {
            for (auto&& prop : type.PropertyList())
            {
                if (prop.Name() == prop_name)
                {
                    getter_iface = write_type_name_temp(w, type, "%", true);
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
                if (for_typedef(w, semantics, [&](TypeDef const& type)
                {
                    return (setter_iface != type) && (search_interface(type) || search_interfaces(type));
                })){
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
% %(%);)",
                bind<write_method_return_type>(signature),
                method.Name(),
                bind_list<write_projection_method_parameter>(", ", signature.params())
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
event WinRT.EventHandler% %;)",
                bind<write_event_param_types>(evt),
                evt.Name());
        }
    }

    void write_dynamic_invoke(writer& w, std::string vmethod_name, std::string_view thisPtrArg, method_signature signature)
    {
        std::vector<std::string> out_marshals;
        auto record_marshal_out = [&](TypeSig const& sig, int object_index, std::string marshal_target)
        {
            bool has_generic_params{};
            auto semantics = get_type_semantics(sig);
            writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
                {
                    has_generic_params = true;
                    w.write("Marshaler<%>.FromAbi",
                        bind<write_generic_type_name_base>(index));
                });
            auto param_type = w.write_temp("%", bind<write_abi_type>(semantics));
            auto marshal_out = w.write_temp("% %;\n",
                marshal_target,
                bind([&](writer& w) {
                    auto param_element = w.write_temp("__invoke_params__[%]", object_index);
                    if (has_generic_params)
                    {
                        w.write("%(%)", param_type, param_element);
                    }
                    else
                    {
                        w.write("(%)%", param_type, param_element);
                        if (param_type == "byte")
                        {
                            w.write(" != 0");
                        }
                    }
                    }));
            out_marshals.push_back(marshal_out);
        };

        auto write_generic_params = [&](writer& w, method_signature signature)
        {
            auto _ = writer::write_generic_type_marshal_to_abi_guard(w,
                [&](writer& w, std::string_view generic_type, std::string_view name)
                {
                    w.write("Marshaler<%>.ToAbi(%)",
                        generic_type,
                        name);
                });

            int object_index = 1;
            for (auto& param : signature.params())
            {
                w.write(", ");
                if (get_param_category(param) == param_category::out)
                {
                    w.write("null");
                    auto marshaled_assignment = w.write_temp("% = ", bind<write_parameter_name>(param));
                    record_marshal_out(param.second->Type(), object_index, marshaled_assignment);
                }
                else
                {
                    write_param_marshal_to_abi(w, param);
                }
                object_index++;
            }
            if (signature.return_signature())
            {
                w.write(", null");
                record_marshal_out(signature.return_signature().Type(), object_index, "return");
            }
        };

        w.write(R"(
var __invoke_params__ = new object[]{ %% };
%.DynamicInvokeAbi(__invoke_params__);
%)",
            thisPtrArg,
            bind([&](writer& w) { write_generic_params(w, signature); }),
            vmethod_name,
            bind_each(out_marshals));
    }

    void write_static_abi_invoke(writer& w, std::string vmethod_name, std::string_view thisPtrArg, method_signature signature)
    {
        w.write("%%unsafe { Marshal.ThrowExceptionForHR(%(%%%)); }%%",
            bind_each(write_param_out_to_abi_local_declare, signature.params()),
            signature.return_signature() ?
            w.write_temp("% __return_value__;\n",
                bind<write_abi_type>(get_type_semantics(signature.return_signature().Type()))) :
            "",
            vmethod_name,
            thisPtrArg,
            bind_each([](writer& w, auto const& param)
                {
                    w.write(", %", bind<write_param_marshal_to_abi>(param));
                }, signature.params()),
            signature.return_signature() ? ", out __return_value__" : "",
            bind_each(write_param_out_local_marshal_from_abi, signature.params()),
            signature.return_signature() ?
            w.write_temp("\nreturn %;",
                bind<write_marshal_from_abi>(get_type_semantics(signature.return_signature().Type()), "__return_value__")) :
            "");
    }

    void write_interface_members(writer& w, TypeDef const& type, std::set<std::string> const& generic_methods)
    {
        auto is_generic_method = [&](std::string vmethod_name)
        {
            return (generic_methods.find(vmethod_name) != generic_methods.end());
        };

        for (auto&& method : type.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }

            method_signature signature{ method };
            auto vmethod_name = get_vmethod_name(w, type, method);
            w.write(R"(
public %% %(%)
{
%
}
)",
                (method.Name() == "ToString"sv) ? "new " : "",
                bind<write_method_return_type>(signature),
                method.Name(),
                bind_list<write_projection_method_parameter>(", ", signature.params()),
                bind([&](writer& w){ is_generic_method(vmethod_name) ? 
                    write_dynamic_invoke(w, w.write_temp("_obj.Vftbl.%", vmethod_name), "ThisPtr", signature)
                    : write_static_abi_invoke(w, w.write_temp("_obj.Vftbl.%", vmethod_name), "ThisPtr.ToPointer()", signature); })
                );
        }

        for (auto&& prop : type.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);
            auto semantics = get_type_semantics(prop.Type().Type());
            w.write(R"(
public unsafe % %
{
)",
                bind<write_projection_type>(semantics),
                prop.Name());
            if (getter)
            {
                method_signature signature{ getter };
                auto vmethod_name = get_vmethod_name(w, type, getter);
                w.write("get\n{");
                if (is_generic_method(vmethod_name))
                {
                    write_dynamic_invoke(w, w.write_temp("_obj.Vftbl.%", vmethod_name), "ThisPtr", signature);
                }
                else
                {
                    w.write(R"(
% __return_value__;
Marshal.ThrowExceptionForHR(_obj.Vftbl.%(ThisPtr.ToPointer(), out __return_value__));
return %;
)",
                        bind<write_abi_type>(semantics),
                        vmethod_name,
                        bind<write_marshal_from_abi>(semantics, "__return_value__"));
                }
                w.write("}\n");
            }
            if (setter)
            {
                if (!getter)
                {
                    auto getter_interface = find_property_interface(w, type, prop.Name());
                    w.write("get{ return As<%>().%; }\n", getter_interface.first, prop.Name());
                }
                method_signature signature{ setter };
                auto vmethod_name = get_vmethod_name(w, type, setter);
                w.write("set\n{");
                if (is_generic_method(vmethod_name))
                {
                    write_dynamic_invoke(w, w.write_temp("_obj.Vftbl.%", vmethod_name), "ThisPtr", signature);
                }
                else
                {
                    w.write(R"(
Marshal.ThrowExceptionForHR(_obj.Vftbl.%(ThisPtr.ToPointer(), %));
)",
                        vmethod_name, 
                        bind<write_marshal_to_abi>(semantics, "value"));
                }
                w.write("}\n");
            }
            w.write("}\n");
        }

        for (auto&& evt : type.EventList())
        {
            auto semantics = get_type_semantics(evt.EventType());
            w.write(R"(
public event WinRT.EventHandler% %
{
add => _%.Event += value;
remove => _%.Event -= value;
}
)",
                bind<write_event_param_types>(evt),
                evt.Name(),
                evt.Name(),
                evt.Name());
        }
    }

    void write_required_interface_members_for_abi_type(writer& w, TypeDef const& type, std::set<std::string>& written_required_interfaces)
    {
        auto write_required_interface = [&](TypeDef const& iface)
        {
            auto interface_name = write_type_name_temp(w, iface);
            if (written_required_interfaces.find(interface_name) != written_required_interfaces.end())
            {
                // We've already written this required interface, so don't write it again.
                return;
            }

            for (auto&& method : iface.MethodList())
            {
                if (!method.SpecialName())
                {
                    auto method_target = w.write_temp("As<%>()", bind<write_type_name>(iface, true, false));
                    auto return_type = w.write_temp("%", bind<write_method_return_type>(method_signature{ method }));
                    write_explicitly_implemented_method(w, method, return_type, iface, method_target);
                }
            }
            w.write_each<write_explicitly_implemented_property>(iface.PropertyList(), iface, true);
            w.write_each<write_explicitly_implemented_event>(iface.EventList(), iface, true);
            written_required_interfaces.insert(std::move(interface_name));
        };

        for (auto&& iface : type.InterfaceImpl())
        {
            for_typedef(w, get_type_semantics(iface.Interface()), [&](TypeDef const& type)
            {
                write_required_interface(type);
                write_required_interface_members_for_abi_type(w, type, written_required_interfaces);
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

    void write_type_inheritance(writer& w, TypeDef const& type, type_semantics base_semantics)
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
            for_typedef(w, get_type_semantics(iface.Interface()), [&](TypeDef const& type)
            {
                write_delimiter();
                w.write("%", bind<write_type_name>(type, false, false));
            });
        }
    }

    void write_delegate_param_marshal(writer& w, method_signature::param_t const& param)
    {
        auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
        write_marshal_from_abi(w, get_type_semantics(param.second->Type()), param_name);
    }

    void write_projection_param_marshal(writer& w, method_signature::param_t const& param)
    {
        if (is_type_blittable(get_type_semantics(param.second->Type())))
        {
            write_parameter_name_with_modifier(w, param);
        }
        else
        {
            switch (get_param_category(param))
            {
            case param_category::in:
                write_delegate_param_marshal(w, param);
                break;
            case param_category::out:
                w.write("%_value", bind<write_parameter_name_with_modifier>(param, true));
                break;
            case param_category::fill_array:
                w.write("/* fill_array */ null");
                break;
            case param_category::pass_array:
                w.write("/* pass_array */ null");
                break;
            case param_category::receive_array:
                w.write("/* receive_array */ null");
                break;
            }
        }
    }

    void write_method_abi_name(writer& w, MethodDef const& method)
    {
        method_signature method_sig{ method };
        w.write(get_vmethod_name(w, method.Parent(), method));
        bool has_generic_params = false;
        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
        {
            has_generic_params = true;
            w.write("%_Native", bind<write_generic_type_name_base>(index));
        });

        separator s{ w };

        auto generic_list = w.write_temp("%", bind([&](writer& w)
        {
            for (auto&& param : method_sig.params())
            {
                auto paramType = get_type_semantics(param.second->Type());
                if (std::holds_alternative<generic_type_index>(paramType))
                {
                    s();
                    w.write(bind<write_generic_type_name>(std::get<generic_type_index>(paramType).index));
                }
            }

            auto return_sig = method_sig.return_signature();
            if (return_sig)
            {
                auto returnType = get_type_semantics(return_sig.Type());
                if (std::holds_alternative<generic_type_index>(returnType))
                {
                    s();
                    w.write(bind<write_generic_type_name>(std::get<generic_type_index>(returnType).index));
                }
            }
        }));

        if (has_generic_params)
        {
            w.write("<%>", generic_list);
        }
    }

    [[nodiscard]] writer::write_generic_type_name_guard get_abi_invoke_generic_name_guard(writer& w)
    {
        return { w, [&](writer& w, uint32_t index)
        {
            w.write("%_Native", bind<write_generic_type_name_base>(index));
        } };
    }

    void write_method_abi_invoke(writer& w, MethodDef const& method)
    {
        if (method.SpecialName()) return;

        method_signature signature{ method };
        auto return_sig = signature.return_signature();
        auto type_name = write_type_name_temp(w, method.Parent());
        auto vmethod_name = get_vmethod_name(w, method.Parent(), method);
        if (!return_sig)
        {
            w.write(
                R"(
private static unsafe int Do_Abi_%(%)
{
    try
    {
        %
        WinRT.ComCallableWrapper.FindObject<%>(new IntPtr(thisPtr)).%(%);
        %
        return 0;
    }
    catch (Exception __ex)
    {
        %
        return __ex.HResult;
    }
}
)",
                bind<write_method_abi_name>(method),
                bind([&](writer& w)
                {
                    writer::write_generic_type_name_guard _ = get_abi_invoke_generic_name_guard(w);
                    w.write(bind<write_abi_parameters>(signature, true));
                }),
                bind_each<write_param_out_to_projection_local_declare>(signature.params()),
                type_name,
                method.Name(),
                bind_list<write_projection_param_marshal>(", ", signature.params()),
                bind_each<write_param_out_local_marshal_to_abi>(signature.params()),
                bind_each([&](writer& w, method_signature::param_t const& param)
                {
                    if (get_param_category(param) == param_category::out)
                    {
                        w.write("% = default;\n", bind<write_parameter_name>(param));
                    }
                }, signature.params()));
            return;
        }

        w.write(
            R"(
private static unsafe int Do_Abi_%(%)
{
    try
    {
        %
        % = %;
        %
        return 0;
    }
    catch (Exception __ex)
    {
        %
        % = default;
        return __ex.HResult;
    }
})",
            bind<write_method_abi_name>(method),
            bind([&](writer& w)
            {
                writer::write_generic_type_name_guard _ = get_abi_invoke_generic_name_guard(w);
                w.write(bind<write_abi_parameters>(signature, true));
            }),
            bind_each<write_param_out_to_projection_local_declare>(signature.params()),
            signature.return_param_name(),
            bind([&](writer& w)
            {
                auto invokeCall = w.write_temp("WinRT.ComCallableWrapper.FindObject<%>(new IntPtr(thisPtr)).%(%)",
                    type_name,
                    method.Name(),
                    bind_list<write_projection_param_marshal>(", ", signature.params()));
                write_marshal_to_abi(w, get_type_semantics(return_sig.Type()), invokeCall);
            }),
            bind_each<write_param_out_local_marshal_to_abi>(signature.params()),
            bind_each([&](writer& w, method_signature::param_t const& param)
            {
                if (get_param_category(param) == param_category::out)
                {
                    w.write("% = default;\n", bind<write_parameter_name>(param));
                }
            }, signature.params()),
            signature.return_param_name());
    }

    void write_property_abi_invoke(writer& w, Property const& prop)
    {
        MethodDef getter, setter;
        std::tie(getter, setter) = get_property_methods(prop);
        auto type_name = write_type_name_temp(w, prop.Parent());
        if (setter)
        {
            method_signature setter_sig{ setter };

            // WinRT properties can't be indexers.
            XLANG_ASSERT(setter_sig.params().size() == 1);

            w.write(
                R"(
// TODO: fix generic CCW invocations (T != Marshaler<T>.AbiType)
private static unsafe int Do_Abi_%(%)
{
    try
    {
        WinRT.ComCallableWrapper.FindObject<%>(new IntPtr(thisPtr)).% = %;
        return 0;
    }
    catch (Exception __ex)
    {
        return __ex.HResult;
    }
}
)",
                bind<write_method_abi_name>(setter),
                bind([&](writer& w)
                {
                    writer::write_generic_type_name_guard _ = get_abi_invoke_generic_name_guard(w);
                    w.write(bind<write_abi_parameters>(setter_sig, true));
                }),
                type_name,
                prop.Name(),
                bind<write_delegate_param_marshal>(setter_sig.params()[0]));
        }

        if (getter)
        {
            method_signature getter_sig{ getter };

            // WinRT properties can't be indexers.
            XLANG_ASSERT(getter_sig.params().size() == 0);
            w.write(
                R"(
// TODO: fix generic CCW invocations (T != Marshaler<T>.AbiType)
private static unsafe int Do_Abi_%(%)
{
    try
    {
        % = %;
        return 0;
    }
    catch (Exception __ex)
    {
        % = default;
        return __ex.HResult;
    }
})",
                bind<write_method_abi_name>(getter),
                bind([&](writer& w)
                {
                    writer::write_generic_type_name_guard _ = get_abi_invoke_generic_name_guard(w);
                    w.write(bind<write_abi_parameters>(getter_sig, true));
                }),
                getter_sig.return_param_name(),
                bind([&](writer& w)
                {
                    auto invokeGetter = w.write_temp("WinRT.ComCallableWrapper.FindObject<%>(new IntPtr(thisPtr)).%",
                        type_name,
                        prop.Name());
                    write_marshal_to_abi(w, get_type_semantics(prop.Type().Type()), invokeGetter);
                }),
                getter_sig.return_param_name());
        }

    }

    void write_event_abi_invoke(writer& w, Event const& evt)
    {
        auto type_name = write_type_name_temp(w, evt.Parent());
        auto semantics = get_type_semantics(evt.EventType());
        MethodDef add_method, remove_method;
        std::tie(add_method, remove_method) = get_event_methods(evt);

        w.write(
                R"(
private static unsafe int Do_Abi_%([In] void* thisPtr, [In] IntPtr handler, [Out] out EventRegistrationToken token)
{
    token = default;
    if (handler == IntPtr.Zero)
    {
        return 0;
    }

    try
    {
        var managedWrapper = new WinRT.EventHandler%(%);
        token = new EventRegistrationToken { Value = (long)GCHandle.ToIntPtr(GCHandle.Alloc(managedWrapper)) };
        var _this = WinRT.ComCallableWrapper.FindObject<%>(new IntPtr(thisPtr));
        _this.% += managedWrapper;
        return 0;
    }
    catch (Exception __ex)
    {
        return __ex.HResult;
    }
}
)",
            bind([&](writer& w)
            {
                w.write(get_vmethod_name(w, evt.Parent(), add_method));
            }),
            bind<write_event_param_types>(evt),
            bind<write_marshal_from_abi>(semantics, "handler"),
            type_name,
            evt.Name());

        w.write(
                R"(
private static unsafe int Do_Abi_%([In] void* thisPtr, [In] EventRegistrationToken token)
{
    if (token.Value == default)
    {
        return 0;
    }

    try
    {
        var handle = GCHandle.FromIntPtr((IntPtr)token.Value);
        var handler = (WinRT.EventHandler%)handle.Target;
        var _this = WinRT.ComCallableWrapper.FindObject<%>(new IntPtr(thisPtr));
        _this.% -= handler;
        handle.Free();
        return 0;
    }
    catch (Exception __ex)
    {
        return __ex.HResult;
    }
})",
            bind([&](writer& w)
            {
                w.write(get_vmethod_name(w, evt.Parent(), remove_method));
            }),
            bind<write_event_param_types>(evt),
            type_name,
            evt.Name());
    }

    std::string get_vmethod_delegate_type(writer& w, MethodDef const& method, std::string vmethod_name)
    {
        method_signature signature{ method };
        if (is_special(method))
        {
            std::string standard_delegate;
            bool getter = starts_with(method.Name(), "get_");
            bool setter = starts_with(method.Name(), "put_");
            if (getter || setter)
            {
                std::string suffix{};
                auto semantics = get_type_semantics(
                    getter ? signature.return_signature().Type() : signature.params()[0].second->Type());
                call(semantics,
                    [&](guid_type) { suffix = "Guid"; },
                    [&](fundamental_type const& type) { suffix = get_delegate_type_suffix(type); },
                    [&](generic_type_index const& /*var*/)
                    {
                        //suffix = w.write_temp("<%>", bind<write_generic_type_name>(var.index));
                    },
                    [&](type_definition const& type)
                    {
                        switch (get_category(type))
                        {
                        case category::struct_type:
                        {
                            //suffix = write_type_name_temp(w, type, "<%>");
                            break;
                        }
                        default:
                        {
                            //w.write("Object /*todo*/");
                            break;
                        }
                        };
                    },
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

    std::pair<std::string, bool> get_generic_abi_type(writer& w, type_semantics semantics)
    {
        bool is_generic_param{};
        writer::write_generic_type_name_guard g(w, [&](writer& w, uint32_t index)
        {
            is_generic_param = true;
            w.write("Marshaler<%>.AbiType",
                bind<write_generic_type_name_base>(index));
        });
        auto generic_abi_type = w.write_temp("%", bind<write_abi_type>(semantics));
        return {generic_abi_type, is_generic_param};
    }

    std::pair<std::string, bool> get_generic_abi_types(writer& w, method_signature const& signature)
    {
        std::string generic_abi_types;
        bool has_generic_params{};
        auto append_generic_abi_type = [&](TypeSig sig, bool byref)
        {
            auto const [generic_abi_type, is_generic_param] = get_generic_abi_type(w, get_type_semantics(sig));
            generic_abi_types += w.write_temp(is_generic_param ? ", %%" : ", typeof(%)%", 
                generic_abi_type, byref ? ".MakeByRefType()" : "");
            has_generic_params |= (bool)is_generic_param;
        };
        for (auto&& param : signature.params())
        {
            append_generic_abi_type(param.second->Type(), get_param_category(param) == param_category::out);
        }
        if (signature.return_signature())
        {
            append_generic_abi_type(signature.return_signature().Type(), true);
        }
        return { generic_abi_types, has_generic_params };
    }

    void write_vtable(writer& w, TypeDef const& type, std::string const& type_name, 
        std::set<std::string>& generic_methods,
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
%%%%%%})",
            bind<write_guid_attribute>(type),
            bind_each([&](writer& w, MethodDef const& method)
            {
                bool has_generic_params{};
                auto vmethod_name = get_vmethod_name(w, type, method);
                auto delegate_type = get_vmethod_delegate_type(w, method, vmethod_name);
                if(delegate_type == "")
                {
                    delegate_type = nongenerics_class + "." + vmethod_name;
                    writer::write_generic_type_name_guard g(w, [&](writer& /*w*/, uint32_t /*index*/) {
                        has_generic_params = true;
                    });
                    auto delegate_definition = w.write_temp("public unsafe delegate int %([In] %);\n",
                        vmethod_name,
                        bind<write_abi_parameters>(method_signature{ method }, true));
                    if (has_generic_params)
                    {
                        delegate_type = "global::System.Delegate";
                    }
                    else
                    {
                        nongeneric_delegates.push_back(delegate_definition);
                    }
                }
                w.write("public % %;\n", delegate_type, vmethod_name);
                uint32_t const vtable_index = method.index() - methods.first.index() + 6;
                if (is_generic)
                {
                    method_marshals_to_abi.emplace_back(has_generic_params ?
                        w.write_temp("% = Marshal.GetDelegateForFunctionPointer(vftbl[%], %_Type);\n",
                            vmethod_name, vtable_index, vmethod_name) :
                        w.write_temp("% = Marshal.GetDelegateForFunctionPointer<%>(vftbl[%]);\n",
                            vmethod_name, delegate_type, vtable_index)
                    );
                    method_marshals_to_projection.emplace_back(has_generic_params ?
                        w.write_temp("nativeVftbl[%] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.%);\n",
                            vtable_index, vmethod_name) :
                        w.write_temp("nativeVftbl[%] = Marshal.GetFunctionPointerForDelegate<%>(AbiToProjectionVftable.%);\n",
                            vtable_index, delegate_type, vmethod_name)
                    );
                    method_create_delegates_to_projection.emplace_back(has_generic_params ?
                        w.write_temp(R"(% = global::System.Delegate.CreateDelegate(%_Type, typeof(Vftbl).GetMethod("Do_Abi_%", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(%)))",
                            vmethod_name, vmethod_name, vmethod_name,
                            bind([&](writer& w, method_signature const& sig)
                            {
                                    separator s{ w };
                                auto write_abi_type = [&](writer& w, type_semantics type)
                                {
                                    auto const [generic_abi_type, is_generic_param] = get_generic_abi_type(w, type);
                                    if (is_generic_param)
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
                            }, method_signature{ method })) :
                        w.write_temp("% = new %(Do_Abi_%)",
                            vmethod_name, delegate_type, vmethod_name)
                    );
                }
                else
                {
                    method_create_delegates_to_projection.emplace_back(
                        w.write_temp("% = new %(Do_Abi_%)",
                            vmethod_name, delegate_type, vmethod_name)
                    );
                }
            }, methods),
            bind([&](writer& w) 
            {
                if (!is_generic) return;
                w.write("public static Guid PIID = GuidGenerator.CreateIID(typeof(%));\n", type_name);
                w.write(R"(%
internal unsafe Vftbl(IntPtr thisPtr)
{
var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
var vftbl = (IntPtr*)vftblPtr.Vftbl;
IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
%}
)",
                    bind_each([&](writer& w, MethodDef const& method)
                    {
                        auto vmethod_name = get_vmethod_name(w, type, method);
                        auto [generic_abi_types, has_generic_params] = get_generic_abi_types(w, method_signature(method));
                        if (has_generic_params)
                        {
                            w.write("private static readonly Type %_Type = Expression.GetDelegateType(new Type[]{ typeof(void*)%, typeof(int) });\n",
                                vmethod_name,
                                generic_abi_types);
                            generic_methods.insert(vmethod_name);
                        }
                    }, methods),
                    bind_each(method_marshals_to_abi)
                );
            }),
            bind([&](writer& w)
                {
                    w.write(R"(
private static readonly Vftbl AbiToProjectionVftable;
public static readonly IntPtr AbiToProjectionVftablePtr;

static unsafe Vftbl()
{
    AbiToProjectionVftable = new Vftbl
    {
        IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
        %
    };
    var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * %);
    %
    AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
}
)",
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
                }),
            bind_each<write_method_abi_invoke>(methods),
            bind_each<write_property_abi_invoke>(type.PropertyList()),
            bind_each<write_event_abi_invoke>(type.EventList())
        );
    }

    void write_base_constructor_dispatch(writer& w, type_semantics type)
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

    void write_interface(writer& w, TypeDef const& type)
    {
        XLANG_ASSERT(get_category(type) == category::interface_type);
        auto type_name = write_type_name_temp(w, type);

        uint32_t const vtable_base = type.MethodList().first.index();
        w.write(R"(%
% interface %%
{%
}
)",
            // Interface
            bind<write_guid_attribute>(type),
            is_exclusive_to(type) ? "internal" : "public",
            type_name,
            bind<write_type_inheritance>(type, object_type{}),
            bind<write_interface_member_signatures>(type)
        );
    }

    bool write_abi_interface_implementation(writer& w, TypeDef const& type)
    {
        if (is_api_contract_type(type)) { return false; }

        auto guard{ w.push_generic_params(type.GenericParam()) };

        XLANG_ASSERT(get_category(type) == category::interface_type);
        auto type_name = write_type_name_temp(w, type, "%", true);
        auto nongenerics_class = w.write_temp("%_Delegates", bind<write_typedef_name>(type, true, false));
        auto is_generic = distance(type.GenericParam()) > 0;
        std::set<std::string> generic_methods;
        std::set<std::string> written_required_interfaces;
        std::vector<std::string> nongeneric_delegates;

        uint32_t const vtable_base = type.MethodList().first.index();

        w.write(R"(%
internal class % : %
{
%

public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr)%
public static implicit operator %(IObjectReference obj) => (obj != null) ? new %(obj) : null;
public static implicit operator %(ObjectReference<Vftbl> obj) => (obj != null) ? new %(obj) : null;
private readonly ObjectReference<Vftbl> _obj;
public IntPtr ThisPtr => _obj.ThisPtr;
public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
public A As<A>() => _obj.AsType<A>();
public @(IObjectReference obj) : this(obj.As<Vftbl>()) {}
public @(ObjectReference<Vftbl> obj)
{
_obj = obj;%
}

public object % { get; set; }
%%%}
)",
            // Interface abi implementation
            bind<write_guid_attribute>(type),
            type_name,
            bind<write_type_name>(type, false, false),
            // Vftbl
            bind<write_vtable>(type, type_name, generic_methods, nongenerics_class, nongeneric_delegates),
            // Interface impl
            bind([&](writer& w) {
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
return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
}
public static Guid PIID = Vftbl.PIID;
)");
    }),
            type_name,
            type_name,
            type_name,
            type_name,
            type.TypeName(),
            type.TypeName(),
            bind<write_event_source_ctors>(type),
            OwnerMemberName,
            bind<write_interface_members>(type, generic_methods),
            bind<write_event_sources>(type),
            bind<write_required_interface_members_for_abi_type>(type, written_required_interfaces)
        );

        if (!nongeneric_delegates.empty())
        {
            w.write(R"(internal static class %
{
%}
)",
                nongenerics_class,
                bind_each(nongeneric_delegates));
        }
        w.write("\n");

        return true;
    }

    void write_class(writer& w, TypeDef const& type)
    {
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
        
        w.write(R"(public %class %%
{
public %IntPtr ThisPtr => _default.ThisPtr;

private % _default;
%
public static %% FromAbi(IntPtr thisPtr) => (thisPtr != IntPtr.Zero) ? new %(new %(WinRT.ObjectReference<%.Vftbl>.FromAbi(thisPtr))) : null;

internal %(% ifc)%
{
_default = ifc;
_default.% = this;
}

private struct InterfaceTag<I>{};

private % AsInternal(InterfaceTag<%> _) => _default;
%
}
)",
            bind<write_class_modifiers>(type),
            type_name,
            bind<write_type_inheritance>(type, base_semantics),
            derived_new,
            default_interface_abi_name,
            bind<write_attributed_types>(type),
            derived_new,
            type_name,
            type_name,
            default_interface_abi_name,
            default_interface_abi_name,
            type_name,
            default_interface_abi_name,
            bind<write_base_constructor_dispatch>(base_semantics),
            OwnerMemberName,
            default_interface_name,
            default_interface_name,
            bind<write_class_members>(type));
    }

    void write_delegate_managed_invoke(writer& w, method_signature const& signature)
    {
        auto return_sig = signature.return_signature();
        if (!return_sig)
        {
            w.write("Marshal.ThrowExceptionForHR(abiInvoke(thisPtr%));\n",
                bind_each([](writer& w, auto const& param)
                {
                    w.write(", %", bind<write_param_marshal_to_abi>(param));
                }, signature.params()));
            return;
        }

        w.write(R"(% %;
Marshal.ThrowExceptionForHR(abiInvoke(thisPtr%, out %));
return %;
)",
            bind<write_abi_type>(get_type_semantics(return_sig.Type())),
            signature.return_param_name(),
            bind_each([](writer& w, auto const& param)
            {
                w.write(", %", bind<write_param_marshal_to_abi>(param));
            }, signature.params()),
            signature.return_param_name(),
            bind<write_marshal_from_abi>(get_type_semantics(signature.return_signature().Type()), signature.return_param_name()));
    };

    void write_delegate_abi_invoke(writer &w, method_signature const& signature, std::string_view type_name)
    {
        auto return_sig = signature.return_signature();
        if (!return_sig)
        {
            w.write(
R"(return WinRT.Delegate.MarshalInvoke(new IntPtr(thisPtr), (% invoke) =>
{
invoke(%);
}))",
                type_name,
                bind_list<write_delegate_param_marshal>(", ", signature.params()));
            return;
        }

        w.write(
R"({
% __result = default;
var __hresult = WinRT.Delegate.MarshalInvoke(new IntPtr(thisPtr), (% invoke) =>
{
__result = %;
});
% = __result;
return __hresult;
})",
            bind<write_abi_type>(get_type_semantics(return_sig.Type())),
            type_name,
            bind([&](writer& w)
            {
                auto invokeCall = w.write_temp("invoke(%)", bind_list<write_delegate_param_marshal>(", ", signature.params()));
                write_marshal_to_abi(w, get_type_semantics(return_sig.Type()), invokeCall);
            }),
            bind([&](writer& w) {
                w.write("%", signature.return_param_name());
            }));
    };

    void write_delegate(writer& w, TypeDef const& type)
    {
        method_signature signature{ get_delegate_invoke(type) };

        auto type_name = write_type_name_temp(w, type);
        auto type_params = w.write_temp("%", bind<write_type_params>(type));
        std::string generic_abi_types;
        bool is_generic_delegate;
        std::tie(generic_abi_types, is_generic_delegate) = get_generic_abi_types(w, signature);

        w.write(R"(public delegate % %(%);

%
public static class @Helper%
{%
private unsafe delegate int Abi_Invoke(%);
private static readonly Type Abi_Invoke_Type = Expression.GetDelegateType(new Type[] { typeof(void*)%, typeof(int) });

public static unsafe % FromAbi(IntPtr thisPtr)
{
var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(thisPtr);
% managedDelegate =
(%) =>
{
    %
};
return managedDelegate;
}

public static unsafe IntPtr ToAbi(% managedDelegate)
{
var self = typeof(@Helper%);
var invoke = self.GetMethod(nameof(Do_Abi_Invoke), BindingFlags.Static | BindingFlags.NonPublic);
var func = Marshal.GetFunctionPointerForDelegate(global::System.Delegate.CreateDelegate(Abi_Invoke_Type, invoke));
return new WinRT.Delegate(func, managedDelegate).ThisPtr;
}

// TODO: fix generic delegate invocations (T != Marshaler<T>.AbiType)
private static unsafe int Do_Abi_Invoke(%)
{
%;
}
}

)",
            // delegate
            bind<write_method_return_type>(signature),
            type_name,
            bind_list<write_projection_method_parameter>(", ", signature.params()),
            // Helper
            bind<write_guid_attribute>(type),
            type.TypeName(),
            type_params,
            bind([&](writer& w) {
                if (type_params.empty()) return;
                w.write(R"(
public static Guid PIID = GuidGenerator.CreateIID(typeof(%));)",
                    type_name
                );
            }),
            bind<write_abi_parameters>(signature, false),
            generic_abi_types,
            // FromAbi
            type_name,
            type_name,
            bind_list<write_projection_method_parameter>(", ", signature.params()),
            bind([&](writer& w)
            {
                if (is_generic_delegate)
                {
                    w.write(R"(
var abiInvoke = Marshal.GetDelegateForFunctionPointer(abiDelegate.Vftbl.Invoke, Abi_Invoke_Type);
%
)",
                        bind<write_dynamic_invoke>("abiInvoke", "thisPtr", signature));
                }
                else
                {
                    w.write(R"(
var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(abiDelegate.Vftbl.Invoke);
%
)",
                        bind<write_delegate_managed_invoke>(signature));
                }
            }),
            // ToAbi
            type_name,
            type.TypeName(),
            type_params,
            bind<write_abi_parameters>(signature, true),
            bind<write_delegate_abi_invoke>(signature, type_name));
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
        if (is_flags_enum(type))
        {
            w.write("[FlagsAttribute]\n");
        }

        w.write("public enum % : %\n{\n", bind<write_type_name>(type, false, false), is_flags_enum(type) ? "uint" : "uint");
        {
            for (auto&& field : type.FieldList())
            {
                if (auto constant = field.Constant())
                {
                    w.write("% = %,\n", field.Name(), bind<write_constant>(constant));
                }
            }
        }
        w.write("}\n");
    }

    void write_struct(writer& w, TypeDef const& type)
    {
        w.write("public struct %\n{\n", bind<write_type_name>(type, w._in_abi_namespace, false));
        {
            for (auto&& field : type.FieldList())
            {
                w.write("public ");

                auto semantics = get_type_semantics(field.Signature().Type());
                if (w._in_abi_namespace)
                {
                    write_abi_type(w, semantics);
                }
                else
                {
                    write_projection_type(w, semantics);
                }

                w.write(" %;\n", field.Name());
            }

            if (w._in_abi_namespace)
            {
                w.write("\npublic static % FromAbi(% value)\n{\n% result;\n",
                    bind<write_projection_type>(type),
                    bind<write_type_name>(type, true, false),
                    bind<write_projection_type>(type));
                for (auto&& field : type.FieldList())
                {
                    w.write("result.% = %;\n", field.Name(), bind<write_marshal_from_abi>(get_type_semantics(field.Signature().Type()), "value." + std::string{field.Name()}));
                }
                w.write("return result;\n}\n\npublic static % ToAbi(% value)\n{\n% result;\n",
                    bind<write_type_name>(type, true, false),
                    bind<write_projection_type>(type),
                    bind<write_type_name>(type, true, false));
                for (auto&& field : type.FieldList())
                {
                    w.write("result.% = %;\n", field.Name(), bind<write_marshal_to_abi>(get_type_semantics(field.Signature().Type()), "value." + std::string{field.Name()}));
                }
                w.write("return result;\n}\n");
            }
        }
        w.write("}\n");
    }

    bool write_type(TypeDef const& type, writer& w)
    {
        if (is_api_contract_type(type)) { return false; }
        if (is_attribute_type(type)) { return false; }

        auto guard{ w.push_generic_params(type.GenericParam()) };
        switch (get_category(type))
        {
        case category::class_type:
            write_class(w, type);
            break;
        case category::delegate_type:
            write_delegate(w, type);
            break;
        case category::enum_type:
            write_enum(w, type);
            break;
        case category::interface_type:
            write_interface(w, type);
            break;
        case category::struct_type:
            write_struct(w, type);
            break;
        }

        return true;
    }
}