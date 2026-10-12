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
        private Rect StickArea=>ControlRect(new Rect(30,355,160,160));
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
            light.intensity = 1.2f; light.shadows = LightShadows.None;
            RenderSettings.ambientLight = new Color(.45f,.43f,.4f);
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            LoadOptions();
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
            int cpu = OpponentIndex;
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
        private bool Held(Rect area,bool circular=false)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled &&
                    ControlContains(area,Position(touch.position),circular)) return true;
            }
            return Input.touchCount == 0 && Input.GetMouseButton(0) && ControlContains(area,Position(Input.mousePosition),circular);
        }
        private bool Pressed(Rect area,bool circular=false)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && ControlContains(area,Position(touch.position),circular)) return true;
            }
            return Input.touchCount == 0 && Input.GetMouseButtonDown(0) && ControlContains(area,Position(Input.mousePosition),circular);
        }
        private Rect MainAttackArea(int index) => MainAttackRect(index,controlsScale,leftHanded);
        private static Rect MoveMenuArea => new Rect(690,80,250,40);
        private static Rect AttackArea(int index) => new Rect(630 + index % 5 * 62, 260 + index / 5 * 48, 60, 44);

        private void Update()
        {
            if((!selecting && Pressed(new Rect(888,12,52,40))) || Input.GetKeyDown(KeyCode.Escape)){if(optionsOpen){optionsOpen=false;paused=false;}else OpenOptions();}
            if(optionsOpen)return;
            if (selecting)
            {
                previewMessageTime=Mathf.Max(0,previewMessageTime-Time.deltaTime);UpdateSelection();
                return;
            }
            if (paused || backgrounded || Time.timeScale == 0) return;
            UpdateJoystick();
            UpdateStrikeGestures();
            bool crouch = Input.GetKey(KeyCode.DownArrow) || Held(CrouchArea,true);
            bool upper = Input.GetKey(KeyCode.UpArrow);
            bool backVariant=Input.GetKey(KeyCode.LeftArrow);
            attackModifier=Input.GetKey(KeyCode.DownArrow)?4:upper?8:backVariant?12:0;
            if(Pressed(MoveMenuArea))advancedMoves=!advancedMoves;
            for (int i = 0; i < AttackKeys.Length; i++)
                if (Input.GetKeyDown(AttackKeys[i]) || (!gesturesEnabled && !advancedMoves && Pressed(MainAttackArea(i),true))) pendingAttack=MainAttackIndex(i);
            if(advancedMoves)for (int i = 0; i < Moves.All.Length; i++) if (Pressed(AttackArea(i))) pendingAttack = i;
            var extras = new[] { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period };
            for (int i = 0; i < extras.Length; i++) if (Input.GetKeyDown(extras[i])) pendingAttack = 16+i;
            float direction = Input.GetKey(KeyCode.RightArrow)?1:Input.GetKey(KeyCode.LeftArrow)?-1:stickInput.x;
            float side = Input.GetKey(KeyCode.W)?1:Input.GetKey(KeyCode.S)?-1:-stickInput.y;
            bool guard = false;
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
            if(cameraShake)fightCamera.transform.position+=Vector3.right*Mathf.Sin(Time.time*87)*feedback.Shake;
            player.PoseTowards(match.A,match.B,contactFreeze>0?0:Time.deltaTime);
            opponent.PoseTowards(match.B,match.A,contactFreeze>0?0:Time.deltaTime);
        }

        private int OpponentIndex => selectedOpponent>=0?selectedOpponent:selectedFighter==4?3:selectedFighter==3?4:(selectedFighter+3)%FighterNames.Length;
        private void OnGUI()
        {
            if (session == null) return;
            var saved = GUI.matrix;
            var area=UiArea;GUI.matrix = Matrix4x4.TRS(new Vector3(area.x,Screen.height-area.yMax,0), Quaternion.identity, new Vector3(area.width / 960f, area.height / 540f, 1));
            if (selecting)
            {
                SelectionGUI(); if(optionsOpen)OptionsGUI(); GUI.matrix=saved; return;

            }
            bool wasEnabled=GUI.enabled; GUI.enabled=wasEnabled&&!optionsOpen;
            SelectionResources();
            Text(new Rect(24,12,350,28),T(FighterNames[selectedFighter],FighterEnglish[selectedFighter]),14);
            Text(new Rect(600,12,280,28),T(FighterNames[OpponentIndex],FighterEnglish[OpponentIndex]),14,null,TextAnchor.MiddleRight);
            Text(new Rect(420,10,120,30),training?"∞":$"{(session.RemainingTicks+59)/60}",22,null,TextAnchor.MiddleCenter);
            if(Pill(new Rect(888,12,52,40),"☰"))OpenOptions();
            Card(new Rect(24,48,350,7),new Color(.15f,.2f,.28f));Card(new Rect(24,48,350*match.A.Health/150f,7),selectionAccent);
            Card(new Rect(586,48,294,7),new Color(.15f,.2f,.28f));Card(new Rect(586,48,294*match.B.Health/150f,7),new Color(1,.65f,.25f));
            if(session.Phase!=FightPhase.Fighting)Text(new Rect(340,90,280,45),session.RoundWinner==0?T("ПОБЕДА","VICTORY"):T("РАУНД ЗАВЕРШЁН","ROUND OVER"),28,null,TextAnchor.MiddleCenter);
            DrawBattleControls();
            if(moveButtonStyle==null)moveButtonStyle=new GUIStyle(GUI.skin.box){fontSize=11,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            if(advancedMoves)for (int i = 0; i < Moves.All.Length; i++) GUI.Box(AttackArea(i), T(MoveNames[i],Moves.All[i].Id),moveButtonStyle);
            else if(!gesturesEnabled)DrawMainAttackControls();
            GUI.enabled=wasEnabled;
            if(optionsOpen)OptionsGUI();
            GUI.matrix = saved;
        }
    }
}
