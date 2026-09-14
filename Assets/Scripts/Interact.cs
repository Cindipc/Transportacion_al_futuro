using System.Collections.Generic;
using UnityEngine;

namespace EiraGame
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(EiraController player);
    }

    // ============ TERMINAL ============
    public class Terminal : MonoBehaviour, IInteractable
    {
        public string Title = "TERMINAL";
        public string[] Lines;
        public string ObjectiveAfter;
        public int Points = 50;
        public bool Used { get; private set; }
        private SpriteRenderer sr;
        private static Sprite sp;

        private Sprite Spr => sp != null ? sp : (sp = Resources.Load<Sprite>("Sprites/terminal_on"));

        public string Prompt => Used ? "Terminal (archivo ya leído)" : "[E] Leer terminal";

        public static Terminal Create(Vector3 pos, string title, params string[] lines)
        {
            var go = new GameObject("Terminal");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/terminal_on");
            sr.sortingOrder = 2;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            World.Register(go);
            var t = go.AddComponent<Terminal>();
            t.Title = title;
            t.Lines = lines;
            return t;
        }

        public void Interact(EiraController player)
        {
            if (Used)
            {
                GameManager.I.UI.ShowShadowToast("No hay más datos en este archivo.");
                return;
            }
            Used = true;
            GameManager.I.AddPoints(Points);
            var list = new List<DialogueLine>();
            list.Add(new DialogueLine(Title, string.Join("\n", Lines)));
            list.Add(new DialogueLine("EIRA", "... debo seguir adelante."));
            GameManager.I.PlayDialogue(list, null);
            if (!string.IsNullOrEmpty(ObjectiveAfter))
                GameManager.I.SetObjective(ObjectiveAfter);
            Sfx.Play("chip");
        }
    }

    // ============ PUERTA ADN ============
    public class DnaDoor : MonoBehaviour
    {
        private static bool powerMomentDone;

        public bool IsOpen { get; private set; }
        private SpriteRenderer sr;
        private BoxCollider2D bc;

        public static DnaDoor Create(Vector3 pos)
        {
            var go = new GameObject("DnaDoor");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/door_locked");
            sr.sortingOrder = 4;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(2.4f, 4.3f);
            bc.offset = new Vector2(0f, 0f);
            var d = go.AddComponent<DnaDoor>();
            d.sr = sr;
            d.bc = bc;
            return d;
        }

        public void Unlock()
        {
            if (IsOpen) return;
            IsOpen = true;
            sr.sprite = Resources.Load<Sprite>("Sprites/door_industrial_open");
            bc.enabled = false;
            Fx.Spark(transform.position, 1f);
            Fx.Spark(transform.position + new Vector3(0f, 2f, 0f), 0.8f);
            Sfx.Play("door");
            GameManager.I.UI.ShowShadowToast("La puerta reconoce tu señal genética.");
            if (!powerMomentDone)
            {
                powerMomentDone = true;
                FirstPower(transform.position);
            }
        }

        // La historia: durante el primer contacto, la puerta corta la mano de Eira
        // y una gota de sangre activa la máquina. Aquí descubre su poder por primera vez.
        private void FirstPower(Vector3 at)
        {
            Fx.SparkBurst(at, 12, new Color(1f, 0.18f, 0.16f));
            Fx.SparkBurst(at + new Vector3(0f, 1.8f, 0f), 7, new Color(1f, 0.75f, 0.15f));
            Sfx.Play("boss");
            var gm = GameManager.I;
            if (gm == null) return;
            gm.PlayDialogue(new List<DialogueLine>
            {
                new DialogueLine("EIRA", "¡…! ¿Qué ha sido eso? ¡Mi mano! Sentí como si algo me cortara."),
                new DialogueLine("EIRA", "Esta puerta no pide una llave… pide sangre."),
                new DialogueLine("NOVA", "Tu sangre ha encendido la máquina, Eira."),
                new DialogueLine("EIRA", "¿Por qué… por qué mi sangre puede hacer esto?"),
                new DialogueLine("NOVA", "Por eso Kael siempre te buscó. Tú no eres solo una superviviente… eres la llave del Núcleo."),
            }, () => gm.SetObjective("Cruza la puerta y explora el laboratorio contando tus pistas."));
        }
    }

    // ============ RELÉ / GENERADORES ============
    public class RelaySwitch : MonoBehaviour, IInteractable
    {
        public bool On { get; private set; }
        public GeneratorPuzzle Puzzle;
        public string nameId;
        private SpriteRenderer sr;

        public string Prompt => On ? "Relé activado" : "[E] Activar relé de energía";

        public static RelaySwitch Create(Vector3 pos, string id)
        {
            var go = new GameObject("Relay_" + id);
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/relay_off");
            sr.sortingOrder = 3;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var r = go.AddComponent<RelaySwitch>();
            r.nameId = id;
            r.sr = sr;
            return r;
        }

        public void Interact(EiraController player)
        {
            if (On) return;
            On = true;
            sr.sprite = Resources.Load<Sprite>("Sprites/relay_on");
            Sfx.Play("beep");
            Fx.Spark(transform.position, 0.7f);
            if (Puzzle != null) Puzzle.OnRelayActivated(this);
        }
    }

    public class GeneratorPuzzle : MonoBehaviour
    {
        public RelaySwitch[] Relays;
        public DnaGateGate Gate;

        public static GeneratorPuzzle Create(RelaySwitch r1, RelaySwitch r2, DnaGateGate gate)
        {
            var g = new GameObject("GeneratorPuzzle").AddComponent<GeneratorPuzzle>();
            World.Register(g.gameObject);
            g.Relays = new[] { r1, r2 };
            g.Gate = gate;
            r1.Puzzle = g;
            r2.Puzzle = g;
            return g;
        }

        public void OnRelayActivated(RelaySwitch r)
        {
            int on = 0;
            foreach (var x in Relays) if (x != null && x.On) on++;
            if (on >= Relays.Length && Gate != null)
            {
                GameManager.I.AddPoints(100);
                GameManager.I.UI.ShowShadowToast("Acertijo resuelto: la barrera se abre (+100)");
                Gate.OpenGate();
                GameManager.I.SetObjective("Cruza el sector evitando a los drones.");
                Sfx.Play("door");
            }
            else if (on < Relays.Length)
            {
                GameManager.I.UI.ShowShadowToast("Relé activado (" + on + "/" + Relays.Length + "). Busca los demás.");
            }
        }
    }

    // Barrera que se abre cuando ambos relés están activos
    public class DnaGateGate : MonoBehaviour
    {
        public bool Open { get; private set; }
        private SpriteRenderer sr;
        private BoxCollider2D bc;

        public static DnaGateGate Create(Vector3 pos)
        {
            var go = new GameObject("Gate");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/door_locked");
            sr.sortingOrder = 4;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(2.4f, 4.3f);
            var g = go.AddComponent<DnaGateGate>();
            g.sr = sr;
            g.bc = bc;
            return g;
        }

        public void OpenGate()
        {
            if (Open) return;
            Open = true;
            sr.sprite = Resources.Load<Sprite>("Sprites/door_industrial_open");
            bc.enabled = false;
            Fx.Spark(transform.position, 1f);
            Sfx.Play("door");
        }
    }

    // ============ RECOGIBLES ============
    public class Pickup : MonoBehaviour
    {
        public enum Kind { Medkit, Capsule, Chip }
        public Kind kind;
        public System.Action<Pickup> OnCollected;
        private bool taken;

        public static Pickup Create(Kind k, Vector3 pos)
        {
            var go = new GameObject("Pickup_" + k);
            go.transform.position = pos;
            World.Register(go);
            string sprName = k == Kind.Medkit ? "medkit" : (k == Kind.Capsule ? "capsule" : "chip");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/" + sprName);
            sr.sortingOrder = 5;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(0.55f, 0.55f);
            var p = go.AddComponent<Pickup>();
            p.kind = k;
            return p;
        }

        private void Update()
        {
            transform.position += new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.02f, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e == null || taken) return;
            taken = true;
            Fx.Spark(transform.position, 0.7f);
            switch (kind)
            {
                case Kind.Medkit:
                    e.Heal(2);
                    GameManager.I.UI.ShowShadowToast("+ Botiquín tecnológico: recuperas vida");
                    Sfx.Play("pickup");
                    break;
                case Kind.Capsule:
                    e.RestoreEnergy(100f);
                    e.Heal(1);
                    GameManager.I.UI.ShowShadowToast("+ Cápsula de energía: corazón estabilizado");
                    Sfx.Play("pickup");
                    break;
                case Kind.Chip:
                    GameManager.I.AddPoints(50);
                    GameManager.I.UI.ShowShadowToast("+50 · Datos recuperados");
                    Sfx.Play("chip");
                    break;
            }
            OnCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }

    // ============ CHECKPOINT ============
    public class Checkpoint : MonoBehaviour
    {
        private SpriteRenderer sr;
        private bool first = true;

        public static Checkpoint Create(Vector3 pos)
        {
            var go = new GameObject("Checkpoint");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/checkpoint");
            sr.sortingOrder = 5;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(0.8f, 1f);
            var cp = go.AddComponent<Checkpoint>();
            cp.sr = sr;
            return cp;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e == null) return;
            e.SetRespawnPoint(transform.position + new Vector3(0f, -0.8f, 0f));
            if (first)
            {
                first = false;
                GameManager.I.UI.ShowShadowToast("Punto de control activado.");
                Sfx.Play("beep");
            }
        }
    }

    // ============ PINCHOS ============
    public class SpikeZone : MonoBehaviour
    {
        public static SpikeZone Create(Vector3 pos, Vector2 size)
        {
            var go = new GameObject("Spikes");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/spikes");
            sr.sortingOrder = 2;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = size;
            var z = go.AddComponent<SpikeZone>();
            return z;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null)
            {
                e.TakeDamage(2);
                Sfx.Play("hit");
            }
        }
    }

    // ============ HABLAR CON NOVA ============
    public class NovaTalk : MonoBehaviour, IInteractable
    {
        public string[] TipLines;
        private bool used;

        public string Prompt => "[E] Hablar con NOVA";

        public static NovaTalk Attach(GameObject nova)
        {
            var t = nova.AddComponent<NovaTalk>();
            return t;
        }

        public void Interact(EiraController player)
        {
            var list = new List<DialogueLine>();
            if (TipLines != null && TipLines.Length > 0 && !used)
            {
                used = true;
                foreach (var l in TipLines) list.Add(new DialogueLine("NOVA", l));
            }
            else
            {
                list.Add(new DialogueLine("NOVA", "El objetivo está claro, Eira: salir de aquí. Si necesitas arriesgarte demasiado, yo vigilo tu corazón."));
            }
            GameManager.I.PlayDialogue(list, null);
        }
    }

    // ============ RESCATAR ANDROIDE REBELDE (NIVEL 2) ============
    public class RescueRebel : MonoBehaviour, IInteractable
    {
        private const int Points = 200;
        public bool Done { get; private set; }
        public System.Action<RescueRebel> OnRescued;
        private static Sprite sp;

        public string Prompt => Done ? "Androide rebelde a salvo" : "[E] Ayudar al androide rebelde";

        private static Sprite Spr => sp != null ? sp : (sp = Resources.Load<Sprite>("Sprites/rescue"));

        public static RescueRebel Create(Vector3 pos)
        {
            var go = new GameObject("RescueRebel");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr;
            sr.sortingOrder = 2;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            return go.AddComponent<RescueRebel>();
        }

        public void Interact(EiraController player)
        {
            if (Done) return;
            Done = true;
            GameManager.I.AddPoints(Points);
            GameManager.I.UI.ShowShadowToast("+200 · Rebelde a salvo");
            Fx.SparkBurst(transform.position, 8, Color.green);
            Sfx.Play("chip");
            OnRescued?.Invoke(this);
        }
    }

    // ============ NÚCLEO CENTRAL (NIVEL 3, FINAL) ============
    public class CoreTerminal : MonoBehaviour, IInteractable
    {
        public bool Resolved { get; private set; }
        private bool channeling;

        public string Prompt => channeling ? "Elige: [1] HUMANOS   [2] ANDROIDES   [3] LIBERTAD" : "[E] Entrar en el Núcleo Central";

        public static CoreTerminal Create(Vector3 pos, float scale)
        {
            var go = new GameObject("CoreTerminal");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/origin_core");
            sr.sortingOrder = 2;
            sr.transform.localScale = Vector3.one * scale;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(4.2f, 4.2f);
            return go.AddComponent<CoreTerminal>();
        }

        public void Interact(EiraController player)
        {
            if (Resolved || channeling) return;
            channeling = true;
            GameManager.I.SetObjective("[NÚCLEO] 1 · HUMANOS   2 · ANDROIDES   3 · LIBERTAD");
            var list = new List<DialogueLine>();
            list.Add(new DialogueLine("NÚCLEO CENTRAL", "…¿A QUIÉN DESEA DAR EL CONTROL?…"));
            list.Add(new DialogueLine("NÚCLEO CENTRAL", "[1] HUMANOS\n[2] ANDROIDES\n[3] LIBERTAD"));
            GameManager.I.PlayDialogue(list, () =>
            {
                GameManager.I.UI.ShowShadowToast("Pulsa 1, 2 o 3 con el teclado");
                GameManager.I.SetObjective("[NÚCLEO] 1 · HUMANOS   2 · ANDROIDES   3 · LIBERTAD");
            });
        }

        private void Update()
        {
            if (!channeling) return;
            if (GameManager.I != null && GameManager.I.DialogueBusy) return;
            if (Inputs.Number(1)) Reject("HUMANOS… elegir un bando repetiría cien años de la misma historia.");
            else if (Inputs.Number(2)) Reject("ANDROIDES… lo mismo, pero al revés. Eira no es la llave de un bando: es la llave de la libre decisión.");
            else if (Inputs.Number(3))
            {
                channeling = false;
                Resolved = true;
                Fx.Spark(transform.position, 30, Color.cyan);
                Fx.Spark(transform.position + Vector3.up * 3f, 20, new Color(1f, 1f, 0.5f));
                GameManager.I.FinishGame();
            }
        }

        private void Reject(string msg)
        {
            channeling = false;
            var list = new List<DialogueLine>();
            list.Add(new DialogueLine("NÚCLEO CENTRAL", msg));
            list.Add(new DialogueLine("NOVA", "Eira… no es eso lo que tu corazón quiere. Escúchalo."));
            GameManager.I.PlayDialogue(list, () =>
            {
                channeling = true;
                GameManager.I.UI.ShowShadowToast("Pulsa 1, 2 o 3 con el teclado");
                GameManager.I.SetObjective("[NÚCLEO] 1 · HUMANOS   2 · ANDROIDES   3 · LIBERTAD");
            });
        }
    }
}