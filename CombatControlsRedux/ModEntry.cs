#undef FacingDirectionPostfix

using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using GenericModConfigMenu;

/*
    This is an adaption of the work of Dj-Stalin (DJ-STLN), who created the original Combat Controls Mod.
    That mod is available at https://www.nexusmods.com/stardewvalley/mods/2590
    As source was not available, the original Mod was decompiled.
    The NexusMods page lists permission as
    "You are allowed to modify my files and release bug fixes or improve on the features so long as you credit me as the original creator"

    This source changes most things from the original but the core functions of the original Mod are unchanged. "how it works".
        the facing direction change logic.
        the slick moves velocity tweaks logic.

    Additions/changes to the original Mod
        implemented config file support.
          added Generic Mod Config menu support with i18n internationalization support.
        facing direction change (MouseFix) on melee weapons/tools only. (left-click, use tool button).
        facing direction change for dagger special attack. (right-click, action button).
        separate control for slick moves on sword and club.
        auto swing with separate control for sword/club and dagger.
        slick moves disabled for daggers and scythe. there were issues here anyway.
        added slick move velocity config settings.
        

    Combat Controls Redux is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License  
    as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    The Combat Controls Redux mod is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;  
    without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
    See the GNU General Public License for more details. <http://www.gnu.org/licenses/>

    I don't really care if you redistribute it or alter it or use it in compilations.  
    I'd ask that you give credit to myself (Norman Black, NormanPCN) and (Dj-Stalin, DJ-STLN), that's all.  
 */

namespace CombatControlsRedux
{
    public class ModEntry : Mod
    {
        public ModConfig Config;

        internal static ModEntry Instance;

        internal IModHelper MyHelper;
        //private IReflectedMethod PerformFireTool;

        private static ITranslationHelper i18n => Instance.MyHelper.Translation;

        private bool IsHoldingAttack;

#if FacingDirectionPostfix
        private int MyFacingDirection;
#endif

        private const int CountdownStart = 8;
        private const int CountdownRepeat = 4;
        private int TickCountdown;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            MyHelper = helper;

            //I18n.Init(helper.Translation);

            // original Mod: all config load and event hooking was done on entry. no removal.
            // entry config loads don't work when using GMCM optional. GMCM may not have been loaded yet.
            // added OnGameLaunched for config setup/load.
            // added OnSaveLoaded to insert input/update event hooks
            // added OnReturnedToTitle to remove the input/update event hooks.
            //     probably not necessary. i.e. WorldIsReady/PlayerIsFree. i just don't want events if a game is not running.
            // added OnUpdateTicked for facing direction change correction.
            

            MyHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
            MyHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            MyHelper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            //Monitor.Log($"MinGameVersion={Constants.MinimumGameVersion}, MaxGameVersion={Constants.MaximumGameVersion}", LogLevel.Debug);
        }

        /// <summary>Raised after a game save is loaded. Here we hook into necessary events for gameplay.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            TickCountdown = 0;
            IsHoldingAttack = false;
#if FacingDirectionPostfix
            MyFacingDirection = -1;
#endif
            //PerformFireTool = MyHelper.Reflection.GetMethod(Game1.player, "performFireTool");

            MyHelper.Events.Input.ButtonPressed += Input_ButtonPressed;
            MyHelper.Events.Input.ButtonReleased += Input_ButtonReleased;
            MyHelper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
#if FacingDirectionPostfix
            MyHelper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
#endif
        }

        /// <summary>Raised after a game has exited a game/save to the title screen.  Here we unhook our gameplay events.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            //PerformFireTool = null;

            MyHelper.Events.Input.ButtonPressed -= Input_ButtonPressed;
            MyHelper.Events.Input.ButtonReleased -= Input_ButtonReleased;
            MyHelper.Events.GameLoop.UpdateTicking -= GameLoop_UpdateTicking;
#if FacingDirectionPostfix
            MyHelper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked;
