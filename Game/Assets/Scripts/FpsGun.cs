using Photon.Pun;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using System.Collections;
using UnityEngine.UI;

public class FpsGun : MonoBehaviourPunCallbacks, IPunObservable {

    [SerializeField]
    private int damagePerShot = 20;
    private int damageHeadShot = 50;

    [SerializeField]
    private float timeBetweenBullets = 0.2f;
    [SerializeField]
    private float weaponRange = 350.0f;
    [SerializeField]
    public TpsGun tpsGun;
    [SerializeField]
    private ParticleSystem gunParticles;
    [SerializeField]
    private LineRenderer gunLine;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Camera raycastCamera;

    private float timer;

    private Transform Hitscan;
    private Transform HitscanHead;

    public NetworkManager net;
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start() {
        net = FindObjectOfType<NetworkManager>();
        timer = 0.0f;
        Hitscan = GameObject.FindGameObjectWithTag("Screen").transform.Find("HitScan");
        HitscanHead = GameObject.FindGameObjectWithTag("Screen").transform.Find("HitscanHead");

    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update() {
        timer += Time.deltaTime;
        
        if (!photonView.IsMine) return; 

        PlayerHealth playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && playerHealth.isDead) {
            animator.SetBool("Firing", false); 
            return;
        }
        
        bool shooting = Input.GetMouseButton(0);
        if (shooting && timer >= timeBetweenBullets && Time.timeScale != 0) {
            Shoot();
        }
        animator.SetBool("Firing", shooting);
    }

    /// <summary>
    /// Shoot once, this also calls RPCShoot for third person view gun.
    /// <summary>
    void Shoot() {
        timer = 0.0f;
        gunLine.enabled = true;
        StartCoroutine(DisableShootingEffect());
        if (gunParticles.isPlaying) {
            gunParticles.Stop();
        }
        gunParticles.Play();
        // Ray casting for shooting hit detection.
        RaycastHit shootHit;
        Ray shootRay = raycastCamera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0f));
        if (Physics.Raycast(shootRay, out shootHit, weaponRange, LayerMask.GetMask("Shootable"))) {
            string hitTag = shootHit.collider.gameObject.tag;

            switch (hitTag) {
                case "PlayerBody":        
                    shootHit.collider.GetComponentInParent<PhotonView>().RPC("TakeDamage", RpcTarget.All, damagePerShot, PhotonNetwork.LocalPlayer.NickName);
                    StartCoroutine(FlashHitScan());               
                    PhotonNetwork.Instantiate("impactFlesh", shootHit.point, Quaternion.Euler(shootHit.normal.x - 90, shootHit.normal.y, shootHit.normal.z), 0);
                    break;
                case "PlayerHead":
                    shootHit.collider.GetComponentInParent<PhotonView>().RPC("TakeDamage", RpcTarget.All, damageHeadShot, PhotonNetwork.LocalPlayer.NickName);
                    StartCoroutine(FlashHitScanHead());               
                    PhotonNetwork.Instantiate("impactFlesh", shootHit.point, Quaternion.Euler(shootHit.normal.x - 90, shootHit.normal.y, shootHit.normal.z), 0);
                    break;
                default:
                    PhotonNetwork.Instantiate("impact" + hitTag, shootHit.point, Quaternion.Euler(shootHit.normal.x - 90, shootHit.normal.y, shootHit.normal.z), 0);
                    break;
            }
        }
        tpsGun.RPCShoot();  
    }


    /// <summary>
    /// Coroutine function to disable shooting effect.
    /// <summary>
    public IEnumerator DisableShootingEffect() {
        yield return new WaitForSeconds(0.05f);
        gunLine.enabled = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator FlashHitScan()
    {
        Hitscan.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        Hitscan.gameObject.SetActive(false);
    }
    IEnumerator FlashHitScanHead()
    {
        HitscanHead.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        HitscanHead.gameObject.SetActive(false);
    }
}
