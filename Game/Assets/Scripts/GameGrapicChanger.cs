using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Globalization;
using System.IO;

public class GameGraphicChanger : MonoBehaviour
{
    public GameObject canvas;
    public GameObject cameraToDeactivate;
    public GameObject cameraToActivate;
    public GameObject objectToActivate1;

    public GameObject objectToActivate5;
    public GameObject rainParticles;     
    public GameObject MainMenu;
    public GameObject SoundOptions;
    public string xmlFileName = "XML_Generator"; 
    public string typeOfStyle = ""; 

    private string typeOfStyleNotToShow = "";
    private AudioSource ambientSource;

    public GameObject AmbientSounds;

    
    private XmlDocument xmlDocument = new();
    public void SetOldSchoolStyle()
    {
        typeOfStyle = "WildWest";
        typeOfStyleNotToShow = "Futurist";
        UpdateUIAndEnvironment();
    }

    public void SetFuturistStyle()
    {
        typeOfStyle = "Futurist";
        typeOfStyleNotToShow = "WildWest";
        UpdateUIAndEnvironment();
    }

    private void UpdateUIAndEnvironment()
    {
        canvas.SetActive(false);
        cameraToActivate.SetActive(true);
        cameraToDeactivate.SetActive(false);
        objectToActivate1.SetActive(true);
        objectToActivate5.SetActive(true);
        MainMenu.SetActive(false);
        SoundOptions.SetActive(true);
        AmbientSounds.SetActive(true);
        LoadXMLAndApplyStyle();

        
        SoundOptions.SetActive(false);
        StartCoroutine(PlayNextAmbientSound(ambientSource));
    }

    private void LoadXMLAndApplyStyle()
    {
        if(typeOfStyle == "Futurist")
        {
            GameObject light = GameObject.FindGameObjectWithTag("ligh");
            light.GetComponent<Light>().intensity = 1f;
        }
        TextAsset xmlFile = Resources.Load<TextAsset>(xmlFileName);
        if (xmlFile == null)
        {
            Debug.LogError($"Ficheiro XML '{xmlFileName}' não encontrado na pasta Resources.");
            return;
        }

        xmlDocument.LoadXml(xmlFile.text);

        // Ativar/desativar partículas de chuva
        XmlNode rainNode = xmlDocument.SelectSingleNode($"/Tese/Rain/{typeOfStyle}");
        if (rainNode != null)
        {
            bool isRainEnabled = bool.Parse(rainNode.InnerText);
            rainParticles.SetActive(isRainEnabled);
        }

        // Aplicar Skybox
        XmlNode skyboxNode = xmlDocument.SelectSingleNode($"/Tese/SkyBox/{typeOfStyle}");
        if (skyboxNode != null)
        {
            string skyboxName = skyboxNode.InnerText;
            Material skyboxMaterial = Resources.Load<Material>(typeOfStyle+"/"+skyboxName);
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                Debug.LogWarning($"Skybox '{skyboxName}' não encontrada nos Resources.");
            }
        }
        XmlNode ambientSoundNode = xmlDocument.SelectSingleNode($"/Tese/AmbientSounds/{typeOfStyle}");
        if (ambientSoundNode != null)
        {
            string ambientSoundName = ambientSoundNode.InnerText;
            AudioClip ambientClip = Resources.Load<AudioClip>(typeOfStyle+"/"+ambientSoundName);

            GameObject ambientSoundObject = GameObject.FindGameObjectWithTag("AmbientSounds");
            if (ambientSoundObject != null)
            {
                ambientSource = ambientSoundObject.GetComponent<AudioSource>();
                if (ambientSource != null && ambientClip != null)
                {
                    ambientSource.clip = ambientClip;
                    ambientSource.loop = false;
                    ambientSource.volume = 0.15f;
                    ambientSource.Play();

                }
                else
                {
                    Debug.LogWarning($"O AudioSource ou AudioClip '{ambientSoundName}' não foi encontrado.");
                }
            }
            else
            {
                Debug.LogWarning("Nenhum objeto com a tag 'AmbientSounds' encontrado na cena.");
            }
        }
        XmlNode BackGroundNode = xmlDocument.SelectSingleNode($"/Tese/BackGround/{typeOfStyle}");
        if (BackGroundNode != null)
        {
            string backgroundName = BackGroundNode.InnerText;


            ActivateBackground(backgroundName);
        }
        else
        {
            Debug.LogWarning("Nenhum objeto com a tag 'BackGround' encontrado na cena.");
        }
        // Aplicar Prefabs e Materiais
        XmlNodeList prefabNodes = xmlDocument.SelectNodes($"/Tese/Prefabs/{typeOfStyle}/*");
        foreach (XmlNode prefabNode in prefabNodes)
        {
            string tag = prefabNode.Name;
            string materialName = prefabNode.SelectSingleNode("Material")?.InnerText;
            string prefabName = prefabNode.SelectSingleNode("pref")?.InnerText;

            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject obj in objectsWithTag)
            {
                // Aplicar material, se existir
                if (!string.IsNullOrEmpty(materialName))
                {
                    Material material = Resources.Load<Material>(typeOfStyle+"/"+materialName);
                    if (material != null)
                    {
                        Renderer renderer = obj.GetComponent<Renderer>() ?? obj.GetComponentInChildren<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material = material;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(prefabName))
                {
                    // Instanciar novo prefab se não houver material
                    GameObject prefab = Resources.Load<GameObject>(typeOfStyle+"/"+prefabName);
                    if (prefab != null)
                    {
                        if(obj.CompareTag("Tree")){
                            float randomYRotation = Random.Range(0f, 180f);
                            obj.transform.rotation = Quaternion.Euler(0, randomYRotation, 0);
                        }
                        GameObject newObject = Instantiate(prefab, obj.transform.position, obj.transform.rotation);
                        newObject.tag = tag;
                        GameObject environment = GameObject.FindGameObjectWithTag("Enviroment");
                        if (environment != null)
                        {
                            newObject.transform.SetParent(environment.transform);
                        }
                        else
                        {
                            Debug.LogWarning("GameObject com tag 'Enviroment' não encontrado na cena.");
                        }
                        Destroy(obj); // Remove o objeto original
                    }
                    else
                    {
                        Debug.LogWarning($"Prefab '{prefabName}' não encontrado nos Resources.");
                    }
                }
            }
        }