#endif
        }

        /// <summary>Raised after the game has loaded and all Mods are loaded. Here we load the config.json file and setup GMCM </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            const float minSlideVelocity = 2.0f;
            const float maxSlideVelocity = 10.0f;

            Config = MyHelper.ReadConfig<ModConfig>();

            // lets clamp the range of these values in case someone editing the config gives a value that may cause problems.

            Config.SlideVelocity = Math.Min(maxSlideVelocity, Math.Max(minSlideVelocity, Config.SlideVelocity));
            Config.SpecialSlideVelocity = Math.Min(maxSlideVelocity, Math.Max(minSlideVelocity, Config.SpecialSlideVelocity));

            // use GMCM in an optional manner.

            IGenericModConfigMenuApi gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm != null)
            {
                gmcm.Register(ModManifest,
                              reset: () => Config = new ModConfig(),
                              save: () => Helper.WriteConfig(Config));

                gmcm.AddBoolOption(ModManifest,
                                   () => Config.MouseFix,
                                   (bool value) => Config.MouseFix = value,
                                   () => i18n.Get("mouseFix.Label"),
                                   () => i18n.Get("mouseFix.Text"));
                gmcm.AddBoolOption(ModManifest,
                                   () => Config.AutoSwing,
                                   (bool value) => Config.AutoSwing = value,
                                   () => i18n.Get("autoSwing.Label"),
                                   () => i18n.Get("autoSwing.Text"));
                gmcm.AddBoolOption(ModManifest,
                                   () => Config.AutoSwingDagger,
                                   (bool value) => Config.AutoSwingDagger = value,
                                   () => i18n.Get("autoSwingDagger.Label"),
                                   () => i18n.Get("autoSwingDagger.Text"));
                gmcm.AddBoolOption(ModManifest,
                                   () => Config.SlickMoves,
                                   (bool value) => Config.SlickMoves = value,
                                   () => i18n.Get("slickMoves.Label"),
                                   () => i18n.Get("slickMoves.Text"));
                gmcm.AddBoolOption(ModManifest,
                                   () => Config.SwordSpecialSlickMove,
                                   (bool value) => Config.SwordSpecialSlickMove = value,
                                   () => i18n.Get("swordSpecial.Label"),
                                   () => i18n.Get("swordSpecial.Text"));
                gmcm.AddBoolOption(ModManifest,
                                   () => Config.ClubSpecialSlickMove,
                                   (bool value) => Config.ClubSpecialSlickMove = value,
                                   () => i18n.Get("clubSpecial.Label"),
                                   () => i18n.Get("clubSpecial.Text"));

//#if MyTest
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.SlideVelocity,
                                     (float value) => Config.SlideVelocity = value,
                                     () => i18n.Get("slideVelocity.Label"),
                                     () => i18n.Get("slideVelocity.Text"),
                                     min: minSlideVelocity,
                                     max: maxSlideVelocity,
                                     interval: 0.1f);
                gmcm.AddNumberOption(ModManifest,
                                     () => Config.SpecialSlideVelocity,
                                     (float value) => Config.SpecialSlideVelocity = value,
                                     () => i18n.Get("specSlideVelocity.Label"),
                                     () => i18n.Get("specSlideVelocity.Text"),
                                     min: minSlideVelocity,
                                     max: maxSlideVelocity,
                                     interval: 0.1f);
