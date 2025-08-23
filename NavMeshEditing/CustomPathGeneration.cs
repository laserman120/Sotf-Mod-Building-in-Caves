using Endnight.Utilities;
using Pathfinding;
using RedLoader;
using Sons.Ai.Vail;
using Sons.Ai;
using Sons.Areas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Sons.Ai.Vail.VailWorldPaths;
using HarmonyLib;
using static UnityEngine.ParticleSystem.PlaybackState;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using SonsSdk;
using UnityEngine.UIElements;
using TheForest.Utils;
using System.Globalization;
using SonsSdk.Attributes;
using AllowBuildInCaves.Debug;

namespace AllowBuildInCaves.NavMeshEditing
{

    public struct PathPoint
    {
        public Vector3 Position;
        public float Width;

        public PathPoint(Vector3 position, float width)
        {
            Position = position;
            Width = width;
        }
    }
    internal static class CustomPathGeneration
    {
        public static List<Vector3> PathCreationPoints = new List<Vector3>();


        public static void ProcessAllCustomPaths(bool showDebug)
        {
            var pathStorageTypes = typeof(CustomPathGeneration).Assembly.GetTypes() 
                .Where(t => t.IsClass && t.Namespace == "AllowBuildInCaves.CaveCustomPathStorage" && t.Name.EndsWith("Path"));

            var pathDefinitions = pathStorageTypes
                .SelectMany(type => type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Where(fieldInfo => fieldInfo.FieldType == typeof(List<PathPoint>))
                    .Select(fieldInfo => new
                    {
                        PathList = (List<PathPoint>)fieldInfo.GetValue(null),
                        FieldName = fieldInfo.Name, // This is the name like "caveBExitPath"
                        ClassName = type.Name       // This is the name like "CaveBPath"
                    }))
                .ToList();

            DebugManager.DebugLog("Found " + pathDefinitions.Count + " custom path lists to process.");

            foreach (var definition in pathDefinitions)
            {
                List<PathPoint> pathPoints = definition.PathList;
                string pathNameFromField = definition.FieldName; // e.g., "caveBExitPath"
                string className = definition.ClassName;         // e.g., "CaveBPath"

                if (pathPoints == null || pathPoints.Count < 2)
                {
                    DebugManager.DebugLogError($"Path points list for {className}.{pathNameFromField} is null or has less than 2 points, skipping.");
                    continue;
                }

                string objectName = pathNameFromField;

                DebugManager.DebugLog($"Processing custom path: {className}.{pathNameFromField}");
                GenerateCustomPath(pathPoints, 0.5f, objectName, false, showDebug);
            }
        }

