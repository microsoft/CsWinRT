#pragma once

#include <functional>

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

    void write_generic_projection_type(writer& w, generic_type_instance const& type);
    void write_projection_type(writer& w, type_semantics const& semantics);

    void write_generic_type_name(writer& w, uint32_t index)
    {
        write_projection_type(w, w.get_generic_arg(index));
    }

    void write_typedef_name(writer& w, type_definition const& type, bool abiNamespace = false)
    {
        if ((type.TypeNamespace() != w._current_namespace) || (abiNamespace != w._in_abi_namespace))
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

    void write_type_name(writer& w, type_semantics const& semantics, bool abiNamespace = false)
    {
        auto write_name = [&](TypeDef const& type)
        {
            write_typedef_name(w, type, abiNamespace);
            write_type_params(w, type);
        };
        call(semantics,
            [&](type_definition const& type){ write_name(type); },
            [&](generic_type_instance const& type)
            {
                auto guard{ w.push_generic_args(type) };
                write_name(type.generic_type);
            },
            [](auto){ throw_invalid("invalid type"); });
    }

    auto write_type_name_temp(writer& w, type_semantics const& type, char const* format = "%")
    {
        return w.write_temp(format, bind<write_type_name>(type, false));
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
                write_generic_projection_type(w, type);
            },
            [&](generic_type_param const& param) { w.write(param.Name()); },
            [&](fundamental_type const& type) { write_fundamental_type(w, type); });
    }

    void write_generic_projection_type(writer& w, generic_type_instance const& type)
    {
        w.write("%<%>",
            bind<write_projection_type>(type.generic_type),
            bind_list<write_projection_type>(", ", type.generic_args));
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

    void write_interop_type(writer& w, type_semantics const& semantics)
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
                w.write(param.Name()); // TODO: Generics are going to be tricky
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

    void write_interop_parameter_type(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::in:
            w.write("[In] %", bind<write_interop_type>(semantics));
            break;
        case param_category::out:
            w.write("[Out] out %", bind<write_interop_type>(semantics));
            break;
        case param_category::pass_array:
            w.write("/*pass_array*/ %[]", bind<write_interop_type>(semantics));
            break;
        case param_category::fill_array:
            w.write("/*fill_array*/ %[]", bind<write_interop_type>(semantics));
            break;
        case param_category::receive_array:
            w.write("/*receive_array*/ %[]", bind<write_interop_type>(semantics));
            break;
        }
    }

    void write_interop_parameters(writer& w, method_signature const& signature)
    {
        w.write("IntPtr @this");

        for (auto&& param : signature.params())
        {
            w.write(", % %", bind<write_interop_parameter_type>(param), bind<write_parameter_name>(param));
        }
    }

    void write_abi_parameters(writer& w, method_signature const& signature)
    {
        write_interop_parameters(w, signature);

        if (signature.return_signature())
        {
            auto semantics = get_type_semantics(signature.return_signature().Type());
            w.write(", [Out] out % %", bind<write_interop_type>(semantics), signature.return_param_name());
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
            bind<write_typedef_name>(type, false),
            bind<write_type_params>(type),
            call, name);
    }

    void write_object_marshal_from_native(writer& w, type_semantics const& param_type, TypeDef const& type, std::string_view name, bool is_boxed = false)
    {
        switch (get_category(type))
        {
        case category::enum_type:
        {
            if (is_boxed)
            {
                w.write("(%)", bind<write_type_name>(type, false));
            }
            w.write("%", name);
            return;
        }
        case category::delegate_type:
        {
            write_delegate_helper_call(w, type, "FromNative", name);
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
                w.write("%.FromNative(%)", bind<write_type_name>(param_type, true), name);
            }
            return;
        }
        case category::interface_type:
        case category::class_type:
        {
            w.write("%.FromNative(%)",
                bind<write_projection_type>(param_type),
                name);
            return;
        }
        }
    }

    void write_fundamental_marshal_to_native(writer& w, fundamental_type type, std::string_view name)
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

    void write_fundamental_marshal_from_native(writer& w, fundamental_type type, std::string_view name, bool is_boxed = false)
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
                w.write("(IntPtr value) => (obj.ThisPtr == value) ? (IInspectable)% : IInspectable.FromNative(value)", OwnerMemberName);
            },
            [&](type_definition const& type)
            {
                w.write(R"((IntPtr value) => (obj.ThisPtr == value) ? (%)% : %)",
                    bind<write_projection_type>(semantics),
                    OwnerMemberName,
                    bind<write_object_marshal_from_native>(semantics, type, "value"sv, true));
            },
            [&](generic_type_index const& var)
            {
                write_type(w.get_generic_arg(var.index));
            },
            [&](generic_type_instance const& type)
            {
                auto guard{ w.push_generic_args(type) };
                w.write(R"((IntPtr value) => (obj.ThisPtr == value) ? (%)% : %)",
                    bind<write_projection_type>(semantics),
                    OwnerMemberName,
                    bind<write_object_marshal_from_native>(semantics, type.generic_type, "value"sv, true));
            },
            [&](fundamental_type type)
            {
                w.write(R"((IntPtr value) => %)",
                    bind<write_fundamental_marshal_from_native>(type, "value"sv, true));
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
        }

        if (!(is_static(type)) && type.Flags().Sealed())
        {
            w.write("sealed ");
        }
    }

    void write_class_method(writer& w, MethodDef const& method, bool is_static, std::string_view interface_member)
    {
        if (method.Flags().SpecialName())
        {
            return;
        }

        method_signature signature{ method };
        auto override_or_new = "";
        auto return_type = w.write_temp("%", bind([&](writer& w) {
            write_method_return_type(w, signature);
        }));
        if (method.Name() == "ToString")
        {
            override_or_new = "new ";
            if (signature.params().empty())
            {
                if (auto ret = signature.return_signature())
                {
                    auto semantics = get_type_semantics(ret.Type());
                    if (auto ft = std::get_if<fundamental_type>(&semantics))
                    {
                        if (*ft == fundamental_type::String)
                        {
                            override_or_new = "override ";
                            return_type = "string";
                        }
                    }
                }
            }
        }

        w.write(R"(
public %%% %(%) => %.%(%);
)",
            is_static ? "static " : "",
            override_or_new,
            return_type,
            method.Name(),
            bind_list<write_projection_method_parameter>(", ", signature.params()),
            interface_member,
            method.Name(),
            bind_list<write_parameter_name_with_modifier>(", ", signature.params(), true)
        );
    }

    void write_class_property(writer& w, std::string_view prop_name, std::string_view prop_type, std::string_view getter_target, std::string_view setter_target)
    {
        if (setter_target.empty())
        {
        w.write(R"(
public % % => %.%;
)",
            prop_type,
            prop_name,
            getter_target,
            prop_name);
        }
        else
        {
            w.write(R"(
public % %
{
get => %.%;
set => %.% = value;
}
)",
                prop_type,
                prop_name,
                getter_target,
                prop_name,
                setter_target,
                prop_name);
        }
    }

    void write_class_event(writer& w, Event const& event, std::string_view interface_member)
    {
        w.write(R"(
public event WinRT.EventHandler% %
{
add => %.% += value;
remove => %.% -= value;
}
)",
            bind<write_event_param_types>(event),
            event.Name(),
            interface_member,
            event.Name(),
            interface_member,
            event.Name());
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
                    R"((new BaseActivationFactory("%", "%.%"))._As<%.Vftbl>)",
                    class_type.TypeNamespace(),
                    class_type.TypeNamespace(),
                    class_type.TypeName(),
                    cache_type_name) :
                w.write_temp(
                    R"(ActivationFactory<%>.As<%.Vftbl>)",
                    class_type.TypeName(),
                    cache_type_name);

        w.write(R"(
internal class _% : %
{
public _%() : base(%()) { }
private static WeakLazy<_%> _instance = new WeakLazy<_%>();
internal static % Instance => _instance.Value;
}
)",
            cache_type_name,
            cache_type_name,
            cache_type_name,
            cache_interface,
            cache_type_name,
            cache_type_name,
            cache_type_name);

        return w.write_temp("_%.Instance", cache_type_name);
    }

    static std::string get_default_interface_name(writer& w, TypeDef const& type)
    {
        return write_type_name_temp(w, get_type_semantics(get_default_interface(type)));
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

    void write_static_members(writer& w, TypeDef const& static_type, TypeDef const& class_type)
    {
        auto cache_object = write_cache_object(w, static_type.TypeName(), class_type, true);

        w.write_each<write_class_method>(static_type.MethodList(), true, cache_object);
        for (auto&& prop : static_type.PropertyList())
        {
            auto [getter, setter] = get_property_methods(prop);
            auto prop_type = w.write_temp("%", bind<write_projection_type>(get_type_semantics(prop.Type().Type())));

            if (!setter)
            {
                w.write(R"(
public static % % => %.%;
)",
                    prop_type,
                    prop.Name(),
                    cache_object,
                    prop.Name());
            }
            else
            {
                w.write(R"(
public static % %
{
get => %.%;
set => %.% = value;
}
)",
                    prop_type,
                    prop.Name(),
                    cache_object,
                    prop.Name(),
                    cache_object,
                    prop.Name());
            }
        }
        for (auto&& evt : static_type.EventList())
        {
            w.write(R"(
public static event WinRT.EventHandler% %
{
add => %.% += value;
remove => %.% -= value;
}
)",
                bind<write_event_param_types>(evt),
                evt.Name(),
                cache_object,
                evt.Name(),
                cache_object,
                evt.Name());
        }
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
        std::map<std::string_view, std::tuple<std::string, std::string, std::string>> properties;
        for (auto&& ii : type.InterfaceImpl())
        {
            auto semantics = get_type_semantics(ii.Interface());

            auto write_interface = [&](TypeDef const& interface_type)
            {
                auto interface_name = write_type_name_temp(w, interface_type);

                auto is_default_interface = has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute");
                auto target = is_default_interface ? "_default" : write_type_name_temp(w, interface_type, "AsInternal(new InterfaceTag<%>())");
                if (!is_default_interface)
                {
                    w.write(R"(
private % AsInternal(InterfaceTag<%> _) => new %(_default.AsInterface<%.Vftbl>());
)",
                        interface_name,
                        interface_name,
                        interface_name,
                        interface_name);
                }

                if (!is_exclusive_to(interface_type))
                {
                    w.write(R"(
public static implicit operator %(% obj) => %;
)",
                    interface_name,
                    type.TypeName(),
                    is_default_interface ? "obj._default" : w.write_temp("obj.AsInternal(new InterfaceTag<%>())", interface_name));
                }

                w.write_each<write_class_method>(interface_type.MethodList(), false, target);
                w.write_each<write_class_event>(interface_type.EventList(), target);

                // Merge property getters/setters, since such may be defined across interfaces
                for (auto&& prop : interface_type.PropertyList())
                {
                    auto [getter, setter] = get_property_methods(prop);
                    auto projection_type = w.write_temp("%", bind<write_projection_type>(get_type_semantics(prop.Type().Type())));
                    auto [prop_targets, inserted]  = properties.try_emplace(prop.Name(), std::move(projection_type), std::move(getter ? target : ""), std::move(setter ? target : ""));
                    if (!inserted)
                    {
                        auto& [property_type, getter_target, setter_target] = prop_targets->second;
                        XLANG_ASSERT(property_type == projection_type);
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
                        XLANG_ASSERT(!getter_target.empty() || !setter_target.empty());
                    }
                }
            };
            call(semantics,
                [&](type_definition const& type) { write_interface(type); },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_interface(type.generic_type);
                },
                [](auto) { throw_invalid("invalid type"); });
        }

        // Write properties with merged accessors
        for (auto& [property_name, property_data] : properties)
        {
            auto& [property_type, getter_target, setter_target] = property_data;
            write_class_property(w, property_name, property_type, getter_target, setter_target);
        }
    }

    void write_static_class(writer& w, TypeDef const& type)
    {
        w.write(R"(public static class %
{
%})",
            bind<write_type_name>(type, false),
            bind<write_attributed_types>(type)
        );
    }

    void write_class(writer& w, TypeDef const& type)
    {
        if (is_static(type))
        {
            write_static_class(w, type);
            return;
        }

        auto type_name = write_type_name_temp(w, type);
        auto default_interface_name = get_default_interface_name(w, type);
        w.write(R"(public %class %
{
public IntPtr NativePtr { get => _default.NativePtr; }

private % _default;
%
public static % FromNative(IntPtr ^@this) => new %(new %(WinRT.ObjectReference<%.Vftbl>.FromNative(^@this)));

internal %(% ifc)
{
_default = ifc;
_default.% = this;
}

private struct InterfaceTag<I>{};

public I As<I>() => _default.As<I>();
%
}
)",
            bind<write_class_modifiers>(type),
            type_name,
            default_interface_name,
            bind<write_attributed_types>(type),
            type_name,
            type_name,
            default_interface_name,
            default_interface_name,
            type_name,
            default_interface_name,
            OwnerMemberName,
            bind<write_class_members>(type));
    }

    void write_vtbl_entry(writer& w, MethodDef const& method, uint32_t vtbl_base)
    {
        XLANG_ASSERT(vtbl_base <= method.index());
        uint32_t const vtbl_index = method.index() - vtbl_base;
        auto get_delegate_type = [&]() -> std::string
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
                        [&](fundamental_type type) { suffix = get_delegate_type_suffix(type); },
                        [&](generic_type_index const&) {},
                        [&](type_definition const& type)
                        {
                            switch (get_category(type))
                            {
                            case category::struct_type:
                                break;

                            default:
                                //w.write("Object /*todo*/");
                                break;
                            };
                        },
                        [&](auto) { suffix = "Object /*todo*/"; });
                    if (!suffix.empty())
                    {
                        return w.write_temp("WinRT.Interop.%_PropertyAs%", (getter ? "_get" : "_put"), suffix);
                    }
                }
                else if (starts_with(method.Name(), "add_"))
                {
                    return "WinRT.Interop._add_EventHandler";
                }
                else if (starts_with(method.Name(), "remove_"))
                {
                    return "WinRT.Interop._remove_EventHandler";
                }
            }

            auto delegate_type = w.write_temp("_%_%", method.Name(), vtbl_index);
            w.write("public unsafe delegate int %([In] %);\n",
                delegate_type,
                bind<write_abi_parameters>(signature));

            return delegate_type;
        };

        w.write("public % %_%;\n",
            get_delegate_type(),
            method.Name(),
            vtbl_index);
    }

    void write_event_source_ctors(writer& w, TypeDef const& type)
    {
        uint32_t const vtbl_base = type.MethodList().first.index();
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
                add.index() - vtbl_base,
                evt.Name(),
                remove.index() - vtbl_base,
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

    void write_marshal_from_native(writer& w, type_semantics const& semantics, std::string_view name)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
            call(semantics,
                [&](object_type)
                {
                    w.write("IInspectable.FromNative(%)", name);
                },
                [&](type_definition const& type)
                {
                    write_object_marshal_from_native(w, semantics, type, name);
                },
                [&](generic_type_index const& /*var*/)
                {
                    w.write("%", name);
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_object_marshal_from_native(w, semantics, type.generic_type, name);
                },
                [&](fundamental_type type)
                {
                    write_fundamental_marshal_from_native(w, type, name);
                },
                [&](auto)
                {
                    w.write("%", name);
                });
        };
        write_type(semantics);
    }

    void write_object_marshal_to_native(writer& w, TypeDef const& type, std::string_view name)
    {
        switch (get_category(type))
        {
            case category::enum_type:
                w.write("%", name);
                return;
            case category::delegate_type:
                write_delegate_helper_call(w, type, "ToNative", name);
                return;
            case category::struct_type:
                if (is_type_blittable(type))
                {
                    w.write("%", name);
                }
                else
                {
                    w.write("%.AsNative(%)", bind<write_type_name>(type, true), name);
                }
                return;
            default:
                w.write("%?.NativePtr ?? IntPtr.Zero", name);
                return;
        }
    }

    void write_marshal_to_native(writer& w, type_semantics const& semantics, std::string_view name)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
            call(semantics,
                [&](object_type)
                {
                    w.write("%?.NativePtr ?? IntPtr.Zero", name);
                },
                [&](type_definition const& type)
                {
                    write_object_marshal_to_native(w, type, name);
                },
                [&](generic_type_index const& var)
                {
                    write_type(w.get_generic_arg(var.index));
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_object_marshal_to_native(w, type.generic_type, name);
                },
                [&](fundamental_type type)
                {
                    write_fundamental_marshal_to_native(w, type, name);
                },
                [&](auto)
                {
                    w.write("%", name);
                });
        };
        write_type(semantics);
    }

    void write_param_out_local_declare(writer& w, method_signature::param_t const& param)
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
                                w.write("% %_value;\n", bind<write_type_name>(semantics, true), bind<write_parameter_name>(param));
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
            //write_marshal_to_native(w, get_type_semantics(param.second->Type()), param_name);
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

    void write_object_out_marshal_from_native(writer& w, type_semantics const& param_type, TypeDef const& type, std::string_view name, bool /*is_boxed*/ = false)
    {
        switch (get_category(type))
        {
        case category::delegate_type:
            write_delegate_helper_call(w, type, "FromNative", name);
            return;
        case category::struct_type:
            if (!is_type_blittable(param_type))
            {
                w.write("%.FromNative(%)", bind<write_type_name>(param_type, true), name);
            }
            return;
        case category::interface_type:
        case category::class_type:
            w.write("%.FromNative(%)",
                bind<write_projection_type>(param_type),
                name);
            return;
        }
    }

    void write_out_marshal_from_native(writer& w, type_semantics const& semantics, std::string_view name)
    {
        if (!is_type_blittable(semantics))
        {
            write_marshal_from_native(w, semantics, name);
        }
    }

    void write_param_out_local_marshal(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
        {
            auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            auto out_marshal = w.write_temp("%", bind<write_out_marshal_from_native>(get_type_semantics(param.second->Type()), param_name + "_value"));
            if (!out_marshal.empty())
            {
                w.write("\n% = %;", param_name, out_marshal);
            }
        }
        break;
        case param_category::in:
        {
            //auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
            //write_marshal_to_native(w, get_type_semantics(param.second->Type()), param_name);
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

    void write_param_marshal_to_native(writer& w, method_signature::param_t const& param)
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
                write_marshal_to_native(w, get_type_semantics(param.second->Type()), param_name);
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

    void write_interface_members(writer& w, TypeDef const& type,
        std::function<void(writer& w, std::string_view name, std::string_view type)> write_custom)
    {
        uint32_t const vtbl_base = type.MethodList().first.index();
        for (auto&& method : type.MethodList())
        {
            if (is_special(method))
            {
                continue;
            }

            auto custom = w.write_temp("%", bind([&](writer& w) {
                if (!write_custom) return;
                write_custom(w, method.Name(), "");
            }));

            method_signature signature{ method };
            w.write(R"(
public unsafe %% %(%)
{
%%%Marshal.ThrowExceptionForHR(_obj.Vftbl.%_%(NativePtr%%));%%
}
)",
                // public unsafe %% %(%)
                (method.Name() == "ToString"sv) ? "new " : "",
                bind<write_method_return_type>(signature),
                method.Name(),
                bind_list<write_projection_method_parameter>(", ", signature.params()),
                // %%%
                bind([&](writer& w) {
                    if (!custom.empty())
                    {
                        w.write("%\nelse\n{\n", custom);
                    }
                }),
                bind_each(write_param_out_local_declare, signature.params()),
                signature.return_signature() ?
                    w.write_temp("% __return_value__;\n",
                        bind<write_interop_type>(get_type_semantics(signature.return_signature().Type()))) :
                    "",
                // _obj.Vftbl.%_%(NativePtr%%)
                method.Name(),
                method.index() - vtbl_base,
                bind_each([](writer& w, auto const& param)
                {
                    w.write(", %", bind<write_param_marshal_to_native>(param));
                }, signature.params()),
                signature.return_signature() ? ", out __return_value__" : "",
                // %%
                bind_each(write_param_out_local_marshal, signature.params()),
                signature.return_signature() ?
                    w.write_temp("\nreturn %;",
                        bind<write_marshal_from_native>(get_type_semantics(signature.return_signature().Type()), "__return_value__")) :
                    "");
            if (!custom.empty())
            {
                w.write("\n}\n");
            }
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
            auto custom = w.write_temp("%", bind([&](writer& w) {
                if (!write_custom) return;
                write_custom(w, prop.Name(), getter ? "get" : "set");
            }));
            if (getter)
            {
                w.write(R"(get
{
%% __return_value__;
Marshal.ThrowExceptionForHR(_obj.Vftbl.get_%_%(NativePtr, out __return_value__));
return %;
}
)",
                    bind([&](writer& w) {
                        if (!custom.empty())
                        {
                            w.write("%\nelse\n{\n", custom);
                        }
                    }),
                    bind<write_interop_type>(semantics),
                    prop.Name(),
                    getter.index() - vtbl_base,
                    bind<write_marshal_from_native>(semantics, "__return_value__"));
            }
            if (setter)
            {
                w.write(R"(set
{
%Marshal.ThrowExceptionForHR(_obj.Vftbl.put_%_%(NativePtr, %));
}
)",
                    bind([&](writer& w) {
                        if (!custom.empty())
                        {
                            w.write("%\nelse\n{\n", custom);
                        }
                    }),
                    prop.Name(),
                    setter.index() - vtbl_base,
                    bind<write_marshal_to_native>(semantics, "value"));
            }
            if (!custom.empty())
            {
                w.write("\n}\n");
            }
            w.write(R"(}
)");
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

    // TODO: temporary strategy for generating generics, until all can be automated,
    // and hardcoded unique suffixes (vtbl offsets) removed from names.
    // currently, haven't found a good way to generate this code at runtime (e.g., via
    // static init expression tree evaluation, as is done with marshalers).  This is due
    // to .NET generics not having any concept of a type-function, as C++ does, so can't
    // generate vtable definitions based on a dependent type (e.g., IntPtr for interfaces).
    auto get_generic_helper(std::string_view type_name)
    {
        static const struct
        {
            std::string_view type_name;
            std::string_view define_delegate_casts;
            std::string_view init_delegate_casts;
            std::function<void(writer& w, std::string_view name, std::string_view type)> write_custom;
        }
        generic_helpers[]
        {
            // Windows.Foundation.Collections
            {
                "IIterable`1"sv
                // no marshaling or delegate casting needed
            },
            {
                "IIterator`1"sv,
                R"(public WinRT.Interop._get_PropertyAsObject get_CurrentAsObject;)"sv,
                R"(get_CurrentAsObject = typeof(T).IsClass ? _obj.Vftbl.get_Current_0.AsDelegate<WinRT.Interop._get_PropertyAsObject>() : null;)"sv,
                [](writer& w, std::string_view name, std::string_view)
                {
                    if (name == "Current"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(get_CurrentAsObject(NativePtr, out __return_value__)); }
return marshaler_T.FromNative(__return_value__);
})");
                    }
                }
            },
            {
                "IKeyValuePair`2"sv,
                R"(public WinRT.Interop._get_PropertyAsObject get_KeyAsObject;
public WinRT.Interop._get_PropertyAsObject get_ValueAsObject;)"sv,
                R"(get_KeyAsObject = typeof(K).IsClass ? _obj.Vftbl.get_Key_0.AsDelegate<WinRT.Interop._get_PropertyAsObject>() : null;
