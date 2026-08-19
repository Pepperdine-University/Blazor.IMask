# Pepperdine.Blazor.IMask

Pepperdine.Blazor.IMask is a packable Razor class library that adds IMask.js client-side
input masking to Blazor and ASP.NET Core projects through static web assets. Apps using
Pepperdine.Blazor.IMask do not need to install the `imask` npm package separately. Masks
can be added with `data-imask` attributes to existing `<input>`, `<textarea>`,
`<InputText>`, or other components that render text fields. For dynamic masks,
component-library fields, or access to unmasked and typed values, use the small C# interop
service instead.

## Package

Create a package from the project:

```powershell
dotnet pack src/Pepperdine.Blazor.IMask/Pepperdine.Blazor.IMask.csproj -c Release
```

Then reference the generated package from a C# app:

```xml
<PackageReference Include="Pepperdine.Blazor.IMask" Version="1.0.0" />
```

## Quick start

Blazor apps load Razor class library JavaScript initializers automatically. For the
attribute-only path below, `data-imask` fields are auto-initialized by the package
JavaScript initializer; no wrapper component, script tag, or `builder.Services.AddIMask()`
call is required.

```razor
@using Pepperdine.Blazor.IMask

<InputText @bind-Value="Phone" data-imask="@IMaskPresets.PhoneUs" />
<InputText @bind-Value="Ssn" data-imask="@IMaskPresets.Ssn" />
<input @bind="Amount" data-imask="@IMaskPresets.CurrencyUs" />
<input @bind="ZipCode" data-imask="@IMaskPresets.ZipUs" />
```

Blazor binding keeps its normal timing. During user input, IMask formats the element value
before Blazor reads the binding event, so `@bind` and `@bind-Value` receive the masked
value on their usual event (`onchange` by default, or `oninput` when a field or component
supports per-keystroke binding).

Normal Blazor binding reads the rendered element's `value`, which is the displayed masked
value. The attribute-only API does not provide a separate unmasked `@bind`. When you need
an unmasked or typed value, register the C# service, keep the returned `IMaskHandle`, and
call `GetUnmaskedValueAsync` or `GetTypedValueAsync<TValue>`. Values set through
`IMaskHandle` are programmatic JavaScript updates; update the bound C# value yourself when
setting values from code.

Register the service only when you want to inject `IIMaskService` for C# interop:

```csharp
using Pepperdine.Blazor.IMask;

builder.Services.AddIMask();
```

## Programmatic usage

Use the C# service when a field is rendered by a component, when you need to choose a mask
dynamically, or when you prefer explicit setup.

```razor
@using Pepperdine.Blazor.IMask
@implements IAsyncDisposable
@inject IIMaskService MaskService

<input @ref="_accountNumberInput" @bind="AccountNumber" />

@code {
    private ElementReference _accountNumberInput;
    private IMaskHandle? _accountMask;
    private string? AccountNumber { get; set; }

    protected override async Task OnAfterRenderAsync(bool isFirstRender)
    {
        if (isFirstRender)
        {
            _accountMask = await MaskService.ApplyMaskAsync(
                _accountNumberInput,
                new IMaskOptions("0000 0000 0000 0000"));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_accountMask is not null)
        {
            await _accountMask.DisposeAsync();
        }
    }
}
```

The service supports three ways to target an input:

```csharp
await MaskService.ApplyMaskAsync(phoneInput, IMaskPresets.PhoneUs);
await MaskService.ApplyMaskByElementIdentifierAsync("phone-input", IMaskPresets.PhoneUs);
await MaskService.ApplyMaskByCssSelectorAsync("[data-phone-input]", IMaskPresets.PhoneUs);
```

Use `ApplyMaskAsync` when you already have an `ElementReference`. Use
`ApplyMaskByElementIdentifierAsync` for third-party components that expose a stable input
element identifier. Use `ApplyMaskByCssSelectorAsync` as an escape hatch when neither an
element reference nor an element identifier is available.

Each apply method returns an `IMaskHandle` that can read values, update options, and
dispose the JavaScript mask:

```csharp
string? maskedValue = await maskHandle.GetValueAsync();
string? unmaskedValue = await maskHandle.GetUnmaskedValueAsync();
decimal? typedValue = await maskHandle.GetTypedValueAsync<decimal?>();
await maskHandle.UpdateOptionsAsync(IMaskPresets.Ssn);
await maskHandle.DisposeAsync();
```

Call programmatic apply methods from `OnAfterRenderAsync` after the input exists. Keep
element identifiers unique on the page, especially in repeated rows or grids.

## Component libraries

For component libraries, attach IMask to the rendered input instead of replacing the
component. Prefer the library's input element identifier parameter when one is available.

