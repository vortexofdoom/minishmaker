﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinishMaker.Utilities;

namespace MinishMaker.Core
{
	public class RoomMetaData
	{
		private int width, height, mapPosX, mapPosY;
		public int PixelWidth
		{
			get
			{
				return width * 16;
			}
		}

		public int PixelHeight
		{
			get
			{
				return height * 16;
			}
		}

		public int TileWidth
		{
			get
			{
				return width;
			}
		}

		public int TileHeight
		{
			get
			{
				return height;
			}
		}

		public int MapPosX
		{
			get
			{
				return mapPosX;
			}
		}

		public int MapPosY
		{
			get
			{
				return mapPosY;
			}
		}

        private string roomPath;

		private int paletteSetID;
		private List<AddrData> tileSetAddrs = new List<AddrData>();

		private List<ChestData> chestInformation = new List<ChestData>();
		public List<ChestData> ChestInfo
		{
			get { return chestInformation;}
		}

		private AddrData? bg2RoomDataAddr;
		private AddrData bg2MetaTilesAddr;

		private AddrData? bg1RoomDataAddr;
		private AddrData bg1MetaTilesAddr;

		private bool chestDataLarger = false;
		public bool ChestDataLarger
		{
			get { return chestDataLarger;}
		}

		private bool bg1Use20344B0 = false;
		public bool Bg1Use20344B0
		{
			get
			{
				return bg1Use20344B0;
			}
		}

		public struct AddrData
		{
			public int src;
			public int dest;
			public int size; // in words (2x bytes)
			public bool compressed;

			public AddrData( int src, int dest, int size, bool compressed )
			{
				this.src = src;
				this.dest = dest;
				this.size = size;
				this.compressed = compressed;
			}
		}

		public struct ChestData
		{
			public byte type;
            public byte chestId;
            public byte itemId;
            public byte itemSubNumber;
            public ushort chestLocation;
            public ushort unknown;

			public ChestData(byte type, byte chestId, byte itemId, byte itemSubNumber, ushort chestLocation, ushort other)
			{
				this.type = type;
				this.chestId = chestId;
				this.itemId = itemId;
				this.itemSubNumber = itemSubNumber;
				this.chestLocation = chestLocation;
				this.unknown = other;
			}
		}

		public RoomMetaData( int areaIndex, int roomIndex )
		{
			LoadMetaData( areaIndex, roomIndex );
		}

