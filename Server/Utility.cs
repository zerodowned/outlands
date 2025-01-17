/***************************************************************************
 *                                Utility.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using Server.Network;

namespace Server
{
	public static class Utility
	{
		private static Encoding m_UTF8, m_UTF8WithEncoding;

		public static Encoding UTF8
		{
			get
			{
				if ( m_UTF8 == null )
					m_UTF8 = new UTF8Encoding( false, false );

				return m_UTF8;
			}
		}

		public static Encoding UTF8WithEncoding
		{
			get
			{
				if ( m_UTF8WithEncoding == null )
					m_UTF8WithEncoding = new UTF8Encoding( true, false );

				return m_UTF8WithEncoding;
			}
		}

		public static void Separate( StringBuilder sb, string value, string separator )
		{
			if ( sb.Length > 0 )
				sb.Append( separator );

			sb.Append( value );
		}

		public static string Intern( string str )
		{
			if ( str == null )
				return null;
			else if ( str.Length == 0 )
				return String.Empty;

			return String.Intern( str );
		}

		public static void Intern( ref string str )
		{
			str = Intern( str );
		}

		private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

		public static IPAddress Intern( IPAddress ipAddress ) {
			if ( _ipAddressTable == null ) {
				_ipAddressTable = new Dictionary<IPAddress, IPAddress>();
			}

			IPAddress interned;

			if ( !_ipAddressTable.TryGetValue( ipAddress, out interned ) ) {
				interned = ipAddress;
				_ipAddressTable[ipAddress] = interned;
			}

			return interned;
		}

		public static void Intern( ref IPAddress ipAddress ) {
			ipAddress = Intern( ipAddress );
		}

		public static bool IsValidIP( string text )
		{
			bool valid = true;

			IPMatch( text, IPAddress.None, ref valid );

			return valid;
		}

		public static bool IPMatch( string val, IPAddress ip )
		{
			bool valid = true;

			return IPMatch( val, ip, ref valid );
		}

		public static string FixHtml( string str )
		{
			if( str == null )
				return "";

			bool hasOpen  = ( str.IndexOf( '<' ) >= 0 );
			bool hasClose = ( str.IndexOf( '>' ) >= 0 );
			bool hasPound = ( str.IndexOf( '#' ) >= 0 );

			if ( !hasOpen && !hasClose && !hasPound )
				return str;

			StringBuilder sb = new StringBuilder( str );

			if ( hasOpen )
				sb.Replace( '<', '(' );

			if ( hasClose )
				sb.Replace( '>', ')' );

			if ( hasPound )
				sb.Replace( '#', '-' );

			return sb.ToString();
		}

		public static bool IPMatchCIDR( string cidr, IPAddress ip )
		{
			if ( ip == null || ip.AddressFamily == AddressFamily.InterNetworkV6 )
				return false;	//Just worry about IPv4 for now


			/*
			string[] str = cidr.Split( '/' );

			if ( str.Length != 2 )
				return false;

			/* **************************************************
			IPAddress cidrPrefix;

			if ( !IPAddress.TryParse( str[0], out cidrPrefix ) )
				return false;
			 * */

			/*
			string[] dotSplit = str[0].Split( '.' );

			if ( dotSplit.Length != 4 )		//At this point and time, and for speed sake, we'll only worry about IPv4
				return false;

			byte[] bytes = new byte[4];

			for ( int i = 0; i < 4; i++ )
			{
				byte.TryParse( dotSplit[i], out bytes[i] );
			}

			uint cidrPrefix = OrderedAddressValue( bytes );

			int cidrLength = Utility.ToInt32( str[1] );
			//The below solution is the fastest solution of the three

			*/

			byte[] bytes = new byte[4];
			string[] split = cidr.Split( '.' );
			bool cidrBits = false;
			int cidrLength = 0;

			for ( int i = 0; i < 4; i++ )
			{
				int part = 0;

				int partBase = 10;

				string pattern = split[i];

				for ( int j = 0; j < pattern.Length; j++ )
				{
					char c = (char)pattern[j];


					if ( c == 'x' || c == 'X' )
					{
						partBase = 16;
					}
					else if ( c >= '0' && c <= '9' )
					{
						int offset = c - '0';

						if ( cidrBits )
						{
							cidrLength *= partBase;
							cidrLength += offset;
						}
						else
						{
							part *= partBase;
							part += offset;
						}
					}
					else if ( c >= 'a' && c <= 'f' )
					{
						int offset = 10 + ( c - 'a' );

						if ( cidrBits )
						{
							cidrLength *= partBase;
							cidrLength += offset;
						}
						else
						{
							part *= partBase;
							part += offset;
						}
					}
					else if ( c >= 'A' && c <= 'F' )
					{
						int offset = 10 + ( c - 'A' );

						if ( cidrBits )
						{
							cidrLength *= partBase;
							cidrLength += offset;
						}
						else
						{
							part *= partBase;
							part += offset;
						}
					}
					else if ( c == '/' )
					{
						if ( cidrBits || i != 3 )	//If there's two '/' or the '/' isn't in the last byte
						{
							return false;
						}

						partBase = 10;
						cidrBits = true;
					}
					else
					{
						return false;
					}
				}

				bytes[i] = (byte)part;
			}

			uint cidrPrefix = OrderedAddressValue( bytes );

			return IPMatchCIDR( cidrPrefix, ip, cidrLength );
		}

		public static bool IPMatchCIDR( IPAddress cidrPrefix, IPAddress ip, int cidrLength )
		{
			if ( cidrPrefix == null || ip == null || cidrPrefix.AddressFamily == AddressFamily.InterNetworkV6 )	//Ignore IPv6 for now
				return false;

			uint cidrValue = SwapUnsignedInt( (uint)GetLongAddressValue( cidrPrefix ) );
			uint ipValue   = SwapUnsignedInt( (uint)GetLongAddressValue( ip ) );

			return IPMatchCIDR( cidrValue, ipValue, cidrLength );
		}

		public static bool IPMatchCIDR( uint cidrPrefixValue, IPAddress ip, int cidrLength )
		{
			if ( ip == null || ip.AddressFamily == AddressFamily.InterNetworkV6)
				return false;

			uint ipValue = SwapUnsignedInt( (uint)GetLongAddressValue( ip ) );

			return IPMatchCIDR( cidrPrefixValue, ipValue, cidrLength );
		}

		public static bool IPMatchCIDR( uint cidrPrefixValue, uint ipValue, int cidrLength )
		{
			if ( cidrLength <= 0 || cidrLength >= 32 )   //if invalid cidr Length, just compare IPs
				return cidrPrefixValue == ipValue;

			uint mask = uint.MaxValue << 32-cidrLength;

			return ( ( cidrPrefixValue & mask ) == ( ipValue & mask ) );
		}

		private static uint OrderedAddressValue( byte[] bytes )
		{
			if ( bytes.Length != 4 )
				return 0;

			return (uint)(((( bytes[0] << 0x18 ) | (bytes[1] << 0x10)) | (bytes[2] << 8)) | bytes[3]) & ((uint)0xffffffff);
		}

		private static uint SwapUnsignedInt( uint source )
		{
			return (uint)( ( ( ( source & 0x000000FF ) << 0x18 )
			| ( ( source & 0x0000FF00 ) << 8 )
			| ( ( source & 0x00FF0000 ) >> 8 )
			| ( ( source & 0xFF000000 ) >> 0x18 ) ) );
		} 

		public static bool TryConvertIPv6toIPv4( ref IPAddress address )
		{
			if ( !Socket.OSSupportsIPv6 || address.AddressFamily == AddressFamily.InterNetwork )
				return true;

			byte[] addr = address.GetAddressBytes();
			if ( addr.Length == 16 )	//sanity 0 - 15 //10 11 //12 13 14 15
			{
				if ( addr[10] != 0xFF || addr[11] != 0xFF )
					return false;

				for ( int i = 0; i < 10; i++ )
				{
					if ( addr[i] != 0 )
						return false;
				}

				byte[] v4Addr = new byte[4];

				for ( int i = 0; i < 4; i++ )
				{
					v4Addr[i] = addr[12 + i];
				}

				address = new IPAddress( v4Addr );
				return true;
			}

			return false;
		}

		public static bool IPMatch( string val, IPAddress ip, ref bool valid )
		{
			valid = true;

			string[] split = val.Split( '.' );

			for ( int i = 0; i < 4; ++i )
			{
				int lowPart, highPart;

				if ( i >= split.Length )
				{
					lowPart = 0;
					highPart = 255;
				}
				else
				{
					string pattern = split[i];

					if ( pattern == "*" )
					{
						lowPart = 0;
						highPart = 255;
					}
					else
					{
						lowPart = 0;
						highPart = 0;

						bool highOnly = false;
						int lowBase = 10;
						int highBase = 10;

						for ( int j = 0; j < pattern.Length; ++j )
						{
							char c = (char)pattern[j];

							if ( c == '?' )
							{
								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += 0;
								}

								highPart *= highBase;
								highPart += highBase - 1;
							}
							else if ( c == '-' )
							{
								highOnly = true;
								highPart = 0;
							}
							else if ( c == 'x' || c == 'X' )
							{
								lowBase = 16;
								highBase = 16;
							}
							else if ( c >= '0' && c <= '9' )
							{
								int offset = c - '0';

								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += offset;
								}

								highPart *= highBase;
								highPart += offset;
							}
							else if ( c >= 'a' && c <= 'f' )
							{
								int offset = 10 + (c - 'a');

								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += offset;
								}

								highPart *= highBase;
								highPart += offset;
							}
							else if ( c >= 'A' && c <= 'F' )
							{
								int offset = 10 + (c - 'A');

								if ( !highOnly )
								{
									lowPart *= lowBase;
									lowPart += offset;
								}

								highPart *= highBase;
								highPart += offset;
							}
							else
							{
								valid = false;	//high & lowpart would be 0 if it got to here.
							}
						}
					}
				}

				int b = (byte)(Utility.GetAddressValue( ip ) >> (i * 8));

				if ( b < lowPart || b > highPart )
					return false;
			}

			return true;
		}

		public static bool IPMatchClassC( IPAddress ip1, IPAddress ip2 )
		{
			return ( (Utility.GetAddressValue( ip1 ) & 0xFFFFFF) == (Utility.GetAddressValue( ip2 ) & 0xFFFFFF) );
		}

		public static int InsensitiveCompare( string first, string second )
		{
			return Insensitive.Compare( first, second );
		}

		public static bool InsensitiveStartsWith( string first, string second )
		{
			return Insensitive.StartsWith( first, second );
		}

		#region To[Something]
		public static bool ToBoolean( string value )
		{
			bool b;
			bool.TryParse( value, out b );

			return b;
		}

		public static double ToDouble( string value )
		{
			double d;
			double.TryParse( value, out d );

			return d;
		}

		public static TimeSpan ToTimeSpan( string value )
		{
			TimeSpan t;
			TimeSpan.TryParse( value, out t );

			return t;
		}

		public static int ToInt32( string value )
		{
			int i;

			if( value.StartsWith( "0x" ) )
				int.TryParse( value.Substring( 2 ), NumberStyles.HexNumber, null, out i );
			else
				int.TryParse( value, out i );

			return i;
		}
		#endregion

		#region Get[Something]
		public static int GetXMLInt32( string intString, int defaultValue )
		{
			try
			{
				return XmlConvert.ToInt32( intString );
			}
			catch
			{
				int val;
				if ( int.TryParse( intString, out val ) )
					return val;

				return defaultValue;
			}
		}

		public static DateTime GetXMLDateTime( string dateTimeString, DateTime defaultValue )
		{
			try
			{
				return XmlConvert.ToDateTime( dateTimeString, XmlDateTimeSerializationMode.Local );
			}
			catch
			{
				DateTime d;

				if( DateTime.TryParse( dateTimeString, out d ) )
					return d;

				return defaultValue;
			}
		}
