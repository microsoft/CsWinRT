#include "pch.h"
#include "QIAgent.h"
#include "QIAgent.g.cpp"

namespace winrt::Beta::implementation
{
    bool QIAgent::CheckForIGamma(winrt::Alpha::IAlpha const& alpha)
    {
        return alpha.try_as<winrt::Gamma::IGamma>() ? true : false;
    }

    winrt::Alpha::IAlpha QIAgent::IdentityAlpha(winrt::Alpha::IAlpha const& alpha)
    {
        return alpha;
    }

    int32_t QIAgent::Run(winrt::Alpha::IAlpha const& alpha)
    {
        return alpha.Five();
    }
}
