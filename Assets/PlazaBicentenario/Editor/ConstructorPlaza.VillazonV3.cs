using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SupervivenciaEnLaPaz.Editor
{
    // Entorno exterior aproximado por referencias fotográficas. No llama a Generar().
    internal sealed partial class ConstructorPlaza
    {
        private const string VillazonV3Nombre = "Villazon y JJ Perez - reconstruccion V3";
        private const string VillazonV3Ruta = "Assets/PlazaBicentenario/Geometria/VillazonV3";

        [MenuItem("Supervivencia/Villazon V3/Generar entorno fotografico")]
        private static void GenerarVillazonV3()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene escena = SceneManager.GetActiveScene();
            GameObject mono = null, antiguo = null, anterior = null, osm = null;
            foreach (var root in escena.GetRootGameObjects())
            {
                if (root.name == "Monoblock Central · Universidad Mayor de San Andrés") mono = root;
                if (root.name == "Entorno de la avenida Villazón") antiguo = root;
                if (root.name == VillazonV3Nombre) anterior = root;
                if (root.name == "Entorno cartografico OSM - vista previa") osm = root;
            }
            if (mono == null || antiguo == null)
            {
                EditorUtility.DisplayDialog("Villazon V3", "Abre la escena original de Plaza Bicentenario.", "Aceptar");
                return;
            }
            if (!EditorUtility.DisplayDialog("Villazon V3", "Construye una aproximacion visual del entorno a partir de fotos. Desactiva los bloques laterales originales y OSM (si existe). No cambia la plaza, los limites, los personajes ni las colisiones. Guarda antes una copia de la escena. Las rampas son solo visuales.", "Generar", "Cancelar")) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("Villazon V3 requiere URP/Lit."); return; }
            Directory.CreateDirectory(VillazonV3Ruta);
            AssetDatabase.Refresh();
            string ruta = AssetDatabase.GenerateUniqueAssetPath(VillazonV3Ruta + "/EntornoV3.asset");
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generar Villazon V3");
            GameObject nueva = null;
            try
            {
                var recursos = new List<Object>();
                Material Mat(string nombre, string hex, float brillo = .16f)
                {
                    var m = new Material(shader) { name = "V3 " + nombre };
                    m.SetColor("_BaseColor", ColorHex(hex)); m.SetFloat("_Smoothness", brillo);
                    recursos.Add(m); return m;
                }
                Material asfalto = Mat("Asfalto", "57585B"), acera = Mat("Acera", "B9B6AD"), junta = Mat("Juntas", "8C8B86"),
                    muro = Mat("Piedra irregular", "958B7D"), bordillo = Mat("Bordillo", "77756F"), amarillo = Mat("Amarillo vial", "C7A23D"),
                    blanco = Mat("Marca blanca", "E3E0D7"), rojo = Mat("Baranda roja", "783F3E"), hierro = Mat("Hierro", "545459"),
                    rosa = Mat("Rosa antiguo", "B88E85"), rosaSombra = Mat("Rosa molduras", "936F69"), crema = Mat("Moldura crema", "C8B9A5"),
                    turquesa = Mat("Turquesa", "468B87"), terracota = Mat("Franjas terracota", "A96F56"),
                    vidrio = Mat("Cristal azul gris", "536B75", .55f), vidrioOscuro = Mat("Cristal oscuro", "34454D", .48f),
                    marco = Mat("Aluminio", "BCB9AA", .35f), beige = Mat("Beige universitario", "BDAA89"),
                    azul = Mat("Azul fachada", "345B89"), claro = Mat("Blanco fachada", "D5D2C6"),
                    ocre = Mat("Comercio ocre", "C29B58"), verde = Mat("Follaje", "4C6940"), tierra = Mat("Tierra", "6E6252"),
                    persiana = Mat("Persiana comercial", "777D7A"), techo = Mat("Cubierta", "898580"), toldo = Mat("Toldo verde", "367B67");
                // Textura repetible generada por C#: pequeñas variaciones del asfalto.
                var tex = new Texture2D(128, 128, TextureFormat.RGB24, true) { name = "V3 grano asfalto", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
                var pixels = new Color32[128 * 128]; var rng = new System.Random(541);
                for (int i = 0; i < pixels.Length; i++) { byte t = (byte)rng.Next(150, 246); pixels[i] = new Color32(t,t,t,255); }
                tex.SetPixels32(pixels); tex.Apply(); asfalto.SetTexture("_BaseMap", tex); asfalto.SetTextureScale("_BaseMap", new Vector2(3,3)); recursos.Add(tex);
                nueva = new GameObject(VillazonV3Nombre);
                SceneManager.MoveGameObjectToScene(nueva, escena);
                // El origen original es (0,0,0), +Z hacia el frontis, +X hacia JJ Perez.
                Transform raiz = nueva.transform;
                Transform Nodo(string nombre, Vector3 pos = default, float giro = 0f) => Grupo(nombre, raiz, pos, giro);
                void Malla(Geometria g, Transform nodo)
                {
                    var (mesh, mats) = g.Exportar(nodo.name); recursos.Add(mesh);
                    nodo.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    nodo.gameObject.AddComponent<MeshRenderer>().sharedMaterials = mats;
                }
                void Caja(Geometria g,float x,float y,float z,float w,float h,float d,Material m,float giro=0f)
                    => g.Caja(new Vector3(x,y,z),new Vector3(w,h,d),m,giro);
                // Franja entre dos puntos 3D: sirve para calles, rampas y aceras.
                void Franja(Geometria g,Vector3 a,Vector3 b,float ancho,Material m)
                {
                    Vector3 d=b-a; d.y=0; if(d.sqrMagnitude<.0001f) return;
                    Vector3 n=new Vector3(-d.z,0,d.x).normalized*ancho*.5f;
                    g.Cuadrado(m,a-n,a+n,b+n,b-n);
                }
                void BarandaV3(Geometria g,Vector3 a,Vector3 b)
                {
                    Vector3 al = a+Vector3.up*.92f, bl=b+Vector3.up*.92f;
                    g.Tubo(al,bl,.038f,rojo,6); g.Tubo(a+Vector3.up*.16f,b+Vector3.up*.16f,.024f,rojo,6);
                    int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.55f));
                    for(int i=0;i<=n;i++) { Vector3 p=Vector3.Lerp(a,b,i/(float)n); g.Tubo(p,p+Vector3.up*.96f,i%5==0?.035f:.018f,rojo,6); }
                }
                // Calles exteriores: no se dibuja sobre el suelo jugable existente.
                var calles = new Geometria(); var detalles = new Geometria();
                // Superficies de apoyo visual; se deja libre la franja de la trinchera.
                var terreno = new Geometria();
                foreach (int lado in new[]{-1,1})
                {
                    Caja(terreno,lado*69f,-.48f,-28.15f,82f,.25f,43.7f,acera);
                    Caja(terreno,lado*69f,-.48f,44.12f,82f,.25f,82f,acera);
                    Caja(terreno,lado*69f,-3.6f,-1.5f,82f,.25f,9.3f,asfalto);
                }
                Caja(terreno,0,-.50f,-36f,56f,.25f,28f,acera);
                Caja(terreno,0,-.50f,59f,56f,.25f,51f,acera);
                Malla(terreno,Nodo("Superficies de apoyo exteriores"));
                void Via(Vector3[] p,float ancho,int carriles,bool barandas=false)
                {
                    for(int i=0;i<p.Length-1;i++)
                    {
                        Vector3 a=p[i],b=p[i+1]; Vector3 d=b-a; d.y=0; if(d.sqrMagnitude<.001f) continue;
                        Vector3 n=new Vector3(-d.z,0,d.x).normalized;
                        Franja(calles,a,b,ancho,asfalto);
                        for(int lado=-1;lado<=1;lado+=2)
                        {
                            Vector3 q=n*lado*(ancho/2+1.15f);
                            Franja(calles,a+q+Vector3.up*.13f,b+q+Vector3.up*.13f,2.3f,acera);
                            q=n*lado*(ancho/2+.045f);
                            Franja(detalles,a+q+Vector3.up*.018f,b+q+Vector3.up*.018f,.09f,amarillo);
                            if(barandas) BarandaV3(detalles,a+n*lado*(ancho/2+2.25f)+Vector3.up*.13f,b+n*lado*(ancho/2+2.25f)+Vector3.up*.13f);
                        }
                        float len=d.magnitude;
                        for(int c=1;c<carriles;c++)
                        {
                            Vector3 q=n*(-ancho/2+ancho*c/carriles)+Vector3.up*.022f;
                            for(float t=1;t<len-.3f;t+=6f) Franja(detalles,Vector3.Lerp(a,b,t/len)+q,Vector3.Lerp(a,b,Mathf.Min(t+2.8f,len)/len)+q,.11f,blanco);
                        }
                    }
                }
                // Laterales de Villazon, con calzada central deprimida visible fuera de la plaza.
                foreach(int signo in new[]{-1,1})
                {
                    float s=signo;
                    // La plataforma peatonal original se conserva entre X=-28 y X=28.
                    Via(new[]{new Vector3(s*28.05f,-3.15f,-1.5f),new Vector3(s*44f,-3.15f,-1.5f),new Vector3(s*69f,-.65f,-1.5f),new Vector3(s*108f,-.20f,-1.5f)},8.6f,2);
                    Via(new[]{new Vector3(s*28.1f,-.15f,-12f),new Vector3(s*64f,-.15f,-12f),new Vector3(s*108f,-.20f,-12f)},6.4f,2);
                    Via(new[]{new Vector3(s*28.1f,-.15f,9.4f),new Vector3(s*61f,-.15f,9.4f),new Vector3(s*108f,-.20f,9.4f)},6.2f,2);
                    // Muros de contencion: la coronacion queda al nivel de la explanada.
                    foreach(float z in new[]{-6.12f,3.12f})
                    {
                        Vector3 a=new(s*28.05f,-3.15f,z), b=new(s*44f,-3.15f,z), c=new(s*69f,-.65f,z);
                        foreach(var par in new[]{(a,b),(b,c)})
                        {
                            Vector3 p=par.Item1,q=par.Item2, pt=new(p.x,-.02f,p.z),qt=new(q.x,-.02f,q.z);
                            detalles.Cuadrado(muro,p,pt,qt,q); detalles.Cuadrado(muro,q,qt,pt,p);
                        }
                        BarandaV3(detalles,new Vector3(s*28.05f,0,z),new Vector3(s*69f,0,z));
                    }
                }
                // JJ Perez asciende hacia el campus, a la derecha del frontis.
                Via(new[]{new Vector3(38f,-.12f,12.5f),new Vector3(39.5f,.15f,25f),new Vector3(44f,.65f,41f),new Vector3(48f,1.35f,59f),new Vector3(51f,2.25f,83f)},7.6f,2);
                Malla(calles,Nodo("Calzadas y aceras exteriores")); Malla(detalles,Nodo("Marcas, barandas y muros del desnivel"));

                void Vent(Geometria g,Vector3 c,float w,float h,float giro,int variante=0)
                {
                    Quaternion r=Quaternion.Euler(0,giro,0);
                    void P(float x,float y,float z,float a,float b,float d,Material m)=>g.Caja(c+r*new Vector3(x,y,z),new Vector3(a,b,d),m,giro);
                    P(0,0,-.018f,w,h,.08f,marco); P(0,0,-.072f,w-.10f,h-.09f,.034f,variante%4==0?vidrioOscuro:vidrio);
                    P(0,0,-.100f,.035f,h,.035f,marco); P(-w*.25f,0,-.10f,.035f,h,.035f,marco); P(w*.25f,0,-.10f,.035f,h,.035f,marco);
                }
                void Torre(string nombre,Vector3 pos,float w,float dep,int pisos,Material principal,Material banda,bool comercial,float giro=0)
                {
                    Transform n=Nodo(nombre,pos,giro); var g=new Geometria(); float pb=comercial?3.4f:0, paso=2.75f, h=pb+pisos*paso;
                    Caja(g,0,h/2,0,w,h,dep,principal); Caja(g,0,h+.17f,0,w+.22f,.34f,dep+.22f,techo);
                    foreach(int lado in new[]{0,90,180,270})
                    {
                        Quaternion r=Quaternion.Euler(0,lado,0); float ancho=lado%180==0?w:dep, fondo=lado%180==0?dep:w;
                        void P(float x,float y,float z,float a,float b,float d,Material m)=>g.Caja(r*new Vector3(x,y,z),new Vector3(a,b,d),m,lado);
                        int mod=Mathf.Max(2,Mathf.RoundToInt(ancho/2.7f)); float celda=(ancho-.7f)/mod;
                        for(int f=0;f<pisos;f++)
                        {
                            float y=pb+f*paso;
                            P(0,y+.42f,-fondo/2-.05f,ancho,.64f,.14f,banda);
                            for(int j=0;j<mod;j++) { float x=-ancho/2+.35f+celda*(j+.5f); Vent(g,r*new Vector3(x,y+1.55f,-fondo/2-.045f),celda*.86f,1.25f,lado,j+f); }
                        }
                        if(comercial) for(int j=0;j<mod;j++)
                        {
                            float x=-ancho/2+.35f+celda*(j+.5f);
                            P(x,1.20f,-fondo/2-.065f,celda*.87f,2.35f,.13f,j%2==0?persiana:vidrioOscuro);
                            P(x,2.76f,-fondo/2-.17f,celda*.89f,.55f,.28f,j%2==0?ocre:azul);
                            if(j%2==0) for(int k=0;k<8;k++) P(x,.18f+k*.27f,-fondo/2-.15f,celda*.82f,.025f,.025f,hierro);
                        }
                        foreach(int signo in new[]{-1,1}) P(signo*(ancho/2-.15f),h/2,-fondo/2-.10f,.30f,h,.23f,principal);
                    }
                    Caja(g,0,h+.7f,0,w*.30f,1.0f,dep*.30f,principal);
                    g.Tubo(new Vector3(w*.28f,h,0),new Vector3(w*.28f,h+2.7f,0),.027f,hierro,6);
                    Malla(g,n);
                }
                // Edificio historico de esquina, con molduras y balcon corrido.
                {
                    Transform n=Nodo("Esquina rosada - arquitectura historica",new Vector3(55f,.05f,17.9f),-9f); var g=new Geometria();
                    Caja(g,0,4.3f,0,13.8f,8.6f,10.4f,rosa);
                    Caja(g,0,8.53f,0,14.30f,.32f,10.85f,crema); Caja(g,0,8.89f,0,14.50f,.30f,11.02f,rosaSombra);
                    Caja(g,0,9.25f,0,13.65f,.40f,10.2f,rosa);
                    foreach(int lado in new[]{0,90,180,270})
                    {
                        Quaternion r=Quaternion.Euler(0,lado,0); float w=lado%180==0?13.8f:10.4f, dep=lado%180==0?10.4f:13.8f;
                        void P(float x,float y,float z,float a,float b,float d,Material m)=>g.Caja(r*new Vector3(x,y,z),new Vector3(a,b,d),m,lado);
                        P(0,3.6f,-dep/2-.13f,w,.24f,.35f,crema); P(0,7.82f,-dep/2-.12f,w,.22f,.27f,crema);
                        int mod=lado%180==0?5:4; float paso=(w-.6f)/mod;
                        for(int i=0;i<mod;i++)
                        {
                            float x=-w/2+.3f+paso*(i+.5f);
                            P(x,5.85f,-dep/2-.07f,paso*.75f,2.65f,.22f,rosaSombra);
                            Vent(g,r*new Vector3(x,5.85f,-dep/2-.21f),paso*.62f,2.35f,lado,i);
                            P(x,7.22f,-dep/2-.20f,paso*.81f,.18f,.35f,crema);
                            P(x,1.43f,-dep/2-.06f,paso*.80f,2.72f,.15f,i%2==0?persiana:vidrioOscuro);
                            P(x,3.05f,-dep/2-.16f,paso*.83f,.46f,.24f,i%2==0?ocre:terracota);
                        }
                        if(lado==0||lado==90)
                        {
                            P(0,4.42f,-dep/2-.53f,w*.74f,.22f,1.05f,crema);
                            BarandaV3(g,r*new Vector3(-w*.36f,4.53f,-dep/2-1.02f),r*new Vector3(w*.36f,4.53f,-dep/2-1.02f));
                        }
                    }
                    Malla(g,n);
                }
                Torre("Torre turquesa con franjas rojizas",new Vector3(60f,.35f,35f),13.2f,12.5f,13,turquesa,terracota,true,-5f);
                Torre("Edificio claro y verde del fondo",new Vector3(79f,.1f,35f),15f,13f,11,claro,turquesa,true);
                Torre("Torre alta de cristal del fondo",new Vector3(97f,.0f,25f),14f,13.5f,16,claro,vidrioOscuro,true);
                Torre("Bloque bajo universitario de JJ Perez",new Vector3(25f,.5f,39f),14f,13f,3,beige,crema,false);
                Torre("Bloque comercial blanco y azul frente a plaza",new Vector3(18f,-.25f,-34f),16f,12f,10,claro,azul,true,180);
                Torre("Bloque comercial rojo y blanco",new Vector3(40f,-.25f,-35f),15f,13f,12,claro,terracota,true,180);
                Torre("Edificio comercial ocre",new Vector3(57f,-.25f,-33f),13f,11f,5,ocre,crema,true,180);
                Torre("Bloque comercial claro del otro extremo",new Vector3(-21f,-.25f,-34f),16f,12f,9,claro,vidrioOscuro,true,180);
                Torre("Torre ladrillo extremo opuesto",new Vector3(-48f,-.15f,26f),14f,12f,12,terracota,crema,true);
                Torre("Bloque claro extremo opuesto",new Vector3(-69f,-.15f,27f),15f,13f,10,claro,turquesa,true);
                // Aceras exteriores, muro de piedra y quiosco de la fotografia.
                var urbano=new Geometria();
                Caja(urbano,33.3f,.9f,33f,.45f,1.8f,18f,muro);
                for(int fila=0;fila<5;fila++) for(int j=0;j<30;j++)
                {
                    float z=24.35f+j*.57f+(fila%2)*.16f;
                    Caja(urbano,33.06f,.18f+fila*.35f,z,.055f,.29f,.48f,(j+fila)%4==0?crema:muro);
                }
                Caja(urbano,31.3f,1.15f,20.5f,2.05f,2.30f,1.6f,hierro);
                Caja(urbano,31.3f,2.38f,20.35f,2.55f,.18f,2.16f,toldo);
                Caja(urbano,31.3f,1.5f,19.65f,1.65f,1.15f,.08f,vidrioOscuro);
                for(int j=0;j<11;j++) { float z=14+j*1.7f; urbano.Tubo(new Vector3(34.4f,0,z),new Vector3(34.4f,.82f,z),.055f,hierro,8); }
                foreach(float x in new[]{-45f,-72f,46f,72f,94f})
                {
                    urbano.Tubo(new Vector3(x,0,-16.4f),new Vector3(x,6.8f,-16.4f),.07f,hierro,8);
                    urbano.Tubo(new Vector3(x,6.8f,-16.4f),new Vector3(x,7.1f,-14.6f),.045f,hierro,8);
                    Caja(urbano,x,7.05f,-14.5f,.45f,.14f,.62f,hierro);
                }
                Malla(urbano,Nodo("Mobiliario, bolardos y quiosco de JJ Perez"));
                // Guardar recursos separados: nunca sobreescribir PlazaBicentenario.asset.
                AssetDatabase.CreateAsset(recursos[0],ruta);
                for(int i=1;i<recursos.Count;i++) AssetDatabase.AddObjectToAsset(recursos[i],ruta);
                AssetDatabase.SaveAssets();
                Undo.RegisterCreatedObjectUndo(nueva,"Crear entorno V3");
                void Ocultar(GameObject o) { if(o==null||!o.activeSelf)return; Undo.RecordObject(o,"Ocultar entorno anterior"); o.SetActive(false); }
                Ocultar(antiguo.transform.Find("Edificios de los laterales")?.gameObject);
                Ocultar(osm);
                if(anterior!=null) Undo.DestroyObjectImmediate(anterior);
                // El panorama importado puede contener suelo/edificios que tapan las nuevas rampas.
                // Se deja visible por defecto; menu separado permite ocultarlo tras comparar.
                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(escena);
                Selection.activeGameObject=nueva;
                SceneView.lastActiveSceneView?.LookAt(new Vector3(44,5,10),Quaternion.Euler(24,-35,0),60f);
                Debug.Log("Villazon V3 generado. Geometria exterior visual, dimensiones aproximadas. Guarda la escena si aceptas el resultado. Recursos: "+ruta);
            }
            catch(Exception ex)
            {
                Undo.RevertAllDownToGroup(undo);
                if(nueva!=null) Object.DestroyImmediate(nueva);
                if(AssetDatabase.LoadMainAssetAtPath(ruta)!=null) AssetDatabase.DeleteAsset(ruta);
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Villazon V3",ex.Message,"Aceptar");
            }
        }

        [MenuItem("Supervivencia/Villazon V3/Alternar panorama OBJ original")]
        private static void AlternarPanoramaVillazonV3()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            Scene escena=SceneManager.GetActiveScene();
            foreach(GameObject root in escena.GetRootGameObjects())
            {
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root)!=Raiz+"/Panorama/PanoramaVillazon.obj")continue;
                Undo.RecordObject(root,"Alternar panorama original"); root.SetActive(!root.activeSelf);
                EditorSceneManager.MarkSceneDirty(escena); return;
            }
            Debug.LogWarning("No se encontro la instancia original del panorama OBJ.");
        }
    }
}
