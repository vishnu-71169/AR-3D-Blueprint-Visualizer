using System.Collections.Generic;
using UnityEngine;

namespace ARBlueprint
{
    /// <summary>Frees the procedurally created meshes when a floor is destroyed.</summary>
    public class MeshOwner : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();

        public void Add(Mesh mesh)
        {
            meshes.Add(mesh);
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
        }
    }

    /// <summary>
    /// Builds axis-aligned boxes whose UVs are measured in model space, so a texture keeps the same
    /// physical scale on every box and runs continuously across neighbouring boxes of one material.
    /// </summary>
    public static class BoxBuilder
    {
        public static GameObject Box(Transform parent, MeshOwner owner, string name, Vector3 center, Vector3 size,
            Material material)
        {
            Mesh mesh = BuildMesh(center, size, MaterialLibrary.TileMeters(material));
            mesh.name = name;
            owner.Add(mesh);

            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 center, float diameter, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        private static Mesh BuildMesh(Vector3 center, Vector3 size, float tileMeters)
        {
            Vector3 h = size * 0.5f;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            void Face(Vector3 normal, Vector3 origin, Vector3 a, Vector3 b, int uAxis, int vAxis)
            {
                int start = vertices.Count;
                Vector3[] corners = { origin, origin + a, origin + a + b, origin + b };
                foreach (Vector3 corner in corners)
                {
                    vertices.Add(corner);
                    normals.Add(normal);
                    Vector3 world = corner + center;
                    uvs.Add(new Vector2(world[uAxis] / tileMeters, world[vAxis] / tileMeters));
                }

                if (Vector3.Dot(Vector3.Cross(a, b), normal) > 0f)
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                else
                    triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }

            Face(Vector3.right, new Vector3(h.x, -h.y, -h.z), new Vector3(0, 0, size.z), new Vector3(0, size.y, 0), 2, 1);
            Face(Vector3.left, new Vector3(-h.x, -h.y, -h.z), new Vector3(0, 0, size.z), new Vector3(0, size.y, 0), 2, 1);
            Face(Vector3.up, new Vector3(-h.x, h.y, -h.z), new Vector3(size.x, 0, 0), new Vector3(0, 0, size.z), 0, 2);
            Face(Vector3.down, new Vector3(-h.x, -h.y, -h.z), new Vector3(size.x, 0, 0), new Vector3(0, 0, size.z), 0, 2);
            Face(Vector3.forward, new Vector3(-h.x, -h.y, h.z), new Vector3(size.x, 0, 0), new Vector3(0, size.y, 0), 0, 1);
            Face(Vector3.back, new Vector3(-h.x, -h.y, -h.z), new Vector3(size.x, 0, 0), new Vector3(0, size.y, 0), 0, 1);

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
