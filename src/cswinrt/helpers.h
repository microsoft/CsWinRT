#pragma once

namespace cswinrt
{
    using namespace std::literals;
    using namespace winmd::reader;

    std::string get_mapped_element_type(ElementType elementType);
    bool is_default_interface(InterfaceImpl const& ifaceImpl);

    static inline bool starts_with(std::string_view const& value, std::string_view const& match) noexcept
    {
        return 0 == value.compare(0, match.size(), match);
    }

    static bool is_remove_overload(MethodDef const& method)
    {
        return method.SpecialName() && starts_with(method.Name(), "remove_");
    }

    template <typename T>
    bool has_attribute(T const& row, std::string_view const& type_namespace, std::string_view const& type_name)
    {
        return static_cast<bool>(get_attribute(row, type_namespace, type_name));
    }

    static bool is_noexcept(MethodDef const& method)
    {
        return is_remove_overload(method) || has_attribute(method, "Windows.Foundation.Metadata", "NoExceptionAttribute");
    }

    static bool is_noexcept(Property const& prop)
    {
        return has_attribute(prop, "Windows.Foundation.Metadata", "NoExceptionAttribute");
    }

    bool is_exclusive_to(TypeDef const& type)
    {
        return get_category(type) == category::interface_type && has_attribute(type, "Windows.Foundation.Metadata"sv, "ExclusiveToAttribute"sv);
    }

    TypeDef get_exclusive_to_type(TypeDef const& type)
    {
        if (auto exclusive_to_attr = get_attribute(type, "Windows.Foundation.Metadata"sv, "ExclusiveToAttribute"sv))
        {
            auto sig = exclusive_to_attr.Value();
            auto const& fixed_args = sig.FixedArgs();
            XLANG_ASSERT(fixed_args.size() == 1);
            auto sys_type = std::get<ElemSig::SystemType>(std::get<ElemSig>(fixed_args[0].value).value);
            return type.get_cache().find_required(sys_type.name);
        }

        throw_invalid("Exclusive type not found");
    }

    bool is_overridable(InterfaceImpl const& interfaceImpl)
    {
        return has_attribute(interfaceImpl, "Windows.Foundation.Metadata"sv, "OverridableAttribute"sv);
    }

    bool is_projection_internal(TypeDef const& type)
    {
        return has_attribute(type, "WinRT.Interop"sv, "ProjectionInternalAttribute"sv);
    }

    bool is_flags_enum(TypeDef const& type)
    {
        return get_category(type) == category::enum_type && has_attribute(type, "System"sv, "FlagsAttribute"sv);
    }

    bool is_api_contract_type(TypeDef const& type)
    {
        return get_category(type) == category::struct_type && has_attribute(type, "Windows.Foundation.Metadata"sv, "ApiContractAttribute"sv);
    }

    bool is_attribute_type(TypeDef const& type)
    {
        return get_category(type) == category::class_type && extends_type(type, "System"sv, "Attribute"sv);
    }

    bool is_ptype(TypeDef const& type)
    {
        return distance(type.GenericParam()) > 0;
    }

    bool is_static(TypeDef const& type)
    {
        return get_category(type) == category::class_type && type.Flags().Abstract();
    }

    bool is_constructor(MethodDef const& method)
    {
        return method.Flags().RTSpecialName() && method.Name() == ".ctor";
    }

    bool has_default_constructor(TypeDef const& type)
    {
        XLANG_ASSERT(get_category(type) == category::class_type);

        for (auto&& method : type.MethodList())
        {
            if (is_constructor(method) && size(method.ParamList()) == 0)
            {
                return true;
            }
        }

        return false;
    }

    bool is_special(MethodDef const& method)
    {
        return method.SpecialName() || method.Flags().RTSpecialName();
    }

    bool is_static(MethodDef const& method)
    {
        return method.Flags().Static();
    }

    auto get_delegate_invoke(TypeDef const& type)
    {
        XLANG_ASSERT(get_category(type) == category::delegate_type);

        for (auto&& method : type.MethodList())
        {
            if (method.SpecialName() && (method.Name() == "Invoke"))
            {
                return method;
            }
        }

        throw_invalid("Invoke method not found");
    }

    enum class fundamental_type
    {
        Boolean,
        Char,
        Int8,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        String,
    };

    inline const char* to_string(fundamental_type ft) {
        switch (ft) {
        case fundamental_type::Boolean: return "Boolean";
        case fundamental_type::Char:    return "Char";
        case fundamental_type::Int8:    return "Byte";
        case fundamental_type::UInt8:   return "Byte";
        case fundamental_type::Int16:   return "Int16";
        case fundamental_type::UInt16:  return "UInt16";
        case fundamental_type::Int32:   return "Int32";
        case fundamental_type::UInt32:  return "UInt32";
        case fundamental_type::Int64:   return "Int64";
        case fundamental_type::UInt64:  return "UInt64";
        case fundamental_type::Float:   return "Float";
        case fundamental_type::Double:  return "Double";
        case fundamental_type::String:  return "String";
        default:                        return "Unknown";
        }
    }

    struct generic_type_instance;
    struct object_type {};
    struct guid_type {};
    struct type_type {};
    using type_definition = TypeDef;
    using generic_type_index = GenericTypeIndex;
    using generic_type_param = GenericParam;

    using type_semantics = std::variant<
        fundamental_type,
        object_type,
        guid_type,
        type_type,
        type_definition,
        generic_type_instance,
        generic_type_index,
        generic_type_param>;

    struct generic_type_instance
    {
        type_definition generic_type;
        std::vector<type_semantics> generic_args{};
    };

    type_semantics get_type_semantics(TypeSig const& signature);

    type_semantics get_type_semantics(GenericTypeInstSig const& type)
    {
        auto generic_type_helper = [&type]()
        {
            switch (type.GenericType().type())
            {
            case TypeDefOrRef::TypeDef:
                return type.GenericType().TypeDef();
            case TypeDefOrRef::TypeRef:
                return find_required(type.GenericType().TypeRef());
            }

            throw_invalid("invalid TypeDefOrRef value for GenericTypeInstSig.GenericType");
        };

        auto gti = generic_type_instance{ generic_type_helper() };

        for (auto&& arg : type.GenericArgs())
        {
            gti.generic_args.push_back(get_type_semantics(arg));
        }

        return gti;
    }

