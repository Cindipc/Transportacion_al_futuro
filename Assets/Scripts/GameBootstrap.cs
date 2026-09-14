using System.Collections.Generic;
using UnityEngine;

namespace EiraGame
{
    // ==================== ARRANQUE DEL JUEGO ====================
    public class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Sfx.Shutdown();
            Sfx.Init();

            var level = LevelBuilder.Build();
            var ui = UIManager.Create();

            var gm = GameManager.I != null ? GameManager.I : new GameObject("GameManager").AddComponent<GameManager>();
            gm.ResetRun();
            gm.Init(ui, level.Player, level.Nova, level.Cam);
            gm.SetLevel(1);

            Vector3 spawn = new Vector3(5f, 2.2f, 0f);
            gm.RespawnPoint = spawn;
            level.Player.SetRespawnPoint(spawn);
            level.Player.TeleportTo(spawn);

            gm.SetState(GameState.Intro);
            gm.SetObjective("…");
            ui.PlayCinematic(IntroScenes(), () =>
            {
                gm.SetState(GameState.Title);
                gm.SetObjective("Despierta en el laboratorio abandonado. Encuentra al androide NOVA.");
                ui.ShowTitle(() => IntroDialogue(gm));
            });
        }

        private static List<CinematicScene> IntroScenes()
        {
            return new List<CinematicScene>
            {
                new CinematicScene("cine_casa",
                    "Año 2026. Eira apenas estaba viva… y ya se sentía una carga para los que quería.\n" +
                    "Por dentro, cada latido le dolía un poco más.",
                    6.5f),
                new CinematicScene("cine_familia",
                    "Su familia la quería con locura. Pero Eira no entendía que su vida valía cada latido,\n" +
                    "porque el corazón enfermo no se lo contaba.",
                    6.5f),
                new CinematicScene("cine_corazon",
                    "Aquella noche su corazón se detuvo en silencio…\n" +
                    "y Eira, por primera vez, no tuvo miedo de descansar.",
                    7f),
                new CinematicScene("cine_hospital",
                    "En el hospital intentaron salvarle la vida.\n" +
                    "Pero su corazón ya no quería seguir latiendo.",
                    6.5f),
                new CinematicScene("cine_renacer",
                    "Sin embargo, algo despertó bajo sus párpados cerrados.\n" +
                    "Un siglo después, una voz mecánica murmuró: “tranquila, no voy a hacerte daño”.",
                    7f),
                new CinematicScene("cine_estrellas",
                    "Año 3000.\nLA LLAVE HA DESPERTADO.",
                    6f),
            };
        }

        private static void IntroDialogue(GameManager gm)
        {
            var lines = new List<DialogueLine>
            {
                new DialogueLine("NARRADOR", "Año 2026. Eira, joven, enferma del corazón y cansada de ser una carga, pidió esa noche que la dejaran descansar para siempre. Su corazón se detuvo en silencio."),
                new DialogueLine("NARRADOR", "Y entonces… el año 3000."),
                new DialogueLine("NOVA", "Tranquila. No voy a hacerte daño."),
                new DialogueLine("EIRA", "¿Quién… quién eres? ¿Dónde estoy?"),
                new DialogueLine("NOVA", "Me llamo NOVA. Estás en el año 3000, Eira. Esto fue un laboratorio genético."),
                new DialogueLine("NOVA", "Los humanos desaparecieron hace décadas. Pero tú… tú eres una anomalía."),
                new DialogueLine("EIRA", "¿Una anomalía? Mi corazón apenas late."),
                new DialogueLine("NOVA", "Por eso mismo. Tu sangre activa máquinas antiguas: el Núcleo Central y las puertas cifradas con ADN. Un humano llamado Kael quiere usarte."),
                new DialogueLine("NARRADOR", "Objetivo: escapa del laboratorio junto con NOVA. Usa X para el pulso ADN y E para interactuar con el entorno.")
            };
            gm.PlayDialogue(lines, () =>
            {
                gm.SetState(GameState.Playing);
                gm.SetObjective("Escapa del laboratorio recuperando pistas sobre el año 3000 y el control de Kael.");
            });
        }
    }
}