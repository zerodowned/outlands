using System;

namespace Server.Items
{
	public class Bottle : Item, ICommodity
	{
		int ICommodity.DescriptionNumber { get { return LabelNumber; } }
		bool ICommodity.IsDeedable { get { return true; } }

		[Constructable]
		public Bottle() : this( 1 )
		{
            Name = "bottle";
		}

		[Constructable]
		public Bottle( int amount ) : base( 0xF0E )
		{
            Name = "bottle";

			Stackable = true;
			Weight = 1.0;
			Amount = amount;
		}

		public Bottle( Serial serial ) : base( serial )
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