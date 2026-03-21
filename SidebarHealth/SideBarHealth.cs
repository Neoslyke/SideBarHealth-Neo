using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;

namespace SideBarHealth
{
    [ApiVersion(2, 1)]
    public class SideBar : TerrariaPlugin
    {
        public override string Name => "SideBarHealth";
        public override Version Version => new Version(2, 2, 0);
        public override string Author => "Neoslyke, Geolindrag";
        public override string Description => "Shows nearby team's health on the sidebar.";

        public static string path = Path.Combine(TShock.SavePath, "SideBarHealth.json");
        public static Config Config = new();

        private static PlrData[] plrData = new PlrData[255];

        public SideBar(Main game) : base(game) { }

        public override void Initialize()
        {
            for (int i = 0; i < plrData.Length; i++)
            {
                plrData[i] = new PlrData();
            }

            Commands.ChatCommands.Add(new Command(Permissions.canchat, ToggleInfo, "toggleinfo"));

            ServerApi.Hooks.NetGetData.Register(this, OnHP);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            GeneralHooks.ReloadEvent += OnReload;

            Config = File.Exists(path) ? Config.Read() : new Config();
            Config.Write();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnHP);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
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

        private int updateCounter = 0;

        private void OnGameUpdate(EventArgs args)
        {
            // Update every 15 ticks (~4 times per second for smooth updates)
            updateCounter++;
            if (updateCounter < 15)
                return;
            updateCounter = 0;

            UpdateAllSidebars();
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
        }

        private void UpdateAllSidebars()
        {
            for (int i = 0; i < plrData.Length; i++)
            {
                var viewer = TShock.Players[i];
                if (viewer == null || !viewer.Active || plrData[i].DoHide == 1)
                    continue;

                int viewerTeam = viewer.TPlayer.team;

                // No team → empty sidebar
                if (viewerTeam < 1 || viewerTeam > 5)
                {
                    viewer.SendData(PacketTypes.Status, Config.Outset, 0, Config.TextFlag);
                    continue;
                }

                string msg = "";
                int playerCount = 0;

                for (int j = 0; j < plrData.Length; j++)
                {
                    if (i == j) // Skip self
                        continue;

                    var target = TShock.Players[j];
                    var data = plrData[j];

                    if (target == null || !target.Active || target.TPlayer == null)
                        continue;

                    // Must be same team
                    if (target.TPlayer.team != viewerTeam)
                        continue;

                    // Check distance
                    float distance = GetDistance(viewer, target);
                    if (distance > Config.MaxDistance)
                        continue;

                    playerCount++;

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
                        .Replace("{2}", data.HP.ToString())
                        .Replace("{3}", data.MaxHP.ToString())
                        .Replace("{4}", ((int)distance).ToString());

                    msg += line;
                }

                if (playerCount == 0)
                {
                    msg = Config.NoNearbyPlayers;
                }

                msg += Config.Outset;

                viewer.SendData(PacketTypes.Status, msg, 0, Config.TextFlag);
            }
        }

        private float GetDistance(TSPlayer player1, TSPlayer player2)
        {
            float dx = player1.TPlayer.position.X - player2.TPlayer.position.X;
            float dy = player1.TPlayer.position.Y - player2.TPlayer.position.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy) / 16f; // Convert to tiles
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
            e.Player?.SendSuccessMessage("[SideBarHealth] Config reloaded.");
        }
    }
}