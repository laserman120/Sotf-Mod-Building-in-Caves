using RedLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.Triggers
{
    [RegisterTypeInIl2Cpp]
    public class SphereColliderPusher : MonoBehaviour
    {
        public float pushRadius = 2.0f;

        public float pushStrength = 1.0f;

        public LayerMask pushableLayers; // Set this in the Inspector!

        private SphereCollider triggerSphere;

        void Start()
        {
            // Ensure there's only one pusher collider on this GameObject
            triggerSphere = GetComponent<SphereCollider>();
            if (triggerSphere == null)
            {
                triggerSphere = gameObject.AddComponent<SphereCollider>();
            }

            triggerSphere.isTrigger = true;
            triggerSphere.radius = pushRadius;

            if (pushableLayers.value == 0) 
            {
                Debug.LogWarning($"SphereColliderPusher on {gameObject.name}: 'Pushable Layers' is not set. Please configure it in the Inspector to specify which layers to affect.", this);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (other.transform == transform || other.transform.IsChildOf(transform))
            {
                return;
            }

            if (other.isTrigger)
            {
                return;
            }

            RLog.Msg("trying to push rocks.");
            TryPushRock(other);
        }

        private bool TryPushRock(Collider otherCollider)
        {
            Transform colliderTransform = otherCollider.transform;
            Transform objectToMoveTransform = colliderTransform.parent;

            if (objectToMoveTransform == null)
            {
                return false;

            }

            if((pushableLayers.value & (1 << otherCollider.gameObject.layer)) != 0)
            {
                // This is a targeted rock object.

                // Determine if a push is needed based on the closest point of its bounds.
                Vector3 closestPointOnBoundsToPusherCenter = otherCollider.bounds.ClosestPoint(transform.position);
                float distanceToClosestPointOnBounds = Vector3.Distance(closestPointOnBoundsToPusherCenter, transform.position);

                if (distanceToClosestPointOnBounds < pushRadius)
                {
                    // The rock's bounds are encroaching into our push radius. We need to push it.

                    // Calculate Push Direction - REVISED LOGIC
                    Vector3 pushDirectionNormalized;
                    // Primary direction: from pusher center to the closest point on the collider's bounds
                    Vector3 directionFromPusherToClosestPoint = closestPointOnBoundsToPusherCenter - transform.position;

                    if (directionFromPusherToClosestPoint.sqrMagnitude > 0.0001f)
                    {
                        pushDirectionNormalized = directionFromPusherToClosestPoint.normalized;
                    }
                    else
                    {
                        // Fallback 1: Closest point is at our center. Try direction to object's bounds.center.
                        Vector3 directionFromPusherToColliderCenter = otherCollider.bounds.center - transform.position;
                        if (directionFromPusherToColliderCenter.sqrMagnitude > 0.0001f)
                        {
                            pushDirectionNormalized = directionFromPusherToColliderCenter.normalized;
                            // RLog.Msg($"Push direction fallback to bounds center for {objectToMove.name}");
                        }
                        else
                        {
                            // Fallback 2: Both closest point and bounds.center are at our center. Use object's up or a default.
                            pushDirectionNormalized = objectToMoveTransform.up;
                            if (pushDirectionNormalized.sqrMagnitude < 0.0001f)
                            {
                                pushDirectionNormalized = Vector3.right; // Absolute fallback if object's up is also zero
                            }
                            // RLog.Msg($"Push direction fallback to object up/right for {objectToMove.name}");
                        }
                    }

                    // Calculate Target Position and Displacement
                    float buffer = 0.05f; // Small buffer to ensure complete clearance

                    Vector3 targetPositionForClosestPoint = transform.position + pushDirectionNormalized * (pushRadius + buffer);

                    Vector3 displacement = targetPositionForClosestPoint - closestPointOnBoundsToPusherCenter;

                    // Apply this displacement to the parent object.
                    objectToMoveTransform.position += displacement;

                    // Using your RLog for mod logging
                    // RLog.Msg($"Pushed {objectToMove.name}. ClosestPtDist: {distanceToClosestPointOnBounds:F2}, PushRadius: {pushRadius:F2}. Displacement: {displacement.magnitude:F2}");
                    return true; // Successfully attempted to push
                }

            }
            return false;
        }

        public List<Collider> GetCollidersCurrentlyInside(bool shouldLog = true)
        {
            List<Collider> collidersInside = new List<Collider>();
            if (triggerSphere == null)
            {
                Debug.LogWarning("SphereColliderPusher: Trigger sphere is not initialized. Cannot get colliders inside.", this);
                return collidersInside;
            }

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, triggerSphere.radius);

            foreach (Collider hit in hitColliders)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }


                Transform objectToMoveTransform = hit.transform.parent;
                if (objectToMoveTransform == null)
                {
                    continue;
                }
                GameObject objectToMove = objectToMoveTransform.gameObject;
                if(objectToMove == null)
                {
                    continue;
                }
                bool hasMeshRenderer = objectToMove.GetComponent<MeshRenderer>() != null;
                bool hasPoolLink = objectToMove.GetComponent<PathologicalGames.PoolLink> != null;
                bool validMask = ((pushableLayers.value & (1 << hit.gameObject.layer)) != 0);

                if (shouldLog)
                {
                    RLog.Msg("Found collider inside SphereColliderPusher: " + hit.name + "  -  Mask: " + hit.transform.gameObject.layer + " LayerName: " + LayerMask.LayerToName(hit.transform.gameObject.layer) + "  -  MeshRenderer: " + hasMeshRenderer + "  -  PoolLink: " + hasPoolLink + "  -  ValidLayer: " + validMask);
                }
            
                collidersInside.Add(hit);
            }
            return collidersInside;
        }

        public void SetPushRadius(float newRadius)
        {
            pushRadius = Mathf.Max(0.01f, newRadius);
            if (triggerSphere != null)
            {
                triggerSphere.radius = pushRadius;
            }
        }

        public float GetPushRadius()
        {
            return pushRadius;
        }
    }
}