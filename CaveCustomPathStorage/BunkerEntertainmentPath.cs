using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.CaveCustomPathStorage
{
    internal class BunkerEntertainmentPath
    {
        public static List<NavMeshEditing.PathPoint> BunkerEntertainmentEntrance = new List<NavMeshEditing.PathPoint>
        {
        new NavMeshEditing.PathPoint(new Vector3(-1189.1057f, 65.89719f, 132.98425f), 1f),
        new NavMeshEditing.PathPoint(new Vector3(-1188.9785f, 66.656044f, 130.73032f), 1f),
        new NavMeshEditing.PathPoint(new Vector3(-1189.7195f, 65.21001f, 128.20804f), 1f)
        };

        public static List<NavMeshEditing.PathPoint> BunkerEntertainmentPath1 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1148.8569f, 58.580284f, 52.696037f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-1149.5582f, 58.60133f, 50.227737f), 2f)
        };

        public static List<NavMeshEditing.PathPoint> BunkerEntertainmentPath2 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1139.6923f, 66.49452f, 26.12183f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-1139.273f, 66.48015f, 28.31055f), 2f)
        };

        public static List<NavMeshEditing.PathPoint> BunkerEntertainmentPath3 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1042.0651f, 80.87205f, 123.44184f), 2f),
            new NavMeshEditing.PathPoint(new Vector3(-1039.7598f, 82.79125f, 122.345055f), 2f)
        };

        public static List<NavMeshEditing.PathPoint> BunkerEntertainmentExit = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1022.44574f, 86.7948f, 119.79666f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-1018.1047f, 80.67541f, 118.95245f), 4f),
            new NavMeshEditing.PathPoint(new Vector3(-1009.91876f, 82.48076f, 119.210915f), 3f),
            new NavMeshEditing.PathPoint(new Vector3(-1004.06146f, 84.486015f, 119.460335f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-998.7553f, 86.51888f, 120.17013f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-993.0746f, 89.27304f, 120.70644f), 1f),
            new NavMeshEditing.PathPoint(new Vector3(-986.4371f, 91.69886f, 119.385254f), 2f)
        };
    }
}