        // Atualizar modelo do jogador
        UpdatePlayerModel(Resources.Load<GameObject>("Controller"));
    }

    public void UpdatePlayerModel(GameObject modelPrefab)
    {
        XmlNode characterModelNode = xmlDocument.SelectSingleNode($"/Tese/caracterModel/{typeOfStyle}");
        if (characterModelNode == null)
        {
            Debug.LogWarning("Nenhum modelo encontrado no XML para esta visualização.");
            return;
        }

        string characterAvatarName = characterModelNode.InnerText;
        Avatar characterAvatar = Resources.Load<Avatar>(typeOfStyle+"/"+characterAvatarName);

        GameObject modelController = Resources.Load<GameObject>("Controller");
        if (modelController == null)
        {
            Debug.LogError("O prefab 'Controller' não foi encontrado nos Resources.");
            return;
        }

        Transform model = FindChildWithTag(modelController.transform, "Model");
        if (model == null)
        {
            Debug.LogError("Nenhum objeto com a tag 'Model' foi encontrado dentro do Controller.");
            return;
        }

        Animator modelAnimator = model.GetComponent<Animator>();
        if (modelAnimator == null)
        {
            Debug.LogError("O objeto 'Model' não tem um Animator.");
            return;
        }

        ResetModelState(model);

        Transform particalsParent = modelPrefab.transform.Find("Particals");
        if (particalsParent != null)
        {
            XmlDocument xmlDocument = new XmlDocument();
            TextAsset xmlFile = Resources.Load<TextAsset>("XML_Generator");
            if (xmlFile != null)
            {
                xmlDocument.LoadXml(xmlFile.text);
                XmlNode particalNode = xmlDocument.SelectSingleNode($"/Tese/Particals/{typeOfStyle}");
                XmlNode particalNodeTodeactivate = xmlDocument.SelectSingleNode($"/Tese/Particals/{typeOfStyleNotToShow}");
                if (particalNode != null)
                {
                    string particleName = particalNode.InnerText;
                    string particleNameToDeativate = particalNodeTodeactivate.InnerText;
                    Transform effectToActivate = particalsParent.Find(particleName);
                    Transform effectToDeactivate= particalsParent.Find(particleNameToDeativate);
                    if (effectToActivate != null)
                    {
                        effectToActivate.gameObject.SetActive(true);
                        effectToDeactivate.gameObject.SetActive(false); 
                    }
                    else
                    {
                        Debug.LogWarning($"Efeito '{particleName}' não encontrado dentro de 'Particals'.");
                    }
                }
            }
        }
        if (characterAvatar != null)
        {
            modelAnimator.avatar = characterAvatar;
        }
        else
        {
            Debug.LogWarning($"Avatar '{characterAvatarName}' não encontrado nos Resources.");
        }

        Transform hideElement = FindChildWithTag(model, typeOfStyleNotToShow);
        if (hideElement != null)
        {
            hideElement.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Não foi encontrado um modelo para desativar: '{typeOfStyleNotToShow}'.");
        }
        
        Transform gunTransform = FindChildWithTag(model, "Gun");
        if (gunTransform != null)
        {
            Transform correctGunSkin = FindChildWithTag(gunTransform, typeOfStyle);
            Transform incorrectGunSkin = FindChildWithTag(gunTransform, typeOfStyleNotToShow);

            if (correctGunSkin != null) correctGunSkin.gameObject.SetActive(true);
            if (incorrectGunSkin != null) incorrectGunSkin.gameObject.SetActive(false);
        }

        Transform mainCameraTransform = FindChildWithTag(modelController.transform, "MainCamera");
        if (mainCameraTransform != null)
        {
            Transform gunInCamera = FindChildWithTag(mainCameraTransform, "Gun");
            if (gunInCamera != null)
            {
                Transform correctCameraGunSkin = FindChildWithTag(gunInCamera, typeOfStyle);
                Transform incorrectCameraGunSkin = FindChildWithTag(gunInCamera, typeOfStyleNotToShow);

                if (correctCameraGunSkin != null) correctCameraGunSkin.gameObject.SetActive(true);
                if (incorrectCameraGunSkin != null) incorrectCameraGunSkin.gameObject.SetActive(false);
            }
        }

        XmlNode gunSoundNode = xmlDocument.SelectSingleNode($"/Tese/GunSound/{typeOfStyle}");
        if (gunSoundNode != null)
        {
            string gunSoundName = gunSoundNode.InnerText;
            AudioClip gunSound = Resources.Load<AudioClip>(typeOfStyle+"/"+gunSoundName);
            Transform soundTransform = FindChildWithTag(modelController.transform, "Som");
            if (soundTransform != null)
            {
                AudioSource audioSource = soundTransform.GetComponent<AudioSource>();
                if (audioSource != null && gunSound != null)
                {
                    audioSource.clip = gunSound;
                    audioSource.volume = 0.25f;
                }
            }
        }

        GameObject networkManagerObject = GameObject.FindGameObjectWithTag("NetworkManager");
        if (networkManagerObject != null)
        {
            NetworkManager networkManager = networkManagerObject.GetComponent<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.playerModel = modelController;
            }
            else
            {
                Debug.LogError("NetworkManager não encontrado no objeto.");
            }
        }
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

    private void ResetModelState(Transform model)
    {
        Transform wildWestSkin = FindChildWithTag(model, "WildWest");
        Transform futuristSkin = FindChildWithTag(model, "Futurist");

        if (wildWestSkin != null) wildWestSkin.gameObject.SetActive(true);
        if (futuristSkin != null) futuristSkin.gameObject.SetActive(true);

        Animator modelAnimator = model.GetComponent<Animator>();
        if (modelAnimator != null)
        {
            modelAnimator.avatar = null;
        }

        Transform gunTransform = FindChildWithTag(model, "Gun");
        if (gunTransform != null)
        {
            Transform wildWestGun = FindChildWithTag(gunTransform, "WildWest");
            Transform futuristGun = FindChildWithTag(gunTransform, "Futurist");

            if (wildWestGun != null) wildWestGun.gameObject.SetActive(true);
            if (futuristGun != null) futuristGun.gameObject.SetActive(true);
        }

        // Reset das skins da arma na câmera
        Transform mainCameraTransform = FindChildWithTag(model.parent, "MainCamera");
        if (mainCameraTransform != null)
        {
            Transform gunInCamera = FindChildWithTag(mainCameraTransform, "Gun");
            if (gunInCamera != null)
            {
                Transform wildWestCameraGun = FindChildWithTag(gunInCamera, "WildWest");
                Transform futuristCameraGun = FindChildWithTag(gunInCamera, "Futurist");

                if (wildWestCameraGun != null) wildWestCameraGun.gameObject.SetActive(true);
                if (futuristCameraGun != null) futuristCameraGun.gameObject.SetActive(true);
            }
        }
    } 
    void ActivateBackground(string backgroundName)
    {
        GameObject background = GameObject.FindGameObjectWithTag("background");
        if (background == null)
        {
            Debug.LogError("GameObject 'background' não encontrado na cena.");
            return;
        }

        Transform targetChild = background.transform.Find(backgroundName);
        Transform NottargetChild = background.transform.Find(typeOfStyleNotToShow);
        if (targetChild != null)
        {
            NottargetChild.gameObject.SetActive(false);
            targetChild.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Nenhum filho chamado '" + backgroundName + "' foi encontrado dentro de 'background'.");
        }
    }
    private IEnumerator PlayNextAmbientSound(AudioSource ambientSource)
    {
        while (ambientSource.isPlaying)
        {
            yield return null;
        }

        XmlNode ambientSound2Node = xmlDocument.SelectSingleNode($"/Tese/AmbientSounds2/{typeOfStyle}");
        if (ambientSound2Node != null)
        {
            string ambientSound2Name = ambientSound2Node.InnerText;
            AudioClip ambientClip2 = Resources.Load<AudioClip>(typeOfStyle+"/"+ambientSound2Name);

            if (ambientClip2 != null)
            {
                ambientSource.clip = ambientClip2;
                ambientSource.loop = true;
                if (typeOfStyle == "Futurist")
                {
                    ambientSource.volume = 0.3f;
                }
                else
                {
                    ambientSource.volume = 1f;
                }
                
                ambientSource.Play();
            }
            else
            {
                Debug.LogWarning($"Segundo AudioClip '{ambientSound2Name}' não encontrado nos Resources.");
            }
        }
        else
        {
            Debug.LogWarning("Nó 'AmbientSounds2' não encontrado no XML.");
        }
    }

}
