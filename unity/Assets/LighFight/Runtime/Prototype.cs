using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Explicit scene component; never auto-spawn in downloaded production scenes.
    public sealed class Prototype : MonoBehaviour
    {
        private static readonly string[] MoveNames = { "Джеб", "Кросс", "Хук", "Корпус", "Фронт", "Лоу", "Подсечка", "Апперкот", "Колено", "Локоть", "Разворот", "Прыжок", "Бэкфист", "Топор", "Боковой", "Круговой", "Молот", "Ладонь", "Пятка", "Смена" };
        private static readonly KeyCode[] AttackKeys = { KeyCode.J, KeyCode.K, KeyCode.U, KeyCode.I };
        private FightSession session;
        private Match match => session.Match;
        private FighterView player, opponent;
        private float accumulator;
        private int pendingAttack = -1;
        private BotController bot = new BotController();
        private bool paused, backgrounded;
                public GameContent content;
        private HitFeedback feedback;
        private Camera fightCamera;
        private bool training;
        private bool russian = true;
        private string T(string ru,string en) => russian?ru:en;
        public string Title => T("LighFight — выбери бойца и арену","LighFight — choose fighter and arena");
        private HostBridge bridge;
        private OfficeArena arena;
        private bool selecting = true;
        private int selectedFighter, selectedArena;
        private static readonly string[] FighterNames = { "Костяная леди", "Ночная ведьма", "Мастер стихий", "Островной боец", "Розовая пантера", "Рейнджер", "Гала-боец", "Кобальтовый чемпион" };
        private static readonly string[] FighterEnglish = { "Bone Lady", "Night Witch", "Element Master", "Island Fighter", "Pink Panther", "Ranger", "Gala Fighter", "Cobalt Champion" };
        private static readonly string[] ArenaEnglish = { "Meeting room", "Kitchen", "Office", "Tight corner", "Courtyard" };
        private static readonly string[] ArenaNames = { "Переговорная", "Кухня", "Рабочий кабинет", "Тесный угол", "Офисный двор" };
        private static readonly Color[] OutfitColors = { new Color(.5f,.65f,.25f), new Color(.3f,.18f,.5f), new Color(.05f,.3f,.32f), new Color(.68f,.58f,.36f), new Color(.85f,.42f,.55f), new Color(.26f,.32f,.17f), new Color(.10f,.18f,.32f), new Color(.05f,.25f,.65f) };

        private void Start()
        {
            session = new FightSession();
            RebuildViews();
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(transform,false);
            camera.transform.position = new Vector3(0, 2.4f, -9);
            camera.fieldOfView = 42;
            camera.gameObject.AddComponent<AudioListener>();fightCamera=camera;
            feedback=gameObject.AddComponent<HitFeedback>();
            camera.backgroundColor = new Color(.08f,.09f,.12f);
            camera.transform.LookAt(new Vector3(0, 1, 0));
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(transform,false);
            light.type = LightType.Directional;
            light.intensity = 1.2f; light.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.45f,.43f,.4f);
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            bridge = FindFirstObjectByType<HostBridge>();
            if (bridge != null) { bridge.LaunchRequested += OnLaunch; if(bridge.CurrentLaunch!=null)OnLaunch(bridge.CurrentLaunch); }
        }

        private void OnLaunch(LaunchRequest request) { russian=string.IsNullOrEmpty(request.locale)||request.locale.StartsWith("ru"); Reset(); selecting = true; }
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
            player.transform.SetParent(transform,true);opponent.transform.SetParent(transform,true);arena.transform.SetParent(transform,true);
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
            session = new FightSession(training); accumulator = 0; bot = new BotController();
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
                if (Input.GetKeyDown(AttackKeys[i])) pendingAttack = i + (crouch ? 4 : upper ? 8 : Input.GetKey(KeyCode.LeftArrow) ? 12 : 0);
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
                bot.Step(session);
                session.Step();feedback.Observe(match);
            }
            float center=(match.A.X+match.B.X)*.5f;
            float distance=Mathf.Max(7.2f,Mathf.Abs(match.A.X-match.B.X)*1.3f+5.5f);
            fightCamera.transform.position=Vector3.Lerp(fightCamera.transform.position,new Vector3(center,2.4f,-distance),Time.deltaTime*4);
            fightCamera.transform.LookAt(new Vector3(center,1,0));
            fightCamera.transform.position+=Vector3.right*Mathf.Sin(Time.time*87)*feedback.Shake;
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
                GUI.Box(new Rect(15,15,930,510), Title);
                for (int i=0;i<FighterNames.Length;i++)
                    if (GUI.Button(new Rect(35+i%4*222,65+i/4*70,210,60),(i==selectedFighter?"● ":"")+T(FighterNames[i],FighterEnglish[i])))
                    { selectedFighter=i; RebuildViews(); }
                for (int i=0;i<ArenaNames.Length;i++)
                    if (GUI.Button(new Rect(35+i*178,235,168,60),(i==selectedArena?"● ":"")+T(ArenaNames[i],ArenaEnglish[i])))
                    { selectedArena=i; RebuildViews(); }
                GUI.Label(new Rect(45,325,850,60), T("Стрелки: движение. J / K / U / I: удары. Shift: блок.\n↑ / ↓ / ← + удар: другие приёмы. Z / X / C / V: ещё четыре. Победа — два раунда.","Arrows: move. J / K / U / I: attacks. Shift: guard.\nUp / Down / Left + attack: alternate moves. Z / X / C / V: four more. First to two rounds wins."));
                training=GUI.Toggle(new Rect(45,375,280,35),training,T("Тренировка без таймера и урона","Practice — unlimited time and health"));
                if (GUI.Button(new Rect(330,410,300,70), T("НАЧАТЬ БОЙ","START FIGHT"))) { Reset(); selecting=false; }
                if (GUI.Button(new Rect(770,410,150,70), T("ВЫХОД","EXIT"))) bridge?.Exit("");
                GUI.matrix=saved; return;
            }
            GUI.Label(new Rect(20,20,330,40), $"LighFight  R{session.Round}   {session.WinsA} : {session.WinsB}   {match.A.Health} : {match.B.Health}");
            GUI.Box(new Rect(360,20,90,45), paused ? T("Продолжить","Resume") : T("Пауза","Pause"));
            GUI.Box(new Rect(460,20,90,45), T("Заново","Restart"));
            GUI.Box(new Rect(560,20,90,45), T("Выход","Exit"));
            GUI.Label(new Rect(20,80,900,35), T("Стрелки + J K U I. ↑ / ↓ / ←: другие удары. Z X C V: дополнительные. Shift: блок.","Arrows + J K U I. Up / Down / Left: alternate attacks. Z X C V: extra moves. Shift: guard."));
            if (paused) GUI.Label(new Rect(430,160,150,35), T("ПАУЗА","PAUSED"));
            GUI.Label(new Rect(700,20,180,40), training?T("ТРЕНИРОВКА","PRACTICE"):$"{(session.RemainingTicks + 59) / 60}");
            var color=GUI.color;GUI.color=new Color(.20f,.85f,.55f);
            GUI.DrawTexture(new Rect(20,130,350*match.A.Health/150f,14),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(590,130,350*match.B.Health/150f,14),Texture2D.whiteTexture);GUI.color=color;
            if(match.A.Stun>0)GUI.Label(new Rect(20,160,220,30),match.A.LastBlocked?T("БЛОК","BLOCK"):match.A.LastCounter?T("КОНТРУДАР","COUNTER"):"−"+match.A.LastDamage);
            if(match.B.Stun>0)GUI.Label(new Rect(740,160,220,30),match.B.LastBlocked?T("БЛОК","BLOCK"):match.B.LastCounter?T("КОНТРУДАР","COUNTER"):"−"+match.B.LastDamage);
            if (session.Phase != FightPhase.Fighting) GUI.Label(new Rect(410,200,240,35), session.Phase == FightPhase.Complete ? (session.Winner == 0 ? T("ПОБЕДА В МАТЧЕ","MATCH WON") : T("МАТЧ ПРОИГРАН","MATCH LOST")) : session.RoundWinner < 0 ? T("НИЧЬЯ","DRAW") : session.RoundWinner == 0 ? T("РАУНД ВЫИГРАН","ROUND WON") : T("РАУНД ПРОИГРАН","ROUND LOST"));
            GUI.Box(new Rect(25,440,70,70), "←"); GUI.Box(new Rect(100,440,70,70), "→");
            GUI.Box(new Rect(175,440,70,70), "↓"); GUI.Box(new Rect(100,365,70,65), "↑");
            GUI.Box(new Rect(250,440,80,70), T("Блок","Guard"));
            for (int i = 0; i < Moves.All.Length; i++) GUI.Box(AttackArea(i), T(MoveNames[i],Moves.All[i].Id));
            GUI.matrix = saved;
        }
    }
}
