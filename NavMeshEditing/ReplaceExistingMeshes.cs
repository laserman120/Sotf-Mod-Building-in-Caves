using RedLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.NavMeshEditing
{
    internal class ReplaceExistingMeshes
    {
        // Helper to find 3 diverse points from a local vertex array and get their indices
        private static bool GetDiverseLocalPointsIndices(
            Vector3[] localVertices,
            out int outIdxA, out int outIdxB, out int outIdxC)
        {
            outIdxA = outIdxB = outIdxC = -1;

            if (localVertices == null || localVertices.Length < 3)
            {
                RLog.Error("Not enough local vertices to select diverse points.");
                return false;
            }

            outIdxA = 0; // First point

            // Find second point (idxB) furthest from idxA
            float maxDistSqToA = -1f;
            for (int i = 1; i < localVertices.Length; i++)
            {
                float dSq = (localVertices[i] - localVertices[outIdxA]).sqrMagnitude;
                if (dSq > maxDistSqToA)
                {
                    maxDistSqToA = dSq;
                    outIdxB = i;
                }
            }

            if (outIdxB == -1 || maxDistSqToA < 0.0001f) // Check if a distinct second point was found
            {
                RLog.Warning("Could not find a distinct second point. Using index 1 if available.");
                outIdxB = (localVertices.Length > 1) ? 1 : -1;
                if (outIdxB == outIdxA || outIdxB == -1)
                {
                    RLog.Error("Failed to select distinct second point."); return false;
                }
            }

            // Find third point (idxC) furthest from the line defined by idxA and idxB
            // (Maximizing the area of the triangle [idxA, idxB, idxC])
            float maxTriangleAreaDoubled = -1f;
            Vector3 vecAB = localVertices[outIdxB] - localVertices[outIdxA];
            for (int i = 0; i < localVertices.Length; i++)
            {
                if (i == outIdxA || i == outIdxB) continue;

                Vector3 vecAC = localVertices[i] - localVertices[outIdxA];
                float areaDoubled = Vector3.Cross(vecAB, vecAC).magnitude; // Magnitude of cross product is 2x triangle area
                if (areaDoubled > maxTriangleAreaDoubled)
                {
                    maxTriangleAreaDoubled = areaDoubled;
                    outIdxC = i;
                }
            }

            if (outIdxC == -1 || maxTriangleAreaDoubled < 0.0001f) // Check if a non-collinear third point was found
            {
                RLog.Warning("Could not find a distinct non-collinear third point. Attempting fallback.");
                // Fallback: pick the next available different index
                for (int i = 0; i < localVertices.Length; i++)
                {
                    if (i != outIdxA && i != outIdxB)
                    {
                        outIdxC = i;
                        break;
                    }
                }
                if (outIdxC == -1)
                {
                    RLog.Error("Failed to select distinct third point."); return false;
                }
            }

            RLog.Msg($"  Selected diverse local indices: A={outIdxA}, B={outIdxB}, C={outIdxC}");
            return true;
        }

        public static void AdjustRecastGraphSettings()
        {
            RLog.Msg("Attempting to change RecastGraph settings for cave meshes...");
            Il2CppSystem.Collections.IEnumerable graphs = AstarPath.active.data.FindGraphsOfType(Il2CppSystem.RuntimeType.GetType("Pathfinding.RecastGraph"));

            foreach (Il2CppSystem.Object graphObject in graphs)
            {
                if (graphObject == null) continue;
                Pathfinding.RecastGraph recastGraph = graphObject.TryCast<Pathfinding.RecastGraph>();
                if (recastGraph == null) continue;
                RLog.Msg($"Processing RecastGraph: '{recastGraph.name}'");
                recastGraph.cellSize = 0.18f;
                recastGraph.characterRadius = 0.5f;
                recastGraph.maxEdgeLength = 10f;
                recastGraph.maxSlope = 48f;
                recastGraph.minRegionSize = 10f;
                recastGraph.colliderRasterizeDetail = 1f;
            }
            RLog.Msg("RecastGraph settings adjusted for cave meshes. Creating meshes...");

        }

        public static void GenerateCaveMeshes()
        {
            if (AstarPath.active == null || AstarPath.active.data == null)
            {
                RLog.Error("AstarPath is not active or data is null.");
                return;
            }

            Il2CppSystem.Collections.IEnumerable graphs = AstarPath.active.data.FindGraphsOfType(Il2CppSystem.RuntimeType.GetType("Pathfinding.NavMeshGraph"));

            int navmeshesProcessed = 0;

            foreach (Il2CppSystem.Object graphObject in graphs)
            {
                if (graphObject == null) continue;
                Pathfinding.NavMeshGraph originalGraph = graphObject.TryCast<Pathfinding.NavMeshGraph>();
                if (originalGraph == null) continue;
                UnityEngine.Mesh sourceMeshForInfo = originalGraph.sourceMesh;
                if (sourceMeshForInfo == null) continue;

                RLog.Msg($"Processing graph: '{originalGraph.name}' (Source: '{sourceMeshForInfo.name}') using GetVertex() and diverse point alignment.");

                Vector3[] sourceLocalVerticesRaw = sourceMeshForInfo.vertices;
                int[] sourceLocalTriangles = sourceMeshForInfo.triangles;

                int idxA, idxB, idxC;
                if (originalGraph.name == "CaveANavMesh")
                {
                    idxA = 2423;
                    idxB = 1184;
                    idxC = 1310;
                }
                else if (!GetDiverseLocalPointsIndices(sourceLocalVerticesRaw, out idxA, out idxB, out idxC))
                {
                    RLog.Warning($"  Could not select 3 diverse points from source mesh '{sourceMeshForInfo.name}'. Skipping alignment.");
                    continue;
                }

                float graphDeclaredScale = originalGraph.scale;
                RLog.Msg($"  Graph Scale: {graphDeclaredScale:F1}. Using local indices: A={idxA}, B={idxB}, C={idxC}");

                // 1. Define Local Reference Points (from sourceMesh, scaled, using diverse indices)
                Vector3 L_A_local = sourceLocalVerticesRaw[idxA];
                Vector3 L_B_local = sourceLocalVerticesRaw[idxB];
                Vector3 L_C_local = sourceLocalVerticesRaw[idxC];

                Vector3 SL_A = L_A_local * graphDeclaredScale;
                Vector3 SL_B = L_B_local * graphDeclaredScale;
                Vector3 SL_C = L_C_local * graphDeclaredScale;
                RLog.Msg($"    Scaled Local Ref Points: SL_A={SL_A.ToString("F3")}, SL_B={SL_B.ToString("F3")}, SL_C={SL_C.ToString("F3")}");

                // 2. Define Target World Anchor Points (using originalGraph.GetVertex() with corresponding diverse indices)
                Vector3 W_A_target, W_B_target, W_C_target;
                try
                {
                    Pathfinding.Int3 i3_A = originalGraph.GetVertex(idxA);
                    Pathfinding.Int3 i3_B = originalGraph.GetVertex(idxB);
                    Pathfinding.Int3 i3_C = originalGraph.GetVertex(idxC);
                    RLog.Msg($"    Raw Int3 from GetVertex({idxA}): {i3_A.ToString()}");
                    RLog.Msg($"    Raw Int3 from GetVertex({idxB}): {i3_B.ToString()}");
                    RLog.Msg($"    Raw Int3 from GetVertex({idxC}): {i3_C.ToString()}");

                    if (originalGraph.transform == null)
                    {
                        RLog.Error($"originalGraph.transform is null for {originalGraph.name}.");
                        continue;
                    }
                    W_A_target = originalGraph.transform.Transform((Vector3)i3_A);
                    W_B_target = originalGraph.transform.Transform((Vector3)i3_B);
                    W_C_target = originalGraph.transform.Transform((Vector3)i3_C);
                    RLog.Msg($"    Target World Points (from GetVertex): W_A={W_A_target.ToString("F3")}, W_B={W_B_target.ToString("F3")}, W_C={W_C_target.ToString("F3")}");
                }
                catch (System.Exception ex)
                {
                    RLog.Error($"  Error using originalGraph.GetVertex({idxA}/{idxB}/{idxC}) or transforming: {ex.Message}. Skipping alignment.");
                    RLog.Error($"  Exception Details: {ex.ToString()}");
                    continue;
                }

                // 3. Calculate alignment transform
                Quaternion R_solved = Quaternion.identity;
                Vector3 T_solved = W_A_target - SL_A;

                Vector3 localRef_Vec_AB = SL_B - SL_A;
                Vector3 localRef_Vec_AC = SL_C - SL_A;
                Vector3 worldTarget_Vec_AB = W_B_target - W_A_target;
                Vector3 worldTarget_Vec_AC = W_C_target - W_A_target;

                // Check for collinearity / zero magnitude vectors
                if (localRef_Vec_AB.sqrMagnitude < 0.0001f || localRef_Vec_AC.sqrMagnitude < 0.0001f ||
                    worldTarget_Vec_AB.sqrMagnitude < 0.0001f || worldTarget_Vec_AC.sqrMagnitude < 0.0001f ||
                    Vector3.Cross(localRef_Vec_AB, localRef_Vec_AC).sqrMagnitude < 0.0001f ||
                    Vector3.Cross(worldTarget_Vec_AB, worldTarget_Vec_AC).sqrMagnitude < 0.0001f)
                {
                    RLog.Warning("  Reference points for alignment are collinear or too close. Using default transform (unrotated, placed by first point).");
                    // R_solved remains identity, T_solved is already W_A_target - SL_A
                }
                else
                {
                    // Normalize for safety, though LookRotation does it.
                    Vector3 localFrame_X = localRef_Vec_AB.normalized;
                    Vector3 localFrame_Y_candidate = localRef_Vec_AC.normalized;
                    Vector3 localFrame_Z = Vector3.Cross(localFrame_X, localFrame_Y_candidate).normalized;
                    Vector3 localFrame_Y = Vector3.Cross(localFrame_Z, localFrame_X).normalized;
                    Quaternion localFrameRotation = Quaternion.LookRotation(localFrame_Z, localFrame_Y);

                    Vector3 worldFrame_X = worldTarget_Vec_AB.normalized;
                    Vector3 worldFrame_Y_candidate = worldTarget_Vec_AC.normalized;
                    Vector3 worldFrame_Z = Vector3.Cross(worldFrame_X, worldFrame_Y_candidate).normalized;
                    Vector3 worldFrame_Y = Vector3.Cross(worldFrame_Z, worldFrame_X).normalized;
                    Quaternion worldTargetFrameRotation = Quaternion.LookRotation(worldFrame_Z, worldFrame_Y);

                    R_solved = worldTargetFrameRotation * Quaternion.Inverse(localFrameRotation);
                    T_solved = W_A_target - (R_solved * SL_A);
                    RLog.Msg($"  Alignment Calculated: R_solved(Euler)={R_solved.eulerAngles.ToString("F3")}, T_solved={T_solved.ToString("F3")}");
                }

                // 4. Create the mesh for NavmeshAdd (vertices are scaled source mesh)
                Vector3[] meshGoVertices = new Vector3[sourceLocalVerticesRaw.Length];
                for (int i = 0; i < sourceLocalVerticesRaw.Length; i++)
                {
                    meshGoVertices[i] = sourceLocalVerticesRaw[i] * graphDeclaredScale;
                }
                UnityEngine.Mesh meshForGO = new UnityEngine.Mesh();
                meshForGO.name = sourceMeshForInfo.name + "_Cloned";
                meshForGO.SetVertices(meshGoVertices);
                meshForGO.SetTriangles(sourceLocalTriangles, 0);
                meshForGO.RecalculateNormals();
                meshForGO.RecalculateBounds();
                RLog.Msg($"  Mesh for GO '{meshForGO.name}' created. Its local bounds center: {meshForGO.bounds.center.ToString("F5")}");

                //NEW TEST CODE ----------------------------------------------------------------------------------------------------------------------------------------------------------------

                // --- 5. Setup GameObject with the SOLVED Transform to be SCANNED by RecastGraph ---
                GameObject meshHolderGO = new GameObject($"ScannableCaveMesh_{originalGraph.name}"); // Renamed for clarity
                meshHolderGO.transform.position = Vector3.zero;
                meshHolderGO.transform.rotation = R_solved;
                meshHolderGO.transform.localScale = Vector3.one; // Ensure localScale is one if graphDeclaredScale already handled scaling of vertices

                // Add MeshFilter and assign the created mesh
                MeshFilter meshFilter = meshHolderGO.AddComponent<MeshFilter>();
                meshFilter.mesh = meshForGO;

                // Add MeshCollider and assign the created mesh
                // This is what the RecastGraph will typically scan if "Rasterize Colliders" is enabled.
                // If "Rasterize Meshes" is enabled on the RecastGraph, it might pick up the MeshFilter directly,
                // but having a MeshCollider is generally safer and more standard for RecastGraph input.
                MeshCollider meshCollider = meshHolderGO.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshForGO;

                // Set the layer of the GameObject so the RecastGraph scans it.
                // Based on RecastGraph.mask = 71303168, Layer 22 or 26 are valid. Let's use 22.
                // IMPORTANT: Make sure Layer 22 (or 26) exists and is appropriately named/used in your project context.
                const int SCAN_LAYER = 26; // Or 22
                meshHolderGO.layer = SCAN_LAYER;

                RLog.Msg($"  Set up Scannable GameObject '{meshHolderGO.name}' on layer {SCAN_LAYER}.");
                RLog.Msg($"    GO P: {meshHolderGO.transform.position.ToString("F3")}, GO R(Euler): {meshHolderGO.transform.rotation.eulerAngles.ToString("F3")}");

                // Verification of chosen alignment points (this part remains useful)
                Vector3 actual_W_A = meshHolderGO.transform.TransformPoint(SL_A);
                RLog.Msg($"    Verify W_A (idx {idxA}): Target={W_A_target.ToString("F3")}, Actual={actual_W_A.ToString("F3")}, DiffMag={(W_A_target - actual_W_A).magnitude:F5}");
                Vector3 actual_W_B = meshHolderGO.transform.TransformPoint(SL_B);
                RLog.Msg($"    Verify W_B (idx {idxB}): Target={W_B_target.ToString("F3")}, Actual={actual_W_B.ToString("F3")}, DiffMag={(W_B_target - actual_W_B).magnitude:F5}");
                Vector3 actual_W_C = meshHolderGO.transform.TransformPoint(SL_C);
                RLog.Msg($"    Verify W_C (idx {idxC}): Target={W_C_target.ToString("F3")}, Actual={actual_W_C.ToString("F3")}, DiffMag={(W_C_target - actual_W_C).magnitude:F5}");

                navmeshesProcessed++;
                /*
                // --- 5. Setup GameObject with the SOLVED Transform ---
                GameObject meshHolderGO = new GameObject($"ClonedNavMesh_{originalGraph.name}");
                meshHolderGO.transform.position = T_solved;
                meshHolderGO.transform.rotation = R_solved;
                meshHolderGO.transform.localScale = Vector3.one;

                Pathfinding.NavmeshAdd navmeshAdd = meshHolderGO.AddComponent<Pathfinding.NavmeshAdd>();
                navmeshAdd.mesh = meshForGO;
                navmeshAdd.type = Pathfinding.NavmeshAdd.MeshType.CustomMesh;
                navmeshAdd.useRotationAndScale = true;
                navmeshAdd.graphMask = 1;

                Vector3 p_go_final = meshHolderGO.transform.position;
                Quaternion r_go_final = meshHolderGO.transform.rotation;
                navmeshAdd.center = Quaternion.Inverse(r_go_final) * (-p_go_final);
                RLog.Msg($"  Set navmeshAdd.center (field) to: {navmeshAdd.center.ToString("F5")}");

                navmeshAdd.enabled = false;
                navmeshAdd.RebuildMesh();
                navmeshAdd.enabled = true;

                RLog.Msg($"  Set up NavmeshAdd on '{meshHolderGO.name}'.");
                RLog.Msg($"    GO P: {meshHolderGO.transform.position.ToString("F3")}, GO R(Euler): {meshHolderGO.transform.rotation.eulerAngles.ToString("F3")}");
                RLog.Msg($"    navmeshAdd.Center (bounds.center): {navmeshAdd.center.ToString("F3")} (Target: {T_solved.ToString("F3")})");

                // Verification of chosen alignment points
                Vector3 actual_W_A = meshHolderGO.transform.TransformPoint(SL_A);
                RLog.Msg($"    Verify W_A (idx {idxA}): Target={W_A_target.ToString("F3")}, Actual={actual_W_A.ToString("F3")}, DiffMag={(W_A_target - actual_W_A).magnitude:F5}");
                Vector3 actual_W_B = meshHolderGO.transform.TransformPoint(SL_B);
                RLog.Msg($"    Verify W_B (idx {idxB}): Target={W_B_target.ToString("F3")}, Actual={actual_W_B.ToString("F3")}, DiffMag={(W_B_target - actual_W_B).magnitude:F5}");
                Vector3 actual_W_C = meshHolderGO.transform.TransformPoint(SL_C);
                RLog.Msg($"    Verify W_C (idx {idxC}): Target={W_C_target.ToString("F3")}, Actual={actual_W_C.ToString("F3")}, DiffMag={(W_C_target - actual_W_C).magnitude:F5}");

                navmeshesProcessed++;
                */
            }

            if (navmeshesProcessed > 0) { 
                RLog.Msg($"{navmeshesProcessed} NavMeshGraph(s) processed. Flushing Graph Updates..."); 
                AstarPath.active.FlushGraphUpdates(); 
                RLog.Msg("Rescanning the AstarPath...");
                AstarPath.active.Scan();
            }
            else { RLog.Msg("No NavMeshGraphs were processed."); }
        }
    }
}
