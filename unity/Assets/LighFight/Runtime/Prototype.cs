using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Explicit scene component; never auto-spawn in downloaded production scenes.
    public sealed class Prototype : MonoBehaviour
    {
        private static readonly KeyCode[] AttackKeys = { KeyCode.J, KeyCode.K, KeyCode.U, KeyCode.I };
        private Match match;
        private Transform player, opponent;
        private float accumulator;
        private int botCooldown, pendingAttack = -1;
        private bool paused, backgrounded;
        private readonly System.Random random = new System.Random(42);
        private HostBridge bridge;

        private void Start()
        {
            match = new Match();
            player = Body("Player", Color.cyan);
            opponent = Body("CPU", new Color(1, .35f, .25f));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.localScale = new Vector3(12, .2f, 6);
            floor.transform.position = new Vector3(0, -.1f, 0);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 3.2f, -8);
            camera.transform.LookAt(new Vector3(0, 1, 0));
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            bridge = FindFirstObjectByType<HostBridge>();
            if (bridge != null) bridge.LaunchRequested += OnLaunch;
        }

        private void OnLaunch(LaunchRequest request) => Reset();
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
            match = new Match(); accumulator = 0; botCooldown = 0;
            pendingAttack = -1; paused = false;
        }

        private Transform Body(string label, Color color)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = label;
            body.GetComponent<Renderer>().material.color = color;
            return body.transform;
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
        private static Rect AttackArea(int index) => new Rect(630 + index % 4 * 78, 330 + index / 4 * 60, 75, 55);

        private void Update()
        {
            if (Pressed(new Rect(360, 20, 90, 45)) || Input.GetKeyDown(KeyCode.Escape))
            { paused = !paused; accumulator = 0; pendingAttack = -1; }
            if (Input.GetKeyDown(KeyCode.R) || Pressed(new Rect(460, 20, 90, 45))) Reset();
            if (Pressed(new Rect(560, 20, 90, 45)) || Input.GetKeyDown(KeyCode.Q))
            { if (bridge != null) bridge.Exit(""); return; }
            if (paused || backgrounded || Time.timeScale == 0) return;
            bool crouch = Held(new Rect(175, 440, 70, 70)) || Input.GetKey(KeyCode.DownArrow);
            bool upper = Held(new Rect(100, 365, 70, 65)) || Input.GetKey(KeyCode.UpArrow);
            for (int i = 0; i < AttackKeys.Length; i++)
                if (Input.GetKeyDown(AttackKeys[i])) pendingAttack = i + (crouch ? 4 : upper ? 8 : 0);
            for (int i = 0; i < Moves.All.Length; i++) if (Pressed(AttackArea(i))) pendingAttack = i;
            float direction = (Held(new Rect(100,440,70,70)) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
                (Held(new Rect(25,440,70,70)) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            bool guard = Held(new Rect(250,440,80,70)) || Input.GetKey(KeyCode.LeftShift);
            accumulator += Mathf.Min(Time.deltaTime, .1f);
            while (accumulator >= 1f / Match.TickRate)
            {
                accumulator -= 1f / Match.TickRate;
                match.A.Guard = guard; match.A.Crouch = crouch;
                if (pendingAttack >= 0) { match.Attack(match.A, pendingAttack); pendingAttack = -1; }
                match.Walk(match.A, direction);
                if (!match.Finished)
                {
                    if (Mathf.Abs(match.A.X - match.B.X) > 1.3f)
                        match.Walk(match.B, Mathf.Sign(match.A.X - match.B.X));
                    match.B.Guard = botCooldown > 35;
                    if (--botCooldown <= 0) { match.Attack(match.B, random.Next(Moves.All.Length)); botCooldown = 60; }
                }
                match.Step();
            }
            Pose(player, match.A); Pose(opponent, match.B);
        }

        private void Pose(Transform body, Fighter fighter)
        {
            body.position = new Vector3(fighter.X, fighter.Crouch ? .65f : 1, 0);
            body.localScale = new Vector3(.7f, fighter.Crouch ? .65f : 1, .7f);
            float lean = fighter.Attack.HasValue ? Mathf.Sin(Mathf.PI *
                Mathf.Clamp01((float)fighter.Frame / fighter.Attack.Value.Duration)) * 15 : 0;
            body.rotation = Quaternion.Euler(0,0,lean * (fighter == match.A ? -1 : 1));
        }

        private void OnGUI()
        {
            if (match == null) return;
            var saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 960f, Screen.height / 540f, 1));
            GUI.Label(new Rect(20,20,330,40), $"LighFight prototype    {match.A.Health} : {match.B.Health}");
            GUI.Box(new Rect(360,20,90,45), paused ? "Resume" : "Pause");
            GUI.Box(new Rect(460,20,90,45), "Restart");
            GUI.Box(new Rect(560,20,90,45), "Exit");
            GUI.Label(new Rect(20,80,900,35), "Arrows + J K U I. Up/Down: alternate attacks. Shift: guard.");
            if (paused) GUI.Label(new Rect(430,160,150,35), "PAUSED");
            if (match.Finished) GUI.Label(new Rect(410,200,200,35), match.A.Health == match.B.Health ? "DRAW" : match.A.Health > 0 ? "PLAYER WINS" : "CPU WINS");
            GUI.Box(new Rect(25,440,70,70), "←"); GUI.Box(new Rect(100,440,70,70), "→");
            GUI.Box(new Rect(175,440,70,70), "↓"); GUI.Box(new Rect(100,365,70,65), "↑");
            GUI.Box(new Rect(250,440,80,70), "Guard");
            for (int i = 0; i < Moves.All.Length; i++) GUI.Box(AttackArea(i), Moves.All[i].Id);
            GUI.matrix = saved;
        }
    }
}
