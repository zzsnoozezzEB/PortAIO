#region

using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using LeagueSharp.SDK.Core.UI;

#endregion

namespace Swiftly_Teemo.Main
{
    internal class MenuConfig
    {
        public static Menu menu, comboMenu, laneMenu, drawMenu;

        public static bool KillStealSummoner;
        public static bool LaneQ;
        public static bool dind;
        public static bool EngageDraw;
        public static bool Flee;

        public static bool getCheckBoxItem(Menu m, string item)
        {
            return m[item].Cast<CheckBox>().CurrentValue;
        }

        public static bool getKeyBindItem(Menu m, string item)
        {
            return m[item].Cast<KeyBind>().CurrentValue;
        }


        public static void Load()
        {

            menu = MainMenu.AddMenu("Swiftly Teemo", "Teemo");

            comboMenu = menu.AddSubMenu("Combo", "ComboMenu");
            comboMenu.Add("KillStealSummoner", new CheckBox("KillSteal Summoner", true));

            laneMenu = menu.AddSubMenu("Lane", "LaneMenu");
            laneMenu.Add("LaneQ", new CheckBox("Last Hit Q AA", true));

            drawMenu = menu.AddSubMenu("Draw", "Draw");
            drawMenu.Add("dind", new CheckBox("Damage Indicator", true));
            drawMenu.Add("EngageDraw", new CheckBox("Draw Engage", true));

            menu.Add("Flee", new KeyBind("Flee", false, KeyBind.BindTypes.HoldActive, 'Z'));

            KillStealSummoner = getCheckBoxItem(comboMenu, "KillStealSummoner");
            LaneQ = getCheckBoxItem(laneMenu, "asheqcombo");
            dind = getCheckBoxItem(drawMenu, "dind");
            EngageDraw = getCheckBoxItem(drawMenu, "EngageDraw");
            Flee = getKeyBindItem(menu, "Flee");

        }

    }
}
