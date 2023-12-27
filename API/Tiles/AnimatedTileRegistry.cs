using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Nitrate.API.Tiles;

/// <summary>
///     Keeps track of "animated" tiles, which are any tiles that have logic
///     resulting in some untracked, dynamic tile updating. This may include
///     interactables such as chests, passively animated tiles such as the Blue
///     Starry Block, and special conditions like Spelunker Potions causing
///     shinies to emanate yellow. Additionally, modded tiles and walls that
///     override/implement certain methods are also lumped in here, since they
///     may expect logic that runs every frame/every tile draw.
/// </summary>
/// <remarks>
///     Inheritance from <see cref="ModSystem"/> is not an API guarantee but
///     rather an implementation detail.
///     <br />
///     Tiles and walls known to this registry are essentially flagged as "to be
///     drawn manually". The way in which this manifests may differ depending on
///     how it is used, but the rewritten Nitrate tile renderer uses this data
///     to allow special tiles to render themselves like normal, since it'd
///     otherwise be extremely out of scope.
/// </remarks>
public sealed class AnimatedTileRegistry : ModSystem
{
    /// <summary>
    ///     Different types of well-known reasons for being considered
    ///     "animated". No logic in Nitrate relies on this, but it may be useful
    ///     for other consumers.
    /// </summary>
    [Flags]
    public enum TileAnimatedType
    {
        /// <summary>
        ///     Animated-through-interaction tiles like chests.
        /// </summary>
        Interactable     = 0b0001,

        /// <summary>
        ///     Passively animated tiles like Blue Starry Blocks.
        /// </summary>
        Passive          = 0b0010,

        /// <summary>
        ///     Tiles which provide special points like trees and master mode
        ///     relics.
        /// </summary>
        SpecialPoint     = 0b0100,

        /// <summary>
        ///     Automatically detected modded tiles which *may* be animated.
        /// </summary>
        ModdedAutoDetect = 0b1000,
    }

