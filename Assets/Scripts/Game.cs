using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EiraGame
{
    public enum GameState { Title, Intro, Playing, Boss, Pause, Win, GameOver }

    public class DialogueLine
    {
        public string Speaker;
        public string Text;
        public DialogueLine(string sp, string tx) { Speaker = sp; Text = tx; }
    }

    /// <summary>Manejador global del juego: puntos, estado, UI y flujo.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public UIManager UI { get; private set; }
        public EiraController Player { get; private set; }
        public NovaController Nova { get; private set; }
        public CameraFollow Cam { get; private set; }

        public int Points { get; private set; }
        public int LevelIndex { get; private set; }
        public GameState State { get; private set; }

        public bool DialogueBusy => UI != null && UI.DialogueActive;

        public Vector3 RespawnPoint { get; set; }

        private void Awake()
        {
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Init(UIManager ui, EiraController player, NovaController nova, CameraFollow cam)
        {
            UI = ui;
            Player = player;
            Nova = nova;
            Cam = cam;
        }

        public void SetLevel(int idx) => LevelIndex = idx;

        public void ResetRun()
        {
            Points = 0;
            RespawnPoint = Vector3.zero;
            Time.timeScale = 1f;
            UI?.SetPoints(0);
        }

        public void AddPoints(int v)
        {
            Points += v;
            UI?.SetPoints(Points);
        }

        public void SetState(GameState s)
        {
            State = s;
            for (int i = 0; i < transform.childCount; i++) { }
            UI?.OnGameState(s);
            if (Player != null) Player.SetControl(s == GameState.Playing || s == GameState.Boss);
        }

        public void SetObjective(string s) => UI?.SetObjective(s);

        public void NotifyHealth(int hp, int maxHp, int lives)
            => UI?.SetHealth(hp, maxHp, lives);

        public void NotifyEnergy(float e, float maxE)
            => UI?.SetEnergy(e, maxE);

        public void OnWin(string epilogue, int bonus = 500)
        {
            AddPoints(bonus);
            SetState(GameState.Win);
            UI?.ShowWin(LevelWinTitle(), Points, epilogue, LevelWinHint());
            Sfx.Play("victory");
        }

        public void FinishGame()
        {
            AddPoints(5000); // LIBERTAD
            SetState(GameState.Win);
            string epi =
                "EIRA llega al Núcleo Central y su sangre desactiva el control de Kael.\n" +
                "Millones de androides recuperan su voluntad. La guerra termina.\n\n" +
                "El sacrificio consume su cuerpo. En sus brazos, NOVA una vez más...\n" +
                "\"Tengo miedo.\"\n" +
                "\"Eira... no vas a morir.\"\n\n" +
                "Cincuenta años después, una estatua domina la ciudad reconstruida.\n" +
                "Una joven humana con la mano sobre el corazón.\n" +
                "No nació para salvar el mundo. Lo salvó cuando decidió que su vida tenía valor.\n\n" +
                "Debajo de las ruinas del laboratorio, una luz vuelve a encenderse:\n" +
                "PROYECTO EIRA · SUJETOS COMPATIBLES DETECTADOS: 7.\n" +
                "\"La primera llave despertó. Ahora debemos encontrar las otras…\"\n\n" +
                "FIN — O COMIENZO.";
            UI?.ShowWin("LA ÚLTIMA GUERRA · LIBERTAD", Points, epi, "PRESIONA  [R]  PARA JUGAR DE NUEVO");
            Sfx.Play("victory");
        }

        public void NextLevel()
        {
            LevelIndex++;
            Time.timeScale = 1f;
            UI?.HideEndPanels();
            World.StripCamera();
            var data = LevelBuilder.BuildLevel(LevelIndex);
            Init(UI, data.Player, data.Nova, data.Cam);
            RespawnPoint = data.Spawn;
            data.Player.SetRespawnPoint(data.Spawn);
            data.Player.ResetForLevel(LevelIndex == 3 ? 5 : 3);
            data.Player.TeleportTo(data.Spawn);
            UI?.ShowBanner("NIVEL " + LevelIndex + " · " + LevelBuilder.LevelName(LevelIndex));
            var intro = LevelBuilder.LevelIntro(LevelIndex);
            UI?.SetObjective(LevelBuilder.LevelObjective(LevelIndex));
            PlayDialogue(intro, () =>
            {
                UI?.SetObjective(LevelBuilder.LevelObjective(LevelIndex));
                SetState(GameState.Playing);
            });
        }

        private string LevelWinTitle()
        {
            switch (LevelIndex)
            {
                case 2: return "¡NIVEL 2 COMPLETADO!";
                case 3: return "¡NIVEL 3 COMPLETADO!";
                default: return "¡NIVEL 1 COMPLETADO!";
            }
        }

        private string LevelWinHint()
        {
            return LevelIndex < 3
                ? "PRESIONA  [E]  PARA CONTINUAR AL NIVEL " + (LevelIndex + 1)
                : "PRESIONA  [R]  PARA JUGAR DE NUEVO";
        }

        public void OnGameOver()
        {
            SetState(GameState.GameOver);
            UI?.ShowGameOver();
            Sfx.Play("lose");
        }

        public void PlayDialogue(List<DialogueLine> lines, System.Action onDone)
        {
            SetState(GameState.Playing);
            UI?.PlayDialogue(lines, onDone);
        }

        private void Update()
        {
            bool busy = State == GameState.Playing || State == GameState.Boss;
            if (State == GameState.Win || State == GameState.GameOver)
            {
                if (Inputs.Restart() || Inputs.Start() || Inputs.Interact())
                {
                    if (State == GameState.Win && LevelIndex < 3) NextLevel();
                    else Reload();
                }
            }
            else if (State == GameState.Pause)
            {
                if (Inputs.PauseKey())
                {
                    Time.timeScale = 1f;
                    UI?.HidePause();
                    SetState(GameState.Playing);
                }
            }
            else if (busy)
            {
                if (Inputs.PauseKey() && !DialogueBusy && UI != null && !UI.TitleActive)
                {
                    Time.timeScale = 0f;
                    SetState(GameState.Pause);
                    UI?.ShowPause();
                }
            }
        }

        public void Reload()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}