# CsWinRT error CSWINRT3005

Windows Runtime APIs can be marked as experimental in their Windows Runtime metadata, with the `[Windows.Foundation.Metadata.Experimental]` attribute (`[experimental]` in MIDL). Such an API is published for evaluation purposes only: it can change shape or be removed entirely in any future Windows SDK, with no compatibility guarantee. CsWinRT projects that marker as `[System.Diagnostics.CodeAnalysis.Experimental]` with the `CSWINRT3005` diagnostic id, so the compiler reports every use site.

For instance, the following sample generates CSWINRT3005:

```csharp
using Windows.Graphics.Capture;

// CSWINRT3005: 'IDisplayGraphicsCaptureSession' is marked as experimental
void Capture(IDisplayGraphicsCaptureSession session)
{
}
```

## Additional resources

`CSWINRT3005` is reported when user code references a Windows Runtime API that CsWinRT projected with `[Experimental]`. Windows Runtime metadata has no per-API diagnostic id (the metadata attribute takes no arguments), so all experimental Windows Runtime APIs share this one id.

Following the [experimental attribute](https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-12.0/experimental-attribute) design, this is reported as an **error** rather than a warning, so that depending on an experimental API is always a deliberate choice. It can be suppressed exactly like a warning, which is how the opt-in is expressed.

Generated projection code suppresses this diagnostic: a projection has to name an experimental type in order to project it at all, so the marker is guidance for the consumers of a projection rather than for the projection itself.

> **Note**: previous versions of CsWinRT relied on the C# compiler recognizing `Windows.Foundation.Metadata.ExperimentalAttribute` by name and reporting `CS8305`. That warning was not actionable per API: it could not be suppressed for one API without suppressing it for all of them, and it carried no link to any documentation. Suppressions of `CS8305` should be replaced with `CSWINRT3005`.

## Recommended action

- Prefer a stable API when one exists, and treat the experimental one as temporary.
- If you do want to depend on an experimental API, opt in explicitly and as narrowly as possible:

```csharp
#pragma warning disable CSWINRT3005
IDisplayGraphicsCaptureSession session = CreateSession();
#pragma warning restore CSWINRT3005
```

  Or, to opt in for a whole project, add the id to `NoWarn`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CSWINRT3005</NoWarn>
</PropertyGroup>
```

- Be ready for the API to change: because it is experimental, an update to the Windows SDK your project targets can change its signature or remove it, and no servicing guarantee applies.
