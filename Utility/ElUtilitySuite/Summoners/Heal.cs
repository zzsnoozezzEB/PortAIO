namespace ElUtilitySuite.Summoners
{
    using System;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;
    using EloBuddy.SDK.Menu;
    using EloBuddy;
    using EloBuddy.SDK.Menu.Values;
    public class Heal : IPlugin
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the heal spell.
        /// </summary>
        /// <value>
        ///     The heal spell.
        /// </value>
        public Spell HealSpell { get; set; }

        /// <summary>
        /// The Menu
        /// </summary>
        public Menu Menu { get; set; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private AIHeroClient Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

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
            if (this.Player.GetSpellSlot("summonerheal") == SpellSlot.Unknown)
            {
                return;
            }

            var healMenu = rootMenu.AddSubMenu("Heal", "Heal");
            {
                healMenu.Add("Heal.Activated", new CheckBox("Heal"));
                healMenu.Add("PauseHealHotkey", new KeyBind("Don't use heal key", false, KeyBind.BindTypes.HoldActive, 'L'));
                healMenu.Add("Heal.HP", new Slider("Health percentage", 20, 1));
                healMenu.Add("Heal.Damage", new Slider("Heal on % incoming damage", 20, 1));
                foreach (var x in ObjectManager.Get<AIHeroClient>().Where(x => x.IsAlly))
                {
                    healMenu.Add("healon" + x.ChampionName, new Slider("Use for " + x.ChampionName));
                }
            }

            this.Menu = healMenu;
        }

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        public void Load()
        {
            try
            {
                var healSlot = this.Player.GetSpellSlot("summonerheal");

                if (healSlot == SpellSlot.Unknown)
                {
                    return;
                }

                this.HealSpell = new Spell(healSlot, 550);

                AttackableUnit.OnDamage += this.AttackableUnit_OnDamage;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        #endregion

        #region Methods

        private void AttackableUnit_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            try
            {
                if (!getCheckBoxItem(this.Menu, "Heal.Activated"))
                {
                    return;
                }

                if (getKeyBindItem(this.Menu, "PauseHealHotkey"))
                {
                    return;
                }

                var source = ObjectManager.GetUnitByNetworkId<GameObject>((uint)args.Source.NetworkId);
                var obj = ObjectManager.GetUnitByNetworkId<GameObject>((uint)args.Target.NetworkId);

                if (obj.Type != GameObjectType.AIHeroClient || source.Type != GameObjectType.AIHeroClient)
                {
                    return;
                }

                var hero = (AIHeroClient)obj;

                if (hero.IsEnemy || (!hero.IsMe && !this.HealSpell.IsInRange(obj))
                    || !getCheckBoxItem(this.Menu, string.Format("healon{0}", hero.ChampionName)))
                {
                    return;
                }

                if (((int)(args.Damage / hero.Health) > getSliderItem(this.Menu, "Heal.Damage"))
                    || (hero.HealthPercent < getSliderItem(this.Menu, "Heal.HP")))
                {
                    this.Player.Spellbook.CastSpell(this.HealSpell.Slot);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        #endregion
    }
}
