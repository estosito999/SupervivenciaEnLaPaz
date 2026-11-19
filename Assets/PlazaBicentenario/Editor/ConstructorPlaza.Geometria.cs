using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SupervivenciaEnLaPaz.Editor
{
    internal sealed partial class ConstructorPlaza
    {
        private sealed class Geometria
        {
            private sealed class Superficie
            {
                internal readonly List<Vector3> vertices = new();
                internal readonly List<Vector2> uv = new();
                internal readonly List<int> indices = new();
            }

            private readonly Dictionary<Material, Superficie> superficies = new();

            internal void Caja(Vector3 centro, Vector3 tamano, Material material, float giro = 0f)
            {
                Vector3 h = tamano / 2f;
                Quaternion r = Quaternion.Euler(0, giro, 0);
                Vector3 a = centro + r * new Vector3(-h.x, -h.y, -h.z);
                Vector3 b = centro + r * new Vector3(-h.x, -h.y, h.z);
                Vector3 c = centro + r * new Vector3(h.x, -h.y, h.z);
                Vector3 d = centro + r * new Vector3(h.x, -h.y, -h.z);
                Vector3 e = a + Vector3.up * tamano.y;
                Vector3 f = b + Vector3.up * tamano.y;
                Vector3 g = c + Vector3.up * tamano.y;
                Vector3 j = d + Vector3.up * tamano.y;
                Cuadrado(material, a, e, j, d);
                Cuadrado(material, d, j, g, c);
                Cuadrado(material, c, g, f, b);
                Cuadrado(material, b, f, e, a);
                Cuadrado(material, e, f, g, j);
                Cuadrado(material, a, d, c, b);
            }

            internal void Tubo(Vector3 inicio, Vector3 fin, float radio, Material material, int lados = 8)
            {
                Cono(inicio, fin, radio, radio, material, lados);
            }

            internal void Cono(Vector3 inicio, Vector3 fin, float radioInicio, float radioFin, Material material, int lados = 8)
            {
                Vector3 eje = (fin - inicio).normalized;
                Vector3 u = Vector3.Cross(eje, Mathf.Abs(eje.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 v = Vector3.Cross(u, eje);
                for (int i = 0; i < lados; i++)
                {
                    float t = i * 2f * Mathf.PI / lados;
                    float siguiente = (i + 1) * 2f * Mathf.PI / lados;
                    Vector3 r0 = u * Mathf.Cos(t) + v * Mathf.Sin(t);
                    Vector3 r1 = u * Mathf.Cos(siguiente) + v * Mathf.Sin(siguiente);
                    Vector3 a = inicio + r0 * radioInicio;
                    Vector3 b = fin + r0 * radioFin;
                    Vector3 c = fin + r1 * radioFin;
                    Vector3 d = inicio + r1 * radioInicio;
                    Cuadrado(material, a, b, c, d);
                    Triangulo(material, inicio, a, d);
                    Triangulo(material, fin, c, b);
                }
            }

            internal void Copa(Vector3 centro, Vector3 radios, Material material, float giro, int lados = 9)
            {
                Vector3 Punto(int anillo, int avance)
                {
                    float t = avance * 2f * Mathf.PI / lados + giro;
                    float a = (anillo / 4f - 0.5f) * Mathf.PI;
                    float irregular = 1f + 0.14f * Mathf.Sin(t * 3 + anillo * 0.88f);
                    return centro + new Vector3(Mathf.Cos(t) * Mathf.Cos(a) * radios.x * irregular,
                        Mathf.Sin(a) * radios.y, Mathf.Sin(t) * Mathf.Cos(a) * radios.z * irregular);
                }

                for (int i = 0; i < lados; i++)
                {
                    Triangulo(material, centro - Vector3.up * radios.y, Punto(1, i), Punto(1, i + 1));
                    for (int y = 1; y < 3; y++)
                    {
                        Vector3 a = Punto(y, i);
                        Vector3 b = Punto(y + 1, i);
                        Vector3 c = Punto(y + 1, i + 1);
                        Vector3 d = Punto(y, i + 1);
                        Triangulo(material, a, b, c);
                        Triangulo(material, a, c, d);
                    }
                    Triangulo(material, Punto(3, i), centro + Vector3.up * radios.y, Punto(3, i + 1));
                }
            }

            internal void Cuadrado(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                float u = Vector3.Distance(a, d) / 3.2f;
                float v = Vector3.Distance(a, b) / 3.2f;
                Cuadrado(material, a, b, c, d, Vector2.zero, new Vector2(0, v), new Vector2(u, v), new Vector2(u, 0));
            }

            internal void Cuadrado(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
            {
                Superficie cara = Obtener(material);
                int i = cara.vertices.Count;
                cara.vertices.Add(a);
                cara.vertices.Add(b);
                cara.vertices.Add(c);
                cara.vertices.Add(d);
                cara.uv.Add(uv0);
                cara.uv.Add(uv1);
                cara.uv.Add(uv2);
                cara.uv.Add(uv3);
                cara.indices.Add(i);
                cara.indices.Add(i + 1);
                cara.indices.Add(i + 2);
                cara.indices.Add(i);
                cara.indices.Add(i + 2);
                cara.indices.Add(i + 3);
            }

            internal void Triangulo(Material material, Vector3 a, Vector3 b, Vector3 c)
            {
                Superficie cara = Obtener(material);
                int i = cara.vertices.Count;
                cara.vertices.Add(a);
                cara.vertices.Add(b);
                cara.vertices.Add(c);
                cara.uv.Add(Vector2.zero);
                cara.uv.Add(Vector2.right);
                cara.uv.Add(Vector2.up);
                cara.indices.Add(i);
                cara.indices.Add(i + 1);
                cara.indices.Add(i + 2);
            }

            internal (Mesh, Material[]) Exportar(string nombre)
            {
                var materiales = new Material[superficies.Count];
                var posiciones = new List<Vector3>();
                var coordenadas = new List<Vector2>();
                var bases = new int[superficies.Count];
                var indices = new List<int>[superficies.Count];
                int i = 0;
                foreach (var par in superficies)
                {
                    materiales[i] = par.Key;
                    bases[i] = posiciones.Count;
                    indices[i] = par.Value.indices;
                    posiciones.AddRange(par.Value.vertices);
                    coordenadas.AddRange(par.Value.uv);
                    i++;
                }

                var malla = new Mesh { name = nombre, subMeshCount = superficies.Count };
                if (posiciones.Count > ushort.MaxValue)
                {
                    malla.indexFormat = IndexFormat.UInt32;
                    malla.subMeshCount = superficies.Count;
                }
                malla.SetVertices(posiciones);
                malla.SetUVs(0, coordenadas);
                for (int submalla = 0; submalla < indices.Length; submalla++)
                    malla.SetTriangles(indices[submalla], submalla, false, bases[submalla]);
                malla.RecalculateNormals();
                malla.RecalculateBounds();
                return (malla, materiales);
            }

            private Superficie Obtener(Material material)
            {
                if (!superficies.TryGetValue(material, out Superficie cara))
                {
                    cara = new Superficie();
                    superficies.Add(material, cara);
                }
                return cara;
            }
        }
    }
}
