using System; 
using System.Collections.Generic; 
using Server.Items;
using Server.Custom;

namespace Server.Mobiles 
{ 
	public class SBFisherman : SBInfo 
	{ 
		private List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo(); 
		private IShopSellInfo m_SellInfo = new InternalSellInfo(); 

		public SBFisherman() 
		{ 
		} 

		public override IShopSellInfo SellInfo { get { return m_SellInfo; } } 
		public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } } 

		public class InternalBuyInfo : List<GenericBuyInfo> 
		{ 
			public InternalBuyInfo() 
			{ 
				Add( new GenericBuyInfo( typeof( FishingPole ), 15, 20, 0xDC0, 0 ) );
			} 
		} 

		public class InternalSellInfo : GenericSellInfo 
		{ 
			public InternalSellInfo() 
			{ 
                Add( typeof( FishCommissionCompletedDeed ), 500);
				Add( typeof( FishingPole ), 7 );
			} 
		} 
	} 
}