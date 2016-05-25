#pragma warning disable 618
namespace ElUtilitySuite.Trackers
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using PortAIO.Properties;
    using ElUtilitySuite.Vendor.SFX;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;
    using SharpDX.Direct3D9;

    using Color = SharpDX.Color;
    using Font = SharpDX.Direct3D9.Font;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy;
    internal class LastPositionTracker : IPlugin
    {
        #region Fields

        /// <summary>
        ///     The hero texture images
        /// </summary>
        private readonly Dictionary<int, Texture> heroTextures = new Dictionary<int, Texture>();

        /// <summary>
        ///     The hero last positions
        /// </summary>
        private readonly List<LastPositionStruct> lastPositions = new List<LastPositionStruct>();

        /// <summary>
        ///    The Line drawings
        /// </summary>
        private Line line;

        /// <summary>
        ///     The enemy spawningpoint
        /// </summary>
        private Vector3 spawnPoint;

        /// <summary>
        ///     Spire drawings
        /// </summary>
        private Sprite sprite;

        /// <summary>
        ///     Teleport texture images
        /// </summary>
        private Texture teleportTexture;

        /// <summary>
        ///     Drawing font face
        /// </summary>
        private Font text;


        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the menu.
        /// </summary>
        /// <value>
        ///     The menu.
        /// </value>
        public Menu Menu { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Creates the menu.
        /// </summary>
        /// <param name="rootMenu">The root menu.</param>
        /// <returns></returns>
        public void CreateMenu(Menu rootMenu)
        {
            var ssMenu = rootMenu.AddSubMenu("Last position tracker", "lastpostracker");
            {
                ssMenu.Add("LastPosition.CircleThickness", new Slider("Circle Thickness", 1, 1, 10));
                ssMenu.Add("LastPosition.TimeFormat", new ComboBox("Time Format", 0, "mm:ss", "ss"));
                ssMenu.Add("LastPosition.FontSize", new Slider("Font Size", 13, 3, 30));
                ssMenu.Add("LastPosition.SSTimerOffset", new Slider("SS Timer Offset", 5, 0, 20));
                ssMenu.Add("LastPosition.SSTimer", new CheckBox("SS Timer", false));
                ssMenu.Add("LastPosition.SSCircle", new CheckBox("SS Circle", false));
                ssMenu.Add("LastPosition.Minimap", new CheckBox("Minimap"));
                ssMenu.Add("LastPosition.Map", new CheckBox("Map"));
                ssMenu.Add("LastPosition.Enabled", new CheckBox("Enabled"));
            }

            this.Menu = ssMenu;
        }

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
        ///     Loads this instance.
        /// </summary>
        public void Load()
        {
            if (!HeroManager.Enemies.Any())
            {
                return;
            }

            this.teleportTexture = Resources.LP_Teleport.ToTexture();

            var spawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);
            this.spawnPoint = spawn != null ? spawn.Position : Vector3.Zero;

            foreach (var enemy in HeroManager.Enemies)
            {
                this.heroTextures[enemy.NetworkId] =
                    (ImageLoader.Load("LP", enemy.ChampionName) ?? Resources.LP_Default).ToTexture();
                var eStruct = new LastPositionStruct(enemy) { LastPosition = this.spawnPoint };
                this.lastPositions.Add(eStruct);
            }

            Drawing.OnEndScene += this.OnDrawingEndScene;
            Obj_AI_Base.OnTeleport += this.OnObjAiBaseTeleport;

            Drawing.OnPreReset += args => { this.text.OnLostDevice(); };
            Drawing.OnPostReset += args => { this.text.OnResetDevice(); };

            this.sprite = MDrawing.GetSprite();
            this.text = MDrawing.GetFont(getSliderItem(this.Menu, "LastPosition.FontSize"));
            this.line = MDrawing.GetLine(1);
        }

        #endregion

        #region Methods

        private void DrawCircleMinimap(Vector3 center, float radius, Color color, int thickness = 5, int quality = 30)
        {
            var sharpColor = new ColorBGRA(color.R, color.G, color.B, 255);
            var pointList = new List<Vector3>();
            for (var i = 0; i < quality; i++)
            {
                var angle = i * Math.PI * 2 / quality;
                pointList.Add(
                    new Vector3(
                        center.X + radius * (float)Math.Cos(angle),
                        center.Y + radius * (float)Math.Sin(angle),
                        center.Z));
            }
            this.line.Width = thickness;
            this.line.Begin();
            for (var i = 0; i < pointList.Count; i++)
            {
                var a = pointList[i];
                var b = pointList[i == pointList.Count - 1 ? 0 : i + 1];

                var aonScreen = Drawing.WorldToMinimap(a);
                var bonScreen = Drawing.WorldToMinimap(b);

                this.line.Draw(new[] { aonScreen, bonScreen }, sharpColor);
            }
            this.line.End();
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed ||
                    !getCheckBoxItem(this.Menu, "LastPosition.Enabled"))
                {
                    return;
                }

                var map = getCheckBoxItem(this.Menu, "LastPosition.Map");
                var minimap = getCheckBoxItem(this.Menu, "LastPosition.Minimap");
                var ssCircle = getCheckBoxItem(this.Menu, "LastPosition.SSCircle");
                var circleThickness = getSliderItem(this.Menu, "LastPosition.CircleThickness");
                //var circleColor = this.Menu.Item("LastPosition.CircleColor").IsActive();
                var totalSeconds = getBoxItem(this.Menu, "LastPosition.TimeFormat") == 1;
                var timerOffset = getSliderItem(this.Menu, "LastPosition.SSTimerOffset");
                var timer = getCheckBoxItem(this.Menu, "LastPosition.SSTimer");


                this.sprite.Begin(SpriteFlags.AlphaBlend);
                foreach (var lp in this.lastPositions)
                {
                    if (!lp.Hero.IsDead && !lp.LastPosition.Equals(Vector3.Zero)
                        && lp.LastPosition.LSDistance(lp.Hero.Position) > 500)
                    {
                        lp.Teleported = false;
                        lp.LastSeen = Game.Time;
                    }
                    lp.LastPosition = lp.Hero.Position;
                    if (lp.Hero.IsVisible)
                    {
                        lp.Teleported = false;
                        if (!lp.Hero.IsDead)
                        {
                            lp.LastSeen = Game.Time;
                        }
                    }
                    if (!lp.Hero.IsVisible && !lp.Hero.IsDead)
                    {
                        var pos = lp.Teleported ? this.spawnPoint : lp.LastPosition;
                        var mpPos = Drawing.WorldToMinimap(pos);
                        var mPos = Drawing.WorldToScreen(pos);

                        if (ssCircle && !lp.LastSeen.Equals(0f) && Game.Time - lp.LastSeen > 3f)
                        {
                            var radius = Math.Abs((Game.Time - lp.LastSeen - 1) * lp.Hero.MoveSpeed * 0.9f);
                            if (radius <= 8000)
                            {
                                if (map && pos.IsOnScreen(50))
                                {
                                    Render.Circle.DrawCircle(
                                        pos,
                                        radius,
                                        System.Drawing.Color.White,
                                        circleThickness,
                                        true);
                                }
                                if (minimap)
                                {
                                    this.DrawCircleMinimap(pos, radius, Color.White, circleThickness);
                                }
                            }
                        }

                        if (map && pos.IsOnScreen(50))
                        {
                            this.sprite.DrawCentered(this.heroTextures[lp.Hero.NetworkId], mPos);
                        }
                        if (minimap)
                        {
                            this.sprite.DrawCentered(this.heroTextures[lp.Hero.NetworkId], mpPos);
                        }

                        if (lp.IsTeleporting)
                        {
                            if (map && pos.IsOnScreen(50))
                            {
                                this.sprite.DrawCentered(this.teleportTexture, mPos);
                            }
                            if (minimap)
                            {
                                this.sprite.DrawCentered(this.teleportTexture, mpPos);
                            }
                        }

                        if (timer && !lp.LastSeen.Equals(0f) && Game.Time - lp.LastSeen > 3f)
                        {
                            var time = (Game.Time - lp.LastSeen).FormatTime(totalSeconds);
                            if (map && pos.IsOnScreen(50))
                            {
                                this.text.DrawTextCentered(
                                    time,
                                    new Vector2(mPos.X, mPos.Y + 15 + timerOffset),
                                    Color.White);
                            }
                            if (minimap)
                            {
                                this.text.DrawTextCentered(
                                    time,
                                    new Vector2(mpPos.X, mpPos.Y + 15 + timerOffset),
                                    Color.White);
                            }
                        }
                    }
                }
                this.sprite.End();
            }
            catch (Exception e)
            {
                Console.WriteLine(@"An error occurred: '{0}'", e);
            }
        }

        private void OnObjAiBaseTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                if (!getCheckBoxItem(this.Menu, "LastPosition.Enabled"))
                {
                    return;
                }

                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                var lastPosition = this.lastPositions.FirstOrDefault(e => e.Hero.NetworkId == packet.UnitNetworkId);
                if (lastPosition != null)
                {
                    switch (packet.Status)
                    {
                        case Packet.S2C.Teleport.Status.Start:
                            lastPosition.IsTeleporting = true;
                            break;
                        case Packet.S2C.Teleport.Status.Abort:
                            lastPosition.IsTeleporting = false;
                            break;
                        case Packet.S2C.Teleport.Status.Finish:
                            lastPosition.Teleported = true;
                            lastPosition.IsTeleporting = false;
                            lastPosition.LastSeen = Game.Time;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"An error occurred: '{0}'", e);
            }
        }

        #endregion

        internal class LastPositionStruct
        {
            #region Constructors and Destructors

            public LastPositionStruct(AIHeroClient hero)
            {
                this.Hero = hero;
                this.LastPosition = Vector3.Zero;
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     The hero
            /// </summary>
            public AIHeroClient Hero { get; private set; }

            /// <summary>
            ///     Hero busy teleporting
            /// </summary>
            public bool IsTeleporting { get; set; }

            /// <summary>
            ///     The last hero position
            /// </summary>
            public Vector3 LastPosition { get; set; }

            /// <summary>
            ///     The last seen position
            /// </summary>
            public float LastSeen { get; set; }

            /// <summary>
            ///     Hero teleported
            /// </summary>
            public bool Teleported { get; set; }

            #endregion
        }
    }
}