using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;
using Main = Terraria.Main;
namespace SidebarHealth{
    [ApiVersion(2,1)]
    public class SideBar : TerrariaPlugin{
        public override string Name => "Sidebar Health Display";
        public override Version Version => new Version(0, 7, 3);
        public override string Author => "Geolindrag";
        public override string Description => "Shows Everyone health on the Text Sidebar";
        //Json Reading stuff
        public static string path = Path.Combine(TShock.SavePath + "/SidebarHealth.json");
        public static Config Config = new Config();
        //
        //Data Allocating
        private static PlrData[] plrData = new PlrData[255];// You have to modify this manually
        //

        public SideBar(Main game) : base(game) { }

        public override void Initialize(){
            for (int i = 0; i < plrData.Length; i++){//Init the array or we die
                plrData[i] = new PlrData();
            }
            Commands.ChatCommands.Add(//Hide/show sidebar
                new Command(
                  Permissions.canchat, ToggleInfo, "toggleinfo"
                )
            );
            ServerApi.Hooks.NetGetData.Register(this, OnHP);
            GeneralHooks.ReloadEvent += OnReload;
            if (File.Exists(path))
                Config = Config.Read();
            else
                Config.Write();
        }

        private void ToggleInfo(CommandArgs a){
            if (a.Player == null) return;
            if (plrData[a.Player.Index].DoHide == 0){
                plrData[a.Player.Index].DoHide++;
                a.Player.SendMessage("The sidebar is now hidden", new Color { R = 255, B = 0, G = 255 });
                a.Player.SendData(PacketTypes.Status, Config.Outset, 0, 0, 0, 0, 0);
            }
            else{
                plrData[a.Player.Index].DoHide = 0;
                a.Player.SendMessage("The Sidebar is now visible", new Color { R = 255, B = 0, G = 255 });
            }
        }
        private void OnHP(GetDataEventArgs args){
           // Console.WriteLine("========Entered method========");
            if (args.MsgID != PacketTypes.PlayerHp || args.Handled) return;
            //Console.WriteLine("checks passed");
            using (BinaryReader reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {//DATA UPDATE
                //Console.WriteLine("reader set");
                byte plrID = reader.ReadByte();
                Int16 HP = reader.ReadInt16();
                Int16 MaxHP = reader.ReadInt16();
                plrData[plrID].HP = HP;
                plrData[plrID].MaxHP = MaxHP;
            }//
            //Console.WriteLine("Data read");
            string param1 = "";
            string msg = "";
            string color = "";
            int HPper = 100;
            int MaxVal = 100;
            int[] Color1 = { 0, 0, 0 };
            int[] Color2 = { 0, 0, 0 };
            int HPperR = 0;
            bool IsDamage = false;
            //Console.WriteLine("Set variables");
            //DEBUG
            /*
            TSPlayer ae = TSPlayer.FindByNameOrID("0").First();
            ae.SendData(PacketTypes.Status, Config.format, 0, Config.TextFlag, 0, 0, 0);
            return;
            */
            //
            for (int i = 0; i < plrData.Length; i++){
               // Console.WriteLine("first for loop, plr {0}",i);
                if (TShock.Players[i] != null && plrData[i].HP != -1){//PER PLAYER
                   // Console.WriteLine("Check for nulls passed");
                    if (plrData[i].HP > 0){
                        color = "";
                        msg += Config.format;
                        HPper = (plrData[i].HP * 100) / plrData[i].MaxHP;
                        // Console.WriteLine("HP% {0}",HPper);
                       // Console.WriteLine("Player data copied");
                        for (int j = 0; j < Config.gradientKeyFrames.Length - 1; j++)//PER COLOR
                        {
                            //Console.WriteLine("2nd for entered");
                            MaxVal= Config.gradientKeyFrames[j] - Config.gradientKeyFrames[j+1];
                            //Color FETCH
                            Color1[0] = Convert.ToInt16(Config.gradient[j][0].ToString() + Config.gradient[j][1].ToString(), 16);
                            Color1[1] = Convert.ToInt16(Config.gradient[j][2].ToString() + Config.gradient[j][3].ToString(), 16);
                            Color1[2] = Convert.ToInt16(Config.gradient[j][4].ToString() + Config.gradient[j][5].ToString(), 16);
                            Color2[0] = Convert.ToInt16(Config.gradient[j + 1][0].ToString() + Config.gradient[j + 1][1].ToString(), 16);
                            Color2[1] = Convert.ToInt16(Config.gradient[j + 1][2].ToString() + Config.gradient[j + 1][3].ToString(), 16);
                            Color2[2] = Convert.ToInt16(Config.gradient[j + 1][4].ToString() + Config.gradient[j + 1][5].ToString(), 16);
                            //Console.WriteLine("{0} {1} {2} ; {3} {4} {5}", Color1[0], Color1[1], Color1[2], Color2[0], Color2[1], Color2[2]);
                                //
                            if (MaxVal != 0 && HPper <= Config.gradientKeyFrames[j] && HPper > Config.gradientKeyFrames[j + 1])
                            {
                                HPperR = MaxVal-(HPper - Config.gradientKeyFrames[j + 1]);
                                //Console.WriteLine("Relative Health: {0}",HPperR);
                                for (int k = 0; k < Color1.Length; k++)//Per VALUE
                                {
                                    if(Color1[k] == Color2[k])
                                    {
                                    //    Console.WriteLine("IS EQUAL");
                                        color += Color1[k].ToString("X");
                                    }else if (Color2[k] > Color1[k])
                                    {
                                        color+=(Color1[k] + ((Color2[k] - Color1[k]) * HPperR) / MaxVal).ToString("X");
                                        //Console.WriteLine("2>1\t{0}", Color1[k] + ((Color2[k] - Color1[k]) * HPperR) / MaxVal);
                                    }
                                    else if (Color1[k] > Color2[k])
                                    {
                                        color += (Color1[k] - ((Color1[k] - Color2[k]) * HPperR) / MaxVal).ToString("X");
                                        //Console.WriteLine(" 1>2\t {0}", Color1[k] - ((Color1[k] - Color2[k]) * HPperR) / MaxVal);
                                    }

                                    if (color.Length % 2 == 1)
                                    {
                                        color = color.Insert(color.Length - 1, "0");
                                    }

                                    
                                }
                                
                               // Console.WriteLine("break at cycle {0}",j);
                                break;
                                
                            }
                           
                        }
                        //Console.WriteLine("Color calc done");
                        //  Console.WriteLine("In the end {0}",color);
                        //Start Parsing the format finally

                        if (msg.Contains("{0}"))
                        {
                            msg = msg.Replace("{0}", TShock.Players[i].Name);
                        }
                        if (msg.Contains("{1}"))
                        {
                            
                            IsDamage = false;
                            param1 = "[c/" + color + ":";
                            for (int j = 0; j < plrData[i].MaxHP / 20; j++)
                            {
                                if (!IsDamage && j * 20 > plrData[i].HP)
                                {
                                    param1 += "][c/" + Config.colorDamage + ":";
                                    IsDamage = true;
                                }
                                param1 += Config.barChar;
                            }
                            param1 += "]";
                            msg = msg.Replace("{1}", param1);
                        }
                        if (msg.Contains("{2}"))
                        {
                            msg = msg.Replace("{2}", plrData[i].HP.ToString());
                        }
                        //Console.WriteLine("MSG parsing done");
                    }
                    else
                    {
                        msg += Config.deadFormat;
                        if (msg.Contains("{0}"))
                        {
                            msg = msg.Replace("{0}", TShock.Players[i].Name);
                        }
                       // Console.WriteLine("Shouldnt be here");
                    }


                       

                }
            }
            msg += Config.Outset;
            for (int i = 0; i < plrData.Length; i++)
            {
               // Console.WriteLine("Sending messages");
                if (TShock.Players[i] != null && plrData[i].DoHide == 0 && plrData[i].HP!=-1)
                {
                    TSPlayer p = TSPlayer.FindByNameOrID(i.ToString()).First();
                    p.SendData(PacketTypes.Status, msg, 0, Config.TextFlag, 0, 0, 0);
                }
            }
        }
        private void OnReload(ReloadEventArgs e){
            if (File.Exists(path))
                Config = Config.Read();
            else
                Config.Write();
        }
    }
}
