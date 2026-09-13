using System.Collections;
using UnityEngine;

namespace EiraGame
{
    public class Drone : MonoBehaviour
    {
        public Vector2[] Waypoints;
        private int wp;
        private float speed = 2.6f;
        private const float Detect = 4.2f;
        private const float AttackRange = 1.7f;
        private int hp = 1;
        private float cd;
        private float stunnedT;
        private bool dead;
        private bool alert;

        private EiraController player;
        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private Sprite idleSpr, alertSpr;

        public static Drone Create(Vector3 pos, params Vector2[] wps)
        {
            var go = new GameObject("Drone");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/drone_idle");
            sr.sortingOrder = 8;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.6f;
            var d = go.AddComponent<Drone>();
            d.sr = sr;
            d.rb = rb;
            d.idleSpr = Resources.Load<Sprite>("Sprites/drone_idle");
            d.alertSpr = Resources.Load<Sprite>("Sprites/drone_alert");
            d.Waypoints = wps;
            return d;
        }

        public void AttachPlayer(EiraController p) => player = p;

        private void Start()
        {
            if (Waypoints == null || Waypoints.Length == 0)
            {
                var p = (Vector2)transform.position;
                Waypoints = new[] { p, p + new Vector2(2f, 0f) };
            }
        }

        private void Update()
        {
            if (dead || player == null || rb == null) return;

            float dt = Time.deltaTime;
            if (stunnedT > 0f)
            {
                stunnedT -= dt;
                rb.linearVelocity = Vector2.zero;
                sr.sprite = alertSpr;
                return;
            }

            float d = Vector2.Distance(player.transform.position, transform.position);
            if (d < Detect)
            {
                alert = true;
                rb.linearVelocity = ((Vector2)player.transform.position - (Vector2)transform.position + new Vector2(0f, 0.4f)).normalized * (speed * 1.7f);
            }
            else
            {
                alert = false;
                Patrol();
            }

            sr.sprite = alert ? alertSpr : idleSpr;

            cd -= dt;
            if (alert && cd <= 0f && d < AttackRange)
            {
                cd = 1.4f;
                player.TakeDamage(2);
                Fx.Spark(transform.position, 0.6f);
                Sfx.Play("hit");
            }
        }

        private void Patrol()
        {
            Vector2 target = Waypoints[wp];
            Vector2 to = target - (Vector2)transform.position;
            if (to.magnitude < 0.3f)
            {
                wp = (wp + 1) % Waypoints.Length;
            }
            else
            {
                to.Normalize();
                rb.linearVelocity = to * speed;
            }
        }

        public void TakeHit(int dmg, Vector3 from)
        {
            if (dead) return;
            hp -= dmg;
            stunnedT = 0.35f;
            Fx.Spark(from, 0.7f);
            Sfx.Play("hit");
            float back = transform.position.x < from.x ? -1f : 1f;
            rb.linearVelocity = new Vector2(back * 3f, 2f);
            if (hp <= 0) Die();
        }

