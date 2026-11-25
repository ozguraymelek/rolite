using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcFps
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")] 
        public PlayerRuntimeCharacterState RuntimeCharacterState;
        
        [Header("Hareket Ayarları")] public float walkSpeed = 5f;
        public float sprintSpeed = 8f;
        public float jumpHeight = 1.2f;
        public float gravity = -19.62f;

        [Header("Kamera Ayarları")] public Transform cameraHolder;
        public float mouseSensitivity = 0.15f; // Mouse Delta piksel bazlı olduğu için düşük değer kullanılır

        private CharacterController controller;
        private PlayerInputActions inputActions;
        public PlayerInputActions InputActions => inputActions;

        private float xRotation = 0f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputActions = new PlayerInputActions();

            // Input olaylarını (Events) dinleme
            inputActions.Runtime.Move.performed += ctx => RuntimeCharacterState.moveInput = ctx.ReadValue<Vector2>();
            inputActions.Runtime.Move.canceled += ctx => RuntimeCharacterState.moveInput = Vector2.zero;

            inputActions.Runtime.Look.performed += ctx => RuntimeCharacterState.lookInput = ctx.ReadValue<Vector2>();
            inputActions.Runtime.Look.canceled += ctx => RuntimeCharacterState.lookInput = Vector2.zero;

            inputActions.Runtime.Aim.performed += ctx => RuntimeCharacterState.isAiming = true;
            inputActions.Runtime.Aim.canceled += ctx => RuntimeCharacterState.isAiming = false;

            inputActions.Runtime.Sprint.performed += ctx => RuntimeCharacterState.isSprinting = true;
            inputActions.Runtime.Sprint.canceled += ctx => RuntimeCharacterState.isSprinting = false;

            inputActions.Runtime.Jump.performed += ctx => Jump();
        }

        void OnEnable() => inputActions.Runtime.Enable();
        void OnDisable() => inputActions.Runtime.Disable();

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            Look();
            Move();
        }

        void Look()
        {
            // Yeni Input Sisteminde Mouse Delta zaten kare (frame) başına değişimi verir, Time.deltaTime ile çarpılmaz
            float mouseX = RuntimeCharacterState.lookInput.x * mouseSensitivity;
            float mouseY = RuntimeCharacterState.lookInput.y * mouseSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        void Move()
        {
            if (controller.isGrounded && RuntimeCharacterState.velocity.y < 0)
            {
                RuntimeCharacterState.velocity.y = -2f;
            }

            float currentSpeed = RuntimeCharacterState.isSprinting ? sprintSpeed : walkSpeed;
            Vector3 move = transform.right * RuntimeCharacterState.moveInput.x + transform.forward * RuntimeCharacterState.moveInput.y;
            controller.Move(move * (currentSpeed * Time.deltaTime));

            RuntimeCharacterState.velocity.y += gravity * Time.deltaTime;
            controller.Move(RuntimeCharacterState.velocity * Time.deltaTime);
        }

        void Jump()
        {
            if (controller.isGrounded)
            {
                RuntimeCharacterState.velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }
}

