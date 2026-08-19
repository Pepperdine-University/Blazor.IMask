const moduleAssetBase = new URL(".", import.meta.url);
const bundledScriptSource = new URL("vendor/imask.min.js", moduleAssetBase).toString();
const maskElementSelector = [
    "input[data-imask]",
    "textarea[data-imask]",
    "input[data-imask-options]",
    "textarea[data-imask-options]",
    "input[data-imask-mask]",
    "textarea[data-imask-mask]"
].join(", ");
const maskInstances = new WeakMap();
const autoInitState =
{
    observer: null,
    isStarted: false,
    scriptLoad: null,
    scriptSource: null
};

const constructorsByName = new Map(
    [
        ["Number", Number],
        ["Date", Date]
    ]);

const presetOptionsByName =
{
    "phone-us": { mask: "(000) 000-0000" },
    ssn: { mask: "000-00-0000" },
    "zip-us": { mask: "00000[-0000]" },
    "currency-us":
    {
        mask: "$num",
        blocks:
        {
            num:
            {
                mask: Number,
                scale: 2,
                thousandsSeparator: ",",
                radix: ".",
                padFractionalZeros: true,
                normalizeZeros: true
            }
        }
    },
    number:
    {
        mask: Number,
        scale: 2,
        thousandsSeparator: ",",
        radix: ".",
        normalizeZeros: true
    },
    integer:
    {
        mask: Number,
        scale: 0,
        thousandsSeparator: ",",
        radix: ".",
        normalizeZeros: true
    }
};

export function configure(configuration = {})
{
    if (
        typeof configuration.scriptSource === "string" &&
        configuration.scriptSource.length > 0)
    {
        autoInitState.scriptSource = configuration.scriptSource;
    }
}

export function registerPreset(presetName, presetOptions)
{
    if (!presetName || typeof presetName !== "string")
    {
        throw new Error("An IMask preset name is required.");
    }

    presetOptionsByName[presetName] = normalizeMaskOptions(presetOptions);
}

export async function startAutoInit(configuration = {})
{
    configure(configuration);

    if (autoInitState.isStarted)
    {
        await safeRefresh();
        return;
    }

    autoInitState.isStarted = true;
    await safeRefresh();

    autoInitState.observer = new MutationObserver((mutations) =>
    {
        for (const mutation of mutations)
        {
            for (const addedNode of mutation.addedNodes)
            {
                if (addedNode instanceof Element)
                {
                    queueRefresh(addedNode);
                }
            }
        }
    });

    autoInitState.observer.observe(
        document.documentElement,
        {
            childList: true,
            subtree: true
        });
}

export function stopAutoInit()
{
    autoInitState.isStarted = false;

    if (autoInitState.observer)
    {
        autoInitState.observer.disconnect();
        autoInitState.observer = null;
    }
}

export async function refresh(refreshRoot = document)
{
    const maskElements = findMaskElements(refreshRoot);

    await Promise.all(maskElements.map((maskElement) => initElement(maskElement)));
}

export async function apply(targetElement, maskOptions = null)
{
    if (!(targetElement instanceof HTMLElement))
    {
        throw new Error("IMask can only be applied to HTML elements.");
    }

    const resolvedOptions = normalizeMaskOptions(maskOptions ?? readMaskOptions(targetElement));

    if (!resolvedOptions || !resolvedOptions.mask)
    {
        throw new Error("IMask options must include a mask.");
    }

    const imaskFactory = await ensureIMask();
    const existingMaskInstance = maskInstances.get(targetElement);

    if (existingMaskInstance)
    {
        existingMaskInstance.updateOptions(resolvedOptions);
        return createHandle(targetElement, existingMaskInstance);
    }

    const maskInstance = imaskFactory(targetElement, resolvedOptions);
    maskInstances.set(targetElement, maskInstance);
    targetElement.dispatchEvent(new CustomEvent("imask:ready", { bubbles: true }));

    return createHandle(targetElement, maskInstance);
}

export async function applyByElementIdentifier(elementIdentifier, maskOptions = null)
{
    if (!elementIdentifier || typeof elementIdentifier !== "string")
    {
        throw new Error("An element identifier is required.");
    }

    const targetElement = document.getElementById(elementIdentifier);

    if (!targetElement)
    {
        throw new Error(`No element with identifier "${elementIdentifier}" was found.`);
    }

    return apply(targetElement, maskOptions);
}

export async function applyByCssSelector(cssSelector, maskOptions = null)
{
    if (!cssSelector || typeof cssSelector !== "string")
    {
        throw new Error("A CSS selector is required.");
    }

    const targetElement = document.querySelector(cssSelector);

    if (!targetElement)
    {
        throw new Error(`No element matching selector "${cssSelector}" was found.`);
    }

    return apply(targetElement, maskOptions);
}

