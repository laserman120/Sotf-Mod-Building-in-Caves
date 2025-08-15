using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.CaveCustomPathStorage
{
    internal class CaveCPath
    {
        public static List<NavMeshEditing.PathPoint> CaveCEntrancePath = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-541.63055f, 196.36938f, 116.743965f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-536.7924f, 196.3674f, 120.56737f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-534.2979f, 194.94284f, 123.14978f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-531.2838f, 195.81093f, 130.81964f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-531.0927f, 194.7159f, 133.65266f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-534.91095f, 193.65744f, 138.15514f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-542.6415f, 190.90652f, 143.71933f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-547.3978f, 188.733f, 149.38701f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-550.29004f, 186.44298f, 146.3793f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-551.83185f, 185.52008f, 145.60379f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-548.746f, 184.12718f, 151.13078f), 0.6f),
        };

        public static List<NavMeshEditing.PathPoint> CaveCExitPath = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-604.11597f, 176.56227f, 98.11596f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-604.49445f, 176.81609f, 94.51859f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-603.7997f, 174.52701f, 88.38717f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-602.53406f, 173.09883f, 83.00866f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-604.3832f, 172.55789f, 79.71882f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-607.0873f, 170.12498f, 74.876434f), 1f)
        };
    }
}
