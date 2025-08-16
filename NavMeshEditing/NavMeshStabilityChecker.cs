using Pathfinding;
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
    [RegisterTypeInIl2Cpp]
    public class NavMeshStabilityChecker : MonoBehaviour
    {
        private List<NavMeshGraph> navMeshGraphs = new List<NavMeshGraph>();
        public float stabilityCheckInterval = 5f; // Time in seconds between checks
        private float nextCheckTime;
        bool hasShownWarning = false;

        private void Start()
        {
            nextCheckTime = Time.time + stabilityCheckInterval;

            // Implement your logic to check NavMesh stability here
            if (AstarPath.active == null || AstarPath.active.data == null)
            {
                RLog.Error("AstarPath is not active or data is null.");
                return;
            }

            Il2CppSystem.Collections.IEnumerable graphs = AstarPath.active.data.FindGraphsOfType(Il2CppSystem.RuntimeType.GetType("Pathfinding.NavMeshGraph"));

            int navmeshesProcessed = 0;

            RLog.Msg($"Initializing NavMeshStabilityChecker...");

            foreach (Il2CppSystem.Object graphObject in graphs)
            {
                if (graphObject == null) continue;
                Pathfinding.NavMeshGraph NavMeshGraph = graphObject.TryCast<Pathfinding.NavMeshGraph>();
                if (NavMeshGraph == null) continue;

                navMeshGraphs.Add(NavMeshGraph);

                RLog.Msg($"Found NavMeshGraph to check: {NavMeshGraph.name}");
            }
        }

        private void Update()
        {
            if (Time.time >= nextCheckTime)
            {
                CheckNavMeshStability();
                nextCheckTime = Time.time + stabilityCheckInterval;
            }
        }

        private void CheckNavMeshStability()
        {
            if(navMeshGraphs == null || navMeshGraphs.Count == 0)
            {
                return;
            }

            for(int i = 0; i < navMeshGraphs.Count; i++)
            {
                NavMeshGraph graph = navMeshGraphs[i];
                if (graph == null)
                {
                    RLog.Error($"NavMeshGraph at index {i} is null.");
                    continue;
                }

                if(graph.CountNodes() == 0)
                {
                    if (hasShownWarning == false)
                    {
                        SonsTools.ShowMessage($"NavMeshGraph {graph.name} has collapsed and is in an unusable state, please report this bug together with your log file", 10f);
                        hasShownWarning = true;
                    }
                    RLog.Msg($"NavMeshGraph {graph.name} has collapsed and is in an unusable state");
                }
            }
        }
    }
}
