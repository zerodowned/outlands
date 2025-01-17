using System;
using Server;

namespace Server.Items
{
	public class CrownOfTalKeesh : Bandana
	{
		public override int LabelNumber{ get{ return 1094903; } } // Crown of Tal'Keesh [Replica]

		public override int BasePhysicalResistance{ get{ return 0; } }
		public override int BaseFireResistance{ get{ return 5; } }
		public override int BaseColdResistance{ get{ return 9; } }
		public override int BasePoisonResistance{ get{ return 20; } }
		public override int BaseEnergyResistance{ get{ return 20; } }

		public override int InitMinHits{ get{ return 150; } }
		public override int InitMaxHits{ get{ return 150; } }

		

		[Constructable]
		public CrownOfTalKeesh()
		{
			Hue = 0x4F2;

			Attributes.BonusInt = 8;
			Attributes.RegenMana = 4;
			Attributes.SpellDamage = 10;
		}

		public CrownOfTalKeesh( Serial serial ) : base( serial )
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
