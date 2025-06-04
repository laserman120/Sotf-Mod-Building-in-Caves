using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.CaveCustomPathStorage
{
    internal class CaveBPath
    {
        public static List<NavMeshEditing.PathPoint> caveBEntrancePath = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1110.265f, 127.1044f, -174.5312f), 2.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1105.938f, 126.6792f, -177.0387f), 2.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1104.349f, 126.6488f, -179.1931f), 1.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1104.005f, 126.4338f, -182.7815f), 1.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1100.407f, 126.5926f, -190.0152f), 7.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1103.263f, 126.8902f, -195.6777f), 6.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1113.952f, 126.6484f, -199.6777f), 3.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1121.671f, 124.8893f, -200.1978f), 3.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1135.445f, 121.3035f, -198.7219f), 3.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1141.307f, 118.1903f, -197.5179f), 2.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1147.759f, 114.5879f, -197.1476f), 2.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1150.017f, 112.3904f, -201.5762f), 5.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1155.494f, 110.2265f, -206.1242f), 8.5f)
        };

        public static List<NavMeshEditing.PathPoint> caveBExitPath = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1248.309f, 147.5671f, -306.7704f), 1.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1240.829f, 145.5723f, -305.8646f), 1.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1236.813f, 143.3673f, -302.8552f), 1.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1228.169f, 140.0232f, -299.5764f), 1.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1226.203f, 138.7428f, -289.9103f), 1.5f),
            new NavMeshEditing.PathPoint(new Vector3(-1231.494f, 138.3829f, -287.4649f), 1.5f)
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath1 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1128.3707f, 111.0935f, -234.0559f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-1120.5676f, 111.47039f, -235.4965f), 1.0f),  
            new NavMeshEditing.PathPoint(new Vector3(-1114.7706f, 112.80737f, -235.28995f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-1107.1766f, 112.54262f, -235.54236f), 1.0f),   
            new NavMeshEditing.PathPoint(new Vector3(-1100.5146f, 112.83308f, -235.20045f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-1095.6952f, 112.60673f, -235.85037f), 1.0f),  
            new NavMeshEditing.PathPoint(new Vector3(-1086.7715f, 112.50865f, -234.81406f), 1.0f)   
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath2 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1066.2992f, 111.605934f, -250.87259f), 1.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1065.2366f, 111.68354f, -253.59924f), 1.0f),  
            new NavMeshEditing.PathPoint(new Vector3(-1063.2893f, 111.68283f, -255.55797f), 1.0f)  
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath3 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1056.2083f, 111.618416f, -262.03076f), 1.0f),  
            new NavMeshEditing.PathPoint(new Vector3(-1051.044f, 111.60003f, -264.24133f), 1.0f),   
            new NavMeshEditing.PathPoint(new Vector3(-1046.7478f, 111.374855f, -265.115f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-1040.5513f, 110.95758f, -267.8926f), 1.0f),    
            new NavMeshEditing.PathPoint(new Vector3(-1028.3813f, 110.52724f, -264.74918f), 1.0f)  
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath4 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-986.441f, 111.3961f, -245.15076f), 1.0f),   
            new NavMeshEditing.PathPoint(new Vector3(-976.46826f, 110.84706f, -245.40886f), 1.0f),   
            new NavMeshEditing.PathPoint(new Vector3(-969.3101f, 111.56114f, -240.44698f), 1.0f)  
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath5 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-954.65546f, 111.63268f, -264.95676f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-953.9552f, 111.67043f, -270.33597f), 1.0f),  
            new NavMeshEditing.PathPoint(new Vector3(-950.90027f, 111.62162f, -274.2838f), 1.0f)  
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath6 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-938.4141f, 111.188286f, -308.21902f), 1.0f),   
            new NavMeshEditing.PathPoint(new Vector3(-937.3134f, 111.040855f, -313.37476f), 1.0f) 
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath7 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-937.29364f, 110.7246f, -319.64484f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-937.9257f, 110.18006f, -327.20706f), 1.0f),  
            new NavMeshEditing.PathPoint(new Vector3(-941.6112f, 108.811874f, -336.71613f), 1.0f), 
            new NavMeshEditing.PathPoint(new Vector3(-945.30646f, 107.8459f, -341.24432f), 1.0f),      
            new NavMeshEditing.PathPoint(new Vector3(-950.52264f, 106.337654f, -348.7366f), 1.0f),
            new NavMeshEditing.PathPoint(new Vector3(-954.55505f, 104.75692f, -356.61072f), 1.0f),    
            new NavMeshEditing.PathPoint(new Vector3(-958.33887f, 103.74474f, -359.15506f), 1.0f),    
            new NavMeshEditing.PathPoint(new Vector3(-960.2669f, 102.94856f, -362.5789f), 1.0f)    
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath8 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1015.0199f, 111.56116f, -255.98657f), 1.0f),
            new NavMeshEditing.PathPoint(new Vector3(-1010.7399f, 111.543304f, -252.89966f), 1.0f),
        };

        public static List<NavMeshEditing.PathPoint> caveBInsidePath9 = new List<NavMeshEditing.PathPoint>
        {
            new NavMeshEditing.PathPoint(new Vector3(-1004.53253f, 111.22773f, -246.08543f), 1.0f),
            new NavMeshEditing.PathPoint(new Vector3(-997.8169f, 111.17458f, -244.70311f), 1.0f),
        };
    }
}
