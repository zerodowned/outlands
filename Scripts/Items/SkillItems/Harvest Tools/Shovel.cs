using System;
using Server;
using Server.Engines.Harvest;

namespace Server.Items
{
	public class Shovel : BaseHarvestTool
	{
		public override HarvestSystem HarvestSystem{ get{ return Mining.System; } }

		[Constructable]
		public Shovel() : this( 30 )
		{
		}

		[Constructable]
		public Shovel( int uses ) : base( uses, 0xF39 )
		{
			Weight = 5.0;
		}

		public Shovel( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}