using UnityEngine;

namespace SupervivenciaEnLaPaz.Editor
{
    internal sealed partial class ConstructorPlaza
    {
        private void Cafe(Transform padre)
        {
            Transform cafe = Grupo("Café Ciudad", padre, new Vector3(-17.05f, 0, -10.1f), -27f);
            var malla = new Geometria();
            malla.Caja(new(0, 1.59f, 0.45f), new(7.9f, 3.18f, 4.1f), materiales["Ladrillo"]);
            malla.Caja(new(0, 3.29f, 0.38f), new(8.35f, 0.23f, 4.66f), materiales["Toldo oscuro"]);
            malla.Caja(new(0, 2.78f, -1.68f), new(7.7f, 0.69f, 0.23f), Metal);
            malla.Caja(new(0, 2.44f, -1.81f), new(7.66f, 0.057f, 0.034f), materiales["Marco bronce"]);
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 1.41f;
                malla.Caja(new(x, 1.42f, -1.62f), new(1.34f, 1.62f, 0.13f), Metal);
                malla.Caja(new(x, 1.42f, -1.71f), new(1.23f, 1.46f, 0.071f), i == 2 ? Cristal : materiales["Cristal cálido"]);
                malla.Caja(new(x, 1.48f, -1.76f), new(0.035f, 1.5f, 0.039f), Metal);
                malla.Caja(new(x, 0.58f, -1.70f), new(1.34f, 0.10f, 0.30f), Piedra);
            }
            malla.Caja(new(4.00f, 1.56f, 0.29f), new(0.12f, 1.72f, 2.96f), Metal);
            malla.Caja(new(4.08f, 1.56f, 0.29f), new(0.09f, 1.52f, 2.74f), materiales["Cristal cálido"]);
            for (int i = -1; i <= 1; i++)
                malla.Caja(new(4.14f, 1.56f, 0.29f + i * 0.69f), new(0.08f, 1.56f, 0.058f), Metal);
            for (int i = 0; i < 14; i++)
            {
                float x = -4.10f + i * 0.585f;
                Material tela = materiales[i % 2 == 0 ? "Toldo rojo" : "Lona cruda"];
                malla.Cuadrado(tela, new(x, 2.11f, -2.82f), new(x, 2.49f, -1.82f),
                    new(x + 0.585f, 2.49f, -1.82f), new(x + 0.585f, 2.11f, -2.82f));
                malla.Caja(new(x + 0.2925f, 2.015f, -2.82f), new(0.585f, 0.19f, 0.043f), tela);
            }
            Guardar(malla, cafe);
            Colision(cafe, new(0, 1.59f, 0.45f), new(7.9f, 3.18f, 4.1f));
            Rotulo(cafe, "Letrero del Café Ciudad", "CAFÉ CIUDAD", new(0, 2.78f, -1.806f), 0.56f, ColorHex("FFF0D0"));
            Rotulo(cafe, "Letrero lateral", "CAFÉ CIUDAD", new(4.03f, 2.77f, 0.33f), 0.31f, ColorHex("FFF0D0"), -90f);

