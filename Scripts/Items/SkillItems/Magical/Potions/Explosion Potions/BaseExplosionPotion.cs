using System;
using System.Collections;
using Server;
using Server.Network;
using Server.Targeting;
using Server.Spells;
using Server.Mobiles;
using Server.Custom;

namespace Server.Items
{
    public abstract class BaseExplosionPotion : BasePotion
    {
        public abstract int MinDamage { get; }
        public abstract int MaxDamage { get; }

        public static double CreatureDamageScalar = 3.0;

        public override bool RequireFreeHand { get { return false; } }

        private static bool LeveledExplosion = false; // Should explosion potions explode other nearby potions?
        private static bool InstantExplosion = false; // Should explosion potions explode on impact?
        private const int ExplosionRange = 2;     // How long is the blast radius?

        public BaseExplosionPotion(PotionEffect effect)
            : base(0xF0D, effect)
        {
        }

        public BaseExplosionPotion(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public virtual object FindParent(Mobile from)
        {
            Mobile m = this.HeldBy;

            if (m != null && m.Holding == this)
                return m;

            object obj = this.RootParent;

            if (obj != null)
                return obj;

            if (Map == Map.Internal)
                return from;

            return this;
        }

        private Timer m_Timer;

        private ArrayList m_Users;

        public override void Drink(Mobile from)
        {
            if (Core.AOS && (from.Paralyzed || from.Frozen || (from.Spell != null && from.Spell.IsCasting)))
            {
                from.SendLocalizedMessage(1062725); // You can not use a purple potion while paralyzed.
                return;
            }

            if (!from.BeginAction(typeof(BaseExplosionPotion)))
            {
                from.PrivateOverheadMessage(MessageType.Regular, 0x22, false, "You must wait a few seconds before using another explosion potion.", from.NetState);
                return;
            }

            ThrowTarget targ = from.Target as ThrowTarget;
            this.Stackable = false; // Scavenged explosion potions won't stack with those ones in backpack, and still will explode.

            if (targ != null && targ.Potion == this)
                return;

            from.RevealingAction();

            if (m_Users == null)
                m_Users = new ArrayList();

            if (!m_Users.Contains(from))
                m_Users.Add(from);

            from.Target = new ThrowTarget(this);

            if (m_Timer == null)
            {
                if (from.Region is UOACZRegion)
                    Timer.DelayCall(TimeSpan.FromSeconds(60.0), new TimerStateCallback(ReleaseExploLock), from);
                else
                    Timer.DelayCall(TimeSpan.FromSeconds(10.0), new TimerStateCallback(ReleaseExploLock), from);

                from.SendLocalizedMessage(500236); // You should throw it now!
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(0.75), TimeSpan.FromSeconds(1.0), 4, new TimerStateCallback(Detonate_OnTick), new object[] { from, 3 });
            }
        }

        private static void ReleaseExploLock(object state)
        {
            ((Mobile)state).EndAction(typeof(BaseExplosionPotion));
        }

        private void Detonate_OnTick(object state)
        {
            if (Deleted)
                return;

            object[] states = (object[])state;
            Mobile from = (Mobile)states[0];
            int timer = (int)states[1];

            object parent = FindParent(from);

            if (timer == 0)
            {
                Point3D loc;
                Map map;

                if (parent is Item)
                {
                    Item item = (Item)parent;

                    loc = item.GetWorldLocation();
                    map = item.Map;
                }
                else if (parent is Mobile)
                {
                    Mobile m = (Mobile)parent;

                    loc = m.Location;
                    map = m.Map;
                }
                else
                {
                    return;
                }

                Explode(from, true, loc, map);
                m_Timer = null;
            }
            else
            {
                if (parent is Item)
                    ((Item)parent).PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());
                else if (parent is Mobile)
                    ((Mobile)parent).PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());