#if Framework_4_0
		public static DateTimeOffset GetXMLDateTimeOffset( string dateTimeOffsetString, DateTimeOffset defaultValue )
		{
			try
			{
				return XmlConvert.ToDateTimeOffset( dateTimeOffsetString );
			}
			catch
			{
				DateTimeOffset d;

				if( DateTimeOffset.TryParse( dateTimeOffsetString, out d ) )
					return d;

				return defaultValue;
			}
		}
#endif
		public static TimeSpan GetXMLTimeSpan( string timeSpanString, TimeSpan defaultValue )
		{
			try
			{
				return XmlConvert.ToTimeSpan( timeSpanString );
			}
			catch
			{
				return defaultValue;
			}
		}

		public static string GetAttribute( XmlElement node, string attributeName )
		{
			return GetAttribute( node, attributeName, null );
		}

		public static string GetAttribute( XmlElement node, string attributeName, string defaultValue )
		{
			if ( node == null )
				return defaultValue;

			XmlAttribute attr = node.Attributes[attributeName];

			if ( attr == null )
				return defaultValue;

			return attr.Value;
		}

		public static string GetText( XmlElement node, string defaultValue )
		{
			if ( node == null )
				return defaultValue;

			return node.InnerText;
		}

		public static int GetAddressValue( IPAddress address )
		{
#pragma warning disable 618
			return (int)address.Address;
#pragma warning restore 618
		}

		public static long GetLongAddressValue( IPAddress address )
		{
#pragma warning disable 618
			return address.Address;
#pragma warning restore 618
		}
		#endregion

		#region In[...]Range
		public static bool InRange( Point3D p1, Point3D p2, int range )
		{
			return ( p1.m_X >= (p2.m_X - range) )
				&& ( p1.m_X <= (p2.m_X + range) )
				&& ( p1.m_Y >= (p2.m_Y - range) )
				&& ( p1.m_Y <= (p2.m_Y + range) );
		}

		public static bool InUpdateRange( Point3D p1, Point3D p2 )
		{
			return ( p1.m_X >= (p2.m_X - 18) )
				&& ( p1.m_X <= (p2.m_X + 18) )
				&& ( p1.m_Y >= (p2.m_Y - 18) )
				&& ( p1.m_Y <= (p2.m_Y + 18) );
		}

		public static bool InUpdateRange( Point2D p1, Point2D p2 )
		{
			return ( p1.m_X >= (p2.m_X - 18) )
				&& ( p1.m_X <= (p2.m_X + 18) )
				&& ( p1.m_Y >= (p2.m_Y - 18) )
				&& ( p1.m_Y <= (p2.m_Y + 18) );
		}

		public static bool InUpdateRange( IPoint2D p1, IPoint2D p2 )
		{
			return ( p1.X >= (p2.X - 18) )
				&& ( p1.X <= (p2.X + 18) )
				&& ( p1.Y >= (p2.Y - 18) )
				&& ( p1.Y <= (p2.Y + 18) );
		}
		#endregion

        public static Point3D GetRandomLocation(Point3D startLocation, int radius, bool allowStartLocation)
        {
            if (allowStartLocation)            
                 return new Point3D(startLocation.X + Utility.RandomMinMax(-1 * radius, radius), startLocation.Y + Utility.RandomMinMax(-1 * radius, radius), startLocation.Z);                
            
            else
            {
                List<int> m_Values = new List<int>();

                for (int a = 1; a < radius + 1; a++)
                {
                    m_Values.Add(a);
                    m_Values.Add(a * -1);
                }

                return new Point3D(startLocation.X + m_Values[Utility.RandomMinMax(0, m_Values.Count - 1)], startLocation.Y + m_Values[Utility.RandomMinMax(0, m_Values.Count - 1)], startLocation.Z);
            }
        }

		public static Direction GetDirection( IPoint2D from, IPoint2D to )
		{
			int dx = to.X - from.X;
			int dy = to.Y - from.Y;

			int adx = Math.Abs( dx );
			int ady = Math.Abs( dy );

			if ( adx >= ady * 3 )
			{
				if ( dx > 0 )
					return Direction.East;
				else
					return Direction.West;
			}
			else if ( ady >= adx * 3 )
			{
				if ( dy > 0 )
					return Direction.South;
				else
					return Direction.North;
			}
			else if ( dx > 0 )
			{
				if ( dy > 0 )
					return Direction.Down;
				else
					return Direction.Right;
			}
			else
			{
				if ( dy > 0 )
					return Direction.Left;
				else
					return Direction.Up;
			}
		}

		/* Should probably be rewritten to use an ITile interface

		public static bool CanMobileFit( int z, StaticTile[] tiles )
		{
			int checkHeight = 15;
			int checkZ = z;

			for ( int i = 0; i < tiles.Length; ++i )
			{
				StaticTile tile = tiles[i];

				if ( ((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))*//* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*//* )
				{
					return false;
				}
				else if ( checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z )
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsInContact( StaticTile check, StaticTile[] tiles )
		{
			int checkHeight = check.Height;
			int checkZ = check.Z;

			for ( int i = 0; i < tiles.Length; ++i )
			{
				StaticTile tile = tiles[i];

				if ( ((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))*//* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*//* )
				{
					return true;
				}
				else if ( checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z )
				{
					return true;
				}
			}

			return false;
		}
		*/

		public static object GetArrayCap( Array array, int index )
		{
			return GetArrayCap( array, index, null );
		}

		public static object GetArrayCap( Array array, int index, object emptyValue )
		{
			if ( array.Length > 0 )
			{
				if ( index < 0 )
				{
					index = 0;
				}
				else if ( index >= array.Length )
				{
					index = array.Length - 1;
				}

				return array.GetValue( index );
			}
			else
			{
				return emptyValue;
			}
		}

        public static bool IsInArea(Point2D location, Point2D upperLeft, Point2D lowerRight)
        {
            if ((upperLeft.X <= location.X && upperLeft.Y <= location.Y) && (lowerRight.X >= location.X && lowerRight.Y >= location.Y))
                return true;

            return false;
        }

        public static bool IsInArea(Point3D location, Point3D upperLeft, Point3D lowerRight)
        {
            if ((upperLeft.X <= location.X && upperLeft.Y <= location.Y) && (lowerRight.X >= location.X && lowerRight.Y >= location.Y))
                return true;

            return false;
        }

        public static int GetDistance(Point3D start, Point3D end)
        {
            int xDelta = Math.Abs(start.X - end.X);
            int yDelta = Math.Abs(start.Y - end.Y);

            return Math.Max(xDelta, yDelta);
        }

        public static double GetDistanceToSqrt(Point3D start, Point3D end)
        {
            int xDelta = start.X - end.X;
            int yDelta = start.Y - end.Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public static int CenteredTextOffset(int textCenterX, string text)
        {
            if (text == null)
                text = "";

            int textOffsetX = (int)(Math.Round(textCenterX - ((double)text.Length * 3.6)));

            return textOffsetX;
        }

        public static int ProgressBarX(double percent)
        {
            return (int)(Math.Round(106.0 * percent));
        }

        public static int ProgressBarWidth(double percent)
        {
            return 106 - (int)(Math.Round(106.0 * percent));
        }

        public static Item Dupe(Item item)
        {
            if (item == null)
                return null;

            Item copy = item;

            Type t = copy.GetType();
            ConstructorInfo c = t.GetConstructor(Type.EmptyTypes);

            if (c != null)
            {
                try
                {   
                    object o = c.Invoke(null);

                    if (o != null && o is Item)
                    {
                        Item newItem = (Item)o;

                        CopyProperties(newItem, copy);
                        copy.OnAfterDuped(newItem);
                        newItem.Parent = null;

                        //newItem.InvalidateProperties(); 
                       
                        return newItem;
                    }
                }

                catch
                {                  
                    return null;
                }
            }

            return null;
        }

        public static void CopyProperties(Item dest, Item src)
        {
            PropertyInfo[] props = src.GetType().GetProperties();

            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite)                                           
                        props[i].SetValue(dest, props[i].GetValue(src, null), null);                    
                }

                catch
                {                  
                }
            }
        }

        public static string CreateTimeRemainingString(DateTime start, DateTime end, bool abbreviated, bool useDays, bool useHours, bool useMinutes, bool useSeconds)
        {
            int days;
            int hours;
            int minutes;
            int seconds;

            string sTimeRemaining = "";

            days = Math.Abs((end - start).Days);
            hours = Math.Abs((end - start).Hours);
            minutes = Math.Abs((end - start).Minutes);
            seconds = Math.Abs((end - start).Seconds);
            
            if (hours >= 24)
                days++;

            if (minutes >= 60)
                hours++;

            if (seconds >= 60)
                minutes++;

            sTimeRemaining = "";

            if (useDays)
            {
                if (days > 0)
                {
                    if (abbreviated)
                        sTimeRemaining += days.ToString() + "d ";
                    else
                    {
                        if (days > 1)
                            sTimeRemaining += days.ToString() + " days ";

                        else if (days == 1)
                            sTimeRemaining += days.ToString() + " day ";
                    }
                }
            }

            if (useHours)
            {
                if (hours > 0)
                {
                    if (abbreviated)
                        sTimeRemaining += hours.ToString() + "h ";
                    else
                    {
                        if (hours > 1)
                            sTimeRemaining += hours.ToString() + " hours ";

                        else if (hours == 1)
                            sTimeRemaining += hours.ToString() + " hour ";
                    }
                }
            }

            if (useMinutes)
            {
                if (minutes > 0)
                {
                    if (abbreviated)
                        sTimeRemaining += minutes.ToString() + "m ";
                    else
                    {
                        if (minutes > 1)
                            sTimeRemaining += minutes.ToString() + " minutes ";

                        else if (minutes == 1)
                            sTimeRemaining += minutes.ToString() + " minute ";
                    }
                }
            }

            if (useSeconds)
            {
                if (seconds > 0)
                {
                    if (abbreviated)
                        sTimeRemaining += seconds.ToString() + "s ";
                    else
                    {
                        if (seconds > 1)
                            sTimeRemaining += seconds.ToString() + " seconds ";

                        else if (seconds == 1)
                            sTimeRemaining += seconds.ToString() + " second ";
                        else
                            sTimeRemaining += " 1 second ";
                    }
                }
            }            

            sTimeRemaining = sTimeRemaining.Trim();

            if (sTimeRemaining == "")
                sTimeRemaining = "1 second";
            
            return sTimeRemaining;
        }

        public static string Capitalize(string text)
        {
            if (text == null || text == "")
                return text;

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        public static string CreateDecimalString(double value, int decimalPlaces)
        {
            double factor = Math.Pow(10, decimalPlaces);

            double roundedValue = ((Math.Round(value * factor)) / factor);

            return roundedValue.ToString();
        }

        public static string CreateDecimalPercentageString(double value, int decimalPlaces)
        {
            double factor = Math.Pow(10, 2 + decimalPlaces);

            double roundedValue = 100 * ((Math.Round(value * factor)) / factor);

            return roundedValue.ToString() + "%";
        }

        public static string CreatePercentageString(double value)
        {
            return (value * 100).ToString() + "%";
        }

        public static string CreateCurrencyString(int amount)
        {
            string result = "";
            string stringAmount = amount.ToString();

            int iCount = 0;

            for (int a = 0; a < stringAmount.Length; a++)
            {
                iCount++;

                result += stringAmount[(stringAmount.Length - 1) - a];

                if (iCount == 3 && a != stringAmount.Length - 1)
                {
                    result += ",";
                    iCount = 0;
                }
            }

            string finalResult = "";

            for (int a = 0; a < result.Length; a++)
            {
                finalResult += result[(result.Length - 1) - a];
            }

            return finalResult;
        }

		#region Random
		//4d6+8 would be: Utility.Dice( 4, 6, 8 )
		public static int Dice( int numDice, int numSides, int bonus )
		{
			int total = 0;

			for (int i = 0; i < numDice; ++i)
				total += RandomImpl.Next(numSides) + 1;

			total += bonus;
			return total;
		}

		public static int RandomList( params int[] list )
		{
			return list[RandomImpl.Next(list.Length)];
		}

		public static bool RandomBool()
		{
			return RandomImpl.NextBool();
		}

		public static int RandomMinMax( int min, int max )
		{
			if ( min > max )
			{
				int copy = min;
				min = max;
				max = copy;
			}
			else if ( min == max )
			{
				return min;
			}

			return min + RandomImpl.Next((max - min) + 1);
		}

		public static int Random( int from, int count )
		{
			if ( count == 0 ) {
				return from;
			} else if ( count > 0 ) {
				return from + RandomImpl.Next(count);
			} else {
				return from - RandomImpl.Next(-count);
			}
		}

		public static int Random( int count )
		{
			return RandomImpl.Next(count);
		}

		public static void RandomBytes( byte[] buffer )
		{
			RandomImpl.NextBytes(buffer);
		}

		public static double RandomDouble()
		{
			return RandomImpl.NextDouble();
		}
		#endregion

		#region Random Hues

		/// <summary>
		/// Random pink, blue, green, orange, red or yellow hue
		/// </summary>
		public static int RandomNondyedHue()
		{
			switch ( Random( 6 ) )
			{
				case 0: return RandomPinkHue();
				case 1: return RandomBlueHue();
				case 2: return RandomGreenHue();
				case 3: return RandomOrangeHue();
				case 4: return RandomRedHue();
				case 5: return RandomYellowHue();
			}

			return 0;
		}

		/// <summary>
		/// Random hue in the range 1201-1254
		/// </summary>
		public static int RandomPinkHue()
		{
			return Random( 1201, 54 );
		}

		/// <summary>
		/// Random hue in the range 1301-1354
		/// </summary>
		public static int RandomBlueHue()
		{
			return Random( 1301, 54 );
		}

		/// <summary>
		/// Random hue in the range 1401-1454
		/// </summary>
		public static int RandomGreenHue()
		{
			return Random( 1401, 54 );
		}

		/// <summary>
		/// Random hue in the range 1501-1554
		/// </summary>
		public static int RandomOrangeHue()
		{
			return Random( 1501, 54 );
		}

		/// <summary>
		/// Random hue in the range 1601-1654
		/// </summary>
		public static int RandomRedHue()
		{
			return Random( 1601, 54 );
		}

		/// <summary>
		/// Random hue in the range 1701-1754
		/// </summary>
		public static int RandomYellowHue()
		{
			return Random( 1701, 54 );
		}

		/// <summary>
		/// Random hue in the range 1801-1908
		/// </summary>
		public static int RandomNeutralHue()
		{
			return Random( 1801, 108 );
		}

		/// <summary>
		/// Random hue in the range 2001-2018
		/// </summary>
		public static int RandomSnakeHue()
		{
			return Random( 2001, 18 );
		}

		/// <summary>
		/// Random hue in the range 2101-2130
		/// </summary>
		public static int RandomBirdHue()
		{
			return Random( 2101, 30 );
		}

		/// <summary>
		/// Random hue in the range 2201-2224
		/// </summary>
		public static int RandomSlimeHue()
		{
			return Random( 2201, 24 );
		}

		/// <summary>
		/// Random hue in the range 2301-2318
		/// </summary>
		public static int RandomAnimalHue()
		{
			return Random( 2301, 18 );
		}

		/// <summary>
		/// Random hue in the range 2401-2430
		/// </summary>
		public static int RandomMetalHue()
		{
			return Random( 2401, 30 );
		}

		public static int ClipDyedHue( int hue )
		{
			if ( hue < 2 )
				return 2;
			else if ( hue > 1001 )
				return 1001;
			else
				return hue;
		}

		/// <summary>
		/// Random hue in the range 2-1001
		/// </summary>
		public static int RandomDyedHue()
		{
			return Random( 2, 1000 );
		}

		/// <summary>
		/// Random hue from 0x62, 0x71, 0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59
		/// </summary>
		public static int RandomBrightHue()
		{
			if ( Utility.RandomDouble() < 0.1  )
				return Utility.RandomList( 0x62, 0x71 );

			return Utility.RandomList( 0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59 );
		}

		//[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
		public static int ClipSkinHue( int hue )
		{
			if ( hue < 1002 )
				return 1002;
			else if ( hue > 1058 )
				return 1058;
			else
				return hue;
		}

		//[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
		public static int RandomSkinHue()
		{
			return Random( 1002, 57 ) | 0x8000;
		}

		//[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
		public static int ClipHairHue( int hue )
		{
			if ( hue < 1102 )
				return 1102;
			else if ( hue > 1149 )
				return 1149;
			else
				return hue;
		}

		//[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
		public static int RandomHairHue()
		{
			return Random( 1102, 48 );
		}

		#endregion

		private static SkillName[] m_AllSkills = new SkillName[]
			{
				SkillName.Alchemy,
				SkillName.Anatomy,
				SkillName.AnimalLore,
				SkillName.ItemID,
				SkillName.ArmsLore,
				SkillName.Parry,
				SkillName.Begging,
				SkillName.Blacksmith,
				SkillName.Peacemaking,
				SkillName.Camping,
				SkillName.Carpentry,
				SkillName.Cartography,
				SkillName.Cooking,
				SkillName.DetectHidden,
				SkillName.Discordance,
				SkillName.EvalInt,
				SkillName.Healing,
				SkillName.Fishing,
				SkillName.Forensics,
				SkillName.Herding,
				SkillName.Hiding,
				SkillName.Provocation,
				SkillName.Inscribe,
				SkillName.Lockpicking,
				SkillName.Magery,
				SkillName.MagicResist,
				SkillName.Tactics,
				SkillName.Snooping,
				SkillName.Musicianship,
				SkillName.Poisoning,
				SkillName.Archery,
				SkillName.SpiritSpeak,
				SkillName.Stealing,
				SkillName.Tailoring,
				SkillName.AnimalTaming,
				SkillName.TasteID,
				SkillName.Tinkering,
				SkillName.Tracking,
				SkillName.Veterinary,
				SkillName.Swords,
				SkillName.Macing,
				SkillName.Fencing,
				SkillName.Wrestling,
				SkillName.Lumberjacking,
				SkillName.Mining,
				SkillName.Meditation,
				SkillName.Stealth,
				SkillName.RemoveTrap,
			};

		private static SkillName[] m_CombatSkills = new SkillName[]
			{
				SkillName.Archery,
				SkillName.Swords,
				SkillName.Macing,
				SkillName.Fencing,
				SkillName.Wrestling
			};

		private static SkillName[] m_CraftSkills = new SkillName[]
			{
				SkillName.Alchemy,
				SkillName.Blacksmith,
				SkillName.Carpentry,
				SkillName.Cartography,
				SkillName.Cooking,
				SkillName.Inscribe,
				SkillName.Tailoring,
				SkillName.Tinkering
			};

		public static SkillName RandomSkill()
		{
			return m_AllSkills[Utility.Random(m_AllSkills.Length - ( Core.ML ? 0 : Core.SE ? 1 : Core.AOS ? 3 : 6 ) )];
		}

		public static SkillName RandomCombatSkill()
		{
			return m_CombatSkills[Utility.Random(m_CombatSkills.Length)];
		}

		public static SkillName RandomCraftSkill()
		{
			return m_CraftSkills[Utility.Random(m_CraftSkills.Length)];
		}

		public static void FixPoints( ref Point3D top, ref Point3D bottom )
		{
			if ( bottom.m_X < top.m_X )
			{
				int swap = top.m_X;
				top.m_X = bottom.m_X;
				bottom.m_X = swap;
			}

			if ( bottom.m_Y < top.m_Y )
			{
				int swap = top.m_Y;
				top.m_Y = bottom.m_Y;
				bottom.m_Y = swap;
			}

			if ( bottom.m_Z < top.m_Z )
			{
				int swap = top.m_Z;
				top.m_Z = bottom.m_Z;
				bottom.m_Z = swap;
			}
		}

		public static ArrayList BuildArrayList( IEnumerable enumerable )
		{
			IEnumerator e = enumerable.GetEnumerator();

			ArrayList list = new ArrayList();

			while ( e.MoveNext() )
			{
				list.Add( e.Current );
			}

			return list;
		}

		public static bool RangeCheck( IPoint2D p1, IPoint2D p2, int range )
		{
			return ( p1.X >= (p2.X - range) )
				&& ( p1.X <= (p2.X + range) )
				&& ( p1.Y >= (p2.Y - range) )
				&& ( p2.Y <= (p2.Y + range) );
		}

		public static void FormatBuffer( TextWriter output, Stream input, int length )
		{
			output.WriteLine( "        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F" );
			output.WriteLine( "       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --" );

			int byteIndex = 0;

			int whole = length >> 4;
			int rem = length & 0xF;

			for ( int i = 0; i < whole; ++i, byteIndex += 16 )
			{
				StringBuilder bytes = new StringBuilder( 49 );
				StringBuilder chars = new StringBuilder( 16 );

				for ( int j = 0; j < 16; ++j )
				{
					int c = input.ReadByte();

					bytes.Append( c.ToString( "X2" ) );

					if ( j != 7 )
					{
						bytes.Append( ' ' );
					}
					else
					{
						bytes.Append( "  " );
					}

					if ( c >= 0x20 && c < 0x7F )
					{
						chars.Append( (char)c );
					}
					else
					{
						chars.Append( '.' );
					}
				}

				output.Write( byteIndex.ToString( "X4" ) );
				output.Write( "   " );
				output.Write( bytes.ToString() );
				output.Write( "  " );
				output.WriteLine( chars.ToString() );
			}

			if ( rem != 0 )
			{
				StringBuilder bytes = new StringBuilder( 49 );
				StringBuilder chars = new StringBuilder( rem );

				for ( int j = 0; j < 16; ++j )
				{
					if ( j < rem )
					{
						int c = input.ReadByte();

						bytes.Append( c.ToString( "X2" ) );

						if ( j != 7 )
						{
							bytes.Append( ' ' );
						}
						else
						{
							bytes.Append( "  " );
						}

						if ( c >= 0x20 && c < 0x7F )
						{
							chars.Append( (char)c );
						}
						else
						{
							chars.Append( '.' );
						}
					}
					else
					{
						bytes.Append( "   " );
					}
				}

				output.Write( byteIndex.ToString( "X4" ) );
				output.Write( "   " );
				output.Write( bytes.ToString() );
				output.Write( "  " );
				output.WriteLine( chars.ToString() );
			}
		}

		private static Stack<ConsoleColor> m_ConsoleColors = new Stack<ConsoleColor>();

		public static void PushColor( ConsoleColor color )
		{
			try
			{
				m_ConsoleColors.Push( Console.ForegroundColor );
				Console.ForegroundColor = color;
			}
			catch
			{
			}
		}

		public static void PopColor()
		{
			try
			{
				Console.ForegroundColor = m_ConsoleColors.Pop();
			}
			catch
			{
			}
		}

		public static bool NumberBetween( double num, int bound1, int bound2, double allowance )
		{
			if ( bound1 > bound2 )
			{
				int i = bound1;
				bound1 = bound2;
				bound2 = i;
			}

			return ( num<bound2+allowance && num>bound1-allowance );
		}

		public static void AssignRandomHair( Mobile m )
		{
			AssignRandomHair( m, true );
		}

		public static void AssignRandomHair( Mobile m, int hue )
		{
			m.HairItemID = m.Race.RandomHair( m );
			m.HairHue = hue;
		}

		public static void AssignRandomHair( Mobile m, bool randomHue )
		{
			m.HairItemID = m.Race.RandomHair( m );

			if( randomHue )
				m.HairHue = m.Race.RandomHairHue();
		}

		public static void AssignRandomFacialHair( Mobile m )
		{
			AssignRandomFacialHair( m, true );
		}

		public static void AssignRandomFacialHair( Mobile m, int hue )
		{
			m.FacialHairItemID = m.Race.RandomFacialHair( m );
			m.FacialHairHue = hue;
		}

		public static void AssignRandomFacialHair( Mobile m, bool randomHue )
		{
			m.FacialHairItemID = m.Race.RandomFacialHair( m );

			if( randomHue )
				m.FacialHairHue = m.Race.RandomHairHue();
		}

		public static List<TOutput> CastConvertList<TInput, TOutput>( List<TInput> list ) where TOutput : TInput
		{
			return list.ConvertAll<TOutput>( new Converter<TInput, TOutput>( delegate( TInput value ) { return (TOutput)value; } ) );
		}

		public static List<TOutput> SafeConvertList<TInput, TOutput>( List<TInput> list ) where TOutput : class
		{
			List<TOutput> output = new List<TOutput>( list.Capacity );

			for( int i = 0; i < list.Count; i++ )
			{
				TOutput t = list[i] as TOutput;

				if( t != null )
					output.Add( t );
			}

			return output;
		}
	}
}