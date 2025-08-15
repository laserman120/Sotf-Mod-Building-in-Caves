using AllowBuildInCaves.NavMeshEditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.CaveCustomPathStorage
{
    internal class CaveAPath
    {

        public static List<PathPoint> CaveEntranceAPath = new List<PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-422.7915f, 14.213082f, 1516.5187f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-423.0554f, 14.176875f, 1510.438f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-423.57507f, 14.258602f, 1504.473f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-424.20978f, 14.187527f, 1499.69f), 2f)
        };

        public static List<PathPoint>  CaveAPath1 = new List<PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-505.34238f, 14.18365f, 1487.3365f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-505.72342f, 12.858449f, 1482.131f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-506.0014f, 10.733319f, 1478.5162f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-505.38913f, 10.219021f, 1472.6107f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-504.46548f, 10.7812f, 1467.5791f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-504.33194f, 11.220503f, 1462.05f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-507.9011f, 11.088988f, 1460.471f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-512.9914f, 10.640795f, 1457.1243f), 2f)
        };

        public static List<PathPoint> CaveAPath2 = new List<PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-541.78564f, 12.776459f, 1395.0784f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-542.03375f, 12.51893f, 1399.8779f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-545.4048f, 12.800155f, 1406.171f), 1f),
        };
    }
}
