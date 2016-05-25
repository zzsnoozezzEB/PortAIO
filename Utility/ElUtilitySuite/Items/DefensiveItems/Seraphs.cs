namespace ElUtilitySuite.Items.DefensiveItems
{
    using System;

    using LeagueSharp;
    using LeagueSharp.Common;

    using ItemData = LeagueSharp.Common.Data.ItemData;
    using EloBuddy;
    using EloBuddy.SDK.Menu.Values;
    internal class Seraphs : Item
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        public Seraphs()
        {
            AttackableUnit.OnDamage += this.AttackableUnit_OnDamage;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the identifier.
        /// </summary>
        /// <value>
        ///     The identifier.
        /// </value>
        public override ItemId Id
        {
            get
            {
                return (ItemId)3040;
            }
        }

        /// <summary>
        ///     Gets or sets the name of the item.
        /// </summary>
        /// <value>
        ///     The name of the item.
        /// </value>
        public override string Name
        {
            get
            {
                return "Seraph's embrace";
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Creates the menu.
        /// </summary>
        public override void CreateMenu()
        {
            this.Menu.AddGroupLabel(Name);
            this.Menu.Add("UseSeraphsCombo", new CheckBox("Activated"));
            this.Menu.Add("ModeSERAPH", new ComboBox("Activation mode: ", 1, "Use always", "Use in combo"));
            this.Menu.Add("Seraphs.HP", new Slider("Health percentage", 20, 1));
            this.Menu.Add("Seraphs.Damage", new Slider("Incoming damage percentage", 20, 1));
            this.Menu.AddSeparator();
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AttackableUnit_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            try
            {
                if (!ItemData.Seraphs_Embrace.GetItem().IsOwned() || !getCheckBoxItem(this.Menu, "UseSeraphsCombo"))
                {
                    return;
                }

                if (getBoxItem(this.Menu, "ModeSERAPH") == 1 && !this.ComboModeActive)
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
                if (hero.IsEnemy)
                {
                    return;
                }

                if (((int)(args.Damage / hero.Health) > getSliderItem(this.Menu, "Seraphs.Damage"))
                   || (hero.HealthPercent < getSliderItem(this.Menu, "Seraphs.HP")))
                {
                    Items.UseItem((int)this.Id);
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