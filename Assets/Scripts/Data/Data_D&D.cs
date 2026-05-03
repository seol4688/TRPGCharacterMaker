using System;
using System.Collections.Generic;

namespace DND
{
    #region Races
    [Serializable]
    public class Race
    {
        public string race;
        public int Str;
        public int Dex;
        public int Con;
        public int Int;
        public int Wis;
        public int Cha;
    }

    [Serializable]
    public class RaceData : ILoader<string, Race>
    {
        public List<Race> Races = new List<Race>();

        public Dictionary<string, Race> MackDict()
        {
            Dictionary<string, Race> dict = new Dictionary<string, Race>();
            foreach (Race race in Races)
                dict.Add(race.race, race);

            return dict;
        }
    }
    #endregion

}