        public static bool GenerateCustomPath(List<PathPoint> IPoints, float backwardDistance, string objectName, bool blockNone, bool debugSpheres)
        {
            if (debugSpheres)
            {
                NavMeshEditing.CustomPathGeneration.DebugPlaceSpheres(IPoints, 0.3f, Color.yellow, "_DebugPathPoints" + objectName);
            }

            DebugManager.DebugLog("Attempting to create nav mesh link for " + objectName);
            if (IPoints.Count < 2)
            {
                DebugManager.DebugLogError(objectName + " At least 2 points are required to create a navmesh line.");
                return false;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3> allVertices = new Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3>();
            Il2CppSystem.Collections.Generic.List<int> allIndices = new Il2CppSystem.Collections.Generic.List<int>();

            // Handle the first IPoint
            Vector3 firstPos = IPoints[0].Position;
            float firstHalfWidth = IPoints[0].Width / 2f;
            Vector3 secondPos = IPoints[1].Position;

            Vector3 firstDirection = (secondPos - firstPos).normalized;
            Vector3 firstPerpendicular = Vector3.Cross(firstDirection, Vector3.up).normalized;
            if (firstPerpendicular == Vector3.zero)
            {
                firstPerpendicular = Vector3.Cross(firstDirection, Vector3.forward).normalized;
            }
            vertices.Add(firstPos - firstPerpendicular * firstHalfWidth); // Vertex 0
            vertices.Add(firstPos + firstPerpendicular * firstHalfWidth); // Vertex 1

            // Vertices Calculation for intermediate points
            for (int i = 1; i < IPoints.Count - 1; i++)
            {
                Vector3 prevPos_actual = IPoints[i - 1].Position;
                Vector3 currentPos_actual = IPoints[i].Position;
                Vector3 nextPos_actual = IPoints[i + 1].Position;
                float currentHalfWidth = IPoints[i].Width / 2f;

                // p1, p2, p3 are for bisector calculation, projected onto currentPos_actual.y plane
                Vector3 p1_proj = new Vector3(prevPos_actual.x, currentPos_actual.y, prevPos_actual.z);
                Vector3 p2_proj = currentPos_actual; // This is the current IPoint's position
                Vector3 p3_proj = new Vector3(nextPos_actual.x, currentPos_actual.y, nextPos_actual.z);

                Vector3 planeNormal = Vector3.Cross(p1_proj - p2_proj, p3_proj - p2_proj).normalized;
                if (planeNormal == Vector3.zero)
                { // Fallback for collinear points on the calculation plane
                    Vector3 tangent = (p3_proj - p1_proj).normalized;
                    planeNormal = Vector3.Cross(tangent, Vector3.up).normalized;
                    if (planeNormal == Vector3.zero) planeNormal = Vector3.Cross(tangent, Vector3.forward).normalized;
                    if (planeNormal == Vector3.zero) planeNormal = Vector3.up; // Absolute fallback
                }

                Vector3 bisectorDirInput1 = (p1_proj - p2_proj).normalized;
                Vector3 bisectorDirInput2 = (p3_proj - p2_proj).normalized;
                Vector3 bisectorSum = bisectorDirInput1 + bisectorDirInput2;

                Vector3 projectedBisector;
                if (bisectorSum.sqrMagnitude < 0.0001f) // Inputs to bisector are nearly opposite (collinear segment)
                {
                    // Perpendicular to the segment direction, on the p2_proj plane
                    projectedBisector = Vector3.Cross((p3_proj - p1_proj).normalized, planeNormal).normalized;
                }
                else
                {
                    projectedBisector = bisectorSum.normalized;
                    // Project onto the plane defined by p2_proj and planeNormal (effectively removing component along planeNormal)
                    projectedBisector = (projectedBisector - Vector3.Dot(projectedBisector, planeNormal) * planeNormal).normalized;
                }
                if (projectedBisector.sqrMagnitude < 0.0001f)
                { // Fallback if projection failed or was zero
                    projectedBisector = Vector3.Cross((p2_proj - p1_proj).normalized, planeNormal).normalized; // Perpendicular to incoming segment
                    if (projectedBisector.sqrMagnitude < 0.0001f) projectedBisector = Vector3.right; // Absolute fallback
                }


                // Vertex order for each intermediate IPoint's triplet:
                // 1. Outer point (along bisector)
                vertices.Add(p2_proj + projectedBisector * currentHalfWidth);
                // 2. "Backward" point towards next IPoint (p3_proj direction from p2_proj)
                vertices.Add(p2_proj + (p2_proj - p3_proj).normalized * backwardDistance);
                // 3. "Backward" point towards previous IPoint (p1_proj direction from p2_proj)
                vertices.Add(p2_proj + (p2_proj - p1_proj).normalized * backwardDistance);
            }

            // Handle the last IPoint
            Vector3 lastPos = IPoints[IPoints.Count - 1].Position;
            float lastHalfWidth = IPoints[IPoints.Count - 1].Width / 2f;
            Vector3 secondToLastPos = IPoints[IPoints.Count - 2].Position;

            Vector3 lastSegmentDirection = (lastPos - secondToLastPos).normalized;
            Vector3 lastPerpendicular = Vector3.Cross(lastSegmentDirection, Vector3.up).normalized;
            if (lastPerpendicular == Vector3.zero)
            {
                lastPerpendicular = Vector3.Cross(lastSegmentDirection, Vector3.forward).normalized;
            }

            vertices.Add(lastPos + lastPerpendicular * lastHalfWidth); // Vertex (vertices.Count-2 after adding)
            vertices.Add(lastPos - lastPerpendicular * lastHalfWidth); // Vertex (vertices.Count-1 after adding)

            if (IPoints.Count == 2)
            {
                triangles.Add(0); triangles.Add(2); triangles.Add(1); // StartL, EndR, StartR
                triangles.Add(0); triangles.Add(3); triangles.Add(2); // StartL, EndL, EndR
            }
            else
            {
                // Triangle loop for intermediate segments
                for (int i = 1; i < IPoints.Count - 1; i++)
                {
                    if (i == 1) // Triangles for segment between IPoints[0] and IPoints[1]
                    {

                        if (DoLinesIntersect(vertices[1], vertices[2], vertices[0], vertices[3]))
                        {
                            DebugManager.DebugLogWarning(objectName + ": First segment (IPoints[0] to IPoints[1]) diagonals do NOT intersect.");
                            triangles.Add(0); triangles.Add(2); triangles.Add(3);
                            triangles.Add(0); triangles.Add(3); triangles.Add(1);
                        }
                        else
                        {
                            DebugManager.DebugLogWarning(objectName + ": First segment (IPoints[0] to IPoints[1]) diagonals do NOT intersect. Using alternative triangulation.");
                            triangles.Add(0); triangles.Add(2); triangles.Add(1);
                            triangles.Add(0); triangles.Add(2); triangles.Add(3);

                        }
                    }
                    else // Triangles for segment between IPoints[i-1] and IPoints[i], where i > 1
                    {
                        int baseIndex = 2 + (i - 2) * 3 + 1;

                        // Vertices for IPoints[i]
                        int currOuterIdx = 2 + (i - 1) * 3 + 0;
                        int currBackToNextIdx = 2 + (i - 1) * 3 + 1;

                        if (!DoLinesIntersect(vertices[baseIndex - 1], vertices[currOuterIdx], vertices[baseIndex + 1], vertices[currBackToNextIdx]))
                        {
                            //If they do NOT intersect
                            triangles.Add(baseIndex + 1);       // Prev.BackToPrev
                            triangles.Add(currOuterIdx);        // Curr.Outer
                            triangles.Add(currBackToNextIdx);   // Curr.BackToNext

                            triangles.Add(baseIndex - 1);       // Prev.Outer
                            triangles.Add(currOuterIdx);        // Curr.Outer
                            triangles.Add(baseIndex + 1);       // Prev.BackToPrev
                        }
                        else
                        {
                            //If they DO intersect
                            triangles.Add(baseIndex - 1);       // Prev.Outer
                            triangles.Add(currBackToNextIdx);   // Curr.BackToNext
                            triangles.Add(currOuterIdx);        // Curr.Outer

                            // User's original second triangle for intersecting case
                            triangles.Add(baseIndex - 1);       // Prev.Outer
                            triangles.Add(currOuterIdx);        // Curr.Outer
                            triangles.Add(baseIndex + 1);       // Prev.BackToPrev
                        }

                        triangles.Add(baseIndex - 1); // Prev.Outer
                        triangles.Add(baseIndex + 1); // Prev.BackToPrev
                        triangles.Add(baseIndex);     // Prev.BackToNext
                    }
                }
                int lastBaseIndex = 2 + ((IPoints.Count - 2) - 1) * 3 + 1;

                int finalV1_idx = vertices.Count - 2; // e.g., EndR
                int finalV2_idx = vertices.Count - 1; // e.g., EndL

                if (!DoLinesIntersect(vertices[lastBaseIndex - 1], vertices[finalV1_idx], vertices[lastBaseIndex + 1], vertices[finalV2_idx]))
                {
                    //If they do NOT intersect
                    triangles.Add(lastBaseIndex + 1); // PrevLast.BackToPrev
                    triangles.Add(finalV1_idx);       // Final_V1
                    triangles.Add(finalV2_idx);       // Final_V2

                    triangles.Add(lastBaseIndex - 1); // PrevLast.Outer
                    triangles.Add(finalV1_idx);       // Final_V1
                    triangles.Add(lastBaseIndex + 1); // PrevLast.BackToPrev
                }
                else
                {
                    //If they DO intersect
                    triangles.Add(lastBaseIndex - 1); // PrevLast.Outer
                    triangles.Add(finalV2_idx);       // Final_V2
                    triangles.Add(finalV1_idx);       // Final_V1

                    // User's original second triangle
                    triangles.Add(lastBaseIndex - 1); // PrevLast.Outer
                    triangles.Add(finalV1_idx);       // Final_V1
                    triangles.Add(lastBaseIndex + 1); // PrevLast.BackToPrev
                }

                // Fan triangle for IPoints[Count-2]'s joint (user's original order)
                triangles.Add(lastBaseIndex - 1); // PrevLast.Outer
                triangles.Add(lastBaseIndex + 1); // PrevLast.BackToPrev
                triangles.Add(lastBaseIndex);     // PrevLast.BackToNext
            }


            foreach (var vertex in vertices) { allVertices.Add(vertex); }
            foreach (int index in triangles) { allIndices.Add(index); }

            GameObject navMeshObject = new GameObject("_CustomNavMeshLine-" + objectName);
            NavMeshCustomMeshAdd navMeshAdder = navMeshObject.AddComponent<NavMeshCustomMeshAdd>();
            navMeshAdder._navLinkMaxDistance = 0.5f;
            ApplyNavMeshAdd(navMeshAdder, allVertices, allIndices, 1); // Default navGraphMask = 1


            //Connect to terrain navmesh
            Vector3 firstIPointPos = IPoints[0].Position;
            bool isConnectedFirst = false;
            for (float offset = 0.05f; offset <= 0.3f; offset += 0.05f)
            {
                Vector3 connectionPointStart = firstIPointPos + Vector3.down * offset;
                if (TryAddNavLinkToTerrain(firstIPointPos, connectionPointStart, AllowBuildInCaves.graphMask, navMeshAdder))
                {
                    DebugManager.DebugLog(objectName + " Found First connection Point with mask " + 1);
                    isConnectedFirst = true;
                    break;
                }

            }
            if (!isConnectedFirst) DebugManager.DebugLogError(objectName + " Failed to connect to terrain navmesh on first point");

            Vector3 lastIPointPos = IPoints[IPoints.Count - 1].Position;
            bool isConnectedLast = false;
            for (float offset = 0.05f; offset <= 0.3f; offset += 0.05f)
            {
                Vector3 connectionPointEnd = lastIPointPos + Vector3.down * offset;
                if (TryAddNavLinkToTerrain(lastIPointPos, connectionPointEnd, AllowBuildInCaves.graphMask, navMeshAdder))
                {
                    DebugManager.DebugLog(objectName + " Found Last connection Point with mask " + 1);
                    isConnectedLast = true;
                    break;
                }
            }

            if (!isConnectedLast) DebugManager.DebugLogError(objectName + " Failed to connect to terrain navmesh on last point! Position: " + lastIPointPos.ToString());

            return true;
        }

        static bool DoLinesIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            // Ensure all points are on the same plane (using p1.y as the height)
            p1.y = p2.y = p3.y = p4.y;

            // Calculate the direction vectors of the lines (Corrected)
            Vector3 d1 = (p2 - p1);
            Vector3 d2 = (p4 - p3);

            // Calculate the denominator for the parametric equations
            float denominator = d1.x * d2.z - d1.z * d2.x;

            // If the denominator is zero, the lines are parallel
            if (Mathf.Abs(denominator) < 1e-6f)
            {
                return false;
            }

            // Calculate the parameters for the intersection point
            float t1 = ((p1.z - p3.z) * d2.x - (p1.x - p3.x) * d2.z) / denominator;
            float t2 = ((p1.z - p3.z) * d1.x - (p1.x - p3.x) * d1.z) / denominator;

            // Check if the intersection point lies within both line segments
            return (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1);
        }

