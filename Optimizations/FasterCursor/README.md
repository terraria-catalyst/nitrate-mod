# Optimizations: Faster Cursor

> [!NOTE]
> This feature has been temporarily shelved while we work on other optimizations. It was sort of working, but inconsistent and very buggy.

Functionally rewrites the cursor to render considerably faster.

## `FasterCursorSystem`

> Authored by:
> - [@steviegt6](https://github.com/steviegt6)

---

Hooks into cursor rendering methods, draws it to a render target instead of the screen, and then uses SDL to natively set the cursor texture to the captured mouse cursor.
