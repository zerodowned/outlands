using System;
using Server;

namespace Server.Items
{
	public class ANecromancerShroud : Robe
	{
		public override int LabelNumber{ get{ return 1094913; } } // A Necromancer Shroud [Replica]

		public override int BaseColdResistance{ get{ return 5; } }

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		

		[Constructable]
		public ANecromancerShroud()
		{
			Hue = 0x455;
		}

		public ANecromancerShroud( Serial serial ) : base( serial )
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
		}
	}
}
