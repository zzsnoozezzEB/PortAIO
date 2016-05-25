#pragma warning disable 618
namespace ElUtilitySuite.Trackers
{
    //Recall tracker from BaseUlt

    #region

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;
    using SharpDX.Direct3D9;

    using Color = System.Drawing.Color;
    using Font = SharpDX.Direct3D9.Font;
    using EloBuddy.SDK.Menu;
    using EloBuddy;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Notifications;
    #endregion

    internal class RecallTracker : IPlugin
    {
        #region Static Fields




        #endregion

        #region Fields

        public List<EnemyInfo> EnemyInfo = new List<EnemyInfo>();

        private readonly int BarHeight = 10;

        private readonly int SeperatorHeight = 5;

        private LeagueSharp.Common.Utility.Map.MapType Map;

        #endregion

        #region Constructors and Destructors

        public RecallTracker()
        {
        }

        #endregion

        #region Public Properties

        public Menu Menu { get; set; }

        #endregion

        #region Properties

        private List<AIHeroClient> Enemies { get; set; }

        private List<AIHeroClient> Heroes { get; set; }

        private Font Text { get; set; }

        #endregion

        #region Public Methods and Operators

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }

        public static int getBoxItem(Menu m, string item)
        {
            return m[item].Cast<ComboBox>().CurrentValue;
        }

        /// <summary>
        ///     Creates the menu.
        /// </summary>
        /// <param name="rootMenu">The root menu.</param>
        /// <returns></returns>
        public void CreateMenu(Menu rootMenu)
        {
            var notificationsMenu = rootMenu.AddSubMenu("Recall tracker", "Recall tracker");
            {
                notificationsMenu.Add("showRecalls", new CheckBox("Show Recalls"));
                notificationsMenu.Add("notifRecFinished", new CheckBox("Recall finished"));
                notificationsMenu.Add("notifRecAborted", new CheckBox("Recall aborted"));
                notificationsMenu.Add("RecallTracker.OffsetBottom", new Slider("Offset bottom", 52, 0, 1500));
                notificationsMenu.Add("RecallTracker.FontSize", new Slider("Font size", 13, 13, 30));
            }

            this.Menu = notificationsMenu;
        }

        public void Load()
        {
            this.Heroes = ObjectManager.Get<AIHeroClient>().ToList();
            this.Enemies = this.Heroes.Where(x => x.IsEnemy).ToList();

            this.EnemyInfo = this.Enemies.Select(x => new EnemyInfo(x)).ToList();
            this.Map = LeagueSharp.Common.Utility.Map.GetMap().Type;

            this.Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                    {
                        FaceName = "Calibri", Height = getSliderItem(this.Menu, "RecallTracker.FontSize"), Width = 6, OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default
                    });

