using Il2CppInterop.Runtime;
using Il2CppSystem;
using Pathfinding;
using RedLoader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Pathfinding.Graphs.Navmesh.ColliderMeshBuilder2D;

namespace AllowBuildInCaves.NavMeshEditing
{
    internal class ReplaceExistingMeshes
    {

        public static List<NavMeshGraph> allNavMeshGraphs = new();

        public static string FetchAstarVersion()
        {
            Il2CppSystem.Version astarVersion = AstarPath.Version;
            return astarVersion.ToString();
        }



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
                recastGraph.cellSize = 0.10f;
            }
            RLog.Msg("RecastGraph settings adjusted for cave meshes. Creating meshes...");

        }

        public static void EnableCuttingOnCaveMeshes()
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

                originalGraph.enableNavmeshCutting = true;
                RLog.Msg($"Enabled Navmesh Cutting for graph: '{originalGraph.name}'");

                allNavMeshGraphs.Add(originalGraph);

                // Attempt to fix scaling
                Mesh originalMesh = originalGraph.sourceMesh;
                if (originalMesh == null) return;

                // Store original values before we change them.
                Vector3 originalOffset = originalGraph.offset;
                float originalScale = originalGraph.scale;
                Bounds originalBounds = originalMesh.bounds;

                // --- STEP 1: Create the new, pre-scaled mesh ---
                Mesh scaledMesh = CreateCorrectlyScaledMesh(originalMesh, originalScale);

                //bool exportfinished = ExportMesh(scaledMesh, $"D:/SteamLibrary/steamapps/common/Sons Of The Forest/Mods/AllowBuildInCaves/{originalGraph.name}_ScaledMesh.obj");
                //RLog.Msg("Export completed" + (exportfinished ? " successfully." : " with errors."));

                string FileName = originalGraph.name;
                string filePath = $"Mods/AllowBuildInCaves/";
                if (File.Exists(filePath + FileName + "_Fixed.obj"))
                {
                    Mesh fixedMesh = ImportMesh(filePath + FileName + "_Fixed.obj");
                    if(fixedMesh != null)
                    {
                        originalGraph.sourceMesh = fixedMesh;
                        RLog.Msg($"Using fixed mesh for {originalGraph.name} from {filePath + FileName + "_Fixed.obj"}");
                    }
                } else
                {
                    originalGraph.sourceMesh = scaledMesh;
                    RLog.Msg($"Using scaled mesh for {originalGraph.name}");
                }

                //Mesh attemptedFixedMesh = MeshRepairUtility.RepairUnityMesh(scaledMesh, 0.5f, 10); //, scaledMesh.vertexCount
                //originalGraph.sourceMesh = attemptedFixedMesh;

                //bool exportfinished = ExportMesh(attemptedFixedMesh, $"D:/SteamLibrary/steamapps/common/Sons Of The Forest/Mods/AllowBuildInCaves/{originalGraph.name}_AutomaticFixTest.obj");
                //RLog.Msg("Export completed" + (exportfinished ? " successfully." : " with errors."));



                originalGraph.scale = 1f; // The whole point: set scale to 1.

                Vector3 V3Scale = new Vector3(originalScale, originalScale, originalScale);
                Vector3 compensationOffset = Vector3.Scale(originalBounds.min, V3Scale);
                originalGraph.offset = originalOffset + compensationOffset;

                AstarPath.active.Scan(originalGraph);
                navmeshesProcessed++;
            }

            if(navmeshesProcessed > 0)
            {
                RLog.Msg($"{navmeshesProcessed} NavMeshGraph(s) processed. Flushing Graph Updates..."); 
                AstarPath.active.FlushGraphUpdates(); 
                RLog.Msg("Rescanning the AstarPath..."); 
                AstarPath.active.Scan(); 
            }
            else
            {
                RLog.Msg("No NavMeshGraphs were processed for cutting.");
            }
        }

        public static bool ExportMesh(Mesh meshToExport, string filePath)
        {
            if (meshToExport == null || !meshToExport.isReadable)
            {
                // Your logging here
                return false;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"# Exported from game on {System.DateTime.Now}");
            sb.AppendLine($"# Object: {meshToExport.name}");
            sb.AppendLine();

            // Write all vertices using InvariantCulture to guarantee '.' as decimal separator
            foreach (Vector3 v in meshToExport.vertices)
            {
                // --- THIS LINE IS THE FIX ---
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "v {0} {1} {2}", -v.x, v.y, v.z));
            }
            sb.AppendLine();

            // Write all triangle indices
            sb.AppendLine("# Faces");
            int[] triangles = meshToExport.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.AppendLine($"f {triangles[i] + 1} {triangles[i + 1] + 1} {triangles[i + 2] + 1}");
            }

            try
            {
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (System.Exception e)
            {
                // Log($"ERROR writing file: {e.Message}");
                return false;
            }

            return true;
        }

        public static Mesh ImportMesh(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var vertices = new Il2CppSystem.Collections.Generic.List<Vector3>();
            var triangles = new Il2CppSystem.Collections.Generic.List<int>();

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                string[] parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    if (parts[0] == "v") // Vertex line
                    {
                        // --- THESE LINES ARE THE FIX ---
                        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        vertices.Add(new Vector3(-x, y, z));
                    }
                    else if (parts[0] == "f") // Face line
                    {
                        triangles.Add(int.Parse(parts[1].Split('/')[0]) - 1);
                        triangles.Add(int.Parse(parts[2].Split('/')[0]) - 1);
                        triangles.Add(int.Parse(parts[3].Split('/')[0]) - 1);
                    }
                }
            }

            if (vertices.Count == 0) return null;

            Mesh newMesh = new Mesh();
            newMesh.name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            newMesh.SetVertices(vertices);
            newMesh.SetTriangles(triangles, 0);

            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
            newMesh.Optimize();

            return newMesh;
        }

        public static Mesh CreateCorrectlyScaledMesh(Mesh sourceMesh, float scaleFactor)
        {
            if (sourceMesh == null) return null;

            // CRITICAL CHECK: This will fail if the mesh is not marked Read/Write enabled.
            if (!sourceMesh.isReadable)
            {
                // Your logging implementation here.
                // Log($"ERROR: Mesh '{sourceMesh.name}' is not readable. Cannot scale.");
                return null;
            }

            Vector3[] originalVertices = sourceMesh.vertices;
            Vector3[] scaledVertices = new Vector3[originalVertices.Length];

            // This is the "secret sauce" matrix from the decompiled code.
            Matrix4x4 transformationMatrix = Matrix4x4.TRS(
                -sourceMesh.bounds.min * scaleFactor,
                Quaternion.identity,
                Vector3.one * scaleFactor
            );

            for (int i = 0; i < originalVertices.Length; i++)
            {
                scaledVertices[i] = transformationMatrix.MultiplyPoint3x4(originalVertices[i]);
            }

            // Create a new mesh and apply the transformed vertices and original triangles.
            Mesh newMesh = new Mesh();
            newMesh.name = sourceMesh.name + "_Scaled";
            newMesh.vertices = scaledVertices;
            newMesh.triangles = sourceMesh.triangles; // Triangles remain the same.

            // Important: Recalculate normals and bounds for the new mesh.
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();

            return newMesh;
        }

        public static void GenerateCaveMeshes()
        {
            if (AstarPath.active == null || AstarPath.active.data == null)
            {
                RLog.Error("AstarPath is not active or data is null.");
                return;
            }

            RecastGraph mainRecastGraph = null;
            //Fetch and store the main recast graph.
            Il2CppSystem.Collections.IEnumerable recastGraphs = AstarPath.active.data.FindGraphsOfType(Il2CppSystem.RuntimeType.GetType("Pathfinding.RecastGraph"));

            foreach (Il2CppSystem.Object graphObject in recastGraphs)
            {
                if (graphObject == null) continue;
                Pathfinding.RecastGraph recastGraph = graphObject.TryCast<Pathfinding.RecastGraph>();
                if (recastGraph == null) continue;
                mainRecastGraph = recastGraph;
                break;
            }

            //Create a holder for our newly created meshes.
            List<GameObject> navMeshHolder = new List<GameObject>();

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

                GameObject meshHolderGO = new GameObject($"ScannableCaveMesh_{originalGraph.name}");
                meshHolderGO.transform.position = Vector3.zero;
                meshHolderGO.transform.rotation = R_solved;
                meshHolderGO.transform.localScale = Vector3.one;

                MeshFilter meshFilter = meshHolderGO.AddComponent<MeshFilter>();
                meshFilter.mesh = meshForGO;

                MeshCollider meshCollider = meshHolderGO.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshForGO;

                // Based on RecastGraph.mask = 71303168, Layer 22 or 26 are valid. Let's use 22.
                const int SCAN_LAYER = 26;
                meshHolderGO.layer = SCAN_LAYER;

                RLog.Msg($"  Set up Scannable GameObject '{meshHolderGO.name}' on layer {SCAN_LAYER}.");
                RLog.Msg($"    GO P: {meshHolderGO.transform.position.ToString("F3")}, GO R(Euler): {meshHolderGO.transform.rotation.eulerAngles.ToString("F3")}");

                Vector3 actual_W_A = meshHolderGO.transform.TransformPoint(SL_A);
                RLog.Msg($"    Verify W_A (idx {idxA}): Target={W_A_target.ToString("F3")}, Actual={actual_W_A.ToString("F3")}, DiffMag={(W_A_target - actual_W_A).magnitude:F5}");
                Vector3 actual_W_B = meshHolderGO.transform.TransformPoint(SL_B);
                RLog.Msg($"    Verify W_B (idx {idxB}): Target={W_B_target.ToString("F3")}, Actual={actual_W_B.ToString("F3")}, DiffMag={(W_B_target - actual_W_B).magnitude:F5}");
                Vector3 actual_W_C = meshHolderGO.transform.TransformPoint(SL_C);
                RLog.Msg($"    Verify W_C (idx {idxC}): Target={W_C_target.ToString("F3")}, Actual={actual_W_C.ToString("F3")}, DiffMag={(W_C_target - actual_W_C).magnitude:F5}");

                //add the new holder to our meshHolder list
                navMeshHolder.Add(meshHolderGO);

                navmeshesProcessed++;

            }

            if (navmeshesProcessed > 0) { 
                RLog.Msg($"{navmeshesProcessed} NavMeshGraph(s) processed. Flushing Graph Updates..."); 
                AstarPath.active.FlushGraphUpdates(); 
                RLog.Msg("Rescanning the AstarPath...");
                AstarPath.active.Scan();
            }
            else 
            { 
                RLog.Msg("No NavMeshGraphs were processed."); 
            }
        }
    }
}
