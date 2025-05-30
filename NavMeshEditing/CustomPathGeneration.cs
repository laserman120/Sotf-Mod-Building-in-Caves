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

namespace AllowBuildInCaves.NavMeshEditing
{
    internal class CustomPathGeneration
    {
        public static bool GenerateMeshData(Vector3[] IPoints, float forwardDistance, float backwardDistance, string objectName, bool blockNone)
        {

            RLog.Msg("Attempting to create nav mesh link for " + objectName);
            if (IPoints.Length < 2)
            {
                RLog.Error(objectName + " At least 2 points are required to create a navmesh line.");
                return false;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3> allVertices = new Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3>();
            Il2CppSystem.Collections.Generic.List<int> allIndices = new Il2CppSystem.Collections.Generic.List<int>();

            // Handle the first IPoint
            Vector3 firstDirection = (IPoints[1] - IPoints[0]).normalized;
            Vector3 firstPerpendicular = Vector3.Cross(firstDirection, Vector3.up).normalized;
            if (firstPerpendicular == Vector3.zero)
            {
                firstPerpendicular = Vector3.Cross(firstDirection, Vector3.forward).normalized;
            }
            vertices.Add(IPoints[0] - firstPerpendicular * forwardDistance); // Point 1
            vertices.Add(IPoints[0] + firstPerpendicular * forwardDistance); // Point 2

            // Vertices Calculation
            for (int i = 1; i < IPoints.Length - 1; i++)
            {
                Vector3 p1 = new Vector3(IPoints[i - 1].x, IPoints[i].y, IPoints[i - 1].z);
                Vector3 p2 = IPoints[i];
                Vector3 p3 = new Vector3(IPoints[i + 1].x, IPoints[i].y, IPoints[i + 1].z);

                // Calculate the normal of the plane
                Vector3 normal = Vector3.Cross(p1 - p2, p3 - p2).normalized;

                // Calculate the angle between IPoint 1 and 3
                float angle = Vector3.Angle(p1 - p2, p3 - p2);
                Vector3 bisector = ((p1 - p2).normalized + (p3 - p2).normalized).normalized;

                // Project the bisector onto the plane
                Vector3 projectedBisector = bisector - Vector3.Dot(bisector, normal) * normal;

                // Place a point along the projected bisector (Point 3, 6, 9, etc.)
                vertices.Add(p2 + projectedBisector.normalized * forwardDistance);

                // Draw imaginary line from IPoint3 to IPoint2 and place a point (Point 4, 7, 10, etc.)
                vertices.Add(p2 + (p2 - p3).normalized * backwardDistance);

                // Draw imaginary line from IPoint1 to IPoint2 and place a point (Point 5, 8, etc.)
                vertices.Add(p2 + (p2 - p1).normalized * backwardDistance);
            }

            // Handle the last IPoint
            Vector3 lastDirection = (IPoints[IPoints.Length - 2] - IPoints[IPoints.Length - 1]).normalized;
            Vector3 lastPerpendicular = Vector3.Cross(lastDirection, Vector3.up).normalized;
            if (lastPerpendicular == Vector3.zero)
            {
                lastPerpendicular = Vector3.Cross(lastDirection, Vector3.forward).normalized;
            }
            vertices.Add(IPoints[IPoints.Length - 1] + lastPerpendicular * forwardDistance); // Point 13
            vertices.Add(IPoints[IPoints.Length - 1] - lastPerpendicular * forwardDistance); // Point 12

            //Triangle calculation
            for (int i = 1; i < IPoints.Length - 1; i++)
            {
                if (i == 1) // Special handling for the second IPoint (since we start at i = 1)
                {
                    triangles.Add(0);     // 0, 1, 2 
                    triangles.Add(2);
                    triangles.Add(1);

                    triangles.Add(1);     // 2, 3, 1 
                    triangles.Add(2);
                    triangles.Add(3);
                }
                else
                {
                    int baseIndex = (i - 1) * 3; // 3 vertices per IPoint
                    if (!DoLinesIntersect(vertices[baseIndex - 1], vertices[baseIndex + 2], vertices[baseIndex + 1], vertices[baseIndex + 3]))
                    {
                        //If they do NOT intersect
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 3);

                        triangles.Add(baseIndex - 1);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 1);


                    }
                    else
                    {
                        //If they DO intersect
                        triangles.Add(baseIndex - 1);
                        triangles.Add(baseIndex + 3);
                        triangles.Add(baseIndex + 2);

                        triangles.Add(baseIndex - 1);
                        triangles.Add(baseIndex + 2);
                        triangles.Add(baseIndex + 1);
                    }



                    //WINDING ORDER COUNTER CLOCKWISE!
                    // Form triangles 
                    triangles.Add(baseIndex - 1);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex);
                }


            }

