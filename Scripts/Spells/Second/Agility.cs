using System;
using Server.Targeting;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Spells.Second
{
	public class AgilitySpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Agility", "Ex Uus",
				212,
				9061,
				Reagent.Bloodmoss,
				Reagent.MandrakeRoot
			);

		public override SpellCircle Circle { get { return SpellCircle.Second; } }

		public AgilitySpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			if ( Engines.ConPVP.DuelContext.CheckSuddenDeath( Caster ) )
			{
				Caster.SendMessage( 0x22, "You cannot cast this spell when in sudden death." );
				return false;
			}

			return base.CheckCast();
		}

		public override void OnCast()
		{
            BaseCreature casterCreature = Caster as BaseCreature;

            if (casterCreature != null)
            {
                if (casterCreature.SpellTarget != null)
                {
                    this.Target(casterCreature.SpellTarget);
                }
            }

            else
            {
                Caster.Target = new InternalTarget(this);
            }
		}

		public void Target( Mobile m )
		{
            if (!Caster.CanSee(m) || m.Hidden)
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}

			else if ( CheckBSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				SpellHelper.AddStatBonus( Caster, m, StatType.Dex );

                bool enhancedSpellcast = SpellHelper.IsEnhancedSpell(Caster, m, EnhancedSpellbookType.Wizard, false, true);

                if (enhancedSpellcast)
                {
                    m.FixedParticles(0x375A, 10, 30, 5010, EffectLayer.Waist);
                    m.PlaySound(0x28E);
                }

                else
                {
                    m.FixedParticles(0x375A, 10, 15, 5010, EffectLayer.Waist);
                    m.PlaySound(0x28E);
                }
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private AgilitySpell m_Owner;

			public InternalTarget( AgilitySpell owner ) : base( 12, false, TargetFlags.Beneficial )
			{
				m_Owner = owner;
			} 

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
				{
					m_Owner.Target( (Mobile)o );
				}
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}