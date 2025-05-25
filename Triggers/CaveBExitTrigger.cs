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
using UnityEngine.SceneManagement;
using Endnight.Physics;

namespace AllowBuildInCaves.Triggers
{
    [RegisterTypeInIl2Cpp]
    public class CaveBExitTrigger : MonoBehaviour
    {
        private BoxCollider triggerCollider;
        public Vector3 size = new Vector3(3f, 3f, 3f);
        private GameObject CaveBCollision;

        private void Start()
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = size;
        }

        private void OnTriggerEnter(Collider other)
        {
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                Warning("Player found");
                CaveBCollision = GameObject.Find("CaveBCollision");
                if (CaveBCollision == null) return;

                GameObject CaveBCollisionAddressable = CaveBCollision.transform.Find("CaveBCollisionAddressable(Clone)").gameObject;
                if (CaveBCollisionAddressable == null) return;

                GameObject CaveCollisionCliffsD = CaveBCollisionAddressable.transform.Find("CaveCollisionCliffsD").gameObject;
                if (CaveCollisionCliffsD == null) return;

                CaveCollisionCliffsD.SetActive(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                CaveBCollision = GameObject.Find("CaveBCollision");
                if (CaveBCollision == null) return;

                GameObject CaveBCollisionAddressable = CaveBCollision.transform.Find("CaveBCollisionAddressable(Clone)").gameObject;
                if (CaveBCollisionAddressable == null) return;

                GameObject CaveCollisionCliffsD = CaveBCollisionAddressable.transform.Find("CaveCollisionCliffsD").gameObject;
                if (CaveCollisionCliffsD == null) return;

                CaveCollisionCliffsD.SetActive(true);
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
}