//#endif
            }
            else
            {
                Monitor.LogOnce("Generic Mod Config Menu not available.", LogLevel.Info);
            };
        }

        /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (SButtonExtensions.IsUseToolButton(e.Button)) // (e.Button == SButton.MouseLeft)
            {
                IsHoldingAttack = false;
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.
        /// This method implements the facing direction change and Slick moves of the Mod.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            Farmer who = Game1.player;
            bool useToolButtonPressed = SButtonExtensions.IsUseToolButton(e.Button);
            bool actionButtonPressed = SButtonExtensions.IsActionButton(e.Button);

            if (
                (useToolButtonPressed || actionButtonPressed) &&
                (who.CurrentTool != null) &&
                (who.CurrentTool is MeleeWeapon) &&
                Context.IsPlayerFree
               )
            {
                // note: the scythe identifies itself as a melee weapon

                if (useToolButtonPressed)
                {
                    IsHoldingAttack = true;
                    TickCountdown = CountdownStart;
                }

                if (Config.MouseFix)
                {

                    MeleeWeapon tool = who.CurrentTool as MeleeWeapon;
                    bool scythe = tool.isScythe();
                    bool dagger = (tool.type.Value == MeleeWeapon.dagger);
                    bool special = tool.isOnSpecial;
                    bool swordSpecial = special && (tool.type.Value == MeleeWeapon.defenseSword);
                    bool clubSpecial = special && (tool.type.Value == MeleeWeapon.club);

                    if (
                        useToolButtonPressed &&
                        (!dagger) &&
                        (!scythe) &&
                        (
                          ((!special) && Config.SlickMoves) ||
                          (swordSpecial && Config.SwordSpecialSlickMove) ||
                          (clubSpecial && Config.ClubSpecialSlickMove)
                        )
                       )
                    {
                        float newVelocity = (special ? Config.SpecialSlideVelocity : Config.SlideVelocity);

                        // diagonal movement returns an up/down/left/right.
                        // for now limit the velocity change to the cardinal directions. Count = 1
                        // it still seems to work okay on the diagonal, with single velocity adjustment.
                        // so maybe not limit

                        //Monitor.Log($".movementDirections.Count={who.movementDirections.Count}", LogLevel.Debug);
                        if (who.movementDirections.Count == 1)
                        {
                            //Monitor.Log($".xV={who.xVelocity}, .yV={who.yVelocity}", LogLevel.Debug);
                            switch (who.getDirection())
                            {
                            case Game1.left:
                                who.canMove = true;
                                who.xVelocity = 0f - newVelocity;
                                break;
                            case Game1.right:
                                who.canMove = true;
                                who.xVelocity = newVelocity;
                                break;
                            case Game1.up:
                                who.canMove = true;
                                who.yVelocity = newVelocity;
                                break;
                            case Game1.down:
                                who.canMove = true;
                                who.yVelocity = 0f - newVelocity;
                                break;
                            default:
                                break;
                            }
                        }
                    }

                    // change the player facing direction.

                    if (useToolButtonPressed || (dagger && actionButtonPressed))
                    {

                        // .Cursor.AbsolutePixels are map relative coords
                        //  who.GetBoundingBox().Center.X/Y, who.Position.X/Y
                        //  should I use BoundingBox center?
                        //Monitor.Log($"Pos.X,Y {(int) who.Position.X},{(int) who.Position.Y} " +
                        //                $"Center.X,Y {(int) who.GetBoundingBox().Center.X},{(int) who.GetBoundingBox().Center.Y} " +
                        //                $"Cursor.X,Y {(int) e.Cursor.AbsolutePixels.X},{(int) e.Cursor.AbsolutePixels.Y}",
                        //            LogLevel.Debug);
                        //float mouseDirectionX = e.Cursor.AbsolutePixels.X - who.Position.X;
                        //float mouseDirectionY = e.Cursor.AbsolutePixels.Y - who.Position.Y;
                        Microsoft.Xna.Framework.Point pos = who.GetBoundingBox().Center;
                        float mouseDirectionX = e.Cursor.AbsolutePixels.X - pos.X;
                        float mouseDirectionY = e.Cursor.AbsolutePixels.Y - pos.Y;
                        float mouseDirectionXpower = mouseDirectionX * mouseDirectionX;
                        float mouseDirectionYpower = mouseDirectionY * mouseDirectionY;

                        if (mouseDirectionXpower > mouseDirectionYpower)
                        {
                            if (mouseDirectionX < 0f)
                            {
                                who.FacingDirection = Game1.left;
#if FacingDirectionPostfix
                                MyFacingDirection = Game1.left;
#endif
                            }
                            else
                            {
                                who.FacingDirection = Game1.right;
#if FacingDirectionPostfix
                                MyFacingDirection = Game1.right;
#endif
                            }
                        }
                        else if (mouseDirectionY < 0f)
                        {
                            who.FacingDirection = Game1.up;
#if FacingDirectionPostfix
                            MyFacingDirection = Game1.up;
#endif
                        }
                        else
                        {
                            who.FacingDirection = Game1.down;
#if FacingDirectionPostfix
                            MyFacingDirection = Game1.down;
#endif
                        }
                    }
                }
            }
        }

        /// <summary>Raised when the game state is about to be updated (≈60 times per second).
        /// This method implements the Auto Swing feature of the mod.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void GameLoop_UpdateTicking(object sender, EventArgs e)
        {
            // Test IsHoldingAttack first. It is the main logic restricter here. IsPlayerFree has a bit of code.

            if (IsHoldingAttack && Context.IsPlayerFree)
            {
                Farmer who = Game1.player;

                // this auto swing code does not work for the Scythe.
                // I am good with that. I explicitly disable the scythe anyway.

                bool melee = who.CurrentTool is MeleeWeapon;
                bool scythe = melee && (who.CurrentTool as MeleeWeapon).isScythe();
                bool dagger = melee && ((who.CurrentTool as MeleeWeapon).type.Value == MeleeWeapon.dagger);

                if (melee && (!scythe) && ((Config.AutoSwing && !dagger) || (Config.AutoSwingDagger && dagger)))
                {
                    // spamming FireTool at every tick (60/s) seems excessive. at least to me.
                    // it seems to work with spams. don't know the overhead.
                    // i'll reduce to every N ticks. N must be small.
                    // too big a number and auto swing just does not work at all.
                    // the next fire may need to be set during a current fire/swing/something.
                    // even a little reduction seems somehow "nicer". what the heck.
                    if (TickCountdown > 0)
                    {
                        TickCountdown -= 1;
                    }
                    else
                    {
                        // which is "better" FireTool or (private internal) PerformFireTool
                        // Looking at the Stardew code, Perform seems to be the implementation of Fire for NetEvent.
                        // Farmer.FireTool is just a call to NetEvent.Fire
                        // Fire clearly has a bit over minor checking/overhead before calling the implementation.
                        // code that we should probably have execute.
                        // i've seen some auto swing code use performFireTool. so I wondered the diff.

                        who.FireTool();
                        //PerformFireTool.Invoke();
                        TickCountdown = CountdownRepeat;
                    }
                }
            }
        }


#if FacingDirectionPostfix
        /// <summary>Raised just after the game state is updated (≈60 times per second).
        /// This method implements facing direction change correction.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void GameLoop_UpdateTicked(object sender, EventArgs e)
        {
            if (MyFacingDirection >= 0)
            {
                int facing = MyFacingDirection;
                MyFacingDirection = -1;

                if ((facing >= 0) && (facing != Game1.player.FacingDirection))
                {
                    //the game changed the facing direction we selected. set it back.
                    //this disagreement only happens in the 8 tiles surrounding the player where the game code may set the facing direction.
                    //re-setting this here seems to work.
                    //this change may be happening on the next game tick.
                                        
                    //Monitor.Log($"FacingDirection different me={facing} game={Game1.player.FacingDirection}", LogLevel.Debug);
                    Game1.player.FacingDirection = facing;
                }
            }
        }
#endif
    }
}

