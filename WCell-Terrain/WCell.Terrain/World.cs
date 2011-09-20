﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Core.Terrain;
using WCell.Constants.World;

namespace WCell.Terrain
{
    public class World
    {
        public Dictionary<MapId, Terrain> WorldTerrain;

        public SimpleTerrain GetOrLoadSimpleTerrain(MapId map)
        {
            Terrain terrain;
            if (!WorldTerrain.TryGetValue(map, out terrain))
            {
                terrain = new SimpleTerrain(map, false);
                terrain.FillTileProfile();
                WorldTerrain.Add(map, terrain);
            }
            return (SimpleTerrain)terrain;
        }

        public SimpleWDTTerrain GetOrLoadSimpleWDTTerrain(MapId map)
        {
            Terrain terrain;
            if (!WorldTerrain.TryGetValue(map, out terrain))
            {
                terrain = new SimpleWDTTerrain(map, false);
                terrain.FillTileProfile();
                WorldTerrain.Add(map, terrain);
            }
            return (SimpleWDTTerrain)terrain;
        }

        public World()
        {
            WorldTerrain = new Dictionary<MapId, Terrain>(TileIdentifier.InternalMapNames.Count);
        }
    }
}
