using System.IO;
using Newtonsoft.Json;

namespace SideBarHealth
{
    public class Config
    {
        public float MaxDistance = 100f; // Distance in tiles
        public string NoNearbyPlayers = "No nearby teammates";
        public string Outset = "";
        public int TextFlag = 0;
        public string format = "{0}: {1} {2}/{3}HP\n";
        public string deadFormat = "[c/FF0000:{0}: DEAD]\n";
        public string barChar = "|";
        public string colorDamage = "444444";
        public string[] gradient = { "00FF00", "FFFF00", "FF0000" };
        public int[] gradientKeyFrames = { 100, 50, 0 };

        public void Write()
        {
            File.WriteAllText(SideBar.path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read()
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(SideBar.path)) ?? new Config();
        }
    }
}