    private static readonly Dictionary<int, TileAnimatedType> vanilla_tiles = new()
    {
        { TileID.Plants, TileAnimatedType.SpecialPoint },
        { TileID.Torches, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Trees, TileAnimatedType.SpecialPoint }, // TODO: Necessary?
        { TileID.ClosedDoor, TileAnimatedType.Interactable }, // TODO: Necessary?
        { TileID.OpenDoor, TileAnimatedType.Interactable }, // TODO: Necessary?
        { TileID.Heart, TileAnimatedType.Passive },
        { TileID.Furnaces, TileAnimatedType.Passive },
        { TileID.Saplings, TileAnimatedType.SpecialPoint },
        { TileID.Containers, TileAnimatedType.Interactable }, // Chests.
        { TileID.CorruptGrass, TileAnimatedType.SpecialPoint },
        { TileID.Sunflower, TileAnimatedType.SpecialPoint },
        { TileID.ShadowOrbs, TileAnimatedType.Passive },
        { TileID.Candles, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Chandeliers, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Jackolanterns, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.HangingLanterns, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.WaterCandle, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Vines, TileAnimatedType.SpecialPoint },
        { TileID.JunglePlants, TileAnimatedType.SpecialPoint },
        { TileID.JungleVines, TileAnimatedType.SpecialPoint },
        { TileID.MushroomPlants, TileAnimatedType.SpecialPoint },
        // TileID.MushroomTrees
        { TileID.Plants2, TileAnimatedType.SpecialPoint },
        { TileID.JunglePlants2, TileAnimatedType.SpecialPoint },
        { TileID.Hellforge, TileAnimatedType.Passive },
        { TileID.Coral, TileAnimatedType.SpecialPoint },
        { TileID.ImmatureHerbs, TileAnimatedType.SpecialPoint },
        { TileID.MatureHerbs, TileAnimatedType.SpecialPoint },
        { TileID.BloomingHerbs, TileAnimatedType.SpecialPoint },
        { TileID.Loom, TileAnimatedType.SpecialPoint },
        { TileID.Banners, TileAnimatedType.SpecialPoint },
        { TileID.Lamps, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.CookingPots, TileAnimatedType.Passive },
        { TileID.SkullLanterns, TileAnimatedType.Passive },
        { TileID.Candelabras, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Sawmill, TileAnimatedType.Passive },
        { TileID.HallowedPlants, TileAnimatedType.SpecialPoint },
        { TileID.HallowedPlants2, TileAnimatedType.SpecialPoint },
        { TileID.HallowedVines, TileAnimatedType.SpecialPoint },
        { TileID.Crystals, TileAnimatedType.Passive }, // Gelatin Crystal
        { TileID.AdamantiteForge, TileAnimatedType.Passive },
        { TileID.PlatinumCandelabra, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.CrimsonPlants, TileAnimatedType.SpecialPoint },
        { TileID.CrimsonVines, TileAnimatedType.SpecialPoint },
        { TileID.WaterFountain, TileAnimatedType.Passive },
        { TileID.Cannon, TileAnimatedType.Interactable },
        { TileID.Campfire, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Blendomatic, TileAnimatedType.Passive },
        { TileID.MeatGrinder, TileAnimatedType.Passive },
        { TileID.Extractinator, TileAnimatedType.Passive },
        { TileID.Solidifier, TileAnimatedType.Passive },
        { TileID.Teleporter, TileAnimatedType.Passive },
        { TileID.PlanteraBulb, TileAnimatedType.Passive },
        { TileID.ImbuingStation, TileAnimatedType.Passive },
        { TileID.BubbleMachine, TileAnimatedType.Passive },
        { TileID.Autohammer, TileAnimatedType.Passive },
        { TileID.Pumpkins, TileAnimatedType.Passive },
        { TileID.BunnyCage, TileAnimatedType.Passive },
        { TileID.SquirrelCage, TileAnimatedType.Passive },
        { TileID.MallardDuckCage, TileAnimatedType.Passive },
        { TileID.DuckCage, TileAnimatedType.Passive },
        { TileID.BirdCage, TileAnimatedType.Passive },
        { TileID.BlueJay, TileAnimatedType.Passive },
        { TileID.CardinalCage, TileAnimatedType.Passive },
        { TileID.FishBowl, TileAnimatedType.Passive },
        { TileID.SnailCage, TileAnimatedType.Passive },
        { TileID.GlowingSnailCage, TileAnimatedType.Passive },
        { TileID.MonarchButterflyJar, TileAnimatedType.Passive },
        { TileID.PurpleEmperorButterflyJar, TileAnimatedType.Passive },
        { TileID.RedAdmiralButterflyJar, TileAnimatedType.Passive },
        { TileID.UlyssesButterflyJar, TileAnimatedType.Passive },
        { TileID.SulphurButterflyJar, TileAnimatedType.Passive },
        { TileID.TreeNymphButterflyJar, TileAnimatedType.Passive },
        { TileID.ZebraSwallowtailButterflyJar, TileAnimatedType.Passive },
        { TileID.JuliaButterflyJar, TileAnimatedType.Passive },
        { TileID.ScorpionCage, TileAnimatedType.Passive },
        { TileID.BlackScorpionCage, TileAnimatedType.Passive },
        { TileID.FrogCage, TileAnimatedType.Passive },
        { TileID.MouseCage, TileAnimatedType.Passive },
        { TileID.BoneWelder, TileAnimatedType.Passive },
        { TileID.FleshCloningVat, TileAnimatedType.Passive },
        { TileID.GlassKiln, TileAnimatedType.Passive },
        { TileID.LihzahrdFurnace, TileAnimatedType.Passive },
        { TileID.SkyMill, TileAnimatedType.Passive },
        { TileID.IceMachine, TileAnimatedType.Passive },
        { TileID.SteampunkBoiler, TileAnimatedType.Passive },
        { TileID.HoneyDispenser, TileAnimatedType.Passive },
        { TileID.PenguinCage, TileAnimatedType.Passive },
        { TileID.WormCage, TileAnimatedType.Passive },
        { TileID.MinecartTrack, TileAnimatedType.Passive }, // Booster Track
        { TileID.BlueJellyfishBowl, TileAnimatedType.Passive },
        { TileID.GreenJellyfishBowl, TileAnimatedType.Passive },
        { TileID.PinkJellyfishBowl, TileAnimatedType.Passive },
        { TileID.Waterfall, TileAnimatedType.Passive },
        { TileID.Lavafall, TileAnimatedType.Passive },
        { TileID.Confetti, TileAnimatedType.Passive },
        { TileID.ConfettiBlack, TileAnimatedType.Passive },
        { TileID.LivingFire, TileAnimatedType.Passive },
        { TileID.LivingCursedFire, TileAnimatedType.Passive },
        { TileID.LivingDemonFire, TileAnimatedType.Passive },
        { TileID.LivingFrostFire, TileAnimatedType.Passive },
        { TileID.LivingIchor, TileAnimatedType.Passive },
        { TileID.LivingUltrabrightFire, TileAnimatedType.Passive },
        { TileID.Honeyfall, TileAnimatedType.Passive },
        { TileID.ChimneySmoke, TileAnimatedType.Passive },
        { TileID.BewitchingTable, TileAnimatedType.Passive },
        { TileID.AlchemyTable, TileAnimatedType.Passive },
        { TileID.Sundial, TileAnimatedType.Interactable },
        { TileID.GoldBirdCage, TileAnimatedType.Passive },
        { TileID.GoldBunnyCage, TileAnimatedType.Passive },
        { TileID.GoldButterflyCage, TileAnimatedType.Passive },
        { TileID.GoldFrogCage, TileAnimatedType.Passive },
        { TileID.GoldGrasshopperCage, TileAnimatedType.Passive },
        { TileID.GoldMouseCage, TileAnimatedType.Passive },
        { TileID.GoldWormCage, TileAnimatedType.Passive },
        { TileID.PeaceCandle, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.WaterDrip, TileAnimatedType.Passive },
        { TileID.LavaDrip, TileAnimatedType.Passive },
        { TileID.HoneyDrip, TileAnimatedType.Passive },
        { TileID.SharpeningStation, TileAnimatedType.Passive },
        { TileID.TargetDummy, TileAnimatedType.Passive },
        { TileID.Bubble, TileAnimatedType.Passive },
        { TileID.TrapdoorOpen, TileAnimatedType.Interactable },
        { TileID.TrapdoorClosed, TileAnimatedType.Interactable },
        { TileID.TallGateClosed, TileAnimatedType.Interactable },
        { TileID.TallGateOpen, TileAnimatedType.Interactable },
        { TileID.LavaLamp, TileAnimatedType.Passive },
        { TileID.CageEnchantedNightcrawler, TileAnimatedType.Passive },
        { TileID.CageBuggy, TileAnimatedType.Passive },
        { TileID.CageGrubby, TileAnimatedType.Passive },
        { TileID.CageSluggy, TileAnimatedType.Passive },
        { TileID.ItemFrame, TileAnimatedType.SpecialPoint }, // TODO: Tile entity, how to handle?
        { TileID.Fireplace, TileAnimatedType.Passive },
        { TileID.Chimney, TileAnimatedType.Passive },
        { TileID.LunarMonolith, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Detonator, TileAnimatedType.Interactable },
        { TileID.LunarCraftingStation, TileAnimatedType.Passive },
        { TileID.SquirrelOrangeCage, TileAnimatedType.Passive },
        { TileID.SquirrelGoldCage, TileAnimatedType.Passive },
        { TileID.ConveyorBeltLeft, TileAnimatedType.Passive },
        { TileID.ConveyorBeltRight, TileAnimatedType.Passive },
        { TileID.WeightedPressurePlate, TileAnimatedType.Interactable },
        { TileID.FakeContainers, TileAnimatedType.Interactable },
        { TileID.SillyBalloonMachine, TileAnimatedType.Passive },
        { TileID.SillyBalloonTile, TileAnimatedType.Passive },
        { TileID.Pigronata, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.PartyMonolith, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.PartyBundleOfBalloonTile, TileAnimatedType.Passive },
        { TileID.SandFallBlock, TileAnimatedType.Passive },
        { TileID.SnowFallBlock, TileAnimatedType.Passive },
        { TileID.SandDrip, TileAnimatedType.Passive },
        { TileID.DefendersForge, TileAnimatedType.Passive },
        { TileID.WarTable, TileAnimatedType.Passive },
        { TileID.Containers2, TileAnimatedType.Interactable },
        { TileID.FakeContainers2, TileAnimatedType.Interactable },
        { TileID.DisplayDoll, TileAnimatedType.Interactable }, // TODO: Tile entity, how to handle? Also what about the old tile IDs?
        { TileID.WeaponsRack2, TileAnimatedType.Interactable }, // ^
        { TileID.HatRack, TileAnimatedType.Interactable }, // ^
        { TileID.BloodMoonMonolith, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.AntlionLarva, TileAnimatedType.Passive },
        { TileID.PinWheel, TileAnimatedType.Passive },
        { TileID.WeatherVane, TileAnimatedType.Passive },
        { TileID.VoidVault, TileAnimatedType.Passive },
        { TileID.LesionStation, TileAnimatedType.Passive },
        { TileID.MysticSnakeRope, TileAnimatedType.Interactable }, // Might cause issues with weird updating, so including.
        { TileID.GoldGoldfishBowl, TileAnimatedType.Passive },
        { TileID.GoldStarryGlassBlock, TileAnimatedType.Passive },
        { TileID.BlueStarryGlassBlock, TileAnimatedType.Passive },
        { TileID.VoidMonolith, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.Cattail, TileAnimatedType.SpecialPoint },
        { TileID.SeaOats, TileAnimatedType.SpecialPoint },
        { TileID.OasisPlants, TileAnimatedType.SpecialPoint },
        { TileID.KryptonMoss, TileAnimatedType.SpecialPoint },
        { TileID.XenonMoss, TileAnimatedType.SpecialPoint },
        { TileID.ArgonMoss, TileAnimatedType.SpecialPoint },
        { TileID.Seaweed, TileAnimatedType.SpecialPoint },
        { TileID.TurtleCage, TileAnimatedType.Passive },
        { TileID.TurtleJungleCage, TileAnimatedType.Passive },
        { TileID.GrebeCage, TileAnimatedType.Passive },
        { TileID.SeagullCage, TileAnimatedType.Passive },
        { TileID.WaterStriderCage, TileAnimatedType.Passive },
        { TileID.GoldWaterStriderCage, TileAnimatedType.Passive },
        { TileID.SeahorseCage, TileAnimatedType.Passive },
        { TileID.GoldSeahorseCage, TileAnimatedType.Passive },
        { TileID.PlasmaLamp, TileAnimatedType.Passive },
        { TileID.FogMachine, TileAnimatedType.Passive },
        { TileID.PinkFairyJar, TileAnimatedType.Passive },
        { TileID.GreenFairyJar, TileAnimatedType.Passive },
        { TileID.BlueFairyJar, TileAnimatedType.Passive },
        { TileID.SoulBottles, TileAnimatedType.Passive },
        { TileID.RockGolemHead, TileAnimatedType.Passive },
        { TileID.HellButterflyJar, TileAnimatedType.Passive },
        { TileID.LavaflyinaBottle, TileAnimatedType.Passive },
        { TileID.MagmaSnailCage, TileAnimatedType.Passive },
        { TileID.GemSaplings, TileAnimatedType.SpecialPoint },
        { TileID.BrazierSuspended, TileAnimatedType.Passive },
        { TileID.VolcanoSmall, TileAnimatedType.Passive },
        { TileID.VolcanoLarge, TileAnimatedType.Passive },
        { TileID.VanityTreeSakuraSaplings, TileAnimatedType.SpecialPoint },
        { TileID.TeleportationPylon, TileAnimatedType.SpecialPoint },
        { TileID.LavafishBowl, TileAnimatedType.Passive },
        { TileID.AmethystBunnyCage, TileAnimatedType.Passive },
        { TileID.TopazBunnyCage, TileAnimatedType.Passive },
        { TileID.SapphireBunnyCage, TileAnimatedType.Passive },
        { TileID.EmeraldBunnyCage, TileAnimatedType.Passive },
        { TileID.RubyBunnyCage, TileAnimatedType.Passive },
        { TileID.DiamondBunnyCage, TileAnimatedType.Passive },
        { TileID.AmberBunnyCage, TileAnimatedType.Passive },
        { TileID.AmethystSquirrelCage, TileAnimatedType.Passive },
        { TileID.TopazSquirrelCage, TileAnimatedType.Passive },
        { TileID.SapphireSquirrelCage, TileAnimatedType.Passive },
        { TileID.EmeraldSquirrelCage, TileAnimatedType.Passive },
        { TileID.RubySquirrelCage, TileAnimatedType.Passive },
        { TileID.DiamondSquirrelCage, TileAnimatedType.Passive },
        { TileID.AmberSquirrelCage, TileAnimatedType.Passive },
        { TileID.PottedLavaPlantTendrils, TileAnimatedType.Passive },
        { TileID.VanityTreeWillowSaplings, TileAnimatedType.SpecialPoint },
        { TileID.MasterTrophyBase, TileAnimatedType.SpecialPoint },
        { TileID.TruffleWormCage, TileAnimatedType.Passive },
        { TileID.EmpressButterflyJar, TileAnimatedType.Passive },
        { TileID.VioletMoss, TileAnimatedType.Passive },
        { TileID.RainbowMoss, TileAnimatedType.Passive },
        { TileID.StinkbugCage, TileAnimatedType.Passive },
        { TileID.ScarletMacawCage, TileAnimatedType.Passive },
        { TileID.CorruptVines, TileAnimatedType.SpecialPoint },
        { TileID.AshPlants, TileAnimatedType.SpecialPoint },
        { TileID.AshVines, TileAnimatedType.SpecialPoint },
        { TileID.ManaCrystal, TileAnimatedType.Passive },
        { TileID.BlueMacawCage, TileAnimatedType.Passive },
        { TileID.ChlorophyteExtractinator, TileAnimatedType.Passive },
        { TileID.ToucanCage, TileAnimatedType.Passive },
        { TileID.YellowCockatielCage, TileAnimatedType.Passive },
        { TileID.GrayCockatielCage, TileAnimatedType.Passive },
        { TileID.ShadowCandle, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.EchoMonolith, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.ShimmerMonolith, TileAnimatedType.Passive | TileAnimatedType.Interactable },
        { TileID.ShimmerflyinaBottle, TileAnimatedType.Passive },
        { TileID.Moondial, TileAnimatedType.Interactable },
        { TileID.LifeCrystalBoulder, TileAnimatedType.Passive },
        { TileID.RainbowMossBrick, TileAnimatedType.Passive },
    };

