using UnityEditor;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Editor
{
    internal sealed partial class ConstructorPlaza
    {
        private void CrearMonoblock()
        {
            Transform umsa = Grupo("Monoblock Central · Universidad Mayor de San Andrés", null, new(0, 0.90f, 19.3f));
            Transform atrio = Grupo("Frontis y acceso principal", umsa);
            var baseEdificio = new Geometria();
            Material cantera = materiales["Cantera tostada"];
            Material bronce = materiales["Marco bronce"];
            Material barro = materiales["Arcilla principal"];

            baseEdificio.Caja(new(0, 3.42f, 5.6f), new(23.0f, 6.84f, 11.2f), cantera);
            baseEdificio.Caja(new(0, 6.94f, 5.18f), new(23.55f, 0.25f, 12.25f), Piedra);
            baseEdificio.Caja(new(0, 3.08f, -0.043f), new(17.25f, 5.57f, 0.18f), Oscura);
            baseEdificio.Caja(new(0, 3.81f, -0.17f), new(16.67f, 4.07f, 0.09f), Cristal);
            baseEdificio.Caja(new(0, 5.24f, -0.255f), new(16.5f, 0.66f, 0.102f), Metal);
            baseEdificio.Caja(new(0, 2.32f, -0.286f), new(16.74f, 0.079f, 0.16f), bronce);
            for (int panel = -5; panel <= 5; panel++)
            {
                float x = panel * 1.40f;
                baseEdificio.Caja(new(x, 1.17f, -0.20f), new(1.27f, 2.26f, 0.16f), bronce);
                baseEdificio.Caja(new(x, 1.22f, -0.297f), new(1.095f, 1.91f, 0.052f), Cristal);
                baseEdificio.Caja(new(x + 0.19f, 1.12f, -0.34f), new(0.024f, 0.33f, 0.03f), materiales["Cristal cálido"]);
                baseEdificio.Caja(new(x + 0.70f, 3.72f, -0.245f), new(0.060f, 2.61f, 0.12f), bronce);
            }
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float x = lado * 22.15f;
                baseEdificio.Caja(new(x, 2.85f, 6.15f), new(21.75f, 5.70f, 12.3f), cantera);
                baseEdificio.Caja(new(x, 5.71f, 6.07f), new(22.00f, 0.20f, 12.6f), Piedra);
                baseEdificio.Caja(new(lado * 9.1f, 2.89f, -0.28f), new(1.24f, 5.79f, 0.68f), Piedra);
                baseEdificio.Caja(new(lado * 9.11f, 5.81f, -0.28f), new(1.70f, 0.35f, 0.82f), cantera);
                for (int i = 0; i < 7; i++)
                    Ventana(baseEdificio, new(lado * (12.62f + i * 2.78f), 3.88f, -0.015f), 1.8f, 1.04f, i % 5);
                Colision(atrio, new(x, 2.85f, 6.15f), new(21.75f, 5.70f, 12.3f));
            }
            for (int i = -8; i <= 8; i++)
            {
                float y = i % 2 == 0 ? 4.37f : 4.17f;
                baseEdificio.Caja(new(i * 0.86f, y, -0.30f), new(0.56f, 0.40f, 0.14f), bronce);
                baseEdificio.Caja(new(i * 0.86f, y, -0.381f), new(0.31f, 0.22f, 0.035f), materiales["Arcilla en sombra"]);
            }
            Guardar(baseEdificio, atrio);
            Colision(atrio, new(0, 3.42f, 5.6f), new(23.0f, 6.84f, 11.2f));
            Rotulo(atrio, "Identificación de la UMSA", "UNIVERSIDAD MAYOR DE SAN ANDRÉS", new(0, 5.23f, -0.318f), 0.48f, ColorHex("F0DFC1"));

            Transform torre = Grupo("Torre y coronación", umsa, new(0, 0, 3.6f));
            var elevacion = new Geometria();
            elevacion.Caja(new(0, 24.16f, 7.15f), new(20.8f, 34.58f, 14.10f), barro);
            elevacion.Caja(new(0, 25.95f, 6.88f), new(13.38f, 38.16f, 14.03f), materiales["Arcilla al sol"]);
            elevacion.Caja(new(0, 44.82f, 6.9f), new(13.93f, 0.40f, 14.22f), barro);
            elevacion.Caja(new(0, 44.93f, 9.22f), new(3.9f, 0.38f, 4.0f), Oscura);
            elevacion.Tubo(new(0.12f, 45.0f, 9.35f), new(0.12f, 47.33f, 9.35f), 0.031f, Metal);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                elevacion.Caja(new(lado * 8.53f, 41.51f, 7.18f), new(3.75f, 0.30f, 14.18f), materiales["Arcilla en sombra"]);
                elevacion.Caja(new(lado * 6.81f, 24.42f, -0.075f), new(0.35f, 34.9f, 0.34f), barro);
            }
            for (int nivel = 0; nivel < 13; nivel++)
            {
                float y = 9.02f + nivel * 2.69f;
                elevacion.Caja(new(0, y - 0.80f, -0.172f), new(13.38f, 0.17f, 0.055f), barro);
                for (int i = -2; i <= 2; i++)
                    Ventana(elevacion, new(i * 2.25f, y, -0.18f), 1.55f, 1.22f, (i + nivel + 5) % 5);
                if (nivel == 12)
                    continue;
                Ventana(elevacion, new(-8.56f, y, 0.08f), 1.72f, 1.24f, nivel % 5);
                Ventana(elevacion, new(8.56f, y, 0.08f), 1.72f, 1.24f, (nivel + 2) % 5);
                for (int i = 0; i < 4; i++)
                {
                    float z = 1.75f + i * 3.55f;
                    Ventana(elevacion, new(10.41f, y, z), 1.65f, 1.17f, (nivel + i) % 5, -90);
                    Ventana(elevacion, new(-10.41f, y, z), 1.65f, 1.17f, (nivel + i + 1) % 5, 90);
                }
            }
            Guardar(elevacion, torre);
            Colision(torre, new(0, 24.16f, 7.15f), new(20.8f, 34.58f, 14.1f));
        }

        private void Ventana(Geometria malla, Vector3 centro, float ancho, float alto, int variante, float giro = 0)
        {
            Quaternion r = Quaternion.Euler(0, giro, 0);
            Material cristal = variante switch
            {
                1 => materiales["Cristal azul"],
                4 => materiales["Cristal cálido"],
                _ => Cristal,
            };
            Material marco = materiales["Marco bronce"];
            malla.Caja(centro, new(ancho, alto, 0.051f), marco, giro);
            malla.Caja(centro + r * new Vector3(0, 0, -0.040f), new(ancho - 0.13f, alto - 0.10f, 0.038f), cristal, giro);
            malla.Caja(centro + r * new Vector3(-0.14f, 0, -0.065f), new(0.058f, alto - 0.10f, 0.033f), marco, giro);
            malla.Caja(centro + r * new Vector3(0, -alto * 0.5f, -0.072f), new(ancho + 0.12f, 0.080f, 0.19f), Piedra, giro);
        }

        private void CrearEntorno()
        {
            Transform grupo = Grupo("Entorno de la avenida Villazón");
            Transform contexto = Grupo("Aceras y calzada lateral", grupo);
            Colision(contexto, new(0, -0.91f, 65), new(405, 1.25f, 231));
            var panorama = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Panorama/PanoramaVillazon.obj");
            PrefabUtility.InstantiatePrefab(panorama);

            Transform laterales = Grupo("Edificios de los laterales", grupo);
            Edificio(laterales, "Edificio claro", -48.8f, 30, 13.5f, 12.1f, 11, -5, "Revoque claro", true);
            Edificio(laterales, "Edificio terracota", -44.3f, 48, 14.6f, 11.5f, 9, -8, "Ladrillo", true);
            Edificio(laterales, "Edificio de balcones", -32.4f, 48, 12f, 10.8f, 8, 6, "Revoque claro", true);
            Edificio(laterales, "Torre azul lateral", -23.9f, 62.1f, 9.6f, 12.8f, 10, -3, "Revoque azul", false);
            Edificio(laterales, "Bloque bajo izquierdo", -38.6f, 23, 11.4f, 9.4f, 3, -2, "Ladrillo", true);
            Edificio(laterales, "Bloque del costado", 37.9f, 28.4f, 12.2f, 12.2f, 4, 4, "Arcilla principal", false);
            Edificio(laterales, "Torre verde", 49.5f, 45.7f, 11.5f, 10.4f, 9, 7, "Revoque verde", true);
            Edificio(laterales, "Edificio azul del fondo", 39.1f, 59.7f, 10, 10.9f, 9, 0, "Revoque azul", false);
            Edificio(laterales, "Edificio del extremo derecho", 64.2f, 58.7f, 14.2f, 12.2f, 8, -6, "Revoque claro", true);
            Edificio(laterales, "Bloque escalonado", 53.7f, 71.9f, 12.4f, 13.1f, 7, 5, "Ladrillo", false);

            var rng = new System.Random(4891);
            var casas = new Geometria();
            string[] acabados = { "Ladrillo", "Arcilla principal", "Arcilla al sol", "Revoque claro", "Revoque verde" };
            for (int fila = 0; fila < 6; fila++)
            for (int columna = -13; columna <= 13; columna++)
            {
                float z = 91 + fila * 9f + rng.Next(-4, 4);
                float x = columna * (8.1f + fila * 1.06f) + rng.Next(-2, 3);
                float baseY = fila * 1.55f - 1.1f;
                float alto = rng.Next(5, 12);
                float ancho = rng.Next(4, 8);
                casas.Caja(new(x, baseY + alto / 2f, z), new(ancho, alto, rng.Next(4, 8)), materiales[acabados[rng.Next(5)]]);
            }
            Guardar(casas, Grupo("Zona urbana en la ladera", grupo), false);
            Ladera(grupo, "Cuesta bajo la ciudad", 81, 166, 184, 17.7f, 0.74f, materiales["Ladera urbana"]);
            Ladera(grupo, "Cerro de fondo", 165, 266, 351, 39.5f, 1.38f, MaterialLit("Cerro cercano", "738A9D", 0.10f));
            Ladera(grupo, "Cordillera en bruma", 283, 472, 593, 69.2f, 2.84f, MaterialLit("Cordillera en bruma", "8C9DB5", 0.11f));
        }

        private void Edificio(Transform padre, string nombre, float x, float z, float ancho, float fondo, int pisos, float giro, string acabado, bool balcones)
        {
            Transform edificio = Grupo(nombre, padre, new(x, -0.28f, z), giro);
            var malla = new Geometria();
            float alto = pisos * 2.66f;
            Material muro = materiales[acabado];
            malla.Caja(new(0, alto / 2, 0), new(ancho, alto, fondo), muro);
            malla.Caja(new(0, alto + 0.16f, 0), new(ancho + 0.35f, 0.32f, fondo + 0.35f), Piedra);
            malla.Caja(new(-ancho * 0.20f, alto + 0.96f, fondo * 0.25f), new(ancho * 0.37f, 1.74f, fondo * 0.31f), muro);
            int modulos = Mathf.RoundToInt(ancho / 2.15f);
            float modulo = ancho / modulos;
            for (int piso = 0; piso < pisos; piso++)
            {
                float y = piso * 2.66f + 1.48f;
                for (int i = 0; i < modulos; i++)
                    Ventana(malla, new((i + 0.5f) * modulo - ancho / 2, y, -fondo / 2), modulo * 0.72f, 1.21f, (i + 3 * piso) % 5);
                for (int i = 0; i < 4; i++)
                {
                    float lateral = -fondo / 2 + (i + 0.5f) * fondo / 4;
                    Ventana(malla, new(ancho / 2, y, lateral), 1.39f, 1.15f, (i + piso) % 5, -90f);
                    Ventana(malla, new(-ancho / 2, y, lateral), 1.39f, 1.15f, (2 * i + piso) % 5, 90f);
                }
                if (!balcones || piso == 0)
                    continue;
                malla.Caja(new(0, y - 0.82f, -fondo / 2 - 0.37f), new(ancho * 0.88f, 0.18f, 0.85f), Piedra);
                malla.Caja(new(0, y - 0.39f, -fondo / 2 - 0.74f), new(ancho * 0.88f, 0.075f, 0.078f), Metal);
                for (int i = 0; i <= modulos; i++)
                    malla.Caja(new(-ancho * 0.44f + i * ancho * 0.88f / modulos, y - 0.63f, -fondo / 2 - 0.74f), new(0.053f, 0.49f, 0.053f), Metal);
            }
            Guardar(malla, edificio);
        }

        private void Ladera(Transform padre, string nombre, float cerca, float lejos, float ancho, float altura, float semilla, Material material)
        {
            var malla = new Geometria();
            const int columnas = 46, filas = 8;
            var cota = new Vector3[filas + 1, columnas + 1];
            for (int f = 0; f <= filas; f++)
            for (int c = 0; c <= columnas; c++)
            {
                float u = c / (float)columnas;
                float t = f / (float)filas;
                float x = (u - 0.5f) * ancho * 2;
                float z = Mathf.Lerp(cerca, lejos, t) + 9f * t * Mathf.Sin(u * 13.7f + semilla);
                float perfil = 0.7f + 0.43f * Mathf.Sin(u * 17.3f + semilla)
                    + 0.25f * Mathf.Sin(u * 38.5f + 2 * semilla) + 0.15f * Mathf.Sin(u * 99.4f);
                float y = Mathf.Lerp(-0.31f, Mathf.Max(perfil, 0.17f) * altura, t);
                cota[f, c] = new(x, y, z);
            }
            for (int f = 0; f < filas; f++)
            for (int c = 0; c < columnas; c++)
            {
                malla.Triangulo(material, cota[f, c], cota[f + 1, c], cota[f + 1, c + 1]);
                malla.Triangulo(material, cota[f, c], cota[f + 1, c + 1], cota[f, c + 1]);
            }
            Guardar(malla, Grupo(nombre, padre), false);
        }
    }
}
