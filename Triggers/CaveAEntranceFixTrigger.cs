using RedLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheForest.Utils;
using UnityEngine;

namespace AllowBuildInCaves.Triggers
{
    [RegisterTypeInIl2Cpp]
    internal class CaveAEntranceFixTrigger : MonoBehaviour
    {

        private SphereCollider triggerCollider;
        public float radius = 4f;
        private TerrainCollider TerrainCollision;
        private GameObject Terrain;

        private void Start()
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            // No need to set isTrigger to true, as we're not using trigger events
            triggerCollider.radius = radius;

            findTerrain();
        }

        private void findTerrain()
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            Terrain = allObjects.FirstOrDefault(go => go.name == "Site02Terrain Tess");
            TerrainCollision = Terrain.GetComponent<TerrainCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TerrainCollision == null) findTerrain();
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                TerrainCollision.enabled = false;
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (TerrainCollision == null) findTerrain();
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                TerrainCollision.enabled = true;
            }
        }


        public void SetTriggerSize(float radius)
        {
            triggerCollider.radius = radius;
            RLog.Msg("Trigger size set to: " + radius);
        }

        public float GetTriggerRadius()
        {
            return triggerCollider.radius;
        }
    }
}
