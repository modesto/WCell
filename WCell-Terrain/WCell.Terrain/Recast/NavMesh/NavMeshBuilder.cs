﻿using System;
using System.Diagnostics;
using System.IO;
using WCell.Constants;
using WCell.Util.Graphics;
using WCell.Terrain.Collision;

namespace WCell.Terrain.Recast.NavMesh
{
	public class NavMeshBuilder
	{
		public static string GenerateTileName(TerrainTile tile)
		{
			return string.Format("tile_{0}", TerrainConstants.GetTileName(tile.TileX, tile.TileY));
		}

		public static int GenerateTileId(TerrainTile tile)
		{
			return tile.TileX + tile.TileY * TerrainConstants.TilesPerMapSide;
		}

		public static Point2D GetTileCoords(int tileId)
		{
			return new Point2D(tileId % TerrainConstants.TilesPerMapSide, tileId / TerrainConstants.TilesPerMapSide);
		}

		public readonly Terrain Terrain;
		private readonly RecastAPI.BuildMeshCallback SmashMeshDlgt;

		public NavMeshBuilder(Terrain terrain)
		{
			Terrain = terrain;
			SmashMeshDlgt = SmashMesh;
		}

		public string InputMeshPrefix
		{
			get
			{
				Directory.CreateDirectory(WCellTerrainSettings.RecastInputMeshFolder);
				return WCellTerrainSettings.RecastInputMeshFolder + Terrain.MapId;
			}
		}

		public string NavMeshPrefix
		{
			get
			{
				Directory.CreateDirectory(WCellTerrainSettings.RecastNavMeshFolder);
				return WCellTerrainSettings.RecastNavMeshFolder + Terrain.MapId;
			}
		}

		public void ExportRecastInputMesh(TerrainTile tile, string filename)
		{
			if (File.Exists(filename)) return;			// skip existing files

			var verts = tile.TerrainVertices;
			var indices = tile.TerrainIndices;

			var start = DateTime.Now;
			Console.Write("Writing file {0}...", filename);

			using (var file = new StreamWriter(filename))
			{
				foreach (var vertex in verts)
				{
					var v = vertex;
					RecastUtil.TransformWoWCoordsToRecastCoords(ref v);
					file.WriteLine("v {0} {1} {2}", v.X, v.Y, v.Z);
				}

				// write faces
				for (var i = 0; i < indices.Length; i += 3)
				{
					//file.WriteLine("f {0} {1} {2}", indices[i] + 1, indices[i + 1] + 1, indices[i + 2] + 1);
					file.WriteLine("f {0} {1} {2}", indices[i + 2] + 1, indices[i + 1] + 1, indices[i] + 1);
				}
			}
			Console.WriteLine("Done. - Exported {0} triangles in: {1:0.000}s",
									indices.Length / 3, (DateTime.Now - start).TotalSeconds);
		}

		/// <summary>
		/// Builds the mesh for a single tile.
		/// NOTE: This sets the Terrain's NavMesh property.
		/// In order to move between tiles, we need to generate one mesh for the entire map
		/// </summary>
		public void BuildMesh(TerrainTile tile)
		{
			// export to file
			var tileName = GenerateTileName(tile);
			var inputMeshFile = InputMeshPrefix + "_" + tileName + RecastAPI.InputMeshExtension;
			var navMeshFile = NavMeshPrefix + "_" + tileName + RecastAPI.NavMeshExtension;
			ExportRecastInputMesh(tile, inputMeshFile);

			// let recast to build the navmesh and then call our callback
			var isNew = !File.Exists(navMeshFile);
			if (isNew)
			{
				Console.Write("Building new NavMesh - This will take a while...");
			}
			var start = DateTime.Now;
			var result = RecastAPI.BuildMesh(
				GenerateTileId(tile),
				inputMeshFile,
				navMeshFile,
				SmashMeshDlgt);

			if (result == 0)
			{
				throw new Exception("Could not build mesh for tile " + tileName + " in map " + Terrain.MapId);
			}

			if (isNew)
			{
				Console.WriteLine("Done in {0:0.000}s", (DateTime.Now - start).TotalSeconds);
			}

			//// move all vertices above the surface
			//var mesh = tile.Terrain.NavMesh;
			//for (var i = 0; i < mesh.Vertices.Length; i++)
			//{
			//    var vert = mesh.Vertices[i];
			//    vert.Z += 0.001f;
			//    var ray = new Ray(vert, Vector3.Down);	// see if vertex is above surface
			//    var hit = tile.FindFirstHitTriangle(ray);
			//    if (hit == -1)
			//    {
			//        vert.Z -= 0.001f;

			//        // vertex is below surface
			//        ray = new Ray(vert, Vector3.Up);	// find surface right above mesh
			//        hit = tile.FindFirstHitTriangle(ray);
			//        if (hit != -1)
			//        {
			//            // set vertex height equal to terrain
			//            var tri = tile.GetTriangle(hit);
			//            var plane = new Plane(tri.Point1, tri.Point2, tri.Point3);
			//            Intersection.LineSegmentIntersectsPlane(ray.Position, ray.Direction, plane, out mesh.Vertices[i]);
			//        }
			//    }
			//}
		} 

