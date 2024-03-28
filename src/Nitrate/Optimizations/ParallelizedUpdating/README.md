# Optimization: Parallelized Updating

Various parts of the game (especially within update loops) can be reliably parallelized to improve performance without introducing any bugs or performance regressions.

## `SceneMetricsParallelismSystem`

> Authored by:
> - [@Golfing7](https://github.com/Golfing7)
>
> Implemented in:
> - [](https://github.com/terraria-catalyst/Nitrate/pull/13)

---

In vanilla Terraria, the `SceneMetrics` class is responsible for keeping track of the how many tiles are 'near' the player.
(We use the word 'near' as this range extends off screen slightly)

These metrics are responsible for determining player biome, status effects such as heart lanterns, campfires, etc.

### The Problem

By default, the metrics are updated once every 4 frames. From my experimentation, each invocation produces around 40k tile lookups.

### The Solution

Since these metrics are only updated once every 4 frames, if we lay out the execution like so:
```
FRAME 0 - SceneMetrics Update
FRAME 1 - Skip
FRAME 2 - Skip
FRAME 3 - Skip
FRAME 4 - SceneMetrics Update
```
We can parallelize this process by starting the process of collecting new metrics every `4n + 1`th frame. For example:
```
FRAME 5 - Begin SceneMetrics Collection Async
FRAME 6 - Skip
FRAME 7 - Skip
FRAME 8 - Process Async SceneMetrics
FRAME 9 - Begin SceneMetrics Collection Async
```
On the frames we would normally scan the scene again, we simply process the data that was collected async 3 frames ago.

Note that due to some mod hooks such as `TileLoader.NearbyEffects`, we still need to process some of the data on the main thread.

#### Results

From experimentation, this can reduce the overall load of the method by up to 99%+, depending on how many mods are using the `TileLoader.NearbyEffects` hook.

[^1]: More experimentation MUST be done with other mods.
[^2]: Mods may use IL editing on the SceneMetrics class. In which case, this modification will not play nicely.