        private static List<AreaMask> AllAreaMasks = new List<AreaMask>
        {
            AreaMask.CaveA,
            AreaMask.CaveB,
            AreaMask.CaveC,
            AreaMask.CaveD,
            AreaMask.CaveF,
            AreaMask.CaveG,
            AreaMask.BunkerA,
            AreaMask.BunkerB,
            AreaMask.BunkerC,
            AreaMask.BunkerE,
            AreaMask.BunkerF,
            AreaMask.BunkerG
        };

        public static bool TryAddNavLinkToTerrain(Vector3 linkPoint, Vector3 checkPoint, int navGraphMask, NavMeshCustomMeshAdd navMeshAdder)
        {
            bool flag;
            Vector3 closestNavMeshPoint = AiUtilities.GetClosestNavMeshPoint(checkPoint, navGraphMask, out flag);
            DebugManager.DebugLog("Found closest nav mesh point at: " + closestNavMeshPoint + " flag: " + flag);
            navMeshAdder._navLinkTests.Add(new NavMeshCustomMeshAdd.NavLinkLocations(linkPoint, checkPoint, checkPoint, false, false, false, false));
            if (!flag || Vector3ExtensionMethods.DistanceWithYMargin(checkPoint, closestNavMeshPoint, 0.25f) > navMeshAdder._navLinkMaxDistance)
            {
                DebugManager.DebugLog("failed to add navlink due to YMargin distance of: " + Vector3ExtensionMethods.DistanceWithYMargin(checkPoint, closestNavMeshPoint, 0.25f));
                return false;
            }
            GameObject gameObject = new GameObject("start");
            NavAddLink navAddLink = gameObject.GetOrAddComponent<NavAddLink>();
            navAddLink.graphMask = navGraphMask;
            gameObject.transform.parent = navMeshAdder.transform;
            gameObject.transform.position = linkPoint;
            Transform transform = new GameObject("target").transform;
            transform.parent = navMeshAdder.transform;
            transform.position = closestNavMeshPoint;
            navAddLink.end = transform;
            navMeshAdder._navAddLinks.Add(navAddLink);
            return true;
        }

