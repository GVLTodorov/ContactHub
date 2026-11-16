/**
 * Escapes text for places that render a string as HTML, such as PrimeNG's ConfirmDialog
 * `message` (bound with `[innerHTML]`). Angular's sanitizer only strips scripts, so without this
 * a contact named `<img src=//tracker>` would still render as markup.
 */
export function escapeHtml(text: string): string {
  return text
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
