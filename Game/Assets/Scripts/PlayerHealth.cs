using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(FirstPersonControllerCostum))]
[RequireComponent(typeof(Rigidbody))]

public class PlayerHealth : MonoBehaviourPunCallbacks, IPunObservable {

    public delegate void Respawn(float time);
    public delegate void AddMessage(string Message);
    public event Respawn RespawnEvent;
    public event AddMessage AddMessageEvent;

    [SerializeField]
    private int startingHealth = 100;
    [SerializeField]
    private float sinkSpeed = 0.12f;
    [SerializeField]
    private float sinkTime = 2.5f;
    [SerializeField]
    private float respawnTime = 8.0f;
    [SerializeField]
    private AudioClip deathClip;
    [SerializeField]
    private AudioClip hurtClip;
    [SerializeField]
    private AudioSource playerAudio;
    [SerializeField]
    private float flashSpeed = 2f;
    [SerializeField]
    private Color flashColour = new Color(1f, 0f, 0f, 0.1f);
    [SerializeField]
    private NameTag nameTag;
    [SerializeField]
    public Animator animator;

    private FirstPersonControllerCostum fpController;
    private IKControl ikControl;
    private Slider healthSliderUi;
    public Slider healthSlider;
    private Slider ShieldSliderUI;
    public Slider ShieldSlider;
    private Image damageImage;
    public int currentHealth;
    public bool isDead;
    private bool isSinking;
    private bool damaged;

    [HideInInspector]public takenDamage effectDamage;

    private NetworkManager net;
    private float outOfCombatTimer = 0f;
    private bool canRegen = true;
    [SerializeField] private float combatCooldown = 5f;
    [SerializeField] private float regenAmount = 25f;
    [SerializeField] private float regenInterval = 0.5f;
    [SerializeField] private int maxHealth = 100;
    private Coroutine regenCoroutine;

    [HideInInspector]public StaminaController _staminaController;

    public float killRatio;
    private int myKills; 
    private int totalDeaths;
    private int shieldAmount = 0;

    public Text shieldNumber;  
    public GameObject shieldImage;

