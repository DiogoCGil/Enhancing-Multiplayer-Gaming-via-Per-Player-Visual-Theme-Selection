using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using Photon.Pun;
using Photon.Realtime;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonControllerCostum : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        [HideInInspector] public StaminaController _staminaController;


        private bool m_IsCrouching = false;
        [SerializeField] private float crouchHeight = 1.5f; 
        [SerializeField] private float standHeight = 2.0f;
        [SerializeField] private float crouchSpeed = 2.0f; 
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector3 cameraStandPos;
        [SerializeField] private Vector3 cameraCrouchPos;

        private Vector3 cameraTargetPos;

        private GameObject Gun;
        private Vector3 gunStandPos;
        private Vector3 gunCrouchPos;
        private Vector3 gunTargetPos;

        public Transform ikPointsParent;
        private Vector3 ikStandPos;
        private Vector3 ikCrouchPos;
        private Vector3 ikTargetPos;
        
        public  PlayerHealth myHealth;
        // Use this for initialization
        private void Start()
        {

            _staminaController = GetComponent<StaminaController>();
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);

            cameraTransform = m_Camera.transform;
            cameraStandPos = cameraTransform.localPosition;
            cameraCrouchPos = new Vector3(cameraStandPos.x, cameraStandPos.y - 0.5f, cameraStandPos.z);
           
            myHealth = GetComponent<PlayerHealth>();

            cameraTargetPos = cameraStandPos;
            if (Gun == null)
            {
                Transform modelTransform = transform.Find("Model");
                if (modelTransform != null)
                {
                    Transform gunTransform = FindChildWithTag(modelTransform,"Gun");
                    if (gunTransform != null)
                    {
                        GameGraphicChanger net = FindObjectOfType<GameGraphicChanger>();
                        if (net != null)
                        {
                            string currentStyle = net.typeOfStyle;
                            Transform gunchosen = FindChildWithTag(gunTransform, currentStyle);
                            if (gunchosen != null)
                            {
                                Gun = gunchosen.gameObject;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[CrouchGun] NetworkManager não encontrado na cena.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[CrouchGun] Não foi possível encontrar o filho 'Gun' dentro de 'Model'.");
                    }
                }
                else
                {
                    Debug.LogWarning("[CrouchGun] Não foi possível encontrar o filho 'Model'.");
                }
            }
            if (Gun != null)
            {
                gunStandPos = Gun.transform.localPosition;
                gunCrouchPos = gunStandPos + new Vector3(0f, -0.52f, -0.1f); 
                gunTargetPos = gunStandPos;
            }
            if (ikPointsParent != null)
            {
                ikStandPos = ikPointsParent.localPosition;
                ikCrouchPos = ikStandPos + new Vector3(0f, -0.5f, 0f); // ou outro valor que aches natural
                ikTargetPos = ikStandPos;
            }
        }
        public void SetRunSpeed(float speed){
            m_RunSpeed = speed;
        }

        // Update is called once per frame
        private void Update()
        {
            if (PhotonView.Get(this).IsMine && GetComponent<PlayerHealth>().isDead)
                return;
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                StartCrouch();
            }
            else if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                StopCrouch();
            }
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && !m_IsCrouching && m_CharacterController.isGrounded)
            {
                if (CrossPlatformInputManager.GetButtonDown("Jump"))
                    m_Jump = true;
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;


            if (ikPointsParent != null || Gun != null)
            {
                bool isCrouchMoving = m_IsCrouching && m_Input.sqrMagnitude > 0.01f;

                Vector3 crouchOffset = m_IsCrouching
                ? (isCrouchMoving ? new Vector3(0.4f, -0.05f, 0f) : new Vector3(0.15f, 0f, 0f))
                : Vector3.zero;

                if (ikPointsParent != null)
                {
                    Vector3 targetIKPos = m_IsCrouching ? ikCrouchPos + crouchOffset : ikStandPos;
                    ikPointsParent.localPosition = Vector3.Lerp(ikPointsParent.localPosition, targetIKPos, Time.deltaTime * 10f);
                }

                if (Gun != null && !myHealth.isDead)
                {
                    Vector3 targetGunPos = m_IsCrouching ? gunCrouchPos + crouchOffset : gunStandPos;
                    Gun.transform.localPosition = Vector3.Lerp(Gun.transform.localPosition, targetGunPos, Time.deltaTime * 10f);
                }
            }
             
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }

        public void PlayerJump(){
            m_MoveDir.y = m_JumpSpeed;
            PlayJumpSound();
            m_Jump = false;
            m_Jumping = true;
        }
        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x*speed;
            m_MoveDir.z = desiredMove.z*speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    _staminaController.StaminaJump();
                }
            }
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            if (!m_UseHeadBob)
                return;

            Vector3 basePos;
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                basePos = m_HeadBob.DoHeadBob(
                    m_CharacterController.velocity.magnitude +
                    (speed * (m_IsWalking ? 1f : m_RunstepLenghten))
                );
            }
            else
            {
                basePos = m_OriginalCameraPosition;
            }

            basePos.y -= m_JumpBob.Offset();

            // Faz Lerp entre a posição base (headbob) e a posição de crouch ou normal
            m_Camera.transform.localPosition = Vector3.Lerp(
                m_Camera.transform.localPosition,
                cameraTargetPos + (basePos - m_OriginalCameraPosition),
                Time.deltaTime * 10f
            );
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            if (m_IsCrouching)
            {
                m_IsWalking = true;
            }
            else
            {
                m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
            }
#endif

            if(m_IsWalking)
            {
                _staminaController.weAreSprinting =false;
            }
            if(!m_IsWalking && m_CharacterController.velocity.sqrMagnitude >0)
            {
                if(_staminaController.playerStamina >0){
                    _staminaController.weAreSprinting = true;
                    _staminaController.Sprinting();
                }
                else
                {
                    m_IsWalking = true;
                }
            }
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            if (m_IsCrouching)
            {
                speed = crouchSpeed;
            }
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }

        private void StartCrouch()
        {
            m_IsCrouching = true;
            m_CharacterController.height = crouchHeight;
            m_CharacterController.center = new Vector3(0, crouchHeight / 2f, 0);
            cameraTargetPos = cameraCrouchPos;
            gunTargetPos = gunCrouchPos;

            ikTargetPos = ikCrouchPos;
            
        }

        private void StopCrouch()
        {
            m_IsCrouching = false;
            m_CharacterController.height = standHeight;
            m_CharacterController.center = new Vector3(0, standHeight / 2f, 0);
            cameraTargetPos = cameraStandPos;
            gunTargetPos = gunStandPos;

            ikTargetPos = ikStandPos;

            
        }
        private Transform FindChildWithTag(Transform parent, string tag)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) // ✅ Busca em objetos desativados
            {
                if (child.CompareTag(tag))
                {
                    return child;
                }
            }
            return null;
        }

        
    }
    
}
