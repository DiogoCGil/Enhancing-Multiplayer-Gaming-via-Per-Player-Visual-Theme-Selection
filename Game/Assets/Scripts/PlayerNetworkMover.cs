using Photon.Pun;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;
using System.Collections.Generic;
[RequireComponent(typeof(FirstPersonControllerCostum))]
public class PlayerNetworkMover : MonoBehaviourPunCallbacks, IPunObservable
{
        [SerializeField] public Animator animator;
        [SerializeField] private GameObject cameraObject;
        [SerializeField] public GameObject gunObject;
        [SerializeField] public GameObject playerObject;
        [SerializeField] private Transform ikPointsParent;
        [SerializeField] private NameTag nameTag;

        // para a posição/remotes
        private Vector3 position;
        private Quaternion rotation;

        // para a arma e os IK
        private Vector3 gunStandLocal;
        private Vector3 gunCrouchLocal;
        private Vector3 gunTargetPos;

        private Vector3 ikStandLocal;
        private Vector3 ikCrouchLocal;
        private Vector3 ikTargetPos;

        // estado de crouch
        private bool isCrouching;

        private float smoothing = 10f;


        [SerializeField] private CapsuleCollider bodyCollider;
        [SerializeField] private CapsuleCollider HeadCollider;

        private float headColliderOriginalHeight;
        private Vector3 headColliderOriginalCenter;

        private float bodyColliderOriginalHeight;
        private Vector3 bodyColliderOriginalCenter;
        private Transform head;
        private Transform spine;
        private GameGraphicChanger graphicChanger;
        public GameObject isMoving;
        private Vector3 lastPosition;
        private bool wasMoving;
        void Awake()
        {
            if (photonView.IsMine)
                cameraObject.SetActive(true);
        }

        void Start()
        {
            if (gunObject != null)
            {
                gunStandLocal = gunObject.transform.localPosition;
                gunCrouchLocal = gunStandLocal + new Vector3(0.3f, -0.5f, -0.1f);
                gunTargetPos = gunStandLocal;
            }
            if (ikPointsParent != null)
            {
                ikStandLocal = ikPointsParent.localPosition;
                ikCrouchLocal = ikStandLocal + new Vector3(0.3f, -0.5f, 0f);
                ikTargetPos = ikStandLocal;
            }

            if (photonView.IsMine)
            {
                GetComponent<FirstPersonControllerCostum>().enabled = true;
                GetComponent<StaminaController>().enabled = true;

                MoveToLayer(gunObject, LayerMask.NameToLayer("Hidden"));
                MoveToLayer(playerObject, LayerMask.NameToLayer("Hidden"));
                
                
                MoveToLayer(bodyCollider.gameObject,LayerMask.NameToLayer("Ignore Raycast"));

                foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
                {
                    var tag = p.GetComponentInChildren<NameTag>();
                    if (tag != null && tag.photonView.Owner != PhotonNetwork.LocalPlayer)
                    {
                        tag.target = cameraObject.transform; 
                    }
                }
            }
            else
            {
                
                position = transform.position;
                rotation = transform.rotation;

                if (bodyCollider != null)
                {
                    bodyColliderOriginalHeight = bodyCollider.height;
                    bodyColliderOriginalCenter = bodyCollider.center;
                }
                if (HeadCollider != null)
                {
                    headColliderOriginalCenter = HeadCollider.center;
                }
            }
            if (animator != null)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);

                if (head != null)
                    Debug.Log("Cabeça encontrada: " + head.name);
                else
                    Debug.LogWarning("Osso da cabeça não encontrado!");

