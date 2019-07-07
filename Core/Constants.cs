﻿using System;
using System.IO;
using System.Reflection;

namespace MinishMaker.Core
{
	public enum RegionVersion
	{
		EU,
		JP,
		US,
		None
	}

	public struct HeaderData
	{
		public int MapHeaderBase;
		// Name on this is gonna have to change sometime
		public int AreaMetadataBase;

		//added for now, change names to whatever you want
		public int tileOffset;
		public int paletteSetTableLoc;
		public int chunk0TableLoc;
		public int area1Chunk0TableLoc;
		public int chunk1TableLoc;
		public int chunk2TableLoc;
		public int swapBase;
		public int paletteChangeBase;
		public int area1SwapBase;
		public int globalTileSetTableLoc;
		public int gfxSourceBase;
		public int globalMetaTileSetTableLoc;
		public int globalTileDataTableLoc;
		public int areaInformationTableLoc;

		public HeaderData( int map, int area, int tileOffset, int paletteSetTableLoc, int c0TableLoc, int a1C0TableLoc, int c1TableLoc, int c2TableLoc, int swapBase, int paletteChangeBase, int area1SwapBase, int globalTileSetTableLoc, int gfxSourceBase, int globalMetaTileSetTableLoc, int globalTileDataTableLoc, int areaInformationTableLoc )
		{
			this.MapHeaderBase = map;
			this.AreaMetadataBase = area;
			this.tileOffset = tileOffset;
			this.paletteSetTableLoc = paletteSetTableLoc;
			this.chunk0TableLoc = c0TableLoc; //looks like each next chunkTable is a 0x16 further (eu and us), not adding because of possible jp
			this.area1Chunk0TableLoc = a1C0TableLoc;
			this.chunk1TableLoc = c1TableLoc; //c0+ 0x16
			this.chunk2TableLoc = c2TableLoc; //c0+ 0x32
			this.swapBase = swapBase;
			this.paletteChangeBase = paletteChangeBase;
			this.area1SwapBase = area1SwapBase; //above -0x140?
			this.globalTileSetTableLoc = globalTileSetTableLoc;
			this.gfxSourceBase = gfxSourceBase;
			this.globalMetaTileSetTableLoc = globalMetaTileSetTableLoc;
			this.globalTileDataTableLoc = globalTileDataTableLoc;
			this.areaInformationTableLoc = areaInformationTableLoc;
		}
	}

	public enum TileEntityType
	{
		None = 0x00,
		TestA = 0x01,
		Chest = 0x02,
		BigChest = 0x03,
		TestB = 0x04,
		TestC = 0x05,
	}

	public enum KinstoneType
	{
		UnTyped,

		YellowTornadoProng = 0x65,
		YellowTornadoSpike = 0x66,
		YellowTornadoChaotic = 0x67,
		//68 and 69 are repeats of above

		YellowTotemProng = 0x6A,
		YellowTotemWave = 0x6B,
		YellowTotemZigZag = 0x6C,

		YellowCrown = 0x6D,

		RedSpike = 0x6E,
		RedCrack = 0x6F,
		RedProng = 0x70,

		BlueL = 0x71,
		BlueS = 0x72,

		GreenSpike = 0x73,
		GreenSquare = 0x74,
		GreenSplit = 0x75,
	}

	public enum ItemType
	{
		Untyped,
		Bow = 0x09,

		Boomerang = 0x0C,

		LanternOff = 0x0F,

		GustJar = 0x11,
		PacciCane = 0x12,
		MoleMitts = 0x13,
		RocsCape = 0x14,
		PegasusBoots = 0x15,
		FireRod = 0x16,
		Ocarina = 0x17,
		Bottle1 = 0x1C,
		Bottle2 = 0x1D,
		Bottle3 = 0x1E,
		Bottle4 = 0x1F,
		BottleEmpty = 0x20,
		BottleButter = 0x21,
		BottleMilk = 0x22,
		BottleHalfMilk = 0x23,
		BottleRedPotion = 0x24,
		BottleBluePotion = 0x25,
		BottleWater = 0x26,
		BottleMineralWater = 0x27,
		BottleFairy = 0x28,
		BottlePicolyteRed = 0x29,
		BottlePicolyteOrange = 0x2A,
		BottlePicolyteYellow = 0x2B,
		BottlePiclolyteGreen = 0x2C,
		BottlePicolyteBlue = 0x2D,
		BottlePicolyteWhite = 0x2E,
		BottleCharmNayru = 0x2F,
		BottleCharmFarore = 0x30,
		BottleCharmDin = 0x31,

		ShellsX = 0x3F,

		PowerBracelets = 0x45,
		Flippers = 0X46,
		
		DungeonMap = 0x50,
		Compass = 0x51,
		BigKey = 0x52,
		SmallKey = 0x53,
		Rupee1 = 0x54,
		Rupee5 = 0x55,
		Rupee20 = 0x56,
		Rupee50 = 0x57,
		Rupee100 = 0x58,
		Rupee200 = 0x59,

		KinstoneX = 0x5C,

        PieceOfHeart = 0x63,
        Wallet = 0x64,

	}

	public class Header
	{
		// Will fill out when relevant, only need EU for now
		private readonly HeaderData[] headerTable = new HeaderData[]
		{   //             MAP     , ENTITY?,	TILEOFFSET	PALETTESET	CHUNK0		CHUNK0AREA1	CHUNK1		CHUNK2		SWAP		PALETTECHANGE	AREA1SWAP	TILESET		GFXSOURCE	METATILE	TILEDATA	AREADATA
            new HeaderData(0x11D95C, 0x0D4828,  0x5A23D0,   0xFED88,    0x107AEC,   0x1077AC,   0x107B02,   0x107B18,   0x107B5C,   0x107940,       0x107800,   0x101BC8,   0x323FEC,   0x1027F8,   0x1070E4,	0x127468),
			new HeaderData(0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0),
			new HeaderData(0x11E214, 0x0D50FC,	0x5A2E80,	0xFF850,	0x108398,	0x108050,	0x1083AE,	0x1083C4,	0x108408,	0x1081E4,		0x1080A4,	0x10246C,	0x324AE4,	0x10309C,	0x107988,	0x127D30)
		};

		public HeaderData GetHeaderAddresses( RegionVersion region )
		{
			return headerTable[(int)region];
		}
	}
}