            // Form triangles for the last segment (adjust indices)
            int lastBaseIndex = (IPoints.Length - 2) * 3;
            if (!DoLinesIntersect(vertices[lastBaseIndex - 1], vertices[lastBaseIndex + 2], vertices[lastBaseIndex + 1], vertices[lastBaseIndex + 3]))
            {
                //If they do NOT intersect
                triangles.Add(lastBaseIndex + 1);
                triangles.Add(lastBaseIndex + 2);
                triangles.Add(lastBaseIndex + 3);

                triangles.Add(lastBaseIndex - 1);
                triangles.Add(lastBaseIndex + 2);
                triangles.Add(lastBaseIndex + 1);


            }
            else
            {
                //If they DO intersect
                triangles.Add(lastBaseIndex - 1);
                triangles.Add(lastBaseIndex + 3);
                triangles.Add(lastBaseIndex + 2);

                triangles.Add(lastBaseIndex - 1);
                triangles.Add(lastBaseIndex + 2);
                triangles.Add(lastBaseIndex + 1);
            }

            triangles.Add(lastBaseIndex - 1);
            triangles.Add(lastBaseIndex + 1);
            triangles.Add(lastBaseIndex);


            // Add the vertices and indices to the combined lists
            foreach (var vertex in vertices)
            {
                allVertices.Add(vertex);
            }
            for (int j = 0; j < triangles.Count; j++)
            {
                allIndices.Add(triangles[j]); // No need for vertexOffset here
            }

            // --- Create GameObject and component ---

            GameObject navMeshObject = new GameObject("_CustomNavMeshLine-" + objectName + "-First");
            NavMeshCustomMeshAdd navMeshAdder = navMeshObject.AddComponent<NavMeshCustomMeshAdd>();
            // --- Find the correct NavMesh to attach to ---
            Vector3 firstPoint = IPoints[0];
            // --- Apply the combined navmesh ---
            ApplyNavMeshAdd(navMeshAdder, allVertices, allIndices, 1); ;
            // --- Nav link creation (only for the first point) ---
            bool isConnected = false;
            Vector3 connectionPoint = Vector3.zero;
            // Multiple connection attempts with raycasting from the FIRST point
            for (float offset = 0.05f; offset <= 0.3f; offset += 0.05f)
            {
                connectionPoint = firstPoint + Vector3.down * offset;

                if (isConnected = TryAddNavLinkToTerrain(firstPoint, connectionPoint, 1, navMeshAdder))
                {
                    RLog.Msg(objectName + " Found First connection Point");
                    isConnected = true;
                    break;
                }
            }
            if (!isConnected)
            {
                RLog.Error(objectName + " Failed to connect to terrain navmesh on first point");
                return false;
            }

            // --- REPEAT FOR LAST POINT ---
            Vector3 lastPoint = IPoints[0];
            bool isConnectedLast = false;
            Vector3 connectionPointLast = Vector3.zero;
            // Multiple connection attempts with raycasting from the LAST point
            for (float offset = 0.05f; offset <= 0.3f; offset += 0.05f)
            {
                connectionPointLast = lastPoint + Vector3.down * offset;

                if (isConnectedLast = TryAddNavLinkToTerrain(lastPoint, connectionPointLast, 1, navMeshAdder))
                {
                    RLog.Msg(objectName + " Found Last connection Point");
                    isConnectedLast = true;
                    break;
                }
            }
            if (!isConnectedLast)
            {
                RLog.Error(objectName + " Failed to connect to terrain navmesh on last point");
                return false;
            }


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
        AreaMask.None,
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

        public static int TryToFindNavMeshPoint(Vector3 input, bool blockNone)
        {
            bool flag;
            float shortestFoundDistance = 9999f;
            int closestNavMeshFound = 999;
            foreach (AreaMask areaMask in AllAreaMasks)
            {
                if (blockNone && areaMask == AreaMask.None) { continue; }
                int NavGraphMask = VailWorldSimulation._instance.GetNavGraphMaskForArea(areaMask);
                Vector3 closestNavMeshPoint = AiUtilities.GetClosestNavMeshPoint(input, NavGraphMask, out flag);

                if (flag)
                {
                    float distanceToTarget = Vector3.Distance(input, closestNavMeshPoint);
                    RLog.Msg("Found NavMesh at " + input + " with mesh id: " + areaMask + " with distance " + distanceToTarget);

                    if (distanceToTarget < shortestFoundDistance)
                    {
                        shortestFoundDistance = distanceToTarget;
                        closestNavMeshFound = NavGraphMask;
                    }
                }
            }
            if (closestNavMeshFound == 999 || shortestFoundDistance == 9999f)
            {
                return closestNavMeshFound;
            }
            RLog.Msg("Closest NavMesh Found: " + closestNavMeshFound + " with distance: " + shortestFoundDistance);
            return closestNavMeshFound;
        }

        public static bool TryAddNavLinkToTerrain(Vector3 linkPoint, Vector3 checkPoint, int navGraphMask, NavMeshCustomMeshAdd navMeshAdder)
        {
            bool flag;
            Vector3 closestNavMeshPoint = AiUtilities.GetClosestNavMeshPoint(checkPoint, navGraphMask, out flag);
            navMeshAdder._navLinkTests.Add(new NavMeshCustomMeshAdd.NavLinkLocations(linkPoint, checkPoint, checkPoint, false, false, false, false));
            if (!flag || Vector3ExtensionMethods.DistanceWithYMargin(checkPoint, closestNavMeshPoint, 0.25f) > navMeshAdder._navLinkMaxDistance)
            {
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
    }
}