get_ValueAsObject = typeof(V).IsClass ? _obj.Vftbl.get_Value_1.AsDelegate<WinRT.Interop._get_PropertyAsObject>() : null;)"sv,
                [](writer& w, std::string_view name, std::string_view type)
                {
                    if (name == "Key"sv && type == "get"sv)
                    {
                        w.write(R"(if (typeof(K).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(get_KeyAsObject(NativePtr, out __return_value__)); }
return marshaler_K.FromNative(__return_value__);
})");
                    }
                    if (name == "Value"sv && type == "get"sv)
                    {
                        w.write(R"(if (typeof(V).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(get_ValueAsObject(NativePtr, out __return_value__)); }
return marshaler_V.FromNative(__return_value__);
})");
                    }
                }
            },
            {
                "IMap`2"sv,
                R"(/*todo*/)"sv,
                R"(/*todo*/)"sv
            },
            {
                "IMapChangedEventArgs`1"sv,
                R"(public WinRT.Interop._get_PropertyAsObject get_KeyAsObject;)"sv,
                R"(get_KeyAsObject = typeof(K).IsClass ? _obj.Vftbl.get_Key_1.AsDelegate<WinRT.Interop._get_PropertyAsObject>() : null;)"sv,
                [](writer& w, std::string_view name, std::string_view type)
                {
                    if (name == "Key"sv && type == "get"sv)
                    {
                        w.write(R"(if (typeof(K).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(get_KeyAsObject(NativePtr, out __return_value__)); }
return marshaler_K.FromNative(__return_value__);
}
)");
                    }
                }
            },
            {
                "IMapView`2"sv,
R"(private unsafe delegate int _LookupK_([In] IntPtr @this, K key, out IntPtr __return_value__);
private _LookupK_ LookupK_;
private unsafe delegate int _Lookup_V([In] IntPtr @this, IntPtr key, out V __return_value__);
private _Lookup_V Lookup_V;
private unsafe delegate int _Lookup__([In] IntPtr @this, IntPtr key, out IntPtr __return_value__);
private _Lookup__ Lookup__;
private unsafe delegate int _HasKey_([In] IntPtr @this, IntPtr key, out bool __return_value__);
private _HasKey_ HasKey_;)"sv,
R"(if (typeof(V).IsClass)
{
if (typeof(K).IsClass)
{
Lookup__ = _obj.Vftbl.Lookup_0.AsDelegate<_Lookup__>();
}
else
{
LookupK_ = _obj.Vftbl.Lookup_0.AsDelegate<_LookupK_>();
}
}
else if (typeof(K).IsClass)
{
Lookup_V = _obj.Vftbl.Lookup_0.AsDelegate<_Lookup_V>();
}
if (typeof(K).IsClass)
{
HasKey_ = _obj.Vftbl.HasKey_2.AsDelegate<_HasKey_>();
})"sv,
                [](writer& w, std::string_view name, std::string_view /*type*/)
                {
                    if (name == "Lookup"sv)
                    {
                        w.write(R"(if (typeof(V).IsClass)
{
if (typeof(K).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(Lookup__(NativePtr, marshaler_K.ToNative(key), out __return_value__)); }
return marshaler_V.FromNative(__return_value__);
}
else
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(LookupK_(NativePtr, key, out __return_value__)); }
return marshaler_V.FromNative(__return_value__);
}
}
else if (typeof(K).IsClass)
{
V __return_value__;
unsafe { Marshal.ThrowExceptionForHR(Lookup_V(NativePtr, marshaler_K.ToNative(key), out __return_value__)); }
return __return_value__;
})");
                    }
                    if (name == "HasKey"sv)
                    {
                        w.write(R"(if (typeof(K).IsClass)
{
bool __return_value__;
unsafe { Marshal.ThrowExceptionForHR(HasKey_(NativePtr, marshaler_K.ToNative(key), out __return_value__)); }
return __return_value__;
})");
                    }
                }
            },
            {
                "IObservableMap`2"sv,
                // no marshaling or delegate casting needed
            },
            {
                "IObservableVector`1"sv,
                // no marshaling or delegate casting needed
            },
            {
                "IVector`1"sv,
R"(private unsafe delegate int _GetAt_([In] IntPtr @this, uint index, out IntPtr __return_value__);
private _GetAt_ GetAt_;
private unsafe delegate int _IndexOf_([In] IntPtr @this, IntPtr value, out uint index, out bool __return_value__);
private _IndexOf_ IndexOf_;
private delegate int _SetAt_([In] IntPtr @this, uint index, IntPtr value);
private _SetAt_ SetAt_;
private delegate int _InsertAt_([In] IntPtr @this, uint index, IntPtr value);
private _InsertAt_ InsertAt_;
private delegate int _Append_([In] IntPtr @this, IntPtr value);
private _Append_ Append_;)"sv,
R"(if (typeof(T).IsClass)
{
GetAt_ = _obj.Vftbl.GetAt_0.AsDelegate<_GetAt_>();
IndexOf_ = _obj.Vftbl.IndexOf_3.AsDelegate<_IndexOf_>();
SetAt_ = _obj.Vftbl.SetAt_4.AsDelegate<_SetAt_>();
InsertAt_ = _obj.Vftbl.InsertAt_5.AsDelegate<_InsertAt_>();
Append_ = _obj.Vftbl.Append_7.AsDelegate<_Append_>();
})"sv,
                [](writer& w, std::string_view name, std::string_view /*type*/)
                {
                    if (name == "GetAt"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(GetAt_(NativePtr, index, out __return_value__)); }
return marshaler_T.FromNative(__return_value__);
})");
                    }
                    else if (name == "IndexOf"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
bool __return_value__;
unsafe { Marshal.ThrowExceptionForHR(IndexOf_(NativePtr, marshaler_T.ToNative(value), out index, out __return_value__)); }
return __return_value__;
})");
                    }
                    else if (name == "SetAt"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
unsafe { Marshal.ThrowExceptionForHR(SetAt_(NativePtr, index, marshaler_T.ToNative(value))); }
})");
                    }
                    else if (name == "InsertAt"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
unsafe { Marshal.ThrowExceptionForHR(InsertAt_(NativePtr, index, marshaler_T.ToNative(value))); }
})");
                    }
                    else if (name == "Append"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
unsafe { Marshal.ThrowExceptionForHR(Append_(NativePtr, marshaler_T.ToNative(value))); }
})");
                    }
                }
            },
            {
                "IVectorView`1"sv,
R"(private unsafe delegate int _GetAt_([In] IntPtr @this, uint index, out IntPtr __return_value__);
private _GetAt_ GetAt_;
private unsafe delegate int _IndexOf_([In] IntPtr @this, IntPtr value, out uint index, out bool __return_value__);
private _IndexOf_ IndexOf_;)"sv,
R"(if (typeof(T).IsClass)
{
GetAt_ = _obj.Vftbl.GetAt_0.AsDelegate<_GetAt_>();
IndexOf_ = _obj.Vftbl.IndexOf_2.AsDelegate<_IndexOf_>();
})"sv,
                [](writer& w, std::string_view name, std::string_view /*type*/)
                {
                    if (name == "GetAt"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
IntPtr __return_value__;
unsafe { Marshal.ThrowExceptionForHR(GetAt_(NativePtr, index, out __return_value__)); }
return marshaler_T.FromNative(__return_value__);
})");
                    }
                    if (name == "IndexOf"sv)
                    {
                        w.write(R"(if (typeof(T).IsClass)
{
bool __return_value__;
unsafe { Marshal.ThrowExceptionForHR(IndexOf_(NativePtr, marshaler_T.ToNative(value), out index, out __return_value__)); }
return __return_value__;
})");
                    }
                }
            },
            // Windows.Foundation
            {
                "IAsyncActionWithProgress`1"sv,
R"()"sv,
R"()"sv
            },
            {
                "IAsyncOperation`1"sv,
R"()"sv,
R"()"sv
            },
            {
                "IAsyncOperationWithProgress`2"sv,
R"()"sv,
R"()"sv
            },
            {
                "IReference`1"sv,
R"()"sv,
R"()"sv
            },
            {
                "IReferenceArray`1"sv,
R"()"sv,
R"()"sv
            },
        };
        auto generic_helper = std::find_if(std::begin(generic_helpers), std::end(generic_helpers),
            [&](decltype(generic_helpers[0]) && helper) {
                return type_name == helper.type_name;
            });
        XLANG_ASSERT(generic_helper != std::end(generic_helpers));
        return generic_helper;
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

    void write_type_inheritance(writer& w, TypeDef const& type)
    {
        bool first{ true };
        bool colon_written{ false };
        auto s = [&]()
        {
            if (!colon_written)
            {
                w.write(" : ");
                colon_written = true;
            }

            if (first)
            {
                first = false;
            }
            else
            {
                w.write(", ");
            }
        };

        if (get_category(type) == category::class_type && !is_static(type))
        {
            auto base_semantics = get_type_semantics(type.Extends());

            if (!std::holds_alternative<object_type>(base_semantics))
            {
                s();
                w.write(bind<write_projection_type>(base_semantics));
            }

            s();
            w.write("IDisposable");
        }

        for (auto&& iface : type.InterfaceImpl())
        {
            call(get_type_semantics(iface.Interface()),
                [&](type_definition const& type)
                {
                    if (!is_exclusive_to(type))
                    {
                        s();
                        w.write("%", bind<write_projection_type>(type));
                    }
                },
                [&](generic_type_instance const& type)
                {
                    if (!is_exclusive_to(type.generic_type))
                    {
                        s();
                        w.write("%", bind<write_projection_type>(type));
                    }
                },
                [](auto) { throw_invalid("invalid interface impl type"); });
        }
    }

    void write_interface(writer& w, TypeDef const& type)
    {
        XLANG_ASSERT(get_category(type) == category::interface_type);

        auto type_name = write_type_name_temp(w, type);

        auto is_generic = distance(type.GenericParam()) > 0;

        auto generic_helper = is_generic ? get_generic_helper(type.TypeName()) : nullptr;

        uint32_t const vtbl_base = type.MethodList().first.index();
        w.write(R"(%
% class %%
{
%
public struct Vftbl
{
#pragma warning disable 0169 // warning CS0169: The field '...' is never used
WinRT.IInspectable.Vftbl IInspectableVftbl;
#pragma warning restore 0169
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to
%#pragma warning restore 0649
%}

%private readonly WinRT.ObjectReference<Vftbl> _obj;
public IntPtr NativePtr { get => _obj.ThisPtr; }
public static WinRT.ObjectReference<Vftbl> FromNative(IntPtr ^@this) => WinRT.ObjectReference<Vftbl>.FromNative(^@this);
public static implicit operator %(WinRT.IObjectReference obj) => new %(obj);
public static implicit operator %(WinRT.ObjectReference<Vftbl> obj) => new %(obj);
public WinRT.ObjectReference<I> AsInterface<I>() => _obj.As<I>();
public A As<A>() => _obj.AsType<A>();
public @(WinRT.IObjectReference obj) : this(obj.As<Vftbl>()) {}
public @(WinRT.ObjectReference<Vftbl> obj)
{
_obj = obj;%%
}

public object % { get; set; }
%%
}
)",
            // Interface class
            bind<write_guid_attribute>(type),
            is_exclusive_to(type) ? "internal" : "public",
            type_name,
            "",  //bind<write_type_inheritance>(type),
            // Vftbl
            bind<write_guid_attribute>(type),
            bind_each([&](writer& w, MethodDef const& method)
            {
                write_vtbl_entry(w, method, vtbl_base);
            }, type.MethodList()),
            bind([&](writer& w) {
                if (!is_generic) return;
                w.write(
R"(public static Guid PIID = GuidGenerator.CreateIID(typeof(%));

)",
                    type_name);
            }),
            // Interface impl
            bind([&](writer& w){
                if (!is_generic) return;
                w.write(
R"(public static Guid PIID = Vftbl.PIID;
)");
                if (generic_helper->define_delegate_casts.empty())
                {
                    // no marshaling or delegate casting (IIterable)
                    return;
                }
                uint32_t index = 0;
                for (auto&& gp : type.GenericParam())
                {
                    gp;
                    auto param_type = w.write_temp("%", bind<write_generic_type_name>(index++));
                        w.write(
R"(public static readonly Marshaler<%> marshaler_% = typeof(%).IsClass ? new Marshaler<%>() : null;
)",
                            param_type,
                            param_type,
                            param_type,
                            param_type
                        );
                }
                w.write("%\n", generic_helper->define_delegate_casts);
            }),
            type_name,
            type_name,
            type_name,
            type_name,
            type.TypeName(),
            type.TypeName(),
            bind([&](writer& w) {
                if (!is_generic) return;
                w.write("\n%", generic_helper->init_delegate_casts);
            }),
            bind<write_event_source_ctors>(type),
            OwnerMemberName,
            bind<write_interface_members>(type, generic_helper ? generic_helper->write_custom : nullptr),
            bind<write_event_sources>(type)
        );
    }

    void write_delegate_param_marshal(writer& w, method_signature::param_t const& param)
    {
        auto param_name = w.write_temp("%", bind<write_parameter_name>(param));
        write_marshal_from_native(w, get_type_semantics(param.second->Type()), param_name);
    }

    void write_delegate_managed_invoke(writer& w, method_signature const& signature)
    {
        auto return_sig = signature.return_signature();
        if (!return_sig)
        {
            w.write("Marshal.ThrowExceptionForHR(nativeInvoke(^@this%));\n",
                bind_each([](writer& w, auto const& param)
                {
                    w.write(", %", bind<write_param_marshal_to_native>(param));
                }, signature.params()));
            return;
        }

        w.write(R"(% %;
Marshal.ThrowExceptionForHR(nativeInvoke(^@this%, out %));
return %;
)",
            bind<write_interop_type>(get_type_semantics(return_sig.Type())),
            signature.return_param_name(),
            bind_each([](writer& w, auto const& param)
            {
                w.write(", %", bind<write_param_marshal_to_native>(param));
            }, signature.params()),
            signature.return_param_name(),
            bind<write_marshal_from_native>(get_type_semantics(signature.return_signature().Type()), signature.return_param_name()));
    };

    void write_delegate_native_invoke(writer &w, method_signature const& signature, std::string_view type_name)
    {
        auto return_sig = signature.return_signature();
        if (!return_sig)
        {
            w.write(
R"(return WinRT.Delegate.MarshalInvoke(^@this, (% invoke) =>
{
invoke(%);
}))",
                type_name,
                bind_list<write_delegate_param_marshal>(", ", signature.params()));
            return;
        }

        static auto write_object_marshal = [&](writer& w, TypeDef const& type)
        {
            switch (get_category(type))
            {
            case category::interface_type:
            case category::class_type:
            {
                w.write("?.NativePtr ?? IntPtr.Zero");
                return;
            }
            }
        };

        w.write(
R"({
% __result = default;
var __hresult = WinRT.Delegate.MarshalInvoke(^@this, (% invoke) =>
{
__result = %;
});
% = __result;
return __hresult;
})",
            bind<write_interop_type>(get_type_semantics(return_sig.Type())),
            type_name,
            bind([&](writer& w)
            {
                auto invokeCall = w.write_temp("invoke(%)", bind_list<write_delegate_param_marshal>(", ", signature.params()));
                write_marshal_to_native(w, get_type_semantics(return_sig.Type()), invokeCall);
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

        w.write(R"(public delegate % %(%);

%
public static class @Helper%
{%
private unsafe delegate int Native_Invoke(%);

public static unsafe % FromNative(IntPtr ^@this)
{
var nativeDelegate = ObjectReference<WinRT.Interop.IDelegateVftbl>.FromNative(^@this);
% managedDelegate =
(%) =>
{
var nativeInvoke = Marshal.GetDelegateForFunctionPointer<Native_Invoke>(nativeDelegate.Vftbl.Invoke);
%
};
return managedDelegate;
}

public static unsafe IntPtr ToNative(% managedDelegate)
{
return new WinRT.Delegate(new Native_Invoke(Do_Native_Invoke), managedDelegate).ThisPtr;
}

private static unsafe int Do_Native_Invoke(%)
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
            bind<write_abi_parameters>(signature),
            // FromNative
            type_name,
            type_name,
            bind_list<write_projection_method_parameter>(", ", signature.params()),
            bind<write_delegate_managed_invoke>(signature),
            // ToNative
            type_name,
            bind<write_abi_parameters>(signature),
            bind<write_delegate_native_invoke>(signature, type_name));
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

        w.write("public enum % : %\n{\n", bind<write_type_name>(type, false), is_flags_enum(type) ? "uint" : "uint");
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
        w.write("public struct %\n{\n", bind<write_type_name>(type, w._in_abi_namespace));
        {
            for (auto&& field : type.FieldList())
            {
                w.write("public ");

                auto semantics = get_type_semantics(field.Signature().Type());
                if (w._in_abi_namespace)
                {
                    write_interop_type(w, semantics);
                }
                else
                {
                    write_projection_type(w, semantics);
                }

                w.write(" %;\n", field.Name());
            }

            if (w._in_abi_namespace)
            {
                w.write("\npublic static % FromNative(% value)\n{\n% result;\n",
                    bind<write_projection_type>(type),
                    bind<write_type_name>(type, true),
                    bind<write_projection_type>(type));
                for (auto&& field : type.FieldList())
                {
                    w.write("result.% = %;\n", field.Name(), bind<write_marshal_from_native>(get_type_semantics(field.Signature().Type()), "value." + std::string{field.Name()}));
                }
                w.write("return result;\n}\n\npublic static % AsNative(% value)\n{\n% result;\n",
                    bind<write_type_name>(type, true),
                    bind<write_projection_type>(type),
                    bind<write_type_name>(type, true));
                for (auto&& field : type.FieldList())
                {
                    w.write("result.% = %;\n", field.Name(), bind<write_marshal_to_native>(get_type_semantics(field.Signature().Type()), "value." + std::string{field.Name()}));
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