```razor
@using Pepperdine.Blazor.IMask
@using MudBlazor
@implements IAsyncDisposable
@inject IIMaskService MaskService

<MudTextField T="string"
              InputId="phone-input"
              @bind-Value="_phone"
              InputType="InputType.Telephone"
              Immediate="true" />

<MudDatePicker InputId="birth-date-input"
               Editable="true"
               DateFormat="MM/dd/yyyy"
               ImmediateText="true"
               @bind-Date="_birthDate" />

@code {
    private string? _phone;
    private DateTime? _birthDate;
    private readonly List<IMaskHandle> _maskHandles = [];

    protected override async Task OnAfterRenderAsync(bool isFirstRender)
    {
        if (!isFirstRender)
        {
            return;
        }

        _maskHandles.Add(
            await MaskService.ApplyMaskByElementIdentifierAsync(
                "phone-input",
                IMaskPresets.PhoneUs));
        _maskHandles.Add(
            await MaskService.ApplyMaskByElementIdentifierAsync(
                "birth-date-input",
                new IMaskOptions("00/00/0000")));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (IMaskHandle maskHandle in _maskHandles)
        {
            await maskHandle.DisposeAsync();
        }
    }
}
```

The example above applies masks once because the target input element identifiers and mask
options are static. If the component library keeps the same underlying `<input>` element
during normal re-renders, the existing IMask instance stays attached.

If a component can replace the underlying input, re-apply the mask after the replacement;
an `IMaskHandle` is tied to the DOM element it was created for. If the input stays in
place but mask options change, call `UpdateOptionsAsync` on the existing handle or dispose
the handle and apply a new mask. The declarative `data-imask*` path watches for newly
added matching inputs and textareas, but programmatic `ApplyMaskAsync`,
`ApplyMaskByElementIdentifierAsync`, and `ApplyMaskByCssSelectorAsync` calls should be coordinated
with the component's render lifecycle.

If a component does not expose a stable input element identifier, use an `ElementReference`
when the component provides one. Use CSS selectors only when the rendered markup is stable
enough to rely on.

## Attribute options

The `data-imask*` attributes are the Pepperdine.Blazor.IMask declarative markup API. The
package JavaScript initializer scans rendered `<input>` and `<textarea>` elements with
those attributes, builds an IMask.js options object, and passes it to IMask.js.

Start with `data-imask` and a named preset for common masks:

```razor
@using Pepperdine.Blazor.IMask

<input data-imask="@IMaskPresets.PhoneUs" />
<input data-imask="@IMaskPresets.CurrencyUs" />
```

Named presets included by the package initializer:

- `phone-us`: `(000) 000-0000`
- `ssn`: `000-00-0000`
- `zip-us`: `00000[-0000]`
- `currency-us`: dollar currency with grouped thousands and two decimals
- `number`: grouped decimal number
- `integer`: grouped whole number

For custom masks, use `data-imask` with a mask string:

```razor
<input data-imask="0000 0000 0000 0000" />
```

For IMask options, use either option-specific attributes or JSON. These two inputs create
the same number mask:

```razor
<input data-imask-mask="Number" data-imask-scale="2" data-imask-thousands-separator="," />

<input data-imask='{"mask":"Number","scale":2,"thousandsSeparator":","}' />
```

The option-specific attributes are a convenience for common scalar options:

- `data-imask-mask`
- `data-imask-lazy`
- `data-imask-overwrite`
- `data-imask-eager`
- `data-imask-autofix`
- `data-imask-scale`
- `data-imask-min`
- `data-imask-max`
- `data-imask-radix`
- `data-imask-thousands-separator`
- `data-imask-pad-fractional-zeros`
- `data-imask-normalize-zeros`

Use JSON for nested or less common IMask options. JSON can be placed in `data-imask` or
`data-imask-options`:

```razor
<input data-imask='{"mask":"Date","pattern":"m{/}`d{/}`Y"}' />
<input data-imask-options='{"mask":"RegExp:/^[0-9]+$/"}' />
```

The JSON form maps to the same IMask.js options object documented in the official
[IMask guide](https://imask.js.org/guide.html). HTML attributes cannot contain JavaScript
constructors or `RegExp` objects directly, so the initializer converts these strings
before passing options to IMask.js:

- `"Number"` becomes the JavaScript `Number` constructor.
- `"Date"` becomes the JavaScript `Date` constructor.
- `"RegExp:/pattern/flags"` becomes a JavaScript `RegExp`.

If you combine forms on the same element, Pepperdine.Blazor.IMask merges them in this
order: `data-imask`, then `data-imask-options`, then option-specific `data-imask-*`
attributes.

## IMask.js source

The package includes IMask.js as a static web asset:

```text
/_content/Pepperdine.Blazor.IMask/vendor/imask.min.js
```

The loader uses the bundled asset by default. If you want to load IMask.js from another
local path or CDN, set `scriptSource` before Blazor starts:

```html
<script>
  window.IMaskBlazor = {
    scriptSource: "/lib/imask/imask.min.js"
  };
</script>
```

For MVC or Razor Pages apps, start the static asset module manually:

```html
<script type="module">
  import { startAutoInit } from "/_content/Pepperdine.Blazor.IMask/imask-blazor.js";

  startAutoInit();
</script>
```

## Attribution

This package wraps and bundles [imask.js](https://imask.js.org), which is published as the
`imask` npm package and licensed under the MIT License. 

The full upstream license for imask.js is included in this repository at
`wwwroot/vendor/LICENSE.imask.txt` and is bundled inside the NuGet package.
