using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Trucardo.EditorScripts
{
    /// <summary>
    /// Repara huecos geométricos y pesos de skinning corruptos en el modelo del Bartender
    /// (hombros/sisas abiertas, esquinas flotantes con pesos negativos de cuello y cintura abierta)
    /// generando una malla fija compatible 1:1 con el rig Humanoid y animaciones existentes.
    /// </summary>
    public static class BartenderMeshRepair
    {
        private struct BoneLookup
        {
            public int hips;
            public int spine;
            public int spine1;
            public int spine2;
            public int neck;
            public int leftShoulder;
            public int leftArm;
            public int rightShoulder;
            public int rightArm;
        }

        private struct RimEdge
        {
            public int a;
            public int b;
            public Vector3Int ka;
            public Vector3Int kb;
        }

        public static Mesh BuildRepairedMeshAsset(Mesh sourceMesh, Transform[] bones, string assetPath)
        {
            if (sourceMesh == null || bones == null || bones.Length == 0)
            {
                return sourceMesh;
            }

            Mesh mesh = Object.Instantiate(sourceMesh);
            mesh.name = "Bartender_Fixed";

            List<Vector3> verts = new List<Vector3>(mesh.vertices);
            List<Vector3> normals = new List<Vector3>(mesh.normals);
            List<Vector2> uvs = new List<Vector2>(mesh.uv);
            List<BoneWeight> weights = new List<BoneWeight>(mesh.boneWeights);
            int[] srcTris = mesh.GetTriangles(0);
            List<int> tris = new List<int>(srcTris);

            // Asegurar listas válidas
            while (normals.Count < verts.Count) normals.Add(Vector3.up);
            while (uvs.Count < verts.Count) uvs.Add(Vector2.zero);
            while (weights.Count < verts.Count) weights.Add(new BoneWeight { boneIndex0 = 0, weight0 = 1f });

            BoneLookup lookup = ResolveBones(bones);

            // 1. Limpiar pesos negativos / >1 y agrupar vértices por posición exacta
            Dictionary<Vector3Int, List<int>> posGroups = BuildPositionGroups(verts);
            SanitizeAllWeights(weights);

            // 2. Suavizar esquinas altas de hombros (chaleco + piel) y transición deltoide-torso
            ApplyShoulderGeometryAndWeightFixes(verts, weights, posGroups, lookup);

            // 3. Reconstruir grupos tras mover vértices y detectar bucles de bordes abiertos (count == 1)
            posGroups = BuildPositionGroups(verts);
            List<List<RimEdge>> openLoops = FindOpenBoundaryLoops(verts, tris);

            foreach (List<RimEdge> loop in openLoops)
            {
                if (loop.Count < 3) continue;

                ComputeLoopBounds(verts, loop, out Vector3 min, out Vector3 max, out Vector3 centroid);
                float widthX = max.x - min.x;

                // Ignorar botas (y < 0.5) y detalles de cara/ojos (y > 1.85)
                if (centroid.y < 0.5f || min.y > 1.85f) continue;

                // a) Sisas del chaleco en hombros (+X y -X)
                if (Mathf.Abs(centroid.x) > 0.24f && centroid.y >= 1.35f && centroid.y <= 1.80f)
                {
                    int sign = centroid.x >= 0f ? 1 : -1;
                    CloseArmholeLoop(verts, normals, uvs, weights, tris, loop, sign, lookup);
                    continue;
                }

                // b) Cintura / faldón abierto en la base del chaleco/camisa
                if (centroid.y >= 0.96f && centroid.y <= 1.09f && widthX > 0.35f)
                {
                    CloseWaistLoop(verts, normals, uvs, weights, tris, posGroups, loop, lookup);
                    continue;
                }

                // c) Cuello / escote superior del chaleco
                if (Mathf.Abs(centroid.x) <= 0.15f && min.y >= 1.55f && max.y <= 1.85f && widthX > 0.20f)
                {
                    CloseCollarLoop(verts, normals, uvs, weights, tris, loop, lookup);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.boneWeights = weights.ToArray();
            mesh.subMeshCount = 1;
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();

            Mesh existingAsset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existingAsset != null)
            {
                EditorUtility.CopySerialized(mesh, existingAsset);
                EditorUtility.SetDirty(existingAsset);
                Object.DestroyImmediate(mesh);
                return existingAsset;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, assetPath);
                EditorUtility.SetDirty(mesh);
                return mesh;
            }
        }

        private static BoneLookup ResolveBones(Transform[] bones)
        {
            BoneLookup b = new BoneLookup
            {
                hips = -1, spine = -1, spine1 = -1, spine2 = -1, neck = -1,
                leftShoulder = -1, leftArm = -1, rightShoulder = -1, rightArm = -1
            };

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                string n = bones[i].name;
                if (n.EndsWith("Hips") || n == "Hips") b.hips = i;
                else if (n.EndsWith("Spine2") || n == "Spine2") b.spine2 = i;
                else if (n.EndsWith("Spine1") || n == "Spine1") b.spine1 = i;
                else if (n.EndsWith("Spine") || n == "Spine") b.spine = i;
                else if (n.EndsWith("Neck") || n == "Neck") b.neck = i;
                else if (n.EndsWith("LeftShoulder") || n == "LeftShoulder") b.leftShoulder = i;
                else if (n.EndsWith("LeftArm") || n == "LeftArm") b.leftArm = i;
                else if (n.EndsWith("RightShoulder") || n == "RightShoulder") b.rightShoulder = i;
                else if (n.EndsWith("RightArm") || n == "RightArm") b.rightArm = i;
            }
            return b;
        }

        private static Vector3Int Quantize(Vector3 v)
        {
            return new Vector3Int(
                Mathf.RoundToInt(v.x * 1000f),
                Mathf.RoundToInt(v.y * 1000f),
                Mathf.RoundToInt(v.z * 1000f));
        }

        private static Dictionary<Vector3Int, List<int>> BuildPositionGroups(List<Vector3> verts)
        {
            Dictionary<Vector3Int, List<int>> groups = new Dictionary<Vector3Int, List<int>>(verts.Count);
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3Int k = Quantize(verts[i]);
                if (!groups.TryGetValue(k, out List<int> list))
                {
                    list = new List<int>(2);
                    groups[k] = list;
                }
                list.Add(i);
            }
            return groups;
        }

        private static void SanitizeAllWeights(List<BoneWeight> weights)
        {
            for (int i = 0; i < weights.Count; i++)
            {
                weights[i] = NormalizeWeight(
                    weights[i].boneIndex0, Mathf.Max(0f, weights[i].weight0),
                    weights[i].boneIndex1, Mathf.Max(0f, weights[i].weight1),
                    weights[i].boneIndex2, Mathf.Max(0f, weights[i].weight2),
                    weights[i].boneIndex3, Mathf.Max(0f, weights[i].weight3));
            }
        }

        private static void ApplyShoulderGeometryAndWeightFixes(
            List<Vector3> verts,
            List<BoneWeight> weights,
            Dictionary<Vector3Int, List<int>> posGroups,
            BoneLookup lookup)
        {
            foreach (var kvp in posGroups)
            {
                List<int> indices = kvp.Value;
                Vector3 p = verts[indices[0]];
                float absX = Mathf.Abs(p.x);
                bool isRight = p.x >= 0f;
                int shoulderBone = isRight ? lookup.rightShoulder : lookup.leftShoulder;
                int armBone = isRight ? lookup.rightArm : lookup.leftArm;

                // Quitar picos rectangulares altos de las esquinas de hombro (chaleco/piel) y darles peso de torso-hombro
                if (absX >= 0.255f && absX <= 0.365f && p.y >= 1.725f && p.y <= 1.825f)
                {
                    float clampedY = Mathf.Min(p.y, 1.735f);
                    float sign = isRight ? 1f : -1f;
                    float clampedX = p.y > 1.75f ? sign * Mathf.Min(absX, 0.31f) : p.x;
                    Vector3 newPos = new Vector3(clampedX, clampedY, p.z);

                    BoneWeight bw = (lookup.spine2 >= 0 && shoulderBone >= 0 && lookup.neck >= 0)
                        ? NormalizeWeight(lookup.spine2, 0.65f, shoulderBone, 0.25f, lookup.neck, 0.10f, 0, 0f)
                        : weights[indices[0]];

                    foreach (int idx in indices)
                    {
                        verts[idx] = newPos;
                        weights[idx] = bw;
                    }
                    continue;
                }

                // Suavizar transición del borde interior de sisa (0.27 <= |X| <= 0.37)
                if (absX >= 0.27f && absX <= 0.37f && p.y >= 1.44f && p.y <= 1.735f && shoulderBone >= 0 && lookup.spine2 >= 0)
                {
                    float spine1W = p.y < 1.54f && lookup.spine1 >= 0 ? 0.35f : 0f;
                    float spine2W = 0.80f - spine1W;
                    BoneWeight bw = NormalizeWeight(lookup.spine2, spine2W, shoulderBone, 0.20f, Mathf.Max(0, lookup.spine1), spine1W, 0, 0f);
                    foreach (int idx in indices) weights[idx] = bw;
                    continue;
                }

                // Suavizar anillo superior del brazo/deltoide (0.38 <= |X| <= 0.48) para que no se hunda al bajar el brazo
                if (absX >= 0.38f && absX <= 0.48f && p.y >= 1.44f && p.y <= 1.74f && shoulderBone >= 0 && armBone >= 0)
                {
                    float tTop = Mathf.InverseLerp(1.46f, 1.70f, p.y);
                    float shoulderW = Mathf.Lerp(0.18f, 0.42f, tTop);
                    float spine2W = Mathf.Lerp(0.05f, 0.15f, tTop);
                    float armW = Mathf.Max(0.1f, 1f - shoulderW - spine2W);
                    BoneWeight bw = lookup.spine2 >= 0
                        ? NormalizeWeight(armBone, armW, shoulderBone, shoulderW, lookup.spine2, spine2W, 0, 0f)
                        : NormalizeWeight(armBone, armW + spine2W, shoulderBone, shoulderW, 0, 0f, 0, 0f);
                    foreach (int idx in indices) weights[idx] = bw;
                }
            }
        }

        private static List<List<RimEdge>> FindOpenBoundaryLoops(List<Vector3> verts, List<int> tris)
        {
            Dictionary<(Vector3Int, Vector3Int), int> edgeCounts = new Dictionary<(Vector3Int, Vector3Int), int>();
            List<RimEdge> allDirected = new List<RimEdge>(tris.Count);

            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i + 1];
                int i2 = tris[i + 2];

                Vector3Int k0 = Quantize(verts[i0]);
                Vector3Int k1 = Quantize(verts[i1]);
                Vector3Int k2 = Quantize(verts[i2]);

                if (k0 == k1 || k1 == k2 || k2 == k0) continue;

                AddEdge(i0, i1, k0, k1, edgeCounts, allDirected);
                AddEdge(i1, i2, k1, k2, edgeCounts, allDirected);
                AddEdge(i2, i0, k2, k0, edgeCounts, allDirected);
            }

            Dictionary<Vector3Int, List<RimEdge>> byStart = new Dictionary<Vector3Int, List<RimEdge>>();
            List<RimEdge> boundaryEdges = new List<RimEdge>();

            foreach (RimEdge e in allDirected)
            {
                var uKey = MakeUndirectedKey(e.ka, e.kb);
                if (edgeCounts.TryGetValue(uKey, out int cnt) && cnt == 1)
                {
                    boundaryEdges.Add(e);
                    if (!byStart.TryGetValue(e.ka, out List<RimEdge> list))
                    {
                        list = new List<RimEdge>(2);
                        byStart[e.ka] = list;
                    }
                    list.Add(e);
                }
            }

            HashSet<(Vector3Int, Vector3Int)> used = new HashSet<(Vector3Int, Vector3Int)>();
            List<List<RimEdge>> loops = new List<List<RimEdge>>();

            foreach (RimEdge seed in boundaryEdges)
            {
                var seedKey = MakeUndirectedKey(seed.ka, seed.kb);
                if (used.Contains(seedKey)) continue;

                List<RimEdge> chain = new List<RimEdge>();
                RimEdge cur = seed;
                int guard = 0;
                while (guard++ < 128)
                {
                    var uKey = MakeUndirectedKey(cur.ka, cur.kb);
                    if (!used.Add(uKey)) break;
                    chain.Add(cur);

                    if (cur.kb == seed.ka) break;

                    bool foundNext = false;
                    if (byStart.TryGetValue(cur.kb, out List<RimEdge> candidates))
                    {
                        foreach (RimEdge cand in candidates)
                        {
                            var candKey = MakeUndirectedKey(cand.ka, cand.kb);
                            if (!used.Contains(candKey))
                            {
                                cur = cand;
                                foundNext = true;
                                break;
                            }
                        }
                    }
                    if (!foundNext) break;
                }

                if (chain.Count >= 3)
                {
                    loops.Add(chain);
                }
            }

            return loops;
        }

        private static void AddEdge(
            int a, int b, Vector3Int ka, Vector3Int kb,
            Dictionary<(Vector3Int, Vector3Int), int> edgeCounts,
            List<RimEdge> directed)
        {
            var uKey = MakeUndirectedKey(ka, kb);
            edgeCounts.TryGetValue(uKey, out int c);
            edgeCounts[uKey] = c + 1;
            directed.Add(new RimEdge { a = a, b = b, ka = ka, kb = kb });
        }

        private static (Vector3Int, Vector3Int) MakeUndirectedKey(Vector3Int a, Vector3Int b)
        {
            if (a.x != b.x) return a.x < b.x ? (a, b) : (b, a);
            if (a.y != b.y) return a.y < b.y ? (a, b) : (b, a);
            return a.z <= b.z ? (a, b) : (b, a);
        }

        private static void ComputeLoopBounds(List<Vector3> verts, List<RimEdge> loop, out Vector3 min, out Vector3 max, out Vector3 centroid)
        {
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 sum = Vector3.zero;
            foreach (RimEdge e in loop)
            {
                Vector3 p = verts[e.a];
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
                sum += p;
            }
            centroid = sum / loop.Count;
        }

        private static void CloseArmholeLoop(
            List<Vector3> verts,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            List<RimEdge> loop,
            int sign,
            BoneLookup lookup)
        {
            int shoulderBone = sign >= 0 ? lookup.rightShoulder : lookup.leftShoulder;
            int armBone = sign >= 0 ? lookup.rightArm : lookup.leftArm;

            BoneWeight cuffWeight = (lookup.spine2 >= 0 && shoulderBone >= 0 && armBone >= 0)
                ? NormalizeWeight(lookup.spine2, 0.35f, shoulderBone, 0.40f, armBone, 0.25f, 0, 0f)
                : weights[loop[0].a];

            BoneWeight capWeight = (shoulderBone >= 0 && armBone >= 0)
                ? NormalizeWeight(shoulderBone, 0.55f, armBone, 0.45f, lookup.spine2 >= 0 ? lookup.spine2 : 0, 0f, 0, 0f)
                : cuffWeight;

            Vector3 armCenter = new Vector3(sign * 0.435f, 1.565f, -0.085f);
            ExtrudeAndCapLoop(
                verts, normals, uvs, weights, tris, loop,
                p => new Vector3(armCenter.x, Mathf.Lerp(p.y, armCenter.y, 0.28f), Mathf.Lerp(p.z, armCenter.z, 0.24f)),
                armCenter, cuffWeight, capWeight);
        }

        private static void CloseWaistLoop(
            List<Vector3> verts,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            Dictionary<Vector3Int, List<int>> posGroups,
            List<RimEdge> loop,
            BoneLookup lookup)
        {
            BoneWeight rimWeight = (lookup.hips >= 0 && lookup.spine >= 0)
                ? NormalizeWeight(lookup.hips, 0.65f, lookup.spine, 0.35f, 0, 0f, 0, 0f)
                : weights[loop[0].a];

            BoneWeight innerWeight = (lookup.hips >= 0 && lookup.spine >= 0)
                ? NormalizeWeight(lookup.hips, 0.72f, lookup.spine, 0.28f, 0, 0f, 0, 0f)
                : rimWeight;

            foreach (RimEdge e in loop)
            {
                foreach (int vIdx in new[] { e.a, e.b })
                {
                    Vector3Int k = Quantize(verts[vIdx]);
                    if (!posGroups.TryGetValue(k, out List<int> samePos)) continue;
                    Vector3 p = verts[vIdx];
                    Vector3 np = new Vector3(Mathf.Abs(p.x) > 0.26f ? p.x * 0.93f : p.x, Mathf.Min(p.y, 1.018f), p.z);
                    foreach (int gi in samePos)
                    {
                        verts[gi] = np;
                        weights[gi] = rimWeight;
                    }
                }
            }

            Vector3 waistCore = new Vector3(0f, 0.985f, 0.02f);
            ExtrudeAndCapLoop(
                verts, normals, uvs, weights, tris, loop,
                p => new Vector3(p.x * 0.68f, p.y - 0.03f, waistCore.z + (p.z - waistCore.z) * 0.68f),
                waistCore, innerWeight, innerWeight);
        }

        private static void CloseCollarLoop(
            List<Vector3> verts,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            List<RimEdge> loop,
            BoneLookup lookup)
        {
            BoneWeight collarWeight = (lookup.spine2 >= 0 && lookup.neck >= 0)
                ? NormalizeWeight(lookup.spine2, 0.65f, lookup.neck, 0.35f, 0, 0f, 0, 0f)
                : weights[loop[0].a];

            Vector3 neckCore = new Vector3(0f, 1.69f, 0f);
            ExtrudeAndCapLoop(
                verts, normals, uvs, weights, tris, loop,
                p => Vector3.Lerp(p, neckCore, 0.50f),
                neckCore, collarWeight, collarWeight);
        }

        private static void ExtrudeAndCapLoop(
            List<Vector3> verts,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            List<int> tris,
            List<RimEdge> loop,
            System.Func<Vector3, Vector3> projectRim,
            Vector3 capCenter,
            BoneWeight ringWeight,
            BoneWeight centerWeight)
        {
            List<int> ringA = new List<int>(loop.Count);
            List<int> ringB = new List<int>(loop.Count);
            Vector2 uvSum = Vector2.zero;

            foreach (RimEdge e in loop)
            {
                int newA = AddVertex(verts, normals, uvs, weights, projectRim(verts[e.a]), uvs[e.a], ringWeight);
                int newB = AddVertex(verts, normals, uvs, weights, projectRim(verts[e.b]), uvs[e.b], ringWeight);
                ringA.Add(newA);
                ringB.Add(newB);
                uvSum += uvs[e.a];

                AddOutwardQuad(verts, normals, tris, e.b, e.a, newA, newB);
            }

            int centerIdx = AddVertex(verts, normals, uvs, weights, capCenter, uvSum / loop.Count, centerWeight);
            for (int i = 0; i < loop.Count; i++)
            {
                AddOutwardTriangle(verts, normals, tris, ringB[i], ringA[i], centerIdx);
            }
        }

        private static int AddVertex(
            List<Vector3> verts,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<BoneWeight> weights,
            Vector3 pos,
            Vector2 uv,
            BoneWeight weight)
        {
            int idx = verts.Count;
            verts.Add(pos);
            normals.Add(Vector3.up);
            uvs.Add(uv);
            weights.Add(weight);
            return idx;
        }

        private static void AddOutwardQuad(List<Vector3> verts, List<Vector3> normals, List<int> tris, int b, int a, int newA, int newB)
        {
            AddOutwardTriangle(verts, normals, tris, b, a, newA);
            AddOutwardTriangle(verts, normals, tris, b, newA, newB);
        }

        private static void AddOutwardTriangle(List<Vector3> verts, List<Vector3> normals, List<int> tris, int i0, int i1, int i2)
        {
            Vector3 p0 = verts[i0];
            Vector3 p1 = verts[i1];
            Vector3 p2 = verts[i2];
            Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            if (n.sqrMagnitude < 1e-10f) return;

            n.Normalize();
            tris.Add(i0);
            tris.Add(i1);
            tris.Add(i2);
            normals[i0] = n;
            normals[i1] = n;
            normals[i2] = n;
        }

        private static BoneWeight NormalizeWeight(
            int b0, float w0,
            int b1, float w1,
            int b2, float w2,
            int b3, float w3)
        {
            w0 = Mathf.Max(0f, w0);
            w1 = Mathf.Max(0f, w1);
            w2 = Mathf.Max(0f, w2);
            w3 = Mathf.Max(0f, w3);
            float sum = w0 + w1 + w2 + w3;
            if (sum <= 1e-6f)
            {
                return new BoneWeight { boneIndex0 = Mathf.Max(0, b0), weight0 = 1f };
            }
            float inv = 1f / sum;
            return new BoneWeight
            {
                boneIndex0 = b0,
                weight0 = w0 * inv,
                boneIndex1 = b1,
                weight1 = w1 * inv,
                boneIndex2 = b2,
                weight2 = w2 * inv,
                boneIndex3 = b3,
                weight3 = w3 * inv
            };
        }
    }
}
