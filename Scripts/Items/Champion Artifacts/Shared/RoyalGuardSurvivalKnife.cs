using System;
using Server;

namespace Server.Items
{
	public class RoyalGuardSurvivalKnife : SkinningKnife
	{
		public override int LabelNumber{ get{ return 1094918; } } // Royal Guard Survival Knife [Replica]

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		[Constructable]
		public RoyalGuardSurvivalKnife()
		{
		}

		public RoyalGuardSurvivalKnife( Serial serial ) : base( serial )
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
