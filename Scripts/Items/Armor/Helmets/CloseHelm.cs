using System;
using Server;

namespace Server.Items
{
	public class CloseHelm : BaseArmor
	{
		public override int BasePhysicalResistance{ get{ return 3; } }
		public override int BaseFireResistance{ get{ return 3; } }
		public override int BaseColdResistance{ get{ return 3; } }
		public override int BasePoisonResistance{ get{ return 3; } }
		public override int BaseEnergyResistance{ get{ return 3; } }

		public override int InitMinHits{ get{ return 45; } }
		public override int InitMaxHits{ get{ return 60; } }

		public override int AosStrReq{ get{ return 55; } }
		public override int OldStrReq{ get{ return 40; } }

		public override int ArmorBase{ get{ return 30; } }

        public override int IconItemId { get { return 5129; } }
        public override int IconHue { get { return Hue; } }
        public override int IconOffsetX { get { return 0; } }
        public override int IconOffsetY { get { return 4; } }

		public override ArmorMaterialType MaterialType{ get{ return ArmorMaterialType.Plate; } }

		[Constructable]
		public CloseHelm() : base( 5129 )
		{
            Name = "close helm";
			Weight = 5.0;
		}

		public CloseHelm( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( Weight == 1.0 )
				Weight = 5.0;
		}
	}
}