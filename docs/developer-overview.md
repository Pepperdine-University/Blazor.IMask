# Developer Overview

This project is a small Razor class library that packages IMask.js for Blazor and
ASP.NET Core consumers. Its job is to make client-side input masking available without
requiring app projects to manage an npm dependency, a wrapper component, or a manual
script tag for the common case.

The README is the consumer-facing usage guide, and the XML comments describe the public
C# API members. This document is meant to explain the design from a maintainer's point
of view.

## Project Shape

The repository contains one packable project:

- `src/Pepperdine.Blazor.IMask/Pepperdine.Blazor.IMask.csproj` defines a Razor class
  library that targets `net8.0`, `net9.0`, and `net10.0`.
- `src/Pepperdine.Blazor.IMask/wwwroot` contains static web assets shipped by the
  NuGet package.
- `src/Pepperdine.Blazor.IMask/wwwroot/vendor` contains the bundled upstream IMask.js
  distribution and license.
- `README.md`, `LICENSE`, and `THIRD-PARTY-NOTICES.txt` are included in the package.

There are no components in the library. The integration is intentionally built around
existing DOM elements rendered by Blazor, Razor Pages, MVC, or third-party component
libraries.

## Runtime Model

There are two runtime paths into the same JavaScript bridge:

1. Declarative attributes on rendered fields.
2. Programmatic C# interop through `IIMaskService` or `IJSRuntime` extension methods.

Both paths eventually call exported functions in
`wwwroot/imask-blazor.js`. That module owns script loading, option normalization, DOM
lookup, IMask instance creation, handle creation, and cleanup.

The C# layer does not implement masking rules. It serializes options and forwards calls
to the JavaScript module. IMask.js remains the source of truth for input behavior,
formatting, typed values, and option semantics.

## Static Assets And Startup

`wwwroot/Pepperdine.Blazor.IMask.lib.module.js` is the package JavaScript initializer.
Blazor loads this module automatically from Razor class library static web assets and
calls the lifecycle exports. Both exported startup hooks delegate to `startAutoInit()`.

`wwwroot/imask-blazor.js` is the actual implementation module. It resolves the bundled
IMask.js script relative to its own module URL:

```text
/_content/Pepperdine.Blazor.IMask/vendor/imask.min.js
```

The bridge only loads IMask.js when a mask is needed. It first reuses `globalThis.IMask`
when the host app already supplied it. Otherwise it injects a script element for either
the configured script source or the bundled vendor asset. The load promise is cached so
concurrent mask initialization shares the same script load.

Consumers can override the script source before startup through `window.IMaskBlazor` or
at runtime through the bridge `configure()` export. This is useful when a host app wants
to provide IMask.js from a CDN or from a different static asset path.

## Declarative Attribute Flow

The auto-init path is designed for fields that can express their mask in markup.
`startAutoInit()` performs an initial refresh and then attaches a `MutationObserver` to
`document.documentElement`. Newly added elements are scanned so masks can be applied after
Blazor renders conditional content, list items, or component-library output.

The scan only considers `input` and `textarea` elements with one of the supported
`data-imask*` attributes. Matching elements are initialized once. A `WeakMap` stores the
active IMask instance by DOM element, which gives the module idempotence without keeping
detached elements alive.

Attribute options are merged in this precedence order:

1. `data-imask`
2. `data-imask-options`
3. option-specific `data-imask-*` attributes

The last source wins when the same option is provided more than once. This allows a
preset or JSON object to provide a base mask while scalar attributes override selected
values.

The declarative path is intentionally limited to values that can be represented in HTML
attributes. The bridge revives selected string values into JavaScript objects before
calling IMask.js: constructor names such as `Number` and `Date`, plus `RegExp:` strings.
Anything more complex should use JSON where possible or the programmatic path.

## Programmatic Interop Flow

The service and extension methods are thin import-and-call wrappers over the JavaScript
module. They exist for cases where markup attributes are not enough: dynamic mask choice,
third-party components where the rendered `input` or `textarea` cannot be addressed
declaratively, access to unmasked or typed values, or explicit lifecycle control.

`ServiceCollectionExtensions.AddIMask()` registers a scoped `IIMaskService`. The concrete
service lazy-loads the JavaScript module once per service instance and reuses that module
reference for all calls in the scope. This is the preferred path when a component makes
several calls over its lifetime.