    type_semantics get_type_semantics(coded_index<TypeDefOrRef> const& type)
    {
        switch (type.type())
        {
        case TypeDefOrRef::TypeDef:
            return type.TypeDef();
        case TypeDefOrRef::TypeRef:
        {
            auto type_ref = type.TypeRef();
            if (type_ref.TypeName() == "Guid" && type_ref.TypeNamespace() == "System")
            {
                return guid_type{};
            }

            if (type_ref.TypeName() == "Object" && type_ref.TypeNamespace() == "System")
            {
                return object_type{};
            }

            if (type_ref.TypeName() == "Type" && type_ref.TypeNamespace() == "System")
            {
                return type_type{};
            }

            return find_required(type_ref);
        }
        case TypeDefOrRef::TypeSpec:
            return get_type_semantics(type.TypeSpec().Signature().GenericTypeInst());
        }

        throw_invalid("TypeDefOrRef not supported");
    }

    namespace impl
    {
        template<class... Ts> struct overloaded : Ts... { using Ts::operator()...; };
        template<class... Ts> overloaded(Ts...)->overloaded<Ts...>;
    }

    type_semantics get_type_semantics(TypeSig const& signature)
    {
        return std::visit(
            impl::overloaded{
            [](ElementType type) -> type_semantics
            {
                switch (type)
                {
                case ElementType::Boolean:
                    return fundamental_type::Boolean;
                case ElementType::Char:
                    return fundamental_type::Char;
                case ElementType::I1:
                    return fundamental_type::Int8;
                case ElementType::U1:
                    return fundamental_type::UInt8;
                case ElementType::I2:
                    return fundamental_type::Int16;
                case ElementType::U2:
                    return fundamental_type::UInt16;
                case ElementType::I4:
                    return fundamental_type::Int32;
                case ElementType::U4:
                    return fundamental_type::UInt32;
                case ElementType::I8:
                    return fundamental_type::Int64;
                case ElementType::U8:
                    return fundamental_type::UInt64;
                case ElementType::R4:
                    return fundamental_type::Float;
                case ElementType::R8:
                    return fundamental_type::Double;
                case ElementType::String:
                    return fundamental_type::String;
                case ElementType::Object:
                    return object_type{};
                }
                throw_invalid("element type not supported: " + get_mapped_element_type(type));
            },
            [](coded_index<TypeDefOrRef> type) -> type_semantics
            {
                return get_type_semantics(type);
            },
            [](GenericTypeIndex var) -> type_semantics {
                return generic_type_index{ var.index }; },
            [](GenericTypeInstSig sig) -> type_semantics {
                return get_type_semantics(sig); },
            [](GenericMethodTypeIndex) -> type_semantics { throw_invalid("Generic methods not supported"); }
            }, signature.Type());
    }

    struct method_signature
    {
        using param_t = std::pair<Param, ParamSig const*>;

        explicit method_signature(MethodDef const& method) :
            m_method(method.Signature())
        {
            auto params = method.ParamList();

            if (m_method.ReturnType() && params.first != params.second && params.first.Sequence() == 0)
            {
                m_return = params.first;
                ++params.first;
            }

            for (uint32_t i{}; i != size(m_method.Params()); ++i)
            {
                m_params.emplace_back(params.first + i, &m_method.Params().first[i]);
            }
        }

        std::vector<param_t>& params()
        {
            return m_params;
        }

        std::vector<param_t> const& params() const
        {
            return m_params;
        }

        auto const& return_signature() const
        {
            return m_method.ReturnType();
        }

        auto return_param_name(std::string_view default_name = "__return_value__") const
        {
            if (m_return)
            {
                return m_return.Name();
            }
            else
            {
                return default_name;
            }
        }

        bool has_params() const
        {
            return !m_params.empty();
        }

    private:

        MethodDefSig m_method;
        std::vector<param_t> m_params;
        Param m_return;
    };

    enum class param_category
    {
        in,
        ref,
        out,
        pass_array,
        fill_array,
        receive_array,
    };

    auto get_param_category(method_signature::param_t const& param)
    {
        if (param.second->Type().is_szarray())
        {
            if (param.first.Flags().In())
            {
                return param_category::pass_array;
            }
            else if (param.second->ByRef())
            {
                XLANG_ASSERT(param.first.Flags().Out());
                return param_category::receive_array;
            }
            else
            {
                XLANG_ASSERT(param.first.Flags().Out());
                return param_category::fill_array;
            }
        }
        else
        {
            if(param.first.Flags().Out())
            {
                return param_category::out;
            }
            else if (param.second->ByRef())
            {
                return param_category::ref;
            }
            else
            {
                return param_category::in;
            }
        }
    }

    auto get_property_methods(Property const& prop)
    {
        MethodDef get_method{}, set_method{};

        for (auto&& method_semantic : prop.MethodSemantic())
        {
            auto semantic = method_semantic.Semantic();

            if (semantic.Getter())
            {
                get_method = method_semantic.Method();
            }
            else if (semantic.Setter())
            {
                set_method = method_semantic.Method();
            }
            else
            {
                throw_invalid("Properties can only have get and set methods");
            }
        }

        XLANG_ASSERT(get_method || set_method);

        if (get_method && set_method)
        {
            XLANG_ASSERT(get_method.Flags().Static() == set_method.Flags().Static());
        }

        return std::make_tuple(get_method, set_method);
    }

    auto get_event_methods(Event const& evt)
    {
        MethodDef add_method{}, remove_method{};

        for (auto&& method_semantic : evt.MethodSemantic())
        {
            auto semantic = method_semantic.Semantic();

            if (semantic.AddOn())
            {
                add_method = method_semantic.Method();
            }
            else if (semantic.RemoveOn())
            {
                remove_method = method_semantic.Method();
            }
            else
            {
                throw_invalid("Events can only have add and remove methods");
            }
        }

        XLANG_ASSERT(add_method);
        XLANG_ASSERT(remove_method);
        XLANG_ASSERT(add_method.Flags().Static() == remove_method.Flags().Static());

        return std::make_tuple(add_method, remove_method);
    }