            for (int i = 0; i < 3; i++)
            {
                Transform mesa = Grupo($"Mesa exterior {i + 1}", cafe, new((i - 1) * 2.25f, 0, -3.27f));
                var muebles = new Geometria();
                muebles.Tubo(Vector3.zero, new(0, 0.71f, 0), 0.042f, Metal);
                muebles.Tubo(new(0, 0.02f, 0), new(0, 0.07f, 0), 0.24f, Metal);
                muebles.Tubo(new(0, 0.71f, 0), new(0, 0.77f, 0), 0.46f, Madera, 16);
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    float x = lado * 0.75f;
                    muebles.Caja(new(x, 0.225f, 0), new(0.032f, 0.45f, 0.31f), Metal);
                    muebles.Caja(new(x, 0.45f, 0), new(0.42f, 0.055f, 0.42f), Madera);
                    muebles.Caja(new(x + lado * 0.18f, 0.68f, 0), new(0.06f, 0.39f, 0.41f), Metal);
                }
                muebles.Tubo(new(0.1f, 0.77f, -0.07f), new(0.1f, 0.86f, -0.07f), 0.046f, materiales["Lona cruda"]);
                Guardar(muebles, mesa);
                Colision(mesa, new(0, 0.735f, 0), new(0.81f, 0.07f, 0.81f));
            }
        }

        private void Puesto(Transform padre, string nombre, Vector3 posicion, float giro, int semilla)
        {
            Transform puesto = Grupo(nombre, padre, posicion, giro);
            var malla = new Geometria();
            const float x = 2.10f;
            const float z = 1.71f;
            const float y = 2.44f;
            Vector3 a = new(-x, y, -z), b = new(x, y, -z), c = new(-x, y, z), d = new(x, y, z);
            Vector3 izquierda = new(-0.61f, 3.21f, 0.05f), derecha = new(0.61f, 3.21f, 0.05f);
            Material rojo = materiales["Toldo rojo"];
            Material sombra = materiales["Toldo oscuro"];
            malla.Cuadrado(materiales["Toldo iluminado"], a, izquierda, derecha, b);
            malla.Cuadrado(rojo, c, d, derecha, izquierda);
            malla.Triangulo(rojo, a, c, izquierda);
            malla.Triangulo(sombra, b, derecha, d);
            malla.Cuadrado(sombra, a, b, d, c);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                malla.Caja(new(0, y - 0.11f, lado * z), new(x * 2, 0.23f, 0.054f), rojo);
                malla.Caja(new(lado * x, y - 0.11f, 0), new(0.054f, 0.23f, z * 2), rojo);
                for (int lateral = -1; lateral <= 1; lateral += 2)
                {
                    Vector3 pie = new(lado * 1.96f, 0, lateral * 1.57f);
                    malla.Tubo(pie, pie + Vector3.up * y, 0.034f, Metal);
                    malla.Caja(pie + Vector3.up * 0.06f, new(0.20f, 0.12f, 0.20f), Oscura);
                    Colision(puesto, pie + Vector3.up * 1.22f, new(0.10f, 2.44f, 0.10f));
                }
            }
            malla.Caja(new(0, y - 0.23f, -z - 0.028f), new(x * 2, 0.026f, 0.026f), materiales["Lona cruda"]);
            malla.Tubo(a + Vector3.up * 0.02f, izquierda + Vector3.up * 0.02f, 0.017f, sombra);
            malla.Tubo(b + Vector3.up * 0.02f, derecha + Vector3.up * 0.02f, 0.017f, sombra);
            malla.Caja(new(0, 0.51f, -0.67f), new(3.42f, 0.93f, 0.78f), materiales["Lona verde"]);
            malla.Caja(new(0, 0.99f, -0.67f), new(3.53f, 0.12f, 0.89f), Madera);
            malla.Caja(new(0, 0.49f, 1.49f), new(3.83f, 0.93f, 0.07f), materiales["Lona verde"]);
            Colision(puesto, new(0, 0.537f, -0.67f), new(3.53f, 1.05f, 0.89f));
            var rng = new System.Random(semilla + 3309);
            Material[] mercaderia = { materiales["Lona cruda"], rojo, materiales["Roble claro"], materiales["Revoque azul"], materiales["Lona verde"] };
            for (int i = 0; i < 16; i++)
            {
                float alto = 0.043f + (float)rng.NextDouble() * 0.12f;
                malla.Caja(new(-1.47f + (i % 8) * 0.42f, 1.05f + alto / 2, -0.85f + (i / 8) * 0.36f),
                    new(0.27f, alto, 0.25f), mercaderia[rng.Next(mercaderia.Length)], rng.Next(-10, 11));
            }
            malla.Caja(new(1.55f, 0.20f, 0.87f), new(0.57f, 0.4f, 0.62f), Madera);
            Guardar(malla, puesto);
        }

        private void Farol(Transform padre, Vector3 posicion, float alto, float giro)
        {
            Transform grupo = Grupo("Farol urbano", padre, posicion, giro);
            var malla = new Geometria();
            malla.Tubo(Vector3.zero, new(0, 0.11f, 0), 0.17f, Oscura, 10);
            malla.Cono(new(0, 0.11f, 0), new(0, alto, 0), 0.075f, 0.037f, Metal, 10);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Vector3 a = new(0, alto * 0.86f, 0), b = new(lado * 0.52f, alto * 0.91f, 0), c = new(lado * 0.88f, alto * 0.9f, 0);
                malla.Tubo(a, b, 0.043f, Metal);
                malla.Tubo(b, c, 0.038f, Metal);
                malla.Tubo(new(0, alto, 0), b, 0.019f, Metal);
                malla.Copa(c, new(0.24f, 0.073f, 0.18f), Metal, lado);
                malla.Copa(c - Vector3.up * 0.045f, new(0.15f, 0.041f, 0.12f), materiales["Lona cruda"], lado);
            }
            Guardar(malla, grupo);
            Colision(grupo, new(0, alto / 2, 0), new(0.16f, alto, 0.16f));
        }

        private void Jardinera(Transform padre, Vector3 posicion, float ancho, float fondo, int semilla)
        {
            Transform grupo = Grupo("Jardinera", padre, posicion);
            var malla = new Geometria();
            malla.Caja(new(0, 0.22f, 0), new(ancho, 0.44f, fondo), materiales["Cantera tostada"]);
            malla.Caja(new(0, 0.436f, 0), new(ancho - 0.19f, 0.022f, fondo - 0.19f), materiales["Tierra"]);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                malla.Caja(new(0, 0.456f, lado * (fondo / 2 - 0.08f)), new(ancho + 0.03f, 0.09f, 0.18f), Piedra);
                malla.Caja(new(lado * (ancho / 2 - 0.08f), 0.456f, 0), new(0.18f, 0.09f, fondo - 0.34f), Piedra);
            }
            var rng = new System.Random(semilla);
            int arbustos = Mathf.CeilToInt(ancho * 3.8f);
            for (int i = 0; i < arbustos; i++)
            {
                float x = Mathf.Lerp(-ancho * 0.42f, ancho * 0.42f, (i + 0.5f) / arbustos);
                float z = Mathf.Lerp(-fondo * 0.29f, fondo * 0.29f, (float)rng.NextDouble());
                float tamano = Mathf.Lerp(0.23f, 0.42f, (float)rng.NextDouble());
                Material hoja = materiales[i % 3 == 0 ? "Follaje al sol" : "Follaje jade"];
                malla.Copa(new(x, 0.57f + tamano * 0.34f, z), new(tamano * 1.2f, tamano * 0.78f, tamano), hoja, i * 0.77f);
                if (i % 4 == 1)
                    malla.Copa(new(x, 0.66f + tamano * 1.08f, z), new(0.11f, 0.09f, 0.11f), materiales["Floral"], i);
            }
            Guardar(malla, grupo);
            Colision(grupo, new(0, 0.24f, 0), new(ancho, 0.48f, fondo));
            if (fondo <= 1.5f)
                return;

            Transform arbol = Grupo("Árbol de la plaza", grupo, new(ancho * 0.17f, 0.44f, 0.06f));
            arbol.localScale *= 1f + (semilla % 6) * 0.08f;
            var copa = new Geometria();
            Vector3 bifurcacion = new(0.15f, 1.99f, 0.06f);
            copa.Cono(Vector3.zero, bifurcacion, 0.12f, 0.063f, Madera);
            Material[] tonos = { materiales["Follaje al sol"], materiales["Follaje jade"], materiales["Follaje oscuro"] };
            for (int i = 0; i < 12; i++)
            {
                float a = i * 2.39996f;
                float radio = 0.25f + (float)rng.NextDouble() * 0.81f;
                Vector3 rama = new(Mathf.Sin(a) * radio, 2.64f + (float)rng.NextDouble() * 0.93f, Mathf.Cos(a) * radio);
                copa.Cono(bifurcacion, rama, 0.039f, 0.014f, Madera, 6);
                copa.Copa(rama, new(0.80f, 0.66f, 0.74f), tonos[i % tonos.Length], a, 11);
            }
            Guardar(copa, arbol);
        }

        private void Banco(Transform padre, Vector3 posicion, float ancho, float giro)
        {
            Transform grupo = Grupo("Banco", padre, posicion, giro);
            var malla = new Geometria();
            for (int i = 0; i < 4; i++)
                malla.Caja(new(0, 0.455f, -0.225f + i * 0.15f), new(ancho, 0.064f, 0.12f), Madera);
            for (int i = 0; i < 3; i++)
                malla.Caja(new(0, 0.622f + i * 0.105f, 0.26f), new(ancho, 0.081f, 0.072f), Madera);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                malla.Caja(new(lado * ancho * 0.38f, 0.225f, 0), new(0.154f, 0.45f, 0.43f), Metal);
                malla.Caja(new(lado * ancho * 0.38f, 0.656f, 0.277f), new(0.059f, 0.39f, 0.045f), Metal);
            }
            Guardar(malla, grupo);
            Colision(grupo, new(0, 0.455f, 0), new(ancho, 0.13f, 0.56f));
        }

        private void Baranda(Geometria malla, Vector3 a, Vector3 b, int postes)
        {
            for (int i = 0; i <= postes; i++)
            {
                Vector3 pie = Vector3.Lerp(a, b, i / (float)postes);
                malla.Tubo(pie, pie + Vector3.up * 0.92f, 0.027f, Metal);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 altura = Vector3.up * (0.20f + i * 0.36f);
                malla.Tubo(a + altura, b + altura, i == 2 ? 0.036f : 0.015f, Metal);
            }
        }
    }
}