The `IJSRuntime` extension methods avoid DI registration. They import the module per
operation. When an operation returns an `IMaskHandle`, the handle owns that module
reference so the module can stay alive as long as the JavaScript object reference is
needed.

All programmatic apply functions return an `IMaskHandle`. The handle wraps the JavaScript
object returned by `createHandle()` in the bridge module. Its methods operate on the
underlying IMask instance rather than on C# state. Programmatic value setters update the
browser-side mask; callers remain responsible for keeping their Blazor model values in
sync when they set values from .NET.

## Option Objects

`IMaskOptions` is a flexible option bag for .NET callers. The `Mask` property serializes
as `mask`, and `AdditionalOptions` uses JSON extension data so callers can pass IMask.js
options that do not have dedicated C# properties.

This is deliberately loose. Mirroring the full IMask.js type system in C# would create a
large maintenance burden and still fail to cover every JavaScript option shape. The
library instead preserves a small .NET surface and lets the JavaScript bridge normalize
payloads before passing them to IMask.js.

Preset names are shared across C# and JavaScript by convention. `IMaskPresets` exposes
the known string names to .NET callers, while `presetOptionsByName` in
`imask-blazor.js` maps those names to actual IMask.js options. When adding a preset,
update both places so markup and C# callers stay aligned.

## Lifetime And Cleanup

There are three cleanup layers:

- JavaScript `destroy()` destroys the IMask instance, removes it from the `WeakMap`, and
  dispatches an `imask:destroyed` event.
- `IMaskHandle.DisposeAsync()` calls the JavaScript handle's `destroy()` function and
  disposes the JS object reference.
- `IMaskService.DisposeAsync()` disposes its cached module reference if it was created.

Disposal catches `JSDisconnectedException` because Blazor circuits or browser contexts
can disappear before .NET cleanup runs. That keeps component disposal from failing during
navigation, refresh, or disconnected server-side circuits.

Applying a mask to an element that already has one updates the existing instance's
options and returns a new handle wrapper for the same JavaScript mask. Declarative refresh
skips already-masked elements, while explicit apply calls are allowed to update them.

## Error Handling

Programmatic calls generally let errors propagate to the .NET caller. That makes missing
elements, invalid selectors, missing mask options, and script-load failures visible to the
component that requested the operation.

Auto-init takes a softer approach. Initialization failures are caught and logged with
`console.error()` so one bad declarative field does not prevent the page from continuing
to render.

Cancellation tokens are checked around the module import and are passed through JS
interop calls. Once execution has crossed into JavaScript, normal browser-side work is not
cancelled mid-function.

## Design Boundaries

This package does not own form validation, model binding, or custom Blazor input
components. It attaches behavior to elements after they exist in the DOM.

Blazor still owns its normal binding events. IMask changes the element's displayed value
before Blazor reads it, so standard binding receives the masked value unless the caller
uses a handle to read unmasked or typed values.

The library also avoids transforming arbitrary component markup. For component libraries,
the intended integration point is a stable input element reference, identifier, or CSS
selector supplied by the consuming app.

## Maintenance Notes

When changing the JavaScript bridge, keep these invariants in mind:

- `startAutoInit()` should remain idempotent.
- `refresh()` should tolerate being called repeatedly on the document or on a subtree.
- The `WeakMap` should remain the single registry of DOM elements to IMask instances.
- Script loading should continue to share one load promise per module instance.
- Programmatic apply should update existing masks, while declarative refresh should skip
  already-initialized elements.
- Option normalization should clone presets and caller-provided objects before reviving
  constructor or regular expression values.

When changing the C# layer, preserve the thin-wrapper model:

- The public surface should stay small and focused on DOM targeting, option passing,
  handle operations, and disposal.
- The service should continue to cache its module reference within the DI scope.
- The extension methods should continue to work without requiring service registration.
- JS object references that outlive a single call need an owner responsible for disposal.

## Where To Look First

For a new change, start with the layer that owns the behavior:

- Declarative attribute behavior: `wwwroot/imask-blazor.js`
- Blazor automatic startup: `wwwroot/Pepperdine.Blazor.IMask.lib.module.js`
- DI-backed interop: `IMaskService.cs`
- No-DI interop: `IMaskJsRuntimeExtensions.cs`
- Handle lifetime and value operations: `IMaskHandle.cs`
- .NET option serialization: `IMaskOptions.cs`
- Preset names exposed to .NET callers: `IMaskPresets.cs`
- Package metadata and static asset inclusion: `Pepperdine.Blazor.IMask.csproj`
