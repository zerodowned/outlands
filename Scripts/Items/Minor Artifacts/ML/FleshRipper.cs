using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class FleshRipper : AssassinSpike
	{
		public override int LabelNumber{ get{ return 1075045; } } // Flesh Ripper

		public override int InitMinHits{ get{ return 255; } }
		public override int InitMaxHits{ get{ return 255; } }

		[Constructable]
		public FleshRipper()
		{
			Hue = 0x341;
		}

		public FleshRipper( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}