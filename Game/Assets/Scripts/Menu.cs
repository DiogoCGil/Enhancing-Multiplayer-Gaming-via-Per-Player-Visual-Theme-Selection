using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;
using TMPro;

public class Menu : MonoBehaviour
{
    public GameObject MenuPause;
    public GameObject ButtonsStart;
    public GameObject ButtonsOptions;
    private Transform CamaraPlayer;
    private GameObject player;
    public GameObject Camara;
    public GameObject MainObjetive;
    private FirstPersonControllerCostum x;
    private StaminaController y;
    void Start()
    {
        if (MainObjetive != null)
        {
            MainObjetive.SetActive(true);
            StartCoroutine(DisableMainObjectiveAfterDelay(5f));
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        bool isActive = !MenuPause.activeSelf;
        MenuPause.SetActive(isActive);
        ButtonsStart.SetActive(isActive);
        ButtonsOptions.SetActive(false);
        Camara.SetActive(isActive);

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject localPlayer = null;

        foreach (GameObject p in players)
        {
            if (p.GetComponent<PhotonView>()?.IsMine == true)
            {
                localPlayer = p;
                break;
            }
        }

        if (localPlayer != null)
        {
            player = localPlayer;
            CamaraPlayer = FindChildWithTag(player.transform, "MainCamera");
            x = player.GetComponent<FirstPersonControllerCostum>();
            y = player.GetComponent<StaminaController>();

            if (CamaraPlayer != null)
                CamaraPlayer.gameObject.SetActive(!isActive);
            if (x != null)
                x.enabled = !isActive;
            if (y != null)
                y.enabled = !isActive;

            UpdateCursorState(isActive);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnClickResume()
    {
        MenuPause.SetActive(false);

        if (player != null)
        {
            Camara.SetActive(false);

            if (CamaraPlayer != null)
                CamaraPlayer.gameObject.SetActive(true);
            if (x != null)
                x.enabled = true;
            if (y != null)
                y.enabled = true;

            UpdateCursorState(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickOptions()
    {
        ButtonsStart.SetActive(false);
        ButtonsOptions.SetActive(true);
        UpdateCursorState(true);
    }

    public void OnClickGoBack()
    {
        ButtonsStart.SetActive(true);
        ButtonsOptions.SetActive(false);
        UpdateCursorState(true);
    }

    private void UpdateCursorState(bool isMenuOpen)
    {
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isMenuOpen;
    }

    private IEnumerator DisableMainObjectiveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (MainObjetive != null)
        {
            MainObjetive.SetActive(false);            
        }
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
        return null;
    }

    public void OnPlayerRespawn()
    {
        FindPlayerController();
    }
    private void FindPlayerController()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player.GetComponent<PhotonView>().IsMine) 
            {
                CamaraPlayer = FindChildWithTag(player.transform, "MainCamera");
                x = player.GetComponent<FirstPersonControllerCostum>();
                y = player.GetComponent<StaminaController>();
                if (x != null && y != null && MenuPause.activeSelf)
                {
                    
                    x.enabled = false;
                    y.enabled = false;
                    CamaraPlayer.gameObject.SetActive(false);
                }
                break;
            }
        }
    }
   
} 