    public GameObject Head;
    public GameObject Body;
    public float spawnTimestamp = 0f;
    public float totalTimeAlive = 0f;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start() {
        net = FindObjectOfType<NetworkManager>();
        effectDamage = GetComponentInChildren<takenDamage>(true);
        _staminaController = GetComponent<StaminaController>();
        fpController = GetComponent<FirstPersonControllerCostum>();
        ikControl = GetComponentInChildren<IKControl>();
        damageImage = GameObject.FindGameObjectWithTag("Screen").transform.Find("DamageImage").GetComponent<Image>();
        healthSliderUi = GameObject.FindGameObjectWithTag("Screen").GetComponentInChildren<Slider>();
        ShieldSliderUI = GameObject.FindGameObjectWithTag("Screen").transform.Find("ShieldSliderUI").GetComponent<Slider>();
        spawnTimestamp = Time.time;
        currentHealth = startingHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = startingHealth;
            healthSlider.value = currentHealth;
        }
        if (photonView.IsMine) {
            gameObject.layer = LayerMask.NameToLayer("FPSPlayer");
            healthSliderUi.value = currentHealth;

            if (Head != null)
                Head.layer = LayerMask.NameToLayer("Ignore Raycast");

            if (Body != null)
                Body.layer = LayerMask.NameToLayer("Ignore Raycast");

            damaged = false;
            isDead = false;
            isSinking = false;

            myKills = net.CurrentKills;
            totalDeaths = net.totalKillCount;

            if (totalDeaths >= 5) {
                killRatio = myKills / (float)(totalDeaths + 0.0001f);

                if (killRatio < 0.2f) {
                    shieldAmount = 50;
                    if (ShieldSliderUI != null)
                    {
                        ShieldSliderUI.value = shieldAmount;
                        ShieldSliderUI.gameObject.SetActive(true);
                    }
                    if(ShieldSlider != null){
                        ShieldSlider.value = shieldAmount;
                        ShieldSlider.gameObject.SetActive(true);
                    }
                    shieldImage.gameObject.SetActive(true);

                 
                }
                else
                {
                    if (ShieldSliderUI != null){
                        ShieldSliderUI.gameObject.SetActive(false);
                        ShieldSlider.gameObject.SetActive(false);
                        shieldImage.gameObject.SetActive(false);

                    }    
                }
            }
            else{
                    if (ShieldSliderUI != null){
                    
                        ShieldSliderUI.gameObject.SetActive(false);
                    }
                    if(ShieldSlider!=null){
                        ShieldSlider.gameObject.SetActive(false);
                    }
                    shieldImage.gameObject.SetActive(false);

            }  
        }
        
    }
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update() {
        if (damaged) {
            damaged = false;
            damageImage.color = flashColour;
        } else {
            damageImage.color = Color.Lerp(damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }
        if (isSinking) {
            transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime);
        }
        if (photonView.IsMine && !isDead && !canRegen)
        {
            outOfCombatTimer += Time.deltaTime;
            if (outOfCombatTimer >= combatCooldown)
            {
                canRegen = true;
                StartRegen();
            }
        }
    }

    /// <summary>
    /// RPC function to let the player take damage.
    /// </summary>
    /// <param name="amount">Amount of damage dealt.</param>
    /// <param name="enemyName">Enemy's name who cause this player's death.</param>
    [PunRPC]
    public void TakeDamage(int amount, string enemyName) {
        if (isDead) return;
        if (photonView.IsMine) {
            damaged = true;
            int remainingDamage = amount;

            if (shieldAmount > 0)
            {

                int absorbed = Mathf.Min(shieldAmount, amount);
                shieldAmount -= absorbed;
                if (ShieldSliderUI != null && ShieldSlider !=null)
                {
                    ShieldSliderUI.value = shieldAmount;
                    ShieldSlider.value = shieldAmount;
                    if (shieldAmount <= 0)
                    {
                        ShieldSliderUI.gameObject.SetActive(false);
                        ShieldSlider.gameObject.SetActive(false);
                        shieldImage.gameObject.SetActive(false);
                    }
                }
                remainingDamage -= absorbed;
            }

            currentHealth -= remainingDamage;

            if (currentHealth <= 0) {
                photonView.RPC("Death", RpcTarget.All, enemyName);

            }

            healthSliderUi.value = currentHealth;
            if (healthSlider != null)
                healthSlider.value = currentHealth;

            if (remainingDamage > 0) {
                effectDamage.AdjustEffect(remainingDamage);
                animator.SetTrigger("IsHurt");
                effectDamage.TakeDamageEffect();
            }

            StopRegen(); 
            canRegen = false;
            outOfCombatTimer = 0f;
        }
        playerAudio.clip = hurtClip;
        playerAudio.Play();
        
    }

    /// <summary>
    /// RPC function to declare death of player.
    /// </summary>
    /// <param name="enemyName">Enemy's name who cause this player's death.</param>
    [PunRPC]
    void Death(string enemyName) {
        isDead = true;
        ikControl.enabled = false;
        nameTag.gameObject.SetActive(false);

        if (photonView.IsMine) {
            float now = Time.time;
            totalTimeAlive += now - spawnTimestamp;
            if (net.totalTimeAliveMain < totalTimeAlive)
            {
                net.totalTimeAliveMain = totalTimeAlive;
            }
            fpController.enabled = false;
            FirstPersonControllerCostum fpc = GetComponent<FirstPersonControllerCostum>();
            PlayerNetworkMover playerMover = GetComponent<PlayerNetworkMover>();
            StaminaController staminaController = GetComponent<StaminaController>();
            if (fpc != null)
            {
                fpc.enabled = false;
            }
            if(staminaController != null){
                staminaController.enabled = false;
            }
            if(playerMover !=null)
            {
                playerMover.enabled = false;
            }
           Transform modelTransform = FindChildWithTag(transform, "Model");
            if (modelTransform != null) {
                Transform gunTransform = FindChildWithTag(modelTransform, "Gun");
                if (gunTransform != null) {
                    if (gunTransform.TryGetComponent<TpsGun>(out var tpsGun))
                    {
                        tpsGun.enabled = false; 
                    }
                    StartCoroutine(DropGunSmoothly(gunTransform));
                } else {
                    Debug.LogWarning("Não foi possível encontrar a arma com a tag 'Gun' dentro do Model.");
                }
            } else {
                Debug.LogWarning("Não foi possível encontrar o Model.");
            }
            Transform camara = FindChildWithTag(transform,"MainCamera");
            if(camara != null)
            {
                camara.GetComponent<AudioListener>().enabled = false;
            }
            effectDamage.RemoveEffect();
            animator.SetTrigger("IsDead");
            AddMessageEvent(PhotonNetwork.LocalPlayer.NickName + " was killed by " + enemyName + "!");
            RespawnEvent(respawnTime);
           
            StartCoroutine("DestoryPlayer", respawnTime);

            playerAudio.clip = deathClip;
            playerAudio.Play();
            StartCoroutine("StartSinking", sinkTime);
        }
       
        
        
        
    }

    /// <summary>
    /// Coroutine function to destory player game object.
    /// </summary>
    /// <param name="delayTime">Delay time before destory.</param>
    IEnumerator DestoryPlayer(float delayTime) {
        yield return new WaitForSeconds(delayTime);
        PhotonNetwork.Destroy(gameObject);
    }

    /// <summary>
    /// RPC function to start sinking the player game object.
    /// </summary>
    /// <param name="delayTime">Delay time before start sinking.</param>
    IEnumerator StartSinking(float delayTime) {
        yield return new WaitForSeconds(delayTime);
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = false;
        isSinking = true;
    }

    /// <summary>
    /// Used to customize synchronization of variables in a script watched by a photon network view.
    /// </summary>
    /// <param name="stream">The network bit stream.</param>
    /// <param name="info">The network message information.</param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(currentHealth);
            stream.SendNext(shieldAmount);
        } else {
            currentHealth = (int)stream.ReceiveNext();
            shieldAmount = (int)stream.ReceiveNext();

            if (healthSlider != null)
                healthSlider.value = currentHealth;

            if (ShieldSlider != null) {
                ShieldSlider.value = shieldAmount;
                ShieldSlider.gameObject.SetActive(shieldAmount > 0);
                shieldImage.gameObject.SetActive(shieldAmount > 0);
            }
        }
    }
    private IEnumerator DropGunSmoothly(Transform tpsGunTransform)
    {
        float duration = 0.25f;
        float elapsedTime = 0f;

        Vector3 startPosition = tpsGunTransform.position;

        Vector3 targetPosition = startPosition + Vector3.down * 2f;

        Quaternion startRotation = tpsGunTransform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(
            Random.Range(-20f, 20f),
            Random.Range(-60f, 60f),
            Random.Range(-20f, 20f)
        );

        while (elapsedTime < duration)
        {
            tpsGunTransform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            tpsGunTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime * 2f;
            yield return null;
        }

        tpsGunTransform.position = targetPosition;
        tpsGunTransform.rotation = targetRotation;
    }
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
        }
        Debug.LogWarning($"Nenhum objeto com tag '{tag}' foi encontrado.");
        return null;
    }
    private void StartRegen()
    {
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        regenCoroutine = StartCoroutine(RegenHealth());
    }

    private void StopRegen()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
    }
    private IEnumerator RegenHealth()
    {
        while (canRegen && currentHealth < maxHealth)
        {
            currentHealth += (int)regenAmount;
            
            if (currentHealth > maxHealth){
                currentHealth = maxHealth;
                effectDamage.RemoveEffect();
            }
                

            healthSliderUi.value = currentHealth;
            
            if (healthSlider != null)
                healthSlider.value = currentHealth;
            
            if (effectDamage != null)
            {
                effectDamage.HealDamage();
            }

            yield return new WaitForSeconds(regenInterval);
        }

        regenCoroutine = null; 
    }

    
}
