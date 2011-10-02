﻿using System;
using System.Collections.Generic;
using WCell.Constants;
using WCell.Constants.World;
using WCell.MPQTool;
using WCell.Terrain.MPQ.ADTs;
using WCell.Terrain.MPQ.DBC;
using WCell.Terrain.MPQ.WMOs;
using WCell.Terrain.Serialization;
using WCell.Util;
using WCell.Util.Graphics;
using System.IO;

namespace WCell.Terrain.MPQ
{
	/// <summary>
	/// Represents a WDT entity and all it's associated data
	/// </summary>
	public class WDT : Terrain
	{
        private const string baseDir = "WORLD\\MAPS\\";

		public MapInfo Entry;

		public int Version;
		public string Name;
		public string Filename;
		public readonly WDTHeader Header = new WDTHeader();

		public readonly List<string> WmoFiles = new List<string>();
		public readonly List<MapObjectDefinition> WmoDefinitions = new List<MapObjectDefinition>();

		public readonly Dictionary<string, M2> M2s = new Dictionary<string, M2>();
		public readonly Dictionary<string, WMORoot> WMOs = new Dictionary<string, WMORoot>();

		public WDT(MapId mapId)
			: base(mapId)
		{
		}

		public override bool IsWMOOnly
		{
			get { return Header.IsWMOMap; }
		}

		public override void FillTileProfile()
		{
			var mpqFinder = WCellTerrainSettings.GetDefaultMPQFinder();
		    var filePath = GetMapMPQDir(MapId);
		    var fileList = mpqFinder.GetAllFiles(Path.Combine(filePath, "*.adt"));

            foreach(var file in fileList)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(fileName)) continue;
                
                var parts = fileName.Split(new [] {'_'});
                var y = int.Parse(parts[1]);
                var x = int.Parse(parts[2]);

                TileProfile[x, y] = true;
            }
		}

		protected override TerrainTile LoadTile(int x, int y)
		{
			var adt = ADTReader.ReadADT(this, x, y);
			if (adt == null)
			{
				return null;
			}

            //adt.GenerateMapWithTerraSimplification();
            adt.GenerateMapWithNoSimplification();

			return adt;
		}

		public WMORoot GetOrReadWMO(MapObjectDefinition definition)
		{
			WMORoot wmo;
			if (!WMOs.TryGetValue(definition.FilePath, out wmo))
			{
				WMOs.Add(definition.FilePath, wmo = WMOReader.ReadWMO(WCellTerrainSettings.GetDefaultMPQFinder(), definition));
			}
			return wmo;
		}

		public M2 GetOrReadM2(MapDoodadDefinition definition)
		{
			M2 m2;
			if (!M2s.TryGetValue(definition.FilePath, out m2))
			{
				M2s.Add(definition.FilePath, m2 = M2Reader.ReadM2(WCellTerrainSettings.GetDefaultMPQFinder(), definition));
			}
			return m2;
		}

		/// <summary>
		/// Creates a dummy WDT and loads the given tile into it
		/// </summary>
		public static ADT LoadTile(MapId map, int x, int y)
		{
			var wdt = new WDT(map);
			wdt.TileProfile[x, y] = true;
			return (ADT)wdt.LoadTile(x, y);
		}

        private static string GetMapMPQDir(MapId map)
        {
            var mapName = TileIdentifier.GetName(map);
            return Path.Combine(baseDir, mapName);
        }

		//public void LoadZone(int zoneId)
		//{
		//    // Rows
		//    for (var x = 0; x < 64; x++)
		//    {
		//        // Columns
		//        for (var y = 0; y < 64; y++)
		//        {
		//            var tileExists = _wdtFile.TileProfile[y, x];
		//            if (!tileExists) continue;

		//            var _adtPath = "WORLD\\MAPS";
		//            var _basePath = Path.Combine(_baseDirectory, _adtPath);
		//            var _continent = "Azeroth";
		//            var continentPath = Path.Combine(_basePath, _continent);

		//            if (!Directory.Exists(continentPath))
		//            {
		//                throw new Exception("Continent data missing");
		//            }

		//            var filePath = string.Format("{0}\\{1}_{2:00}_{3:00}.adt", continentPath, _continent, x, y);

		//            if (!File.Exists(filePath))
		//            {
		//                throw new Exception("ADT Doesn't exist: " + filePath);
		//            }

		//            var adt = ADTParser.Process(filePath, this);

		//            for (var j = 0; j < 16; j++)
		//            {
		//                for (var i = 0; i < 16; i++)
		//                {
		//                    var mapChunk = adt.MapChunks[i, j];
		//                    if (mapChunk == null) continue;
		//                    if (mapChunk.Header.AreaId != zoneId) continue;
		//                }
		//            }

		//            _adtManager.MapTiles.Clear();
		//        }
		//    }
		//}

	}
}