		/// <summary>
		/// Put it all together
		/// </summary>
		private unsafe void SmashMesh(
			int userId,
			int vertComponentCount,
			int polyCount,
			IntPtr vertComponentsPtr,

			int totalPolyIndexCount,				// count of all vertex indices in all polys
			IntPtr pIndexCountsPtr,
			IntPtr pIndicesPtr,
			IntPtr pNeighborsPtr,
			IntPtr pFlagsPtr,
			IntPtr polyAreasAndTypesPtr
			)
		{
			//var coords = GetTileCoords(userId);
			//var tile = Terrain.GetTile(coords);

			//if (tile == null)
			//{
			//    throw new Exception("Invalid tile: " + id);
			//}

			// read native data
			var vertComponents = (float*)vertComponentsPtr;
			var pIndexCounts = (byte*)pIndexCountsPtr;
			var pIndices = (uint*)pIndicesPtr;
			var pNeighbors = (uint*)pNeighborsPtr;
			var pFlags = (ushort*)pFlagsPtr;
			var polyAreasAndTypes = (byte*) polyAreasAndTypesPtr;


			// create vertices array
			var indices = new int[totalPolyIndexCount];
			var vertCount = vertComponentCount / 3;
			var vertices = new Vector3[vertCount];
			for (int i = 0, v = 0; i < vertCount; i++, v += 3)
			{
			    vertices[i] = new Vector3(vertComponents[v], vertComponents[v + 1], vertComponents[v + 2]);
			    RecastUtil.TransformRecastCoordsToWoWCoords(ref vertices[i]);
			}

			// polygon first pass -> Create polygons
			var polys = new NavMeshPolygon[polyCount];
			var polyEdgeIndex = 0;
			var p = 0;
			for (var i = 0; i < polyCount; i++)
			{
			    var poly = polys[i] = new NavMeshPolygon();
			    var polyIndexCount = pIndexCounts[i];
				poly.Indices = new int[polyIndexCount];

				Debug.Assert(3 == polyIndexCount);

			    for (var j = 0; j < polyIndexCount; j++)
			    {
			    	var idx = (int) pIndices[polyEdgeIndex + j];
					indices[p++] = idx;
			        poly.Indices[j] = idx;

			        Debug.Assert(poly.Indices[j] >= 0 && poly.Indices[j] < vertCount);
			    }

			    polyEdgeIndex += polyIndexCount;
			}

			// polygon second pass -> Initialize neighbors
			polyEdgeIndex = 0;
			for (var i = 0; i < polyCount; i++)
			{
			    var poly = polys[i];
			    var polyIndexCount = pIndexCounts[i];
				poly.Neighbors = new int[polyIndexCount];
				var a = poly.Indices[0];
				var b = poly.Indices[1];
				var c = poly.Indices[2];

			    for (var j = 0; j < polyIndexCount; j++)
			    {
			        var neighbor = (int)pNeighbors[polyEdgeIndex + j];
			        if (neighbor == -1) continue;

			        var neighborPoly = polys[neighbor];
					
					// sort the neighbor poly into the array of neighbors, correctly
					var a2 = neighborPoly.Indices[0];
					var b2 = neighborPoly.Indices[1];
					var c2 = neighborPoly.Indices[2];

					var nCount = 0;
					var mask = 0;
					if (a == a2 || a == b2 || a == c2)
					{
						// some vertex matches the first vertex of the triangle
						nCount++;
						mask |= WCellTerrainConstants.TrianglePointA;
					}
					if (b == a2 || b == b2 || b == c2)
					{
						// some vertex matches the second vertex of the triangle
						nCount++;
						mask |= WCellTerrainConstants.TrianglePointB;
					}
					if (c == a2 || c == b2 || c == c2)
					{
						// some vertex matches the third vertex of the triangle
						nCount++;
						mask |= WCellTerrainConstants.TrianglePointC;
					}

					if (nCount == 2)
					{
						// we have a neighbor
						switch (mask)
						{
							case WCellTerrainConstants.ABEdgeMask:
								// neighbor shares a and b
								poly.Neighbors[WCellTerrainConstants.ABEdgeIndex] = neighbor;
								break;
							case WCellTerrainConstants.ACEdgeMask:
								// second shares a and c
								poly.Neighbors[WCellTerrainConstants.ACEdgeIndex] = neighbor;
								break;
							case WCellTerrainConstants.BCEdgeMask:
								// neighbor shares b and c
								poly.Neighbors[WCellTerrainConstants.BCEdgeIndex] = neighbor;
								break;
							default:
								throw new Exception("Two neighboring polygons don't share an edge");

						}
					}
			    }

			    polyEdgeIndex += polyIndexCount;
			}

			// create new NavMesh object
			Terrain.NavMesh = new NavMesh(Terrain, polys, vertices, indices);
		}
	}
}
