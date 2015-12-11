﻿using LeagueSharp;
using SoloVayne.Skills.General;
using SoloVayne.Utility;

namespace SoloVayne
{
    class SOLOBootstrap
    {
        public MenuGenerator MenuGenerator;

        public SOLOVayne SOLOVayne;

        public SOLOAntigapcloser Antigapcloser;

        public SOLOBootstrap()
        {
            if (Variables.Instance != null)
            {
                return;
            }

            SOLOVayne = new SOLOVayne();
            MenuGenerator = new MenuGenerator();
            Antigapcloser = new SOLOAntigapcloser();

            MenuGenerator.GenerateMenu();

            PrintLoaded();
        }

        public void PrintLoaded()
        {
            Game.PrintChat("<b>[<font color='#009aff'>SOLO</font>] <font color='#009aff'>S</font>mart <font color='#009aff'>O</font>ptimized <font color='#009aff'>L</font>ightweight<font color='#009aff'> O</font>riginal <font color='#009aff'>Vayne</font></b> loaded!");
        }

        public SOLOBootstrap GetInstance()
        {
            return Variables.Instance ?? (Variables.Instance = new SOLOBootstrap());
        }
    }
}
