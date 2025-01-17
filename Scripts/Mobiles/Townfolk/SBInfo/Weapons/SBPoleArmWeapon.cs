using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
	public class SBPoleArmWeapon: SBInfo
	{
		private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBPoleArmWeapon()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : List<GenericBuyInfo>
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( typeof( Bardiche ), 68, 20, 0xF4D, 0 ) );
				Add( new GenericBuyInfo( typeof( Halberd ), 50, 20, 0x143E, 0 ) );
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
				Add( typeof( Bardiche ), 21 );
				Add( typeof( Halberd ), 17 );
			}
		}
	}
}