                if (spine != null)
                    Debug.Log("Spine encontrado: " + spine.name);
                else
                    Debug.LogWarning("Osso da coluna não encontrado!");
            }
                
            graphicChanger = FindObjectOfType<GameGraphicChanger>();
            lastPosition = transform.position;
        }

        void Update()
        {
            if (!photonView.IsMine)
            {
                var health = GetComponent<PlayerHealth>();
                if (health != null && health.isDead) return;
                if (bodyCollider != null && spine != null)
                {
                    bodyCollider.transform.position = spine.position;
                    bodyCollider.transform.rotation = spine.rotation;
                }

                // 2) Atualizar o HeadCollider com base no bone Head
                if (HeadCollider != null && head != null)
                {
                    HeadCollider.transform.position = head.position;
                    HeadCollider.transform.rotation = head.rotation;
                }
                // Movimentação remota suave
                transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * smoothing);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * smoothing);

                if (gunObject != null)
                    gunObject.transform.localPosition = Vector3.Lerp(gunObject.transform.localPosition, gunTargetPos, Time.deltaTime * smoothing);

                if (ikPointsParent != null)
                    ikPointsParent.localPosition = Vector3.Lerp(ikPointsParent.localPosition, ikTargetPos, Time.deltaTime * smoothing);

                if (animator != null)
                    animator.SetBool("IsCrouch", isCrouching);
                if (playerObject != null)
                {
                    Vector3 modelPos = playerObject.transform.localPosition;

                    float targetY = isCrouching ? 0.1f : 0f;
                    modelPos.y = Mathf.Lerp(modelPos.y, targetY, Time.deltaTime * 10f);

                    playerObject.transform.localPosition = modelPos;
                }
                if (bodyCollider != null)
                {
                    float yOffset = 0f;

                    if (isCrouching && !photonView.IsMine && graphicChanger != null && graphicChanger.typeOfStyle == "WildWest")
                    {
                        // Verifica se se moveu desde o último frame
                        float moveThreshold = 0.001f;
                        wasMoving = Vector3.Distance(transform.position, lastPosition) > moveThreshold;
                        lastPosition = transform.position;

                        if (wasMoving)
                        {
                            yOffset = 0.3f;
                        }
                    }

                    if (isCrouching)
                    {
                        Vector3 bodyPos = bodyCollider.transform.localPosition;
                        bodyPos.y = 0.6f + yOffset;
                        bodyCollider.transform.localPosition = bodyPos;
                    }
                    else
                    {
                        bodyCollider.height = bodyColliderOriginalHeight;

                        Vector3 bodyPos = bodyCollider.transform.localPosition;
                        bodyPos.y = 1f;
                        bodyCollider.transform.localPosition = bodyPos;
                    }
                }
            }
        }



        void FixedUpdate()
        {
            if (photonView.IsMine)
            {
                animator.SetFloat("Horizontal", CrossPlatformInputManager.GetAxis("Horizontal"));
                animator.SetFloat("Vertical", CrossPlatformInputManager.GetAxis("Vertical"));
                if (CrossPlatformInputManager.GetButtonDown("Jump"))
                    animator.SetTrigger("IsJumping");
                animator.SetBool("Running", Input.GetKey(KeyCode.LeftShift));

                isCrouching = Input.GetKey(KeyCode.LeftControl);
                animator.SetBool("IsCrouch", isCrouching);

                gunTargetPos = isCrouching ? gunCrouchLocal : gunStandLocal;
                ikTargetPos = isCrouching ? ikCrouchLocal : ikStandLocal;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(isCrouching);
            }
            else
            {
                object posObj = stream.ReceiveNext();
                object rotObj = stream.ReceiveNext();
                object crouchObj = stream.ReceiveNext();

                if (posObj is Vector3 && rotObj is Quaternion && crouchObj is bool)
                {
                    position = (Vector3)posObj;
                    rotation = (Quaternion)rotObj;
                    isCrouching = (bool)crouchObj;

                    gunTargetPos = isCrouching ? gunCrouchLocal : gunStandLocal;
                    ikTargetPos = isCrouching ? ikCrouchLocal : ikStandLocal;
                }
                else
                {
                    Debug.LogError("Erro de tipo nos dados recebidos via Photon. Verifique versões sincronizadas!");
                }
            }
        }

        void MoveToLayer(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform c in go.transform)
                MoveToLayer(c.gameObject, layer);
        }

        private Transform FindBoneTransform(Transform parent, string boneName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(boneName))
                    return child;
            }
            return null;
        }
}