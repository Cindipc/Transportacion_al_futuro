using System.Collections.Generic;
using UnityEngine;

namespace EiraGame
{
    public class EiraController : MonoBehaviour
    {
        private Rigidbody2D rb;
        private SpriteRenderer spr;
        private BoxCollider2D col;

        private Sprite idleSprite, jumpSprite, hurtSprite;
        private Sprite[] runSprites = new Sprite[3];

        private const float MoveSpeed = 6.8f;
        private const float JumpSpeed = 13.5f;
        private const int MaxHp = 4;
        private const float MaxEnergy = 100f;
        private const float EnergyRegen = 9f;
        private const float PulseCost = 13f;
        private const float PulseCooldown = 0.35f;

        private int hp = MaxHp;
        private int lives = 3;
        private float energy = MaxEnergy;

        private bool control;
        private float animTimer;
        private int runIdx;
        private float pulseCd;
        private float hurtT;

        private bool grounded;
        private float jumpGrace;

        public bool FacingRight { get; private set; } = true;
        public int HP => hp;
        public int Lives => lives;
        public bool MarkedDamaged { get; set; }

        public static EiraController Create(Vector3 pos)
        {
            var go = new GameObject("Eira");
            go.transform.position = pos;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<SpriteRenderer>();
            World.Register(go);
            return go.AddComponent<EiraController>();
        }

        private static Sprite S(string name) => Resources.Load<Sprite>("Sprites/" + name);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            spr = GetComponent<SpriteRenderer>();
            idleSprite = S("eira_idle");
            jumpSprite = S("eira_jump");
            hurtSprite = S("eira_hurt");
            runSprites[0] = S("eira_run1");
            runSprites[1] = S("eira_run2");
            runSprites[2] = S("eira_run3");
        }

        private void Start()
        {
            rb.gravityScale = 4f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            col.size = new Vector2(0.85f, 1.55f);
            col.offset = new Vector2(0f, 0.75f);

            spr.sortingOrder = 10;
            spr.sprite = idleSprite;

            if (GameManager.I != null)
            {
                GameManager.I.NotifyHealth(hp, MaxHp, lives);
                GameManager.I.NotifyEnergy(energy, MaxEnergy);
            }
        }

        public void ResetForLevel(int livesCount)
        {
            hp = MaxHp;
            lives = livesCount;
            energy = MaxEnergy;
            MarkedDamaged = false;
            hurtT = 0f;
            control = true;
            if (GameManager.I != null)
            {
                GameManager.I.NotifyHealth(hp, MaxHp, lives);
                GameManager.I.NotifyEnergy(energy, MaxEnergy);
            }
        }

        public void SetControl(bool on)
        {
            control = on;
            if (!on && rb != null) rb.linearVelocity = Vector2.zero;
            if (on) hurtT = 0f;
        }

