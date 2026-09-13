using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EiraGame
{
    /// <summary>Construye toda la interfaz de usuario por código (uGUI).</summary>
    public class UIManager : MonoBehaviour
    {
        private Font font;

        private GameObject hud, dialogueRoot, titleRoot, winRoot, overRoot, pauseRoot, bannerRoot, toastRoot;

        private Image[] hearts;
        private Image hpFill, energyFill;
        private float hpMaxW, energyMaxW;
        private Text points, objective, prompt, ability;
        private Text dName, dText, dHint;
        private Text bannerText, toastText;
        private Image bannerImg;

        private bool dialogueBusy;
        private List<DialogueLine> lines;
        private int di;
        private System.Action onDialogueDone;

        private bool titleOn;
        private System.Action onTitleStart;

        private float bannerT, toastT;

        public bool DialogueActive => dialogueBusy;
        public bool TitleActive => titleOn;

        public static UIManager Create()
        {
            var go = new GameObject("UIManager");
            var ui = go.AddComponent<UIManager>();
            ui.Build();
            return ui;
        }

        // ---------- BUILD ----------
        private void Build()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Verdana" }, 18);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = new Font("Arial");

            var cg = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cg.transform.SetParent(transform, false);
            canvas_ = cg.GetComponent<Canvas>();
            canvas_.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas_.sortingOrder = 100;
            var sc = cg.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight = 0.5f;

            BuildHud();
            BuildDialogue();
            BuildTitle();
            BuildEndPanels();
            BuildBannerAndToast();

            hud.SetActive(true);
            dialogueRoot.SetActive(false);
            titleRoot.SetActive(false);
            winRoot.SetActive(false);
            overRoot.SetActive(false);
            pauseRoot.SetActive(false);
            bannerRoot.SetActive(false);
            toastRoot.SetActive(false);
        }

        private Canvas canvas_;
        private Transform Root => canvas_.transform;

        private Text MakeText(Transform parent, string name, int size, Color color, Vector2 anchor, Vector2 pod, Vector2 sz, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pod;
            rt.sizeDelta = sz;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Image MakeImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 pod, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pod;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void AddOutline(Text t, Color c, float w)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(w, -w);
        }

        // ---------- HUD ----------
        private void BuildHud()
        {
            hud = new GameObject("HUD", typeof(RectTransform));
            hud.transform.SetParent(Root, false);
            ((RectTransform)hud.transform).anchorMin = Vector2.zero;
            ((RectTransform)hud.transform).anchorMax = Vector2.one;
            ((RectTransform)hud.transform).sizeDelta = Vector2.zero;

            var colL = new Color(0f, 0f, 0f, 0.55f);
            var colE = new Color(0f, 0f, 0f, 0.45f);

            // Panel de salud (esquina superior izquierda)
            MakeImage(hud.transform, "HealthPanel", colL, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(280f, 140f));

            var lbl = MakeText(hud.transform, "VidaLbl", 20, Color.white, new Vector2(0f, 1f), new Vector2(40f, -52f), new Vector2(120f, 26f), TextAnchor.MiddleLeft);
            lbl.text = "VIDA";

            hearts = new Image[5];
            var sfx = LoadSprite("heart_full");
            var sxe = LoadSprite("heart_empty");
            for (int i = 0; i < 5; i++)
            {
                var img = MakeImage(hud.transform, "Heart" + i, Color.white, new Vector2(0f, 1f), new Vector2(52f + i * 34f, -86f), new Vector2(30f, 26f));
                img.sprite = sfx;
                hearts[i] = img;
            }
            _heartEmpty = sxe;

            // HP bar
            hpMaxW = 230f;
            MakeImage(hud.transform, "HpBack", new Color(0.1f, 0.1f, 0.1f, 0.75f), new Vector2(0f, 1f), new Vector2(44f, -120f), new Vector2(hpMaxW, 16f));
            hpFill = MakeImage(hud.transform, "HpFill", new Color(0.94f, 0.30f, 0.26f, 1f), new Vector2(0f, 1f), new Vector2(44f, -120f), new Vector2(hpMaxW, 16f));

            // Energía cardíaca
            MakeImage(hud.transform, "EnPanel", colL, new Vector2(0f, 1f), new Vector2(24f, -150f), new Vector2(280f, 70f));
            var elbl = MakeText(hud.transform, "EnLbl", 18, new Color(0.65f, 0.9f, 1f), new Vector2(0f, 1f), new Vector2(40f, -166f), new Vector2(160f, 24f), TextAnchor.MiddleLeft);
            elbl.text = "ENERGÍA";
            energyMaxW = 230f;
            MakeImage(hud.transform, "EnBack", new Color(0.1f, 0.1f, 0.1f, 0.75f), new Vector2(0f, 1f), new Vector2(44f, -194f), new Vector2(energyMaxW, 13f));
            energyFill = MakeImage(hud.transform, "EnFill", new Color(0.2f, 0.78f, 0.9f, 1f), new Vector2(0f, 1f), new Vector2(44f, -194f), new Vector2(energyMaxW, 13f));

            points = MakeText(hud.transform, "Points", 22, new Color(1f, 0.9f, 0.4f), new Vector2(0f, 1f), new Vector2(40f, -238f), new Vector2(280f, 28f), TextAnchor.MiddleLeft);
            points.text = "PUNTOS: 0";

            // Objetivo (esquina superior derecha)
            MakeImage(hud.transform, "ObjPanel", colL, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(560f, 120f));
            objective = MakeText(hud.transform, "Objective", 20, Color.white, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(520f, 88f), TextAnchor.UpperRight);
            objective.horizontalOverflow = HorizontalWrapMode.Wrap;
            objective.verticalOverflow = VerticalWrapMode.Truncate;

            // Aviso de interacción (abajo centro)
            prompt = MakeText(hud.transform, "Interact", 26, new Color(1f, 0.95f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1000f, 40f), TextAnchor.MiddleCenter);
            prompt.text = "";
            AddOutline(prompt, Color.black, 1.4f);

            ability = MakeText(hud.transform, "Ability", 16, new Color(0.85f, 0.85f, 0.85f, 0.85f), new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(900f, 24f), TextAnchor.LowerLeft);
            ability.text = "A/D MOVER  ·  ESPACIO SALTAR  ·  E INTERACTUAR  ·  X PULSO ADN";
        }

        private Sprite _heartEmpty;
        private Text winT;
        private Text winRe;

        private static Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>("Sprites/" + name);
        }

        // ---------- DIÁLOGO ----------
        private void BuildDialogue()
        {
            dialogueRoot = new GameObject("Dialogue", typeof(RectTransform));
            dialogueRoot.transform.SetParent(Root, false);
            var rt = (RectTransform)dialogueRoot.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 34f);
            rt.sizeDelta = new Vector2(1560f, 210f);

            MakeImage(dialogueRoot.transform, "BG", new Color(0.02f, 0.04f, 0.07f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 210f));
            MakeImage(dialogueRoot.transform, "Accent", new Color(0.14f, 0.8f, 0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1560f, 4f));

            dName = MakeText(dialogueRoot.transform, "Speaker", 24, new Color(0.4f, 0.85f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1480f, 34f), TextAnchor.UpperLeft);
            dText = MakeText(dialogueRoot.transform, "Body", 24, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(1480f, 120f), TextAnchor.UpperLeft);
            dText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dText.verticalOverflow = VerticalWrapMode.Truncate;
            dHint = MakeText(dialogueRoot.transform, "Hint", 22, new Color(1f, 0.92f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1480f, 26f), TextAnchor.LowerRight);
            dHint.text = "▶ [E] continuar";
            dHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void BuildTitle()
        {
            titleRoot = new GameObject("Title", typeof(RectTransform));
            titleRoot.transform.SetParent(Root, false);
            var rt = (RectTransform)titleRoot.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            MakeImage(titleRoot.transform, "BG", new Color(0.02f, 0.03f, 0.06f, 0.97f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));

            var t1 = MakeText(titleRoot.transform, "T1", 80, new Color(0.2f, 0.8f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1800f, 110f), TextAnchor.MiddleCenter);
            t1.text = "E I R A";
            AddOutline(t1, new Color(0.02f, 0.15f, 0.2f), 2.5f);

            var t2 = MakeText(titleRoot.transform, "T2", 40, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(1800f, 55f), TextAnchor.MiddleCenter);
            t2.text = "LA LLAVE DEL AÑO 3000";

            var t3 = MakeText(titleRoot.transform, "T3", 24, new Color(0.9f, 0.9f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(1500f, 40f), TextAnchor.MiddleCenter);
            t3.text = "Un videojuego 2D de plataformas basado en la historia original.";

            var panel = MakeImage(titleRoot.transform, "Chip", new Color(0f, 0f, 0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1300f, 400f));
            _ = panel;

            var story = MakeText(titleRoot.transform, "Story", 22, new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1180f, 300f), TextAnchor.MiddleCenter);
            story.alignment = TextAnchor.MiddleCenter;
            story.text = "Año 2026. Eira, una joven con el corazón enfermo y cansada de ser vista como una carga, decide rendirse.\nSu corazón se detiene…\n\n"
                + "AÑO 3000.\nEira despierta en un laboratorio abandonado. Los humanos desaparecieron hace décadas. Las máquinas gobiernan las ruinas y solo un androide llamado NOVA parece querer ayudarla.\n\n"
                + "Ella es la llave de un secreto antiguo: su sangre activa el Núcleo Central. Y alguien llamado Kael hará lo imposible por usarla.";

            var controls = MakeText(titleRoot.transform, "Controls", 22, new Color(0.6f, 0.85f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1400f, 30f), TextAnchor.MiddleCenter);
            controls.text = "A/D mover · ESPACIO saltar · E interactuar · X pulso ADN";

            var levels = MakeText(titleRoot.transform, "Levels", 22, Color.white, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1400f, 30f), TextAnchor.MiddleCenter);
            levels.text = "NIVEL 1 · EL DESPERTAR [LISTO]     NIVEL 2 · LA CAZA [PRÓXIMAMENTE]     NIVEL 3 · LA ÚLTIMA GUERRA [PRÓXIMAMENTE]";

            var start = MakeText(titleRoot.transform, "Start", 34, new Color(1f, 0.95f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1400f, 46f), TextAnchor.MiddleCenter);
            start.text = "PRESIONA  [E]  PARA COMENZAR EL NIVEL 1";
            AddOutline(start, Color.black, 2f);
        }

        private void BuildEndPanels()
        {
            winRoot = new GameObject("Win", typeof(RectTransform));
            winRoot.transform.SetParent(Root, false);
            var wr = (RectTransform)winRoot.transform;
            wr.anchorMin = Vector2.zero; wr.anchorMax = Vector2.one; wr.sizeDelta = Vector2.zero;
            MakeImage(winRoot.transform, "BG", new Color(0.02f, 0.06f, 0.05f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));

            var wt = MakeText(winRoot.transform, "T", 66, new Color(0.55f, 1f, 0.7f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1800f, 90f), TextAnchor.MiddleCenter);
            wt.text = "¡NIVEL 1 COMPLETADO!";
            AddOutline(wt, Color.black, 2.5f);
            winT = wt;

            var wScore = MakeText(winRoot.transform, "Score", 30, new Color(1f, 0.92f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1800f, 40f), TextAnchor.MiddleCenter);
            wScore.name = "wScore";

            var wEp = MakeText(winRoot.transform, "Ep", 24, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1400f, 300f), TextAnchor.MiddleCenter);
            wEp.horizontalOverflow = HorizontalWrapMode.Wrap;

            var wRe = MakeText(winRoot.transform, "Re", 26, new Color(1f, 0.95f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1400f, 36f), TextAnchor.MiddleCenter);
            wRe.text = "PRESIONA  [R]  PARA JUGAR DE NUEVO";
            winRe = wRe;
            MakeText(winRoot.transform, "Info", 20, new Color(0.8f, 0.8f, 0.8f, 0.8f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1400f, 28f), TextAnchor.MiddleCenter).text = "El año 3000 espera a la portadora. Gracias por jugar.";

            overRoot = new GameObject("Over", typeof(RectTransform));
            overRoot.transform.SetParent(Root, false);
            var or = (RectTransform)overRoot.transform;
            or.anchorMin = Vector2.zero; or.anchorMax = Vector2.one; or.sizeDelta = Vector2.zero;
            MakeImage(overRoot.transform, "BG", new Color(0.05f, 0.01f, 0.01f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));

            var ot = MakeText(overRoot.transform, "T", 60, new Color(1f, 0.5f, 0.45f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1800f, 90f), TextAnchor.MiddleCenter);
            ot.text = "EL CORAZÓN DE EIRA SE DETUVO";
            AddOutline(ot, Color.black, 2.5f);

            MakeText(overRoot.transform, "Sub", 28, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(1600f, 40f), TextAnchor.MiddleCenter).text = "Se perdieron las 3 vidas. La llave del año 3000 sigue perdida…";

            var oRe = MakeText(overRoot.transform, "Re", 26, new Color(1f, 0.95f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1400f, 36f), TextAnchor.MiddleCenter);
            oRe.text = "PRESIONA  [R]  PARA REINTENTAR";

            pauseRoot = new GameObject("Pause", typeof(RectTransform));
            pauseRoot.transform.SetParent(Root, false);
            var pr = (RectTransform)pauseRoot.transform;
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.sizeDelta = Vector2.zero;
            MakeImage(pauseRoot.transform, "BG", new Color(0f, 0f, 0f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            MakeText(pauseRoot.transform, "T", 60, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1400f, 90f), TextAnchor.MiddleCenter).text = "PAUSA";
            MakeText(pauseRoot.transform, "H", 26, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1400f, 36f), TextAnchor.MiddleCenter).text = "PRESIONA  [ESC]  PARA CONTINUAR";
        }

        private void BuildBannerAndToast()
        {
            bannerRoot = new GameObject("Banner", typeof(RectTransform));
            bannerRoot.transform.SetParent(Root, false);
            var br = (RectTransform)bannerRoot.transform;
            br.anchorMin = new Vector2(0.5f, 1f); br.anchorMax = new Vector2(0.5f, 1f);
            br.pivot = new Vector2(0.5f, 1f);
            br.anchoredPosition = new Vector2(0f, -2f);
            br.sizeDelta = new Vector2(900f, 90f);

            bannerImg = MakeImage(bannerRoot.transform, "BG", new Color(0.1f, 0f, 0f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 90f));
            bannerText = MakeText(bannerRoot.transform, "T", 34, new Color(1f, 0.75f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 70f), TextAnchor.MiddleCenter);
            bannerText.alignment = TextAnchor.MiddleCenter;
            AddOutline(bannerText, Color.black, 1.8f);

            toastRoot = new GameObject("Toast", typeof(RectTransform));
            toastRoot.transform.SetParent(Root, false);
            var tr = (RectTransform)toastRoot.transform;
            tr.anchorMin = new Vector2(0.5f, 0.5f); tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = new Vector2(0f, -140f);
            tr.sizeDelta = new Vector2(1000f, 70f);
            MakeImage(toastRoot.transform, "BG", new Color(0.2f, 0f, 0f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 70f));
            toastText = MakeText(toastRoot.transform, "T", 28, new Color(1f, 0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 60f), TextAnchor.MiddleCenter);
            AddOutline(toastText, Color.black, 1.5f);
        }

        // ---------- PUBLIC API ----------
        public void SetPoints(int v)
        {
            if (points != null) points.text = "PUNTOS: " + v;
        }

        public void SetObjective(string s)
        {
            if (objective != null) objective.text = "OBJETIVO\n" + s;
        }

        public void SetEnergy(float e, float maxE)
        {
            if (energyFill == null) return;
            float v = Mathf.Clamp01(e / Mathf.Max(1f, maxE));
            energyFill.rectTransform.sizeDelta = new Vector2(energyMaxW * v, energyFill.rectTransform.sizeDelta.y);
            energyFill.color = Color.Lerp(new Color(1f, 0.35f, 0.3f), new Color(0.2f, 0.78f, 0.9f), v);
        }

        public void SetHealth(int hp, int maxHp, int lives)
        {
            if (hpFill == null) return;
            float v = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));
            hpFill.rectTransform.sizeDelta = new Vector2(hpMaxW * v, hpFill.rectTransform.sizeDelta.y);
            hpFill.color = hp == 1 ? new Color(1f, 0.12f, 0.12f) : new Color(0.94f, 0.30f, 0.26f);
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].sprite = i < lives ? LoadSprite("heart_full") : LoadSprite("heart_empty");
        }

        public void ShowInteract(string text)
        {
            if (prompt != null) prompt.text = text;
        }

        public void HideInteract()
        {
            if (prompt != null) prompt.text = "";
        }

        public void ShowShadowToast(string text)
        {
            if (toastRoot == null) return;
            toastText.text = text;
            toastRoot.SetActive(true);
            toastT = 2.2f;
        }

        public void PlayDialogue(List<DialogueLine> list, System.Action done)
        {
            lines = list;
            di = 0;
            onDialogueDone = done;
            dialogueBusy = true;
            dialogueRoot.SetActive(true);
            Sfx.Play("beep");
            ShowLine();
        }

        private void ShowLine()
        {
            if (di >= lines.Count) { EndDialogue(); return; }
            var l = lines[di];
            dName.text = l.Speaker;
            dText.text = l.Text;
        }

        private void EndDialogue()
        {
            dialogueBusy = false;
            dialogueRoot.SetActive(false);
            var d = onDialogueDone;
            onDialogueDone = null;
            d?.Invoke();
        }

        public void ShowTitle(System.Action onStart)
        {
            titleOn = true;
            onTitleStart = onStart;
            titleRoot.SetActive(true);
        }

        private void StartTitle()
        {
            var cb = onTitleStart;
            onTitleStart = null;
            titleOn = false;
            titleRoot.SetActive(false);
            cb?.Invoke();
        }

        public void ShowBanner(string text, float keep = 2.5f)
        {
            if (bannerRoot == null) return;
            bannerText.text = text;
            bannerRoot.SetActive(true);
            bannerT = keep;
        }

        public void ShowWin(string title, int totalPoints, string epilogue, string hint)
        {
            winRoot.SetActive(true);
            if (winT != null) winT.text = title;
            if (winRe != null) winRe.text = hint;
            var sc = winRoot.transform.Find("Score").GetComponent<Text>();
            sc.text = "PUNTOS TOTALES: " + totalPoints;
            var ep = winRoot.transform.Find("Ep").GetComponent<Text>();
            ep.text = epilogue;
        }

        public void HideEndPanels()
        {
            if (winRoot != null) winRoot.SetActive(false);
            if (overRoot != null) overRoot.SetActive(false);
        }

        public void ShowGameOver()
        {
            overRoot.SetActive(true);
        }

        public void ShowPause()
        {
            pauseRoot.SetActive(true);
        }

        public void HidePause()
        {
            pauseRoot.SetActive(false);
        }

        public void OnGameState(GameState s)
        {
            bool play = s == GameState.Playing || s == GameState.Boss;
            hud.SetActive(play || s == GameState.Pause);
        }

        // ---------- UPDATE ----------
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (dialogueBusy)
            {
                if (Inputs.Interact())
                {
                    di++;
                    Sfx.Play("beep");
                    ShowLine();
                }
            }
            else if (titleOn)
            {
                if (Inputs.Start()) StartTitle();
            }

            if (toastT > 0f)
            {
                toastT -= dt;
                if (toastT <= 0f && toastRoot != null)
                {
                    toastRoot.SetActive(false);
                    toastText.text = "";
                }
            }

            if (bannerT > 0f)
            {
                bannerT -= dt;
                if (bannerT <= 0f && bannerRoot != null)
                {
                    bannerRoot.SetActive(false);
                    bannerText.text = "";
                }
            }
            if (bannerRoot != null && bannerRoot.activeSelf)
            {
                float a = bannerT > 0.5f ? 1f : bannerT / 0.5f;
                bannerImg.color = new Color(0.1f, 0f, 0f, 0.85f * Mathf.Clamp01(a));
                bannerText.color = new Color(1f, 0.75f, 0.4f, Mathf.Clamp01(a));
            }
        }
    }
}