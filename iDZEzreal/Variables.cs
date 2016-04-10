﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace iDZEzreal
{
    internal class Variables
    {
        public static readonly Dictionary<SpellSlot, Spell> Spells = new Dictionary<SpellSlot, Spell>
        {
            {SpellSlot.Q, new Spell(SpellSlot.Q, 1150)},
            {SpellSlot.W, new Spell(SpellSlot.W, 1000)},
            {SpellSlot.E, new Spell(SpellSlot.E, 475)},
            {SpellSlot.R, new Spell(SpellSlot.R, 3000f)}
        };

        public static Menu Menu { get; set; }
        public static Orbwalking.Orbwalker Orbwalker { get; set; }
    }
}