    private static readonly Dictionary<int, TileAnimatedType> vanilla_walls = new()
    {
        { WallID.Waterfall,           TileAnimatedType.Passive },
        { WallID.Lavafall,            TileAnimatedType.Passive },
        { WallID.ArcaneRunes,         TileAnimatedType.Passive },
        { WallID.Confetti,            TileAnimatedType.Passive },
        { WallID.ConfettiBlack,       TileAnimatedType.Passive },
        { WallID.Honeyfall,           TileAnimatedType.Passive },
        { WallID.CogWall,             TileAnimatedType.Passive },
        { WallID.SandFall,            TileAnimatedType.Passive },
        { WallID.SnowFall,            TileAnimatedType.Passive },
        { WallID.GoldStarryGlassWall, TileAnimatedType.Passive },
        { WallID.BlueStarryGlassWall, TileAnimatedType.Passive },
    };

    private static readonly Dictionary<int, TileAnimatedType> tiles = new();
    private static readonly Dictionary<int, TileAnimatedType> walls = new();

    public override void Load()
    {
        base.Load();

        foreach (KeyValuePair<int, TileAnimatedType> pair in vanilla_tiles)
        {
            tiles.Add(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<int, TileAnimatedType> pair in vanilla_walls)
        {
            walls.Add(pair.Key, pair.Value);
        }
    }

    public override void PostSetupContent()
    {
        base.PostSetupContent();

        foreach (ModTile tile in TileLoader.tiles)
        {
            // PreDraw
            // PostDraw
            // AnimationFrameHeight > 0
            // IsDoor
            // IsTileDangerous
            // IsTileBiomeSightable
            // IsTileSpelunkable
            // SetSpriteEffects
            // SetDrawPositions
            // AnimateTile
            // AnimateIndividualTile
            // DrawEffects
            // TileFrame
            // RightClick
            // MouseOver
            // MouseOverFar
            // IsLockedChest
            // UnlockChest
            // LockChest

            Type tileType = tile.GetType();
            bool animated = false;

            animated |= IsMethodOverridden(tileType, nameof(tile.PreDraw));
            animated |= IsMethodOverridden(tileType, nameof(tile.PostDraw));
            animated |= tile.AnimationFrameHeight > 0;
            animated |= IsMethodOverridden(tileType, "get_" + nameof(tile.AnimationFrameHeight));
            animated |= IsMethodOverridden(tileType, "set_" + nameof(tile.AnimationFrameHeight));
            animated |= tile.IsDoor;
            animated |= IsMethodOverridden(tileType, nameof(tile.IsTileDangerous));
            animated |= IsMethodOverridden(tileType, nameof(tile.IsTileBiomeSightable));
            animated |= IsMethodOverridden(tileType, nameof(tile.IsTileSpelunkable));
            animated |= IsMethodOverridden(tileType, nameof(tile.SetSpriteEffects));
            animated |= IsMethodOverridden(tileType, nameof(tile.SetDrawPositions));
            animated |= IsMethodOverridden(tileType, nameof(tile.AnimateTile));
            animated |= IsMethodOverridden(tileType, nameof(tile.AnimateIndividualTile));
            animated |= IsMethodOverridden(tileType, nameof(tile.DrawEffects));
            animated |= IsMethodOverridden(tileType, nameof(tile.TileFrame));
            animated |= IsMethodOverridden(tileType, nameof(tile.RightClick));
            animated |= IsMethodOverridden(tileType, nameof(tile.MouseOver));
            animated |= IsMethodOverridden(tileType, nameof(tile.MouseOverFar));
            animated |= IsMethodOverridden(tileType, nameof(tile.IsLockedChest));
            animated |= IsMethodOverridden(tileType, nameof(tile.UnlockChest));
            animated |= IsMethodOverridden(tileType, nameof(tile.LockChest));

            if (!animated)
            {
                continue;
            }

            if (!tiles.ContainsKey(tile.Type))
            {
                tiles.Add(tile.Type, TileAnimatedType.ModdedAutoDetect);
            }
            else
            {
                tiles[tile.Type] |= TileAnimatedType.ModdedAutoDetect;
            }
        }

        foreach (ModWall wall in WallLoader.walls)
        {
            // PreDraw
            // PostDraw
            // AnimateWall
            // WallFrame

            Type wallType = wall.GetType();
            bool animated = false;

            animated |= IsMethodOverridden(wallType, nameof(wall.PreDraw));
            animated |= IsMethodOverridden(wallType, nameof(wall.PostDraw));
            animated |= IsMethodOverridden(wallType, nameof(wall.AnimateWall));
            animated |= IsMethodOverridden(wallType, nameof(wall.WallFrame));

            if (!animated)
            {
                continue;
            }

            if (!walls.ContainsKey(wall.Type))
            {
                walls.Add(wall.Type, TileAnimatedType.ModdedAutoDetect);
            }
            else
            {
                walls[wall.Type] |= TileAnimatedType.ModdedAutoDetect;
            }
        }
    }
    
    public static void RegisterTile(int tileId, TileAnimatedType type)
    {
        if (!tiles.ContainsKey(tileId))
        {
            tiles.Add(tileId, type);
        }
        else
        {
            tiles[tileId] |= type;
        }
    }
    
    public static void RegisterWall(int wallId, TileAnimatedType type)
    {
        if (!walls.ContainsKey(wallId))
        {
            walls.Add(wallId, type);
        }
        else
        {
            walls[wallId] |= type;
        }
    }

    // Also keep DontDrawTileSliced in mind?
    public static bool IsTilePossiblyAnimated(int tileId) => tiles.ContainsKey(tileId) ||
                                                             ShouldSwayInWind(tileId) ||
                                                             TileID.Sets.GetsCheckedForLeaves[tileId] ||
                                                             TileID.Sets.CommonSapling[tileId] ||
                                                             TileID.Sets.IsVine[tileId] ||
                                                             TileID.Sets.VineThreads[tileId] ||
                                                             TileID.Sets.ReverseVineThreads[tileId] ||
                                                             TileID.Sets.BasicChest[tileId] ||
                                                             TileID.Sets.BasicChestFake[tileId] ||
                                                             TileID.Sets.Torch[tileId] ||
                                                             TileID.Sets.Campfire[tileId] ||
                                                             TileID.Sets.TreeSapling[tileId] ||
                                                             IsTileDynamicallyAnimated(tileId);

    // Main::tileShine2?
    private static bool IsTileDynamicallyAnimated(int tileId) => TileID.Sets.CorruptBiomeSight[tileId] || TileID.Sets.CrimsonBiomeSight[tileId] || TileID.Sets.HallowBiomeSight[tileId] || Main.tileSpelunker[tileId] || IsTileDangerous(tileId);

    private static bool IsTileDangerous(int tileId) => TileID.Sets.Boulders[tileId] || /*Minecart.IsPressurePlate()*/ tileId == TileID.MinecartTrack ||
                                                       tileId == TileID.CrispyHoneyBlock ||
                                                       tileId == TileID.Cactus ||
                                                       tileId == 32 ||
                                                       tileId == 69 ||
                                                       tileId == 48 ||
                                                       tileId == 232 ||
                                                       tileId == 352 ||
                                                       tileId == 483 ||
                                                       tileId == 482 ||
                                                       tileId == 481 ||
                                                       tileId == 51 ||
                                                       tileId == 229 ||
                                                       tileId == 37 ||
                                                       tileId == 58 ||
                                                       tileId == 76 ||
                                                       tileId == 162;

    public static bool IsWallPossiblyAnimated(int tileId) => walls.ContainsKey(tileId);

    // ReSharper disable once SuggestBaseTypeForParameter
    private static bool IsMethodOverridden(Type type, string methodName)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.Name == methodName))
        {
            if (method.GetBaseDefinition().DeclaringType != method.DeclaringType)
            {
                return true;
            }
        }

        return false;
    }

    /*Main.SettingsEnabled_TilesSwayInWind &&*/
    private static bool ShouldSwayInWind(int tileId) => TileID.Sets.SwaysInWindBasic[tileId] /*&& tileId != 227*/;
}