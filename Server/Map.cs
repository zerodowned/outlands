/***************************************************************************
 *                                  Map.cs
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
using System.Runtime.Serialization;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server
{
	[Flags]
	public enum MapRules
	{
		None					= 0x0000,
		Internal				= 0x0001, // Internal map (used for dragging, commodity deeds, etc)
		FreeMovement			= 0x0002, // Anyone can move over anyone else without taking stamina loss
		BeneficialRestrictions	= 0x0004, // Disallow performing beneficial actions on criminals/murderers
		HarmfulRestrictions		= 0x0008, // Disallow performing harmful actions on innocents
		TrammelRules			= FreeMovement | BeneficialRestrictions | HarmfulRestrictions,
		FeluccaRules			= None
	}

	public interface IPooledEnumerable : IEnumerable {
		void Free();
	}

	public interface IPooledEnumerable<T> : IPooledEnumerable, IEnumerable<T> {
	}

	public interface IPooledEnumerator<T> : IEnumerator<T> {
		void Free();
	}

	[Parsable]
	//[CustomEnum( new string[]{ "Felucca", "Trammel", "Ilshenar", "Malas", "Internal" } )]
	public sealed class Map : IComparable, IComparable<Map>
	{
		public const int SectorSize = 16;
		public const int SectorShift = 4;
		public static int SectorActiveRange = 2;

		private static Map[] m_Maps = new Map[0x100];

		public static Map[] Maps { get { return m_Maps; } }

		public static Map Felucca { get { return m_Maps[0]; } }
		public static Map Trammel { get { return m_Maps[1]; } }
		public static Map Ilshenar { get { return m_Maps[2]; } }
		public static Map Malas { get { return m_Maps[3]; } }
		public static Map Tokuno { get { return m_Maps[4]; } }
		public static Map TerMur { get { return m_Maps[5]; } }
		public static Map Internal { get { return m_Maps[0x7F]; } }

		private static List<Map> m_AllMaps = new List<Map>();

		public static List<Map> AllMaps { get { return m_AllMaps; } }

		private int m_MapID, m_MapIndex, m_FileIndex;

		private int m_Width, m_Height;
		private int m_SectorsWidth, m_SectorsHeight;
		private int m_Season;
		private Dictionary<string, Region> m_Regions;
		private Region m_DefaultRegion;

		public int Season { get { return m_Season; } set { m_Season = value; } }

		private string m_Name;
		private MapRules m_Rules;
		private Sector[][] m_Sectors;
		private Sector m_InvalidSector;

		private TileMatrix m_Tiles;

		private static string[] m_MapNames;
		private static Map[] m_MapValues;

		public static string[] GetMapNames()
		{
			CheckNamesAndValues();
			return m_MapNames;
		}

		public static Map[] GetMapValues()
		{
			CheckNamesAndValues();
			return m_MapValues;
		}

		public static Map Parse( string value )
		{
			CheckNamesAndValues();

			for ( int i = 0; i < m_MapNames.Length; ++i )
			{
				if ( Insensitive.Equals( m_MapNames[i], value ) )
					return m_MapValues[i];
			}

			int index;

			if( int.TryParse( value, out index ) )
			{
				if( index >= 0 && index < m_Maps.Length && m_Maps[index] != null )
					return m_Maps[index];
			}

			throw new ArgumentException( "Invalid map name" );
		}

		private static void CheckNamesAndValues()
		{
			if ( m_MapNames != null && m_MapNames.Length == m_AllMaps.Count )
				return;

			m_MapNames = new string[m_AllMaps.Count];
			m_MapValues = new Map[m_AllMaps.Count];

			for ( int i = 0; i < m_AllMaps.Count; ++i )
			{
				Map map = m_AllMaps[i];

				m_MapNames[i] = map.Name;
				m_MapValues[i] = map;
			}
		}

		public override string ToString()
		{
			return m_Name;
		}

		public int GetAverageZ( int x, int y )
		{
			int z = 0, avg = 0, top = 0;

			GetAverageZ( x, y, ref z, ref avg, ref top );

			return avg;
		}

		public void GetAverageZ( int x, int y, ref int z, ref int avg, ref int top )
		{
			int zTop = Tiles.GetLandTile( x, y ).Z;
			int zLeft = Tiles.GetLandTile( x, y + 1 ).Z;
			int zRight = Tiles.GetLandTile( x + 1, y ).Z;
			int zBottom = Tiles.GetLandTile( x + 1, y + 1 ).Z;

			z = zTop;
			if ( zLeft < z )
				z = zLeft;
			if ( zRight < z )
				z = zRight;
			if ( zBottom < z )
				z = zBottom;

			top = zTop;
			if ( zLeft > top )
				top = zLeft;
			if ( zRight > top )
				top = zRight;
			if ( zBottom > top )
				top = zBottom;

			if ( Math.Abs( zTop - zBottom ) > Math.Abs( zLeft - zRight ) )
				avg = FloorAverage( zLeft, zRight );
			else
				avg = FloorAverage( zTop, zBottom );
		}

		private static int FloorAverage( int a, int b )
		{
			int v = a + b;

			if ( v < 0 )
				--v;

			return ( v / 2 );
		}

		#region Get*InRange/Bounds
		public IPooledEnumerable<IEntity> GetObjectsInRange(Point3D p)
		{
			if ( this == Map.Internal )
				return NullEnumerable<IEntity>.Instance;

			return PooledEnumerable<IEntity>.Instantiate(EntityEnumerator.Instantiate(this, new Rectangle2D(p.m_X - 18, p.m_Y - 18, 37, 37)));
		}

		public IPooledEnumerable<IEntity> GetObjectsInRange(Point3D p, int range)
		{
			if ( this == Map.Internal )
				return NullEnumerable<IEntity>.Instance;

			return PooledEnumerable<IEntity>.Instantiate(EntityEnumerator.Instantiate(this, new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1)));
		}

		public IPooledEnumerable<IEntity> GetObjectsInBounds(Rectangle2D bounds)
		{
			if ( this == Map.Internal )
				return NullEnumerable<IEntity>.Instance;

			return PooledEnumerable<IEntity>.Instantiate(EntityEnumerator.Instantiate(this, bounds));
		}

		public IPooledEnumerable<NetState> GetClientsInRange(Point3D p)
		{
			if ( this == Map.Internal )
				return NullEnumerable<NetState>.Instance;

			return PooledEnumerable<NetState>.Instantiate(ClientEnumerator.Instantiate(this, new Rectangle2D(p.m_X - 18, p.m_Y - 18, 37, 37)));
		}

		public IPooledEnumerable<NetState> GetClientsInRange(Point3D p, int range)
		{
			if ( this == Map.Internal )
				return NullEnumerable<NetState>.Instance;

			return PooledEnumerable<NetState>.Instantiate(ClientEnumerator.Instantiate(this, new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1)));
		}

		public IPooledEnumerable<NetState> GetClientsInBounds(Rectangle2D bounds)
		{
			if ( this == Map.Internal )
				return NullEnumerable<NetState>.Instance;

			return PooledEnumerable<NetState>.Instantiate(ClientEnumerator.Instantiate(this, bounds));
		}

		public IPooledEnumerable<Item> GetItemsInRange(Point3D p)
		{
			if ( this == Map.Internal )
				return NullEnumerable<Item>.Instance;

			return PooledEnumerable<Item>.Instantiate(ItemEnumerator.Instantiate(this, new Rectangle2D(p.m_X - 18, p.m_Y - 18, 37, 37)));
		}

		public IPooledEnumerable<Item> GetItemsInRange(Point3D p, int range)
		{
			if ( this == Map.Internal )
				return NullEnumerable<Item>.Instance;

			return PooledEnumerable<Item>.Instantiate(ItemEnumerator.Instantiate(this, new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1)));
		}

		public IPooledEnumerable<Item> GetItemsInBounds(Rectangle2D bounds)
		{
			if ( this == Map.Internal )
				return NullEnumerable<Item>.Instance;

			return PooledEnumerable<Item>.Instantiate(ItemEnumerator.Instantiate(this, bounds));
		}

        public int ItemCountAtLocationByType(Point3D location, Type itemType)
        {
            int itemCount = 0;

            IPooledEnumerable itemsOnTile = GetItemsInRange(location, 0);
                
            foreach (Item item in itemsOnTile)
            {
                if (item.GetType() == itemType)
                    itemCount++;
            }

            itemsOnTile.Free();

            return itemCount;
        }

		public IPooledEnumerable<Mobile> GetMobilesInRange(Point3D p)
		{
			if ( this == Map.Internal )
				return NullEnumerable<Mobile>.Instance;

			return PooledEnumerable<Mobile>.Instantiate(MobileEnumerator.Instantiate(this, new Rectangle2D(p.m_X - 18, p.m_Y - 18, 37, 37)));
		}

		public IPooledEnumerable<Mobile> GetMobilesInRange(Point3D p, int range)
		{
			if ( this == Map.Internal )
				return NullEnumerable<Mobile>.Instance;

			return PooledEnumerable<Mobile>.Instantiate(MobileEnumerator.Instantiate(this, new Rectangle2D(p.m_X - range, p.m_Y - range, range * 2 + 1, range * 2 + 1)));
		}

		public IPooledEnumerable<Mobile> GetMobilesInBounds(Rectangle2D bounds)
		{
			if ( this == Map.Internal )
				return NullEnumerable<Mobile>.Instance;

			return PooledEnumerable<Mobile>.Instantiate(MobileEnumerator.Instantiate(this, bounds));
		}
		#endregion

		public IPooledEnumerable<StaticTile[]> GetMultiTilesAt(int x, int y)
		{
			if ( this == Map.Internal )
				return NullEnumerable<StaticTile[]>.Instance;

			Sector sector = GetSector( x, y );

			if ( sector.Multis.Count == 0 )
				return NullEnumerable<StaticTile[]>.Instance;

			return PooledEnumerable<StaticTile[]>.Instantiate(MultiTileEnumerator.Instantiate(sector, new Point2D(x, y)));
		}

		#region CanFit
		public bool CanFit( Point3D p, int height, bool checkBlocksFit )
		{
			return CanFit( p.m_X, p.m_Y, p.m_Z, height, checkBlocksFit, true, true );
		}

		public bool CanFit( Point3D p, int height, bool checkBlocksFit, bool checkMobiles )
		{
			return CanFit( p.m_X, p.m_Y, p.m_Z, height, checkBlocksFit, checkMobiles, true );
		}

		public bool CanFit( Point2D p, int z, int height, bool checkBlocksFit )
		{
			return CanFit( p.m_X, p.m_Y, z, height, checkBlocksFit, true, true );
		}

		public bool CanFit( Point3D p, int height )
		{
			return CanFit( p.m_X, p.m_Y, p.m_Z, height, false, true, true );
		}

		public bool CanFit( Point2D p, int z, int height )
		{
			return CanFit( p.m_X, p.m_Y, z, height, false, true, true );
		}

		public bool CanFit( int x, int y, int z, int height )
		{
			return CanFit( x, y, z, height, false, true, true );
		}

		public bool CanFit( int x, int y, int z, int height, bool checksBlocksFit )
		{
			return CanFit( x, y, z, height, checksBlocksFit, true, true );
		}

		public bool CanFit( int x, int y, int z, int height, bool checkBlocksFit, bool checkMobiles )
		{
			return CanFit( x, y, z, height, checkBlocksFit, checkMobiles, true );
		}

		public bool CanFit( int x, int y, int z, int height, bool checkBlocksFit, bool checkMobiles, bool requireSurface )
		{
            if (this == Map.Internal)
            {
                return false;
            }

            if (x < 0 || y < 0 || x >= m_Width || y >= m_Height)
            {
                return false;
            }

			bool hasSurface = false;

			LandTile lt = Tiles.GetLandTile( x, y );
			int lowZ = 0, avgZ = 0, topZ = 0;

			GetAverageZ( x, y, ref lowZ, ref avgZ, ref topZ );
			TileFlag landFlags = TileData.LandTable[lt.ID & TileData.MaxLandValue].Flags;

            if ((landFlags & TileFlag.Impassable) != 0 && avgZ > z && (z + height) > lowZ)
            {
                return false;
            }
            else if ((landFlags & TileFlag.Impassable) == 0 && z == avgZ && !lt.Ignored)
                hasSurface = true;

			StaticTile[] staticTiles = Tiles.GetStaticTiles( x, y, true );

			bool surface, impassable;

			for ( int i = 0; i < staticTiles.Length; ++i )
			{
				ItemData id = TileData.ItemTable[staticTiles[i].ID & TileData.MaxItemValue];
				surface = id.Surface;
				impassable = id.Impassable;

                if ((surface || impassable) && (staticTiles[i].Z + id.CalcHeight) > z && (z + height) > staticTiles[i].Z)
                {
                    return false;
                }
                else if (surface && !impassable && z == (staticTiles[i].Z + id.CalcHeight))
                    hasSurface = true;
			}

			Sector sector = GetSector( x, y );
			List<Item> items  = sector.Items;
			List<Mobile> mobs = sector.Mobiles;

			for ( int i = 0; i < items.Count; ++i )
			{
				Item item = items[i];

				if ( !(item is BaseMulti) && item.ItemID <= TileData.MaxItemValue && item.AtWorldPoint( x, y ) )
				{
					ItemData id = item.ItemData;
					surface = id.Surface;
					impassable = id.Impassable;

                    if ((surface || impassable || (checkBlocksFit && item.BlocksFit)) && (item.Z + id.CalcHeight) > z && (z + height) > item.Z)
                    {
                        return false;
                    }

                    else if (surface && !impassable && !item.Movable && z == (item.Z + id.CalcHeight))
                        hasSurface = true;
				}
			}

			if ( checkMobiles )
			{
				for ( int i = 0; i < mobs.Count; ++i )
				{
					Mobile m = mobs[i];

                    if (m.Location.m_X == x && m.Location.m_Y == y && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
                    {
                        if ((m.Z + 16) > z && (z + height) > m.Z)
                        {
                            return false;
                        }
                    }
				}
			}

			return !requireSurface || hasSurface;
		}

		#endregion

		#region CanSpawnMobile
		public bool CanSpawnMobile( Point3D p )
		{
			return CanSpawnMobile( p.m_X, p.m_Y, p.m_Z );
		}

		public bool CanSpawnMobile( Point2D p, int z )
		{
			return CanSpawnMobile( p.m_X, p.m_Y, z );
		}

		public bool CanSpawnMobile( int x, int y, int z )
		{
			if ( !Region.Find( new Point3D( x, y, z ), this ).AllowSpawn() )
				return false;

			return CanFit( x, y, z, 16 );
		}
		#endregion

		private class ZComparer : IComparer<Item>
		{
			public static readonly ZComparer Default = new ZComparer();

			public int Compare( Item x, Item y )
			{
				return x.Z.CompareTo( y.Z );
			}
		}

		public void FixColumn( int x, int y )
		{
			LandTile landTile = Tiles.GetLandTile( x, y );

			int landZ = 0, landAvg = 0, landTop = 0;
			GetAverageZ( x, y, ref landZ, ref landAvg, ref landTop );

			StaticTile[] tiles = Tiles.GetStaticTiles( x, y, true );

			List<Item> items = new List<Item>();

			IPooledEnumerable<Item> eable = GetItemsInRange( new Point3D( x, y, 0 ), 0 );

			foreach ( Item item in eable )
			{
				if ( !(item is BaseMulti) && item.ItemID <= TileData.MaxItemValue )
				{
					items.Add( item );

					if ( items.Count > 100 )
						break;
				}
			}

			eable.Free();

			if ( items.Count > 100 )
				return;

			items.Sort( ZComparer.Default );

			for ( int i = 0; i < items.Count; ++i )
			{
				Item toFix = items[i];

				if ( !toFix.Movable )
					continue;

				int z = int.MinValue;
				int currentZ = toFix.Z;

				if ( !landTile.Ignored && landAvg <= currentZ )
					z = landAvg;

				for ( int j = 0; j < tiles.Length; ++j )
				{
					StaticTile tile = tiles[j];
					ItemData id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

					int checkZ = tile.Z;
					int checkTop = checkZ + id.CalcHeight;

					if ( checkTop == checkZ && !id.Surface )
						++checkTop;

					if ( checkTop > z && checkTop <= currentZ )
						z = checkTop;
				}

				for ( int j = 0; j < items.Count; ++j )
				{
					if ( j == i )
						continue;

					Item item = items[j];
					ItemData id = item.ItemData;

					int checkZ = item.Z;
					int checkTop = checkZ + id.CalcHeight;

					if ( checkTop == checkZ && !id.Surface )
						++checkTop;

					if ( checkTop > z && checkTop <= currentZ )
						z = checkTop;
				}

				if ( z != int.MinValue )
					toFix.Location = new Point3D( toFix.X, toFix.Y, z );
			}
		}

		/* This could be probably be re-implemented if necessary (perhaps via an ITile interface?).
		public List<Tile> GetTilesAt( Point2D p, bool items, bool land, bool statics )
		{
			List<Tile> list = new List<Tile>();

			if ( this == Map.Internal )
				return list;

			if ( land )
				list.Add( Tiles.GetLandTile( p.m_X, p.m_Y ) );

			if ( statics )
				list.AddRange( Tiles.GetStaticTiles( p.m_X, p.m_Y, true ) );

			if ( items )
			{
				Sector sector = GetSector( p );

				foreach ( Item item in sector.Items )
					if ( item.AtWorldPoint( p.m_X, p.m_Y ) )
						list.Add( new StaticTile( (ushort)item.ItemID, (sbyte) item.Z ) );
			}

			return list;
		}
		*/

		/// <summary>
		/// Gets the highest surface that is lower than <paramref name="p"/>.
		/// </summary>
		/// <param name="p">The reference point.</param>
		/// <returns>A surface <typeparamref name="Tile"/> or <typeparamref name="Item"/>.</returns>
		public object GetTopSurface( Point3D p )
		{
			if ( this == Map.Internal )
				return null;

			object surface = null;
			int surfaceZ = int.MinValue;


			LandTile lt = Tiles.GetLandTile( p.X, p.Y );

			if ( !lt.Ignored )
			{
				int avgZ = GetAverageZ( p.X, p.Y );

				if ( avgZ <= p.Z )
				{
					surface = lt;
					surfaceZ = avgZ;

					if ( surfaceZ == p.Z )
						return surface;
				}
			}


			StaticTile[] staticTiles = Tiles.GetStaticTiles( p.X, p.Y, true );

			for ( int i = 0; i < staticTiles.Length; i++ )
			{
				StaticTile tile = staticTiles[i];
				ItemData id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

				if ( id.Surface || ( id.Flags & TileFlag.Wet ) != 0 )
				{
					int tileZ = tile.Z + id.CalcHeight;

					if ( tileZ > surfaceZ && tileZ <= p.Z )
					{
						surface = tile;
						surfaceZ = tileZ;

						if ( surfaceZ == p.Z )
							return surface;
					}
				}
			}


			Sector sector = GetSector( p.X, p.Y );

			for ( int i = 0; i < sector.Items.Count; i++ )
			{
				Item item = sector.Items[i];

				if ( !(item is BaseMulti) && item.ItemID <= TileData.MaxItemValue && item.AtWorldPoint( p.X, p.Y ) && !item.Movable )
				{
					ItemData id = item.ItemData;

					if ( id.Surface || ( id.Flags & TileFlag.Wet ) != 0 )
					{
						int itemZ = item.Z + id.CalcHeight;

						if ( itemZ > surfaceZ && itemZ <= p.Z )
						{
							surface = item;
							surfaceZ = itemZ;

							if ( surfaceZ == p.Z )
								return surface;
						}
					}
				}
			}


			return surface;
		}

        public int GetSurfaceZ(Point3D point, int startingHeight)
        {
            //Start at Z Level and scan downward from there to find the first surface tile
            point.Z += startingHeight;

            int adjustedZ = 0;

            object surface = GetTopSurface(point);

            if (surface is LandTile)
                adjustedZ = ((LandTile)surface).Z;

            else if (surface is StaticTile)
                adjustedZ = ((StaticTile)surface).Z;

            return adjustedZ;
        }

		public void Bound( int x, int y, out int newX, out int newY )
		{
			if ( x < 0 )
				newX = 0;
			else if ( x >= m_Width )
				newX = m_Width - 1;
			else
				newX = x;

			if ( y < 0 )
				newY = 0;
			else if ( y >= m_Height )
				newY = m_Height - 1;
			else
				newY = y;
		}

		public Point2D Bound( Point2D p )
		{
			int x = p.m_X, y = p.m_Y;

			if ( x < 0 )
				x = 0;
			else if ( x >= m_Width )
				x = m_Width - 1;

			if ( y < 0 )
				y = 0;
			else if ( y >= m_Height )
				y = m_Height - 1;

			return new Point2D( x, y );
		}

		public Map( int mapID, int mapIndex, int fileIndex, int width, int height, int season, string name, MapRules rules )
		{
			m_MapID = mapID;
			m_MapIndex = mapIndex;
			m_FileIndex = fileIndex;
			m_Width = width;
			m_Height = height;
			m_Season = season;
			m_Name = name;
			m_Rules = rules;
			m_Regions = new Dictionary<string, Region>( StringComparer.OrdinalIgnoreCase );
			m_InvalidSector = new Sector( 0, 0, this );
			m_SectorsWidth = width >> SectorShift;
			m_SectorsHeight = height >> SectorShift;
			m_Sectors = new Sector[m_SectorsWidth][];
		}

		#region GetSector
		public Sector GetSector( Point3D p )
		{
			return InternalGetSector( p.m_X >> SectorShift, p.m_Y >> SectorShift );
		}

		public Sector GetSector( Point2D p )
		{
			return InternalGetSector( p.m_X >> SectorShift, p.m_Y >> SectorShift );
		}

		public Sector GetSector( IPoint2D p )
		{
			return InternalGetSector( p.X >> SectorShift, p.Y >> SectorShift );
		}

		public Sector GetSector( int x, int y )
		{
			return InternalGetSector( x >> SectorShift, y >> SectorShift );
		}

		public Sector GetRealSector( int x, int y )
		{
			return InternalGetSector( x, y );
		}

		private Sector InternalGetSector( int x, int y )
		{
			if ( x >= 0 && x < m_SectorsWidth && y >= 0 && y < m_SectorsHeight )
			{
				Sector[] xSectors = m_Sectors[x];

				if ( xSectors == null )
					m_Sectors[x] = xSectors = new Sector[m_SectorsHeight];

				Sector sec = xSectors[y];

				if ( sec == null )
					xSectors[y] = sec = new Sector( x, y, this );

				return sec;
			}
			else
			{
				return m_InvalidSector;
			}
		}
		#endregion

		public void ActivateSectors( int cx, int cy )
		{
			for ( int x = cx - SectorActiveRange; x <= cx + SectorActiveRange; ++x )
			{
				for ( int y = cy - SectorActiveRange; y <= cy + SectorActiveRange; ++y )
				{
					Sector sect = GetRealSector( x, y );
					if ( sect != m_InvalidSector )
						sect.Activate();
				}
			}
		}

		public void DeactivateSectors( int cx, int cy )
		{
			for ( int x = cx - SectorActiveRange; x <= cx + SectorActiveRange; ++x )
			{
				for ( int y = cy - SectorActiveRange; y <= cy + SectorActiveRange; ++y )
				{
					Sector sect = GetRealSector( x, y );
					if ( sect != m_InvalidSector && !PlayersInRange( sect, SectorActiveRange ) )
						sect.Deactivate();
				}
			}
		}

		private bool PlayersInRange( Sector sect, int range )
		{
			for ( int x = sect.X - range; x <= sect.X + range; ++x )
			{
				for ( int y = sect.Y - range; y <= sect.Y + range; ++y )
				{
					Sector check = GetRealSector( x, y );
					if ( check != m_InvalidSector && check.Players.Count > 0 )
						return true;
				}
			}

			return false;
		}

		public void OnClientChange( NetState oldState, NetState newState, Mobile m )
		{
			if ( this == Map.Internal )
				return;

			GetSector( m ).OnClientChange( oldState, newState );
		}

		public void OnEnter( Mobile m )
		{
			if ( this == Map.Internal )
				return;

			Sector sector = GetSector( m );

			sector.OnEnter( m );
		}

		public void OnEnter( Item item )
		{
			if ( this == Map.Internal )
				return;

			GetSector( item ).OnEnter( item );

			if ( item is BaseMulti )
			{
				BaseMulti m = (BaseMulti)item;
				MultiComponentList mcl = m.Components;

				Sector start = GetMultiMinSector( item.Location, mcl );
				Sector end = GetMultiMaxSector( item.Location, mcl );

				AddMulti( m, start, end );
			}
		}

		public void OnLeave( Mobile m )
		{
			if ( this == Map.Internal )
				return;

			Sector sector = GetSector( m );

			sector.OnLeave( m );
		}

		public void OnLeave( Item item )
		{
			if ( this == Map.Internal )
				return;

			GetSector( item ).OnLeave( item );

			if ( item is BaseMulti )
			{
				BaseMulti m = (BaseMulti)item;
				MultiComponentList mcl = m.Components;

				Sector start = GetMultiMinSector( item.Location, mcl );
				Sector end = GetMultiMaxSector( item.Location, mcl );

				RemoveMulti( m, start, end );
			}
		}

		public void RemoveMulti( BaseMulti m, Sector start, Sector end )
		{
			if ( this == Map.Internal )
				return;

			for ( int x = start.X; x <= end.X; ++x )
				for ( int y = start.Y; y <= end.Y; ++y )
					InternalGetSector( x, y ).OnMultiLeave( m );
		}

		public void AddMulti( BaseMulti m, Sector start, Sector end )
		{
			if ( this == Map.Internal )
				return;

			for ( int x = start.X; x <= end.X; ++x )
				for ( int y = start.Y; y <= end.Y; ++y )
					InternalGetSector( x, y ).OnMultiEnter( m );
		}

		public Sector GetMultiMinSector( Point3D loc, MultiComponentList mcl )
		{
			return GetSector( Bound( new Point2D( loc.m_X + mcl.Min.m_X, loc.m_Y + mcl.Min.m_Y ) ) );
		}

		public Sector GetMultiMaxSector( Point3D loc, MultiComponentList mcl )
		{
			return GetSector( Bound( new Point2D( loc.m_X + mcl.Max.m_X, loc.m_Y + mcl.Max.m_Y ) ) );
		}

		public void OnMove( Point3D oldLocation, Mobile m )
		{
			if ( this == Map.Internal )
				return;

			Sector oldSector = GetSector( oldLocation );
			Sector newSector = GetSector( m.Location );

			if ( oldSector != newSector )
			{
				oldSector.OnLeave( m );
				newSector.OnEnter( m );
			}
		}

		public void OnMove( Point3D oldLocation, Item item )
		{
			if ( this == Map.Internal )
				return;

			Sector oldSector = GetSector( oldLocation );
			Sector newSector = GetSector( item.Location );

			if ( oldSector != newSector )
			{
				oldSector.OnLeave( item );
				newSector.OnEnter( item );
			}

			if ( item is BaseMulti )
			{
				BaseMulti m = (BaseMulti)item;
				MultiComponentList mcl = m.Components;

				Sector start = GetMultiMinSector( item.Location, mcl );
				Sector end = GetMultiMaxSector( item.Location, mcl );

				Sector oldStart = GetMultiMinSector( oldLocation, mcl );
				Sector oldEnd = GetMultiMaxSector( oldLocation, mcl );

				if ( oldStart != start || oldEnd != end )
				{
					RemoveMulti( m, oldStart, oldEnd );
					AddMulti( m, start, end );
				}
			}
		}

		private object tileLock = new object();

		public TileMatrix Tiles
		{
			get
			{
				if (m_Tiles == null)
					lock (tileLock)
						if (m_Tiles == null)
							m_Tiles = new TileMatrix(this, m_FileIndex, m_MapID, m_Width, m_Height);

				return m_Tiles;
			}
		}

		public int MapID
		{
			get
			{
				return m_MapID;
			}
		}

		public int MapIndex
		{
			get
			{
				return m_MapIndex;
			}
		}

		public int Width
		{
			get
			{
				return m_Width;
			}
		}

		public int Height
		{
			get
			{
				return m_Height;
			}
		}

		public Dictionary<string, Region> Regions
		{
			get
			{
				return m_Regions;
			}
		}

		public void RegisterRegion( Region reg )
		{
			string regName = reg.Name;

			if ( regName != null )
			{
				if ( m_Regions.ContainsKey( regName ) )
					Console.WriteLine( "Warning: Duplicate region name '{0}' for map '{1}'", regName, this.Name );
				else
					m_Regions[regName] = reg;
			}
		}

		public void UnregisterRegion( Region reg )
		{
			string regName = reg.Name;

			if ( regName != null )
				m_Regions.Remove( regName );
		}

		public Region DefaultRegion
		{
			get
			{
				if ( m_DefaultRegion == null )
					m_DefaultRegion = new Region( null, this, 0, new Rectangle3D[0] );

				return m_DefaultRegion;
			}
			set
			{
				m_DefaultRegion = value;
			}
		}

		public MapRules Rules
		{
			get
			{
				return m_Rules;
			}
			set
			{
				m_Rules = value;
			}
		}

		public Sector InvalidSector
		{
			get
			{
				return m_InvalidSector;
			}
		}

		public string Name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;
			}
		}

		#region Enumerables
		public class NullEnumerable<T> : IPooledEnumerable<T>
		{
			private InternalEnumerator<T> m_Enumerator;

			public static readonly NullEnumerable<T> Instance = new NullEnumerable<T>();

			private NullEnumerable() {
				m_Enumerator = new InternalEnumerator<T>();
			}

			IEnumerator IEnumerable.GetEnumerator() { return m_Enumerator; }
			public IEnumerator<T> GetEnumerator() { return m_Enumerator; }

			public void Free() { }

			private class InternalEnumerator<K> : IEnumerator<K> {
				public void Reset() { }
				object IEnumerator.Current { get { return null; } }
				public K Current { get { return default(K); } }
				public bool MoveNext() { return false; }
				void IDisposable.Dispose() { }
			}
		}

		private class PooledEnumerable<T> : IPooledEnumerable<T>, IDisposable
		{
			private IPooledEnumerator<T> m_Enumerator;

			private static Queue<PooledEnumerable<T>> m_InstancePool = new Queue<PooledEnumerable<T>>();

			public static PooledEnumerable<T> Instantiate(IPooledEnumerator<T> etor)
			{
				PooledEnumerable<T> e = null;

				lock (m_InstancePool) {
					if ( m_InstancePool.Count > 0 ) {
						e = m_InstancePool.Dequeue();
						e.m_Enumerator = etor;
					}
				}

				if (e == null )
					e = new PooledEnumerable<T>( etor );

				return e;
			}

			private PooledEnumerable(IPooledEnumerator<T> etor)
			{
				m_Enumerator = etor;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				if (m_Enumerator == null)
					throw new ObjectDisposedException("PooledEnumerable", "GetEnumerator() called after Free()");

				return m_Enumerator;
			}

			public IEnumerator<T> GetEnumerator()
			{
				if ( m_Enumerator == null )
					throw new ObjectDisposedException( "PooledEnumerable", "GetEnumerator() called after Free()" );

				return m_Enumerator;
			}

			public void Free()
			{
				if ( m_Enumerator != null) {
						m_Enumerator.Free();
						m_Enumerator = null;
				}

				lock (m_InstancePool) {
					if (m_InstancePool.Count < 200) // Arbitrary
						m_InstancePool.Enqueue( this );
				}
			}

			public void Dispose()
			{
				// Don't return disposed objects to the instance pool
				//Free();

				if (m_Enumerator != null) {
					m_Enumerator.Free();
					m_Enumerator = null;
				}
			}
		}
		#endregion

		#region Enumerators
		private class ClientEnumerator : IPooledEnumerator<NetState>
		{
			private Map m_Map;
			private Rectangle2D m_Bounds;

			private int m_xSector, m_ySector;
			private int m_xSectorStart, m_ySectorStart;
			private int m_xSectorEnd, m_ySectorEnd;
			private List<NetState> m_CurrentList;
			private int m_CurrentIndex;

			private static Queue<ClientEnumerator> m_InstancePool = new Queue<ClientEnumerator>();

			public static ClientEnumerator Instantiate(Map map, Rectangle2D bounds)
			{
				ClientEnumerator e = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count > 0)
					{
						e = m_InstancePool.Dequeue();

						e.m_Map = map;
						e.m_Bounds = bounds;
					}
				}

				if (e == null)
				{
					e = new ClientEnumerator(map, bounds);
				}

				e.Reset();

				return e;
			}

			public void Free()
			{
				if (m_Map == null)
					return;

				m_Map = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count < 200) // Arbitrary
						m_InstancePool.Enqueue(this);
				}
			}

			private ClientEnumerator(Map map, Rectangle2D bounds)
			{
				m_Map = map;
				m_Bounds = bounds;
			}

			public NetState Current
			{
				get
				{
					return m_CurrentList[m_CurrentIndex];
				}
			}

			object IEnumerator.Current { get { return m_CurrentList[m_CurrentIndex]; } }

			void IDisposable.Dispose() { }

			public bool MoveNext()
			{
				while (true)
				{
					++m_CurrentIndex;

					if (m_CurrentIndex == m_CurrentList.Count)
					{
						++m_ySector;

						if (m_ySector > m_ySectorEnd)
						{
							m_ySector = m_ySectorStart;
							++m_xSector;

							if (m_xSector > m_xSectorEnd)
							{
								m_CurrentIndex = -1;
								return false;
							}
						}

						m_CurrentIndex = -1;
						m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Clients;
					}
					else
					{
						Mobile m = m_CurrentList[m_CurrentIndex].Mobile;

						if ( m != null && !m.Deleted && m_Bounds.Contains( m.Location ) )
							return true;
					}
				}
			}

			public void Reset()
			{
				m_Map.Bound(m_Bounds.Start.m_X, m_Bounds.Start.m_Y, out m_xSectorStart, out m_ySectorStart);
				m_Map.Bound(m_Bounds.End.m_X - 1, m_Bounds.End.m_Y - 1, out m_xSectorEnd, out m_ySectorEnd);

				m_xSector = m_xSectorStart >>= Map.SectorShift;
				m_ySector = m_ySectorStart >>= Map.SectorShift;

				m_xSectorEnd >>= Map.SectorShift;
				m_ySectorEnd >>= Map.SectorShift;

				m_CurrentIndex = -1;
				m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Clients;
			}
		}

		private class EntityEnumerator : IPooledEnumerator<IEntity>
		{
			private Map m_Map;
			private Rectangle2D m_Bounds;

			private int m_xSector, m_ySector;
			private int m_xSectorStart, m_ySectorStart;
			private int m_xSectorEnd, m_ySectorEnd;
			private int m_Stage;
			private IList m_CurrentList;
			private int m_CurrentIndex;

			private static Queue<EntityEnumerator> m_InstancePool = new Queue<EntityEnumerator>();

			public static EntityEnumerator Instantiate(Map map, Rectangle2D bounds)
			{
				EntityEnumerator e = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count > 0)
					{
						e = m_InstancePool.Dequeue();

						e.m_Map = map;
						e.m_Bounds = bounds;
					}
				}

				if (e == null)
				{
					e = new EntityEnumerator(map, bounds);
				}

				e.Reset();

				return e;
			}

			public void Free()
			{
				if (m_Map == null)
					return;

				m_Map = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count < 200) // Arbitrary
						m_InstancePool.Enqueue(this);
				}
			}

			private EntityEnumerator(Map map, Rectangle2D bounds)
			{
				m_Map = map;
				m_Bounds = bounds;
			}

			public IEntity Current
			{
				get
				{
					return (IEntity)m_CurrentList[m_CurrentIndex];
				}
			}

			object IEnumerator.Current { get { return m_CurrentList[m_CurrentIndex]; } }

			void IDisposable.Dispose() { }

			public bool MoveNext()
			{
				while (true)
				{
					++m_CurrentIndex;

					if (m_CurrentIndex < 0 || m_CurrentIndex > m_CurrentList.Count) { // Sanity
						Console.WriteLine("EntityEnumerator OOB: {0}", m_CurrentIndex);
						return false;
					}

					if (m_CurrentIndex == m_CurrentList.Count)
					{
						++m_ySector;

						if (m_ySector > m_ySectorEnd)
						{
							m_ySector = m_ySectorStart;
							++m_xSector;

							if (m_xSector > m_xSectorEnd)
							{
								if (m_Stage > 0) {
									m_CurrentIndex = -1;
									return false;
								}
								++m_Stage;
								m_xSector = m_xSectorStart >>= Map.SectorShift;
								m_ySector = m_ySectorStart >>= Map.SectorShift;
							}
						}

						m_CurrentIndex = -1;

						if (m_Stage == 0)
							m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Items;
						else
							m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Mobiles;
					}
					else
					{
						IEntity e = (IEntity)m_CurrentList[m_CurrentIndex];

						if (e is Item)
						{
							Item item = (Item)e;

							if (!item.Deleted && item.Parent == null && m_Bounds.Contains(e.Location))
								return true;
						}
						else if (e is Mobile)
						{
							Mobile m = (Mobile)e;
							if (!m.Deleted && m_Bounds.Contains(e.Location))
								return true;
						}
					}
				}
			}

			public void Reset()
			{
				m_Map.Bound(m_Bounds.Start.m_X, m_Bounds.Start.m_Y, out m_xSectorStart, out m_ySectorStart);
				m_Map.Bound(m_Bounds.End.m_X - 1, m_Bounds.End.m_Y - 1, out m_xSectorEnd, out m_ySectorEnd);

				m_xSector = m_xSectorStart >>= Map.SectorShift;
				m_ySector = m_ySectorStart >>= Map.SectorShift;

				m_xSectorEnd >>= Map.SectorShift;
				m_ySectorEnd >>= Map.SectorShift;

				m_CurrentIndex = -1;
				m_Stage = 0;
				m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Items;
			}
		}

		private class ItemEnumerator : IPooledEnumerator<Item>
		{
			private Map m_Map;
			private Rectangle2D m_Bounds;

			private int m_xSector, m_ySector;
			private int m_xSectorStart, m_ySectorStart;
			private int m_xSectorEnd, m_ySectorEnd;
			private List<Item> m_CurrentList;
			private int m_CurrentIndex;

			private static Queue<ItemEnumerator> m_InstancePool = new Queue<ItemEnumerator>();

			public static ItemEnumerator Instantiate(Map map, Rectangle2D bounds)
			{
				ItemEnumerator e = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count > 0)
					{
						e = m_InstancePool.Dequeue();

						e.m_Map = map;
						e.m_Bounds = bounds;
					}
				}

				if (e == null)
				{
					e = new ItemEnumerator(map, bounds);
				}

				e.Reset();

				return e;
			}

			public void Free()
			{
				if (m_Map == null)
					return;

				m_Map = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count < 200) // Arbitrary
						m_InstancePool.Enqueue(this);
				}
			}

			private ItemEnumerator(Map map, Rectangle2D bounds)
			{
				m_Map = map;
				m_Bounds = bounds;
			}

			public Item Current
			{
				get
				{
					return m_CurrentList[m_CurrentIndex];
				}
			}

			object IEnumerator.Current { get { return m_CurrentList[m_CurrentIndex]; } }

			void IDisposable.Dispose() { }

			public bool MoveNext()
			{
				while (true)
				{
					++m_CurrentIndex;

					if (m_CurrentIndex == m_CurrentList.Count)
					{
						++m_ySector;

						if (m_ySector > m_ySectorEnd)
						{
							m_ySector = m_ySectorStart;
							++m_xSector;

							if (m_xSector > m_xSectorEnd)
							{
								m_CurrentIndex = -1;
								return false;
							}
						}

						m_CurrentIndex = -1;
						m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Items;
					}
					else
					{
						Item item = m_CurrentList[m_CurrentIndex];

						if (!item.Deleted && item.Parent == null && m_Bounds.Contains(item.Location))
							return true;
					}
				}
			}

			public void Reset()
			{
				m_Map.Bound(m_Bounds.Start.m_X, m_Bounds.Start.m_Y, out m_xSectorStart, out m_ySectorStart);
				m_Map.Bound(m_Bounds.End.m_X - 1, m_Bounds.End.m_Y - 1, out m_xSectorEnd, out m_ySectorEnd);

				m_xSector = m_xSectorStart >>= Map.SectorShift;
				m_ySector = m_ySectorStart >>= Map.SectorShift;

				m_xSectorEnd >>= Map.SectorShift;
				m_ySectorEnd >>= Map.SectorShift;

				m_CurrentIndex = -1;
				m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Items;
			}
		}

		private class MobileEnumerator : IPooledEnumerator<Mobile>
		{
			private Map m_Map;
			private Rectangle2D m_Bounds;

			private int m_xSector, m_ySector;
			private int m_xSectorStart, m_ySectorStart;
			private int m_xSectorEnd, m_ySectorEnd;
			private List<Mobile> m_CurrentList;
			private int m_CurrentIndex;

			private static Queue<MobileEnumerator> m_InstancePool = new Queue<MobileEnumerator>();

			public static MobileEnumerator Instantiate(Map map, Rectangle2D bounds)
			{
				MobileEnumerator e = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count > 0)
					{
						e = m_InstancePool.Dequeue();

						e.m_Map = map;
						e.m_Bounds = bounds;
					}
				}

				if (e == null)
				{
					e = new MobileEnumerator(map, bounds);
				}

				e.Reset();

				return e;
			}

			public void Free()
			{
				if (m_Map == null)
					return;

				m_Map = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count < 200) // Arbitrary
						m_InstancePool.Enqueue(this);
				}
			}

			private MobileEnumerator(Map map, Rectangle2D bounds)
			{
				m_Map = map;
				m_Bounds = bounds;
			}

			public Mobile Current
			{
				get
				{
					return m_CurrentList[m_CurrentIndex];
				}
			}

			object IEnumerator.Current { get { return m_CurrentList[m_CurrentIndex]; } }

			void IDisposable.Dispose() { }

			public bool MoveNext()
			{
				while (true)
				{
					++m_CurrentIndex;

					if (m_CurrentIndex == m_CurrentList.Count)
					{
						++m_ySector;

						if (m_ySector > m_ySectorEnd)
						{
							m_ySector = m_ySectorStart;
							++m_xSector;

							if (m_xSector > m_xSectorEnd)
							{
								m_CurrentIndex = -1;
								return false;
							}
						}

						m_CurrentIndex = -1;
						m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Mobiles;
					}
					else
					{
						Mobile m = m_CurrentList[m_CurrentIndex];

						if ( !m.Deleted && m_Bounds.Contains( m.Location ) )
							return true;
					}
				}
			}

			public void Reset()
			{
				m_Map.Bound(m_Bounds.Start.m_X, m_Bounds.Start.m_Y, out m_xSectorStart, out m_ySectorStart);
				m_Map.Bound(m_Bounds.End.m_X - 1, m_Bounds.End.m_Y - 1, out m_xSectorEnd, out m_ySectorEnd);

				m_xSector = m_xSectorStart >>= Map.SectorShift;
				m_ySector = m_ySectorStart >>= Map.SectorShift;

				m_xSectorEnd >>= Map.SectorShift;
				m_ySectorEnd >>= Map.SectorShift;

				m_CurrentIndex = -1;
				m_CurrentList = m_Map.InternalGetSector(m_xSector, m_ySector).Mobiles;
			}
		}

		private class MultiTileEnumerator : IPooledEnumerator<StaticTile[]>
		{
			private List<BaseMulti> m_List;
			private Point2D m_Location;
			private StaticTile[] m_Current;
			private int m_Index;

			private static Queue<MultiTileEnumerator> m_InstancePool = new Queue<MultiTileEnumerator>();

			public static MultiTileEnumerator Instantiate(Sector sector, Point2D loc)
			{
				MultiTileEnumerator e = null;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count > 0)
					{
						e = m_InstancePool.Dequeue();

						e.m_List = sector.Multis;
						e.m_Location = loc;
					}
				}

				if (e == null)
				{
					e = new MultiTileEnumerator(sector, loc);
				}

				e.Reset();

				return e;
			}

			private MultiTileEnumerator(Sector sector, Point2D loc)
			{
				m_List = sector.Multis;
				m_Location = loc;
			}

			public StaticTile[] Current { get { return m_Current; } }

			object IEnumerator.Current { get { return m_Current; } }

			void IDisposable.Dispose() { }

			public bool MoveNext()
			{
				while (++m_Index < m_List.Count)
				{
					BaseMulti m = m_List[m_Index];

					if (m != null && !m.Deleted)
					{
						MultiComponentList list = m.Components;

						int xOffset = m_Location.m_X - (m.Location.m_X + list.Min.m_X);
						int yOffset = m_Location.m_Y - (m.Location.m_Y + list.Min.m_Y);

						if (xOffset >= 0 && xOffset < list.Width && yOffset >= 0 && yOffset < list.Height)
						{
							StaticTile[] tiles = list.Tiles[xOffset][yOffset];

							if (tiles.Length > 0)
							{
								// TODO: How to avoid this copy?
								StaticTile[] copy = new StaticTile[tiles.Length];

								for (int i = 0; i < copy.Length; ++i)
								{
									copy[i] = tiles[i];
									copy[i].Z += m.Z;
								}

								m_Current = copy;
								return true;
							}
						}
					}
				}

				return false;
			}

			public void Free()
			{
				if (m_List == null)
					return;

				lock (m_InstancePool)
				{
					if (m_InstancePool.Count < 200) // Arbitrary
						m_InstancePool.Enqueue(this);

					m_List = null;
				}
			}

			public void Reset()
			{
				m_Current = null;
				m_Index = -1;
			}
		}
		#endregion

		public Point3D GetPoint( object o, bool eye )
		{
			Point3D p;

			if( o is Mobile )
			{
				p = ((Mobile)o).Location;
				p.Z += 14;//eye ? 15 : 10;
			}
			else if( o is Item )
			{
				p = ((Item)o).GetWorldLocation();
				p.Z += (((Item)o).ItemData.Height / 2) + 1;
			}
			else if( o is Point3D )
			{
				p = (Point3D)o;
			}
			else if( o is LandTarget )
			{
				p = ((LandTarget)o).Location;

				int low = 0, avg = 0, top = 0;
				GetAverageZ( p.X, p.Y, ref low, ref avg, ref top );

				p.Z = top + 1;
			}
			else if( o is StaticTarget )
			{
				StaticTarget st = (StaticTarget)o;
				ItemData id = TileData.ItemTable[st.ItemID & TileData.MaxItemValue];

				p = new Point3D( st.X, st.Y, st.Z - id.CalcHeight + (id.Height / 2) + 1 );
			}
			else if( o is IPoint3D )
			{
				p = new Point3D( (IPoint3D)o );
			}
			else
			{
				Console.WriteLine( "Warning: Invalid object ({0}) in line of sight", o );
				p = Point3D.Zero;
			}

			return p;
		}

		#region Line Of Sight
		private static int m_MaxLOSDistance = 25;

		public static int MaxLOSDistance
		{
			get { return m_MaxLOSDistance; }
			set { m_MaxLOSDistance = value; }
		}

		public bool LineOfSight( Point3D org, Point3D dest )
		{
            if (this == Map.Internal)
            {
                return false;
            }

            if (!Utility.InRange(org, dest, m_MaxLOSDistance))
            {

                return false;
            }

			Point3D start = org;
			Point3D end = dest;

			if( org.X > dest.X || (org.X == dest.X && org.Y > dest.Y) || (org.X == dest.X && org.Y == dest.Y && org.Z > dest.Z) )
			{
				Point3D swap = org;
				org = dest;
				dest = swap;
			}

			double rise, run, zslp;
			double sq3d;
			double x, y, z;
			int xd, yd, zd;
			int ix, iy, iz;
			int height;
			bool found;
			Point3D p;
			Point3DList path = new Point3DList();
			TileFlag flags;

            if (org == dest)
            {
                return true;
            }

			if( path.Count > 0 )
				path.Clear();

			xd = dest.m_X - org.m_X;
			yd = dest.m_Y - org.m_Y;
			zd = dest.m_Z - org.m_Z;
			zslp = Math.Sqrt( xd * xd + yd * yd );
			if( zd != 0 )
				sq3d = Math.Sqrt( zslp * zslp + zd * zd );
			else
				sq3d = zslp;

			rise = ((float)yd) / sq3d;
			run = ((float)xd) / sq3d;
			zslp = ((float)zd) / sq3d;

			y = org.m_Y;
			z = org.m_Z;
			x = org.m_X;
			while( Utility.NumberBetween( x, dest.m_X, org.m_X, 0.5 ) && Utility.NumberBetween( y, dest.m_Y, org.m_Y, 0.5 ) && Utility.NumberBetween( z, dest.m_Z, org.m_Z, 0.5 ) )
			{
				ix = (int)Math.Round( x );
				iy = (int)Math.Round( y );
				iz = (int)Math.Round( z );
				if( path.Count > 0 )
				{
					p = path.Last;

					if( p.m_X != ix || p.m_Y != iy || p.m_Z != iz )
						path.Add( ix, iy, iz );
				}
				else
				{
					path.Add( ix, iy, iz );
				}
				x += run;
				y += rise;
				z += zslp;
			}

            if (path.Count == 0)
            {
                return true;//<--should never happen, but to be safe.
            }

			p = path.Last;

			if( p != dest )
				path.Add( dest );

			Point3D pTop = org, pBottom = dest;
			Utility.FixPoints( ref pTop, ref pBottom );

			int pathCount = path.Count;
			int endTop = end.m_Z + 1;

			for( int i = 0; i < pathCount; ++i )
			{
				Point3D point = path[i];
				int pointTop = point.m_Z + 1;

				LandTile landTile = Tiles.GetLandTile( point.X, point.Y );
				int landZ = 0, landAvg = 0, landTop = 0;
				GetAverageZ( point.m_X, point.m_Y, ref landZ, ref landAvg, ref landTop );

                if (landZ <= pointTop && landTop >= point.m_Z && (point.m_X != end.m_X || point.m_Y != end.m_Y || landZ > endTop || landTop < end.m_Z) && !landTile.Ignored)
                {
                    return false;
                }

				/* --Do land tiles need to be checked?  There is never land between two people, always statics.--
				LandTile landTile = Tiles.GetLandTile( point.X, point.Y );
				if ( landTile.Z-1 >= point.Z && landTile.Z+1 <= point.Z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0 )
					return false;
				*/

				StaticTile[] statics = Tiles.GetStaticTiles( point.m_X, point.m_Y, true );

				bool contains = false;
				int ltID = landTile.ID;

				for( int j = 0; !contains && j < m_InvalidLandTiles.Length; ++j )
					contains = (ltID == m_InvalidLandTiles[j]);

				if( contains && statics.Length == 0 )
				{
					IPooledEnumerable<Item> eable = GetItemsInRange( point, 0 );

					foreach( Item item in eable )
					{
						if( item.Visible )
							contains = false;

						if( !contains )
							break;
					}

					eable.Free();

                    if (contains)
                    {
                        return false;
                    }
				}

				for( int j = 0; j < statics.Length; ++j )
				{
					StaticTile t = statics[j];

					ItemData id = TileData.ItemTable[t.ID & TileData.MaxItemValue];

					flags = id.Flags;
					height = id.CalcHeight;

					if( t.Z <= pointTop && t.Z + height >= point.Z && (flags & (TileFlag.Window | TileFlag.NoShoot)) != 0 )
					{
						if( point.m_X == end.m_X && point.m_Y == end.m_Y && t.Z <= endTop && t.Z + height >= end.m_Z )
							continue;
                        
						return false;
					}

					/*if ( t.Z <= point.Z && t.Z+height >= point.Z && (flags&TileFlag.Window)==0 && (flags&TileFlag.NoShoot)!=0
						&& ( (flags&TileFlag.Wall)!=0 || (flags&TileFlag.Roof)!=0 || (((flags&TileFlag.Surface)!=0 && zd != 0)) ) )*/
					/*{
						//Console.WriteLine( "LoS: Blocked by Static \"{0}\" Z:{1} T:{3} P:{2} F:x{4:X}", TileData.ItemTable[t.ID&TileData.MaxItemValue].Name, t.Z, point, t.Z+height, flags );
						//Console.WriteLine( "if ( {0} && {1} && {2} && ( {3} || {4} || {5} || ({6} && {7} && {8}) ) )", t.Z <= point.Z, t.Z+height >= point.Z, (flags&TileFlag.Window)==0, (flags&TileFlag.Impassable)!=0, (flags&TileFlag.Wall)!=0, (flags&TileFlag.Roof)!=0, (flags&TileFlag.Surface)!=0, t.Z != dest.Z, zd != 0 ) ;
						return false;
					}*/
				}
			}

			Rectangle2D rect = new Rectangle2D( pTop.m_X, pTop.m_Y, (pBottom.m_X - pTop.m_X) + 1, (pBottom.m_Y - pTop.m_Y) + 1 );

			IPooledEnumerable<Item> area = GetItemsInBounds( rect );

			foreach( Item i in area )
			{
				if( !i.Visible )
					continue;

				if( i is BaseMulti || i.ItemID > TileData.MaxItemValue )
					continue;

				ItemData id = i.ItemData;
				flags = id.Flags;

				if( (flags & (TileFlag.Window | TileFlag.NoShoot)) == 0 )
					continue;

				height = id.CalcHeight;

				found = false;

				int count = path.Count;

				for( int j = 0; j < count; ++j )
				{
					Point3D point = path[j];
					int pointTop = point.m_Z + 1;
					Point3D loc = i.Location;

					//if ( t.Z <= point.Z && t.Z+height >= point.Z && ( height != 0 || ( t.Z == dest.Z && zd != 0 ) ) )
					if( loc.m_X == point.m_X && loc.m_Y == point.m_Y &&
						loc.m_Z <= pointTop && loc.m_Z + height >= point.m_Z )
					{
						if( loc.m_X == end.m_X && loc.m_Y == end.m_Y && loc.m_Z <= endTop && loc.m_Z + height >= end.m_Z )
							continue;

						found = true;
						break;
					}
				}

				if( !found )
					continue;

				area.Free();
                
				return false;

				/*if ( (flags & (TileFlag.Impassable | TileFlag.Surface | TileFlag.Roof)) != 0 )

				//flags = TileData.ItemTable[i.ItemID&TileData.MaxItemValue].Flags;
				//if ( (flags&TileFlag.Window)==0 && (flags&TileFlag.NoShoot)!=0 && ( (flags&TileFlag.Wall)!=0 || (flags&TileFlag.Roof)!=0 || (((flags&TileFlag.Surface)!=0 && zd != 0)) ) )
				{
					//height = TileData.ItemTable[i.ItemID&TileData.MaxItemValue].Height;
					//Console.WriteLine( "LoS: Blocked by ITEM \"{0}\" P:{1} T:{2} F:x{3:X}", TileData.ItemTable[i.ItemID&TileData.MaxItemValue].Name, i.Location, i.Location.Z+height, flags );
					area.Free();
					return false;
				}*/
			}

			area.Free();

			return true;
		}

		public bool LineOfSight( object from, object dest )
		{
			if ( from == dest || ( from is Mobile && ( (Mobile) from ).AccessLevel > AccessLevel.Player ) )
				return true;
			else if ( dest is Item && from is Mobile && ( (Item) dest ).RootParent == from )
				return true;

			return LineOfSight( GetPoint( from, true ), GetPoint( dest, false ) );
		}

		public bool LineOfSight( Mobile from, Point3D target )
		{
			if ( from.AccessLevel > AccessLevel.Player )
				return true;

			Point3D eye = from.Location;

			eye.Z += 14;

			return LineOfSight( eye, target );
		}

		public bool LineOfSight( Mobile from, Mobile to )
		{
			if ( from == to || from.AccessLevel > AccessLevel.Player )
				return true;

			Point3D eye = from.Location;
			Point3D target = to.Location;

			eye.Z += 14;
			target.Z += 14;//10;

			return LineOfSight( eye, target );
		}

        public bool InLOS(Point3D from, Point3D to)
        {
            if (from == to)
                return true;

            Point3D eye = from;
            Point3D target = to;

            eye.Z += 14;
            target.Z += 14;

            return LineOfSight(eye, target);
        }

		#endregion

		private static int[] m_InvalidLandTiles = new int[] { 0x244 };

		public static int[] InvalidLandTiles
		{
			get { return m_InvalidLandTiles; }
			set { m_InvalidLandTiles = value; }
		}

		public int CompareTo( Map other )
		{
			if ( other == null )
				return -1;

			return m_MapID.CompareTo( other.m_MapID );
		}

		public int CompareTo( object other )
		{
			if ( other == null || other is Map )
				return this.CompareTo( other );

			throw new ArgumentException();
		}
	}
}