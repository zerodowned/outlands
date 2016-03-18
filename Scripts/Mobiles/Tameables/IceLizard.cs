using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
	[CorpseName( "an ice lizard corpse" )]
	public class IceLizard : BaseCreature
	{
        public override bool CanBeResurrectedThroughVeterinary { get { return false; } }

		[Constructable]
		public IceLizard() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "an ice lizard";
			Body = 0xCE;
			Hue = 2590;

			BaseSoundID = 0x5A;

            SetStr(75);
            SetDex(25);
            SetInt(25);

            SetHits(200);

            SetDamage(8, 16);

            SetSkill(SkillName.Wrestling, 70);
            SetSkill(SkillName.Tactics, 100);

            SetSkill(SkillName.MagicResist, 75);

            VirtualArmor = 25;

			Fame = 3000;
			Karma = -3000;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 90;

            PackItem(new SulfurousAsh(Utility.Random(6, 12)));
        }

        public override int Meat { get { return 2; } }
        public override int Hides { get { return 12; } }
        public override HideType HideType { get { return HideType.Spined; } }

        //Animal Lore Display Info
        public override int TamedItemId { get { return 8497; } }
        public override int TamedItemHue { get { return Hue; } }
        public override int TamedItemXOffset { get { return 5; } }
        public override int TamedItemYOffset { get { return 15; } }

        //Dynamic Stats and Skills (Scale Up With Creature XP)
        public override int TamedBaseMaxHits { get { return 225; } }
        public override int TamedBaseMinDamage { get { return 9; } }
        public override int TamedBaseMaxDamage { get { return 11; } }
        public override double TamedBaseWrestling { get { return 85; } }
        public override double TamedBaseEvalInt { get { return 0; } }

        //Static Stats and Skills (Do Not Scale Up With Creature XP)
        public override int TamedBaseStr { get { return 5; } }
        public override int TamedBaseDex { get { return 25; } }
        public override int TamedBaseInt { get { return 5; } }
        public override int TamedBaseMaxMana { get { return 0; } }
        public override double TamedBaseMagicResist { get { return 50; } }
        public override double TamedBaseMagery { get { return 0; } }
        public override double TamedBasePoisoning { get { return 0; } }
        public override double TamedBaseTactics { get { return 100; } }
        public override double TamedBaseMeditation { get { return 0; } }
        public override int TamedBaseVirtualArmor { get { return 75; } }

        public override void SetUniqueAI()
        {
            DictCombatAction[CombatAction.CombatSpecialAction] = 3;
            DictCombatSpecialAction[CombatSpecialAction.IceBreathAttack] = 1;
        }

        public override void SetTamedAI()
        {
            DictCombatAction[CombatAction.CombatSpecialAction] = 3;
        }

        public IceLizard(Serial serial): base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();
		}
	}
}