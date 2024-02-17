# Optimizations: UI

Contains optimizations related to the UI. This refers to elements such as the inventory, menus, and the various overlays the game provides (laser ruler, etc.).

## `NewLaserRulerRenderSystem`

> Authored by:
> - [@Golfing7](https://github.com/Golfing7)
>
> Implemented in:
> - [GH-17](https://github.com/terraria-catalyst/Nitrate/pull/17)

---

The vanilla implementation of `Main.DrawInterface_3_LaserRuler` uses a large amount of Main.Draw calls to render something as simple as a grid.

### The Problem

The vanilla renderer of the laser ruler renders each grid cell individually, causing over 14,000 render calls for a single frame.

### The Solution

Instead of rendering each grid cell individually, render the grid as a sequence of overlapping shapes. 

The new system does so in the following order:
1. Background (1 call)
2. Grid lines (Up to ~240 calls, depends on screen size)
3. Red mouse hover highlight (3 calls)

#### Results

From basic profiling, the reduction in performance toll was seen to be around 25x. (From 15% usage to 0.6%).
