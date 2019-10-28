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

    void write_fundamental_type(writer& w, fundamental_type type)
    {
        w.write(to_csharp_type(type));
    }

    void write_generic_projection_type(writer& w, generic_type_instance const& type);
    void write_projection_type(writer& w, type_semantics const& semantics);

    void write_generic_param_name(writer& w, uint32_t index)
    {
        w.write(w.get_generic_param(index).Name());
    }

    void write_generic_arg_name(writer& w, uint32_t index)
    {
        auto semantics = w.get_generic_arg(index);
        if (auto gti = std::get_if<generic_type_index>(&semantics))
        {
            write_generic_param_name(w, gti->index);
            return;
        }
        write_projection_type(w, semantics);
    }

    void write_generic_type_name(writer& w, uint32_t index)
    {
        w.in_generic_instance ? write_generic_arg_name(w, index) : write_generic_param_name(w, index);
    }

    void write_typedef_name(writer& w, type_definition const& type)
    {
        if (type.TypeNamespace() != w._current_namespace)
        {
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

    void write_type_name(writer& w, type_semantics const& semantics)
    {
        auto write_name = [&](TypeDef const& type)
        {
            write_typedef_name(w, type);
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
        return w.write_temp(format, bind<write_type_name>(type));
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
            [&](fundamental_type const& type) { write_fundamental_type(w, type); });
    }

    void write_generic_projection_type(writer& w, generic_type_instance const& type)
    {
        w.write("%<%>",
            bind<write_projection_type>(type.generic_type),
            bind_list<write_projection_type>(", ", type.generic_args));
    }

    void write_parameter_name_with_modifier(writer& w, method_signature::param_t const& param, bool with_modifier = true)
    {
        static const std::set<std::string_view> keywords = {
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
            "using",     "virtual",  "void",     "volatile",   "while" };

        if (std::find(keywords.begin(), keywords.end(), param.first.Name()) != keywords.end())
        {
            w.write("@");
        }

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

        w.write(param.first.Name());
    }

    void write_parameter_name(writer& w, method_signature::param_t const& param)
    {
        write_parameter_name_with_modifier(w, param, false);
    }

    void write_generic_projection_param_type(writer& w, generic_type_instance const& type);

    void write_projection_param_type(writer& w, type_semantics const& semantics)
    {
        call(semantics,
            [&](object_type) { w.write("IInspectable"); },
            [&](guid_type) { w.write("Guid"); },
            [&](type_definition const& type) { write_typedef_name(w, type); },
            [&](generic_type_index const& var) 
            { 
                write_generic_type_name(w, var.index);
            },
            [&](generic_type_instance const& type) 
            { 
                auto guard{ w.push_generic_args(type) };
                write_generic_projection_param_type(w, type);
            },
            [&](fundamental_type const& type) { write_fundamental_type(w, type); });
    }

    void write_generic_projection_param_type(writer& w, generic_type_instance const& type)
    {
        w.write("%<%>",
            bind<write_projection_param_type>(type.generic_type),
            bind_list<write_projection_param_type>(", ", type.generic_args));
    }

    void write_projection_parameter_type(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::in:
            w.write("%", bind<write_projection_param_type>(semantics));
            break;
        case param_category::out:
            w.write("out %", bind<write_projection_param_type>(semantics));
            break;
        case param_category::pass_array:
            w.write("/*pass_array*/ %[]", bind<write_projection_param_type>(semantics));
            break;
        case param_category::fill_array:
            w.write("/*fill_array*/ %[]", bind<write_projection_param_type>(semantics));
            break;
        case param_category::receive_array:
            w.write("/*receive_array*/ %[]", bind<write_projection_param_type>(semantics));
            break;
        }
    }

    void write_method_return(writer& w, method_signature const& signature)
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

    void write_method_parameter(writer& w, method_signature::param_t const& param)
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
                    {
                        write_type_name(w, type);
                        break;
                    }
                    case category::struct_type:
                    {
                        write_type_name(w, type);
                        break;
                    }
                    default:
                    {
                        w.write("IntPtr");
                        break;
                    }
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
            [&](fundamental_type const& type)
            {
                if (type == fundamental_type::String)
                {
                    w.write("IntPtr");
                }
                else
                {
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
            w.write("%", bind<write_interop_type>(semantics));
            break;
        case param_category::out:
            w.write("out %", bind<write_interop_type>(semantics));
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
            w.write(", out % %", bind<write_interop_type>(semantics), signature.return_param_name());
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
            bind<write_typedef_name>(type),
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
                w.write("(%)", bind<write_type_name>(type));
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
            w.write("%", name);
            return;
        }
        case category::interface_type:
        case category::class_type:
        {
            w.write("%.FromNative(%)",
                bind<write_projection_param_type>(param_type),
                name);
            return;
        }
        }
    }

    void write_fundamental_marshal_to_native(writer& w, fundamental_type const& type, std::string_view name)
    {
        if (type == fundamental_type::String)
        {
            w.write("%.Handle", name);
        }
        else
        {
            w.write("%", name);
        }
    }

    void write_fundamental_marshal_from_native(writer& w, fundamental_type const& type, std::string_view name, bool is_boxed = false)
    {
        if (type == fundamental_type::String)
        {
            w.write(R"(new WinRT.HString(%))", name);
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
                    bind<write_projection_param_type>(semantics),
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
                    bind<write_projection_param_type>(semantics),
                    OwnerMemberName,
                    bind<write_object_marshal_from_native>(semantics, type.generic_type, "value"sv, true));
            },
            [&](fundamental_type const& type) 
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

    void write_class_method(writer& w, MethodDef const& method, TypeDef const& /*type*/, bool is_static, std::string_view interface_member)
    {
        if (method.Flags().SpecialName())
        {
            return;
        }

        method_signature signature{ method };
        auto override_or_new = "";
        auto return_type = w.write_temp("%", bind([&](writer& w) {
            write_method_return(w, signature);
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
public %%% %(%)
{
%%.%(%);
}
)",
            is_static ? "static " : "",
            override_or_new,
            return_type,
            method.Name(),
            bind_list<write_method_parameter>(", ", signature.params()),
            signature.return_signature() ? "return " : "",
            interface_member,
            method.Name(),
            bind_list<write_parameter_name_with_modifier>(", ", signature.params(), true)
        );
    }

    void write_class_property(writer& w, std::string_view prop_name, std::string_view prop_type, std::string_view getter_target, std::string_view setter_target)
    {
        w.write(R"(
public % %
{
%%}
)",
            prop_type,
            prop_name,
            getter_target.empty() ? "" : w.write_temp("get => %.%;\n", getter_target, prop_name),
            setter_target.empty() ? "" : w.write_temp("set => %.% = value;\n", setter_target, prop_name));
    }


    void write_class_event(writer& w, Event const& event, bool is_static, std::string_view interface_member)
    {
        w.write(R"(
public %event WinRT.EventHandler% %
{
add => %.% += value;
remove => %.% -= value;
}
)",
            is_static ? "static " : "",
            bind<write_event_param_types>(event),
            event.Name(),
            interface_member,
            event.Name(),
            interface_member,
            event.Name());
    }

