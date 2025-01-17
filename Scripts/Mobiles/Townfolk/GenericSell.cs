using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
	public class GenericSellInfo : IShopSellInfo
	{
		private Dictionary<Type, int> m_Table = new Dictionary<Type, int>();
		private Type[] m_Types;

		public GenericSellInfo()
		{
		}

		public void Add( Type type, int price )
		{
            Add(type, price, false);
		}

        public void Add(Type type, int price, bool exact)
        {
            m_Table[type] = price;
            m_Types = null;
        }

		public int GetSellPriceFor( Item item )
		{
			int price = 0;

			m_Table.TryGetValue( item.GetType(), out price );

            if (item is BaseBeverage)
            {
                int price1 = price, price2 = price;

                if (item is Pitcher)
                {
                    price1 = 1;
                    price2 = 1;
                }

                else if (item is BeverageBottle)
                {
                    price1 = 1;
                    price2 = 1;
                }

                else if (item is Jug)
                {
                    price1 = 1;
                    price2 = 1;
                }

                BaseBeverage bev = (BaseBeverage)item;

                if (bev.IsEmpty || bev.Content == BeverageType.Milk)
                    price = price1;

                else
                    price = price2;
            }

            double basePrice = (double)price;
            double priceScalar = item.GetSellValueScalar();

            price = (int)Math.Floor(basePrice * priceScalar);

            if (price < 1)
                price = 1;

			return price;
		}
                
		public int GetBuyPriceFor( Item item )
		{
            return (int)(GenericBuyInfo.PurchaseCompareToSellPriceScalar * GetSellPriceFor(item));
		}        

		public Type[] Types
		{
			get
			{
				if ( m_Types == null )
				{
					m_Types = new Type[m_Table.Keys.Count];
					m_Table.Keys.CopyTo( m_Types, 0 );
				}

				return m_Types;
			}
		}

		public string GetNameFor( Item item )
		{
			if ( item.Name != null )
				return item.Name;

			else
				return item.LabelNumber.ToString();
		}

		public bool IsSellable( Item item )
		{
			if ( item.Nontransferable )
				return false;

			return IsInList( item.GetType() );
		}
	 
		public bool IsResellable( Item item )
		{
			if ( item.Nontransferable )
				return false;

			return IsInList( item.GetType() );
		}

		public bool IsInList( Type type )
		{
			return m_Table.ContainsKey( type );
		}
	}
}