		private void LoadMetaData( int areaIndex, int roomIndex )
		{
            roomPath = Project.Instance.projectPath + "/Areas/Area " + StringUtil.AsStringHex2(areaIndex) + "/Room " + StringUtil.AsStringHex2(roomIndex);

            var r = ROM.Instance.reader;
			var header = ROM.Instance.headers;

			int areaRMDTableLoc = r.ReadAddr( header.MapHeaderBase + (areaIndex << 2) );
			int roomMetaDataTableLoc = areaRMDTableLoc + (roomIndex * 0x0A);
			this.mapPosX = r.ReadUInt16( roomMetaDataTableLoc )>>4;
			this.mapPosY = r.ReadUInt16()>>4;
			this.width = r.ReadUInt16() >> 4; //bytes 5+6 pixels/16 = tiles
			this.height = r.ReadUInt16() >> 4;                          //bytes 7+8 pixels/16 = tiles

			//get addr of TPA data
			int tileSetOffset = r.ReadUInt16() << 2;                    //bytes 9+10

			int areaTileSetTableLoc = r.ReadAddr( header.globalTileSetTableLoc + (areaIndex << 2) );
			int roomTileSetLoc = r.ReadAddr( areaTileSetTableLoc + tileSetOffset );

			r.SetPosition( roomTileSetLoc );

			ParseData( r, Set1 );

			//metatiles
			int metaTileSetsLoc = r.ReadAddr( header.globalMetaTileSetTableLoc + (areaIndex << 2) );

			r.SetPosition( metaTileSetsLoc );

			ParseData( r, Set2 );

			//get addr of room data 
			int areaTileDataTableLoc = r.ReadAddr( header.globalTileDataTableLoc + (areaIndex << 2) );
			int tileDataLoc = r.ReadAddr( areaTileDataTableLoc + (roomIndex << 2) );
			r.SetPosition( tileDataLoc );

			ParseData( r, Set3 );

			//attempt at obtaining chest data (+various)
			int areaEntityTableAddrLoc = header.AreaMetadataBase + (areaIndex << 2);
            int areaEntityTableAddr = r.ReadAddr(areaEntityTableAddrLoc);

            int roomEntityTableAddrLoc = areaEntityTableAddr + (roomIndex << 2);
            int roomEntityTableAddr =r.ReadAddr(roomEntityTableAddrLoc);

            //4 byte chunks, 1-3 are unknown use, 4th seems to be chests
            string chestDataPath = roomPath + "/" + (int)DataType.chestData;
            if (File.Exists(chestDataPath))
            {
                byte[] data = File.ReadAllBytes(chestDataPath);
                int index = 0;
                while (index < data.Length && (TileEntityType)data[index] != TileEntityType.None)
                {
                    var type = data[index];
                    var id = data[index + 1];
                    var item = data[index + 2];
                    var subNum = data[index + 3];
                    ushort loc = (ushort)(data[index + 4] | (data[index + 5] << 8));
                    ushort other = (ushort)(data[index + 6] | (data[index + 7] << 8));
                    chestInformation.Add(new ChestData(type, id, item, subNum, loc, other));
                    index += 8;
                }
            } 
            else
            {
                int chestTableAddr = r.ReadAddr(roomEntityTableAddr + 12);

                var data = r.ReadBytes(8, chestTableAddr);

                while ((TileEntityType)data[0] != TileEntityType.None) //ends on type 0
                {
                    var type = data[0];
                    var id = data[1];
                    var item = data[2];
                    var subNum = data[3];
                    ushort loc = (ushort)(data[4] | (data[5] << 8));
                    ushort other = (ushort)(data[6] | (data[7] << 8));
                    chestInformation.Add(new ChestData(type, id, item, subNum, loc, other));
                    data = r.ReadBytes(8);
                }
            }
		}

		public TileSet GetTileSet()
		{
            string tilesetPath = roomPath + "/" + (int)DataType.tileSet;
            if (File.Exists(tilesetPath))
            {
                return new TileSet(File.ReadAllBytes(tilesetPath));
            }
            else
            {
                return new TileSet(tileSetAddrs);
            }
		}

		public PaletteSet GetPaletteSet()
		{
            return new PaletteSet(paletteSetID);
		}

		public bool GetBG2Data( ref byte[] bg2RoomData, ref MetaTileSet bg2MetaTiles )
		{
			if( bg2RoomDataAddr != null )
			{
				bg2MetaTiles = new MetaTileSet( bg2MetaTilesAddr, false );

                byte[] data = null;
                string bg2Path = roomPath + "/" + (int)DataType.bg2Data;
                if (File.Exists(bg2Path)) {
                    data = new byte[0x2000];
                    byte[] savedData = File.ReadAllBytes(bg2Path);

                    using (MemoryStream os = new MemoryStream(data))
                    {
                        using (MemoryStream ms = new MemoryStream(savedData))
                        {
                            Reader r = new Reader(ms);
                            DataHelper.Lz77Decompress(r, os);
                        }
                    }
                }
                else
                {
                    data = DataHelper.GetData((AddrData)bg2RoomDataAddr);
                }
				data.CopyTo( bg2RoomData, 0 );

				return true;
			}
			return false;
		}

		public bool GetBG1Data( ref byte[] bg1RoomData, ref MetaTileSet bg1MetaTiles )
		{
			if( bg1RoomDataAddr != null )
			{
                byte[] data = null;
                string bg1Path = roomPath + "/" + (int)DataType.bg1Data;
                if (File.Exists(bg1Path))
                {
                    data = new byte[0x2000];
                    byte[] savedData = File.ReadAllBytes(bg1Path);

                    using (MemoryStream os = new MemoryStream(data))
                    {
                        using (MemoryStream ms = new MemoryStream(savedData))
                        {
                            Reader r = new Reader(ms);
                            DataHelper.Lz77Decompress(r, os);
                        }
                    }
                }
                else
                {
                    data = DataHelper.GetData((AddrData)bg1RoomDataAddr);
                }

				if( !bg1Use20344B0 )
                {
					bg1MetaTiles = new MetaTileSet( bg1MetaTilesAddr , true );
				}

                data.CopyTo(bg1RoomData, 0);
                return true;
			}
			return false;
		}

