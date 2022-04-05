using System;
using System.Text;

using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using GenericModConfigMenu;
using Helpers;
using HarmonyLib;


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

        public struct MonsterRec
        {
            public string GroupName;
            public float KillsReq;//vanilla kill value
            public string RewardName;
            public string[] Monsters;

            public MonsterRec(string group, int killsReq, string rewardName, string[] monsterNames)
            {
                GroupName = group;
                KillsReq = killsReq;
                RewardName = rewardName;
                Monsters = monsterNames;
            }
        }

        private static MonsterRec[] MonstersTable = new MonsterRec[12]
        {
            new MonsterRec("Slimes",      1000, "Gil_Slime Charmer Ring",    new string[4] { "Green Slime", "Frost Jelly", "Sludge", "Tiger Slime" }),
            new MonsterRec("DustSprites", 500,  "Gil_Burglar's Ring",        new string[1] { "Dust Spirit" }),
            new MonsterRec("Bats",        200,  "Gil_Vampire Ring",          new string[4] { "Bat", "Frost Bat", "Lava Bat", "Iridium Bat" }),
            new MonsterRec("Serpent",     250,  "Gil_Napalm Ring",           new string[2] { "Serpent", "Royal Serpent" }),
            new MonsterRec("VoidSpirits", 150,  "Gil_Savage Ring",           new string[4] { "Shadow Guy", "Shadow Shaman", "Shadow Brute", "Shadow Sniper" }),
            new MonsterRec("MagmaSprite", 150,  "Gil_Telephone",             new string[2] { "Magma Sprite", "Magma Sparker" }),
            new MonsterRec("CaveInsects", 125,  "Gil_Insect Head",           new string[3] { "Grub", "Fly", "Bug" }),
            new MonsterRec("Mummies",     100,  "Gil_Arcane Hat",            new string[1] { "Mummy" }),
            new MonsterRec("RockCrabs",   60,   "Gil_Crabshell Ring",        new string[3] { "Rock Crab", "Lava Crab", "Iridium Crab" }),
            new MonsterRec("Skeletons",   50,   "Gil_Skeleton Mask",         new string[2] { "Skeleton", "Skeleton Mage" }),
            new MonsterRec("PepperRex",   50,   "Gil_Knight's Helmet",       new string[1] { "Pepper Rex" }),
            new MonsterRec("Duggies",     30,   "Gil_Hard Hat",              new string[2] { "Duggy", "Magma Duggy" })
        };

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
        }

        private string GetParagraphText()
        {
            //return MyHelper.Translation.Get("VanillaMonsters", new { slimes = MonstersTable[0].KillsReq.ToString() });
            return I18nGet("VanillaMonsters");
        }

        /// <summary>Raised after the game has loaded and all Mods are loaded. Here we load the config.json file and setup GMCM </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Config = MyHelper.ReadConfig<ModConfig>();
            if (Config.MonsterPercentage < MinPercent)
                Config.MonsterPercentage = MinPercent;
            if (Config.MonsterPercentage > MaxPercent)
                Config.MonsterPercentage = MaxPercent;

            // use GMCM in an optional manner.

            //IGenericModConfigMenuApi gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            var gmcm = Helper.ModRegistry.GetGenericModConfigMenuApi(this.Monitor);
            if (gmcm != null)
            {
                gmcm.Register(ModManifest,
                              reset: () => Config = new ModConfig(),
                              save: () => Helper.WriteConfig(Config),
                              titleScreenOnly:true);

                //gmcm.AddBoolOption(ModManifest,
                //                   () => Config.xxx,
                //                   (bool value) => Config.xxx = value,
                //                   () => I18nGet("config.Label"),
                //                   () => I18nGet("config.Tooltip"));
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.MonsterPercentage,
                                     (float value) => Config.MonsterPercentage = value,
                                     () => I18nGet("monsterPercent.Label"),
                                     () => I18nGet("monsterPercent.tooltip"),
                                     min: MinPercent,
                                     max: MaxPercent,
                                     interval: 0.1f);
                gmcm.AddParagraph(ModManifest,
                                  () => GetParagraphText());
            }
            else
            {
                Monitor.LogOnce("Generic Mod Config Menu not available.", LogLevel.Info);
            };

            Debug = Config.Debug;
#if DEBUG
            Debug = true;