export function destroy(targetElement)
{
    const maskInstance = maskInstances.get(targetElement);

    if (!maskInstance)
    {
        return;
    }

    maskInstance.destroy();
    maskInstances.delete(targetElement);
    targetElement.dispatchEvent(new CustomEvent("imask:destroyed", { bubbles: true }));
}

export function destroyByElementIdentifier(elementIdentifier)
{
    if (!elementIdentifier || typeof elementIdentifier !== "string")
    {
        return;
    }

    const targetElement = document.getElementById(elementIdentifier);

    if (targetElement)
    {
        destroy(targetElement);
    }
}

export function destroyByCssSelector(cssSelector)
{
    if (!cssSelector || typeof cssSelector !== "string")
    {
        return;
    }

    const targetElement = document.querySelector(cssSelector);

    if (targetElement)
    {
        destroy(targetElement);
    }
}

async function initElement(maskElement)
{
    if (maskInstances.has(maskElement))
    {
        return;
    }

    const maskOptions = readMaskOptions(maskElement);

    if (maskOptions)
    {
        await apply(maskElement, maskOptions);
    }
}

async function safeRefresh(refreshRoot = document)
{
    try
    {
        await refresh(refreshRoot);
    }
    catch (initializationError)
    {
        reportError(initializationError);
    }
}

function queueRefresh(refreshRoot)
{
    refresh(refreshRoot).catch(reportError);
}

function reportError(initializationError)
{
    console.error("IMask failed to initialize a field.", initializationError);
}

function findMaskElements(searchRoot)
{
    const maskElements = [];

    if (searchRoot instanceof Element && searchRoot.matches(maskElementSelector))
    {
        maskElements.push(searchRoot);
    }

    if (searchRoot.querySelectorAll)
    {
        maskElements.push(...searchRoot.querySelectorAll(maskElementSelector));
    }

    return maskElements;
}

async function ensureIMask()
{
    if (globalThis.IMask)
    {
        return globalThis.IMask;
    }

    if (!autoInitState.scriptLoad)
    {
        autoInitState.scriptLoad = loadScript(resolveSource()).then(() =>
        {
            if (!globalThis.IMask)
            {
                throw new Error("IMask.js loaded but window.IMask was not found.");
            }

            return globalThis.IMask;
        });
    }

    return autoInitState.scriptLoad;
}

function resolveSource()
{
    const globalConfiguration = globalThis.IMaskBlazor ?? {};

    return (
        autoInitState.scriptSource ??
        globalConfiguration.scriptSource ??
        bundledScriptSource);
}

function loadScript(scriptSource)
{
    return new Promise((resolve, reject) =>
    {
        const absoluteSource = new URL(scriptSource, document.baseURI).toString();
        const existingScript = Array.from(document.scripts).find((documentScript) =>
            documentScript.src === absoluteSource ||
            documentScript.getAttribute("src") === scriptSource);

        if (existingScript)
        {
            if (existingScript.dataset.imaskBlazorLoaded === "true")
            {
                resolve();
                return;
            }

            existingScript.addEventListener("load", resolve, { once: true });
            existingScript.addEventListener("error", reject, { once: true });
            return;
        }

        const scriptElement = document.createElement("script");
        scriptElement.src = scriptSource;
        scriptElement.async = true;
        scriptElement.onload = () =>
        {
            scriptElement.dataset.imaskBlazorLoaded = "true";
            resolve();
        };
        scriptElement.onerror = () =>
            reject(new Error(`Unable to load IMask.js from ${scriptSource}.`));

        document.head.appendChild(scriptElement);
    });
}

function readMaskOptions(maskElement)
{
    const optionsAttributeOptions = parseJsonOptions(maskElement.getAttribute("data-imask-options"));
    const mainAttributeOptions = parseMainAttribute(maskElement.getAttribute("data-imask"));
    const optionAttributeOptions = readOptionAttributes(maskElement);
    const maskOptions = mergeMaskOptions(
        mainAttributeOptions,
        optionsAttributeOptions,
        optionAttributeOptions);

    return maskOptions && Object.keys(maskOptions).length > 0 ? maskOptions : null;
}

function parseMainAttribute(attributeValue)
{
    if (!attributeValue)
    {
        return null;
    }

    const trimmedValue = attributeValue.trim();

    if (!trimmedValue)
    {
        return null;
    }

    if (presetOptionsByName[trimmedValue])
    {
        return cloneMaskOptions(presetOptionsByName[trimmedValue]);
    }

    if (trimmedValue.startsWith("{") || trimmedValue.startsWith("["))
    {
        return parseJsonOptions(trimmedValue);
    }

    return { mask: trimmedValue };
}