		private void ParseData( Reader r, Func<AddrData, bool> postFunc )
		{
			var header = ROM.Instance.headers;
			bool cont = true;
			while( cont )
			{
				UInt32 data = r.ReadUInt32();
				UInt32 data2 = r.ReadUInt32();
				UInt32 data3 = r.ReadUInt32();

				

				if( data2 == 0 )
				{ //palette
					this.paletteSetID = (int)(data & 0x7FFFFFFF); //mask off high bit
				}
				else
				{
					int source = (int)((data & 0x7FFFFFFF) + header.gfxSourceBase); //08324AE4 is tile gfx base
					int dest = (int)(data2 & 0x7FFFFFFF);
					bool compressed = (data3 & 0x80000000) != 0; //high bit of size determines LZ or DMA
					int size = (int)(data3 & 0x7FFFFFFF);

					cont = postFunc( new AddrData( source, dest, size, compressed ) );
				}
				if(cont == true)
				{
					cont = (data & 0x80000000) != 0; //high bit determines if more to load
				}
			}
		}

		public long CompressBG1(ref byte[] outdata, byte[] bg1data)
		{
			var compressed = new byte[bg1data.Length];
			long totalSize = 0;
			MemoryStream ous = new MemoryStream( compressed );
			totalSize = DataHelper.Compress(bg1data, ous, false);

			outdata = new byte[totalSize];
			Array.Copy(compressed,outdata,totalSize);
            //var sizeDifference = totalSize - bg1RoomDataAddr.Value.size;

            totalSize |= 0x80000000;

			return totalSize;
		}

		public long CompressBG2(ref byte[] outdata,byte[] bg2data)
		{
			var compressed = new byte[bg2data.Length];
			long totalSize = 0;
			MemoryStream ous = new MemoryStream( compressed );
			totalSize = DataHelper.Compress(bg2data, ous, false);
			
			outdata = new byte[totalSize];
			Array.Copy(compressed,outdata,totalSize);
            //var sizeDifference = totalSize - bg2RoomDataAddr.Value.size;

            totalSize |= 0x80000000;

            return totalSize;
		}

		public long GetChestData(ref byte[] outdata )
		{
            //var size = (chestInformation.Count*8)+8;
            string chestDataPath = roomPath + "/" + (int)DataType.chestData;
            if (File.Exists(chestDataPath))
            {
                outdata = File.ReadAllBytes(chestDataPath);
                return outdata.Length;
            }

            outdata = new byte[chestInformation.Count*8+8];

			for(int i = 0; i< chestInformation.Count; i++)
			{
				var index = i*8;
				var data = chestInformation[i];
				outdata[index] = data.type;
				outdata[index+1] = data.chestId;
				outdata[index+2] = data.itemId;
				outdata[index+3] = data.itemSubNumber;
				byte high = (byte)(data.chestLocation>>8);
				byte low = (byte)(data.chestLocation-(high<<8));
				outdata[index+4] = low;
				outdata[index+5] = high;
				high = (byte)(data.unknown>>8);
				low = (byte)(data.unknown-(high<<8));
				outdata[index+6] = low;
				outdata[index+7] = high;

				if(i == chestInformation.Count-1)// add ending 0's
				{
					for(int j= 0; j<8;j++)
						outdata[index+8+j]=0;
				}
			}

            return outdata.Length;
		}

		public void AddChestData(ChestData data)
		{
			chestDataLarger = true; //larger so should be moved
			chestInformation.Add(data);
		}

