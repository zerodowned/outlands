
using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
	public class SBBanker : SBInfo
	{
		private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
		private IShopSellInfo m_SellInfo = new InternalSellInfo();

		public SBBanker()
		{
		}

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
		public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }

		public class InternalBuyInfo : List<GenericBuyInfo>
		{
			public InternalBuyInfo()
			{
				Add( new GenericBuyInfo( "1047016", typeof( CommodityDeed ), 5, 20, 0x14F0, 0x47 ) );
                //Add( new GenericBuyInfo( "1041243", typeof( ContractOfEmployment ), 1025, 20, 0x14F0, 0 ) );
                Add(new GenericBuyInfo("1062332", typeof(VendorRentalContract), 1025, 20, 0x14F0, 0));
			}
		}

		public class InternalSellInfo : GenericSellInfo
		{
			public InternalSellInfo()
			{
			}
		}
	}
}