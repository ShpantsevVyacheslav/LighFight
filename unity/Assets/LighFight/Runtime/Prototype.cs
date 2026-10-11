using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Explicit scene component; never auto-spawn in downloaded production scenes.
    public sealed partial class Prototype : MonoBehaviour
    {
        private static readonly string[] MoveNames = { "Джеб", "Кросс", "Хук", "Корпус", "Фронт", "Лоу", "Подсечка", "Апперкот", "Колено", "Локоть", "Разворот", "Прыжок", "Бэкфист", "Топор", "Боковой", "Круговой", "Молот", "Толчок", "Пятка", "Смена", "Высокий", "Короткий хук", "Двойной", "Шаг + нога", "Контрудар" };
        private static readonly KeyCode[] AttackKeys = { KeyCode.J, KeyCode.K, KeyCode.U, KeyCode.I };
        private FightSession session;
        private Match match => session.Match;
        private FighterView player, opponent;
        private float accumulator;
        private int contactFreeze;
        private PhysicalContacts physicalContacts;
        private Match contactMatch;
        private Vector2 stickInput;
        private int stickFinger=-1;
        private static Rect StickArea=>new Rect(30,355,160,160);
        private int pendingAttack = -1;
        private BotController bot = new BotController();
        private bool paused, backgrounded, advancedMoves;
        private int attackModifier;
        private GUIStyle moveButtonStyle;
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
        [SerializeField] private int selectedFighter, selectedArena;
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
            camera.transform.position = new Vector3(0, 1.6f, -FitCameraDistance(match.A.X,match.B.X,camera.aspect));
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

        public void BeginFight() { ReleaseSelectionRender(); Reset(); selecting=false; advancedMoves=false; rulesOpen=false; arenaOpen=false; opponent.gameObject.SetActive(true);arena.gameObject.SetActive(true);fightCamera.rect=new Rect(0,0,1,1); }
        public static float FitCameraDistance(float a,float b,float aspect) => Mathf.Max(3.6f,(Mathf.Abs(a-b)+2.2f)/(2*Mathf.Tan(21*Mathf.Deg2Rad)*Mathf.Max(.3f,aspect)));
        private void OnLaunch(LaunchRequest request) { russian=string.IsNullOrEmpty(request.locale)||request.locale.StartsWith("ru"); Reset(); selecting = true; }
        private void RebuildViews()
        {
            if (player != null) Destroy(player.gameObject);
            if (opponent != null) Destroy(opponent.gameObject);
            if (arena != null) Destroy(arena.gameObject);
            int cpu = selectedFighter==4?3:(selectedFighter + 3) % FighterNames.Length;
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
            ReleaseSelectionRender();if(selectionBackdrop!=null)Destroy(selectionBackdrop);if(selectionHalo!=null)Destroy(selectionHalo);if(circle!=null)Destroy(circle);
        }
        private void OnApplicationPause(bool value)
        {
            backgrounded = value;stickFinger=-1;stickInput=Vector2.zero;strikeGesture.Cancel();
            accumulator = 0;
            pendingAttack = -1;
        }
        private void Reset()
        {
            var defence=session!=null?(int)session.Defence:0;
            session = new FightSession(training,targetWins,roundSeconds);session.SetDummyDefence(defence); accumulator = 0; bot = new BotController(42,difficulty);
            pendingAttack = -1; paused = false; contactFreeze=0;contactMatch=null;stickFinger=-1;stickInput=Vector2.zero;strikeGesture.Cancel();
        }

        // All touch positions converted into the same logical coordinates as GUI.
        private static Rect UiArea => Screen.safeArea.width>1 && Screen.safeArea.height>1?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
        private Vector2 Position(Vector2 screen) {var area=UiArea;return new Vector2((screen.x-area.x)*960/area.width,(area.yMax-screen.y)*540/area.height);}
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
        private static Rect MainAttackArea(int index) => new Rect(755+index%2*85,350+index/2*85,74,74);
        private static Rect MoveMenuArea => new Rect(690,80,250,40);
        private static Rect AttackArea(int index) => new Rect(630 + index % 5 * 62, 260 + index / 5 * 48, 60, 44);

        private void Update()
        {
            if (!selecting && (Pressed(new Rect(360, 20, 90, 45)) || Input.GetKeyDown(KeyCode.Escape)))
            { paused = !paused; accumulator = 0; pendingAttack = -1;stickFinger=-1;stickInput=Vector2.zero;strikeGesture.Cancel(); }
            if (!selecting && (Input.GetKeyDown(KeyCode.R) || Pressed(new Rect(460, 20, 90, 45)))) Reset();
            if ((!selecting && Pressed(new Rect(560, 20, 90, 45))) || Input.GetKeyDown(KeyCode.Q))
            { if (bridge != null) bridge.Exit(""); return; }
            if (selecting)
            {
                previewMessageTime=Mathf.Max(0,previewMessageTime-Time.deltaTime);UpdateSelection();
                return;
            }
            if (paused || backgrounded || Time.timeScale == 0) return;
            UpdateJoystick();
            UpdateStrikeGestures();
            bool crouch = Input.GetKey(KeyCode.DownArrow) || Held(new Rect(270,455,78,52));
            bool upper = Input.GetKey(KeyCode.UpArrow);
            bool backVariant=Input.GetKey(KeyCode.LeftArrow);
            attackModifier=Input.GetKey(KeyCode.DownArrow)?4:upper?8:backVariant?12:0;
            if(Pressed(MoveMenuArea))advancedMoves=!advancedMoves;
            for (int i = 0; i < AttackKeys.Length; i++)
                if (Input.GetKeyDown(AttackKeys[i]) || (!advancedMoves && Pressed(MainAttackArea(i)))) pendingAttack=MainMove(i)+attackModifier;
            if(advancedMoves)for (int i = 0; i < Moves.All.Length; i++) if (Pressed(AttackArea(i))) pendingAttack = i;
            var extras = new[] { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period };
            for (int i = 0; i < extras.Length; i++) if (Input.GetKeyDown(extras[i])) pendingAttack = 16+i;
            float direction = Input.GetKey(KeyCode.RightArrow)?1:Input.GetKey(KeyCode.LeftArrow)?-1:stickInput.x;
            float side = Input.GetKey(KeyCode.W)?1:Input.GetKey(KeyCode.S)?-1:-stickInput.y;
            bool guard = Held(new Rect(660,423,78,78)) || Input.GetKey(KeyCode.LeftShift);
            accumulator += Mathf.Min(Time.deltaTime, .1f);
            while (accumulator >= 1f / Match.TickRate)
            {
                accumulator -= 1f / Match.TickRate;
                if(contactFreeze>0){contactFreeze--;continue;}
                match.A.Guard = guard; match.A.Crouch = crouch;
                if (pendingAttack >= 0) { session.QueueAttack(true, pendingAttack); pendingAttack = -1; }
                match.Walk(match.A, direction,side);
                bot.Step(session);
                if(contactMatch!=match){strikeGesture.Cancel();contactMatch=match;physicalContacts=new PhysicalContacts(match,player,opponent);}
                int contactA=match.A.HitSerial, contactB=match.B.HitSerial;
                session.Step();feedback.Observe(match,physicalContacts.ImpactA,physicalContacts.ImpactB);
                if(match.A.HitSerial!=contactA || match.B.HitSerial!=contactB)contactFreeze=(match.A.HitSerial!=contactA && match.A.LastCounter)||(match.B.HitSerial!=contactB && match.B.LastCounter)?5:3;
            }
            float center=(match.A.X+match.B.X)*.5f;
            float distance=FitCameraDistance(match.A.X,match.B.X,fightCamera.aspect);
            fightCamera.transform.position=Vector3.Lerp(fightCamera.transform.position,new Vector3(center,1.6f,(match.A.Z+match.B.Z)*.5f-distance),Time.deltaTime*4);
            fightCamera.transform.LookAt(new Vector3(center,1,(match.A.Z+match.B.Z)*.5f));
            fightCamera.transform.position+=Vector3.right*Mathf.Sin(Time.time*87)*feedback.Shake;
            player.PoseTowards(match.A,match.B,contactFreeze>0?0:Time.deltaTime);
            opponent.PoseTowards(match.B,match.A,contactFreeze>0?0:Time.deltaTime);
        }

        private void OnGUI()
        {
            if (session == null) return;
            var saved = GUI.matrix;
            var area=UiArea;GUI.matrix = Matrix4x4.TRS(new Vector3(area.x,Screen.height-area.yMax,0), Quaternion.identity, new Vector3(area.width / 960f, area.height / 540f, 1));
            if (selecting)
            {
                SelectionGUI(); GUI.matrix=saved; return;

            }
            GUI.Label(new Rect(20,20,330,40), $"LighFight  R{session.Round}   {session.WinsA} : {session.WinsB}   {match.A.Health} : {match.B.Health}");
            GUI.Box(new Rect(360,20,90,45), paused ? T("Продолжить","Resume") : T("Пауза","Pause"));
            GUI.Box(new Rect(460,20,90,45), T("Заново","Restart"));
            GUI.Box(new Rect(560,20,90,45), T("Выход","Exit"));
            GUI.Label(new Rect(20,80,650,35), T("Джойстик: движение · руки / ноги: удары · удерживай блок","Joystick: move · hands / feet: attack · hold guard"));
            GUI.Box(MoveMenuArea,advancedMoves?T("Скрыть приёмы","Hide moves"):T("Все 25 приёмов","All 25 moves"));
            if (paused) GUI.Label(new Rect(430,160,150,35), T("ПАУЗА","PAUSED"));
            GUI.Label(new Rect(700,20,180,40), training?T("ТРЕНИРОВКА","PRACTICE"):$"{(session.RemainingTicks + 59) / 60}");
            var color=GUI.color;GUI.color=new Color(.20f,.85f,.55f);
            GUI.DrawTexture(new Rect(20,130,350*match.A.Health/150f,14),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(590,130,350*match.B.Health/150f,14),Texture2D.whiteTexture);GUI.color=color;
            if(match.A.Stun>0)GUI.Label(new Rect(20,160,220,30),match.A.LastBlocked?T("БЛОК","BLOCK"):match.A.LastCounter?T("КОНТРУДАР","COUNTER"):"−"+match.A.LastDamage);
            if(match.B.Stun>0)GUI.Label(new Rect(740,160,220,30),match.B.LastBlocked?T("БЛОК","BLOCK"):match.B.LastCounter?T("КОНТРУДАР","COUNTER"):"−"+match.B.LastDamage);
            if (session.Phase != FightPhase.Fighting) GUI.Label(new Rect(410,200,240,35), session.Phase == FightPhase.Complete ? (session.Winner == 0 ? T("ПОБЕДА В МАТЧЕ","MATCH WON") : T("МАТЧ ПРОИГРАН","MATCH LOST")) : session.RoundWinner < 0 ? T("НИЧЬЯ","DRAW") : session.RoundWinner == 0 ? T("РАУНД ВЫИГРАН","ROUND WON") : T("РАУНД ПРОИГРАН","ROUND LOST"));
            DrawBattleControls();
            if(moveButtonStyle==null)moveButtonStyle=new GUIStyle(GUI.skin.box){fontSize=11,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            if(advancedMoves)for (int i = 0; i < Moves.All.Length; i++) GUI.Box(AttackArea(i), T(MoveNames[i],Moves.All[i].Id),moveButtonStyle);
            else for(int i=0;i<4;i++){var labels=new[]{"Р1","Р2","Н1","Н2"};Pill(MainAttackArea(i),labels[i]+"\n"+T(MoveNames[MainMove(i)+attackModifier],Moves.All[MainMove(i)+attackModifier].Id));}
            GUI.matrix = saved;
        }
    }
}
