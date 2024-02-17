# Optimizations: SIMD

> [!NOTE]
> These optimizations are additionally applied elsewhere. Currently, `MiscSimdApplicationSystem` goes unused as we haven't pinpointed any notable methods that would benefit from these changes that we don't already edit.

SIMD types can be used to gain performance boosts by the JITter.

## `MiscSimdApplicationSystem`

> Authored by:
> - [@steviegt6](https://github.com/steviegt6)

---

Applies SIMDification to various hotspots.