function readOptionAttributes(maskElement)
{
    const attributeOptions = {};

    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "mask",
        "data-imask-mask",
        parseOptionValue);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "lazy",
        "data-imask-lazy",
        parseBooleanOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "overwrite",
        "data-imask-overwrite",
        parseBooleanOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "eager",
        "data-imask-eager",
        parseBooleanOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "autofix",
        "data-imask-autofix",
        parseBooleanOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "scale",
        "data-imask-scale",
        parseNumberOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "min",
        "data-imask-min",
        parseNumberOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "max",
        "data-imask-max",
        parseNumberOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "radix",
        "data-imask-radix",
        parseOptionValue);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "thousandsSeparator",
        "data-imask-thousands-separator",
        parseOptionValue);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "padFractionalZeros",
        "data-imask-pad-fractional-zeros",
        parseBooleanOption);
    assignOptionAttribute(
        attributeOptions,
        maskElement,
        "normalizeZeros",
        "data-imask-normalize-zeros",
        parseBooleanOption);

    return attributeOptions;
}

function assignOptionAttribute(
    targetOptions,
    maskElement,
    optionName,
    attributeName,
    valueParser)
{
    if (!maskElement.hasAttribute(attributeName))
    {
        return;
    }

    targetOptions[optionName] = valueParser(maskElement.getAttribute(attributeName));
}

function parseJsonOptions(jsonText)
{
    if (!jsonText)
    {
        return null;
    }

    return JSON.parse(jsonText);
}

function normalizeMaskOptions(rawOptions)
{
    if (typeof rawOptions === "string")
    {
        return cloneMaskOptions(presetOptionsByName[rawOptions] ?? { mask: rawOptions });
    }

    if (!rawOptions)
    {
        return null;
    }

    return reviveConstructors(cloneMaskOptions(rawOptions));
}

function mergeMaskOptions(...optionSources)
{
    const mergedOptions = {};

    for (const optionSource of optionSources)
    {
        const normalizedOptions = normalizeMaskOptions(optionSource);

        if (normalizedOptions)
        {
            Object.assign(mergedOptions, normalizedOptions);
        }
    }

    return reviveConstructors(mergedOptions);
}

function cloneMaskOptions(sourceOptions)
{
    if (Array.isArray(sourceOptions))
    {
        return sourceOptions.map((optionItem) => cloneMaskOptions(optionItem));
    }

    if (sourceOptions && typeof sourceOptions === "object")
    {
        const clonedOptions = {};

        for (const [optionName, optionValue] of Object.entries(sourceOptions))
        {
            clonedOptions[optionName] = cloneMaskOptions(optionValue);
        }

        return clonedOptions;
    }

    return sourceOptions;
}

function reviveConstructors(rawValue)
{
    if (Array.isArray(rawValue))
    {
        return rawValue.map((nestedValue) => reviveConstructors(nestedValue));
    }

    if (typeof rawValue === "string")
    {
        return parseOptionValue(rawValue);
    }

    if (rawValue && typeof rawValue === "object")
    {
        for (const [optionName, nestedValue] of Object.entries(rawValue))
        {
            rawValue[optionName] = reviveConstructors(nestedValue);
        }
    }

    return rawValue;
}

function parseOptionValue(optionValue)
{
    if (constructorsByName.has(optionValue))
    {
        return constructorsByName.get(optionValue);
    }

    if (typeof optionValue === "string" && optionValue.startsWith("RegExp:"))
    {
        const regularExpressionText = optionValue.slice("RegExp:".length);
        const regularExpressionMatch = regularExpressionText.match(/^\/(.*)\/([dgimsuvy]*)$/);

        if (regularExpressionMatch)
        {
            return new RegExp(regularExpressionMatch[1], regularExpressionMatch[2]);
        }

        return new RegExp(regularExpressionText);
    }

    return optionValue;
}

function parseBooleanOption(booleanText)
{
    return (
        booleanText === "" ||
        booleanText === "true" ||
        booleanText === "True" ||
        booleanText === "1");
}

function parseNumberOption(numberText)
{
    if (numberText === null || numberText === "")
    {
        return undefined;
    }

    const parsedNumber = Number(numberText);

    return Number.isNaN(parsedNumber) ? undefined : parsedNumber;
}

function createHandle(maskElement, maskInstance)
{
    return {
        getValue: () => maskInstance.value,
        setValue: (maskedValue) =>
        {
            maskInstance.value = maskedValue ?? "";
        },
        getUnmaskedValue: () => maskInstance.unmaskedValue,
        setUnmaskedValue: (unmaskedValue) =>
        {
            maskInstance.unmaskedValue = unmaskedValue ?? "";
        },
        getTypedValue: () => maskInstance.typedValue,
        setTypedValue: (typedValue) =>
        {
            maskInstance.typedValue = typedValue;
        },
        updateOptions: (maskOptions) =>
        {
            maskInstance.updateOptions(normalizeMaskOptions(maskOptions));
        },
        destroy: () =>
        {
            destroy(maskElement);
        }
    };
}