#endif

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        /// <summary>Raised after a game save is loaded. Here we hook into necessary events for gameplay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
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
                    if (e.Button == SButton.F5) //set monsters just below threshold
                    {
                        var monstersData = MonstersTable;
                        for (int i = 0; i < monstersData.Length; i++)
                        {
                            var player = Game1.player;
                            var group = monstersData[i];

                            int killed = 0;
                            foreach (string monster in group.Monsters)
                            {
                                if (player.stats.specificMonstersKilled.TryGetValue(monster, out int thisKill))
                                {
                                    killed += thisKill;
                                }
                            }

                            foreach (string monster in group.Monsters)
                            {
                                if (player.stats.specificMonstersKilled.ContainsKey(monster))
                                {
                                    int needed = (int)(group.KillsReq * Config.MonsterPercentage);
                                    needed = needed - killed - 2;
                                    if (needed > 1)
                                        player.stats.specificMonstersKilled[monster] += needed;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool willThisKillCompleteAMonsterSlayerQuest(string nameOfMonster)
        {
            var monsters = MonstersTable;
            for (int i = 0; i < monsters.Length; i++)
            {
                var player = Game1.player;
                var group = monsters[i];
                foreach (string monster in group.Monsters)
                {
                    if (nameOfMonster.Equals(monster))
                    {
                        if (!Game1.player.mailReceived.Contains(group.RewardName))
                        {
                            int killed = 0;
                            foreach (string specificMonster in group.Monsters)
                            {
                                if (player.stats.specificMonstersKilled.TryGetValue(specificMonster, out int thisKill))
                                {
                                    killed += thisKill;
                                }
                            }
                            int needed = (int)(group.KillsReq * Config.MonsterPercentage);

                            if ((killed < needed) && (killed+1 >= needed))
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        private static bool areAllMonsterSlayerQuestsComplete()
        {
            var monsters = MonstersTable;
            for (int i = 0; i < monsters.Length; i++)
            {
                var player = Game1.player;
                var group = monsters[i];

                int killed = 0;
                foreach (string monster in group.Monsters)
                {
                    if (player.stats.specificMonstersKilled.TryGetValue(monster, out int thisKill))
                    {
                        killed += thisKill;
                    }
                }
                int needed = (int)(group.KillsReq * Config.MonsterPercentage);

                if (killed < needed)
                    return false;
            }
            return true;
        }

        private static string killListLine(string monsterType, int killCount, int target)
        {
            string monsterNamePlural = Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_" + monsterType);
            if (killCount == 0)
            {
                return Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_LineFormat_None", killCount, target, monsterNamePlural) + "^";
            }
            if (killCount >= target)
            {
                return Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_LineFormat_OverTarget", killCount, target, monsterNamePlural) + "^";
            }
            return Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_LineFormat", killCount, target, monsterNamePlural) + "^";
        }

        private static void showMonsterKillList()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (!Game1.player.mailReceived.Contains("checkedMonsterBoard"))
            {
                Game1.player.mailReceived.Add("checkedMonsterBoard");
            }

            stringBuilder.Append(Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_Header").Replace('\n', '^') + "^");

            var monsters = MonstersTable;
            for (int i = 0; i < monsters.Length; i++)
            {
                var player = Game1.player;
                var group = monsters[i];

                int killed = 0;
                foreach (string monster in group.Monsters)
                {
                    if (player.stats.specificMonstersKilled.TryGetValue(monster, out int thisKill))
                    {
                        killed += thisKill;
                    }
                }
                int needed = (int)(group.KillsReq * Config.MonsterPercentage);

                stringBuilder.Append(killListLine(group.GroupName, killed, needed));
            }

            stringBuilder.Append(Game1.content.LoadString("Strings\\Locations:AdventureGuild_KillList_Footer").Replace('\n', '^'));
            Game1.drawLetterMessage(stringBuilder.ToString());
        }

        public void onRewardCollected(Item item, Farmer who)
        {
            if (item != null && !who.hasOrWillReceiveMail("Gil_" + item.Name))
            {
                who.mailReceived.Add("Gil_" + item.Name);
            }
        }

        public void GillRewards(StardewValley.Locations.AdventureGuild __instance)
        {
            List<Item> rewards = new List<Item>();

            var monstersData = MonstersTable;
            for (int i = 0; i < monstersData.Length; i++)
            {
                var player = Game1.player;
                var group = monstersData[i];

                int killed = 0;
                foreach (string specificMonster in group.Monsters)
                {
                    if (player.stats.specificMonstersKilled.TryGetValue(specificMonster, out int thisKill))
                    {
                        killed += thisKill;
                    }
                }
                int needed = (int)(group.KillsReq * Config.MonsterPercentage);

                if ((killed >= needed) && !Game1.player.mailReceived.Contains(group.RewardName))
                {
                    if (group.GroupName.Equals("Slimes"))
                    {
                        rewards.Add(new StardewValley.Objects.Ring(520));
                    }
                    else if (group.GroupName.Equals("VoidSpirits"))
                    {
                        rewards.Add(new StardewValley.Objects.Ring(523));
                    }
                    else if (group.GroupName.Equals("Skeletons"))
                    {
                        rewards.Add(new StardewValley.Objects.Hat(8));
                    }
                    else if (group.GroupName.Equals("DustSprites"))
                    {
                        rewards.Add(new StardewValley.Objects.Ring(526));
                    }
                    else if (group.GroupName.Equals("Bats"))
                    {
                        rewards.Add(new StardewValley.Objects.Ring(522));
                    }
                    else if (group.GroupName.Equals("Serpent"))
                    {
                        rewards.Add(new StardewValley.Objects.Ring(811));
                    }
                    else if (group.GroupName.Equals("MagmaSprite"))
                    {
                        var gilNpc = MyHelper.Reflection.GetField<StardewValley.NPC>(__instance, "Gil").GetValue();
                        Game1.addMail("Gil_Telephone", noLetter: true, sendToEveryone: true);
                        Game1.drawDialogue(gilNpc, Game1.content.LoadString("Strings\\Locations:Gil_Telephone"));
                        return;
                    }
                    else if (group.GroupName.Equals("CaveInsects"))
                    {
                        rewards.Add(new StardewValley.Tools.MeleeWeapon(13));
                    }
                    else if (group.GroupName.Equals("Mummies"))
                    {
                        rewards.Add(new StardewValley.Objects.Hat(60));
                    }
                    else if (group.GroupName.Equals("RockCrabs"))
                    {
                        rewards.Add(new StardewValley.Objects.Ring(810));
                    }
                    else if (group.GroupName.Equals("PepperRex"))
                    {
                        rewards.Add(new StardewValley.Objects.Hat(50));
                    }
                    else if (group.GroupName.Equals("Duggies"))
                    {
                        rewards.Add(new StardewValley.Objects.Hat(27));
                    }
                }
            }

            foreach (Item i in rewards)
            {
                if (i is StardewValley.Object)
                {
                    (i as StardewValley.Object).specialItem = true;
                }
            }
            if (rewards.Count > 0)
            {
                Game1.activeClickableMenu = new StardewValley.Menus.ItemGrabMenu(rewards, __instance)
                {
                    behaviorOnItemGrab = onRewardCollected
                };
                return;
            }

            var gil = MyHelper.Reflection.GetField<StardewValley.NPC>(__instance, "Gil").GetValue();
            var talkedToGil = MyHelper.Reflection.GetField<bool>(__instance, "talkedToGil");
            if (talkedToGil.GetValue())
            {
                Game1.drawDialogue(gil, Game1.content.LoadString("Characters\\Dialogue\\Gil:Snoring"));
            }
            else
            {
                Game1.drawDialogue(gil, Game1.content.LoadString("Characters\\Dialogue\\Gil:ComeBackLater"));
            }
            talkedToGil.SetValue(true);
        }


        [HarmonyPatch(typeof(StardewValley.Locations.AdventureGuild))]
        public class AdventureGuildPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(StardewValley.Locations.AdventureGuild.willThisKillCompleteAMonsterSlayerQuest))]
            [HarmonyPatch(new Type[] { typeof(string) })]
            public static bool willThisKillCompleteAMonsterSlayerQuest_Prefix(StardewValley.Locations.AdventureGuild __instance, ref bool __result, string nameOfMonster)
            {
                try
                {
                    __result = willThisKillCompleteAMonsterSlayerQuest(nameOfMonster);
                    return false;
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Error($"Failed in {nameof(willThisKillCompleteAMonsterSlayerQuest_Prefix)}:\n{ex}");
                    return true;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(StardewValley.Locations.AdventureGuild.areAllMonsterSlayerQuestsComplete))]
            public static bool areAllMonsterSlayerQuestsComplete_Prefix(StardewValley.Locations.AdventureGuild __instance, ref bool __result)
            {
                try
                {
                    __result = areAllMonsterSlayerQuestsComplete();
                    return false;
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Error($"Failed in {nameof(areAllMonsterSlayerQuestsComplete_Prefix)}:\n{ex}");
                    return true;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(StardewValley.Locations.AdventureGuild.showMonsterKillList))]
            public static bool showMonsterKillList_Prefix(StardewValley.Locations.AdventureGuild __instance)
            {
                try
                {
                    showMonsterKillList();
                    return false;
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Error($"Failed in {nameof(showMonsterKillList_Prefix)}:\n{ex}");
                    return true;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("gil")] // private method
            public static bool gil_Prefix(StardewValley.Locations.AdventureGuild __instance)
            {
                try
                {
                    Instance.GillRewards(__instance);
                    return false;
                }
                catch (Exception ex)
                {
                    ModEntry.Log.Error($"Failed in {nameof(gil_Prefix)}:\n{ex}");
                    return true;
                }
            }

        }

    }
}

