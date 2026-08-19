import { startAutoInit } from "./imask-blazor.js";

export function afterWebStarted()
{
    return startAutoInit();
}

export function afterStarted()
{
    return startAutoInit();
}
