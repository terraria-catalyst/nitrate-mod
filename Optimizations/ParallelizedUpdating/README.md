# Optimization: Parallelized Updating

Various parts of the game (especially within update loops) can be reliably parallelized to improve performance without introducing any bugs or performance regressions.

## Dust Updating

Dust updating may be reliably parallelized (at least in the context of vanilla dusts) since no dust depends on the state of another dust, and the few fields that are changed during updating are negligible.

Dust parallelization is handled pretty simply by replacing a few method locals with thread-statics and wrapping the method in a detour that facilitates our `FasterParallel::For` method.