    inline coded_index<TypeDefOrRef> get_default_interface(TypeDef const& type)
    {
        auto impls = type.InterfaceImpl();

        for (auto&& impl : impls)
        {
            if (is_default_interface(impl))
            {
                return impl.Interface();
            }
        }

        if (!empty(impls))
        {
            throw_invalid("Type '", type.TypeNamespace(), ".", type.TypeName(), "' does not have a default interface");
        }

        return {};
    }

    template<typename T>
    bool is_param_expected_type(TypeSig const& param, T const& expected_type)
    {
        auto semantics = get_type_semantics(param);
        if (auto variant_type = std::get_if<T>(&semantics))
        {
            return *variant_type == expected_type;
        }

        return false;
    }

    // Checks for expected method name and parameters and returns true if those
    // match (ignoring the return type). The parameter return_type_matches indicates whether
    // the return type also matches.
    template <typename ParamMatches, typename ReturnMatches>
    bool is_expected_method(
        MethodDef const& method,
        std::string_view expected_method,
        ParamMatches&& check_expected_params,
        ReturnMatches&& check_expected_return,
        bool* return_type_matches = nullptr)
    {
        if (method.Name() != expected_method)
        {
            return false;
        }

        method_signature signature(method);
        if (!check_expected_params(signature))
        {
            return false;
        }

        if (return_type_matches)
        {
            *return_type_matches = false;
            if (check_expected_return(signature))
            {
                *return_type_matches = true;
            }
        }

        return true;
    }

    // Checks for Equals(object obj).
    bool is_object_equals_method(MethodDef const& method, bool* return_type_matches = nullptr)
    {
        auto checkParams = [](method_signature signature)
        {
            return signature.has_params() && 
                signature.params().size() == 1 && 
                std::holds_alternative<object_type>(get_type_semantics(signature.params()[0].second->Type()));
        };

        auto checkReturn = [](method_signature signature)
        {
            if (auto return_sig = signature.return_signature())
            {
                if (is_param_expected_type(return_sig.Type(), fundamental_type::Boolean))
                {
                    return true;
                }
            }

            return false;
        };

        return is_expected_method(method, "Equals"sv, checkParams, checkReturn, return_type_matches);
    }

    // Checks for Equals(Class obj).
    bool is_class_equals_method(MethodDef const& method, std::string_view expected_namespace, std::string_view expected_type_name, bool* return_type_matches = nullptr)
    {
        auto checkParams = [&expected_namespace, &expected_type_name](method_signature signature)
        {
            if (signature.has_params() && signature.params().size() == 1)
            {
                auto semantics = get_type_semantics(signature.params()[0].second->Type());
                if (auto td = std::get_if<type_definition>(&semantics))
                {
                    return td->TypeNamespace() == expected_namespace && td->TypeName() == expected_type_name;
                }
            }

            return false;
        };

        auto checkReturn = [](method_signature signature)
        {
            if (auto return_sig = signature.return_signature())
            {
                if (is_param_expected_type(return_sig.Type(), fundamental_type::Boolean))
                {
                    return true;
                }
            }

            return false;
        };

        return is_expected_method(method, "Equals"sv, checkParams, checkReturn, return_type_matches);
    }

    // Checks for GetHashCode().
    bool is_object_hashcode_method(MethodDef const& method, bool* return_type_matches = nullptr)
    {
        auto checkParams = [](method_signature signature)
        {
            return !signature.has_params();
        };

        auto checkReturn = [](method_signature signature)
        {
            if (auto return_sig = signature.return_signature())
            {
                if (is_param_expected_type(return_sig.Type(), fundamental_type::Int32))
                {
                    return true;
                }
            }

            return false;
        };

        return is_expected_method(method, "GetHashCode"sv, checkParams, checkReturn, return_type_matches);
    }

    bool has_object_equals_method(TypeDef const& type, bool* return_type_matches = nullptr)
    {
        XLANG_ASSERT(get_category(type) == category::class_type);

        for (auto&& method : type.MethodList())
        {
            if (is_object_equals_method(method, return_type_matches))
            {
                return true;
            }
        }

        return false;
    }

    bool has_class_equals_method(TypeDef const& type, bool* return_type_matches = nullptr)
    {
        XLANG_ASSERT(get_category(type) == category::class_type);

        for (auto&& method : type.MethodList())
        {
            if (is_class_equals_method(method, type.TypeNamespace(), type.TypeName(), return_type_matches))
            {
                return true;
            }
        }

        return false;
    }

    bool has_object_hashcode_method(TypeDef const& type, bool* return_type_matches = nullptr)
    {
        XLANG_ASSERT(get_category(type) == category::class_type);

        for (auto&& method : type.MethodList())
        {
            if (is_object_hashcode_method(method, return_type_matches))
            {
                return true;
            }
        }

        return false;
    }

    struct mapped_type
    {
        std::string_view abi_name;
        std::string_view mapped_namespace;
        std::string_view mapped_name;
        bool requires_marshaling;
        bool has_custom_members_output;
    };

