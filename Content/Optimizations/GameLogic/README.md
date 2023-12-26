# Optimizations: Game Logic

Contains optimizations pertaining to generic game logic (specifically logic that does not belong to existing, established areas: tile rendering, parallelization, particle rendering, etc.).

## `FasterPylonSystem`

> Authored by:
> - [@Golfing7](https://github.com/Golfing7)
>
> Implemented in:
> - [GH-13](https://github.com/terraria-catalyst/Nitrate/pull/13)

---

The vanilla implementation of `TeleportPylonsSystem.IsPlayerNearAPylon` uses the `Player.IsTileTypeInInteractionRange` method to find if any pylon is nearby the player.

### The Problem

A single invocation of this method can (and usually does) perform over 14,000 tile lookups.

### The Solution

Instead of searching for nearby pylons from the player, search for the player from every pylon.
The game limits the amount of pylons that can be placed[^1], putting an upper bound on the potential downside of this operation[^2].

#### Results

From testing, the original method would perform at an average of 0.5ms per invocation. The updated method reduces this to 0.005ms per invocation on average.

[^1]: PENDING: What happens if a mod permits multiple types of the same pylon to be placed? Needs looking into.
[^2]: Mods can, of course, add additional pylons, but this scales well enough and we will not end up with 14,000 pylons.
