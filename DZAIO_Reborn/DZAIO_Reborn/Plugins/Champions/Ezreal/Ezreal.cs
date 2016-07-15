﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DZAIO_Reborn.Core;
using DZAIO_Reborn.Helpers;
using DZAIO_Reborn.Helpers.Entity;
using DZAIO_Reborn.Helpers.Modules;
using DZAIO_Reborn.Plugins.Champions.Sivir.Modules;
using DZAIO_Reborn.Plugins.Champions.Veigar.Modules;
using DZAIO_Reborn.Plugins.Interface;
using DZLib.Core;
using DZLib.Menu;
using DZLib.MenuExtensions;
using LeagueSharp;
using LeagueSharp.Common;
using SPrediction;

namespace DZAIO_Reborn.Plugins.Champions.Ezreal
{
    class Ezreal : IChampion
    {
        public void OnLoad(Menu menu)
        {
            var comboMenu = new Menu(ObjectManager.Player.ChampionName + ": Combo", "dzaio.champion.ezreal.combo");
            {
                comboMenu.AddModeMenu(ModesMenuExtensions.Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.R }, new[] { true, true, true });
                menu.AddSubMenu(comboMenu);
            }

            var mixedMenu = new Menu(ObjectManager.Player.ChampionName + ": Mixed", "dzaio.champion.ezreal.harrass");
            {
                mixedMenu.AddModeMenu(ModesMenuExtensions.Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
                mixedMenu.AddSlider("dzaio.champion.sivir.mixed.mana", "Min Mana % for Harass", 30, 0, 100);
                menu.AddSubMenu(mixedMenu);
            }

            var farmMenu = new Menu(ObjectManager.Player.ChampionName + ": Farm", "dzaio.champion.ezreal.farm");
            {
                farmMenu.AddModeMenu(ModesMenuExtensions.Mode.Laneclear, new[] { SpellSlot.Q }, new[] { true });
                farmMenu.AddSlider("dzaio.champion.sivir.farm.mana", "Min Mana % for Farm", 30, 0, 100);
                menu.AddSubMenu(farmMenu);
            }

            var extraMenu = new Menu(ObjectManager.Player.ChampionName + ": Extra", "dzaio.champion.ezreal.extra");
            {
                extraMenu.AddBool("dzaio.champion.ezreal.extra.antigapcloser", "E Antigapcloser", true);
                extraMenu.AddSlider("dzaio.champion.ezreal.extra.e.antigpdelay", "E Antigapcloser Delay", 120, 0, 350);
                extraMenu.AddBool("dzaio.champion.ezreal.extra.autoQKS", "Q KS", true);
                extraMenu.AddBool("dzaio.champion.ezreal.extra.autoQRoot", "Q Root/Slow/Dash", false);
                extraMenu.AddBool("dzaio.champion.ezreal.extra.autoRKS", "R KS", true);
            }

            Variables.Spells[SpellSlot.Q].SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            Variables.Spells[SpellSlot.W].SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            Variables.Spells[SpellSlot.R].SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

        }

        public void RegisterEvents()
        {
            DZInterrupter.OnInterruptableTarget += OnInterrupter;
            DZAntigapcloser.OnEnemyGapcloser += OnGapcloser;
            Orbwalking.AfterAttack += AfterAttack;
        }


        private void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe || !unit.IsValid<Obj_AI_Base>())
            {
                return;
            }

            if (Variables.Spells[SpellSlot.Q].IsEnabledAndReady(ModesMenuExtensions.Mode.Combo)
                || (Variables.Spells[SpellSlot.Q].IsEnabledAndReady(ModesMenuExtensions.Mode.Harrass)
                && ObjectManager.Player.ManaPercent >= Variables.AssemblyMenu.GetItemValue<Slider>("dzaio.champion.extra.mixed.mana").Value))
            {
                if (target.IsValid<Obj_AI_Hero>() && target.IsValidTarget())
                {
                    Variables.Spells[SpellSlot.Q].CastIfHitchanceEquals(target as Obj_AI_Hero, HitChance.High);
                }
            }
        }

        private void OnGapcloser(DZLib.Core.ActiveGapcloser gapcloser)
        {

        }

        private void OnInterrupter(Obj_AI_Hero sender, DZInterrupter.InterruptableTargetEventArgs args)
        {

        }
        public Dictionary<SpellSlot, Spell> GetSpells()
        {
            return new Dictionary<SpellSlot, Spell>
                      {
                                    { SpellSlot.Q, new Spell(SpellSlot.Q, 1180f) },
                                    { SpellSlot.W, new Spell(SpellSlot.W, 850f) },
                                    { SpellSlot.E, new Spell(SpellSlot.E, 475f) },
                                    { SpellSlot.R, new Spell(SpellSlot.R, 2500f) }
                      };
        }

        public List<IModule> GetModules()
        {
            return new List<IModule>()
            {

            };
        }

        public void OnTick()
        {

        }

        public void OnCombo()
        {
            if (Variables.Spells[SpellSlot.Q].IsEnabledAndReady(ModesMenuExtensions.Mode.Combo))
            {
                var qTarget = Variables.Spells[SpellSlot.Q].GetTarget();
                if (qTarget.IsValidTarget())
                {
                    var qPrediction = Variables.Spells[SpellSlot.Q].GetPrediction(qTarget);
                    if (qPrediction.Hitchance >= HitChance.High)
                    {
                        Variables.Spells[SpellSlot.Q].Cast(qPrediction.CastPosition);
                    }
                }
            }
        }

        public void OnMixed()
        {
            if (ObjectManager.Player.ManaPercent <
                Variables.AssemblyMenu.GetItemValue<Slider>("dzaio.champion.ezreal.mixed.mana").Value)
            {
                return;
            }

            if (Variables.Spells[SpellSlot.Q].IsEnabledAndReady(ModesMenuExtensions.Mode.Harrass))
            {
                var qTarget = Variables.Spells[SpellSlot.Q].GetTarget();
                if (qTarget.IsValidTarget())
                {
                    var qPrediction = Variables.Spells[SpellSlot.Q].GetPrediction(qTarget);
                    if (qPrediction.Hitchance >= HitChance.High)
                    {
                        Variables.Spells[SpellSlot.Q].Cast(qPrediction.CastPosition);
                    }
                }
            }
        }

        public void OnLastHit()
        { }

        public void OnLaneclear()
        {
            if (ObjectManager.Player.ManaPercent <
                Variables.AssemblyMenu.GetItemValue<Slider>("dzaio.champion.ezreal.farm.mana").Value)
            {
                return;
            }

        }
    }
}
