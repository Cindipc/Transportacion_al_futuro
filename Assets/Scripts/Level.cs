using System;
using System.Collections.Generic;
using UnityEngine;

namespace EiraGame
{
    // ==================== REGISTRO MUNDIAL ====================
    public static class World
    {
        private static readonly List<GameObject> items = new List<GameObject>();

        public static void Register(GameObject go)
        {
            if (go == null || items.Contains(go)) return;
            items.Add(go);
        }

        public static void Clear()
        {
            foreach (var go in items)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            items.Clear();
        }

        public static void StripCamera()
        {
            var camObj = GameObject.Find("Main Camera");
            var f = camObj != null ? camObj.GetComponent<CameraFollow>() : null;
            if (f != null) UnityEngine.Object.Destroy(f);
        }
    }

    // ==================== FX ====================
    public static class Fx
    {
        private static Sprite spark;

        private static Sprite SparkSpr => spark != null ? spark : (spark = Resources.Load<Sprite>("Sprites/spark"));

        public static void Spark(Vector3 pos, float scale = 0.5f)
        {
            Spark(pos, scale, Color.white);
        }

        public static void Spark(Vector3 pos, float scale, Color c)
        {
            var go = new GameObject("Spark");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SparkSpr;
            sr.color = c;
            sr.sortingOrder = 30;
            UnityEngine.Object.Destroy(go, 0.3f);
        }