    inline const std::initializer_list<mapped_type> get_mapped_types_in_namespace(std::string_view typeNamespace)
    {
        static const struct
        {
            std::string_view name_space;
            std::initializer_list<mapped_type> types;
        } mapped_types[] =
        {
            // Make sure to keep this table consistent with the registrations in WinRT.Runtime/Projections.cs
            // and the reverse mapping in WinRT.SourceGenerator/TypeMapper.cs.
            // This table can include both the MUX and WUX types as only one will be selected at runtime.
            // NOTE: Must keep namespaces sorted (outer) and abi type names sorted (inner)
            { "Microsoft.UI.Xaml",
                {
                    { "CornerRadius", "Microsoft.UI.Xaml", "CornerRadius" },
                    { "CornerRadiusHelper" },
                    { "Duration", "Microsoft.UI.Xaml", "Duration" },
                    { "DurationHelper" },
                    { "DurationType", "Microsoft.UI.Xaml", "DurationType" },
                    { "GridLength", "Microsoft.UI.Xaml", "GridLength" },
                    { "GridLengthHelper" },
                    { "GridUnitType", "Microsoft.UI.Xaml", "GridUnitType" },
                    { "ICornerRadiusHelper" },
                    { "ICornerRadiusHelperStatics" },
                    { "IDurationHelper" },
                    { "IDurationHelperStatics" },
                    { "IGridLengthHelper" },
                    { "IGridLengthHelperStatics" },
                    { "IThicknessHelper" },
                    { "IThicknessHelperStatics" },
                    { "Thickness", "Microsoft.UI.Xaml", "Thickness" },
                    { "ThicknessHelper" },
                    { "IXamlServiceProvider", "System", "IServiceProvider" },
                }
            },
            { "Microsoft.UI.Xaml.Controls.Primitives",
                {
                    { "GeneratorPosition", "Microsoft.UI.Xaml.Controls.Primitives", "GeneratorPosition" },
                    { "GeneratorPositionHelper" },
                    { "IGeneratorPositionHelper" },
                    { "IGeneratorPositionHelperStatics" },
                }
            },
            { "Microsoft.UI.Xaml.Data",
                {
                    { "DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs" },
                    { "INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true },
                    { "INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged" },
                    { "PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs" },
                    { "PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler" },
                }
            },
            { "Microsoft.UI.Xaml.Input",
                {
                    { "ICommand", "System.Windows.Input", "ICommand", true }
                }
            },
            { "Microsoft.UI.Xaml.Interop",
                {
                    { "IBindableIterable", "System.Collections", "IEnumerable", true, true },
                    { "IBindableVector", "System.Collections", "IList", true, true },
                    { "INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true },
                    { "NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction" },
                    { "NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true },
                    { "NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true },
                }
            },
            { "Microsoft.UI.Xaml.Media",
                {
                    { "IMatrixHelper" },
                    { "IMatrixHelperStatics" },
                    { "Matrix", "Microsoft.UI.Xaml.Media", "Matrix" },
                    { "MatrixHelper" },
                }
            },
            { "Microsoft.UI.Xaml.Media.Animation",
                {
                    { "IKeyTimeHelper" },
                    { "IKeyTimeHelperStatics" },
                    { "IRepeatBehaviorHelper" },
                    { "IRepeatBehaviorHelperStatics" },
                    { "KeyTime", "Microsoft.UI.Xaml.Media.Animation", "KeyTime" },
                    { "KeyTimeHelper" },
                    { "RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior" },
                    { "RepeatBehaviorHelper" },
                    { "RepeatBehaviorType", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehaviorType" }
                }
            },
            { "Microsoft.UI.Xaml.Media.Media3D",
                {
                    { "IMatrix3DHelper" },
                    { "IMatrix3DHelperStatics" },
                    { "Matrix3D", "Microsoft.UI.Xaml.Media.Media3D", "Matrix3D" },
                    { "Matrix3DHelper" },
                }
            },
            { "WinRT.Interop",
                {
                    { "HWND", "System", "IntPtr" },
                    { "ProjectionInternalAttribute" },
                }
            },
            { "Windows.Foundation",
                {
                    { "DateTime", "System", "DateTimeOffset", true },
                    { "EventHandler`1", "System", "EventHandler", false },
                    { "EventRegistrationToken", "WindowsRuntime.InteropServices", "EventRegistrationToken", false },
                    { "FoundationContract", "Windows.Foundation", "FoundationContract"},
                    { "HResult", "System", "Exception", true },
                    { "IClosable", "System", "IDisposable", true, true },
                    { "IPropertyValue", "Windows.Foundation", "IPropertyValue", true },
                    { "IReferenceArray`1", "Windows.Foundation", "IReferenceArray", true },
                    { "IReference`1", "System", "Nullable", true },
                    { "Point", "Windows.Foundation", "Point" },
                    { "Rect", "Windows.Foundation", "Rect" },
                    { "Size", "Windows.Foundation", "Size" },
                    { "TimeSpan", "System", "TimeSpan", true },
                    { "Uri", "System", "Uri", true }
                }
            },
            { "Windows.Foundation.Collections",
                {
                    { "IIterable`1", "System.Collections.Generic", "IEnumerable`1", true, true },
                    { "IIterator`1", "System.Collections.Generic", "IEnumerator`1", true, true },
                    { "IKeyValuePair`2", "System.Collections.Generic", "KeyValuePair`2", true },
                    { "IMapView`2", "System.Collections.Generic", "IReadOnlyDictionary`2", true, true },
                    { "IMap`2", "System.Collections.Generic", "IDictionary`2", true, true },
                    { "IVectorView`1", "System.Collections.Generic", "IReadOnlyList`1", true, true },
                    { "IVector`1", "System.Collections.Generic", "IList`1", true, true },
                }
            },
            { "Windows.Foundation.Metadata",
                {
                    { "AttributeTargets", "System", "AttributeTargets" },
                    { "AttributeUsageAttribute", "System", "AttributeUsageAttribute" },
                    { "ContractVersionAttribute", "Windows.Foundation.Metadata", "ContractVersionAttribute"}
                }
            },
            { "Windows.Foundation.Numerics",
                {
                    { "Matrix3x2", "System.Numerics", "Matrix3x2" },
                    { "Matrix4x4", "System.Numerics", "Matrix4x4" },
                    { "Plane", "System.Numerics", "Plane" },
                    { "Quaternion", "System.Numerics", "Quaternion" },
                    { "Vector2", "System.Numerics", "Vector2" },
                    { "Vector3", "System.Numerics", "Vector3" },
                    { "Vector4", "System.Numerics", "Vector4" },
                }
            },
            { "Windows.UI",
                {
                    { "Color", "Windows.UI", "Color" },
                }
            },
            { "Windows.UI.Xaml",
                {
                    { "CornerRadius", "Windows.UI.Xaml", "CornerRadius" },
                    { "CornerRadiusHelper" },
                    { "Duration", "Windows.UI.Xaml", "Duration" },
                    { "DurationHelper" },
                    { "DurationType", "Windows.UI.Xaml", "DurationType" },
                    { "GridLength", "Windows.UI.Xaml", "GridLength" },
                    { "GridLengthHelper" },
                    { "GridUnitType", "Windows.UI.Xaml", "GridUnitType" },
                    { "ICornerRadiusHelper" },
                    { "ICornerRadiusHelperStatics" },
                    { "IDurationHelper" },
                    { "IDurationHelperStatics" },
                    { "IGridLengthHelper" },
                    { "IGridLengthHelperStatics" },
                    { "IThicknessHelper" },
                    { "IThicknessHelperStatics" },
                    { "Thickness", "Windows.UI.Xaml", "Thickness" },
                    { "ThicknessHelper" },
                    { "IXamlServiceProvider", "System", "IServiceProvider" },
                }
            },
            { "Windows.UI.Xaml.Controls.Primitives",
                {
                    { "GeneratorPosition", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition" },
                    { "GeneratorPositionHelper" },
                    { "IGeneratorPositionHelper" },
                    { "IGeneratorPositionHelperStatics" },
                }
            },
            { "Windows.UI.Xaml.Data",
                {
                    { "DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs" },
                    { "INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true },
                    { "INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged" },
                    { "PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs" },
                    { "PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler" },
                }
            },
            { "Windows.UI.Xaml.Input",
                {
                    { "ICommand", "System.Windows.Input", "ICommand", true }
                }
            },
            { "Windows.UI.Xaml.Interop",
                {
                    { "IBindableIterable", "System.Collections", "IEnumerable", true, true },
                    { "IBindableVector", "System.Collections", "IList", true, true },
                    { "INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true },
                    { "NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction" },
                    { "NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true },
                    { "NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true },
                    { "TypeKind", "Windows.UI.Xaml.Interop", "TypeKind", true },
                    { "TypeName", "System", "Type", true }
                }
            },
            { "Windows.UI.Xaml.Media",
                {
                    { "IMatrixHelper" },
                    { "IMatrixHelperStatics" },
                    { "Matrix", "Windows.UI.Xaml.Media", "Matrix" },
                    { "MatrixHelper" },
                }
            },
            { "Windows.UI.Xaml.Media.Animation",
                {
                    { "IKeyTimeHelper" },
                    { "IKeyTimeHelperStatics" },
                    { "IRepeatBehaviorHelper" },
                    { "IRepeatBehaviorHelperStatics" },
                    { "KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime" },
                    { "KeyTimeHelper" },
                    { "RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior" },
                    { "RepeatBehaviorHelper" },
                    { "RepeatBehaviorType", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType" }
                }
            },
            { "Windows.UI.Xaml.Media.Media3D",
                {
                    { "IMatrix3DHelper" },
                    { "IMatrix3DHelperStatics" },
                    { "Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D" },
                    { "Matrix3DHelper" },
                }
            },
        };

        auto nsItr = std::lower_bound(std::begin(mapped_types), std::end(mapped_types), typeNamespace, [](auto&& v, std::string_view ns)
        {
            return v.name_space < ns;
        });

        if ((nsItr == std::end(mapped_types)) || (nsItr->name_space != typeNamespace))
        {
            return {};
        }

        return nsItr->types;
    }

    inline const mapped_type* get_mapped_type(std::string_view typeNamespace, std::string_view typeName)
    {
        auto mapped_types = get_mapped_types_in_namespace(typeNamespace);

        if (mapped_types.size() == 0)
        {
            return nullptr;
        }

        auto nameItr = std::lower_bound(mapped_types.begin(), mapped_types.end(), typeName, [](auto&& v, std::string_view name)
        {
            return v.abi_name < name;
        });
        if ((nameItr == mapped_types.end()) || (nameItr->abi_name != typeName))
        {
            return nullptr;
        }

        return &*nameItr;
    }

    inline const std::string_view get_contract_platform(std::string_view contract_name, int32_t contract_version)
    {
        struct contract_platform
        {
            int32_t contract_version;
            std::string_view platform_version;
        };

        static const struct
        {
            std::string_view contract_name;
            std::vector<contract_platform> versions;
        } contract_mappings[] =
        {
            // Use PreviousPlatforms.linq LinqPad query to generate mapping data
            { "Windows.AI.MachineLearning.MachineLearningContract",
                {
                    { 1, "10.0.17763.0" },
                    { 2, "10.0.18362.0" },
                    { 3, "10.0.19041.0" },
                }
            },
            { "Windows.AI.MachineLearning.Preview.MachineLearningPreviewContract",
                {
                    { 1, "10.0.17134.0" },
                    { 2, "10.0.17763.0" },
                }
            },
            { "Windows.ApplicationModel.Calls.Background.CallsBackgroundContract",
                {
                    { 1, "10.0.17763.0" },
                    { 2, "10.0.18362.0" },
                }
            },
            { "Windows.ApplicationModel.Calls.CallsPhoneContract",
                {
                    { 4, "10.0.17763.0" },
                    { 5, "10.0.18362.0" },
                }
            },
            { "Windows.ApplicationModel.Calls.CallsVoipContract",
                {
                    { 1, "10.0.10586.0" },
                    { 2, "10.0.16299.0" },
                    { 3, "10.0.17134.0" },
                    { 4, "10.0.17763.0" },
                }
            },
            { "Windows.ApplicationModel.CommunicationBlocking.CommunicationBlockingContract",
                {
                    { 2, "10.0.17763.0" },
                }
            },
            { "Windows.ApplicationModel.SocialInfo.SocialInfoContract",
                {
                    { 1, "10.0.14393.0" },
                    { 2, "10.0.15063.0" },
                }
            },
            { "Windows.ApplicationModel.StartupTaskContract",
                {
                    { 2, "10.0.16299.0" },
                    { 3, "10.0.17134.0" },
                }
            },
            { "Windows.Devices.Custom.CustomDeviceContract",
                {
                    { 1, "10.0.16299.0" },
                }
            },
            { "Windows.Devices.DevicesLowLevelContract",
                {
                    { 2, "10.0.14393.0" },
                    { 3, "10.0.15063.0" },
                }
            },
            { "Windows.Devices.Printers.PrintersContract",
                {
                    { 1, "10.0.10586.0" },
                }
            },
            { "Windows.Devices.SmartCards.SmartCardBackgroundTriggerContract",
                {
                    { 3, "10.0.16299.0" },
                }
            },
            { "Windows.Devices.SmartCards.SmartCardEmulatorContract",
                {
                    { 5, "10.0.16299.0" },
                    { 6, "10.0.17763.0" },
                }
            },
            { "Windows.Foundation.FoundationContract",
                {
                    { 1, "10.0.10240.0" },
                    { 2, "10.0.10586.0" },
                    { 3, "10.0.15063.0" },
                    { 4, "10.0.19041.0" },
                }
            },
            { "Windows.Foundation.UniversalApiContract",
                {
                    { 1, "10.0.10240.0" },
                    { 2, "10.0.10586.0" },
                    { 3, "10.0.14393.0" },
                    { 4, "10.0.15063.0" },
                    { 5, "10.0.16299.0" },
                    { 6, "10.0.17134.0" },
                    { 7, "10.0.17763.0" },
                    { 8, "10.0.18362.0" },
                    { 10, "10.0.19041.0" },
                }
            },
            { "Windows.Foundation.VelocityIntegration.VelocityIntegrationContract",
                {
                    { 1, "10.0.17134.0" },
                }
            },
            { "Windows.Gaming.XboxLive.StorageApiContract",
                {
                    { 1, "10.0.16299.0" },
                }
            },
            { "Windows.Graphics.Printing3D.Printing3DContract",
                {
                    { 2, "10.0.10586.0" },
                    { 3, "10.0.14393.0" },
                    { 4, "10.0.16299.0" },
                }
            },
            { "Windows.Networking.Connectivity.WwanContract",
                {
                    { 1, "10.0.10240.0" },
                    { 2, "10.0.17134.0" },
                }
            },
            { "Windows.Networking.Sockets.ControlChannelTriggerContract",
                {
                    { 3, "10.0.17763.0" },
                }
            },
            { "Windows.Security.Isolation.IsolatedWindowsEnvironmentContract",
                {
                    { 1, "10.0.19041.0" },
                }
            },
            { "Windows.Services.Maps.GuidanceContract",
                {
                    { 3, "10.0.17763.0" },
                }
            },
            { "Windows.Services.Maps.LocalSearchContract",
                {
                    { 4, "10.0.17763.0" },
                }
            },
            { "Windows.Services.Store.StoreContract",
                {
                    { 1, "10.0.14393.0" },
                    { 2, "10.0.15063.0" },
                    { 3, "10.0.17134.0" },
                    { 4, "10.0.17763.0" },
                }
            },
            { "Windows.Services.TargetedContent.TargetedContentContract",
                {
                    { 1, "10.0.15063.0" },
                }
            },
            { "Windows.Storage.Provider.CloudFilesContract",
                {
                    { 4, "10.0.19041.0" },
                }
            },
            { "Windows.System.Profile.ProfileHardwareTokenContract",
                {
                    { 1, "10.0.14393.0" },
                }
            },
            { "Windows.System.Profile.ProfileSharedModeContract",
                {
                    { 1, "10.0.14393.0" },
                    { 2, "10.0.15063.0" },
                }
            },
            { "Windows.System.Profile.SystemManufacturers.SystemManufacturersContract",
                {
                    { 3, "10.0.17763.0" },
                }
            },
            { "Windows.System.SystemManagementContract",
                {
                    { 6, "10.0.17763.0" },
                    { 7, "10.0.19041.0" },
                }
            },
            { "Windows.UI.ViewManagement.ViewManagementViewScalingContract",
                {
                    { 1, "10.0.14393.0" },
                }
            },
            { "Windows.UI.Xaml.Core.Direct.XamlDirectContract",
                {
                    { 1, "10.0.17763.0" },
                    { 2, "10.0.18362.0" },
                }
            },
        };

        auto contractItr = std::lower_bound(std::begin(contract_mappings), std::end(contract_mappings), contract_name, [](auto&& c, std::string_view contract_name)
        {
            return c.contract_name < contract_name;
        });

        if ((contractItr == std::end(contract_mappings)) || (contractItr->contract_name != contract_name))
        {
            return {};
        }

        auto& versions = contractItr->versions;
        auto versionItr = std::lower_bound(std::begin(versions), std::end(versions), contract_version, [](auto&& v, int32_t contract_version)
        {
            return v.contract_version < contract_version;
        });

        if ((versionItr == std::end(versions)) || (versionItr->contract_version != contract_version))
        {
            return {};
        }

        return versionItr->platform_version;
    }

    enum class typedef_name_type
    {
        Projected,
        CCW,
        ABI,
        NonProjected,
        StaticAbiClass,
        EventSource
    };

    std::string get_mapped_element_type(ElementType elementType)
    {
        switch (elementType)
        {
        case ElementType::End:
            return "End";
        case ElementType::Void:
            return "Void";
        case ElementType::Boolean:
            return "Boolean";
        case ElementType::Char:
            return "Char";
        case ElementType::I1:
            return "I1";
        case ElementType::U1:
            return "UI";
        case ElementType::I2:
            return "I2";
        case ElementType::U2:
            return "U2";
        case ElementType::I4:
            return "I4";
        case ElementType::U4:
            return "U4";
        case ElementType::I8:
            return "I8";
        case ElementType::U8:
            return "U8";
        case ElementType::R4:
            return "R4";
        case ElementType::R8:
            return "R8";
        case ElementType::String:
            return "String";
        case ElementType::Ptr:
            return "Ptr";
        case ElementType::ByRef:
            return "ByRef";
        case ElementType::ValueType:
            return "ValueType";
        case ElementType::Class:
            return "Class";
        case ElementType::Var:
            return "Var";
        case ElementType::Array:
            return "Array";
        case ElementType::GenericInst:
            return "GenericInst";
        case ElementType::TypedByRef:
            return "TypedByRef";
        case ElementType::I:
            return "IntPtr";
        case ElementType::U:
            return "UIntPtr";
        case ElementType::FnPtr:
            return "FnPtr";
        case ElementType::Object:
            return "Object";
        case ElementType::SZArray:
            return "SZArray";
        case ElementType::MVar:
            return "MVar";
        case ElementType::CModReqd:
            return "CModReqd";
        case ElementType::CModOpt:
            return "CModOpt";
        case ElementType::Internal:
            return "Internal";
        case ElementType::Modifier:
            return "Modifier";
        case ElementType::Sentinel:
            return "Sentinel";
        case ElementType::Pinned:
            return "Pinned";
        case ElementType::Type:
            return "Type";
        case ElementType::TaggedObject:
            return "TaggedObject";
        case ElementType::Field:
            return "Field";
        case ElementType::Property:
            return "Property";
        case ElementType::Enum:
            return "Enum";
        default:
            return "Unknown";
        }
    }

    template <typename T>
    int get_number_of_attributes(T const& row, std::string_view const& type_namespace, std::string_view const& type_name)
    {
        auto count = 0;
        for (auto&& attribute : row.CustomAttribute())
        {
            auto pair = attribute.TypeNamespaceAndName();

            if (pair.first == type_namespace && pair.second == type_name)
            {
                count++;
            }
        }
        return count;
    }

    int get_class_hierarchy_index(TypeDef const& classType)
    {
        auto sem = get_type_semantics(classType.Extends());
        if (std::holds_alternative<type_definition>(sem))
        {
            return get_class_hierarchy_index(std::get<type_definition>(sem)) + 1;
        }
        return 0;
    }

    bool interfaces_equal(TypeDef const& interface1, TypeDef const& interface2)
    {
        return interface1.TypeNamespace() == interface2.TypeNamespace()
            && interface1.TypeName() == interface2.TypeName();
    }

    template <typename T>
    auto get_attribute_value(CustomAttribute const& attribute, uint32_t const arg)
    {
        return std::get<T>(std::get<ElemSig>(attribute.Value().FixedArgs()[arg].value).value);
    }

    std::optional<int> get_contract_version(TypeDef const& type)
    {
        if (!has_attribute(type, "Windows.Foundation.Metadata", "ContractVersionAttribute"))
        {
            return {};
        }
        return get_attribute_value<uint32_t>(get_attribute(type, "Windows.Foundation.Metadata"sv, "ContractVersionAttribute"sv), 1);
    }

    std::optional<int> get_version(TypeDef const& type)
    {
        if (!has_attribute(type, "Windows.Foundation.Metadata", "VersionAttribute"))
        {
            return {};
        }
        return get_attribute_value<uint32_t>(get_attribute(type, "Windows.Foundation.Metadata"sv, "VersionAttribute"sv), 0);
    }

    bool is_fast_abi_class(TypeDef const& type)
    {
        return has_attribute(type, "Windows.Foundation.Metadata"sv, "FastAbiAttribute"sv) && !settings.netstandard_compat;
    }

    bool is_default_interface(InterfaceImpl const& ifaceImpl)
    {
        return has_attribute(ifaceImpl, "Windows.Foundation.Metadata", "DefaultAttribute");
    }

    std::optional<TypeDef> find_fast_abi_class_type(TypeDef const& iface)
    {
        static std::map<TypeDef, std::optional<TypeDef>> cache;
        if (cache.find(iface) != cache.end())
        {
            return cache[iface];
        }
        auto exclusiveToAttribute = get_attribute(iface, "Windows.Foundation.Metadata"sv, "ExclusiveToAttribute"sv);
        if (exclusiveToAttribute)
        {
            auto sys_type = get_attribute_value<ElemSig::SystemType>(exclusiveToAttribute, 0);
            TypeDef exclusiveToClass = iface.get_cache().find_required(sys_type.name);
            if (!is_fast_abi_class(exclusiveToClass))
            {
                return {};
            }
            cache[iface] = exclusiveToClass;
            return exclusiveToClass;
        }
        return {};
    }

    std::pair<TypeDef, std::vector<TypeDef>> get_default_and_exclusive_interfaces(TypeDef const& classType)
    {
        std::pair<TypeDef, std::vector<TypeDef>> exclusive_ifaces;
        for (auto&& ifaceImpl : classType.InterfaceImpl())
        {
            auto&& sem = get_type_semantics(ifaceImpl.Interface());
            if (is_default_interface(ifaceImpl))
            {
                exclusive_ifaces.first = std::get<type_definition>(sem);
            }
            else if (std::holds_alternative<type_definition>(sem))
            {
                if (has_attribute(std::get<type_definition>(sem), "Windows.Foundation.Metadata"sv, "ExclusiveToAttribute"sv))
                {
                    exclusive_ifaces.second.push_back(std::get<type_definition>(sem));
                }
            }
        }
        return exclusive_ifaces;
    }

    type_semantics get_default_iface_as_type_sem(TypeDef const& classType)
    {
        for (auto&& ifaceImpl : classType.InterfaceImpl())
        {
            auto&& sem = get_type_semantics(ifaceImpl.Interface());
            if (is_default_interface(ifaceImpl))
            {
                return sem;
            }
        }
        throw_invalid("Class does not have a default interface");
    }

    void sort_fast_abi_ifaces(std::vector<TypeDef>& fast_abi_ifaces)
    {
        std::sort(fast_abi_ifaces.begin(), fast_abi_ifaces.end(), [](TypeDef const& iface, TypeDef const& otherIface)
        {
            // compare relative contracts
            auto relativeContractValueIface = -1 * get_number_of_attributes(iface, "Windows.Foundation.Metadata"sv, "PreviousContractVersionAttribute"sv);
            auto relativeContractValueOtherIface = -1 * get_number_of_attributes(otherIface, "Windows.Foundation.Metadata"sv, "PreviousContractVersionAttribute"sv);
            if (relativeContractValueIface != relativeContractValueOtherIface)
                return relativeContractValueIface < relativeContractValueOtherIface;

            //compare contract versions if they exist
            auto contractVersionIface = get_contract_version(iface);
            auto contractVersionOtherIface = get_contract_version(otherIface);
            if (contractVersionIface.has_value() && contractVersionOtherIface.has_value() && contractVersionIface.value() != contractVersionOtherIface.value())
                return contractVersionIface.value() < contractVersionOtherIface.value();

            //compare versions
            auto versionIface = get_version(iface);
            auto versionOtherIface = get_version(otherIface);
            if (versionIface.has_value() && versionOtherIface.has_value() && versionIface.value() != versionOtherIface.value())
                return versionIface.value() < versionOtherIface.value();

            //compare type names
            return iface.TypeNamespace() == otherIface.TypeNamespace() ? iface.TypeName() < otherIface.TypeName() : iface.TypeNamespace() < otherIface.TypeNamespace();
        });
    }

    struct fast_abi_class
    {
        const TypeDef class_type;
        const TypeDef default_interface;
        const std::vector<TypeDef> other_interfaces;

        fast_abi_class(TypeDef class_type, TypeDef default_interface, std::vector<TypeDef> other_interfaces)
            : class_type(class_type), default_interface(default_interface), other_interfaces(other_interfaces)
        {
            int vtable_start_index = 6;
            add_property_caches(default_interface, vtable_start_index);
            vtable_start_index += distance(default_interface.MethodList()) + get_class_hierarchy_index(class_type);
            for (auto&& other_iface : other_interfaces)
            {
                other_interfaces_cache.insert(std::pair(other_iface.TypeNamespace(), other_iface.TypeName()));
                add_property_caches(other_iface, vtable_start_index);
                vtable_start_index += distance(other_iface.MethodList());
            }
        }

        bool contains_other_interface(TypeDef const& iface)
        {
            return other_interfaces_cache.find(std::pair(iface.TypeNamespace(), iface.TypeName())) != other_interfaces_cache.end();
        }

        std::pair<MethodDef, int> find_property_setter(std::string_view property_name)
        {
            return property_setters_cache[property_name];
        }

        bool contains_setter(std::string_view property_name)
        {
            return property_setters_cache.find(property_name) != property_setters_cache.end();
        }

        bool contains_getter(std::string_view property_name)
        {
            return property_getters_cache.find(property_name) != property_getters_cache.end();
        }

    private:
        std::set<std::pair<std::string_view, std::string_view>> other_interfaces_cache;
        std::map<std::string_view, std::pair<MethodDef, int>> property_setters_cache;
        std::set<std::string_view> property_getters_cache;
        
        void add_property_caches(TypeDef const& iface, int vtable_start_index)
        {
            for (auto prop : iface.PropertyList())
            {
                auto&& [getter, setter] = get_property_methods(prop);
                if (getter)
                {
                    property_getters_cache.insert(prop.Name());
                }
                if (setter)
                {
                    property_setters_cache[prop.Name()] = std::pair(setter, vtable_start_index);
                }
            }
        }
    };

    std::optional<fast_abi_class> get_fast_abi_class_for_class(TypeDef const& classType)
    {
        if (!is_fast_abi_class(classType))
        {
            return {};
        }
        auto [default_iface, other_ifaces] = get_default_and_exclusive_interfaces(classType);
        sort_fast_abi_ifaces(other_ifaces);
        return fast_abi_class(classType, default_iface, other_ifaces);
    }

    std::optional<fast_abi_class> get_fast_abi_class_for_interface(TypeDef const& iface)
    {
        auto fast_abi_class_type = find_fast_abi_class_type(iface);
        if (!fast_abi_class_type.has_value()) {
            return {};
        }
        return get_fast_abi_class_for_class(fast_abi_class_type.value());
    }

    int get_gc_pressure_amount(TypeDef const& classType)
    {
        auto gc_pressure_amount = 0;
        if (auto gc_pressure_attr = get_attribute(classType, "Windows.Foundation.Metadata", "GCPressureAttribute"))
        {
            // Restricting to sealed scenarios because unsealed scenarios require more handling to prevent mismatches in
            // adding and removing memory pressure and none of the Windows namespace types which use it today are unsealed.
            if (classType.Flags().Sealed())
            {
                auto sig = gc_pressure_attr.Value();
                auto const& args = sig.NamedArgs();
                auto amount = std::get<int32_t>(std::get<ElemSig::EnumValue>(std::get<ElemSig>(args[0].value.value).value).value);
                gc_pressure_amount = amount == 0 ? 12000 : amount == 1 ? 120000 : 1200000;
            }
        }
        return gc_pressure_amount;
    }

    struct generic_abi_delegate
    {
        std::string abi_delegate_name;
        std::string abi_delegate_declaration;
        std::string abi_delegate_types;

        // Hash / equality for the hast set this is added to is based on the types in the delegate
        // as we do not need duplicate delegate entries from different collection types.
        bool operator==(const generic_abi_delegate& entry) const
        {
            return abi_delegate_types == entry.abi_delegate_types;
        }
    };

    struct generic_type_instantiation
    {
        generic_type_instance instance;
        std::string instantiation_class_name;

        // Hash / equality for the hash set.
        bool operator==(const generic_type_instantiation& other) const
        {
            return instantiation_class_name == other.instantiation_class_name;
        }
    };

    std::string escape_type_name_for_identifier(std::string typeName)
    {
        std::regex re(R"-((\ |:|<|>|`|,|\.))-");
        return std::regex_replace(typeName, re, "_");
    }

    std::string get_fundamental_type_guid_signature(fundamental_type type)
    {
        switch (type)
        {
        case fundamental_type::Boolean: return "b1";
        case fundamental_type::Char: return "c2";
        case fundamental_type::Int8: return "i1";
        case fundamental_type::UInt8: return "u1";
        case fundamental_type::Int16: return "i2";
        case fundamental_type::UInt16: return "u2";
        case fundamental_type::Int32: return "i4";
        case fundamental_type::UInt32: return "u4";
        case fundamental_type::Int64: return "i8";
        case fundamental_type::UInt64: return "u8";
        case fundamental_type::Float: return "f4";
        case fundamental_type::Double: return "f8";
        case fundamental_type::String: return "string";
        default: throw_invalid("Unknown type");
        }
    }

    // Returns whether the ABI interface should implement the CCW interface or not.
    // In authoring scenarios, exclusive interfaces don't exist, so we use the CCW impl type.
    bool does_abi_interface_implement_ccw_interface(TypeDef const& type)
    {
        return settings.component &&
               settings.filter.includes(type) &&
               is_exclusive_to(type);
    }
}

namespace std
{
    template<>
    struct hash<cswinrt::generic_abi_delegate> {
        size_t operator()(const cswinrt::generic_abi_delegate& entry) const
        {
            return hash<string>()(entry.abi_delegate_types);
        }
    };

    template<>
    struct hash<cswinrt::generic_type_instantiation> {
        size_t operator()(const cswinrt::generic_type_instantiation& instantiation) const
        {
            return hash<string>()(instantiation.instantiation_class_name);
        }
    };
}
