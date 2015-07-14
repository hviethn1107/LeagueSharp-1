﻿namespace VHR_SDK
{
    #region
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Math.Prediction;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;
    using SharpDX;
    using Interfaces;
    using Modules;
    using Utility;
    using Utility.Helpers;
    using LeagueSharp.SDK.Core.UI.IMenu;
    #endregion

    class VHR
    {
        #region Variables and fields
        public static Menu VHRMenu { get; set; }

        public static Dictionary<SpellSlot, Spell> spells = new Dictionary<SpellSlot, Spell>()
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q) },
            { SpellSlot.W, new Spell(SpellSlot.W) },
            { SpellSlot.E, new Spell(SpellSlot.E, 590f) },
            { SpellSlot.R, new Spell(SpellSlot.R) }
        };

        private static Spell TrinketSpell = new Spell(SpellSlot.Trinket);

        public static List<IVHRModule> VhrModules = new List<IVHRModule>()
        {
            new TestModule()
        };
        #endregion

        #region Initialization, Public Methods and operators
        public static void OnLoad()
        {
            LoadSpells();
            LoadModules();
            LoadEvents();

            TickLimiter.Add("CondemnLimiter", 250);
            TickLimiter.Add("ModulesLimiter", 300);
            TickLimiter.Add("ComboLimiter", 80);
        }

        private static void LoadSpells()
        {
            spells[SpellSlot.E].SetTargetted(0.25f, 2000f);
        }

        private static void LoadEvents()
        {
            Orbwalker.OnAction += Orbwalker_OnAction;
        }

        private static void LoadModules()
        {
            foreach (var module in VhrModules.Where(module => module.ShouldBeLoaded()))
            {
                try
                {
                    module.OnLoad();
                }
                catch (Exception exception)
                {
                    VHRDebug.WriteError(string.Format("Failed to load module! Module name: {0} - Exception: {1} ", module.GetModuleName(), exception));
                }
            }
        }
        #endregion

        #region Event Delegates
        private static void Orbwalker_OnAction(object sender, Orbwalker.OrbwalkerActionArgs e)
        {
            switch (e.Type)
            {
                case OrbwalkerType.AfterAttack:
                    //AfterAttack Delegate. Q Spells Usage Here.
                    OnAfterAttack(e);
                    break;
                case OrbwalkerType.BeforeAttack:
                    //BeforeAttack Delegate, focus target with W stacks here.
                    OnBeforeAttack(e);
                    break;
            }
        }
        #endregion

        #region Private Methods and operators
        private static void OnAfterAttack(Orbwalker.OrbwalkerActionArgs e)
        {
            if (e.Target.IsValidTarget() && e.Sender.IsMe && (e.Target is Obj_AI_Base))
            {
                switch (Orbwalker.ActiveMode)
                {
                    case OrbwalkerMode.Orbwalk:
                        PreliminaryQCheck((Obj_AI_Base)e.Target, OrbwalkerMode.Orbwalk);
                        var condemnTarget = GetCondemnTarget(ObjectManager.Player.ServerPosition);
                        if (spells[SpellSlot.E].IsEnabledAndReady(OrbwalkerMode.Orbwalk) && condemnTarget != null)
                        {
                            spells[SpellSlot.E].Cast(condemnTarget);
                        }
                        break;
                    case OrbwalkerMode.Hybrid:
                        PreliminaryQCheck((Obj_AI_Base)e.Target, OrbwalkerMode.Hybrid);
                        break;
                }
            }
        }

        private static void OnBeforeAttack(Orbwalker.OrbwalkerActionArgs e)
        {
            if (VHRMenu["dz191.vhr.misc.general"]["specialfocus"].GetValue<MenuBool>().Value)
            {
                var currentTarget = e.Target;
                if (currentTarget is Obj_AI_Hero)
                {
                    var target = (Obj_AI_Hero) currentTarget;
                    var TwoStacksTarget = VHRExtensions.GetHeroWith2WStacks();
                    if (TwoStacksTarget != null && TwoStacksTarget != target)
                    {
                        Orbwalker.OrbwalkTarget = TwoStacksTarget;
                    }
                }
            }
        }
        #endregion

        #region Skills Usage

        #region Tumble

        private static void PreliminaryQCheck(Obj_AI_Base target, OrbwalkerMode mode)
        {
            ////TODO Try to reset AA faster by doing Q against a wall if possible

            if (spells[SpellSlot.Q].IsEnabledAndReady(mode))
            {
                if (GetQEPosition(target) != Vector3.Zero)
                {
                    UseTumble(GetQEPosition(target), target);

                    DelayAction.Add(
                        (int) (Game.Ping / 2f + spells[SpellSlot.Q].Delay * 1000 + 300f / 1200f + 50f), () =>
                        {
                            if (!spells[SpellSlot.Q].IsReady())
                            {
                                spells[SpellSlot.E].Cast(target);
                            }
                        });
                }
                else
                {
                    UseTumble(target);
                }
            }
        }

        #region Q-E Combo Calculation
        private static Vector3 GetQEPosition(Obj_AI_Base Target)
        {
            if (VHRMenu["dz191.vhr.misc.tumble"]["smartq"].GetValue<MenuBool>().Value && spells[SpellSlot.E].IsReady())
            {
                const int currentStep = 30;
                var direction = ObjectManager.Player.Direction.ToVector2().Perpendicular();
                for (var i = 0f; i < 360f; i += currentStep)
                {
                    var angleRad = (i) * (float)(Math.PI / 180f);
                    var rotatedPosition = ObjectManager.Player.Position.ToVector2() + (300f * direction.Rotated(angleRad));
                    if (GetCondemnTarget(rotatedPosition.ToVector3()) != null && rotatedPosition.ToVector3().IsSafePosition())
                    {
                        return rotatedPosition.ToVector3();
                    }
                }
                return Vector3.Zero;
            }
            return Vector3.Zero;

        }
        #endregion

        #region Tumble Overloads
        private static void UseTumble(Obj_AI_Base Target)
        {
            var Position = Game.CursorPos;
            var extendedPosition = ObjectManager.Player.ServerPosition.Extend(Position, 300f);
            var distanceAfterTumble = Vector3.DistanceSquared(extendedPosition, Target.ServerPosition);

            if (VHRMenu["dz191.vhr.misc.tumble"]["limitQ"].GetValue<MenuBool>().Value)
            {
                if ((distanceAfterTumble <= 550 * 550 && distanceAfterTumble >= 100 * 100))
                {
                    if (extendedPosition.IsSafePosition() && extendedPosition.PassesNoQIntoEnemiesCheck())
                    {
                        spells[SpellSlot.Q].Cast(extendedPosition);
                    }
                }
                else
                {
                    if (VHRMenu["dz191.vhr.misc.tumble"]["qspam"].GetValue<MenuBool>().Value)
                    {
                        if (extendedPosition.IsSafePosition() && extendedPosition.PassesNoQIntoEnemiesCheck())
                        {
                            spells[SpellSlot.Q].Cast(extendedPosition);
                        }
                    }
                }
            }
            else
            {
                if (extendedPosition.IsSafePosition() && extendedPosition.PassesNoQIntoEnemiesCheck())
                {
                    spells[SpellSlot.Q].Cast(extendedPosition);
                }
            }
        }

        private static void UseTumble(Vector3 Position, Obj_AI_Base Target = null)
        {
            var extendedPosition = ObjectManager.Player.ServerPosition.Extend(Position, 300f);
            var distanceAfterTumble = Vector3.DistanceSquared(extendedPosition, Target.ServerPosition);

            if (VHRMenu["dz191.vhr.misc.tumble"]["limitQ"].GetValue<MenuBool>().Value)
            {
                if ((distanceAfterTumble <= 550 * 550 && distanceAfterTumble >= 100 * 100))
                {
                    if (extendedPosition.IsSafePosition() && extendedPosition.PassesNoQIntoEnemiesCheck())
                    {
                        spells[SpellSlot.Q].Cast(extendedPosition);
                    }
                }
                else
                {
                    if (VHRMenu["dz191.vhr.misc.tumble"]["qspam"].GetValue<MenuBool>().Value)
                    {
                        if (extendedPosition.IsSafePosition() && extendedPosition.PassesNoQIntoEnemiesCheck())
                        {
                            spells[SpellSlot.Q].Cast(extendedPosition);
                        }
                    }
                }
            }
            else
            {
                if (extendedPosition.IsSafePosition() && extendedPosition.PassesNoQIntoEnemiesCheck())
                {
                    spells[SpellSlot.Q].Cast(extendedPosition);
                }
            }
        }
        #endregion

        #endregion

        #region Condemn

        private static Obj_AI_Base GetCondemnTarget(Vector3 FromPosition)
        {
            if (TickLimiter.CanTick("CondemnLimiter"))
            {
                switch (VHRMenu["dz191.vhr.misc.condemn"]["condemnmethod"].GetValue<MenuList<string>>().Index)
                {
                    case 0:
                        ////VHR SDK Condemn Method

                        if (!VHRMenu["dz191.vhr.misc.general"]["lightweight"].GetValue<MenuBool>().Value)
                        {
                            #region VHR SDK Method (Non LW Method)

                            var HeroList =
                                GameObjects.EnemyHeroes.Where(
                                    h =>
                                        h.IsValidTarget(spells[SpellSlot.E].Range) &&
                                        !h.HasBuffOfType(BuffType.SpellShield) &&
                                        !h.HasBuffOfType(BuffType.SpellImmunity));
                            var NumberOfChecks =
                                VHRMenu["dz191.vhr.misc.condemn"]["predictionNumber"].GetValue<MenuSlider>().Value;
                            var MinChecksPercent =
                                (VHRMenu["dz191.vhr.misc.condemn"]["accuracy"].GetValue<MenuSlider>().Value);
                            var PushDistance =
                                VHRMenu["dz191.vhr.misc.condemn"]["pushdistance"].GetValue<MenuSlider>().Value;
                            var NextPrediction =
                                (VHRMenu["dz191.vhr.misc.condemn"]["nextprediction"].GetValue<MenuSlider>().Value);
                            var PredictionsList = new List<Vector3>();
                            var interval = NextPrediction / NumberOfChecks;
                            var currentInterval = interval;
                            var LastUnitPosition = Vector3.Zero;

                            foreach (var Hero in HeroList)
                            {
                                PredictionsList.Add(Hero.ServerPosition);

                                for (var i = 0; i < NumberOfChecks; i++)
                                {
                                    var Prediction = Movement.GetPrediction(Hero, currentInterval);
                                    var UnitPosition = Prediction.UnitPosition;
                                    if (UnitPosition.DistanceSquared(LastUnitPosition) >=
                                        Hero.BoundingRadius * Hero.BoundingRadius)
                                    {
                                        PredictionsList.Add(UnitPosition);
                                        LastUnitPosition = UnitPosition;
                                        currentInterval += interval;
                                    }
                                }

                                var ExtendedList = new List<Vector3>();

                                foreach (var position in PredictionsList)
                                {
                                    ExtendedList.Add(position.Extend(FromPosition, -PushDistance / 4f));
                                    ExtendedList.Add(position.Extend(FromPosition, -PushDistance / 2f));
                                    ExtendedList.Add(position.Extend(FromPosition, -(PushDistance * 0.75f)));
                                    ExtendedList.Add(position.Extend(FromPosition, -PushDistance));
                                }

                                var WallListCount = ExtendedList.Count(h => h.IsWall());
                                var TotalListCount = ExtendedList.Count();
                                if ((WallListCount / TotalListCount) * 100 >= MinChecksPercent)
                                {
                                    return Hero;
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region VHR SDK Method (LW Method)
                            //// ReSharper disable once LoopCanBePartlyConvertedToQuery
                            foreach (
                                var target in
                                    GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(spells[SpellSlot.E].Range)))
                            {
                                var PushDistance =
                                VHRMenu["dz191.vhr.misc.condemn"]["pushdistance"].GetValue<MenuSlider>().Value;
                                var FinalPosition = target.ServerPosition.Extend(FromPosition, -PushDistance);
                                var AlternativeFinalPosition = target.ServerPosition.Extend(FromPosition, -(PushDistance/2f));
                                if (FinalPosition.IsWall() || AlternativeFinalPosition.IsWall())
                                {
                                    return target;
                                }
                            }
                            #endregion
                        }
                        
                        break;
                    case 1:
                        ////Marksman/Gosu
                        
                        #region Marksman/Gosu Method
                        //// ReSharper disable once LoopCanBePartlyConvertedToQuery
                        foreach (
                            var target in
                                GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(spells[SpellSlot.E].Range)))
                        {
                            var PushDistance =
                            VHRMenu["dz191.vhr.misc.condemn"]["pushdistance"].GetValue<MenuSlider>().Value;
                            var FinalPosition = target.ServerPosition.Extend(FromPosition, -PushDistance);
                            var AlternativeFinalPosition = target.ServerPosition.Extend(FromPosition, -(PushDistance / 2f));
                            if (FinalPosition.IsWall() || AlternativeFinalPosition.IsWall())
                            {
                                return target;
                            }
                        }
                        #endregion

                        break;
                }
            }
            return null;
        }
        #endregion

        #endregion

    }
}
