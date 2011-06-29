using System.Collections.Generic;
using TerrainDisplay.Collision;
using WCell.Terrain.MPQ.WMO;
using WCell.Util.Graphics;

namespace WCell.Terrain.MPQ.ADT
{
    public abstract class ADTBase
    {
        public List<int> LiquidIndices;
        public List<Vector3> LiquidVertices;
        public List<int> Indices;
        public List<Vector3> TerrainVertices;
        
        public abstract void GenerateLiquidVertexAndIndices();
        public abstract void GenerateHeightVertexAndIndices();

        /// <summary>
        /// Adds the rendering liquid vertices to the provided list for the MapChunk given by:
        /// </summary>
        /// <param name="indexY">The y index of the map chunk.</param>
        /// <param name="indexX">The x index of the map chunk</param>
        /// <param name="vertices">The Collection to add the vertices to.</param>
        /// <returns>The number of vertices added.</returns>
        public abstract int GenerateLiquidVertices(int indexY, int indexX, ICollection<Vector3> vertices);

        /// <summary>
        /// Adds the rendering liquid indices to the provided list for the MapChunk given by:
        /// </summary>
        /// <param name="indexY">The y index of the map chunk.</param>
        /// <param name="indexX">The x index of the map chunk</param>
        /// <param name="offset">The number to add to the indices so as to match the end of the Vertices list.</param>
        /// <param name="indices">The Collection to add the indices to.</param>
        public abstract void GenerateLiquidIndices(int indexY, int indexX, int offset, List<int> indices);

        /// <summary>
        /// Adds the rendering liquid vertices to the provided list for the MapChunk given by:
        /// </summary>
        /// <param name="indexY">The y index of the map chunk.</param>
        /// <param name="indexX">The x index of the map chunk</param>
        /// <param name="vertices">The Collection to add the vertices to.</param>
        /// <returns>The number of vertices added.</returns>
        public abstract int GenerateHeightVertices(int indexY, int indexX, ICollection<Vector3> vertices);

        /// <summary>
        /// Adds the rendering indices to the provided list for the MapChunk given by:
        /// </summary>
        /// <param name="indexY">The y index of the map chunk.</param>
        /// <param name="indexX">The x index of the map chunk</param>
        /// <param name="offset">The number to add to the indices so as to match the end of the Vertices list.</param>
        /// <param name="indices">The Collection to add the indices to.</param>
        public abstract void GenerateHeightIndices(int indexY, int indexX, int offset, List<int> indices);

        public abstract bool GetPotentialColliders(Ray2D ray2D, List<Index3> results);
    }
}