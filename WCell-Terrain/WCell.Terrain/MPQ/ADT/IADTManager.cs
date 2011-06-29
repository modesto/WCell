﻿using System.Collections.Generic;
using TerrainDisplay;

namespace WCell.Terrain.MPQ.ADT
{
    public interface IADTManager
    {
        IList<ADTBase> MapTiles { get; }

        /// <summary>
        /// Loads an ADT into the manager.
        /// </summary>
        /// <param name="tileX">X coordinate of the ADT in the 64 x 64 Grid 
        /// (The x-axis points into the screen and represents rows in the grid.)</param>
        /// <param name="tileY">Y coordinate of the ADT in the 64 x 64 grid 
        /// (The y-axis points left and also represents columns in the grid.)</param>
        bool LoadTile(TileIdentifier tileId);
    }
}