                states[1] = timer - 1;
            }
        }

        private void Reposition_OnTick(object state)
        {
            if (Deleted)
                return;

            object[] states = (object[])state;
            Mobile from = (Mobile)states[0];
            Point3D p = (Point3D)states[1];
            Map map = (Map)states[2];

            if (InstantExplosion)
                Explode(from, true, p, map);
            else
                MoveToWorld(p, map);
        }

        private class ThrowTarget : Target
        {
            private BaseExplosionPotion m_Potion;

            public BaseExplosionPotion Potion
            {
                get { return m_Potion; }
            }

            public ThrowTarget(BaseExplosionPotion potion)
                : base(12, true, TargetFlags.None)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Potion.Deleted || m_Potion.Map == Map.Internal)
                    return;

                IPoint3D ip = targeted as IPoint3D;

                if (ip == null)
                    return;

                Map map = from.Map;

                if (map == null)
                    return;

                SpellHelper.GetSurfaceTop(ref ip);

                Point3D p = new Point3D(ip);

                from.RevealingAction();

                IEntity to;

                /*				if ( p is Mobile )
                                    to = (Mobile)p;
                                else
                */
                to = new Entity(Serial.Zero, new Point3D(p), map);

                Effects.SendMovingEffect(from, to, m_Potion.ItemID & 0x3FFF, 10, 0, false, false, m_Potion.Hue, 0);

                if (m_Potion.Amount > 1)
                {
                    Mobile.LiftItemDupe(m_Potion, 1);
                }

                m_Potion.Internalize();
                Timer.DelayCall(TimeSpan.FromSeconds(0.35), new TimerStateCallback(m_Potion.Reposition_OnTick), new object[] { from, p, map });
            }
        }

        public void Explode(Mobile from, bool direct, Point3D loc, Map map)
        {
            if (Deleted)
                return;

            // 12/8/13 Xiani - Nullifying an explosion when in an ArenaRegion.
            if (from.CombatProhibited)
                return;

            Consume();

            for (int i = 0; m_Users != null && i < m_Users.Count; ++i)
            {
                Mobile m = (Mobile)m_Users[i];
                ThrowTarget targ = m.Target as ThrowTarget;

                if (targ != null && targ.Potion == this)
                    Target.Cancel(m);
            }

            if (map == null)
                return;

            Effects.PlaySound(loc, map, 0x207);
            Effects.SendLocationEffect(loc, map, 0x36BD, 20);

            int alchemyBonus = 0;

            IPooledEnumerable eable = LeveledExplosion ? (IPooledEnumerable)map.GetObjectsInRange(loc, ExplosionRange) : (IPooledEnumerable)map.GetMobilesInRange(loc, ExplosionRange);
            ArrayList toExplode = new ArrayList();

            int toDamage = 0;

            foreach (object o in eable)
            {
                if (o is Mobile)
                {
                    toExplode.Add(o);
                    ++toDamage;
                }

                else if (o is BaseExplosionPotion && o != this)                
                    toExplode.Add(o);                

                if (o is BreakableContainer)                
                    toExplode.Add(o);
                
                if (o is BreakableStatic)                
                    toExplode.Add(o);                
            }

            eable.Free();

            int min = Scale(from, MinDamage);
            int max = Scale(from, MaxDamage);

            int baseDamage = Utility.RandomMinMax(min, max);

            for (int i = 0; i < toExplode.Count; ++i)
            {
                object o = toExplode[i];
                double divisor = 0;

                if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    if (from == null || (SpellHelper.ValidIndirectTarget(from, m) && from.CanBeHarmful(m, false)))
                    {
                        if (from != null)
                            from.DoHarmful(m);

                        int damage = baseDamage;

                        damage += alchemyBonus;

                        if (m is PlayerMobile)
                        {
                            if (m.InRange(loc, 0))
                                divisor = 1;
                            else if (m.InRange(loc, 1))
                                divisor = 2;
                            else if (m.InRange(loc, 2))
                                divisor = 4;

                            damage = (int)(damage / divisor);

                            if (damage > 40)
                                damage = 40;
                        }

                        else
                            damage = (int)((double)damage * CreatureDamageScalar);

                        AOS.Damage(m, from, damage, 0, 100, 0, 0, 0);
                    }
                }

                else if (o is BaseExplosionPotion)
                {
                    BaseExplosionPotion pot = (BaseExplosionPotion)o;

                    pot.Explode(from, false, pot.GetWorldLocation(), pot.Map);
                }

                else if (o is BreakableContainer)
                {
                   BreakableContainer breakableContainer = (BreakableContainer)o;

                   if (breakableContainer.ObjectBreakingDeviceDamageScalar == 0)
                       continue;

                   baseDamage = (int)((double)baseDamage * CreatureDamageScalar * breakableContainer.ObjectBreakingDeviceDamageScalar);
                        
                   breakableContainer.ReceiveDamage(from, baseDamage, BreakableContainer.InteractionType.ObjectBreakingDevice);
                }

                else if (o is BreakableStatic)
                {
                    BreakableStatic breakableStatic = (BreakableStatic)o;

                    if (breakableStatic.ObjectBreakingDeviceDamageScalar == 0)
                        continue;

                    baseDamage = (int)((double)baseDamage * CreatureDamageScalar * breakableStatic.ObjectBreakingDeviceDamageScalar);

                    breakableStatic.ReceiveDamage(from, baseDamage, BreakableStatic.InteractionType.ObjectBreakingDevice);
                }
            }
        }
    }
}