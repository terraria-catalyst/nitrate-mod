# Nitrate

Nitrate is a performance optimization mod focused on generalized improvements to many features throughout the game.

Leveraging parallelism, SIMD instructions, GPU rendering, and other optimizations, Nitrate can provide a significant boost in performance.

**NITRATE IS STILL IN ITS EARLY STAGES OF DEVELOPMENT; CERTAIN FEATURES MAY BE ENTIRELY INCOMPATIBLE WITH OTHER MODS WHILE WE ATTEMPT TO STRIKE A BALANCE BETWEEN PERFORMANCE AND ACCURACY. WHILE YOUR SAVES SHOULD BE SAFE, VISUAL DISPARITIES AND CRASHES MAY STILL OCCUR.**

## General Feature Overview

- Rewrites and optimizes dust rendering to be done almost entirely on the GPU (~5x faster in testing, mileage may vary)
- Parallelizes dust updating (~5x faster in testing, mileage may vary)
- Replaces usages of typical FNA types with JIT intrinsic SIMD types in notable hotspots
