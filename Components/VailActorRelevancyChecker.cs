using Endnight.Environment;
using Endnight.Utilities;
using Pathfinding;
using RedLoader;
using Sons.Ai.Vail;
using Sons.Areas;
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

        SpecialActorAdjustments();
    }

    private void Start()
    {
        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }

        SpecialActorAdjustments();
    }

    private void SpecialActorAdjustments()
    {
        if(vailActor.TypeId == VailActorTypeId.Robby)
        {
            vailActor._defaultMoveSettings._keepAboveTerrain = false;
            DynamicBoneCollider boneCollider = GetComponent<DynamicBoneCollider>();
            boneCollider.m_Height = 1f;
        }
    }

    private void Update()
    {
        // Custom reimplementation to keepAboveTerrain for Robby and Virginia as a failsafe
        if(vailActor.TypeId == VailActorTypeId.Robby || vailActor.TypeId == VailActorTypeId.Virginia)
        {
            if(vailActor.transform.position.y < -250f)
            {
                float terrainHeight = TerrainUtilities.GetTerrainHeight(vailActor.transform.position);
                vailActor.transform.position = new Vector3(vailActor.transform.position.x, terrainHeight + 1f, vailActor.transform.position.z);
            }
        }


        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }

        //enforce graphMask 1 always
        if (vailActor.GetNavGraphMask() != GraphMask.everything)
        {
            vailActor.SetNavGraphMask(GraphMask.everything);
        }

        //Run Relevancy check

        Il2CppSystem.Collections.Generic.List<PlayerLocation.ViewerInfo> viewerList = PlayerLocation.GetViewerList();
        bool isRelevant = false;
        bool isRelevantMask = false;

        foreach (PlayerLocation.ViewerInfo viewerInfo in viewerList)
        {
            Vector3 position = viewerInfo.Position;
            //Is the actor relevant to any player? Extra margin 5f for safety.
            isRelevant = vailActor._worldSimActor.IsRelevant(position, vailActor.Position(), 5f);
            if (isRelevant)
            {
                break;
            }

            AreaMask viewerAreaMask = VailWorldSimulation.GetViewerAreaMask(viewerInfo.AreaMask);
            isRelevantMask = viewerAreaMask.Matches(vailActor._worldSimActor._areaMask);
        }

        if (isRelevantMask)
        {
            //Player is INSIDE the same cave as Actor
            vailActor._drownDepth = 2f;
            vailActor._worldSimActor.SetKeepAboveTerrain(false);
        } else
        {
            //Player is NOT inside the same cave as Actor
            if (isRelevant && vailActor._allowDeactivate)
            {
                //Player is outside the cave, but actor is still close enough to stay active
                vailActor._allowDeactivate = false; //Prevent disabling while close enough
                //Only change drown depth if the actor is spawned inside the cave
                if(vailActor._worldSimActor._areaMask != AreaMask.None)
                {
                    vailActor._drownDepth = 1000f; // Set a very high drown depth to prevent drowning in caves
                    vailActor._worldSimActor.SetKeepAboveTerrain(false);
                }
            }
            else if (!isRelevant && !vailActor._allowDeactivate)
            {
                //Player is outside the cave and far enough away to be no longer relevant
                vailActor._allowDeactivate = true;
                //Only change drown depth if the actor is spawned inside the cave
                if (vailActor._worldSimActor._areaMask != AreaMask.None)
                {
                    vailActor._drownDepth = 2f; // Reset drown depth to default
                    vailActor._worldSimActor.SetKeepAboveTerrain(true);
                }
            }
        }
    }
}


[RegisterTypeInIl2Cpp]
public class SpecialActorCaveFixes : MonoBehaviour
{
    public VailActor vailActor;

    private void Awake()
    {
        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }

        SpecialyActorAdjustments();
    }

    private void Start()
    {
        if (vailActor == null)
        {
            vailActor = GetComponent<VailActor>();
        }

        SpecialyActorAdjustments();
    }

    private void SpecialyActorAdjustments()
    {
        vailActor._defaultMoveSettings._keepAboveTerrain = false;
        DynamicBoneCollider boneCollider = GetComponent<DynamicBoneCollider>();
        boneCollider.m_Height = 1f;
    }

    private void Update()
    {
        if (vailActor.GetNavGraphMask() != GraphMask.everything)
        {
            vailActor.SetNavGraphMask(GraphMask.everything);
        }
    }
}
