using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace SidebarHealth
{
    [ApiVersion(2, 1)]
    public class SideBar : TerrariaPlugin
    {
        public override string Name => "Sidebar Health Display";
        public override Version Version => new Version(0, 7, 5);
        public override string Author => "Neoslyke (fork of Geolindrag)";
        public override string Description => "Shows everyone’s health on the sidebar";

        public static string path = Path.Combine(TShock.SavePath, "SideBarHealth.json");
        public static Config Config = new();

        private static PlrData[] plrData = new PlrData[255];

        // ✅ tracking changes + delay
        private static int[] lastHP = new int[255];
        private static Stopwatch timer = Stopwatch.StartNew();
        private const int UpdateDelayMs = 250;

        public SideBar(Main game) : base(game) { }

        public override void Initialize()
        {
            for (int i = 0; i < plrData.Length; i++)
            {
                plrData[i] = new PlrData();
                lastHP[i] = -1;
            }

            Commands.ChatCommands.Add(new Command(Permissions.canchat, ToggleInfo, "toggleinfo"));

            ServerApi.Hooks.NetGetData.Register(this, OnHP);
            GeneralHooks.ReloadEvent += OnReload;

            Config = File.Exists(path) ? Config.Read() : new Config();
            Config.Write();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnHP);
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }

        private void ToggleInfo(CommandArgs args)
        {
            var player = args.Player;
            if (player == null) return;

            ref var data = ref plrData[player.Index];

            data.DoHide = (byte)(data.DoHide == 0 ? 1 : 0);

            if (data.DoHide == 1)
            {
                player.SendMessage("Sidebar hidden", Color.Magenta);
                player.SendData(PacketTypes.Status, Config.Outset, 0, 0);
            }
            else
            {
                player.SendMessage("Sidebar visible", Color.Magenta);
            }
        }

        private void OnHP(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.PlayerHp || args.Handled)
                return;

            if (args.Msg == null)
                return;

            using var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length));

            byte id = reader.ReadByte();
            if (id >= plrData.Length)
                return;

            short hp = reader.ReadInt16();
            short maxHp = reader.ReadInt16();

            plrData[id].HP = hp;
            plrData[id].MaxHP = maxHp;

            // ✅ only update if HP changed
            if (lastHP[id] == hp)
                return;

            lastHP[id] = hp;

            // ✅ delay check
            if (timer.ElapsedMilliseconds < UpdateDelayMs)
                return;

            timer.Restart();

            for (int i = 0; i < plrData.Length; i++)
            {
                var viewer = TShock.Players[i];
                if (viewer == null || plrData[i].DoHide == 1 || plrData[i].HP == -1)
                    continue;

                int viewerTeam = viewer.TPlayer.team;

                // ❌ no team → empty
                if (viewerTeam < 1 || viewerTeam > 5)
                {
                    viewer.SendData(PacketTypes.Status, Config.Outset, 0, Config.TextFlag);
                    continue;
                }

                string msg = "";

                for (int j = 0; j < plrData.Length; j++)
                {
                    var target = TShock.Players[j];
                    var data = plrData[j];

                    if (target == null || target.TPlayer == null || data.HP == -1)
                        continue;

                    if (target.TPlayer.team != viewerTeam)
                        continue;

                    if (data.HP <= 0)
                    {
                        msg += Config.deadFormat.Replace("{0}", target.Name);
                        continue;
                    }

                    if (data.MaxHP <= 0)
                        continue;

                    int hpPercent = (data.HP * 100) / data.MaxHP;

                    string color = GetGradientColor(hpPercent);
                    string bar = BuildBar(data, color);

                    string line = Config.format
                        .Replace("{0}", target.Name)
                        .Replace("{1}", bar)
                        .Replace("{2}", data.HP.ToString());

                    msg += line;
                }

                msg += Config.Outset;

                viewer.SendData(PacketTypes.Status, msg, 0, Config.TextFlag);
            }
        }

        private string BuildBar(PlrData data, string color)
        {
            string result = $"[c/{color}:";

            bool damage = false;

            for (int j = 0; j < data.MaxHP / 20; j++)
            {
                if (!damage && j * 20 > data.HP)
                {
                    result += $"][c/{Config.colorDamage}:";
                    damage = true;
                }
                result += Config.barChar;
            }

            return result + "]";
        }

        private string GetGradientColor(int hp)
        {
            for (int j = 0; j < Config.gradientKeyFrames.Length - 1; j++)
            {
                int max = Config.gradientKeyFrames[j] - Config.gradientKeyFrames[j + 1];

                if (max == 0) continue;

                if (hp <= Config.gradientKeyFrames[j] && hp > Config.gradientKeyFrames[j + 1])
                {
                    int rel = max - (hp - Config.gradientKeyFrames[j + 1]);
                    return LerpColor(Config.gradient[j], Config.gradient[j + 1], rel, max);
                }
            }

            return "FFFFFF";
        }

        private string LerpColor(string c1, string c2, int value, int max)
        {
            int[] a = {
                Convert.ToInt32(c1.Substring(0,2),16),
                Convert.ToInt32(c1.Substring(2,2),16),
                Convert.ToInt32(c1.Substring(4,2),16)
            };

            int[] b = {
                Convert.ToInt32(c2.Substring(0,2),16),
                Convert.ToInt32(c2.Substring(2,2),16),
                Convert.ToInt32(c2.Substring(4,2),16)
            };

            string result = "";

            for (int i = 0; i < 3; i++)
            {
                int val = a[i] + ((b[i] - a[i]) * value) / max;
                result += val.ToString("X2");
            }

            return result;
        }

        private void OnReload(ReloadEventArgs e)
        {
            Config = File.Exists(path) ? Config.Read() : new Config();
        }
    }
}