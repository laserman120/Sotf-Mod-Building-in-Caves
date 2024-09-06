using UnityEngine;
using RedLoader;
using static RedLoader.RLog;
namespace AllowBuildInCaves.Triggers

{
    [RegisterTypeInIl2Cpp]
    internal class RockRemoverTriggerBunkerFoodExit : MonoBehaviour
    {
        private BoxCollider triggerCollider;
        private GameObject BunkerObjects;
        public Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
        float timer = 0.0f;
        float waitTime = 5.0f;


        private void Start()
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = size;
            FindBunker();
        }

        private void FindBunker()
        {
            BunkerObjects = GameObject.Find("BunkerFoodEntranceObjects");
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
            if (timer > waitTime && IsInCavesStateManager.RockRemoverBunkerFoodRunning)
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
                if (parentName == "BunkerFoodEntranceObjects")
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
    public class RockRemoverTriggerBunkerFoodExitPlayerDetectionTrigger : MonoBehaviour
    {
        private SphereCollider proximityCollider; // Larger trigger for player proximity
        public float proximityRadius = 150f; // Adjust as needed

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
                IsInCavesStateManager.RockRemoverBunkerFoodRunning = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsInCavesStateManager.TryAddItems) return;


            Transform playerTransform = other.transform;

            if (playerTransform.name.Contains("LocalPlayer"))
            {
                IsInCavesStateManager.RockRemoverBunkerFoodRunning = false;
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
            return IsInCavesStateManager.RockRemoverBunkerFoodRunning;
        }

        public float GetProximityRadius()
        {
            return proximityCollider.radius;
        }
    }
}
