using Endnight.Utilities;
using Pathfinding;
using RedLoader;
using Sons.Ai.Vail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.Components
{
    internal class VailActorRelevancyChecker
    {
    }
}

[RegisterTypeInIl2Cpp]
public class BuildInCavesActorMaskChanger : MonoBehaviour
{
    public VailActor vailActor;

    private void Awake()
    {
        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }
    }

    private void Start()
    {
        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }
    }

    private void Update()
    {
        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }

        //enforce graphMask 1 always
        if (vailActor.GetNavGraphMask() != GraphMask.FromGraphIndex(0))
        {
            vailActor.SetNavGraphMask(GraphMask.FromGraphIndex(0));
        }

        //Run Relevancy check

        Il2CppSystem.Collections.Generic.List<PlayerLocation.ViewerInfo> viewerList = PlayerLocation.GetViewerList();
        bool isRelevant = false;
        foreach (PlayerLocation.ViewerInfo viewerInfo in viewerList)
        {
            Vector3 position = viewerInfo.Position;
            //Is the actor relevant to any player? Extra margin 5f for safety.
            isRelevant = vailActor._worldSimActor.IsRelevant(position, vailActor.Position(), 5f);
            if (isRelevant)
            {
                break;
            }
        }

        if (isRelevant && vailActor._allowDeactivate)
        {
            vailActor._allowDeactivate = false;
            vailActor._drownDepth = 1000f; // Set a very high drown depth to prevent drowning in caves
            vailActor._worldSimActor.SetKeepAboveTerrain(false);
        }
        else if (!isRelevant && !vailActor._allowDeactivate)
        {
            vailActor._allowDeactivate = true;
            vailActor._drownDepth = 2f; // Reset drown depth to default
            vailActor._worldSimActor.SetKeepAboveTerrain(true);
        }
    }
}
