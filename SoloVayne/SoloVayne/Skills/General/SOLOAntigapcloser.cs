﻿using DZLib.Logging;
using LeagueSharp;
using LeagueSharp.Common;
using SoloVayne.Skills.Tumble;
using SoloVayne.Utility;
using SOLOVayne.Utility.General;
using ActiveGapcloser = SOLOVayne.Utility.General.ActiveGapcloser;

namespace SoloVayne.Skills.General
{
    class SOLOAntigapcloser
    {
        public SOLOAntigapcloser()
        {
            CustomAntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptable;
        }

        private void OnInterruptable(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            var interrupterEnabled = MenuExtensions.GetItemValue<bool>("solo.vayne.misc.miscellaneous.interrupter");

            if (!interrupterEnabled
                || !Variables.spells[SpellSlot.E].IsReady()
                || !sender.IsValidTarget())
            {
                return;
            }

            if (args.DangerLevel == Interrupter2.DangerLevel.High)
            {
                Variables.spells[SpellSlot.E].Cast(sender);
            }
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var antigapcloserEnabled = MenuExtensions.GetItemValue<bool>("solo.vayne.misc.miscellaneous.antigapcloser");
            var antigapcloserMode =
                MenuExtensions.GetItemValue<StringList>("solo.vayne.misc.miscellaneous.gapcloser.mode").SelectedIndex;
            var endPosition = gapcloser.End;

            if (!antigapcloserEnabled 
                || !Variables.spells[SpellSlot.E].IsReady() 
                || !gapcloser.Sender.IsValidTarget()
                || ObjectManager.Player.Distance(endPosition) > 370)
            {
                return;
            }

            switch (antigapcloserMode)
            {
                case 0:
                    //Smart
                    var ShouldBeRepelled = CustomAntiGapcloser.SpellShouldBeRepelledOnSmartMode(gapcloser.SData.Name);

                    if (ShouldBeRepelled)
                    {
                        Variables.spells[SpellSlot.E].Cast(gapcloser.Sender);
                    }
                    else
                    {
                        //Use Q
                        var extendedPosition = ObjectManager.Player.ServerPosition.Extend(endPosition, -300f);
                        if (extendedPosition.IsSafe())
                        {
                            Variables.spells[SpellSlot.Q].Cast(extendedPosition);
                        }
                    }
                    break;
                case 1:
                    Variables.spells[SpellSlot.E].Cast(gapcloser.Sender);
                    break;
            }
        }
    }
}
