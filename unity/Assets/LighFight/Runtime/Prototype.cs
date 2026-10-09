using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Explicit scene component; never auto-spawn in downloaded production scenes.
    public sealed class Prototype : MonoBehaviour
    {
        private static readonly KeyCode[] AttackKeys = { KeyCode.J, KeyCode.K, KeyCode.U, KeyCode.I };
        private FightSession session;
        private Match match => session.Match;
        private FighterView player, opponent;
        private float accumulator;
        private int botCooldown, pendingAttack = -1;
        private bool paused, backgrounded;
        private readonly System.Random random = new System.Random(42);
        public GameContent content;
        private HostBridge bridge;
        private OfficeArena arena;
        private bool selecting = true;
        private int selectedFighter, selectedArena;
        private static readonly string[] FighterNames = { "Костяная леди", "Ночная ведьма", "Мастер стихий", "Островной боец", "Розовая пантера", "Рейнджер", "Гала-боец", "Кобальтовый чемпион" };
        private static readonly string[] ArenaNames = { "Переговорная", "Кухня", "Рабочий кабинет", "Тесный угол", "Офисный двор" };
        private static readonly Color[] OutfitColors = { new Color(.5f,.65f,.25f), new Color(.3f,.18f,.5f), new Color(.05f,.3f,.32f), new Color(.68f,.58f,.36f), new Color(.85f,.42f,.55f), new Color(.26f,.32f,.17f), new Color(.10f,.18f,.32f), new Color(.05f,.25f,.65f) };

        private void Start()
        {
            session = new FightSession();
            RebuildViews();
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 2.4f, -9);
            camera.fieldOfView = 42;
            camera.backgroundColor = new Color(.08f,.09f,.12f);
            camera.transform.LookAt(new Vector3(0, 1, 0));
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f; light.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.45f,.43f,.4f);
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            bridge = FindFirstObjectByType<HostBridge>();
            if (bridge != null) bridge.LaunchRequested += OnLaunch;
        }

        private void OnLaunch(LaunchRequest request) { Reset(); selecting = true; }
        private void RebuildViews()
        {
            if (player != null) Destroy(player.gameObject);
            if (opponent != null) Destroy(opponent.gameObject);
            if (arena != null) Destroy(arena.gameObject);
            int cpu = (selectedFighter + 3) % FighterNames.Length;
            if (content != null)
            {
                player = Instantiate(content.fighters[selectedFighter]).GetComponent<FighterView>();
                opponent = Instantiate(content.fighters[cpu]).GetComponent<FighterView>();
                arena = Instantiate(content.arenas[selectedArena]).GetComponent<OfficeArena>();
            }
            else
            {
                player = FighterView.Create(FighterNames[selectedFighter], OutfitColors[selectedFighter], selectedFighter == 0 || selectedFighter == 1 || selectedFighter == 4);
                opponent = FighterView.Create(FighterNames[cpu], OutfitColors[cpu], cpu == 0 || cpu == 1 || cpu == 4);
                arena = OfficeArena.Create((OfficeLocation)selectedArena);
            }
            player.Pose(match.A,1,0); opponent.Pose(match.B,-1,0);
        }
        private void OnDestroy()
        {
            if (bridge != null) bridge.LaunchRequested -= OnLaunch;
        }
        private void OnApplicationPause(bool value)
        {
            backgrounded = value;
            accumulator = 0;
            pendingAttack = -1;
        }
        private void Reset()
        {
            session = new FightSession(); accumulator = 0; botCooldown = 0;
            pendingAttack = -1; paused = false;
        }

        // All touch positions converted into the same logical coordinates as GUI.
        private Vector2 Position(Vector2 screen) => new Vector2(screen.x * 960 / Screen.width,
            (Screen.height - screen.y) * 540 / Screen.height);
        private bool Held(Rect area)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled &&
                    area.Contains(Position(touch.position))) return true;
            }
            return Input.touchCount == 0 && Input.GetMouseButton(0) && area.Contains(Position(Input.mousePosition));
        }
        private bool Pressed(Rect area)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && area.Contains(Position(touch.position))) return true;
            }
            return Input.touchCount == 0 && Input.GetMouseButtonDown(0) && area.Contains(Position(Input.mousePosition));
        }
        private static Rect AttackArea(int index) => new Rect(630 + index % 5 * 62, 285 + index / 5 * 54, 60, 50);

        private void Update()
        {
            if (!selecting && (Pressed(new Rect(360, 20, 90, 45)) || Input.GetKeyDown(KeyCode.Escape)))
            { paused = !paused; accumulator = 0; pendingAttack = -1; }
            if (!selecting && (Input.GetKeyDown(KeyCode.R) || Pressed(new Rect(460, 20, 90, 45)))) Reset();
            if ((!selecting && Pressed(new Rect(560, 20, 90, 45))) || Input.GetKeyDown(KeyCode.Q))
            { if (bridge != null) bridge.Exit(""); return; }
            if (selecting)
            {
                player.Pose(match.A,1,Time.deltaTime); opponent.Pose(match.B,-1,Time.deltaTime);
                return;
            }
            if (paused || backgrounded || Time.timeScale == 0) return;
            bool crouch = Held(new Rect(175, 440, 70, 70)) || Input.GetKey(KeyCode.DownArrow);
            bool upper = Held(new Rect(100, 365, 70, 65)) || Input.GetKey(KeyCode.UpArrow);
            for (int i = 0; i < AttackKeys.Length; i++)
                if (Input.GetKeyDown(AttackKeys[i])) pendingAttack = i + (crouch ? 4 : upper ? 8 : 0);
            for (int i = 0; i < Moves.All.Length; i++) if (Pressed(AttackArea(i))) pendingAttack = i;
            var extras = new[] { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V };
            for (int i = 0; i < extras.Length; i++) if (Input.GetKeyDown(extras[i])) pendingAttack = 16+i;
            float direction = (Held(new Rect(100,440,70,70)) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
                (Held(new Rect(25,440,70,70)) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            bool guard = Held(new Rect(250,440,80,70)) || Input.GetKey(KeyCode.LeftShift);
            accumulator += Mathf.Min(Time.deltaTime, .1f);
            while (accumulator >= 1f / Match.TickRate)
            {
                accumulator -= 1f / Match.TickRate;
                match.A.Guard = guard; match.A.Crouch = crouch;
                if (pendingAttack >= 0) { session.QueueAttack(true, pendingAttack); pendingAttack = -1; }
                match.Walk(match.A, direction);
                if (session.Phase == FightPhase.Fighting)
                {
                    if (Mathf.Abs(match.A.X - match.B.X) > 1.3f)
                        match.Walk(match.B, Mathf.Sign(match.A.X - match.B.X));
                    match.B.Guard = botCooldown > 35;
                    if (--botCooldown <= 0) { session.QueueAttack(false, random.Next(Moves.All.Length)); botCooldown = 60; }
                }
                session.Step();
            }
            player.Pose(match.A, match.B.X - match.A.X, Time.deltaTime);
            opponent.Pose(match.B, match.A.X - match.B.X, Time.deltaTime);
        }

        private void OnGUI()
        {
            if (session == null) return;
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 960f, Screen.height / 540f, 1));
            if (selecting)
            {
                GUI.Box(new Rect(15,15,930,510), "LighFight — выбери бойца и арену");
                for (int i=0;i<FighterNames.Length;i++)
                    if (GUI.Button(new Rect(35+i%4*222,65+i/4*70,210,60),(i==selectedFighter?"● ":"")+FighterNames[i]))
                    { selectedFighter=i; RebuildViews(); }
                for (int i=0;i<ArenaNames.Length;i++)
                    if (GUI.Button(new Rect(35+i*178,235,168,60),(i==selectedArena?"● ":"")+ArenaNames[i]))
                    { selectedArena=i; RebuildViews(); }
                GUI.Label(new Rect(45,325,850,60), "Стрелки: движение и приседание. J / K / U / I: удары.\n↑ или ↓ + удар: альтернативный приём. Shift: блок. Победа — два раунда.");
                if (GUI.Button(new Rect(330,410,300,70), "НАЧАТЬ БОЙ")) { Reset(); selecting=false; }
                if (GUI.Button(new Rect(770,410,150,70), "ВЫХОД")) bridge?.Exit("");
                GUI.matrix=saved; return;
            }
            GUI.Label(new Rect(20,20,330,40), $"LighFight  R{session.Round}   {session.WinsA} : {session.WinsB}   {match.A.Health} : {match.B.Health}");
            GUI.Box(new Rect(360,20,90,45), paused ? "Resume" : "Pause");
            GUI.Box(new Rect(460,20,90,45), "Restart");
            GUI.Box(new Rect(560,20,90,45), "Exit");
            GUI.Label(new Rect(20,80,900,35), "Arrows + J K U I. Up/Down: alternate attacks. Shift: guard.");
            if (paused) GUI.Label(new Rect(430,160,150,35), "PAUSED");
            GUI.Label(new Rect(700,20,180,40), $"{(session.RemainingTicks + 59) / 60}");
            if (session.Phase != FightPhase.Fighting) GUI.Label(new Rect(410,200,240,35), session.Phase == FightPhase.Complete ? (session.Winner == 0 ? "MATCH WON" : "MATCH LOST") : session.RoundWinner < 0 ? "DRAW" : session.RoundWinner == 0 ? "ROUND WON" : "ROUND LOST");
            GUI.Box(new Rect(25,440,70,70), "←"); GUI.Box(new Rect(100,440,70,70), "→");
            GUI.Box(new Rect(175,440,70,70), "↓"); GUI.Box(new Rect(100,365,70,65), "↑");
            GUI.Box(new Rect(250,440,80,70), "Guard");
            for (int i = 0; i < Moves.All.Length; i++) GUI.Box(AttackArea(i), Moves.All[i].Id);
            GUI.matrix = saved;
        }
    }
}