        //Rewrite of ApplyNavMeshAdd
        public static void ApplyNavMeshAdd(NavMeshCustomMeshAdd navMeshCustomMeshAdd, Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3> points, Il2CppSystem.Collections.Generic.List<int> indices, int navGraphMask)
        {
            CreateCustomMesh(navMeshCustomMeshAdd, points, indices);
            SetupNavMeshAdd(navMeshCustomMeshAdd, navGraphMask);
        }

        private static void CreateCustomMesh(NavMeshCustomMeshAdd navMeshCustomMeshAdd, Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3> points, Il2CppSystem.Collections.Generic.List<int> indices)
        {
            if (navMeshCustomMeshAdd._customNavAddMesh == null)
            {
                navMeshCustomMeshAdd._customNavAddMesh = new Mesh();
                navMeshCustomMeshAdd._customNavAddMesh.name = "CustomNavMeshAdd";
                if (navMeshCustomMeshAdd._previewMeshFilter != null)
                {
                    navMeshCustomMeshAdd._previewMeshFilter.sharedMesh = navMeshCustomMeshAdd._customNavAddMesh;
                }
            }
            navMeshCustomMeshAdd._customNavAddMesh.Clear();
            navMeshCustomMeshAdd._customNavAddMesh.SetVertices(points);
            navMeshCustomMeshAdd._customNavAddMesh.SetIndices(indices, MeshTopology.Triangles, 0, true, 0);
        }