        public static void SparkBurst(Vector3 pos, int count, Color? c = null)
        {
            for (int i = 0; i < count; i++)
            {
                Spark(pos + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f), 0f),
                    UnityEngine.Random.Range(0.4f, 0.9f), c ?? Color.white);
            }
        }

        public static void BigSpark(Vector3 pos)
        {
            Spark(pos, 1.6f);
            Spark(pos + new Vector3(0.35f, 0.35f, 0f), 1f);
            Spark(pos + new Vector3(-0.35f, -0.35f, 0f), 1f);
        }
    }

    // ==================== ZONA DISPARADORA ====================
    public class TriggerZone : MonoBehaviour
    {
        public string Id;
        public Action<EiraController> OnEnter;
        public Action<EiraController> OnExit;

        public static TriggerZone Create(Vector2 center, Vector2 size, string id)
        {
            var go = new GameObject("Zone_" + id);
            go.transform.position = center;
            World.Register(go);
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = size;
            var z = go.AddComponent<TriggerZone>();
            z.Id = id;
            return z;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null) OnEnter?.Invoke(e);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null) OnExit?.Invoke(e);
        }
    }

    // ==================== CÁMARA ====================
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 min = new Vector2(-8f, -3f);
        public Vector2 max = new Vector2(214f, 12f);

        private Camera cam;
        private float zoom, targetZoom = 5f;

        public static CameraFollow Attach(Camera c, Transform t)
        {
            var f = c.gameObject.AddComponent<CameraFollow>();
            f.cam = c;
            f.target = t;
            f.zoom = c.orthographicSize;
            f.targetZoom = c.orthographicSize;
            return f;
        }

        private void LateUpdate()
        {
            if (target == null || cam == null) return;

            Vector3 p = target.position + Vector3.up * 0.6f;
            p.x = Mathf.Clamp(p.x, min.x, max.x);
            p.y = Mathf.Clamp(p.y, min.y, max.y);
            p.z = -10f;
            transform.position = Vector3.Lerp(transform.position, p, 1f - Mathf.Exp(-5f * Time.deltaTime));

            if (!Mathf.Approximately(zoom, targetZoom))
            {
                zoom = Mathf.Lerp(zoom, targetZoom, 1f - Mathf.Exp(-2f * Time.deltaTime));
                cam.orthographicSize = zoom;
            }
        }

        public void SetTargetZoom(float z) => targetZoom = z;

        public void RestoreZoom() => targetZoom = 5f;
    }

    // ==================== PORTAL DE SALIDA ====================
    public class ExitPortal : MonoBehaviour
    {
        private Vector3 baseScale = new Vector3(2.8f, 3f, 1f);
        private int bonus = 500;
        private string epilogue = "";

        public static ExitPortal Create(Vector3 pos, int bonus = 500, string epilogue = "")
        {
            var go = new GameObject("ExitPortal");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/portal_exit");
            sr.sortingOrder = 3;
            go.transform.localScale = new Vector3(2.8f, 3f, 1f);
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 1.4f;
            var p = go.AddComponent<ExitPortal>();
            p.baseScale = go.transform.localScale;
            p.bonus = bonus;
            p.epilogue = epilogue;
            return p;
        }

        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * 3.5f) * 0.06f;
            transform.localScale = baseScale * s;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e == null) return;
            GameManager.I?.OnWin(epilogue, bonus);
        }
    }

    // ==================== CONSTRUCCIÓN DEL NIVEL ====================
    public class LevelData
    {
        public EiraController Player;
        public NovaController Nova;
        public CameraFollow Cam;
        public GuardianBoss Boss;
        public Vector3 Spawn = new Vector3(5f, 2f, 0f);
    }

    public static class LevelBuilder
    {
        private static Sprite Sp(string n) => Resources.Load<Sprite>("Sprites/" + n);

        public static string LevelName(int idx)
        {
            switch (idx)
            {
                case 2: return "LA CAZA";
                case 3: return "LA ÚLTIMA GUERRA";
                default: return "EL DESPERTAR";
            }
        }

        public static string LevelObjective(int idx)
        {
            switch (idx)
            {
                case 2: return "Cruza la ciudad en ruinas y alcanza el portal de salida.";
                case 3: return "Desactiva a KAEL y toca el Núcleo Central para decidir.";
                default: return "Escapa del laboratorio recuperando pistas sobre el año 3000 y el control de Kael.";
            }
        }

        public static List<DialogueLine> LevelIntro(int idx)
        {
            if (idx == 2)
            {
                return new List<DialogueLine>
                {
                    new DialogueLine("EIRA", "Wow… la ciudad. NOVA, ¿esto era nuestro hogar?"),
                    new DialogueLine("NOVA", "Esto son las ruinas del año 3000. Kael nos busca; sus soldados ya patrullan las calles."),
                    new DialogueLine("NOVA", "Algunos rebeldes escondidos emiten señales de auxilio. Ayúdalos si puedes, y avanza hacia el portal del refugio."),
                    new DialogueLine("EIRA", "…Kael quiere usarme. Pues que venga a buscarme.")
                };
            }
            if (idx == 3)
            {
                return new List<DialogueLine>
                {
                    new DialogueLine("NOVA", "El Núcleo Central. El corazón del control de Kael. Tras esas puertas te espera su última trampa."),
                    new DialogueLine("EIRA", "Entonces de aquí no salimos las dos sin… decidir algo."),
                    new DialogueLine("NOVA", "Los archivos dicen que solo una portadora puede elegir el destino de todos. Eira… tú eliges."),
                    new DialogueLine("EIRA", "Tres vidas en mi ADN. A ver qué pedí esta vez.")
                };
            }
            return new List<DialogueLine>();
        }

        // Suelo (recorre tiles y crea colisión)
        private static void Floor(float a, float b, string sprite)
        {
            var go = new GameObject("Floor_" + a + "_" + b);
            World.Register(go);
            for (float x = a; x < b; x++)
            {
                var t = new GameObject("t");
                t.transform.SetParent(go.transform);
                t.transform.position = new Vector3(x + 0.5f, 0.5f, 0f);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = Sp(sprite);
                sr.sortingOrder = -10;
            }
            var col = new GameObject("col");
            col.transform.SetParent(go.transform);
            col.transform.position = new Vector3((a + b) / 2f, 0.5f, 0f);
            var bc = col.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(b - a, 1f);
        }

        private static void Bar(Vector2 center, Vector2 size, string sprite, int order, Color? tint = null)
        {
            var go = new GameObject("Bar_" + sprite);
            go.transform.position = center;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sp(sprite);
            sr.sortingOrder = order;
            sr.color = tint ?? Color.white;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private static void Background(string sprite, float xFrom, float xTo, float yCenter)
        {
            var go = new GameObject("BG_" + sprite);
            World.Register(go);
            for (float x = xFrom; x < xTo; x += 16f)
            {
                var t = new GameObject("t");
                t.transform.SetParent(go.transform);
                t.transform.position = new Vector3(x + 8f, yCenter, 0f);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = Sp(sprite);
                sr.sortingOrder = -100;
            }
        }

        private static void Platform(float a, float b, float top, string sprite = "tile_platform_metal")
        {
            var go = new GameObject("Platform_" + a + "_" + b);
            World.Register(go);
            for (float x = a + 1f; x < b; x += 2f)
            {
                var t = new GameObject("t");
                t.transform.SetParent(go.transform);
                t.transform.position = new Vector3(x, top - 0.5f, 0f);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = Sp(sprite);
                sr.sortingOrder = -5;
            }
            var col = new GameObject("col");
            col.transform.SetParent(go.transform);
            col.transform.position = new Vector3((a + b) / 2f, top - 0.5f, 0f);
            var bc = col.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(b - a, 0.9f);
        }

        private static void SetCamera(LevelData data, Color bg, Vector2 min, Vector2 max)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj == null) return;
            var cam = camObj.GetComponent<Camera>();
            if (cam == null) return;
            cam.backgroundColor = bg;
            cam.orthographicSize = 5f;
            data.Cam = CameraFollow.Attach(cam, data.Player.transform);
            data.Cam.min = min;
            data.Cam.max = max;
        }

        private static GameObject MakeGate(float x)
        {
            var go = new GameObject("ArenaGate");
            go.transform.position = new Vector3(x, 3.15f, 0f);
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sp("door_locked");
            sr.sortingOrder = 4;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(2.4f, 4.3f);
            go.SetActive(false);
            return go;
        }

        public static LevelData Build() => BuildLevel(1);

        public static LevelData BuildLevel(int index)
        {
            World.Clear();
            World.StripCamera();
            switch (index)
            {
                case 2: return BuildCity();
                case 3: return BuildCore();
                default: return BuildLab();
            }
        }

        // ============================================================
        // NIVEL 1 · EL DESPERTAR (laboratorio)
        // ============================================================
        private static LevelData BuildLab()
        {
            var data = new LevelData();
            data.Spawn = new Vector3(5f, 2f, 0f);

            // ---------- FONDO Y TECHO ----------
            Background("bg_lab", -8f, 224f, 4.5f);
            Bar(new Vector2(100f, 12f), new Vector2(216f, 5f), "tile_core_wall", -95, new Color(0.02f, 0.03f, 0.05f, 1f));
            Bar(new Vector2(101f, 9.5f), new Vector2(202f, 1f), "tile_wall_lab2", -90);
            Bar(new Vector2(100f, -0.5f), new Vector2(206f, 1f), "tile_floor_lab2", -15);

            // ---------- SUELO (con huecos trampa) ----------
            Floor(-3f, 119f, "tile_floor_lab");
            Floor(124f, 150f, "tile_floor_lab");
            Floor(155f, 203f, "tile_floor_lab");

            // ---------- MURALLAS ----------
            Bar(new Vector2(-1.5f, 7f), new Vector2(1f, 14f), "tile_wall_lab", -20);
            Bar(new Vector2(202.5f, 7f), new Vector2(1f, 14f), "tile_wall_lab", -20);
            var wl = new GameObject("WallL");
            wl.transform.position = new Vector3(-1.5f, 7f, 0f);
            World.Register(wl);
            wl.AddComponent<BoxCollider2D>().size = new Vector2(1f, 14f);
            var wr = new GameObject("WallR");
            wr.transform.position = new Vector3(202.5f, 7f, 0f);
            World.Register(wr);
            wr.AddComponent<BoxCollider2D>().size = new Vector2(1f, 14f);

            // ---------- PLATAFORMAS ----------
            Platform(35f, 45f, 3f);      // P1
            Platform(60f, 68f, 2.5f);    // P2
            Platform(69f, 81f, 3f);      // P3 (relé R2 + dron) — altura baja para alcanzarla desde el suelo
            Platform(95f, 103f, 3f);     // P4
            Platform(111f, 121f, 4.2f);  // P5 (terminal T2 + cápsula)
            Platform(135f, 153f, 3f);    // P6 (recogibles + zona de evasión)
            Platform(107f, 111f, 2.2f);  // P7 (paso hacia P5)

            // ---------- PINCHOS DE SEGURIDAD ----------
            SpikeZone.Create(new Vector3(130.5f, 1.6f, 0f), new Vector2(1.8f, 0.8f));

            // ---------- PUERTA ADN ----------
            DnaDoor.Create(new Vector3(29f, 3.15f, 0f));

            // ---------- TERMINALES ----------
            Terminal t1 = Terminal.Create(new Vector3(13f, 2f, 0f), "INFORME FINAL — AÑO 3000",
                "Proyecto 'EIRA': verter el genoma humano en un chip reveló una compatibilidad imposible con la tecnología.",
                "La portadora puede activar puertas, núcleos y máquinas antiguas. Por eso debía desaparecer.",
                "Pero Kael nunca consintió perder a su 'llave'.");
            t1.ObjectiveAfter = "Pulsa (X) cerca de una puerta cifrada para desbloquearla con tu ADN.";

            Terminal t2 = Terminal.Create(new Vector3(118f, 5.4f, 0f), "PROYECTO EIRA — NÚCLEO",
                "La sangre de la portadora cierra el circuito completo del Núcleo Central.",
                "Kael ya reúne las tres llaves. Si despiertas su uso, la [ÚLTIMA GUERRA] comenzará.",
                "Por ahora, primero hay que restaurar la energía del sector. Busca los relés.");
            t2.ObjectiveAfter = "Activa los dos relés de energía para abrir la barrera del sector.";

            Terminal t3 = Terminal.Create(new Vector3(148f, 2f, 0f), "ADVERTENCIA DE SEGURIDAD",
                "SECTOR 4: Guardián de red en estado crítico. No intentar desactivar sin autorización.",
                "Se recomienda encarecidamente no cruzar la barrera. Y jamás alimentar al núcleo con sangre humana.");
            t3.ObjectiveAfter = "Cruza la barrera y enfréntate a la Máquina Guardiana para abrir la salida.";

            // ---------- RECOGIBLES ----------
            Pickup.Create(Pickup.Kind.Chip, new Vector3(20f, 1.7f, 0f));
            Pickup.Create(Pickup.Kind.Medkit, new Vector3(39f, 3.5f, 0f));
            Pickup.Create(Pickup.Kind.Chip, new Vector3(76f, 2.5f, 0f));
            Pickup.Create(Pickup.Kind.Capsule, new Vector3(96f, 1.9f, 0f));
            Pickup.Create(Pickup.Kind.Capsule, new Vector3(116f, 5f, 0f));
            Pickup.Create(Pickup.Kind.Medkit, new Vector3(139f, 3.4f, 0f));
            Pickup.Create(Pickup.Kind.Chip, new Vector3(144f, 3.4f, 0f));

            // ---------- CHECKPOINTS ----------
            Checkpoint.Create(new Vector3(8f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(50f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(93f, 1.8f, 0f));

            // ---------- ACERTIJO DE RELÉS ----------
            RelaySwitch r1 = RelaySwitch.Create(new Vector3(52f, 1.7f, 0f), "R1");
            RelaySwitch r2 = RelaySwitch.Create(new Vector3(72f, 3.6f, 0f), "R2");
            DnaGateGate gate = DnaGateGate.Create(new Vector3(86f, 3.15f, 0f));
            GeneratorPuzzle.Create(r1, r2, gate);

            // ---------- JUGADOR, NOVA Y CÁMARA ----------
            data.Player = EiraController.Create(data.Spawn);
            data.Nova = NovaController.Create();
            data.Nova.Player = data.Player;

            var novaTalk = data.Nova.GetComponent<NovaTalk>();
            if (novaTalk != null)
            {
                novaTalk.TipLines = new[]
                {
                    "Este laboratorio estudiaba el ADN. Todo empezó con unos archivos llamados 'Kael'.",
                    "Una puerta como esa solo se abre con ADN compatible. Prueba con tu pulso (X).",
                    "Las máquinas de Kael patrullan los pasillos. Si te ven, disparan. No dudes en usar tu pulso."
                };
            }

            SetCamera(data, new Color(0.03f, 0.04f, 0.07f, 1f), new Vector2(-8f, -3f), new Vector2(214f, 12f));

            // ---------- DRONES ----------
            Drone d1 = Drone.Create(new Vector3(33f, 2.2f, 0f), new Vector2(33f, 2.2f), new Vector2(40f, 2.2f));
            d1.AttachPlayer(data.Player);
            Drone d2 = Drone.Create(new Vector3(71f, 5f, 0f), new Vector2(71f, 5f), new Vector2(79f, 5f));
            d2.AttachPlayer(data.Player);
            Drone d3 = Drone.Create(new Vector3(96f, 2.4f, 0f), new Vector2(96f, 2.4f), new Vector2(109f, 2.4f));
            d3.AttachPlayer(data.Player);
            Drone d4 = Drone.Create(new Vector3(126f, 4.6f, 0f), new Vector2(126f, 4.6f), new Vector2(142f, 4.6f));
            d4.AttachPlayer(data.Player);
            Drone d5 = Drone.Create(new Vector3(148f, 2.2f, 0f), new Vector2(148f, 2.2f), new Vector2(155f, 2.2f));
            d5.AttachPlayer(data.Player);

            // ---------- ZONA DE EVASIÓN (+150) ----------
            TriggerZone.Create(new Vector2(128f, 3f), new Vector2(16f, 8f), "AvoidIn").OnEnter = p =>
                p.MarkedDamaged = false;
            TriggerZone.Create(new Vector2(150f, 3f), new Vector2(4f, 8f), "AvoidOut").OnExit = p =>
            {
                if (p.transform.position.x < 151f) return; // salió por la izquierda
                if (p.transform.position.y < 1f) return;   // cayó en el vacío, no cuenta
                if (!p.MarkedDamaged)
                {
                    GameManager.I?.AddPoints(150);
                    GameManager.I?.UI.ShowShadowToast("+150 · Cruzaste la zona sin ser detectada");
                    Sfx.Play("chip");
                }
            };

            // ---------- ARENA DEL JEFE ----------
            data.Boss = GuardianBoss.Create(new Vector3(174f, 2.8f, 0f));
            data.Boss.Player = data.Player;
            data.Boss.gameObject.SetActive(true);

            GameObject gateL = MakeGate(157f);
            GameObject gateR = MakeGate(198f);
            var colL = gateL.GetComponent<BoxCollider2D>();
            var colR = gateR.GetComponent<BoxCollider2D>();
            var srL = gateL.GetComponent<SpriteRenderer>();
            var srR = gateR.GetComponent<SpriteRenderer>();

            TriggerZone.Create(new Vector2(156f, 4f), new Vector2(1.5f, 9f), "BossStart").OnEnter = _ =>
            {
                if (data.Boss != null)
                {
                    data.Boss.Activate(true);
                    gateL.SetActive(true);
                    gateR.SetActive(true);
                    colL.enabled = true;
                    colR.enabled = true;
                }
            };

            data.Boss.OnDefeated = () =>
            {
                srL.sprite = Sp("door_industrial_open");
                srR.sprite = Sp("door_industrial_open");
                colL.enabled = false;
                colR.enabled = false;
                ExitPortal.Create(new Vector3(195f, 3.2f, 0f), 500,
                    "Sanas y salvas, Eira y NOVA cruzan el portal hacia la ciudad en ruinas del año 3000.\n\n" +
                    "\"Has despertado la primera llave, Eira. Aquí comienza todo.\" — NOVA sonríe.\n" +
                    "Pero entre los rascacielos derruidos, los drones de KAEL ya han detectado vuestra señal.");
                GameManager.I?.SetObjective("Cruza el portal para escapar del laboratorio hacia la ciudad.");
                Sfx.Play("door");
            };

            return data;
        }

        // ============================================================
        // NIVEL 2 · LA CAZA (ciudad en ruinas)
        // ============================================================
        private static LevelData BuildCity()
        {
            var data = new LevelData();
            data.Spawn = new Vector3(5f, 2f, 0f);

            Background("bg_city", -8f, 256f, 4.4f);

            // Suelo con tres plataformas flotantes
            Floor(-3f, 95f, "tile_floor_city");
            Floor(102f, 170f, "tile_floor_city");
            Floor(178f, 246f, "tile_floor_city");

            Bar(new Vector2(-1.5f, 7f), new Vector2(1f, 14f), "tile_wall_city", -20);
            Bar(new Vector2(246.5f, 7f), new Vector2(1f, 14f), "tile_wall_city", -20);
            var wl = new GameObject("WallL");
            wl.transform.position = new Vector3(-1.5f, 7f, 0f);
            World.Register(wl);
            wl.AddComponent<BoxCollider2D>().size = new Vector2(1f, 14f);
            var wr = new GameObject("WallR");
            wr.transform.position = new Vector3(246.5f, 7f, 0f);
            World.Register(wr);
            wr.AddComponent<BoxCollider2D>().size = new Vector2(1f, 14f);

            // ---------- PLATAFORMAS ----------
            Platform(18f, 30f, 3f);
            Platform(58f, 72f, 2.5f);
            Platform(78f, 96f, 3.6f);
            Platform(105f, 118f, 3f);
            Platform(128f, 148f, 3.6f);
            Platform(155f, 172f, 2.6f);
            Platform(95.5f, 102.5f, 2.4f);   // paso del primer hueco
            Platform(170.5f, 177.5f, 2.4f); // paso del segundo hueco

            // ---------- PINCHOS ----------
            SpikeZone.Create(new Vector3(99f, 1.6f, 0f), new Vector2(2f, 0.8f));
            SpikeZone.Create(new Vector3(175f, 1.6f, 0f), new Vector2(2f, 0.8f));

            // ---------- TERMINALES: ARCHIVOS PROYECTO EIRA ----------
            var p1 = Terminal.Create(new Vector3(23f, 3.6f, 0f), "ARCHIVO KAE/ÁRIO 1",
                "Kael era el director del proyecto: buscar un corazón lo bastante fuerte.",
                "Nunca lo encontró en los humanos… así que lo construyó en la máquina.",
                "Pero una máquina no siente. Y Kael convirtió la ciudad entera en su coto de caza.");
            p1.Points = 200;
            var p2 = Terminal.Create(new Vector3(115f, 3.9f, 0f), "ARCHIVO KAE/ÁRIO 2",
                "Caza de portadores: Protocolo 7. La 'llave' jamás debe cruzar las ruinas vivas.",
                "Por eso cada soldado que deambula entre los rascacielos busca señal humana.",
                "…Y sin embargo, aquí sigues leyendo.");
            p2.Points = 200;

            // ---------- RESCATES DE REBELDES ----------
            int rescued = 0;
            RescueRebel.Create(new Vector3(44f, 1.8f, 0f)).OnRescued = r =>
            {
                rescued++;
                if (rescued >= 2)
                {
                    GameManager.I.AddPoints(250);
                    GameManager.I.UI?.ShowShadowToast("+250 · Refugio rebelde descubierto");
                    GameManager.I.SetObjective("Cruza la ciudad en ruinas hasta el portal del refugio. Cuidado con la emboscada de Kael.");
                }
                else
                {
                    GameManager.I.SetObjective("Rescata también al segundo rebelde que emite señal de auxilio. (1/2)");
                }
            };
            RescueRebel.Create(new Vector3(110f, 4.4f, 0f)).OnRescued = r =>
            {
                rescued++;
                if (rescued >= 2)
                {
                    GameManager.I.AddPoints(250);
                    GameManager.I.UI?.ShowShadowToast("+250 · Refugio rebelde descubierto");
                    GameManager.I.SetObjective("Cruza la ciudad en ruinas hasta el portal del refugio. Cuidado con la emboscada de Kael.");
                }
                else
                {
                    GameManager.I.SetObjective("Rescata también al primer rebelde que emite señal de auxilio. (2/2)");
                }
            };

            // ---------- DATOS TECNOLÓGICOS (chip) ----------
            int chips = 0;
            System.Action<Pickup> chipMission = pk =>
            {
                chips++;
                if (chips >= 3)
                {
                    GameManager.I.AddPoints(250);
                    GameManager.I.UI?.ShowShadowToast("+250 · Sonda rebelde reparada");
                    GameManager.I.SetObjective("Lleva los datos completos: NOVA ya puede ver los movimientos de Kael.");
                }
            };
            Pickup.Create(Pickup.Kind.Chip, new Vector3(20f, 1.7f, 0f)).OnCollected = chipMission;
            Pickup.Create(Pickup.Kind.Chip, new Vector3(66f, 2.5f, 0f)).OnCollected = chipMission;
            Pickup.Create(Pickup.Kind.Chip, new Vector3(122f, 1.7f, 0f)).OnCollected = chipMission;

            // ---------- OTROS RECOGIBLES ----------
            Pickup.Create(Pickup.Kind.Medkit, new Vector3(86f, 3.2f, 0f));
            Pickup.Create(Pickup.Kind.Capsule, new Vector3(96f, 1.9f, 0f));
            Pickup.Create(Pickup.Kind.Medkit, new Vector3(156f, 3f, 0f));
            Pickup.Create(Pickup.Kind.Capsule, new Vector3(136f, 4.4f, 0f));

            // ---------- CHECKPOINTS ----------
            Checkpoint.Create(new Vector3(10f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(62f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(112f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(185f, 1.8f, 0f));

            // ---------- JUGADOR, NOVA Y CÁMARA ----------
            data.Player = EiraController.Create(data.Spawn);
            data.Nova = NovaController.Create();
            data.Nova.Player = data.Player;

            var novaTalk = data.Nova.GetComponent<NovaTalk>();
            if (novaTalk != null)
            {
                novaTalk.TipLines = new[]
                {
                    "Esos soldados son androides leales a Kael. Dos golpes de pulso los desactivan, pero si te acercas, atacan con descargas.",
                    "Las señales de auxilio vienen de un refugio al este. Ayudarlos es peligroso, pero nos dará buenos ojos sobre la ciudad.",
                    "Los archivos Kae/ário ocultan dónde planea Kael tu control total."
                };
            }

            SetCamera(data, new Color(0.08f, 0.05f, 0.10f, 1f), new Vector2(-8f, -3f), new Vector2(256f, 12f));

            // ---------- ENEMIGOS ----------
            AndroidSoldier a1 = AndroidSoldier.Create(new Vector3(20f, 1.9f, 0f), new Vector2(16f, 1.9f), new Vector2(34f, 1.9f));
            a1.AttachPlayer(data.Player);
            AndroidSoldier a2 = AndroidSoldier.Create(new Vector3(66f, 1.9f, 0f), new Vector2(60f, 1.9f), new Vector2(90f, 1.9f));
            a2.AttachPlayer(data.Player);
            AndroidSoldier a3 = AndroidSoldier.Create(new Vector3(140f, 1.9f, 0f), new Vector2(120f, 1.9f), new Vector2(160f, 1.9f));
            a3.AttachPlayer(data.Player);

            Drone d1 = Drone.Create(new Vector3(46f, 2.2f, 0f), new Vector2(40f, 2.2f), new Vector2(52f, 2.2f));
            d1.AttachPlayer(data.Player);
            Drone d2 = Drone.Create(new Vector3(110f, 4.2f, 0f), new Vector2(104f, 4.2f), new Vector2(118f, 4.2f));
            d2.AttachPlayer(data.Player);
            Drone d3 = Drone.Create(new Vector3(198f, 2.4f, 0f), new Vector2(190f, 2.4f), new Vector2(210f, 2.4f));
            d3.AttachPlayer(data.Player);

            // ---------- EMBOSCADA DE KAEL ----------
            GameObject gateL = MakeGate(207f);
            GameObject gateR = MakeGate(237f);
            var colL = gateL.GetComponent<BoxCollider2D>();
            var colR = gateR.GetComponent<BoxCollider2D>();
            var srL = gateL.GetComponent<SpriteRenderer>();
            var srR = gateR.GetComponent<SpriteRenderer>();
            bool ambush = false;

            TriggerZone.Create(new Vector2(216f, 4f), new Vector2(2.4f, 10f), "KaelAmbush").OnEnter = _ =>
            {
                if (ambush) return;
                ambush = true;
                gateL.SetActive(true);
                gateR.SetActive(true);
                colL.enabled = true;
                colR.enabled = true;
                Sfx.Play("door");

                var kael = KaelBoss.Create(new Vector3(222f, 2.8f, 0f), 0, true);
                kael.Player = data.Player;
                kael.Activate();
                kael.OnDefeated = () =>
                {
                    srL.sprite = Sp("door_industrial_open");
                    srR.sprite = Sp("door_industrial_open");
                    colL.enabled = false;
                    colR.enabled = false;
                    Sfx.Play("door");
                    ExitPortal.Create(new Vector3(226f, 3f, 0f), 1000,
                        "Kael se retira envuelto en llamas.\n\"…ÁNDATE, EIRA. PERO EL NÚCLEO CENTRAL ME PERTENECE, Y ALLÍ TE ESPERO.\"\n\n" +
                        "NOVA: \"Los archivos del refugio confirman lo que sospechaba: Kael no quiere la ciudad. Quiere despertar el Núcleo Central y reiniciarlo a su imagen.\n\n" +
                        "NIVEL 3 · LA ÚLTIMA GUERRA.");
                    GameManager.I.SetObjective("Cruza el portal y ve al Núcleo Central a enfrentar a Kael.");
                };
            };

            return data;
        }

        // ============================================================
        // NIVEL 3 · LA ÚLTIMA GUERRA (núcleo central)
        // ============================================================
        private static LevelData BuildCore()
        {
            var data = new LevelData();
            data.Spawn = new Vector3(5f, 2f, 0f);

            Background("bg_core", -8f, 200f, 4.4f);

            Floor(-3f, 70f, "tile_floor_core");
            Floor(77f, 150f, "tile_floor_core");
            Floor(156f, 196f, "tile_floor_core");

            Bar(new Vector2(-1.5f, 8f), new Vector2(1f, 16f), "tile_core_wall", -20);
            Bar(new Vector2(196.5f, 8f), new Vector2(1f, 16f), "tile_core_wall", -20);
            var wl = new GameObject("WallL");
            wl.transform.position = new Vector3(-1.5f, 8f, 0f);
            World.Register(wl);
            wl.AddComponent<BoxCollider2D>().size = new Vector2(1f, 16f);
            var wr = new GameObject("WallR");
            wr.transform.position = new Vector3(196.5f, 8f, 0f);
            World.Register(wr);
            wr.AddComponent<BoxCollider2D>().size = new Vector2(1f, 16f);

            // ---------- PLATAFORMAS ----------
            Platform(24f, 38f, 3f);
            Platform(50f, 64f, 2.6f);
            Platform(90f, 104f, 3f);
            Platform(116f, 130f, 3.5f);
            Platform(151f, 158f, 3f); // paso hacia la arena de Kael

            // ---------- PINCHOS ----------
            SpikeZone.Create(new Vector3(73.5f, 1.6f, 0f), new Vector2(2.5f, 0.8f));
            SpikeZone.Create(new Vector3(153f, 1.6f, 0f), new Vector2(2f, 0.8f));

            // ---------- ARCHIVOS SECRETOS ----------
            var s1 = Terminal.Create(new Vector3(62f, 2f, 0f), "ARCHIVO SECRETO — PLAN KAEL",
                "Reiniciar el Núcleo borraría la personalidad de cada androide para construir un ejército leal.",
                "Solo la sangre de una portadora puede desactivar el protocolo de seguridad del núcleo.",
                "Si la portadora decidiera otro camino… no hay precedentes.");
            s1.Points = 300;
            var s2 = Terminal.Create(new Vector3(126f, 4.4f, 0f), "ARCHIVO SECRETO — COMPATIBLES",
                "PROYECTO EIRA · SUJETOS COMPATIBLES DETECTADOS: 7.",
                "Kael los persigue a todos. Nunca encontrarán la tercera llave sobre las ruinas: las llaves son libres.");
            s2.Points = 300;

            // ---------- RECOGIBLES ----------
            Pickup.Create(Pickup.Kind.Chip, new Vector3(32f, 1.7f, 0f));
            Pickup.Create(Pickup.Kind.Chip, new Vector3(120f, 2f, 0f));
            Pickup.Create(Pickup.Kind.Medkit, new Vector3(31f, 3.5f, 0f));
            Pickup.Create(Pickup.Kind.Capsule, new Vector3(60f, 3.2f, 0f));
            Pickup.Create(Pickup.Kind.Capsule, new Vector3(96f, 1.9f, 0f));
            Pickup.Create(Pickup.Kind.Medkit, new Vector3(127f, 4.8f, 0f));

            // ---------- CHECKPOINTS ----------
            Checkpoint.Create(new Vector3(10f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(86f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(133f, 1.8f, 0f));
            Checkpoint.Create(new Vector3(154f, 1.8f, 0f));

            // ---------- RELÉS DE SEGURIDAD ----------
            RelaySwitch c1 = RelaySwitch.Create(new Vector3(98f, 1.7f, 0f), "C1");
            RelaySwitch c2 = RelaySwitch.Create(new Vector3(124f, 4.2f, 0f), "C2");
            DnaGateGate gate = DnaGateGate.Create(new Vector3(147f, 3.15f, 0f));
            GeneratorPuzzle.Create(c1, c2, gate);
            foreach (var sr in gate.GetComponentsInChildren<SpriteRenderer>()) sr.sprite = Sp("door_industrial");

            // ---------- JUGADOR, NOVA Y CÁMARA ----------
            data.Player = EiraController.Create(data.Spawn);
            data.Nova = NovaController.Create();
            data.Nova.Player = data.Player;

            var novaTalk = data.Nova.GetComponent<NovaTalk>();
            if (novaTalk != null)
            {
                novaTalk.TipLines = new[]
                {
                    "El núcleo respira. Si Kael lo agarra, el mundo entero queda en sus manos.",
                    "Los relés de seguridad sellan la sala del jefe. Actívalos en orden para abrir la puerta final.",
                    "Solo tú puedes tocar el Núcleo y elegir. Tómate tu tiempo."
                };
            }

            SetCamera(data, new Color(0.02f, 0.01f, 0.05f, 1f), new Vector2(-8f, -3f), new Vector2(200f, 12f));

            // ---------- ENEMIGOS ----------
            AndroidSoldier a1 = AndroidSoldier.Create(new Vector3(32f, 1.9f, 0f), new Vector2(24f, 1.9f), new Vector2(46f, 1.9f));
            a1.AttachPlayer(data.Player);
            AndroidSoldier a2 = AndroidSoldier.Create(new Vector3(80f, 1.9f, 0f), new Vector2(78f, 1.9f), new Vector2(138f, 1.9f));
            a2.AttachPlayer(data.Player);

            Drone d1 = Drone.Create(new Vector3(34f, 2.6f, 0f), new Vector2(28f, 2.6f), new Vector2(44f, 2.6f));
            d1.AttachPlayer(data.Player);
            Drone d2 = Drone.Create(new Vector3(112f, 5f, 0f), new Vector2(106f, 5f), new Vector2(122f, 5f));
            d2.AttachPlayer(data.Player);

            // ---------- ARENA FINAL: KAEL ----------
            GameObject gateL = MakeGate(149f);
            GameObject gateR = MakeGate(194f);
            var colL = gateL.GetComponent<BoxCollider2D>();
            var colR = gateR.GetComponent<BoxCollider2D>();
            var srL = gateL.GetComponent<SpriteRenderer>();
            var srR = gateR.GetComponent<SpriteRenderer>();

            KaelBoss kael = KaelBoss.Create(new Vector3(174f, 2.6f, 0f), 12, false);
            kael.Player = data.Player;
            kael.gameObject.SetActive(false);

            TriggerZone.Create(new Vector2(151.5f, 4f), new Vector2(2f, 10f), "KaelArena").OnEnter = _ =>
            {
                gateL.SetActive(true);
                gateR.SetActive(true);
                colL.enabled = true;
                colR.enabled = true;
                kael.Activate();
                Sfx.Play("door");
            };

            kael.OnDefeated = () =>
            {
                srL.sprite = Sp("door_industrial_open");
                srR.sprite = Sp("door_industrial_open");
                colL.enabled = false;
                colR.enabled = false;
                Sfx.Play("door");
                GameManager.I.AddPoints(5000); // recompensa del sector
                CoreTerminal.Create(new Vector3(173f, 1.8f, 0f), 0.55f);
                GameManager.I.SetObjective("⇒ TOCA EL NÚCLEO CENTRAL  (E).  Decide con 1, 2 o 3.");
                GameManager.I.UI?.ShowBanner("EL NÚCLEO CENTRAL SE HA LIBERADO");
                var lines = new List<DialogueLine>
                {
                    new DialogueLine("NOVA", "Lo hicimos. El corazón del núcleo ahora escucha tu sangre, Eira."),
                    new DialogueLine("NÚCLEO CENTRAL", "…PORTADORA DETECTADA… EL CONTROL PUEDE SER OTORGADO…"),
                    new DialogueLine("EIRA", "NOVA… ¿qué debo elegir?"),
                    new DialogueLine("NOVA", "Tres futuros. Solo tú puedes decidir cuál merece la pena."),
                };
                GameManager.I.PlayDialogue(lines, () =>
                {
                    GameManager.I.SetObjective("⇒ TOCA EL NÚCLEO CENTRAL  (E).  Decide con 1, 2 o 3.");
                });
            };

            return data;
        }
    }
}