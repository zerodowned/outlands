using System;
using Server;
using Server.Misc;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.SkillHandlers
{
	public class Snooping
	{
		public static void Configure()
		{
			Container.SnoopHandler = new ContainerSnoopHandler( Container_Snoop );
		}

		public static void ReleaseSnoopingLock_Callback( object state )
		{ 
			((Mobile)state).EndAction( typeof( Snooping ) ); 
		}

		public static bool CheckSnoopAllowed( Mobile from, Mobile to )
		{
		    Map map = from.Map;

			if ( to.Player )
				return from.CanBeHarmful( to, false, true );

			if ( map != null && (map.Rules & MapRules.HarmfulRestrictions) == 0 )
				return true;

			GuardedRegion reg = (GuardedRegion) to.Region.GetRegion( typeof( GuardedRegion ) );

			if ( reg == null || reg.IsDisabled() )
				return true;

			BaseCreature cret = to as BaseCreature;

			if ( to.Body.IsHuman && (cret == null || (!cret.AlwaysAttackable && !cret.IsMurderer())) )
				return false;

			return true;
		}

		public static void Container_Snoop( Container cont, Mobile from )
		{
			if (!from.BeginAction(typeof(Snooping)))
                return;

			Timer.DelayCall( TimeSpan.FromSeconds( SkillCooldown.SnoopingCooldown ), new TimerStateCallback( ReleaseSnoopingLock_Callback ), from );

			if ( from.AccessLevel > AccessLevel.Player || from.InRange( cont.GetWorldLocation(), 1 ) )
			{
				Mobile root = cont.RootParent as Mobile;

				if ( root != null && !root.Alive )
					return;

				if ( root != null && root.AccessLevel > AccessLevel.Player && from.AccessLevel == AccessLevel.Player )
				{
					from.SendLocalizedMessage( 500209 ); // You can not peek into the container.
					return;
				}

				if ( root != null && from.AccessLevel == AccessLevel.Player && !CheckSnoopAllowed( from, root ) )
				{
					from.SendLocalizedMessage( 1001018 ); // You cannot perform negative acts on your target.
					return;
				}

                double noticeChance = 1.0 - (from.Skills[SkillName.Snooping].Value / 100);

                if (noticeChance > .5)
                    noticeChance = .5;

                if (noticeChance < .05)
                    noticeChance = .05;

				if (Utility.RandomDouble() <= noticeChance && root != null && from.AccessLevel == AccessLevel.Player)
				{
					Map map = from.Map;

					if ( map != null )
					{
						string message = String.Format( "You notice {0} attempting to peek into {1}'s belongings.", from.Name, root.Name );

						IPooledEnumerable eable = map.GetClientsInRange( from.Location, 8 );

						foreach ( NetState ns in eable )
						{
							if ( ns.Mobile != from )
								ns.Mobile.SendMessage( message );
						}

						eable.Free();
					}
				}

				if ( from.AccessLevel == AccessLevel.Player )
					FameKarmaTitles.AwardKarma( from, -4, true );

                from.CheckTargetSkill(SkillName.Snooping, cont, 0.0, 100.0, 1.0);

                double stealingSkill = from.Skills[SkillName.Snooping].Value / 100;
                double successChance = stealingSkill * .75;

                if (from.Hidden)
                {
                    double hidingBonus = .25;

                    if (hidingBonus > stealingSkill)
                        hidingBonus = stealingSkill;

                    successChance += hidingBonus;
                }

                if (from.LastCombatTime + TimeSpan.FromSeconds(30) > DateTime.UtcNow)
                    successChance -= .25;

                bool successful = (Utility.RandomDouble() <= successChance);

                if (from.AccessLevel > AccessLevel.Player || successful)
				{
					if ( cont is TrapableContainer && ((TrapableContainer)cont).ExecuteTrap( from ) )
						return;

					cont.DisplayTo( from );
				}

				else				
					from.SendLocalizedMessage( 500210 ); // You failed to peek into the container.				
			}

			else			
				from.SendLocalizedMessage( 500446 ); // That is too far away.			
		}
	}
}