using Newtonsoft.Json;
using System.Drawing;

namespace SidebarHealth
{
    public class Config
    {
        public string[] gradient = { "00FFFF","00FF00","00FF00","FFFF00","FF0000" };
        public int[] gradientKeyFrames = { 120,100,50,50, 0 };//% of health, Setting values lower than 0 will break it
        //Example for a Blue-Purple themed bar; gradientKeyFrames= {120,100,50,50,25,0};gradient={"FFFFFF","00FFFF","2266FF","4400FF","FF00FF","FF0000"};
        public string colorDamage = "000000";//Color of HP lost , AKA emptyness
        public string format = "{0}\n{1}<{2}\n";//{0}=Name {1}=HealthBar   {2}=HealthNumber This would display : Geolindrag <newline> lllll<HPVALUE
        public string barChar = "l"; //Char used to fill the nar ie:  11111,lllll,ooooo etc...
        public string deadFormat = "[c/ff0000:{0}]\n";//{0}  =Name ; Format shown when someone is dead
        public string Outset = "\n                                                            ";//align, This is enough to hide the default message
        public int ArrayLenght = 255;//Does nothing for the time being, as player list is read before this
        public int TextFlag = 2; //See Terraria multiplayer packet structure for reference 1= hide default message | 2=Use shadows/Text outline | 3=1 and 2 
        public void Write()
        {
            File.WriteAllText(SideBar.path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read()
        {
            if (!File.Exists(SideBar.path))
                return new Config();
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(SideBar.path));
        }
    }
}