        private bool IsGrounded()
        {
            Vector2 p = (Vector2)transform.position + new Vector2(0f, -0.55f);
            var cols = Physics2D.OverlapBoxAll(p, new Vector2(0.7f, 0.35f), 0f);
            foreach (var c in cols)
            {
                if (c == col) continue;
                if (c.isTrigger) continue;
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (GameManager.I == null || GameManager.I.UI == null) return;

            bool busy = GameManager.I.DialogueBusy;
            float dt = Time.deltaTime;

            if (hurtT > 0f) hurtT -= dt;

            // Caída al vacío
            if (transform.position.y < -4f) { FallToDeath(); return; }

            // Regeneración de energía (más lenta en estado crítico)
            if (!busy)
            {
                float regen = EnergyRegen * (hp == 1 ? 0.55f : 1f);
                if (energy < MaxEnergy)
                {
                    energy += regen * dt;
                    if (energy > MaxEnergy) energy = MaxEnergy;
                }
            }

            if (control && !busy)
            {
                pulseCd -= dt;
                if (purplePulse() && pulseCd <= 0f) DoPulse();
                ScanInteractions();
            }

            GameManager.I.NotifyHealth(hp, MaxHp, lives);
            GameManager.I.NotifyEnergy(energy, MaxEnergy);
        }

        private bool purplePulse() => Inputs.Pulse();

        private void FixedUpdate()
        {
            bool busy = GameManager.I != null && GameManager.I.UI != null && GameManager.I.DialogueBusy;
            if (!control || busy || hurtT > 0.9f)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            float h = Inputs.MoveAxis();
            Vector2 vel = rb.linearVelocity;
            vel.x = h * MoveSpeed;

            grounded = IsGrounded();
            if (grounded) jumpGrace = 0.12f; else jumpGrace -= Time.fixedDeltaTime;

            if (Inputs.Jump() && (grounded || jumpGrace > 0f) && vel.y <= 0.2f)
            {
                vel.y = JumpSpeed;
                grounded = false;
                jumpGrace = 0f;
                Sfx.Play("jump");
            }

            rb.linearVelocity = vel;

            if (h > 0.05f) FacingRight = true;
            else if (h < -0.05f) FacingRight = false;
            spr.flipX = !FacingRight;

            // Animación
            if (needsHurtSprite())
            {
                spr.sprite = hurtSprite;
            }
            else if (!grounded)
            {
                spr.sprite = jumpSprite;
            }
            else if (Mathf.Abs(h) > 0.1f)
            {
                animTimer += Time.fixedDeltaTime;
                if (animTimer >= 0.11f) { animTimer = 0f; runIdx = (runIdx + 1) % 3; }
                spr.sprite = runSprites[runIdx];
            }
            else
            {
                spr.sprite = idleSprite;
            }
        }

        private bool needsHurtSprite() => hurtT > 0.55f;

        // ---------- HABILIDADES ----------
        private void DoPulse()
        {
            pulseCd = PulseCooldown;

            var door = ScanNear<DnaDoor>(3.8f);
            if (door != null && !door.IsOpen)
            {
                if (energy < 6f) { HeartStrain(); return; }
                energy -= 6f;
                door.Unlock();
                Sfx.Play("door");
                GameManager.I.AddPoints(100);
                GameManager.I.SetObjective("Explora las instalaciones. Busca pistas sobre el año 3000.");
                return;
            }

            bool ok = energy >= PulseCost;
            if (energy < PulseCost) energy = 0f;
            else energy -= PulseCost;

            Vector2 dir = FacingRight ? Vector2.right : Vector2.left;
            Vector3 fire = transform.position + (Vector3)dir * 1.0f + Vector3.up * 0.35f;
            Projectile.Spawn(fire, dir, "player", 2, 15f, "pulse", 0.6f, 0.55f);
            Sfx.Play("pulse");

            if (!ok) HeartStrain();
        }

        private void HeartStrain()
        {
            energy = 0f;
            MarkedDamaged = true;
            TakeDamage(1);
            Sfx.Play("heart");
            GameManager.I.UI?.ShowShadowToast("¡Tu corazón late con demasiada fuerza!");
        }

        // ---------- INTERACCIONES ----------
        private void ScanInteractions()
        {
            var closest = FindClosestInteractable();
            if (closest != null)
            {
                GameManager.I.UI.ShowInteract(closest.Prompt);
                if (Inputs.Interact()) closest.Interact(this);
            }
            else
            {
                GameManager.I.UI.HideInteract();
            }
        }

        private IInteractable FindClosestInteractable()
        {
            var cols = Physics2D.OverlapCircleAll(transform.position, 2.4f);
            IInteractable best = null;
            float bd = float.MaxValue;
            foreach (var c in cols)
            {
                if (c == col) continue;
                var ia = c.GetComponent<IInteractable>();
                if (ia == null) continue;
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bd) { bd = d; best = ia; }
            }
            return best;
        }

        private T ScanNear<T>(float radius) where T : Component
        {
            var cols = Physics2D.OverlapCircleAll(transform.position, radius);
            T best = null;
            float bd = float.MaxValue;
            foreach (var c in cols)
            {
                if (c == col) continue;
                var t = c.GetComponent<T>();
                if (t == null) continue;
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }

        // ---------- DAÑO / CURACIÓN ----------
        public void TakeDamage(int d)
        {
            if (!control || hurtT > 0.5f) return;
            hurtT = 1.1f;
            MarkedDamaged = true;
            hp -= d;
            Sfx.Play("hit");
            if (hp <= 0) LoseLife();
            else GameManager.I?.NotifyHealth(hp, MaxHp, lives);
        }

        public void LoseLife()
        {
            lives--;
            Sfx.Play("lose");
            if (lives <= 0) { GameManager.I.OnGameOver(); return; }
            hp = MaxHp;
            transform.position = GameManager.I.RespawnPoint;
            rb.linearVelocity = Vector2.zero;
            GameManager.I.NotifyHealth(hp, MaxHp, lives);
        }

        private void FallToDeath()
        {
            LoseLife();
        }

        public void Heal(int v)
        {
            hp = Mathf.Min(MaxHp, hp + v);
            GameManager.I?.NotifyHealth(hp, MaxHp, lives);
        }

        public void RestoreEnergy(float v)
        {
            energy = Mathf.Min(MaxEnergy, energy + v);
        }

        public void SetRespawnPoint(Vector3 p)
        {
            if (GameManager.I != null) GameManager.I.RespawnPoint = p;
        }

        public void TeleportTo(Vector3 p)
        {
            transform.position = p;
            rb.linearVelocity = Vector2.zero;
        }
    }