        private static void SetupNavMeshAdd(NavMeshCustomMeshAdd navMeshCustomMeshAdd, int navGraphMask)
        {
            if (navMeshCustomMeshAdd._navMeshAdd == null)
            {
                navMeshCustomMeshAdd._navMeshAdd = navMeshCustomMeshAdd.gameObject.AddComponent<NavmeshAdd>();
            }
            navMeshCustomMeshAdd._navMeshAdd.enabled = false;
            navMeshCustomMeshAdd._navMeshAdd.mesh = navMeshCustomMeshAdd._customNavAddMesh;
            navMeshCustomMeshAdd._navMeshAdd.type = NavmeshAdd.MeshType.CustomMesh;
            navMeshCustomMeshAdd._navMeshAdd.useRotationAndScale = true;
            navMeshCustomMeshAdd._navMeshAdd.mesh = navMeshCustomMeshAdd._customNavAddMesh;
            navMeshCustomMeshAdd._navMeshAdd.graphMask = navGraphMask;
            navMeshCustomMeshAdd._navMeshAdd.RebuildMesh();
            navMeshCustomMeshAdd._navMeshAdd.enabled = true;
        }

        public static void DebugPlaceSpheres(List<PathPoint> iPoints, float sphereDiameter = 0.2f, Color? sphereColor = null, string parentObjectName = "_DebugPathPoints")
        {
            if (iPoints == null || iPoints.Count == 0)
            {
                DebugManager.DebugLogWarning("DebugPlaceSpheres: No points provided to visualize.");
                return;
            }

            Transform parentTransform = null;
            if (!string.IsNullOrEmpty(parentObjectName))
            {
                GameObject parentObject = GameObject.Find(parentObjectName);
                if (parentObject == null)
                {
                    parentObject = new GameObject(parentObjectName);
                }
                parentTransform = parentObject.transform;
            }

            Color colorToUse = sphereColor ?? Color.yellow; // Default to yellow if no color is specified

            for (int i = 0; i < iPoints.Count; i++)
            {
                PathPoint currentPoint = iPoints[i];
                var cube = DebugTools.CreateCuboid(currentPoint.Position, new Vector3(0.3f, 0.3f, 0.3f), Color.yellow, false);

                // Name the sphere for easier identification in the hierarchy
                cube.name = $"DebugSphere_Point_{i}_Pos({currentPoint.Position.x:F1},{currentPoint.Position.y:F1},{currentPoint.Position.z:F1})_Width({currentPoint.Width:F1})";

                if (parentTransform != null)
                {
                    cube.transform.SetParent(parentTransform, true);
                }
            }
            DebugManager.DebugLog($"DebugPlaceSpheres: Placed {iPoints.Count} debug spheres" + (parentTransform != null ? $" under '{parentObjectName}'." : "."));
        }

