﻿using System;
using System.Text;

using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using GenericModConfigMenu;
using Helpers;
using HarmonyLib;
using StardewValley.GameData;
using StardewValley.Extensions;
using StardewValley.Monsters;

namespace EasierMonsterEradication
{
    public class ModEntry : Mod
    {
        public const float MinPercent = 0.2f;
        public const float MaxPercent = 1.5f;

        public static ModConfig Config;

        internal static ModEntry Instance;
        internal static IModHelper MyHelper;
        internal static Logger Log;

        internal bool Debug;

        public String I18nGet(String str)
        {
            return MyHelper.Translation.Get(str);
        }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            MyHelper = helper;
            Log = new Logger(this.Monitor);

            MyHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
            MyHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            MyHelper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            MyHelper.Events.Content.AssetRequested += OnAssetRequested;
        }

        internal void ChangeMonsterPercent(float value)
        {
            Config.MonsterPercentage = value;
            MyHelper.GameContent.InvalidateCache("Data\\MonsterSlayerQuests");
        }

        /// <summary>Raised after the game has loaded and all Mods are loaded. Here we load the config.json file and setup GMCM </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Config = MyHelper.ReadConfig<ModConfig>();
            if (Config.MonsterPercentage < MinPercent)
                Config.MonsterPercentage = MinPercent;
            else if (Config.MonsterPercentage > MaxPercent)
                Config.MonsterPercentage = MaxPercent;

            // use GMCM in an optional manner.

            var gmcm = Helper.ModRegistry.GetGenericModConfigMenuApi(this.Monitor);
            if (gmcm != null)
            {
                gmcm.Register(ModManifest,
                              reset: () => Config = new ModConfig(),
                              save: () => Helper.WriteConfig(Config),
                              titleScreenOnly:false);

                //gmcm.AddBoolOption(ModManifest,
                //                   () => Config.xxx,
                //                   (bool value) => Config.xxx = value,
                //                   () => I18nGet("config.Label"),
                //                   () => I18nGet("config.Tooltip"));
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.MonsterPercentage,
                                     (float value) => ChangeMonsterPercent(value),
                                     () => I18nGet("monsterPercent.Label"),
                                     () => I18nGet("monsterPercent.tooltip"),
                                     min: MinPercent,
                                     max: MaxPercent,
                                     interval: 0.05f);

                gmcm.AddParagraph(ModManifest, () => I18nGet("VanillaMonsters"));
            }
            else
            {
                Log.LogOnce("Generic Mod Config Menu not available.");
            };

            Debug = Config.Debug;
#if DEBUG
            Debug = true;
#endif

        }


        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/MonsterSlayerQuests"))
            {
                e.Edit(asset =>
                {
                    var quests = asset.AsDictionary<string, MonsterSlayerQuestData>();
                    foreach (KeyValuePair<string, MonsterSlayerQuestData> kvp in quests.Data)
                    {
                        quests.Data[kvp.Key].Count = (int)((float)quests.Data[kvp.Key].Count * Config.MonsterPercentage);
                    }
                }
                );
            };
        }

        /// <summary>Raised after a game save is loaded. Here we hook into necessary events for gameplay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            MyHelper.GameContent.InvalidateCache("Data\\MonsterSlayerQuests");
            if (Debug)
                MyHelper.Events.Input.ButtonPressed += Input_ButtonPressed;
        }

        /// <summary>Raised after a game has exited a game/save to the title screen.  Here we unhook our gameplay events.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            if (Debug)
                MyHelper.Events.Input.ButtonPressed -= Input_ButtonPressed;
        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Context.IsPlayerFree)
            {
                if (Debug)
                {
                    var stats = Game1.player.stats;

                    if (e.Button == SButton.F6) //set monsters just below threshold
                    {
                        foreach (MonsterSlayerQuestData questData in DataLoader.MonsterSlayerQuests(Game1.content).Values)
                        {
                            if (questData.Targets != null)
                            {
                                //set the monster targets just below the threshold
                                foreach (string targetType in questData.Targets)
                                {
                                    if (!stats.specificMonstersKilled.ContainsKey(targetType))
                                        stats.specificMonstersKilled.Add(targetType, 0);
                                    stats.specificMonstersKilled[targetType] = (questData.Count / questData.Targets.Count) - 1;
                                }
                            }
                        }
                    }
                    else if (e.Button == SButton.F7) //complete one monster slayer goal
                    {
                        foreach (MonsterSlayerQuestData questData in DataLoader.MonsterSlayerQuests(Game1.content).Values)
                        {
                            if (questData.Targets != null)
                            {
                                int needed = questData.Count;

                                // make sure the first monster target exists
                                if (!stats.specificMonstersKilled.ContainsKey(questData.Targets[0]))
                                    stats.specificMonstersKilled.Add(questData.Targets[0], 0);

                                int killed = 0;
                                foreach (string targetType in questData.Targets)
                                {
                                    killed += Game1.stats.getMonstersKilled(targetType);
                                }
                                if (killed < needed)
                                {
                                    do
                                    {
                                        killed++;
                                        stats.monsterKilled(questData.Targets[0]);
                                    }
                                    while (killed < needed);
                                    return;
                                }
                            }

                        }
                    }
                }
            }
        }
    }
}

