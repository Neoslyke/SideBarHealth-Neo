using System.IO;
using Newtonsoft.Json;

namespace SideBarHealth
{
    public class Configuration
    {
        public float MaxDistance = 100f;
        public string[] gradient = { "00FFFF", "00FF00", "00FF00", "FFFF00", "FF0000" };
        public int[] gradientKeyFrames = { 120, 100, 50, 50, 0 };
        public string colorDamage = "000000";
        public string format = "{0}\n{1}<{2}\n";
        public string selfFormat = "[c/FFFF00:★ You]\n{1}<{2}\n";
        public string barChar = "l";
        public string deadFormat = "[c/ff0000:{0}]\n";
        public string selfDeadFormat = "[c/ff0000:★ You: DEAD]\n";
        public string Outset = "\n                                                            ";
        public int TextFlag = 2;

        public void Write()
        {
            File.WriteAllText(SideBar.path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Configuration Read()
        {
            if (!File.Exists(SideBar.path))
                return new Configuration();
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(SideBar.path)) ?? new Configuration();
        }
    }
}