        public static void LogCurrentPositionData()
        {
            PathCreationPoints.Add(new Vector3(LocalPlayer._instance.transform.position.x, (LocalPlayer._instance.transform.position.y - 1f), LocalPlayer._instance.transform.position.z));
            SonsTools.ShowMessage("Added point at current position");
        }

        [DebugCommand("resetcurrentpathpoints")]
        private static void ResetCurrentPathPoints()
        {
            PathCreationPoints = new List<Vector3>();
            SonsTools.ShowMessage("Reset current path points");
        }

        [DebugCommand("printpathpoints")]
        private static void PrintPathPoints()
        {
            if (PathCreationPoints.Count == 0)
            {
                SonsTools.ShowMessage("No path points to print.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Current Path Points:");
            sb.AppendLine("public static List<NavMeshEditing.PathPoint> NAME_HERE = new List<NavMeshEditing.PathPoint>");
            sb.AppendLine("{");
            for (int i = 0; i < PathCreationPoints.Count; i++)
            {
                if (i == PathCreationPoints.Count - 1)
                {
                    sb.AppendLine($"new NavMeshEditing.PathPoint(new Vector3({PathCreationPoints[i].x.ToString(CultureInfo.InvariantCulture)}f, {PathCreationPoints[i].y.ToString(CultureInfo.InvariantCulture)}f, {PathCreationPoints[i].z.ToString(CultureInfo.InvariantCulture)}f), 1f)"); // Last point without comma
                }
                else 
                { 
                    sb.AppendLine($"new NavMeshEditing.PathPoint(new Vector3({PathCreationPoints[i].x.ToString(CultureInfo.InvariantCulture)}f, {PathCreationPoints[i].y.ToString(CultureInfo.InvariantCulture)}f, {PathCreationPoints[i].z.ToString(CultureInfo.InvariantCulture)}f), 1f),");
                }
            }
            sb.AppendLine("};");
            DebugManager.DebugLog("" + sb.ToString() + "");
        }
    }


}
