using System;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x13dc, 0x13d4 )]
	public class StuddedArms : BaseArmor
	{
		public override int BasePhysicalResistance{ get{ return 2; } }
		public override int BaseFireResistance{ get{ return 4; } }
		public override int BaseColdResistance{ get{ return 3; } }
		public override int BasePoisonResistance{ get{ return 3; } }
		public override int BaseEnergyResistance{ get{ return 4; } }

		public override int InitMinHits{ get{ return 35; } }       
 		public override int InitMaxHits{ get{ return 44; } }

		public override int AosStrReq{ get{ return 25; } }
		public override int OldStrReq{ get{ return 25; } }

        public override int RevertArmorBase{ get{ return 2; } }

		public override ArmorMaterialType MaterialType{ get{ return ArmorMaterialType.Studded; } }
		public override CraftResource DefaultResource{ get{ return CraftResource.RegularLeather; } }

        public override int ArmorBase { get { return 20; } }
        public override int OldDexBonus { get { return 0; } }

        public override int IconItemId { get { return 5076; } }
        public override int IconHue { get { return Hue; } }
        public override int IconOffsetX { get { return 0; } }
        public override int IconOffsetY { get { return 5; } }

        public override ArmorMeditationAllowance DefMedAllowance { get { return ArmorMeditationAllowance.ThreeQuarter; } }

		[Constructable]
		public StuddedArms() : base( 5076 )
		{
            Name = "studded arms";
			Weight = 4.0;
		}

		public StuddedArms( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( Weight == 1.0 )
				Weight = 4.0;
		}
	}
}