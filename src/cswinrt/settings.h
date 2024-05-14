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
        winmd::reader::filter filter;
        bool netstandard_compat{};
        bool net7_0_or_greater{};
        bool component{};
        bool internal{};
        bool embedded{};
        bool public_enums{};
    };

    extern settings_type settings;
}
