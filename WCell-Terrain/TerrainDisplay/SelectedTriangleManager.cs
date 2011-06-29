﻿using System;
using System.Collections.Generic;
using WCell.Terrain.MPQ.ADT;
using WCell.Terrain.MPQ.WMO;
using TerrainDisplay.Util;
using WCell.Util.Graphics;

namespace TerrainDisplay.Collision
{
	/// <summary>
	/// TODO: Fix me
	/// </summary>
    public class SelectedTriangleManager
    {
        private readonly IADTManager _adtManager;

        public List<Vector3> Vertices { get; private set; }
        public List<int> Indices { get; private set; }

        private void AddSelectedTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var offset = Vertices.Count;
            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);

            Indices.Add(offset + 0);
            Indices.Add(offset + 1);
            Indices.Add(offset + 2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectRay">A ray in Xna Coords that was cast according to the viewport</param>
        /// <param name="add">Whether to add the selected triangle to the selection</param>
        public void UpdateSelectedTriangle(Ray selectRay, bool add)
        {
            if (!add)
            {
                Vertices.Clear();
                Indices.Clear();
            }

            var pos3D = selectRay.Position;
            var dir3D = selectRay.Direction;
            XNAUtil.TransformXnaCoordsToWoWCoords(ref pos3D);
            XNAUtil.TransformXnaCoordsToWoWCoords(ref dir3D);
            var ray3D = new Ray(pos3D, dir3D);

            var pos2D = new Vector2(pos3D.X, pos3D.Y);
            var dir2D = new Vector2(dir3D.X, dir3D.Y).NormalizedCopy();
            var ray2D = new Ray2D(pos2D, dir2D);

            var closestTime = float.MaxValue;
            var closestVec0 = Vector3.Zero;
            var closestVec1 = Vector3.Zero;
            var closestVec2 = Vector3.Zero;

            foreach (var tile in _adtManager.MapTiles)
            {
                var results = new List<Index3>();
                if (!tile.GetPotentialColliders(ray2D, results)) continue;

                foreach (var tri in results)
                {
                    var vec0 = tile.TerrainVertices[tri.Index0];
                    var vec1 = tile.TerrainVertices[tri.Index1];
                    var vec2 = tile.TerrainVertices[tri.Index2];

                    float time;
                    if (!Intersection.RayTriangleIntersect(ray3D, vec0, vec1, vec2, out time)) continue;
                    if (time > closestTime) continue;

                    closestTime = time;
                    closestVec0 = vec0;
                    closestVec1 = vec1;
                    closestVec2 = vec2;
                }
            }
            if (closestTime == float.MaxValue) return;
            AddSelectedTriangle(closestVec0, closestVec1, closestVec2);
        }

        public SelectedTriangleManager(IADTManager adtManager)
        {
            _adtManager = adtManager;
            Vertices = new List<Vector3>();
            Indices = new List<int>();
        }
    }
}
