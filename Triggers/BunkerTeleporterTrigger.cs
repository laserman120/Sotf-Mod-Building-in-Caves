using UnityEngine;
using RedLoader;
using TheForest.Utils;
using static RedLoader.RLog;
using UnityEngine.SceneManagement;

namespace AllowBuildInCaves.Triggers
{
    [RegisterTypeInIl2Cpp]
    public class BunkerTeleportTrigger : MonoBehaviour
    {
        private BoxCollider triggerCollider;
        public Vector3 size = new Vector3(0.3f, 1.5f, 0.3f);
        private GameObject bunkerAny;
        private GameObject bunkerExternal;
        private GameObject climbInGroup;

        private float moveDown = 6f;

        private void Start()
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            // No need to set isTrigger to true, as we're not using trigger events
            triggerCollider.size = size;

            // Add SphereCollider for proximity detection


            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            Scene currentScene = gameObject.scene;

            string bunkerAnyName = currentScene.name.Replace("_", "") + "-Any";

            bunkerAny = allObjects.FirstOrDefault(go => go.name == bunkerAnyName);

            string bunkerExternalName = currentScene.name.Replace("_", "") + "External";
            bunkerExternal = GameObject.Find(bunkerExternalName);

            climbInGroup = allObjects.FirstOrDefault(go =>
                go.name == "ClimbInGroup" &&
                go.transform.parent != null &&
                go.transform.parent.parent != null &&
                go.transform.parent.parent.parent != null &&
                go.transform.parent.parent.parent.name == bunkerExternalName);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Config.EasyBunkers.Value) return;
            if (!climbInGroup.active) return;

            Transform pickupElement = other.transform;
            while (pickupElement != null && !pickupElement.name.Contains("LogPickup") && !pickupElement.name.Contains("StonePickup"))
            {
                pickupElement = pickupElement.parent;
            }

            if (pickupElement != null)
            {
                AllowBuildInCaves.PlaySound(pickupElement.transform.position);
                pickupElement.position = new Vector3(pickupElement.position.x, pickupElement.position.y - moveDown, pickupElement.position.z);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!Config.EasyBunkers.Value) return;
            if (!climbInGroup.active) return;

            Transform pickupElement = other.transform;
            while (pickupElement != null && !pickupElement.name.Contains("LogPickup") && !pickupElement.name.Contains("StonePickup"))
            {
                pickupElement = pickupElement.parent;
            }

            if (pickupElement != null)
            {
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
    public class PlayerDetectionTrigger : MonoBehaviour
    {
        private SphereCollider proximityCollider; // Larger trigger for player proximity
        public float proximityRadius = 400f; // Adjust as needed
        public bool isPlayerNearby = false;
        private float timer = 0f;
        private float checkInterval = 3f; // Check every second
        GameObject bunkerAny;
        GameObject bunkerExternal;
        GameObject climbInGroup;

        private void Start()
        {
            proximityCollider = gameObject.AddComponent<SphereCollider>();
            proximityCollider.isTrigger = true;
            proximityCollider.radius = proximityRadius;

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            Scene currentScene = gameObject.scene;

            string bunkerAnyName = currentScene.name.Replace("_", "") + "-Any";

            bunkerAny = allObjects.FirstOrDefault(go => go.name == bunkerAnyName);

            string bunkerExternalName = currentScene.name.Replace("_", "") + "External";
            bunkerExternal = GameObject.Find(bunkerExternalName);

            climbInGroup = allObjects.FirstOrDefault(go =>
                go.name == "ClimbInGroup" &&
                go.transform.parent != null &&
                go.transform.parent.parent != null &&
                go.transform.parent.parent.parent != null &&
                go.transform.parent.parent.parent.name == bunkerExternalName);
        }

        private void Update()
        {
            if (!IsInCavesStateManager.EnableEasyBunkers) return;
            if (!climbInGroup.active) return;
            timer += Time.deltaTime;
            if (timer >= checkInterval)
            {
                timer = 0f;
                if (isPlayerNearby)
                {
                    if (!bunkerAny.active) bunkerAny.SetActive(true);
                }
                else
                {
                    if (bunkerAny.active) bunkerAny.SetActive(false);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Config.EasyBunkers.Value) return;
            if (!climbInGroup.active) return;

            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                isPlayerNearby = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!Config.EasyBunkers.Value) return;
            if (!climbInGroup.active) return;
            if (IsInCavesStateManager.TryAddItems) return;


            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                isPlayerNearby = false;
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
