using RedLoader;
using SonsSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            RLog.Msg("Searching Cave Collision");
            GameObject CaveCollisionHoler = new GameObject("CaveCollisionHolder");
            GameObject[] allLoadedGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (string prefix in parentNamePrefixes)
            {
                foreach (GameObject potentialParent in allLoadedGameObjects)
                {
                    if (potentialParent.name.StartsWith(prefix))
                    {
                        GameObject SpecificCaveHolder = new GameObject("Holder-" + potentialParent.name);
                        SpecificCaveHolder.SetParent(CaveCollisionHoler.transform, true); // Keep world position
                        for (int i = potentialParent.transform.childCount - 1; i >= 0; i--)
                        {
                            Transform child = potentialParent.transform.GetChild(i);
                            if (child.name.ToLower().Contains("collision"))
                            {
                                RLog.Msg(potentialParent.name + "  -> Found child: " + child.name);
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
                                    RLog.Msg(potentialParent.name + "  -> Found child: " + child.name);
                                    child.SetParent(SpecificCaveHolder.transform, true); // Keep world position
                                }
                            }
                        }
                    }
                }
            }
            RLog.Msg("Cave Collision search complete");
        }
    }
}
