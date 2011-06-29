﻿using System.Collections.Generic;
using WCell.Constants.World;
using WCell.Util.Graphics;
using WCell.Terrain.MPQ.ADT.Components;
using WCell.Terrain.MPQ.M2;
using TerrainDisplay.Extracted.M2;

namespace TerrainDisplay.Extracted
{
    public class ExtractedM2Manager : IM2Manager
    {
        private MapId _mapId;
        private string _basePath;
        public List<ExtractedM2> Models;

        private List<Vector3> _renderVertices;
        private List<int> _renderIndices;

        public List<Vector3> RenderVertices
        {
            get
            {
                if (_renderVertices == null)
                {
                    GenerateVerticesAndIndices();
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
                    GenerateVerticesAndIndices();
                }
                return _renderIndices;
            }
            set { _renderIndices = value; }
        }

        public ExtractedM2Manager(string basePath, MapId mapId)
        {
            _basePath = basePath;
            _mapId = mapId;
            Models = new List<ExtractedM2>();
        }

        public void Add(MapDoodadDefinition doodadDefinition)
        {
            if (doodadDefinition is ExtractedMapM2Definition)
            {
                Add((ExtractedMapM2Definition)doodadDefinition);
            }
        }

        private void Add(ExtractedMapM2Definition def)
        {
            var model = ExtractedM2Parser.Process(_basePath, _mapId, def.FilePath);

            Transform(model, def);

            Models.Add(model);
        }

        private static void Transform(ExtractedM2 m2, ExtractedMapM2Definition def)
        {
            for (var i = 0; i < m2.BoundingVertices.Count; i++)
            {
                // Scale and transform
                var vertex = m2.BoundingVertices[i];

                Vector3 rotatedVector;
                Vector3.Transform(ref vertex, ref def.ModelToWorld, out rotatedVector);

                // Translate
                Vector3 finalVector;
                Vector3.Add(ref rotatedVector, ref def.Position, out finalVector);

                m2.BoundingVertices[i] = finalVector;
                //currentM2.Vertices.Add(new VertexPositionNormalColored(finalVector, Color.Red, Vector3.Up));
            }
        }

        private void GenerateVerticesAndIndices()
        {
            _renderVertices = new List<Vector3>();
            _renderIndices = new List<int>();

            foreach (var m2 in Models)
            {
                if (m2 == null) continue;

                var offset = _renderVertices.Count;
                foreach (var vec in m2.BoundingVertices)
                {
                    _renderVertices.Add(vec);
                }
                
                foreach (var index3 in m2.BoundingTriangles)
                {
                    _renderIndices.Add(index3.Index0 + offset);
                    _renderIndices.Add(index3.Index1 + offset);
                    _renderIndices.Add(index3.Index2 + offset);
                }
                
            }
        }
    }
}
