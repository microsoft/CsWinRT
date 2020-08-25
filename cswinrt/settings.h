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
    };

    extern settings_type settings;
}
