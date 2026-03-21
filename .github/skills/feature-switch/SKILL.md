---
name: feature-switch
description: Add a new runtime feature switch for CsWinRT. Use when the user wants to add a feature switch, add a feature flag, add a trim-compatible configuration option, or make a feature opt-in or opt-out at compile time.
---

# Add a runtime feature switch

Add a new `[FeatureSwitchDefinition]`-based runtime feature switch to CsWinRT. Feature switches allow enabling or disabling runtime behavior at compile time in a way that ILLink (trimming) and ILC (Native AOT) can recognize, treating the values as constants and dead-code-eliminating all code behind disabled switches. This makes opt-in or opt-out features fully pay-for-play.

<investigate_before_answering>
Before adding the feature switch, read the existing switches to understand the naming conventions, code style, and patterns used:

Key files to read:
- `src/WinRT.Runtime2/Properties/WindowsRuntimeFeatureSwitches.cs` — all existing feature switches
- `nuget/Microsoft.Windows.CsWinRT.targets` — MSBuild property defaults and `RuntimeHostConfigurationOption` items (at the bottom of the file)
</investigate_before_answering>

## Step 1: Add the feature switch definition

**File:** `src/WinRT.Runtime2/Properties/WindowsRuntimeFeatureSwitches.cs`

Add three things to the `WindowsRuntimeFeatureSwitches` class, following the existing pattern exactly:

### 1a. Add a private constant for the configuration property name

Add it after the last existing `private const string` field, in the constants block at the top of the class. Use the existing naming conventions:

- The C# field name is `{PropertyName}PropertyName` (e.g. `EnableFooPropertyName`)
- The string value uses the `CSWINRT_` prefix with `SCREAMING_SNAKE_CASE` (e.g. `"CSWINRT_ENABLE_FOO"`)

**Conventions:**
- The constant name must match the property name with a `PropertyName` suffix
- The string value must use `CSWINRT_` prefix followed by the property name converted to `SCREAMING_SNAKE_CASE`

### 1b. Add a public static property with `[FeatureSwitchDefinition]`

Add it after the last existing public property, maintaining the same ordering as the constants. Follow this exact pattern:

```csharp
/// <summary>
/// Gets a value indicating whether or not [describe the feature] (defaults to <see langword="true/false"/>).
/// </summary>
[FeatureSwitchDefinition(MyNewSwitchPropertyName)]
public static bool MyNewSwitch { get; } = GetConfigurationValue(MyNewSwitchPropertyName, defaultValue: true);
```

**Conventions:**
- The property must have `[FeatureSwitchDefinition(...)]` referencing the constant field (not a string literal)
- The property must be initialized via `GetConfigurationValue(...)` with the constant and the correct default value
- The XML doc must state the default value using `<see langword="true"/>` or `<see langword="false"/>`
- The XML doc must use `/// <summary>` style (no remarks)
- The property must use `{ get; } =` (auto-property with initializer), not a getter body — this is critical to avoid generating a static constructor, which would break ILLink's ability to substitute the value

### 1c. Add a constant XML doc `<see cref="..."/>` back-reference

The private constant's XML doc comment must reference the property via `<see cref="PropertyName"/>`:

```csharp
/// <summary>
/// The configuration property name for <see cref="MyNewSwitch"/>.
/// </summary>
private const string MyNewSwitchPropertyName = "CSWINRT_MY_NEW_SWITCH";
```

## Step 2: Add the MSBuild property and runtime host configuration

**File:** `nuget/Microsoft.Windows.CsWinRT.targets`

Make two additions at the bottom of the file, in the existing feature switch sections.

### 2a. Add a default MSBuild property

Add a new property to the `PropertyGroup` under the `<!-- Default values for all custom CsWinRT runtime feature switches -->` comment. Follow the existing pattern:

```xml
<CsWinRTMyNewSwitch Condition="'$(CsWinRTMyNewSwitch)' == ''">true</CsWinRTMyNewSwitch>
```

**Conventions:**
- The property name uses `CsWinRT` prefix followed by PascalCase property name (matching the C# property)
- The `Condition` guard ensures user-specified values are not overwritten
- The default value must match the `defaultValue` argument from `GetConfigurationValue` in the C# code

### 2b. Add a `RuntimeHostConfigurationOption` item

Add a new item to the `ItemGroup` under the `<!-- Configuration for the feature switches (to support IL trimming) -->` comment:

```xml
<!-- CSWINRT_MY_NEW_SWITCH switch -->
<RuntimeHostConfigurationOption Include="CSWINRT_MY_NEW_SWITCH"
                                Value="$(CsWinRTMyNewSwitch)"
                                Trim="true" />
```

**Conventions:**
- The `Include` value must exactly match the string constant from `WindowsRuntimeFeatureSwitches`
- The `Value` must reference the MSBuild property from step 2a
- `Trim="true"` is always set — this is what tells ILLink/ILC to treat the value as a constant
- Add a comment above the item with the switch name (e.g. `<!-- CSWINRT_MY_NEW_SWITCH switch -->`)
- Maintain consistent formatting with 4-space indentation and aligned attributes

## Commit convention

**Commit all changes together** (both files) with a message like:

```
Add CSWINRT_MY_NEW_SWITCH feature switch

Add a new [FeatureSwitchDefinition]-based feature switch to control [description].
Defaults to [true/false]. Configurable via the CsWinRTMyNewSwitch MSBuild property.
```
