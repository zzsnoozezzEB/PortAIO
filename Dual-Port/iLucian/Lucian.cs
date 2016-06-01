namespace iLucian
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DZLib.Core;
    using DZLib.Positioning;
    using iLucian.MenuHelper;
    using LeagueSharp.Common;
    using SharpDX;
    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Menu.Values;
    using EloBuddy.SDK.Menu;
    using iLucian.Utils;
    using ActiveGapcloser = DZLib.Core.ActiveGapcloser;
    using Color = System.Drawing.Color;
    using Prediction = LeagueSharp.Common.Prediction;
    using Geometry = LeagueSharp.Common.Geometry;


    class Lucian
    {
        #region Public Methods and Operators

        public static float GetComboDamage(Obj_AI_Base target)
        {
            float damage = 0;
            if (Variables.Spell[Variables.Spells.Q].IsReady())
                damage = damage + Variables.Spell[Variables.Spells.Q].GetDamage(target)
                         + (float)ObjectManager.Player.LSGetAutoAttackDamage(target);
            if (Variables.Spell[Variables.Spells.W].IsReady())
                damage = damage + Variables.Spell[Variables.Spells.W].GetDamage(target)
                         + (float)ObjectManager.Player.LSGetAutoAttackDamage(target);
            if (Variables.Spell[Variables.Spells.E].IsReady()) damage = damage + (float)ObjectManager.Player.LSGetAutoAttackDamage(target) * 2;

            damage = (float)(damage + ObjectManager.Player.LSGetAutoAttackDamage(target));

            return damage;
        }

        public void AutoHarass()
        {
            if (!getKeyBindItem(MenuGenerator.harassOptions, "com.ilucian.harass.auto.autoharass")) return;

            var target = TargetSelector.GetTarget(
                Variables.Spell[Variables.Spells.Q2].Range,
                DamageType.Physical);

            if (getCheckBoxItem(MenuGenerator.harassOptions, "com.ilucian.harass.auto.q") && Variables.Spell[Variables.Spells.Q].IsReady())
            {
                if (Variables.Spell[Variables.Spells.Q].IsReady() &&
                    Variables.Spell[Variables.Spells.Q].IsInRange(target) && target.LSIsValidTarget())
                {
                    Variables.Spell[Variables.Spells.Q].Cast(target);
                }
            }

            if (getCheckBoxItem(MenuGenerator.harassOptions, "com.ilucian.harass.auto.qExtended")
                && Variables.Spell[Variables.Spells.Q].IsReady())
            {
                CastExtendedQ();
            }
        }

        /// <summary>
        ///     Credits to Myo, stolen from him, ily :^)
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public Vector2 Deviation(Vector2 point1, Vector2 point2, double angle)
        {
            angle *= Math.PI / 180.0;
            var temp = Vector2.Subtract(point2, point1);
            var result = new Vector2(0)
            {
                X = (float)(temp.X * Math.Cos(angle) - temp.Y * Math.Sin(angle)) / 4,
                Y = (float)(temp.X * Math.Sin(angle) + temp.Y * Math.Cos(angle)) / 4
            };
            result = Vector2.Add(result, point1);
            return result;
        }

        public List<Obj_AI_Base> GetHittableTargets()
        {
            var unitList = new List<Obj_AI_Base>();
            var minions = MinionManager.GetMinions(
                ObjectManager.Player.Position,
                Variables.Spell[Variables.Spells.Q].Range);
            var champions =
                HeroManager.Enemies.Where(
                    x =>
                    ObjectManager.Player.LSDistance(x) <= Variables.Spell[Variables.Spells.Q].Range
                    && !x.HasBuffOfType(BuffType.SpellShield)
                    && !getCheckBoxItem(MenuGenerator.harassOptions, "com.ilucian.harass.whitelist." + x.ChampionName.ToLower()));

            unitList.AddRange(minions);

            /*if (Variables.Menu.IsEnabled("com.ilucian.misc.LSExtendChamps"))
            {
                unitList.AddRange(champions);
            }*/

            return unitList;
        }

        public void Killsteal()
        {
            var target =
                TargetSelector.GetTarget(
                    Variables.Spell[Variables.Spells.E].Range + Variables.Spell[Variables.Spells.Q2].Range,
                    DamageType.Physical);

            if (!getCheckBoxItem(MenuGenerator.miscOptions, "com.ilucian.misc.eqKs") || !Variables.Spell[Variables.Spells.Q].IsReady()
                || !target.LSIsValidTarget(
                    Variables.Spell[Variables.Spells.E].Range + Variables.Spell[Variables.Spells.Q2].Range))
            {
                return;
            }

            if (Variables.Spell[Variables.Spells.Q].GetDamage(target) - 20 >= target.Health)
            {
                if (target.LSIsValidTarget(Variables.Spell[Variables.Spells.Q].Range))
                {
                    Variables.Spell[Variables.Spells.Q].Cast(target);
                }

                if (target.LSIsValidTarget(Variables.Spell[Variables.Spells.Q2].Range)
                    && !target.LSIsValidTarget(Variables.Spell[Variables.Spells.Q].Range))
                {
                    CastExtendedQ();
                }
                else if (Variables.Spell[Variables.Spells.E].IsReady() && Variables.Spell[Variables.Spells.Q].IsReady())
                {
                    CastEqKillsteal();
                }
            }
        }

        public void OnLoad()
        {
            Console.WriteLine("Loaded Lucian");
            MenuGenerator.Generate();
            LoadSpells();
            LoadEvents();

            Chat.Print("[iLucian] -> Don't forget to upvote on assembly database.");
        }

        public void SemiUlt()
        {
            var target = TargetSelector.SelectedTarget != null
                             ? TargetSelector.SelectedTarget
                             : TargetSelector.GetTarget(
                                 Variables.Spell[Variables.Spells.R].Range,
                                 DamageType.Physical);
            if (target.IsValid && Variables.Spell[Variables.Spells.R].IsReady()
                && !ObjectManager.Player.HasBuff("LucianR"))
            {
                Variables.Spell[Variables.Spells.R].Cast(target.Position);
            }
        }

        public void UltimateLock()
        {
            var currentTarget = TargetSelector.SelectedTarget;
            if (currentTarget.LSIsValidTarget())
            {
                var predictedPosition = Variables.Spell[Variables.Spells.R].GetPrediction(currentTarget).UnitPosition;
                var directionVector = (currentTarget.ServerPosition - ObjectManager.Player.ServerPosition).Normalized();
                const float RRangeCoefficient = 0.95f;
                var rRangeAdjusted = Variables.Spell[Variables.Spells.R].Range * RRangeCoefficient;
                var rEndPointXCoordinate = predictedPosition.X + directionVector.X * rRangeAdjusted;
                var rEndPointYCoordinate = predictedPosition.Y + directionVector.Y * rRangeAdjusted;
                var rEndPoint = new Vector2(rEndPointXCoordinate, rEndPointYCoordinate).To3D();

                if (rEndPoint.LSDistance(ObjectManager.Player.ServerPosition) < Variables.Spell[Variables.Spells.R].Range)
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, rEndPoint);
                }
            }
        }

        #endregion
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
        #region Methods

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!getCheckBoxItem(MenuGenerator.miscOptions, "com.ilucian.misc.gapcloser"))
            {
                return;
            }

            if (!gapcloser.Sender.IsEnemy || !(gapcloser.End.LSDistance(ObjectManager.Player.ServerPosition) < 200))
                return;

            var extendedPosition = ObjectManager.Player.ServerPosition.LSExtend(
                Game.CursorPos,
                Variables.Spell[Variables.Spells.E].Range);
            if (extendedPosition.IsSafe(Variables.Spell[Variables.Spells.E].Range))
            {
                Variables.Spell[Variables.Spells.E].Cast(extendedPosition);
            }
        }

        private void CastE(Obj_AI_Base target)
        {
            // TODO check possible wall dashes :^)
                return;

            var dashRange = getSliderItem(MenuGenerator.comboOptions, "com.ilucian.combo.eRange");

            switch (getBoxItem(MenuGenerator.comboOptions, "com.ilucian.combo.eMode"))
            {
                case 0: // kite
                    var hypotheticalPosition = ObjectManager.Player.ServerPosition.LSExtend(
                        Game.CursorPos,
                        Variables.Spell[Variables.Spells.E].Range);
                    if (ObjectManager.Player.HealthPercent <= 70
                        && target.HealthPercent >= ObjectManager.Player.HealthPercent)
                    {
                        if (ObjectManager.Player.Position.LSDistance(ObjectManager.Player.ServerPosition) >= 35
                            && target.LSDistance(ObjectManager.Player.ServerPosition)
                            < target.LSDistance(ObjectManager.Player.Position)
                            && hypotheticalPosition.IsSafe(Variables.Spell[Variables.Spells.E].Range))
                        {
                            Variables.Spell[Variables.Spells.E].Cast(hypotheticalPosition);
                        }
                    }

                    if (hypotheticalPosition.IsSafe(Variables.Spell[Variables.Spells.E].Range)
                        && hypotheticalPosition.LSDistance(target.ServerPosition)
                        <= Orbwalking.GetRealAutoAttackRange(null)
                        && (hypotheticalPosition.LSDistance(target.ServerPosition) > 400) && !Variables.HasPassive())
                    {
                        Variables.Spell[Variables.Spells.E].Cast(hypotheticalPosition);
                    }

                    break;

                case 1: // side
                    Variables.Spell[Variables.Spells.E].Cast(
                        Deviation(ObjectManager.Player.Position.LSTo2D(), target.Position.LSTo2D(), dashRange).To3D());
                    break;

                case 2: // Cursor
                    if (Game.CursorPos.IsSafe(475))
                    {
                        Variables.Spell[Variables.Spells.E].Cast(
                            ObjectManager.Player.Position.LSExtend(Game.CursorPos, dashRange));
                    }

                    break;

                case 3: // Enemy
                    Variables.Spell[Variables.Spells.E].Cast(
                        ObjectManager.Player.Position.LSExtend(target.Position, dashRange));
                    break;
                case 4:
                    Variables.Spell[Variables.Spells.E].Cast(
                        Deviation(ObjectManager.Player.Position.LSTo2D(), target.Position.LSTo2D(), 65f).To3D());
                    break;
                case 5: // Smart E Credits to ASUNOOO
                    var ePosition = new EPosition();
                    var bestPosition = ePosition.GetEPosition();
                    if (bestPosition != Vector3.Zero
                        && bestPosition.LSDistance(target.ServerPosition) < Orbwalking.GetRealAutoAttackRange(target))
                    {
                        Variables.Spell[Variables.Spells.E].Cast(bestPosition);
                    }

                    break;
            }
        }

        private void CastEqKillsteal()
        {
            var target =
                TargetSelector.GetTarget(
                    Variables.Spell[Variables.Spells.E].Range + Variables.Spell[Variables.Spells.Q2].Range,
                    DamageType.Physical);

            if (
                !target.LSIsValidTarget(
                    Variables.Spell[Variables.Spells.E].Range + Variables.Spell[Variables.Spells.Q2].Range))
                return;

            var dashSpeed = (int)(Variables.Spell[Variables.Spells.E].Range / (700 + ObjectManager.Player.MoveSpeed));
            var extendedPrediction = GetExtendedPrediction(target, dashSpeed);

            var minions =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.IsEnemy && x.IsValid && x.LSDistance(extendedPrediction, true) < 900 * 900)
                    .OrderByDescending(x => x.LSDistance(extendedPrediction));

            foreach (var minion in
                minions.Select(x => Prediction.GetPrediction(x, dashSpeed))
                    .Select(
                        pred =>
                        MathHelper.GetCicleLineInteraction(
                            pred.UnitPosition.LSTo2D(),
                            extendedPrediction.LSTo2D(),
                            ObjectManager.Player.ServerPosition.LSTo2D(),
                            Variables.Spell[Variables.Spells.E].Range))
                    .Select(inter => inter.GetBestInter(target)))
            {
                if (Math.Abs(minion.X) < 1) return;

                if (!NavMesh.GetCollisionFlags(minion.To3D()).HasFlag(CollisionFlags.Wall)
                    && !NavMesh.GetCollisionFlags(minion.To3D()).HasFlag(CollisionFlags.Building)
                    && minion.To3D().IsSafe(Variables.Spell[Variables.Spells.E].Range))
                {
                    Variables.Spell[Variables.Spells.E].Cast((Vector3)minion);
                }
            }

            var champions =
                ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.IsEnemy && x.IsValid && x.LSDistance(extendedPrediction, true) < 900 * 900)
                    .OrderByDescending(x => x.LSDistance(extendedPrediction));

            if (getCheckBoxItem(MenuGenerator.miscOptions, "com.ilucian.misc.useChampions"))
            {
                foreach (var position in
                    champions.Select(x => Prediction.GetPrediction(x, dashSpeed))
                        .Select(
                            pred =>
                            MathHelper.GetCicleLineInteraction(
                                pred.UnitPosition.LSTo2D(),
                                extendedPrediction.LSTo2D(),
                                ObjectManager.Player.ServerPosition.LSTo2D(),
                                Variables.Spell[Variables.Spells.E].Range))
                        .Select(inter => inter.GetBestInter(target)))
                {
                    if (Math.Abs(position.X) < 1) return;

                    if (!NavMesh.GetCollisionFlags(position.To3D()).HasFlag(CollisionFlags.Wall)
                        && !NavMesh.GetCollisionFlags(position.To3D()).HasFlag(CollisionFlags.Building)
                        && position.To3D().IsSafe(Variables.Spell[Variables.Spells.E].Range))
                    {
                        Variables.Spell[Variables.Spells.E].Cast((Vector3)position);
                    }
                }
            }
        }

        private void CastExtendedQ()
        {
            if (!Variables.Spell[Variables.Spells.Q].IsReady())
            {
                return;
            }

            var target = TargetSelector.SelectedTarget != null
                         && TargetSelector.SelectedTarget.LSDistance(ObjectManager.Player) < 1800
                             ? TargetSelector.SelectedTarget
                             : TargetSelector.GetTarget(
                                 Variables.Spell[Variables.Spells.Q2].Range,
                                 DamageType.Physical);

            var predictionPosition = Variables.Spell[Variables.Spells.Q2].GetPrediction(target);

            foreach (var unit in from unit in GetHittableTargets()
                                 let polygon =
                                     new Geometry.Polygon.Rectangle(
                                     ObjectManager.Player.ServerPosition,
                                     ObjectManager.Player.ServerPosition.LSExtend(
                                         unit.ServerPosition,
                                         Variables.Spell[Variables.Spells.Q2].Range),
                                     65f)
                                 where polygon.IsInside(predictionPosition.CastPosition)
                                 select unit)
            {
                Variables.Spell[Variables.Spells.Q].Cast(unit);
            }
        }

        // Detuks ofc
        private Vector3 GetExtendedPrediction(AIHeroClient target, int delay)
        {
            var res = Variables.Spell[Variables.Spells.Q2].GetPrediction(target);
            var del = Prediction.GetPrediction(target, delay);

            var dif = del.UnitPosition - target.ServerPosition;
            return res.CastPosition + dif;
        }

        private void LoadEvents()
        {
            {
                SemiUlt();
            }

            AutoHarass();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                OnCombo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {

        }

        {



            {





            var target = TargetSelector.GetTarget(Variables.Spell[Variables.Spells.Q2].Range, DamageType.Physical);

            if (target == null || Variables.HasPassive())
                return;
            if (getCheckBoxItem(MenuGenerator.comboOptions, "com.ilucian.combo.qExtended"))
            {
                CastExtendedQ(ch);
            }
            if (target.LSIsValidTarget(Variables.Spell[Variables.Spells.Q].Range) && getCheckBoxItem(MenuGenerator.comboOptions, "com.ilucian.combo.q"))
            {
                if (Variables.Spell[Variables.Spells.Q].IsReady() && Variables.Spell[Variables.Spells.Q].IsInRange(target))
                {
                    Variables.Spell[Variables.Spells.Q].Cast(target);
                }
            }
            if (ObjectManager.Player.LSIsDashing() || !getCheckBoxItem(MenuGenerator.comboOptions, "com.ilucian.combo.w")) return;
            if (!Variables.Spell[Variables.Spells.W].IsReady()) return;

            if (getCheckBoxItem(MenuGenerator.miscOptions, "com.ilucian.misc.usePrediction"))
            {
                var prediction = Variables.Spell[Variables.Spells.W].GetPrediction(target);
                if (prediction.Hitchance >= HitChance.High)
                {
                    Variables.Spell[Variables.Spells.W].Cast(prediction.CastPosition);
                }
            }
            else
            {
                if (target.LSDistance(ObjectManager.Player) < 600)
                {
                    Variables.Spell[Variables.Spells.W].Cast(target.Position);
                }
            }
        }

        private void OnHarass()
        {

            if (getCheckBoxItem(MenuGenerator.harassOptions, "com.ilucian.harass.qExtended"))
            {
            }

            if (target.LSIsValidTarget(Variables.Spell[Variables.Spells.Q].Range) && getCheckBoxItem(MenuGenerator.harassOptions, "com.ilucian.harass.q"))
            {
                {
                    Variables.Spell[Variables.Spells.Q].Cast(target);
                }
            }
            if (!ObjectManager.Player.LSIsDashing() && getCheckBoxItem(MenuGenerator.harassOptions, "com.ilucian.harass.w"))
            {
                if (Variables.Spell[Variables.Spells.W].IsReady())
                {
                    if (getCheckBoxItem(MenuGenerator.miscOptions, "com.ilucian.misc.usePrediction"))
                    {
                        var prediction = Variables.Spell[Variables.Spells.W].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            Variables.Spell[Variables.Spells.W].Cast(prediction.CastPosition);
                        }
                    }
                    else
                    {
                        if (target.LSDistance(ObjectManager.Player) < 600)
                        {
                            Variables.Spell[Variables.Spells.W].Cast(target.Position);
                        }
                    }
                }
            }
        }

        private void OnJungleclear()
        {

            if (jungleMob != null)
            {
                if (Variables.Spell[Variables.Spells.Q].IsReady() &&
                    getCheckBoxItem(MenuGenerator.jungleclearOptions, "com.ilucian.jungleclear.q") && !Variables.HasPassive())
                {
                    Variables.Spell[Variables.Spells.Q].Cast(jungleMob);
                }
                if (Variables.Spell[Variables.Spells.W].IsReady() &&
                    getCheckBoxItem(MenuGenerator.jungleclearOptions, "com.ilucian.jungleclear.w") && !Variables.HasPassive())
                {
                    Variables.Spell[Variables.Spells.W].Cast(jungleMob);
                }
                if (Variables.Spell[Variables.Spells.E].IsReady() &&
                    getCheckBoxItem(MenuGenerator.jungleclearOptions, "com.ilucian.jungleclear.e") && !Variables.HasPassive())
                {
                    Variables.Spell[Variables.Spells.E].Cast(ObjectManager.Player.Position.LSExtend(Game.CursorPos, 475));
                }
            }
        }

        private void OnLaneclear()
        {
            if (getCheckBoxItem(MenuGenerator.laneclearOptions, "com.ilucian.laneclear.q"))
            {
                var minions = MinionManager.GetMinions(Variables.Spell[Variables.Spells.Q].Range);
                var bestLocation = Variables.Spell[Variables.Spells.Q].GetCircularFarmLocation(minions, 60);

                    return;
                var adjacentMinions = minions.Where(m => m.LSDistance(bestLocation.Position) <= 45).ToList();
                if (!adjacentMinions.Any())
                {
                    return;
                }

                var firstMinion = adjacentMinions.OrderBy(m => m.LSDistance(bestLocation.Position)).First();

                if (!Variables.HasPassive() && Orbwalking.InAutoAttackRange(firstMinion))
                {
                    Variables.Spell[Variables.Spells.Q].Cast(firstMinion);
                }
            }
        }

        {
            {

            }
        }

        {



            {
                {
                }
            }

            {

        }
