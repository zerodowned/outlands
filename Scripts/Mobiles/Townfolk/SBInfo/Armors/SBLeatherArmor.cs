using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
	public class SBLeatherArmor: SBInfo
	{
		private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBLeatherArmor()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : List<GenericBuyInfo>
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( LeatherArms ), 80, 20, 0x13CD, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherChest ), 101, 20, 0x13CC, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherGloves ), 60, 20, 0x13C6, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherGorget ), 74, 20, 0x13C7, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherLegs ), 80, 20, 0x13cb, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherCap ), 10, 20, 0x1DB9, 0 ) );
				Add( new GenericBuyInfo( typeof( FemaleLeatherChest ), 116, 20, 0x1C06, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherBustier ), 97, 20, 0x1C0A, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherShorts ), 86, 20, 0x1C00, 0 ) );
				Add( new GenericBuyInfo( typeof( LeatherSkirt ), 87, 20, 0x1C08, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( LeatherArms ), 30 );
				Add( typeof( LeatherChest ), 40 );
				Add( typeof( LeatherGloves ), 18 );
				Add( typeof( LeatherGorget ), 26 );
				Add( typeof( LeatherLegs ), 20 );
				Add( typeof( LeatherCap ), 4 );
				Add( typeof( FemaleLeatherChest ), 40 );
				Add( typeof( LeatherBustier ), 35 );
				Add( typeof( LeatherShorts ), 33 );
				Add( typeof( LeatherSkirt ), 33 );
			}
		}
	}
}