        private void Die()
        {
            dead = true;
            GameManager.I.AddPoints(100);
            GameManager.I.UI.ShowShadowToast("+100 · Máquina desactivada");
            Fx.BigSpark(transform.position);
            Sfx.Play("boom");
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null) e.TakeDamage(1);
        }
    }

    public class GuardianBoss : MonoBehaviour
    {
        private const int MaxHp = 8;
        private int hp = MaxHp;
        private bool dormant = true;
        private bool dead;

        public EiraController Player;
        public System.Action OnDefeated;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Sprite idleSpr, attackSpr;
        private float stateT;
        private float chaseSpeed = 2.2f;
        private float breatheT;
        private bool shooting;
        private float contactCd;

        public static GuardianBoss Create(Vector3 pos)
        {
            var go = new GameObject("GuardianBoss");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(4f, 3.4f);
            var b = go.AddComponent<GuardianBoss>();
            b.sr = sr;
            b.rb = rb;
            b.idleSpr = Resources.Load<Sprite>("Sprites/guardian_idle");
            b.attackSpr = Resources.Load<Sprite>("Sprites/guardian_attack");
            sr.sprite = b.idleSpr;
            return b;
        }

        public void Activate(bool full)
        {
            if (dormant && !full) return;
            dormant = false;
            gameObject.SetActive(true);
            GameManager.I.Cam?.SetTargetZoom(9f);
            GameManager.I.UI?.ShowBanner("¡JEFE · MÁQUINA GUARDIANA!");
            GameManager.I.SetObjective("Activa tu pulso para dañar a la Máquina Guardiana (X).");
            Sfx.Play("boss");
        }

        private void Update()
        {
            if (contactCd > 0f) contactCd -= Time.deltaTime;
            if (dead) return;

            if (dormant)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                sr.sprite = idleSpr;
                return;
            }
            if (Player == null || GameManager.I == null || GameManager.I.DialogueBusy) return;

            float dt = Time.deltaTime;
            stateT += dt;
            breatheT += dt;

            if (stateT < 1.6f)
            {
                Vector2 toP = ((Vector2)Player.transform.position - (Vector2)transform.position);
                rb.linearVelocity = toP.normalized * chaseSpeed;
                shooting = false;
                sr.sprite = idleSpr;
            }
            else if (stateT < 2.6f)
            {
                rb.linearVelocity = Vector2.zero;
                shooting = true;
            }
            else
            {
                stateT = 0f;
                ShootBurst();
            }

            if (shooting && idleSpr != null && attackSpr != null)
                sr.sprite = (breatheT * 12f) % 1f < 0.5f ? attackSpr : idleSpr;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Sin(breatheT * 2f) * 0.3f);
        }

        private void ShootBurst()
        {
            StartCoroutine(Burst());
        }

        private IEnumerator Burst()
        {
            Sfx.Play("pulse");
            for (int i = 0; i < 3; i++)
            {
                if (dead || dormant) yield break;
                Vector2 toP = (Vector2)Player.transform.position - (Vector2)transform.position;
                toP.Normalize();
                Vector3 from = transform.position + (Vector3)(toP * 2.2f) + new Vector3(0f, 0.2f, 0f);
                Projectile.Spawn(from, toP, "enemy", 2, 6f, "pulse", 3f, 0.55f);
                sr.sprite = attackSpr;
                yield return new WaitForSeconds(0.15f);
            }
        }

        public void TakeHit(int dmg, Vector3 from)
        {
            if (dead || dormant) return;
            hp -= dmg;
            Fx.Spark(from, 1.2f);
            Sfx.Play("hit");
            StartCoroutine(Flash());
            if (hp <= 0) Die();
        }

        private IEnumerator Flash()
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 1f);
            yield return new WaitForSeconds(0.12f);
            sr.color = Color.white;
        }

        private void Die()
        {
            dead = true;
            StopAllCoroutines();
            StartCoroutine(Dying());
        }

        private IEnumerator Dying()
        {
            Sfx.Play("boom");
            for (int i = 0; i < 4; i++)
            {
                Fx.BigSpark(transform.position + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-1.5f, 2f), 0f));
                yield return new WaitForSeconds(0.28f);
            }
            Fx.BigSpark(transform.position);
            GameManager.I.AddPoints(500);
            GameManager.I.UI.ShowShadowToast("+500 · Máquina Guardiana desactivada");
            OnDefeated?.Invoke();
            GameManager.I.Cam?.RestoreZoom();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null && contactCd <= 0f)
            {
                contactCd = 1.2f;
                e.TakeDamage(3);
                Sfx.Play("boom");
            }
        }
    }

    // ============ SOLDADO ANDROIDE (NIVEL 2) ============
    public class AndroidSoldier : MonoBehaviour
    {
        public Vector2[] Waypoints;
        private int wp;
        private float speed = 1.9f;
        private const float Detect = 4.8f;
        private const float AttackRange = 1.6f;
        private float meleeCd;
        private int hp = 2;
        private float stun;
        private bool dead;
        private float homeY;

        private EiraController player;
        private SpriteRenderer sr;
        private Rigidbody2D rb;
        private Sprite idleSpr, alertSpr;

        public static AndroidSoldier Create(Vector3 pos, params Vector2[] wps)
        {
            var go = new GameObject("Android");
            go.transform.position = pos;
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(0.8f, 1.8f);
            var a = go.AddComponent<AndroidSoldier>();
            a.sr = sr;
            a.rb = rb;
            a.idleSpr = Resources.Load<Sprite>("Sprites/android_idle");
            a.alertSpr = Resources.Load<Sprite>("Sprites/android_alert");
            sr.sprite = a.idleSpr;
            a.Waypoints = wps;
            return a;
        }

        public void AttachPlayer(EiraController p) => player = p;

        private void Start()
        {
            homeY = transform.position.y;
            if (Waypoints == null || Waypoints.Length == 0)
            {
                var p = (Vector2)transform.position;
                Waypoints = new[] { p, p + new Vector2(2f, 0f) };
            }
        }

        private void Update()
        {
            if (dead || player == null || rb == null) return;
            if (stun > 0f) { stun -= Time.deltaTime; return; }

            bool chasing = Vector2.Distance(transform.position, player.transform.position) < Detect;
            sr.sprite = chasing ? alertSpr : idleSpr;

            Vector3 target;
            if (chasing)
            {
                target = player.transform.position;
                target.y = homeY;
            }
            else
            {
                target = new Vector3(Waypoints[wp].x, homeY, 0f);
                if (Vector2.Distance(transform.position, target) < 0.15f)
                    wp = (wp + 1) % Waypoints.Length;
            }

            Vector2 dir = (target - transform.position);
            dir.y = 0f;
            Vector2 vel = dir.normalized * (chasing ? speed * 1.6f : speed);
            rb.linearVelocity = new Vector2(vel.x, 0f);

            if (chasing)
            {
                float dx = player.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) < AttackRange && meleeCd <= 0f)
                {
                    meleeCd = 1.1f;
                    player.TakeDamage(2);
                    Sfx.Play("boom");
                    Fx.SparkBurst(transform.position + new Vector3(Mathf.Sign(dx) * 0.7f, 0.6f, 0f), 4, Color.yellow);
                }
            }
            if (meleeCd > 0f) meleeCd -= Time.deltaTime;
        }

        public void TakeHit(int dmg, Vector3 from)
        {
            if (dead) return;
            hp -= dmg;
            stun = 0.35f;
            Fx.Spark(from, 0.7f);
            Sfx.Play("hit");
            if (hp <= 0) Die();
        }

        private void Die()
        {
            dead = true;
            GameManager.I.AddPoints(100);
            GameManager.I.UI.ShowShadowToast("+100 · Soldado androide desactivado");
            Fx.BigSpark(transform.position);
            Sfx.Play("boom");
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null && meleeCd <= 0f)
            {
                meleeCd = 1.1f;
                e.TakeDamage(1);
            }
        }
    }

    // ============ KAEL (JEFE, NIVEL 2 Y 3) ============
    public class KaelBoss : MonoBehaviour
    {
        private int maxHp;
        private int hp;
        private bool scripted;
        private bool active;
        private bool dead;
        private float scriptedT;
        private float summonT;
        private float shotCd;
        private float contactCd;
        private float breatheT;
        private bool inv = true;

        public EiraController Player;
        public System.Action OnDefeated;
        public bool Scripted { get => scripted; }

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Sprite idleSpr, attackSpr;

public KaelBoss() { }

        public static KaelBoss Create(Vector3 pos, int maxHp, bool isScripted)
        {
            var go = new GameObject("Kael");
            World.Register(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(1.6f, 3.2f);
            var k = go.AddComponent<KaelBoss>();
            k.sr = sr;
            k.rb = rb;
            k.idleSpr = Resources.Load<Sprite>("Sprites/kael_idle");
            k.attackSpr = Resources.Load<Sprite>("Sprites/kael_attack");
            sr.sprite = k.idleSpr;
            k.maxHp = maxHp;
            k.hp = maxHp > 0 ? maxHp : 9999;
            k.scripted = isScripted;
            return k;
        }

        public void Activate()
        {
            active = true;
            inv = scripted;
            gameObject.SetActive(true);
            GameManager.I.Cam?.SetTargetZoom(9f);
            GameManager.I.UI?.ShowBanner("¡¡KAEL!!");
            GameManager.I.SetObjective("Sobrevive y daña a KAEL con tu pulso (X).");
            Sfx.Play("boss");
        }

        private void Update()
        {
            if (dead || !active || Player == null || rb == null) return;

            if (scripted)
            {
                scriptedT += Time.deltaTime;
                if (scriptedT > 15f) ScriptedEnd();
                Chase(false);
            }
            else
            {
                Chase(true);

                if (inv)
                {
                    inv = false;
                    sr.color = new Color(1f, 0.6f, 0.6f, 1f);
                    Sfx.Play("hit");
                }

                if (hp <= maxHp / 2)
                {
                    summonT += Time.deltaTime;
                    if (summonT > 6f)
                    {
                        summonT = 0f;
                        SpawnMinion();
                    }
                }
            }
        }

        private void Chase(bool lethal)
        {
            Vector2 target = Player.transform.position;
            Vector2 pos = transform.position;
            float d = Vector2.Distance(pos, target);
            Vector2 dir = (target - pos).normalized;
            rb.linearVelocity = dir * 3.1f;

            if (lethal && shotCd <= 0f && d < 8f)
            {
                shotCd = Random.Range(1.3f, 2.1f);
                Projectile.Spawn(pos + dir * 1.4f, dir, "kael", 2, 8f, "spark", 4f, 0.9f);
            }
            else if (!lethal)
            {
                shotCd -= Time.deltaTime;
            }
            if (shotCd > 0f) shotCd -= Time.deltaTime;

            sr.sprite = d < 3f ? attackSpr : idleSpr;
        }

        private void SpawnMinion()
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-2.6f, 2.6f), 2.4f, 0f);
            if (hp <= maxHp / 4)
            {
                var a = AndroidSoldier.Create(p);
                a.AttachPlayer(Player);
                GameManager.I.UI?.ShowShadowToast("KAEL llama refuerzos…");
            }
            else
            {
                var dr = Drone.Create(p);
                dr.AttachPlayer(Player);
                GameManager.I.UI?.ShowShadowToast("KAEL llama a un dron de combate…");
            }
            Sfx.Play("boom");
        }

        private void ScriptedEnd()
        {
            dead = true;
            Sfx.Play("boom");
            for (int i = 0; i < 3; i++)
                Fx.BigSpark(transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 2f), 0f));
            OnDefeated?.Invoke();
            GameManager.I.Cam?.RestoreZoom();
            Destroy(gameObject);
        }

        public void TakeHit(int dmg, Vector3 from)
        {
            if (!active || dead) return;
            Fx.Spark(from, 1.2f);
            Sfx.Play("hit");
            if (scripted) { sr.color = Color.red; Invoke(nameof(RestoreColor), 0.12f); return; }

            hp -= dmg;
            sr.color = Color.red;
            Invoke(nameof(RestoreColor), 0.12f);
            if (hp <= 0) Die();
        }

        private void RestoreColor() => sr.color = Color.white;

        private void Die()
        {
            dead = true;
            StopAllCoroutines();
            StartCoroutine(DyingKael());
        }

        private System.Collections.IEnumerator DyingKael()
        {
            Sfx.Play("boom");
            for (int i = 0; i < 5; i++)
            {
                Fx.BigSpark(transform.position + new Vector3(Random.Range(-2.8f, 2.8f), Random.Range(-2f, 2.2f), 0f));
                yield return new WaitForSeconds(0.26f);
            }
            GameManager.I.AddPoints(2000);
            GameManager.I.UI.ShowShadowToast("+2000 · KAEL derrotado");
            OnDefeated?.Invoke();
            GameManager.I.Cam?.RestoreZoom();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var e = other.GetComponent<EiraController>();
            if (e != null && contactCd <= 0f)
            {
                contactCd = 1.2f;
                e.TakeDamage(hp <= maxHp / 4 ? 3 : 2);
                Sfx.Play("boom");
            }
        }
    }
}