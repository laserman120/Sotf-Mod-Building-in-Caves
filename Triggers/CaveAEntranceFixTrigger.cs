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
        private Rigidbody PlayerRigidBody;
        int terrainLayerIndex;
        int terrainLayerMask;


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
            
            PlayerRigidBody = LocalPlayer.GameObject.GetComponent<Rigidbody>();

            terrainLayerIndex = LayerMask.NameToLayer("Terrain");
            terrainLayerMask = 1 << terrainLayerIndex;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TerrainCollision == null) findTerrain();
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                IgnoreTerrainCollision(true);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (TerrainCollision == null) findTerrain();
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                IgnoreTerrainCollision(false);
            }
        }

        private void IgnoreTerrainCollision(bool ignore)
        {
            if(PlayerRigidBody == null) findTerrain();


            if (ignore)
            {
                PlayerRigidBody.excludeLayers = terrainLayerMask; // Add Terrain layer to excluded layers
            }
            else
            {
                PlayerRigidBody.excludeLayers = 0; // Remove Terrain layer from excluded layers
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
