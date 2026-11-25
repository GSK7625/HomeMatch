// URL filters use API codes; device location stays in component state.
export function searchParamsFor(filters: Record<string, string | number | number[] | undefined>): URLSearchParams {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value === undefined || value === "" || value === "all") continue;
    if (Array.isArray(value)) value.forEach(id => params.append(key, String(id)));
    else params.set(key, String(value));
  }
  return params;
}
