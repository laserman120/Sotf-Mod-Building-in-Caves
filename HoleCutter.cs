using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using g3; // Import geometry3Sharp namespace
using gs; // Import geometry3Sharp.Unity namespace

namespace AllowBuildInCaves
{
    
    public class TerrainMeshModifier : MonoBehaviour
    {
        public Terrain terrain;
        public Vector3 holeCenter;
        public float holeRadius = 2f;

        private void Start()
        {
            if (terrain == null)
            {
                Debug.LogError("Terrain not assigned to TerrainHoleCreator!");
                return;
            }

            CreateHole();
        }

        public void CreateHole()
        {
            var terrainData = terrain.terrainData;
            int heightmapWidth = terrainData.heightmapResolution;
            int heightmapHeight = terrainData.heightmapResolution;
            Vector3 terrainSize = terrainData.size;
        }
    }
}
