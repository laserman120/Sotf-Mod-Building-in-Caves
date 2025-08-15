using Endnight.Environment;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using RedLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.NavMeshEditing
{
    internal class CustomHoleGeneration
    {
        public struct TerrainHole
        {
            public int RectHeight;
            public int RectWidth;
            public Vector3 Position;
            public int Rotation;

            public TerrainHole(int rectHeight, int rectWidth, Vector3 position, int rotation)
            {
                RectHeight = rectHeight;
                RectWidth = rectWidth;
                Position = position;
                Rotation = rotation;
            }
        }

        public static void CreateTerrainHole(List<TerrainHole> terrainHoles)
        {
            Il2CppReferenceArray<Terrain> activeTerrains = TerrainUtilities.ActiveTerrains();
            for (int i = 0; i < activeTerrains.Length; i++)
            {
                Terrain terrain = activeTerrains[i];
                if (terrain == null)
                {
                    RLog.Error("Terrain is null, skipping terrain processing.");
                    continue;
                }

                if (terrain.name.StartsWith("Site02"))
                {
                    RLog.Msg($"Processing terrain: {terrain.name}");
                    TerrainData td = terrain.terrainData;

                    int holeMapResolution = td.holesResolution;
                    RLog.Msg($"Hole map resolution for {terrain.name}: {holeMapResolution}x{holeMapResolution}");

                    RenderTexture holeProcessingRT = RenderTexture.GetTemporary(holeMapResolution, holeMapResolution, 0, RenderTextureFormat.R8);

                    if (holeProcessingRT == null)
                    {
                        RLog.Error("Failed to get temporary RenderTexture.");
                        continue;
                    }

                    RenderTexture oldActive = RenderTexture.active;
                    RenderTexture.active = holeProcessingRT;

                    Texture existingHolesTexture = td.holesTexture;
                    if (existingHolesTexture != null)
                    {
                        RLog.Msg("Existing holesTexture found. Blitting to processing RT.");
                        Graphics.Blit(existingHolesTexture, holeProcessingRT);
                    }
                    else
                    {
                        RLog.Msg("No existing holesTexture found or it's null. Clearing processing RT to 'all solid'.");
                        GL.Clear(true, true, Color.white);
                    }

                    Material blackMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                    blackMaterial.SetColor("_Color", Color.black);

                    DrawTerrainHole(terrainHoles, holeMapResolution, blackMaterial);

                    RectInt rectToCopyFromRT = new RectInt(0, 0, holeMapResolution, holeMapResolution);
                    int destinationXOnHoleMap = 0;
                    int destinationYOnHoleMap = 0;
                    bool allowDelayedCPUSync = true;

                    RLog.Msg($"Calling Internal_CopyActiveRenderTextureToHoles for {terrain.name}");
                    RLog.Msg($"  Source Rect: x={rectToCopyFromRT.x}, y={rectToCopyFromRT.y}, w={rectToCopyFromRT.width}, h={rectToCopyFromRT.height}");
                    RLog.Msg($"  Dest Coords: x={destinationXOnHoleMap}, y={destinationYOnHoleMap}");
                    RLog.Msg($"  Delayed Sync: {allowDelayedCPUSync}");

                    try
                    {
                        td.Internal_CopyActiveRenderTextureToHoles(rectToCopyFromRT, destinationXOnHoleMap, destinationYOnHoleMap, allowDelayedCPUSync);
                        RLog.Msg("Successfully called Internal_CopyActiveRenderTextureToHoles.");
                    }
                    catch (System.Exception e)
                    {
                        RLog.Error($"Exception calling Internal_CopyActiveRenderTextureToHoles: {e.Message}\n{e.StackTrace}");
                    }

                    //finally sync collider with holes
                    td.Internal_SyncHoles();


                    // Restore previously active RenderTexture and release the temporary one
                    RenderTexture.active = oldActive;
                    RenderTexture.ReleaseTemporary(holeProcessingRT);

                    if (blackMaterial != null) UnityEngine.Object.Destroy(blackMaterial);

                    break;
                }
            }
        }

        private static void DrawTerrainHole(List<TerrainHole> terrainHoles, int holeMapResolution, Material drawMaterial) // holeMapResolution is still passed for GL.LoadPixelMatrix and clarity
        {
            foreach (TerrainHole hole in terrainHoles)
            {
                int rectHeight = hole.RectHeight;
                int rectWidth = hole.RectWidth;
                Vector3 worldCenterPosition = hole.Position;
                int rotationDegrees = hole.Rotation;


                RLog.Msg($"DrawRectangularHoleOnActiveRT called with: H={rectHeight}, W={rectWidth}, Pos={worldCenterPosition}, Rot={rotationDegrees}, Res={holeMapResolution}");

                if (drawMaterial == null)
                {
                    RLog.Error("Draw material is null in DrawRectangularHoleOnActiveRT.");
                    return;
                }
                drawMaterial.SetPass(0); // Ensure material is set to draw the desired "hole" color

                GL.PushMatrix();
                // Setup GL to draw in pixel coordinates on the active RenderTexture.
                // (0,0) is top-left, (holeMapResolution, holeMapResolution) is bottom-right for drawing.
                GL.LoadPixelMatrix(0, holeMapResolution, holeMapResolution, 0);

                // --- Define Terrain World Boundaries for mapping to its hole texture ---
                const float terrainMinX = -2000f;
                const float terrainMinZ = -2000f; // The "bottom" or "south" Z extent of the terrain
                const float terrainTotalWidth = 4000f;  // (+2000 - (-2000))
                const float terrainTotalHeight = 4000f; // (+2000 - (-2000))

                // --- Calculate Rotated Rectangle Corners in World Space ---
                Vector2 center = new Vector2(worldCenterPosition.x, worldCenterPosition.z);
                float halfW = rectWidth / 2.0f;
                float halfH = rectHeight / 2.0f;

                float angleRad = rotationDegrees * Mathf.Deg2Rad;
                float cosTheta = Mathf.Cos(angleRad);
                float sinTheta = Mathf.Sin(angleRad);

                Vector2[] localCorners = new Vector2[] {
                    new Vector2(-halfW,  halfH), // Local Top-Left
                    new Vector2( halfW,  halfH), // Local Top-Right
                    new Vector2( halfW, -halfH), // Local Bottom-Right
                    new Vector2(-halfW, -halfH)  // Local Bottom-Left
                };

                Vector3[] pixelVertices = new Vector3[4]; // Will store Z as 0 for GL.Vertex3
                RLog.Msg("Calculating Pixel Vertices (Mapping to Terrain [-2000,+2000] -> Hole Texture [0,holeMapRes]):");

                for (int i = 0; i < 4; i++)
                {
                    float localX = localCorners[i].x;
                    float localY = localCorners[i].y;

                    float worldXOffset = localX * cosTheta - localY * sinTheta;
                    float worldZOffset = localX * sinTheta + localY * cosTheta;

                    float currentWorldX = center.x + worldXOffset;
                    float currentWorldZ = center.y + worldZOffset;

                    // --- Convert World Corner to This Terrain's Hole Map Pixel Coordinates ---
                    float normalizedX_onTerrain = (currentWorldX - terrainMinX) / terrainTotalWidth;
                    float normalizedZ_onTerrain_fromMin = (currentWorldZ - terrainMinZ) / terrainTotalHeight;

                    float pixelU_float = normalizedX_onTerrain * holeMapResolution;
                    // V=0 corresponds to Max Z of terrain (+2000), where normalizedZ_onTerrain_fromMin = 1.0
                    float pixelV_float = (1.0f - normalizedZ_onTerrain_fromMin) * holeMapResolution;

                    // Clamp to ensure coordinates are within the texture dimensions (0 to holeMapResolution-1)
                    // Subtract a small epsilon before floor/round for the max value to avoid going over due to float precision.
                    float clampedU = Mathf.Clamp(pixelU_float, 0f, holeMapResolution - 0.001f);
                    float clampedV = Mathf.Clamp(pixelV_float, 0f, holeMapResolution - 0.001f);

                    pixelVertices[i] = new Vector3(Mathf.Floor(clampedU), Mathf.Floor(clampedV), 0f);

                    RLog.Msg($"  Corner {i}: World({currentWorldX:F2},{currentWorldZ:F2}) => NormOnTerrain({normalizedX_onTerrain:F4},{normalizedZ_onTerrain_fromMin:F4}) => PixelFloat({pixelU_float:F2},{pixelV_float:F2}) => PixelInt({pixelVertices[i].x},{pixelVertices[i].y})");
                }

                RLog.Msg($"Drawing Quad at Pixels: V0({pixelVertices[0].x},{pixelVertices[0].y}), V1({pixelVertices[1].x},{pixelVertices[1].y}), V2({pixelVertices[2].x},{pixelVertices[2].y}), V3({pixelVertices[3].x},{pixelVertices[3].y})");

                GL.Begin(GL.QUADS);
                GL.Vertex(pixelVertices[0]); // Corresponds to local Top-Left
                GL.Vertex(pixelVertices[1]); // Corresponds to local Top-Right
                GL.Vertex(pixelVertices[2]); // Corresponds to local Bottom-Right
                GL.Vertex(pixelVertices[3]); // Corresponds to local Bottom-Left
                GL.End();

                GL.PopMatrix();
            }

        }

    }
}