    // ================= NOVA =================
    public class NovaController : MonoBehaviour
    {
        private Rigidbody2D rb;
        private SpriteRenderer spr;
        private Sprite idle, w1, w2;
        private float bobT;

        public EiraController Player { get; set; }

        public static NovaController Create()
        {
            var go = new GameObject("NOVA");
            World.Register(go);
            var nova = go.AddComponent<NovaController>();
            go.AddComponent<NovaTalk>();
            return nova;
        }

        private static Sprite S(string name) => Resources.Load<Sprite>("Sprites/" + name);

        private void Start()
        {
            idle = S("nova_idle");
            w1 = S("nova_walk1");
            w2 = S("nova_walk2");

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;

            if (GetComponent<CircleCollider2D>() == null)
            {
                var c = gameObject.AddComponent<CircleCollider2D>();
                c.radius = 0.7f;
                c.isTrigger = true;
            }
            spr = GetComponent<SpriteRenderer>();
            if (spr == null) spr = gameObject.AddComponent<SpriteRenderer>();
            spr.sortingOrder = 9;
            spr.sprite = idle;
        }

        private void Update()
        {
            if (Player == null) return;
            if (GameManager.I != null && (GameManager.I.State == GameState.Win || GameManager.I.State == GameState.GameOver))
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            bobT += Time.deltaTime;
            float bob = Mathf.Sin(bobT * 2.2f) * 0.06f;

            Vector3 target = Player.transform.position + new Vector3(Player.FacingRight ? -2.2f : 2.2f, 1.7f + bob, 0f);
            Vector2 to = target - transform.position;
            float dist = to.magnitude;

            Vector2 vel = Vector2.zero;
            if (dist > 0.3f) vel = to.normalized * Mathf.Min(4.5f, dist * 3f);

            var kv = GetComponent<Rigidbody2D>();
            if (kv != null) kv.linearVelocity = vel;

            bool moving = dist > 0.4f;
            int f = (int)(Time.time * 8f) % 2;
            spr.sprite = moving ? (f == 0 ? w1 : w2) : idle;

            float flipAmp = Mathf.Clamp(Player.transform.position.x - transform.position.x, -1f, 1f);
            if (Mathf.Abs(flipAmp) > 0.05f) spr.flipX = flipAmp < 0f;
        }
    }

    // ================= PROYECTIL =================
    public class Projectile : MonoBehaviour
    {
        public string Team;
        private int dmg;
        private Vector2 dir;
        private float speed;
        private Collider2D col;
        private bool hit;

        public static Projectile Spawn(Vector2 pos, Vector2 d, string team, int damage, float spd, string sprite, float life, float scale)
        {
            var go = new GameObject("Proj_" + team);
            World.Register(go);
            go.transform.position = pos;
            var p = go.AddComponent<Projectile>();
            p.Team = team;
            p.dmg = damage;
            p.dir = d.normalized;
            p.speed = spd;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/" + sprite);
            sr.sortingOrder = 20;
            sr.color = team == "enemy" ? new Color(1f, 0.35f, 0.3f, 1f) : Color.white;
            go.transform.localScale = Vector3.one * scale;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.32f;
            p.col = c;

            rb.linearVelocity = p.dir * p.speed;
            Destroy(go, life);
            return p;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hit) return;

            if (Team == "player")
            {
                if (other.GetComponent<EiraController>() != null) return; // no auto-dañarse
                var dr = other.GetComponent<Drone>();
                if (dr != null) { dr.TakeHit(2, transform.position); HitFx(); return; }
                var bs = other.GetComponent<GuardianBoss>();
                if (bs != null) { bs.TakeHit(2, transform.position); HitFx(); return; }
                var ad = other.GetComponent<AndroidSoldier>();
                if (ad != null) { ad.TakeHit(2, transform.position); HitFx(); return; }
                var kb = other.GetComponent<KaelBoss>();
                if (kb != null) { kb.TakeHit(2, transform.position); HitFx(); return; }
                if (other.isTrigger) return; // props/recogibles ignorados
                HitFx();
            }
            else
            {
                var ei = other.GetComponent<EiraController>();
                if (ei != null) { ei.TakeDamage(dmg); HitFx(); return; }
                if (other.isTrigger) return;
                HitFx();
            }
        }

        private void HitFx()
        {
            hit = true;
            Fx.Spark(transform.position);
            Destroy(gameObject);
        }
    }
}