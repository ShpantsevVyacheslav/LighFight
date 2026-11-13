using System;
using UnityEngine;

namespace LighFight.Runtime
{
    /// <summary>
    /// Free-roam 3D world exploration controller for the Rostov-on-Don Embankment.
    /// Enables full 360-degree character navigation across the promenade, under the bridge,
    /// up/down grand staircases, along the riverfront, and to viewing lookouts.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class Rostov3DWorldExplorer : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 2.8f;
        public float sprintSpeed = 6.2f;
        public float rotationSpeed = 12.0f;
        public float jumpSpeed = 5.5f;
        public float gravity = 20.0f;

        [Header("Camera Orbit")]
        public Camera explorationCamera;
        public float cameraDistance = 4.2f;
        public float minCameraDistance = 2.0f;
        public float maxCameraDistance = 9.0f;
        public float cameraHeight = 1.6f;
        public float mouseSensitivity = 3.0f;

        [Header("HUD")]
        public bool showHUD = true;

        private CharacterController controller;
        private Animator animator;
        private FighterView fighterView;

        private float yaw = 0.0f;
        private float pitch = 12.0f;
        private float currentDistance;
        private float verticalVelocity = 0.0f;
        private Vector3 cameraVelocity;

        private string currentPOIName = "Центральный гранитный променад · Набережная реки Дон";
        private string currentPOIDesc = "Свободное перемещение по набережной адмирала Ушакова";

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!controller) controller = gameObject.AddComponent<CharacterController>();
            controller.radius = 0.35f;
            controller.height = 1.8f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 46.0f;

            animator = GetComponentInChildren<Animator>();
            fighterView = GetComponent<FighterView>();
            currentDistance = cameraDistance;
        }

        private void Start()
        {
            if (!explorationCamera)
            {
                explorationCamera = Camera.main;
                if (!explorationCamera)
                {
                    var camGo = GameObject.Find("Review Camera") ?? GameObject.Find("Camera");
                    if (camGo) explorationCamera = camGo.GetComponent<Camera>();
                }
            }

            if (explorationCamera)
            {
                yaw = transform.eulerAngles.y;
            }
        }

        private void Update()
        {
            HandleQuickTeleport();
            HandleCameraOrbit();
            HandleMovement();
            UpdatePOI();
        }

        private void HandleQuickTeleport()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                // 1. Riverfront viewing lookout overlooking Rostov Arena on the LEFT
                controller.enabled = false;
                transform.position = new Vector3(-12.0f, 0.0f, 0.8f);
                transform.rotation = Quaternion.Euler(0, 18f, 0);
                yaw = 18f;
                controller.enabled = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                // 2. Under Voroshilovsky Bridge
                controller.enabled = false;
                transform.position = new Vector3(0.0f, 0.0f, -0.6f);
                transform.rotation = Quaternion.Euler(0, 0f, 0);
                yaw = 0f;
                controller.enabled = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                // 3. Grand connecting staircase
                controller.enabled = false;
                transform.position = new Vector3(-32.0f, 0.1f, -7.5f);
                transform.rotation = Quaternion.Euler(0, 180f, 0);
                yaw = 180f;
                controller.enabled = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                // 4. Upper terrace along Beregovaya Street
                controller.enabled = false;
                transform.position = new Vector3(0.0f, 2.8f, -20.0f);
                transform.rotation = Quaternion.Euler(0, 0f, 0);
                yaw = 0f;
                controller.enabled = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                // 5. Eastern Promenade
                controller.enabled = false;
                transform.position = new Vector3(45.0f, 0.0f, -4.0f);
                transform.rotation = Quaternion.Euler(0, -90f, 0);
                yaw = -90f;
                controller.enabled = true;
            }
        }

        private void HandleCameraOrbit()
        {
            if (!explorationCamera) return;

            // Mouse orbit when left or right mouse held, or Q/E keyboard keys
            float mouseX = 0f;
            float mouseY = 0f;

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY = -Input.GetAxis("Mouse Y") * mouseSensitivity;
            }

            if (Input.GetKey(KeyCode.Q)) mouseX -= 2.5f;
            if (Input.GetKey(KeyCode.E)) mouseX += 2.5f;

            yaw += mouseX;
            pitch = Mathf.Clamp(pitch + mouseY, -12.0f, 65.0f);

            // Zoom distance
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance = Mathf.Clamp(currentDistance - scroll * 4.0f, minCameraDistance, maxCameraDistance);
            }

            // Camera target point
            Vector3 targetPivot = transform.position + Vector3.up * cameraHeight;
            Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 targetCamPos = targetPivot - (orbitRot * Vector3.forward * currentDistance);

            // Collision damping so camera does not clip through bridge pillars
            if (Physics.SphereCast(targetPivot, 0.25f, targetCamPos - targetPivot, out RaycastHit hit, currentDistance))
            {
                targetCamPos = hit.point + hit.normal * 0.25f;
            }

            explorationCamera.transform.position = Vector3.SmoothDamp(explorationCamera.transform.position, targetCamPos, ref cameraVelocity, 0.05f);
            explorationCamera.transform.LookAt(targetPivot + Vector3.up * 0.15f);
        }

        private void HandleMovement()
        {
            if (!controller) return;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            Vector3 moveDirection = Vector3.zero;
            if (explorationCamera)
            {
                Vector3 camForward = Vector3.Scale(explorationCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 camRight = Vector3.Scale(explorationCamera.transform.right, new Vector3(1, 0, 1)).normalized;
                moveDirection = (camForward * v + camRight * h);
            }
            else
            {
                moveDirection = new Vector3(h, 0, v);
            }

            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
            float inputMagnitude = Mathf.Clamp01(moveDirection.magnitude);

            if (inputMagnitude > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Gravity & Jumping
            if (controller.isGrounded)
            {
                verticalVelocity = -0.5f;
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    verticalVelocity = jumpSpeed;
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 finalMove = moveDirection.normalized * (inputMagnitude * currentSpeed);
            finalMove.y = verticalVelocity;

            controller.Move(finalMove * Time.deltaTime);

            // Animate humanoid safely
            if (animator && animator.runtimeAnimatorController != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float)
                    {
                        animator.SetFloat("Speed", inputMagnitude * (isSprinting ? 2.0f : 1.0f));
                        break;
                    }
                }
            }
        }

        private void UpdatePOI()
        {
            Vector3 pos = transform.position;

            // 1. Under Voroshilovsky Bridge
            if (Mathf.Abs(pos.x) <= 15.0f && pos.z >= -6.0f && pos.z <= 6.0f)
            {
                currentPOIName = "🌉 Под Ворошиловским мостом · Центральный пилон";
                currentPOIDesc = "Массивные железобетонные колонны первого пилона на гранитном мощении набережной";
            }
            // 2. Riverfront Viewing Lookout overlooking Rostov Arena (Left)
            else if (pos.z >= 0.5f)
            {
                currentPOIName = "🏟️ Смотровая площадка набережной · Вид на «Ростов Арену»";
                currentPOIDesc = "Панорамный вид через Дон: стадион со шпилями СЛЕВА, Ворошиловский мост СПРАВА";
            }
            // 3. Grand Connecting Staircases
            else if (Mathf.Abs(Mathf.Abs(pos.x) - 32.0f) <= 5.0f && pos.z >= -14.5f && pos.z <= -8.0f)
            {
                currentPOIName = "🏛️ Парадный гранитный сход между ярусами";
                currentPOIDesc = "14 ступеней и плавный пандус, соединяющий нижний променад с улицей Береговой";
            }
            // 4. Upper Terrace (Beregovaya Street)
            else if (pos.z <= -13.5f)
            {
                currentPOIName = "🏙️ Верхний ярус набережной · Улица Береговая";
                currentPOIDesc = "Панорамный тротуар вдоль подпорной стены с видом на Дон и город";
            }
            // 5. General Lower Promenade
            else
            {
                currentPOIName = "🌊 Центральный гранитный променад · Набережная реки Дон";
                currentPOIDesc = "Широкая пешеходная аллея с фонарями, скамьями и чугунным парапетом";
            }
        }

        private void OnGUI()
        {
            if (!showHUD) return;

            int screenW = Screen.width;
            int screenH = Screen.height;

            // Location banner at top-center
            int bannerW = Mathf.Min(680, screenW - 40);
            int bannerH = 68;
            int bannerX = (screenW - bannerW) / 2;

            GUI.color = new Color(0.08f, 0.12f, 0.18f, 0.88f);
            GUI.Box(new Rect(bannerX, 16, bannerW, bannerH), GUIContent.none);
            GUI.color = Color.white;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.98f, 0.92f, 0.72f);

            var descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            descStyle.normal.textColor = new Color(0.80f, 0.88f, 0.96f);

            GUI.Label(new Rect(bannerX + 10, 20, bannerW - 20, 26), currentPOIName, titleStyle);
            GUI.Label(new Rect(bannerX + 10, 48, bannerW - 20, 24), currentPOIDesc, descStyle);

            // Controls hint at bottom-left
            int helpW = Mathf.Min(620, screenW - 40);
            int helpH = 46;
            GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);
            GUI.Box(new Rect(20, screenH - helpH - 20, helpW, helpH), GUIContent.none);
            GUI.color = Color.white;

            var helpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            helpStyle.normal.textColor = new Color(0.85f, 0.90f, 0.95f);
            GUI.Label(new Rect(30, screenH - helpH - 18, helpW - 20, helpH),
                "🎮 [W][A][S][D] / Стрелки — движение   |   [Shift] — бег   |   [Пробел] — прыжок   |   [1..5] — телепорт\n🖱️ [ЛКМ / ПКМ / Q, E] — обзор камеры   |   [Колесико] — зум камеры", helpStyle);

            // Compass indicator at top-right
            int compassW = 210;
            int compassH = 40;
            GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.82f);
            GUI.Box(new Rect(screenW - compassW - 20, 16, compassW, compassH), GUIContent.none);
            GUI.color = Color.white;

            var compassStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            compassStyle.normal.textColor = new Color(0.55f, 0.85f, 1.0f);
            GUI.Label(new Rect(screenW - compassW - 20, 18, compassW, compassH), "🧭 Стадион «Ростов Арена»:\n◄ СЛЕВА через реку Дон", compassStyle);
        }
    }
}