            Obj_AI_Base.OnTeleport += this.Obj_AI_Base_OnTeleport;
            Drawing.OnDraw += this.Drawing_OnDraw;
            Drawing.OnPreReset += args => { this.Text.OnLostDevice(); };
            Drawing.OnPostReset += args => { this.Text.OnResetDevice(); };
            AppDomain.CurrentDomain.DomainUnload += this.CurrentDomainDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += this.CurrentDomainDomainUnload;
        }

        #endregion

        #region Methods

        private void CurrentDomainDomainUnload(object sender, EventArgs e)
        {
            this.Text.Dispose();
        }

        /// <summary>
        ///     
        /// </summary>
        private int BarY
        {
            get
            {
                return (int)(Drawing.Height - 150f - getSliderItem(this.Menu, "RecallTracker.OffsetBottom"));
            }
        }

        /// <summary>
        ///     
        /// </summary>
        private int BarX
        {
            get
            {
                return (int)(Drawing.Width * 0.425f);
            }
        }


        private int BarWidth
        {
            get
            {
                return (int)(Drawing.Width - 2 * this.BarX);
            }
        }

        private float Scale
        {
            get
            {
                return (float)this.BarWidth / 8000;
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!getCheckBoxItem(this.Menu, "showRecalls") || Drawing.Direct3DDevice == null)
            {
                return;
            }

            var indicated = false;

            var fadeout = 1f;
            var count = 0;

            foreach (var enemyInfo in
                this.EnemyInfo.Where(
                    x =>
                    x.Player.IsValid<AIHeroClient>() && x.RecallInfo.ShouldDraw() && !x.Player.IsDead
                    && x.RecallInfo.GetRecallCountdown() > 0).OrderBy(x => x.RecallInfo.GetRecallCountdown()))
            {
                if (!enemyInfo.RecallInfo.LockedTarget)
                {
                    fadeout = 1f;
                    var color = Color.White;

                    if (enemyInfo.RecallInfo.WasAborted())
                    {
                        fadeout = enemyInfo.RecallInfo.GetDrawTime() / (float)enemyInfo.RecallInfo.FADEOUT_TIME;
                        color = Color.Yellow;
                    }

                    this.DrawRect(
                        this.BarX,
                        this.BarY,
                        (int)(this.Scale * enemyInfo.RecallInfo.GetRecallCountdown()),
                        this.BarHeight,
                        1,
                        Color.FromArgb(255, Color.DeepSkyBlue));

                    this.DrawRect(
                        this.BarX + this.Scale * enemyInfo.RecallInfo.GetRecallCountdown() - 1,
                        this.BarY - this.SeperatorHeight,
                        0,
                        this.SeperatorHeight + 1,
                        1,
                        Color.FromArgb(0, Color.DeepSkyBlue));


                    var champInfo = enemyInfo.Player.ChampionName + " (" + (int)enemyInfo.Player.HealthPercent + ")% ";

                    this.Text.DrawText(
                        null,
                        champInfo,
                        (int)this.BarX
                        + (int)
                          (this.Scale * enemyInfo.RecallInfo.GetRecallCountdown()
                           - this.Text.MeasureText(null, champInfo, FontDrawFlags.Right).Width / 2f),
                        (int)this.BarY - this.SeperatorHeight - this.Text.Description.Height - 1,
                        new ColorBGRA(color.R, color.G, color.B, (byte)(color.A * fadeout)));
                }
                else
                {
                    if (!indicated && (int)enemyInfo.RecallInfo.EstimatedShootT != 0)
                    {
                        indicated = true;
                        this.DrawRect(
                            this.BarX + this.Scale * enemyInfo.RecallInfo.EstimatedShootT,
                            this.BarY + this.SeperatorHeight + this.BarHeight - 3,
                            0,
                            this.SeperatorHeight * 2,
                            2,
                            Color.Orange);
                    }

                    this.DrawRect(
                        this.BarX,
                        this.BarY,
                        (int)(this.Scale * enemyInfo.RecallInfo.GetRecallCountdown()),
                        this.BarHeight,
                        1,
                        Color.FromArgb(255, Color.Red));
                    this.DrawRect(
                        this.BarX + this.Scale * enemyInfo.RecallInfo.GetRecallCountdown() - 1,
                        this.BarY + this.SeperatorHeight + this.BarHeight - 3,
                        0,
                        this.SeperatorHeight + 1,
                        1,
                        Color.IndianRed);

                    this.Text.DrawText(
                        null,
                        enemyInfo.Player.ChampionName,
                        (int)this.BarX
                        + (int)
                          (this.Scale * enemyInfo.RecallInfo.GetRecallCountdown()
                           - (float)(enemyInfo.Player.ChampionName.Length * this.Text.Description.Width) / 2),
                        (int)this.BarY + this.SeperatorHeight + this.Text.Description.Height / 2,
                        new ColorBGRA(255, 92, 92, 255));
                }

                count++;
            }

            if (count > 0)
            {
                if (count != 1)
                {
                    fadeout = 1f;
                }

                this.DrawRect(
                    this.BarX,
                    this.BarY,
                    this.BarWidth,
                    this.BarHeight,
                    1,
                    Color.FromArgb((int)(40f * fadeout), Color.White));

                this.DrawRect(
                    this.BarX - 1,
                    this.BarY + 1,
                    0,
                    this.BarHeight,
                    1,
                    Color.FromArgb((int)(255f * fadeout), Color.White));
                this.DrawRect(
                    this.BarX - 1,
                    this.BarY - 1,
                    this.BarWidth + 2,
                    1,
                    1,
                    Color.FromArgb((int)(255f * fadeout), Color.White));
                this.DrawRect(
                    this.BarX - 1,
                    this.BarY + this.BarHeight,
                    this.BarWidth + 2,
                    1,
                    1,
                    Color.FromArgb((int)(255f * fadeout), Color.White));
                this.DrawRect(
                    this.BarX + 1 + this.BarWidth,
                    this.BarY + 1,
                    0,
                    this.BarHeight,
                    1,
                    Color.FromArgb((int)(255f * fadeout), Color.White));
            }
        }

        private void DrawRect(float x, float y, int width, int height, float thickness, Color color)
        {
            for (var i = 0; i < height; i++)
            {
                Drawing.DrawLine(x, y + i, x + width, y + i, thickness, color);
            }
        }

        private void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            var unit = sender as AIHeroClient;

            if (unit == null || !unit.IsValid || unit.IsAlly)
            {
                return;
            }

            var recall = Packet.S2C.Teleport.Decoded(unit, args);
            var enemyInfo =
                this.EnemyInfo.Find(x => x.Player.NetworkId == recall.UnitNetworkId).RecallInfo.UpdateRecall(recall);

            if (recall.Type == Packet.S2C.Teleport.Type.Recall)
            {
                switch (recall.Status)
                {
                    case Packet.S2C.Teleport.Status.Abort:
                        if (getCheckBoxItem(this.Menu, "notifRecAborted"))
                        {
                            this.ShowNotification(enemyInfo.Player.ChampionName + ": Recall ABORTED", Color.Orange, 4000);
                        }

                        break;
                    case Packet.S2C.Teleport.Status.Finish:
                        if (getCheckBoxItem(this.Menu, "notifRecFinished"))
                        {
                            this.ShowNotification(
                                enemyInfo.Player.ChampionName + ": Recall FINISHED",
                                Color.White,
                                4000);
                        }

                        break;
                }
            }
        }

        private void ShowNotification(string message, Color color, int duration = -1, bool dispose = true)
        {
            Notifications.Show(new SimpleNotification("Recall Tracker", message), 10000);
        }

        #endregion
    }

    internal class EnemyInfo
    {
        #region Fields

        public AIHeroClient Player;

        public RecallInfo RecallInfo;

        #endregion

        #region Constructors and Destructors

        public EnemyInfo(AIHeroClient player)
        {
            this.Player = player;
            this.RecallInfo = new RecallInfo(this);
        }

        #endregion
    }

    internal class RecallInfo
    {
        #region Fields

        public float EstimatedShootT;

        public int FADEOUT_TIME = 3000;

        public bool LockedTarget;

        private readonly EnemyInfo EnemyInfo;

        private Packet.S2C.Teleport.Struct AbortedRecall;

        private int AbortedT;

        private Packet.S2C.Teleport.Struct Recall;

        #endregion

        #region Constructors and Destructors

        public RecallInfo(EnemyInfo enemyInfo)
        {
            this.EnemyInfo = enemyInfo;
            this.Recall = new Packet.S2C.Teleport.Struct(
                this.EnemyInfo.Player.NetworkId,
                Packet.S2C.Teleport.Status.Unknown,
                Packet.S2C.Teleport.Type.Unknown,
                0);
        }

        #endregion

        #region Public Methods and Operators

        public int GetDrawTime()
        {
            var drawtime = 0;

            if (this.WasAborted())
            {
                drawtime = this.FADEOUT_TIME - (Utils.TickCount - this.AbortedT);
            }
            else
            {
                drawtime = this.GetRecallCountdown();
            }

            return drawtime < 0 ? 0 : drawtime;
        }

        public int GetRecallCountdown()
        {
            var time = Utils.TickCount;
            var countdown = 0;

            if (time - this.AbortedT < this.FADEOUT_TIME)
            {
                countdown = this.AbortedRecall.Duration - (this.AbortedT - this.AbortedRecall.Start);
            }
            else if (this.AbortedT > 0)
            {
                countdown = 0;
            }
            else
            {
                countdown = this.Recall.Start + this.Recall.Duration - time;
            }

            return countdown < 0 ? 0 : countdown;
        }

        public bool IsPorting()
        {
            return this.Recall.Type == Packet.S2C.Teleport.Type.Recall
                   && this.Recall.Status == Packet.S2C.Teleport.Status.Start;
        }

        public bool ShouldDraw()
        {
            return this.IsPorting() || (this.WasAborted() && this.GetDrawTime() > 0);
        }

        public override string ToString()
        {
            var drawtext = this.EnemyInfo.Player.ChampionName + ": " + this.Recall.Status;

            var countdown = this.GetRecallCountdown() / 1000f;

            if (countdown > 0)
            {
                drawtext += " (" + countdown.ToString("0.00", CultureInfo.InvariantCulture) + "s)";
            }

            return drawtext;
        }

        public EnemyInfo UpdateRecall(Packet.S2C.Teleport.Struct newRecall)
        {
            this.LockedTarget = false;
            this.EstimatedShootT = 0;

            if (newRecall.Type == Packet.S2C.Teleport.Type.Recall
                && newRecall.Status == Packet.S2C.Teleport.Status.Abort)
            {
                this.AbortedRecall = this.Recall;
                this.AbortedT = Utils.TickCount;
            }
            else
            {
                this.AbortedT = 0;
            }

            this.Recall = newRecall;
            return this.EnemyInfo;
        }

        public bool WasAborted()
        {
            return this.Recall.Type == Packet.S2C.Teleport.Type.Recall
                   && this.Recall.Status == Packet.S2C.Teleport.Status.Abort;
        }

        #endregion
    }
}
 