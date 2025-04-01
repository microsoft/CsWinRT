#pragma once

namespace cswinrt
{
    struct settings_type
    {
        std::set<std::string> input;
        std::filesystem::path output_folder;
        bool verbose{};
        std::set<std::string> include;
        std::set<std::string> exclude;
        std::set<std::string> addition_exclude;
        winmd::reader::filter filter;
        winmd::reader::filter addition_filter;
        bool netstandard_compat{};
        bool net7_0_or_greater{};
        bool component{};
        bool internal{};
        bool embedded{};
        bool public_enums{};
        bool public_exclusiveto{};
        bool idic_exclusiveto{};
        bool partial_factory{};
    };

    extern settings_type settings;
}
