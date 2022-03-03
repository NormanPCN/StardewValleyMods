using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using StardewValley.BellsAndWhistles;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace BetterButterflyHutch
{
    public class ModEntry : Mod
    {
        public static ModEntry Instance;
        public static ModConfig Config;
        public const int MaxMaxButterflies = 64;
        public const int MaxMinButterflies = MaxMaxButterflies;
        public const int MinBatWings = 10;
        public const int MaxBatWings = 200;
        public const int HutchIdx = 1971;

        private Random Rand;

        internal IModHelper MyHelper;

        public String I18nGet(String str)
        {
            return MyHelper.Translation.Get(str);
        }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            MyHelper = helper;
            Instance = this;

            MyHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
            MyHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            MyHelper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            Rand = new Random(DateTime.Now.Millisecond);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static void NormalizeMinMax()
        {
            if (Config.MinIndoors > Config.MaxIndoors)
                Config.MaxIndoors = Config.MinIndoors;
            if (Config.MinOutdoors > Config.MaxOutdoors)
                Config.MaxOutdoors = Config.MinOutdoors;
        }

        /// <summary>Raised after the game has loaded and all Mods are loaded. Here we load the config.json file and setup GMCM </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Config = MyHelper.ReadConfig<ModConfig>();
            Config.MinIndoors = Clamp(Config.MinIndoors, 0, MaxMinButterflies);
            Config.MaxIndoors = Clamp(Config.MaxIndoors, 1, MaxMaxButterflies);
            Config.MinOutdoors = Clamp(Config.MinOutdoors, 0, MaxMinButterflies);
            Config.MaxOutdoors = Clamp(Config.MaxOutdoors, 1, MaxMaxButterflies);
            NormalizeMinMax();
            Config.NumBatWings = Math.Min(MaxBatWings, Math.Max(MinBatWings, Config.NumBatWings));
#if DEBUG
            Config.Debug = true;
#endif

            var harmony = new Harmony(this.ModManifest.UniqueID);
            //harmony.PatchAll();
            System.Reflection.MethodInfo mInfo;

            // must use a patch to perform this action.
            mInfo = harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Locations.Desert), nameof(StardewValley.Locations.Desert.getDesertMerchantTradeStock)),
                                  postfix: new HarmonyMethod(typeof(DesertTraderPatches), nameof(DesertTraderPatches.getDesertMerchantTradeStock_Postfix))
                                 );

            if (Config.UseHarmony)
            {
                try
                {
                    mInfo = harmony.Patch(original: AccessTools.Method(typeof(StardewValley.Objects.Furniture), nameof(StardewValley.Objects.Furniture.resetOnPlayerEntry)),
                                          prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(FurniturePatches.resetOnPlayerEntry_Prefix)),
                                          postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(FurniturePatches.resetOnPlayerEntry_Postfix))
                                         );
                }
                catch (Exception ex)
                {
                    ModEntry.Instance.Monitor.Log($"Failed Harmony Furniture Patches:\n{ex}", LogLevel.Error);
                    Config.UseHarmony = false;
                }
            }

            // use GMCM in an optional manner.

            //IGenericModConfigMenuApi gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            var gmcm = Helper.ModRegistry.GetGenericModConfigMenuApi(this.Monitor);
            if (gmcm != null)
            {
                gmcm.Register(ModManifest,
                              reset: () => Config = new ModConfig(),
                              save: () => Helper.WriteConfig(Config));

                gmcm.AddNumberOption(ModManifest,
                                     () => Config.MinIndoors,
                                     (int value) => { Config.MinIndoors = value; NormalizeMinMax(); },
                                     () => I18nGet("minIndoors.Label"),
                                     () => I18nGet("minIndoors.Tooltip"),
                                     min: 0,
                                     max: MaxMinButterflies);
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.MaxIndoors,
                                     (int value) => { Config.MaxIndoors = value; NormalizeMinMax(); },
                                     () => I18nGet("maxIndoors.Label"),
                                     () => I18nGet("maxIntdoors.Tooltip"),
                                     min: 1,
                                     max: MaxMaxButterflies);
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.MinOutdoors,
                                     (int value) => { Config.MinOutdoors = value; NormalizeMinMax(); },
                                     () => I18nGet("minOutdoors.Label"),
                                     () => I18nGet("minOutdoors.Tooltip"),
                                     min: 0,
                                     max: MaxMinButterflies);
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.MaxOutdoors,
                                     (int value) => { Config.MaxOutdoors = value; NormalizeMinMax(); },
                                     () => I18nGet("maxOutdoors.Label"),
                                     () => I18nGet("maxOutdoors.Tooltip"),
                                     min: 1,
                                     max: MaxMaxButterflies);
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.NumBatWings,
                                     (int value) => Config.NumBatWings = value,
                                     () => I18nGet("numBatWings.Label"),
                                     () => I18nGet("numBatWings.Tooltip"),
                                     min: MinBatWings,
                                     max: MaxBatWings,
                                     interval: 10);
                gmcm.AddBoolOption(ModManifest,
                                   () => Config.WinterButterflies,
                                   (bool value) => Config.WinterButterflies = value,
                                   () => I18nGet("winterButterflies.Label"),
                                   () => I18nGet("winterButterflies.Tooltip"));
            }
            else
            {
                Monitor.LogOnce("Generic Mod Config Menu not available.", LogLevel.Info);
            };
        }

        /// <summary>Raised after a game save is loaded. Here we hook into necessary events for gameplay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Config.UseHarmony)
                MyHelper.Events.Player.Warped += Player_Warped;
        }

        /// <summary>Raised after a game has exited a game/save to the title screen.  Here we unhook our gameplay events.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            if (!Config.UseHarmony)
                MyHelper.Events.Player.Warped -= Player_Warped;
        }

        internal static int CountButterflies(GameLocation loc)
        {
            int count = 0;
            if (loc.critters != null)
            {
                for (int i = 0; i < loc.critters.Count; i++)
                {
                    if (loc.critters[i] is Butterfly)
                        count++;
                }
            }
            return count;
        }

        internal static void SpawnButterflies(GameLocation loc, int hutchCount, Rectangle? boundingBox)
        {
            // if the hutch did not spawn anything, then we will not
            if (hutchCount > 0)
            {
                bool island = loc.GetLocationContext() == StardewValley.GameLocation.LocationContext.Island;
                int min = 0;
                int max = 0;

                if (!loc.IsOutdoors)
                {
                    if (Config.MinIndoors > 0)
                    {
                        if (hutchCount < Config.MinIndoors)
                            min = Config.MinIndoors - hutchCount;
                        max = Config.MaxIndoors - hutchCount;
                    }
                }
                else
                {
                    // the game hutch code can spawn butterfies in the rain or snow or wind debris. i don't do that.
                    // don't spawn in winter in the valley. desert and island okay.
                    // Is...Here returns true for the Desert when it is raining/etc in sdv, because the Desert LocationContext is the same as sdv.
                    // really only two contexts. sdv and island.

                    bool desert = loc.Name.Equals("Desert", StringComparison.Ordinal);
                    bool isClear = !(Game1.IsRainingHere(loc) || Game1.IsLightningHere(loc) || Game1.IsSnowingHere(loc) || Game1.IsDebrisWeatherHere(loc));
                    bool spawn = island || desert || (!Game1.currentSeason.Equals("winter", StringComparison.Ordinal) || Config.WinterButterflies);
                    spawn = spawn && (isClear || desert);
                    spawn = spawn && !Game1.isDarkOut();

                    if (Config.MinOutdoors > 0)
                    {
                        if (spawn)
                        {
                            if (hutchCount < Config.MinOutdoors)
                                min = Config.MinOutdoors - hutchCount;
                            max = Config.MaxOutdoors - hutchCount;
                        }
                        else
                        {
                            // remove hutch spawned butterflies in instances we think they should not spawn
                            // the game will not spawn ambient butterfies in these conditions.
                            // so just remove all butterflies

                            if (Config.Debug)
                                Instance.Monitor.Log($"Remove Butterflies. critters={loc.critters.Count}", LogLevel.Debug);
                            for (int i = loc.critters.Count - 1; i >= 0; i--)
                            {
                                if (loc.critters[i] is Butterfly)
                                {
                                    if (Config.Debug)
                                        Instance.Monitor.Log($"    Remove Butterfly idx={i}", LogLevel.Debug);
                                    loc.critters.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
                max = Math.Max(min, max);// possible for max to go <= 0 if the game spawns a ton of butterflies

                if (max > 0)
                {
                    int spawn = Instance.Rand.Next(min, max + 1);//result always < upper value.
                    if (Config.Debug)
                    {
                        int x = -1;
                        int y = -1;
                        if (boundingBox.HasValue)
                        {
                            x = boundingBox.Value.X / Game1.tileSize;
                            y = boundingBox.Value.Y / Game1.tileSize;
                        }
                        Instance.Monitor.Log($"Butterfly spawns={spawn}, hutchSpawned={hutchCount}, HutchAt={x},{y}", LogLevel.Debug);
                    }

                    for (int i = 0; i < spawn; i++)
                    {
                        loc.addCritter(new Butterfly(loc.getRandomTile(), island).setStayInbounds(stayInbounds: true));
                    }
                }
            }
            //else
            //{
            //    if (Config.Debug)
            //        Instance.Monitor.Log("Hutch did not spawn butterflies", LogLevel.Debug);
            //}
        }

        /// <summary>Raised just after the player changes location. Here we spawn our Butterflies.
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (Context.IsPlayerFree)
            {
                GameLocation loc = e.NewLocation;

                foreach (StardewValley.Object obj in loc.furniture)
                {
                    if (obj.ParentSheetIndex == HutchIdx)//butterfly hutch
                    {
                        // we can't distinguish from ambient and hutch spawns. only matters outdoors.
                        int count = CountButterflies(loc);
                        if (Config.Debug)
                            Monitor.Log($"Found Hutch at {loc.Name}, Outdoors={loc.IsOutdoors}, Game Butterflies={count}", LogLevel.Debug);

                        SpawnButterflies(loc, count, null);
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StardewValley.Objects.Furniture))]
        public class FurniturePatches
        {
            private static int Before;

            [HarmonyPrefix]
            [HarmonyPatch(nameof(StardewValley.Objects.Furniture.resetOnPlayerEntry))]
            [HarmonyPatch(new Type[] { typeof(GameLocation), typeof(bool) })]
            public static bool resetOnPlayerEntry_Prefix(StardewValley.Objects.Furniture __instance, GameLocation environment, bool dropDown)
            {
                try
                {
                    if ((__instance.ParentSheetIndex == HutchIdx) && !dropDown)
                        Before = CountButterflies(environment);
                    return true;
                }
                catch (Exception e)
                {
                    ModEntry.Instance.Monitor.Log($"Failed in {nameof(resetOnPlayerEntry_Prefix)}:\n{e}", LogLevel.Error);
                    return true;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(StardewValley.Objects.Furniture.resetOnPlayerEntry))]
            [HarmonyPatch(new Type[] { typeof(GameLocation), typeof(bool) })]
            public static void resetOnPlayerEntry_Postfix(StardewValley.Objects.Furniture __instance, GameLocation environment, bool dropDown)
            {
                try
                {
                    if ((__instance.ParentSheetIndex == HutchIdx) && !dropDown)
                        SpawnButterflies(environment, CountButterflies(environment) - Before, __instance.boundingBox.Value);
                }
                catch (Exception e)
                {
                    ModEntry.Instance.Monitor.Log($"Failed in {nameof(resetOnPlayerEntry_Postfix)}:\n{e}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(StardewValley.Locations.Desert))]
        public class DesertTraderPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(StardewValley.Locations.Desert.getDesertMerchantTradeStock))]
            [HarmonyPatch(new Type[] { typeof(Farmer) })]
            public static void getDesertMerchantTradeStock_Postfix(StardewValley.Locations.Desert __instance, ref Dictionary<ISalable, int[]> __result, Farmer who)
            {
                try
                {
                    foreach (var item in __result)
                    {
                        if (item.Key.Name.Equals("Butterfly Hutch", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Value[StardewValley.Menus.ShopMenu.extraTradeItemCountIndex] = Config.NumBatWings;
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Instance.Monitor.Log($"Failed in {nameof(getDesertMerchantTradeStock_Postfix)}:\n{e}", LogLevel.Error);
                }
            }
        }
    }
}

