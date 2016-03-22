using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class ColdForgedBlade : ElvenSpellblade
	{
		public override int LabelNumber{ get{ return 1072916; } } // Cold Forged Blade

		[Constructable]
		public ColdForgedBlade()
		{

			Hue = this.GetElementalDamageHue();
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct )
		{
			phys = fire = pois = nrgy = chaos = direct = 0;
			cold = 100;
		}

		public ColdForgedBlade( Serial serial ) : base( serial )
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