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
        public override Version Version => new Version(2, 1, 0);
        public override string Author => "Neoslyke, Geolindrag";
        public override string Description => "Shows nearby team's health on the sidebar.";

        public static string path = Path.Combine(TShock.SavePath, "SideBarHealth.json");
        public static Config Config = new();

        private static PlrData[] plrData = new PlrData[255];
        private int updateCounter = 0;
        private bool hpChanged = false;

        public SideBar(Main game) : base(game) { }

        public override void Initialize()
        {
            for (int i = 0; i < plrData.Length; i++)
                plrData[i] = new PlrData();

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
                player.SendData(PacketTypes.Status, Config.Outset, 0, 0, 0, 0, 0);
            }
            else
            {
                player.SendMessage("Sidebar visible", Color.Magenta);
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            updateCounter++;

            if (!hpChanged && updateCounter < 30)
                return;

            updateCounter = 0;
            hpChanged = false;

            UpdateAllSidebars();
        }

        private void OnHP(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.PlayerHp || args.Handled || args.Msg == null)
                return;

            using var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length));

            byte id = reader.ReadByte();
            if (id >= plrData.Length) return;

            short hp = reader.ReadInt16();
            short maxHp = reader.ReadInt16();

            if (plrData[id].HP != hp || plrData[id].MaxHP != maxHp)
            {
                plrData[id].HP = hp;
                plrData[id].MaxHP = maxHp;
                hpChanged = true;
            }
        }

        private void UpdateAllSidebars()
        {
            for (int i = 0; i < 255; i++)
            {
                var viewer = TShock.Players[i];
                if (viewer == null || !viewer.Active || plrData[i].DoHide == 1)
                    continue;

                if (viewer.TPlayer == null)
                    continue;

                int viewerTeam = viewer.TPlayer.team;
                string msg = "";

                // ===== SELF HEALTH FIRST (always on top) =====
                int selfHP = viewer.TPlayer.statLife;
                int selfMaxHP = viewer.TPlayer.statLifeMax2;

                if (selfMaxHP > 0)
                {
                    if (selfHP <= 0)
                    {
                        msg += Config.selfDeadFormat.Replace("{0}", viewer.Name);
                    }
                    else
                    {
                        int hpPercent = (selfHP * 100) / selfMaxHP;
                        string color = GetGradientColor(hpPercent);
                        string bar = BuildBarDirect(selfHP, selfMaxHP, color);

                        msg += Config.selfFormat
                            .Replace("{0}", viewer.Name)
                            .Replace("{1}", bar)
                            .Replace("{2}", selfHP.ToString())
                            .Replace("{3}", selfMaxHP.ToString());
                    }
                }

                // ===== TEAMMATES (only if in a team) =====
                if (viewerTeam >= 1 && viewerTeam <= 5)
                {
                    for (int j = 0; j < 255; j++)
                    {
                        if (i == j) continue; // Skip self (already added above)

                        var target = TShock.Players[j];
                        if (target == null || !target.Active || target.TPlayer == null)
                            continue;

                        // Must be same team
                        if (target.TPlayer.team != viewerTeam)
                            continue;

                        // Check distance
                        float distance = GetDistance(viewer, target);
                        if (distance > Config.MaxDistance)
                            continue;

                        // Get HP data
                        int targetHP = target.TPlayer.statLife;
                        int targetMaxHP = target.TPlayer.statLifeMax2;

                        if (targetMaxHP <= 0)
                            continue;

                        // Dead teammate
                        if (targetHP <= 0)
                        {
                            msg += Config.deadFormat.Replace("{0}", target.Name);
                            continue;
                        }

                        // Build health bar
                        int hpPercent = (targetHP * 100) / targetMaxHP;
                        string color = GetGradientColor(hpPercent);
                        string bar = BuildBarDirect(targetHP, targetMaxHP, color);

                        msg += Config.format
                            .Replace("{0}", target.Name)
                            .Replace("{1}", bar)
                            .Replace("{2}", targetHP.ToString())
                            .Replace("{3}", targetMaxHP.ToString())
                            .Replace("{4}", ((int)distance).ToString());
                    }
                }

                msg += Config.Outset;

                // EXACT SAME SendData FORMAT AS ORIGINAL CODE
                viewer.SendData(PacketTypes.Status, msg, 0, Config.TextFlag, 0, 0, 0);
            }
        }

        private float GetDistance(TSPlayer a, TSPlayer b)
        {
            float dx = a.TPlayer.position.X - b.TPlayer.position.X;
            float dy = a.TPlayer.position.Y - b.TPlayer.position.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy) / 16f;
        }

        private string BuildBarDirect(int hp, int maxHp, string color)
        {
            string bar = $"[c/{color}:";
            bool damaged = false;

            int segments = maxHp / 20;
            for (int k = 0; k < segments; k++)
            {
                if (!damaged && k * 20 >= hp)
                {
                    bar += $"][c/{Config.colorDamage}:";
                    damaged = true;
                }
                bar += Config.barChar;
            }
            return bar + "]";
        }

        private string GetGradientColor(int hpPercent)
        {
            for (int i = 0; i < Config.gradientKeyFrames.Length - 1; i++)
            {
                int high = Config.gradientKeyFrames[i];
                int low = Config.gradientKeyFrames[i + 1];

                if (hpPercent <= high && hpPercent > low)
                {
                    int range = high - low;
                    if (range == 0) continue;
                    int progress = range - (hpPercent - low);
                    return LerpColor(Config.gradient[i], Config.gradient[i + 1], progress, range);
                }
            }

            if (hpPercent >= Config.gradientKeyFrames[0])
                return Config.gradient[0];
            if (hpPercent <= Config.gradientKeyFrames[Config.gradientKeyFrames.Length - 1])
                return Config.gradient[Config.gradient.Length - 1];

            return "FFFFFF";
        }

        private string LerpColor(string c1, string c2, int value, int max)
        {
            if (max == 0) return c1;

            int[] a = {
                Convert.ToInt32(c1.Substring(0, 2), 16),
                Convert.ToInt32(c1.Substring(2, 2), 16),
                Convert.ToInt32(c1.Substring(4, 2), 16)
            };
            int[] b = {
                Convert.ToInt32(c2.Substring(0, 2), 16),
                Convert.ToInt32(c2.Substring(2, 2), 16),
                Convert.ToInt32(c2.Substring(4, 2), 16)
            };

            string result = "";
            for (int i = 0; i < 3; i++)
            {
                int val = a[i] + (b[i] - a[i]) * value / max;
                val = Math.Clamp(val, 0, 255);
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