//TODO: rationalize writing of members of default interfaces, non-default interfaces, 
//activatable classes, static classes.  
    void write_factory_members(writer& w, TypeDef const& factory_type, TypeDef const& type)
    {
        if (factory_type)
        {
            for (auto&& method : factory_type.MethodList())
            {
                //method_signature signature{ method };
                //w.write("/*1*/ public %(%)\n{\n", type.TypeName(), bind_list<write_method_parameter>(", ", signature.params()));
                //{
                //    write_throw_not_impl(w);
                //}
                //w.write("}\n");
            }
        }
        else
        {
            //w.write("/*0*/ public %()\n{\n", type.TypeName());
            //{
            //    w.write("_instance = Windows.Foundation.IActivationFactory.ActivateInstance(_factory.Value);\n");
            //}
            //w.write("}\n");
        }
    }

    void write_static_members(writer& w, TypeDef const& static_type, TypeDef const& type)
    {
        for (auto&& method : static_type.MethodList())
        {
            //method_signature signature{ method };
            //w.write("/*3*/ public %(%)\n{\n", type.TypeName(), bind_list<write_method_parameter>(", ", signature.params()));
            //{
            //    write_throw_not_impl(w);
            //}
            //w.write("}\n");
        }
    }

    void write_class(writer& w, TypeDef const& type)
    {
        if (is_static(type))
        {
            // todo
            return;
        }

        auto factories = get_attribute_types(type, "ActivatableAttribute");
        auto statics = get_attribute_types(type, "StaticAttribute");

        auto default_interface = get_default_interface(type);
        auto default_interface_name = write_type_name_temp(w, get_type_semantics(default_interface));
        auto type_name = write_type_name_temp(w, type);
        w.write(R"(public %class %
{
public IntPtr NativePtr { get => _default.NativePtr; }

private % _default;

public %() : this(ActivationFactory<%>.ActivateInstance<%.Vftbl>()){}

%%public static % FromNative(IntPtr ^@this) => new %(WinRT.ObjectReference<%.Vftbl>.FromNative(^@this));

internal %(% ifc)
{
_default = ifc;
_default.% = this;
}

private struct InterfaceTag<I>{};

public I As<I>() => _default.As<I>();
)",
            bind<write_class_modifiers>(type),
            type_name,
            default_interface_name,
            type_name,
            type_name,
            default_interface_name,
            bind_each(write_factory_members, factories, type),
            bind_each(write_static_members, statics, type),
            type_name,
            type_name,
            default_interface_name,
            type_name,
            default_interface_name,
            OwnerMemberName);

        std::map<std::string_view, std::tuple<std::string, std::string, std::string>> property_targets;

        for (auto&& ii : type.InterfaceImpl())
        {
            auto semantics = get_type_semantics(ii.Interface());
        
            auto write_interface = [&](TypeDef const& interface_type)
            {
                auto interface_name = write_type_name_temp(w, interface_type);
                w.write(R"(
private % AsInternal(InterfaceTag<%> _) =>
    new %(_default.AsInterface<%.Vftbl>());
)",
                    interface_name,
                    interface_name,
                    interface_name,
                    interface_name);

                if( !is_exclusive_to(interface_type) )
                {
                    w.write(R"(
public static implicit operator %(% obj) => obj.AsInternal(new InterfaceTag<%>());
)",
                        interface_name,
                        type_name,
                        interface_name);
                }

                auto is_default_interface = has_attribute(ii, "Windows.Foundation.Metadata", "DefaultAttribute");
                auto target = is_default_interface ? "_default" : write_type_name_temp(w, interface_type, "AsInternal(new InterfaceTag<%>())");
                w.write_each<write_class_method>(interface_type.MethodList(), interface_type, false, target);
                w.write_each<write_class_event>(interface_type.EventList(), false, target);

                // Accumulate property getters/setters, since such may be defined across interfaces
                for (auto&& prop : interface_type.PropertyList())
                {
                    auto [getter, setter] = get_property_methods(prop);
                    auto projection_type = w.write_temp("%", bind<write_projection_type>(get_type_semantics(prop.Type().Type())));
                    auto targets = property_targets.try_emplace(prop.Name(), std::move(projection_type), std::move(getter ? target : ""), std::move(setter ? target : ""));
                    if (!targets.second)
                    {
                        auto& [property_type, getter_target, setter_target] = targets.first->second;
                        XLANG_ASSERT(property_type == projection_type);
                        if (getter)
                        {
                            XLANG_ASSERT(getter_target.empty());
                            getter_target = std::move(target);
                        }
                        if (setter)
                        {
                            XLANG_ASSERT(setter_target.empty());
                            setter_target = std::move(target);
                        }
                    }
                }
                //w.write_each<write_class_property>(interface_type.PropertyList(), false, target);
            };
            call(semantics,
                [&](type_definition const& type){ write_interface(type); },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_interface(type.generic_type);
                },
                [](auto){ throw_invalid("invalid type"); });
        }

        for (auto& [property_name, property_data] : property_targets)
        {
            auto& [property_type, getter_target, setter_target] = property_data;
            write_class_property(w, property_name, property_type, getter_target, setter_target);
        }

        w.write(R"(}
)");
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
                        [&](fundamental_type const& type) { suffix = get_delegate_type_suffix(type); },
                        [&](generic_type_index const& var) 
                        { 
                            suffix = w.write_temp("<%>", bind<write_generic_type_name>(var.index));
                        },
                        [&](type_definition const& type)
                        {
                            switch (get_category(type))
                            {
                            case category::struct_type:
                            {
                                suffix = write_type_name_temp(w, type, "<%>");
                                break;
                            }
                            default:
                            {
                                //w.write("Object /*todo*/");
                                break;
                            }
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
                [&](fundamental_type const& type)
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
            {
                w.write("%", name);
                return;
            }
            case category::delegate_type:
            {
                write_delegate_helper_call(w, type, "ToNative", name);
                return;
            }
            case category::struct_type:
            {
                w.write("%", name);
                return;
            }
            default:
            {
                w.write("%.NativePtr", name);
                return;
            }
        }
    }

    void write_marshal_to_native(writer& w, type_semantics const& semantics, std::string_view name)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
            call(semantics,
                [&](object_type) 
                { 
                    w.write("%.NativePtr", name);
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
                [&](fundamental_type const& type) 
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
                        // TODO: non-blittable fields
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
            [&](auto) 
            { 
                return true;
            });
    }

    void write_param_out_local_declare(writer& w, method_signature::param_t const& param)
    {
        auto semantics = get_type_semantics(param.second->Type());

        switch (get_param_category(param))
        {
        case param_category::out:
        {
            if(!is_type_blittable(get_type_semantics(param.second->Type())))
            {
                w.write("IntPtr %_value;\n", bind<write_parameter_name>(param));
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

    void write_object_out_marshal_from_native(writer& w, type_semantics const& param_type, TypeDef const& type, std::string_view name, bool /*is_boxed*/ = false)
    {
        switch (get_category(type))
        {
        case category::delegate_type:
        {
            write_delegate_helper_call(w, type, "FromNative", name);
            return;
        }
        case category::struct_type:
        {
  //          w.write("/*todo: struct_type %*/%", bind<write_type_name>(type), name);
            return;
        }
        case category::interface_type:
        case category::class_type:
        {
            w.write("%.FromNative(%)",
                bind<write_projection_param_type>(param_type),
                name);
            return;
        }
        }
    }

    void write_out_marshal_from_native(writer& w, type_semantics const& semantics, std::string_view name)
    {
        std::function<void(type_semantics const&)> write_type = [&](type_semantics const& semantics) {
            call(semantics,
                [&](object_type)
                {
                    w.write("IInspectable.FromNative(%)", name);
                },
                [&](type_definition const& type)
                {
                    write_object_out_marshal_from_native(w, semantics, type, name);
                },
                [&](generic_type_index const& /*var*/)
                {
                    //w.write("%", name);
                },
                [&](generic_type_instance const& type)
                {
                    auto guard{ w.push_generic_args(type) };
                    write_object_out_marshal_from_native(w, semantics, type.generic_type, name);
                },
                [&](fundamental_type const& type)
                {
                    if (type == fundamental_type::String)
                    {
                        w.write(R"(new WinRT.HString(%))", name);
                    }
                    //write_fundamental_marshal_from_native(w, type, name);
                },
                [&](auto) 
                {
                    //w.write("%", name);
                });
        };
        write_type(semantics);
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
public %% %(%)
{
%%%unsafe { Marshal.ThrowExceptionForHR(_obj.Vftbl.%_%(NativePtr%%)); }%%
})",
                (method.Name() == "ToString"sv) ? "new " : "",
                bind<write_method_return>(signature),
                method.Name(),
                bind_list<write_method_parameter>(", ", signature.params()),
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
                method.Name(),
                method.index() - vtbl_base,
                bind_each([](writer& w, auto const& param)
                {
                    w.write(", %", bind<write_param_marshal_to_native>(param));
                }, signature.params()),
                signature.return_signature() ? ", out __return_value__" : "",
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
public % %
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
unsafe { Marshal.ThrowExceptionForHR(_obj.Vftbl.get_%_%(NativePtr, out __return_value__)); }
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
%unsafe { Marshal.ThrowExceptionForHR(_obj.Vftbl.put_%_%(NativePtr, %)); }
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
R"(WinRT.Delegate.MarshalInvoke(^@this, (% invoke) =>
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
                w.write(".NativePtr");
                return;
            }
            }
        };

        w.write(
R"({
% __result = default;
var __hresult = WinRT.Delegate.MarshalInvoke(^@this, (% invoke) =>
{
__result = invoke(%)%;
});
% = __result;
return __hresult;
})",
            bind<write_interop_type>(get_type_semantics(return_sig.Type())),
            type_name,
            bind_list<write_delegate_param_marshal>(", ", signature.params()),
            bind([&](writer &w){
                call(get_type_semantics(return_sig.Type()),
                    [&](object_type) 
                    { 
                        w.write(".NativePtr");
                    },
                    [&](type_definition const& type) 
                    { 
                        write_object_marshal(w, type);
                    },
                    [&](generic_type_instance const& type) 
                    { 
                        auto guard{ w.push_generic_args(type) };
                        write_object_marshal(w, type.generic_type);
                    },
                    [&](fundamental_type const& type) 
                    { 
                        if ((type == fundamental_type::String) && !return_sig.Type().is_szarray())
                        {
                            w.write(".Handle");
                        }
                    },
                    [](auto){});
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
Native_Invoke nativeInvoke = (%) => 
%;
return new WinRT.Delegate(nativeInvoke, managedDelegate).ThisPtr;
}
};
)",
            // delegate
            bind<write_method_return>(signature),
            type_name,
            bind_list<write_method_parameter>(", ", signature.params()),
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
            bind_list<write_method_parameter>(", ", signature.params()),
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

        w.write("public enum % : %\n{\n", bind<write_type_name>(type), is_flags_enum(type) ? "uint" : "uint");
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
        w.write("public struct %\n{\n", bind<write_type_name>(type));
        {
            for (auto&& field : type.FieldList())
            {
                auto semantics = get_type_semantics(field.Signature().Type());
                w.write("public % %;\n", bind<write_projection_type>(semantics), field.Name());
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