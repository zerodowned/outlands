using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x2D22, 0x2D2E )]
	public class Leafblade : BaseKnife
	{		
		public override int BaseMissSound{ get{ return 0x239; } }
		public override SkillName BaseSkill { get { return SkillName.Fencing; } }

		public override int InitMinHits{ get{ return 30; } } // TODO
		public override int InitMaxHits{ get{ return 60; } } // TODO

		[Constructable]
		public Leafblade() : base( 0x2D22 )
		{
			Weight = 8.0;
		}

		public Leafblade( Serial serial ) : base( serial )
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