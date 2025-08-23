using AllowBuildInCaves.Debug;
using Endnight.Types;
using Endnight.Utilities;
using Pathfinding;
using RedLoader;
using SonsSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.SceneManagement;

namespace AllowBuildInCaves.NavMeshEditing
{
    internal class ForceActivateCaveCollision
    {
        private static readonly List<string> parentNamePrefixes = new List<string>
    {
        "CaveAInternal-Any",
        "CaveBInternal-Any",
        "CaveCInternal-Any",
        "CaveD_Internal-Any",
        "CaveF_Internal-Any",
        "CaveG_Internal-Any",
        "CaveHInternal-Any",
        "CaveEInternal-Any",
        "BE_Internal-Any",
        "BF_Internal-Any",
        "BunkerFInternal-Any"
    };

        public static void ForceActivateCaveCollisions()
        {
            DebugManager.DebugLog("Searching Cave Collision");
            GameObject CaveCollisionHolder = new GameObject("CaveCollisionHolder");

            GameObject[] allLoadedGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (string prefix in parentNamePrefixes)
            {
                foreach (GameObject potentialParent in allLoadedGameObjects)
                {
                    if (potentialParent.name.StartsWith(prefix))
                    {
                        GameObject SpecificCaveHolder = new GameObject("Holder-" + potentialParent.name);
                        SpecificCaveHolder.SetParent(CaveCollisionHolder.transform, true); // Keep world position
                        for (int i = potentialParent.transform.childCount - 1; i >= 0; i--)
                        {
                            Transform child = potentialParent.transform.GetChild(i);
                            if (child.name.ToLower().Contains("collision"))
                            {
                                DebugManager.DebugLog(potentialParent.name + "  -> Found child: " + child.name);

                                //special cases
                                if (potentialParent.name.StartsWith("CaveBInternal-Any"))
                                {
                                    ObjectDestroyer destroyer = child.gameObject.GetOrAddComponent<ObjectDestroyer>();
                                    destroyer.ObjectNamesForDestruction = new List<string>
                                    {
                                        "CaveCollisionCliffsD"
                                    };

                                }
                                child.SetParent(SpecificCaveHolder.transform, true); // Keep world position
                            }
                        }

                        if (potentialParent.name == "BE_Internal-Any")
                        {
                            for (int i = potentialParent.transform.childCount - 1; i >= 0; i--)
                            {
                                Transform child = potentialParent.transform.GetChild(i);
                                if (child.name.ToLower().Contains("collider"))
                                {
                                    DebugManager.DebugLog(potentialParent.name + "  -> Found child: " + child.name);
                                    child.SetParent(SpecificCaveHolder.transform, true); // Keep world position
                                }
                            }
                        }
                    }
                }
            }

            DebugManager.DebugLog("Cave Collision search complete");
        }
    }
}

[RegisterTypeInIl2Cpp]
public class ObjectDestroyer : MonoBehaviour
{
    public List<string> ObjectNamesForDestruction;
    private bool successfullyDestroyed = false;
    private void Start()
    {
        RunCheck();
    }
    private void Awake()
    {
        RunCheck();
    }
    private void Update()
    {
        RunCheck();
    }

    private void RunCheck()
    {
        if (successfullyDestroyed)
        {
            return;
        }

        foreach (string objectName in ObjectNamesForDestruction)
        {
            Transform foundChild = transform.FindDeepChild(objectName);

            if(foundChild != null)
            {
                DebugManager.DebugLog(gameObject.name + "Found object for destruction: " + foundChild.name);
                KeepObjectDeactivated objectDeactivated = foundChild.gameObject.GetOrAddComponent<KeepObjectDeactivated>();

                if(objectDeactivated != null)
                {
                    successfullyDestroyed = true;
                }
            }
        }
    }
}