		//To be changed as actual data gets changed and tested
		public int GetPointerLoc(DataType type, int areaIndex, int roomIndex)
		{
			var r = ROM.Instance.reader;
			var header = ROM.Instance.headers;
			int retAddr = 0;
			int areaRMDTableLoc = r.ReadAddr( header.MapHeaderBase + (areaIndex << 2) );
			int roomMetaDataTableLoc = areaRMDTableLoc + (roomIndex * 0x0A);

			switch(type)
			{
				case DataType.roomMetaData:
					retAddr = roomMetaDataTableLoc;
					break;

				case DataType.tileSet:
					//get addr of TPA data
					int tileSetOffset = r.ReadUInt16(roomMetaDataTableLoc+8) << 2;                    //bytes 9+10

					int areaTileSetTableLoc = r.ReadAddr( header.globalTileSetTableLoc + (areaIndex << 2) );
					int roomTileSetAddrLoc = areaTileSetTableLoc + tileSetOffset;
					retAddr = roomTileSetAddrLoc;
					break;

				case DataType.metaTileSet:
					int metaTileSetsAddrLoc = r.ReadAddr( header.globalMetaTileSetTableLoc + (areaIndex << 2) );
					retAddr = metaTileSetsAddrLoc;
					break;

				case DataType.bg1Data:
				case DataType.bg2Data:
					int areaTileDataTableLoc = r.ReadAddr( header.globalTileDataTableLoc + (areaIndex << 2) );
					int tileDataLoc = r.ReadAddr( areaTileDataTableLoc + (roomIndex << 2) );
					r.SetPosition( tileDataLoc );

					if(type == DataType.bg1Data)
					{
						ParseData(r,Bg1Check);
					}
					else //not bg1 so has to be bg2
					{
						ParseData(r,Bg2Check);
					}
					retAddr = (int)r.Position-12; //step back 12 bytes as the bg was found after reading
					break;

				case DataType.chestData:
					int areaEntityTableAddrLoc = header.AreaMetadataBase + (areaIndex << 2);
					int areaEntityTableAddr = r.ReadAddr(areaEntityTableAddrLoc);

					int roomEntityTableAddrLoc = areaEntityTableAddr + (roomIndex << 2);
					int roomEntityTableAddr = r.ReadAddr(roomEntityTableAddrLoc);

					//4 byte chunks, 1-3 are unknown use, 4th seems to be chests
					retAddr = roomEntityTableAddr + 0x0C;

                    Console.WriteLine(retAddr);
					break;

				default:
					break;
			}

			return retAddr;
		}

		//dont have any good names for these 3
		private bool Set1( AddrData data )
		{
			if( (data.dest & 0xF000000) != 0x6000000 )
			{ //not valid tile data addr
				Console.WriteLine( "Unhandled tile data destination address: " + data.dest.Hex() + " Source:" + data.src.Hex() + " Compressed:" + data.compressed + " Size:" + data.size.Hex() );
				return false;
			}

			data.dest = data.dest & 0xFFFFFF;
			this.tileSetAddrs.Add( data );
			return true;
		}

		private bool Set2( AddrData data )
		{
			//use a switch in case this data is out of order
			switch( data.dest )
			{
				case 0x0202CEB4:
					this.bg2MetaTilesAddr = data;
					Debug.WriteLine( data.src.Hex() + " bg2" );
					break;
				case 0x02012654:
					this.bg1MetaTilesAddr = data;
					Debug.WriteLine( data.src.Hex() + " bg1" );
					break;
				//case 0x0202AEB4:
				//ret[8] = source; //not handled
				//break;
				//case 0x02010654:
				//ret[9] = source; //not handled
				//break;
				default:
					Debug.Write( "Unhandled metatile addr: " );
					Debug.Write( data.src.Hex() + "->" + data.dest.Hex() );
					Debug.WriteLine( data.compressed ? " (compressed)" : "" );
					break;
			}
			return true;
		}

		private bool Set3( AddrData data )
		{
			switch( data.dest )
			{
				case 0x02025EB4:
					this.bg2RoomDataAddr = data;
					break;
				case 0x0200B654:
					this.bg1RoomDataAddr = data;
					break;
				case 0x02002F00:
					this.bg1RoomDataAddr = data;
					this.bg1Use20344B0 = true;
					break;
				default:
					Debug.Write( "Unhandled room data addr: " );
					Debug.Write( data.src.Hex() + "->" + data.dest.Hex() );
					Debug.WriteLine( data.compressed ? " (compressed)" : "" );
					break;
			}
			return true;
		}
		
		private bool Bg1Check(AddrData data)
		{
			switch(data.dest)
			{
				case 0x0200B654:
					return false;
				case 0x2002F00:
					return false;
				default:
					break;
			}
			return true;
		}

		private bool Bg2Check(AddrData data)
		{
			switch(data.dest)
			{
				case 0x02025EB4:
					return false;
				default:
					break;
			}
			return true;
		}
	}
}
