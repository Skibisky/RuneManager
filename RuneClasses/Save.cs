﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RuneOptim
{
    // Deserializes the .json into this
    public class Save
    {
        [JsonProperty("mons")]
        public IList<Monster> Monsters;

        // Ask for monsters nicely
        public Monster GetMonster(string name)
        {
            return Monsters.Where(m => m.Name == name).FirstOrDefault();
        }

        public Monster GetMonster(string name, int num)
        {
            return Monsters.Where(m => m.Name == name).Skip(num - 1).FirstOrDefault();
        }

        public Monster GetMonster(int id)
        {
            return Monsters.Where(m => m.ID == id).FirstOrDefault();
        }

        [JsonProperty("runes")]
        public IList<Rune> Runes;

        // builds from rune optimizer don't match mine.
        // Don't care right now, perhaps a fuzzy-import later?
        [JsonProperty("savedBuilds")]
        public IList<object> Builds;
    }
}