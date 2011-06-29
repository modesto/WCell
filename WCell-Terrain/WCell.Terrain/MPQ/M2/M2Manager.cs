﻿using System;
using System.Collections.Generic;
using System.IO;
using TerrainDisplay;
using TerrainDisplay.Collision;
using TerrainDisplay.Collision.QuadTree;
using WCell.Terrain.MPQ.ADT.Components;
using WCell.Util.Graphics;
using Matrix = WCell.Util.Graphics.Matrix;
using Vector3 = WCell.Util.Graphics.Vector3;

namespace WCell.Terrain.MPQ.M2
{
    public class M2Manager : IM2Manager
    {
        #region variables
        /// <summary>
        /// List of filenames managed by this M2Manager
        /// </summary>
        private readonly List<String> _names = new List<String>();

        /// <summary>
        /// List of WMOs managed by this WMOManager
        /// </summary>
        public List<M2> M2s = new List<M2>();

        #endregion

        private List<Vector3> _renderVertices;
        private List<int> _renderIndices;

        public List<Vector3> RenderVertices
        {
            get
            {
                if (_renderVertices == null)
                {
                    GenerateRenderVerticesAndIndices();
                }
                return _renderVertices;
            }
            set { _renderVertices = value; }
        }

        public List<int> RenderIndices
        {
            get
            {
                if (_renderIndices == null)
                {
                    GenerateRenderVerticesAndIndices();
                }
                return _renderIndices;
            }
            set { _renderIndices = value; }
        }

        private void GenerateRenderVerticesAndIndices()
        {
            _renderVertices = new List<Vector3>();
            _renderIndices = new List<int>();

            var offset = 0;
            foreach (var m2 in M2s)
            {
                foreach (var vector in m2.Vertices)
                {
                    _renderVertices.Add(vector);
                }
                foreach (var index in m2.Indices)
                {
                    _renderIndices.Add(index + offset);
                }
                offset = _renderVertices.Count;
            }
        }

        /// <summary>
        /// Add a M2 to this manager.
        /// </summary>
        /// <param name="doodadDefinition">MDDF (placement information) for this M2</param>
        public void Add(MapDoodadDefinition doodadDefinition)
        {
            var filePath = doodadDefinition.FilePath;

            if (Path.GetExtension(filePath).Equals(".mdx") || 
                Path.GetExtension(filePath).Equals(".mdl"))
            {
                filePath = Path.ChangeExtension(filePath, ".m2");
            }

			lock (_names)
			{
				_names.Add(filePath);
			}
        	var currentM2 = Process(doodadDefinition);
            
            // Ignore models with no bounding volumes
            if (currentM2.Vertices.Count < 1) return;
            
			lock (M2s)
			{
				M2s.Add(currentM2);
			}
        }

        private static M2 Process(MapDoodadDefinition doodadDefinition)
        {
            var model = M2ModelParser.Process(MpqTerrainManager.MpqManager, doodadDefinition.FilePath);

            var tempIndices = new List<int>();
            foreach (var tri in model.BoundingTriangles)
            {
                tempIndices.Add(tri.Index2);
                tempIndices.Add(tri.Index1);
                tempIndices.Add(tri.Index0);
            }

            var currentM2 = Transform(model, tempIndices, doodadDefinition);
            var tempVertices = currentM2.Vertices;
            var bounds = new BoundingBox(tempVertices.ToArray());
            currentM2.Bounds = bounds;

            return currentM2;
        }

        private static M2 Transform(M2Model model, IEnumerable<int> indicies, MapDoodadDefinition mddf)
        {
            var currentM2 = new M2();
            currentM2.Vertices.Clear();
            currentM2.Indices.Clear();

            var posX = (mddf.Position.X - TerrainConstants.CenterPoint)*-1;
            var posY = (mddf.Position.Y - TerrainConstants.CenterPoint)*-1;
            var origin = new Vector3(posX, posY, mddf.Position.Z);
            
            // Create the scale matrix used in the following loop.
            Matrix scaleMatrix;
            Matrix.CreateScale(mddf.Scale, out scaleMatrix);

            // Creation the rotations
            var rotateZ = Matrix.CreateRotationZ(MathHelper.ToRadians(mddf.OrientationB + 180));
            var rotateY = Matrix.CreateRotationY(MathHelper.ToRadians(mddf.OrientationA));
            var rotateX = Matrix.CreateRotationX(MathHelper.ToRadians(mddf.OrientationC));

            var worldMatrix = Matrix.Multiply(scaleMatrix, rotateZ);
            worldMatrix = Matrix.Multiply(worldMatrix, rotateX);
            worldMatrix = Matrix.Multiply(worldMatrix, rotateY);

            for (var i = 0; i < model.BoundingVertices.Length; i++)
            {
                var position = model.BoundingVertices[i];
                var normal = model.BoundingNormals[i];

                // Scale and Rotate
                Vector3 rotatedPosition;
                Vector3.Transform(ref position, ref worldMatrix, out rotatedPosition);

                Vector3 rotatedNormal;
                Vector3.Transform(ref normal, ref worldMatrix, out rotatedNormal);
                rotatedNormal.Normalize();

                // Translate
                Vector3 finalVector;
                Vector3.Add(ref rotatedPosition, ref origin, out finalVector);

                currentM2.Vertices.Add(finalVector);
            }
            
            currentM2.Indices.AddRange(indicies);
            return currentM2;
        }
    }
}