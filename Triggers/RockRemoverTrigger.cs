using Construction;
using Sons.Gui;
using SonsSdk;
using UnityEngine;
using SUI;
using RedLoader;
using Sons.Areas;
using HarmonyLib;
using Endnight.Utilities;
using TheForest.Utils;
using System.Reflection.Emit;
using System.Reflection;
using TheForest.Player.Actions;
using Endnight.Environment;
using System.Runtime.InteropServices;
using Sons.Gameplay;
using Sons.Gameplay.GPS;
using Sons.Ai;
using Sons.Animation.PlayerControl;
using Sons.Cutscenes;
using UnityEngine.Playables;
using static RedLoader.RLog;
using TheForest.Items.Inventory;
using TheForest.Items.Special;
using Sons.Settings;
using Sons.Atmosphere;
using Endnight.Extensions;
using Sons.Player;
using TheForest.World;
using System.Collections;
using JetAnnotations;
using UnityEngine.SceneManagement;
using Endnight.Physics;
using static Sons.Wearable.Armour.NpcArmourSystem;
namespace AllowBuildInCaves.Triggers

{
    [RegisterTypeInIl2Cpp]
    internal class RockRemoverTrigger : MonoBehaviour
    {
        private BoxCollider triggerCollider;
        public Vector3 size = new Vector3(0.5f, 1f, 0.5f);
        private GameObject CaveBCollision;
        float timer = 0.0f;
        float waitTime = 5.0f;


        private void Start()
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = size;
        }

        private void OnTriggerEnter(Collider other)
        {
        }

        private void OnTriggerExit(Collider other)
        {
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > waitTime && IsInCavesStateManager.RockRemoverCaveBRunning)
            {
                FetchObjectsInCollider();
                timer = 0.0f;
            }
        }

        public void FetchObjectsInCollider()
        {
            Collider[] hitColliders = Physics.OverlapBox(
            triggerCollider.bounds.center,
            triggerCollider.bounds.extents,
            triggerCollider.transform.rotation
            );

            foreach (Collider collider in hitColliders)
            {
                Transform rockTransform = collider.transform;
                if (rockTransform.parent == null) continue;
                if (rockTransform.parent.parent == null) continue;
                string parentName = rockTransform.parent.parent.name;
                if (parentName == "Pool_Rocks")
                {
                    Destroy(rockTransform.parent.gameObject);
                    return;
                }
            }
        }

        public void SetTriggerSize(Vector3 newSize)
        {
            if (triggerCollider != null)
            {
                triggerCollider.size = newSize;
                Msg("Trigger size set to: " + newSize);
            }
        }

        public Vector3 GetTriggerSize()
        {
            return triggerCollider.size;
        }
    }

    [RegisterTypeInIl2Cpp]
    public class RockRemoverPlayerDetectionTrigger : MonoBehaviour
    {
        private SphereCollider proximityCollider; // Larger trigger for player proximity
        public float proximityRadius = 150f; // Adjust as needed
        public bool isPlayerNearby = false;

        private void Start()
        {
            proximityCollider = gameObject.AddComponent<SphereCollider>();
            proximityCollider.isTrigger = true;
            proximityCollider.radius = proximityRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                IsInCavesStateManager.RockRemoverCaveBRunning = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsInCavesStateManager.TryAddItems) return;


            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                IsInCavesStateManager.RockRemoverCaveBRunning = false;
            }
        }

        public void SetProximityRadius(float newSize)
        {
            if (proximityCollider != null)
            {
                proximityCollider.radius = newSize;
                Msg("proximity radius set to: " + newSize);
            }
        }

        public bool IsPlayerNearby()
        {
            return isPlayerNearby;
        }

        public float GetProximityRadius()
        {
            return proximityCollider.radius;
        }
    }
}
