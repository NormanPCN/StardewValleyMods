﻿using HarmonyLib;
//using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.GameData.Shops;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ShopMenuFilter
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry : Mod
    {

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;

        public static ModEntry context;

        public static bool accelerating;
        //private static Texture2D boardTexture;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            if (!Config.ModEnabled)
                return;

            context = this;

            SMonitor = Monitor;
            SHelper = helper;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

            var harmony = new Harmony(ModManifest.UniqueID);

            // postfix only uses the instance as a parameter so it is safe to iterate regardless of the types
            foreach (ConstructorInfo constructor in AccessTools.GetDeclaredConstructors(typeof(ShopMenu)))
            {
                harmony.Patch(
                    original: constructor,
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(Shopmenu_Constructor_Postfix)));
            }

            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), nameof(ShopMenu.applyTab)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(applyTab_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), nameof(ShopMenu.gameWindowSizeChanged)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(gameWindowSizeChanged_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), nameof(ShopMenu.drawCurrency)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(drawCurrency_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), nameof(ShopMenu.receiveLeftClick)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(receiveLeftClick_Postfix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), nameof(ShopMenu.receiveKeyPress)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(receiveKeyPress_Prefix)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ShopMenu), nameof(ShopMenu.performHoverAction)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(performHoverAction_Postfix)));
        }


        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => ModEntry.SHelper.Translation.Get("GMCM_Option_ModEnabled_Name"),
                getValue: () => Config.ModEnabled,
                setValue: value => Config.ModEnabled = value
            );
        }
    }
}