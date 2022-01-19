#pragma once
#include "QIAgent.g.h"

namespace winrt::Beta::implementation
{
    struct QIAgent : QIAgentT<QIAgent>
    {
        QIAgent() = default;

        bool CheckForIGamma(winrt::Alpha::IAlpha const& alpha);
        winrt::Alpha::IAlpha IdentityAlpha(winrt::Alpha::IAlpha const& alpha);
        int32_t Run(winrt::Alpha::IAlpha const& alpha);
    };
}
namespace winrt::Beta::factory_implementation
{
    struct QIAgent : QIAgentT<QIAgent, implementation::QIAgent>
    {
    };
}
