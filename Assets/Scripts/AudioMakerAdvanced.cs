using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Audio;
using System;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class AudioMakerAdvanced : MonoBehaviour
{
    [System.Serializable]
    public class Pattern
    {
        public bool[] steps = new bool[16];
        public Pattern(bool[] steps = null) => this.steps = steps ?? new bool[16];
        public bool ShouldPlay(int step) => step >= 0 && step < steps.Length && steps[step];
    }

    [Header("Tempo Settings")]
    [Range(40, 240)] public float tempo = 120f;
    [Range(1, 32)] public int totalBars = 16;
    [Range(1, 8)] public int sectionLength = 4;

    [Header("Drum Samples")]
    public AudioClip kickSample;
    public AudioClip snareSample;
    public AudioClip hiHatSample;
    public AudioClip percussionSample;


    [Header("Volumes")]
    [Range(0f, 1f)] public float kickVolume = 1f;
    [Range(0f, 1f)] public float snareVolume = 0.8f;
    [Range(0f, 1f)] public float hiHatVolume = 0.6f;
    [Range(0f, 1f)] public float percussionVolume = 0.5f;

    [Header("Block Settings")]
    public GameObject kickBlock;
    public GameObject snareBlock;
    public GameObject hiHatBlock;
    public GameObject percBlock;
    public float minRiseSpeed = 1f;
    public float maxRiseSpeed = 5f;
    public float xSpacingFactor = 2f;
    public float kickY = 0f;
    public float snareY = 5f;
    public float hiHatY = 10f;
    public float percY = 15f;

    private AudioSource audioSource;
    private AudioSource kickSource;
    private AudioSource snareSource;
    private AudioSource hatSource;
    private AudioSource percSource;

    public AudioMixerGroup kickmixer;
    public AudioMixerGroup snaremixer;
    public AudioMixerGroup hatmixer;
    public AudioMixerGroup percmixer;

    private float sampleRate;
    private Dictionary<AudioClip, float[]> sampleCache = new Dictionary<AudioClip, float[]>();
    private List<float> kickTimes = new List<float>();
    private List<float> snareTimes = new List<float>();
    private List<float> hiHatTimes = new List<float>();
    private List<float> percTimes = new List<float>();
    private List<GameObject> kickBlocks = new List<GameObject>();
    private List<GameObject> snareBlocks = new List<GameObject>();
    private List<GameObject> hiHatBlocks = new List<GameObject>();
    private List<GameObject> percBlocks = new List<GameObject>();
    private float elapsedTime = 0f;
    private bool hasStarted = false;
    private Vector3 caminital;
    private Vector3 camlast;

    public Transform camera;
    public float camspeed;
    private int intros;
    private List<Pattern> kickPatterns = new List<Pattern>
    {
        new Pattern(new bool[16] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false }),
        new Pattern(new bool[16] { true, false, false, false, false, false, false, false, true, false, false, false, true, false, false, false }),
        new Pattern(new bool[16] { true, false, false, false, false, false, true, false, true, false, false, false, true, false, false, false }),
        new Pattern(new bool[16] { true, false, false, false, true, false, false, false, false, false, true, false, false, true, false, false })
    };

    private List<Pattern> hiHatPatterns = new List<Pattern>
    {
        new Pattern(new bool[16] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true }),
        new Pattern(new bool[16] { true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false }),
        new Pattern(new bool[16] { false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true }),
        new Pattern(new bool[16] { true, false, false, false, true, false, true, false, false, false, true, false, true, false, false, false })
    };

    private List<Pattern> snarePatterns = new List<Pattern>
    {
        new Pattern(new bool[16] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false }),
        new Pattern(new bool[16] { false, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false }),
        new Pattern(new bool[16] { false, false, false, true, true, false, false, true, false, false, false, true, false, false, false, false }),
        new Pattern(new bool[16] { false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false })
    };

    private List<Pattern> percPatterns = new List<Pattern>
    {
        new Pattern(new bool[16] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false }),
        new Pattern(new bool[16] { false, true, false, true, true, false, true, false, false, true, true, false, true, false, true, true }),
        new Pattern(new bool[16] { false, false, false, true, false, false, true, false, false, false, false, true, false, true, false, false }),
        new Pattern(new bool[16] { true, false, false, true, false, false, true, false, false, true, false, false, true, false, false, false })
    };



    public Text Message;

    public Slider slider;
    private int ttsamples;
    private bool isDraggin;
    private float trackLength; // Total length in world units
    private float trackStartX; // Starting X position
    private AudioClip clip;
    private AudioClip kickclip, snareclip, hatclip, percclip;
    private float[] kickdata, snaredata, hatdata, percdata;
    public Text Name;


    private bool wasPlaying = false;
    private bool kicksolo = false;
    private bool snaresolo = false;
    private bool hatsolo = false;
    private bool percsolo = false;


    public Pattern[] kickSections;
    public Pattern[] snareSections;
    public Pattern[] hatSections;
    public Pattern[] percSections;

    public Image[] Sections = new Image[8];
    public Toggle[] kicktog = new Toggle[16];
    public Toggle[] hattog = new Toggle[16];
    public Toggle[] snaretog = new Toggle[16];
    public Toggle[] perctog = new Toggle[16];

    public int UpdateIndex = 0;
    private bool kickplaying = false, snareplaying = false, percplaying = false, hatplaying = false;

    public Button kickbrowse, snarebrowse, hatbrowse, percbrowse;
    public Image BrowserMenu;
    public Text titlebrowse;


    [SerializeField] private string kicksFolderPath = "Scenes/Groover/AudioSamples/MinistryOfTechno/Kicks";
    [SerializeField] private string snaresFolderPath = "Scenes/Groover/AudioSamples/MinisitryOfTechno/Snares";
    [SerializeField] private string percussionsFolderPath = "Scenes/Groover/AudioSamples/MinisitryOfTechno/Percussions";
    [SerializeField] private string hatsFolderPath = "Scenes/Groover/AudioSamples/MinisitryOfTechno/Hats";
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private float verticalSpacing = 10f;

    private string currentLoadedFolder;
    public AudioSource previewAudioSource; // Assign in inspector

    public Slider tempoSlider;
    public Text tempovalue;

    private void ClearExistingButtons()
    {
        foreach (Transform child in contentParent)
        {
            if (child.gameObject != buttonPrefab)
                Destroy(child.gameObject);
        }
    }


private void GenerateButtonsForFolder(string folderPath)
{
    ClearExistingButtons();

    // Configure layout
    var contentRect = contentParent.GetComponent<RectTransform>();
    var layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
    if (layoutGroup == null) layoutGroup = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
    
    layoutGroup.padding = new RectOffset(-60, 0, 10, 10);
    layoutGroup.spacing = verticalSpacing;
    layoutGroup.childAlignment = TextAnchor.UpperCenter;
    layoutGroup.childControlHeight = false;
    layoutGroup.childControlWidth = false; 

    // Add Content Size Fitter
    var sizeFitter = contentParent.GetComponent<ContentSizeFitter>();
    if (sizeFitter == null) sizeFitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    // Get audio files
    string fullPath = Path.Combine(Application.dataPath, folderPath).Replace("\\", "/");
    
    if (!Directory.Exists(fullPath))
    {
        Debug.LogError($"Directory not found: {fullPath}");
        return;
    }

    string[] validExtensions = { ".mp3", ".wav" };
    string[] audioFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.TopDirectoryOnly)
        .Where(f => validExtensions.Contains(Path.GetExtension(f).ToLower()))
        .ToArray();

    if (audioFiles.Length == 0)
    {
        Debug.LogWarning("No audio files found at: " + fullPath);
        return;
    }

    // Calculate total content height needed
    float buttonHeight = buttonPrefab.GetComponent<RectTransform>().rect.height;
    float totalContentHeight = (buttonHeight + verticalSpacing) * audioFiles.Length;
    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalContentHeight);

    foreach (string filePath in audioFiles)
    {
       string fileName = Path.GetFileNameWithoutExtension(filePath);
        GameObject newButton = Instantiate(buttonPrefab, contentParent);
        newButton.SetActive(true);

        // Set sample name text
        Text textComponent = newButton.GetComponentInChildren<Text>(true);
        if (textComponent != null) textComponent.text = fileName;

        // Get the child preview button (must match your prefab structure)
        Button previewButton = newButton.transform.Find("PlayButton").GetComponent<Button>();
        
        // Set up PREVIEW functionality (child button)
        if (previewButton != null)
        {
            previewButton.onClick.AddListener(() => 
            {
                StartCoroutine(PreviewAudioClip(filePath));
            });
        }

        // Set up SELECTION functionality (main button)
        Button mainButton = newButton.GetComponent<Button>();
        if (mainButton != null)
        {
            mainButton.onClick.AddListener(() => 
            {
                // Determine sample type and load
                SampleType type = GetSampleTypeFromPath(folderPath);
                StartCoroutine(LoadAudioClip(filePath, type));
                BrowserMenu.gameObject.SetActive(false);
            });
        }

        // Disable raycast on main button image to prevent blocking child button
        Image mainButtonImage = newButton.GetComponent<Image>();
        if (mainButtonImage != null) mainButtonImage.raycastTarget = false;
    }

    // Force double layout update (sometimes needed for proper sizing)
    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    
    // Reset scroll position
    contentRect.anchoredPosition = Vector2.zero;
}

private SampleType GetSampleTypeFromPath(string path)
{
    if (path == kicksFolderPath) return SampleType.Kick;
    if (path == snaresFolderPath) return SampleType.Snare;
    if (path == hatsFolderPath) return SampleType.Hat;
    return SampleType.Percussion;
}

private IEnumerator PreviewAudioClip(string filePath)
{
    if (previewAudioSource == null)
    {
        Debug.LogError("Preview AudioSource not assigned!");
        yield break;
    }

    string url = "file://" + filePath.Replace("\\", "/");
    AudioType audioType = filePath.EndsWith(".mp3") ? AudioType.MPEG : AudioType.WAV;

    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Preview failed: " + www.error);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
        previewAudioSource.Stop();
        previewAudioSource.clip = clip;
        previewAudioSource.Play();
    }
}

private enum SampleType { Kick, Snare, Hat, Percussion }

private IEnumerator LoadAudioClip(string filePath, SampleType sampleType)
{
    string url = "file://" + filePath.Replace("\\", "/");
    AudioType audioType = filePath.EndsWith(".mp3") ? AudioType.MPEG : AudioType.WAV;

    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load audio: " + www.error);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
        
        // Assign to the appropriate sample variable based on type
        switch (sampleType)
        {
            case SampleType.Kick:
                kickSample = clip;
                break;
            case SampleType.Snare:
                snareSample = clip;
                break;
            case SampleType.Hat:
                hiHatSample = clip;
                break;
            case SampleType.Percussion:
                percussionSample = clip;
                break;
        }
    }
}
    public void KickBrowser()
    {
        bool ActiveMenu = BrowserMenu.gameObject.activeSelf;
        bool TitleRight = titlebrowse.text == "Kick Samples";
        
        if (!ActiveMenu || !TitleRight || currentLoadedFolder != kicksFolderPath)
        {
            BrowserMenu.gameObject.SetActive(true);
            titlebrowse.text = "Kick Samples";
            GenerateButtonsForFolder(kicksFolderPath);
            currentLoadedFolder = kicksFolderPath;
        }
        else
        {
            BrowserMenu.gameObject.SetActive(false);
        }
    }

    public void SnareBrowser()
    {
        bool ActiveMenu = BrowserMenu.gameObject.activeSelf;
        bool TitleRight = titlebrowse.text == "Snare Samples";
        
        if (!ActiveMenu || !TitleRight || currentLoadedFolder != snaresFolderPath)
        {
            BrowserMenu.gameObject.SetActive(true);
            titlebrowse.text = "Snare Samples";
            GenerateButtonsForFolder(snaresFolderPath);
            currentLoadedFolder = snaresFolderPath;
        }
        else
        {
            BrowserMenu.gameObject.SetActive(false);
        }
    }

    public void HatBrowser()
    {
        bool ActiveMenu = BrowserMenu.gameObject.activeSelf;
        bool TitleRight = titlebrowse.text == "Hat Samples";
        
        if (!ActiveMenu || !TitleRight || currentLoadedFolder != hatsFolderPath)
        {
            BrowserMenu.gameObject.SetActive(true);
            titlebrowse.text = "Hat Samples";
            GenerateButtonsForFolder(hatsFolderPath);
            currentLoadedFolder = hatsFolderPath;
        }
        else
        {
            BrowserMenu.gameObject.SetActive(false);
        }
    }

    public void PercBrowser()
    {
        bool ActiveMenu = BrowserMenu.gameObject.activeSelf;
        bool TitleRight = titlebrowse.text == "Perc Samples";
        
        if (!ActiveMenu || !TitleRight || currentLoadedFolder != percussionsFolderPath)
        {
            BrowserMenu.gameObject.SetActive(true);
            titlebrowse.text = "Perc Samples";
            GenerateButtonsForFolder(percussionsFolderPath);
            currentLoadedFolder = percussionsFolderPath;
        }
        else
        {
            BrowserMenu.gameObject.SetActive(false);
        }
    }




    //



    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        kickSource = this.gameObject.AddComponent<AudioSource>();
        kickSource.outputAudioMixerGroup = kickmixer;
        snareSource = this.gameObject.AddComponent<AudioSource>();
        snareSource.outputAudioMixerGroup = snaremixer;
        hatSource = this.gameObject.AddComponent<AudioSource>();
        hatSource.outputAudioMixerGroup = hatmixer;
        percSource = this.gameObject.AddComponent<AudioSource>();
        percSource.outputAudioMixerGroup = percmixer;
        sampleRate = AudioSettings.outputSampleRate;
        GenerateAndPlay();
    }

    void Start()
    {
        slider.minValue = 0;
        slider.maxValue = ttsamples;
        caminital = camera.position;
        SpawnAllBlocks();

    }


    //secstart
    public void SoloKick()
    {
        if (kicksolo == false)
        {
        kickSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
        audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
        kickSource.Play();
        kicksolo = true;
        }
        else if (kicksolo == true)
        {
            kickSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
            if (!hatsolo && !snaresolo && !percsolo)
            {
                audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
            }
            kickSource.Pause();
            kicksolo = false;

        }
    }
    public void SoloHat()
    {
        if (hatsolo == false)
        {
        hatSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
        audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
        hatSource.Play();
        hatsolo = true;
        }
        else if (hatsolo == true)
        {
            hatSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
            if (!snaresolo && !kicksolo && !percsolo)
            {
            audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
            }
            hatSource.Pause();
            hatsolo = false;
        }
    }
    public void SoloSnare()
    {
        if (snaresolo == false)
        {
        snareSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
        audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
        snareSource.Play();
        snaresolo = true;
        }
        else if (snaresolo == true)
        {
            snareSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
            if (!hatsolo && !kicksolo && !percsolo)
            {
                audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);

            }
            snareSource.Pause();
            snaresolo = false;
        }
    }
    public void SoloPerc()
    {
        if (percsolo == false)
        {
        percSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
        audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
        percSource.Play();
        percsolo = true;
        }
        else if (percsolo == true)
        {
            percSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", -80f);
            if (!hatsolo && !kicksolo && !snaresolo)
            {
            audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Master", 0f);
            }
            percSource.Pause();
            percsolo = false;
        }
    }



    //secend
    void Update()
    {
        tempo = tempoSlider.value;
        tempovalue.text = tempo.ToString();
        if (!kicksolo)
            kickSource.timeSamples = audioSource.timeSamples;
        if (!hatsolo)
            hatSource.timeSamples = audioSource.timeSamples;
        if (!snaresolo)
            snareSource.timeSamples = audioSource.timeSamples;
        if (!percsolo)
            percSource.timeSamples = audioSource.timeSamples;

        if (!hasStarted && audioSource.isPlaying)
        {
            hasStarted = true;
            elapsedTime = 0f;
        }

        if (!hasStarted) return;

        if (!isDraggin && audioSource.isPlaying)
        {
        float playbackProgress = (audioSource.timeSamples - intros) / (float)(ttsamples);
        float targetX = trackStartX + (trackLength * playbackProgress);

        }

        elapsedTime += Time.deltaTime;

        if (audioSource.timeSamples >= intros && audioSource.isPlaying)
        {
        UpdateInstrumentBlocks(kickBlocks, kickTimes, kickY);
        UpdateInstrumentBlocks(snareBlocks, snareTimes, snareY);
        UpdateInstrumentBlocks(hiHatBlocks, hiHatTimes, hiHatY);
        UpdateInstrumentBlocks(percBlocks, percTimes, percY);
        slider.value = audioSource.timeSamples - intros;
        camspeed = xSpacingFactor; // units per second
        camera.position += new Vector3(camspeed * Time.deltaTime, 0f, 0f);
        }
    }
    

 private void UpdateInstrumentBlocks(List<GameObject> blocks, List<float> times, float yPos)
{
    float audioTime = audioSource.timeSamples / (float)sampleRate;

    for (int i = 0; i < times.Count; i++)
    {
        if (audioTime >= times[i] && blocks[i].transform.position.y < yPos)
        {
            // Use global reference (e.g., start at X = -10)
            float xPos = -10f - ((intros /sampleRate)* xSpacingFactor) + times[i] * xSpacingFactor;

            float riseSpeed = minRiseSpeed;
            if (i < times.Count - 1)
            {
                float nextTimeDiff = times[i + 1] - times[i];
                riseSpeed = Mathf.Lerp(maxRiseSpeed, minRiseSpeed, nextTimeDiff / 0.5f);
            }

            Vector3 startPos = new Vector3(xPos, -50f, 0f);
            Vector3 targetPos = new Vector3(xPos, yPos, 0f);
            float riseProgress = (audioTime - times[i]) * riseSpeed;

            blocks[i].transform.position = Vector3.Lerp(
                startPos,
                targetPos,
                Mathf.Clamp01(riseProgress)
            );
        }
    }
}




    private void SpawnAllBlocks()
    {
        float globalStartTime = 0f; // or find the earliest of all `times[0]`
        SpawnInstrumentBlocks(kickTimes, kickBlock, kickY, ref kickBlocks, globalStartTime);
        SpawnInstrumentBlocks(snareTimes, snareBlock, snareY, ref snareBlocks, globalStartTime);
        SpawnInstrumentBlocks(hiHatTimes, hiHatBlock, hiHatY, ref hiHatBlocks, globalStartTime);
        SpawnInstrumentBlocks(percTimes, percBlock, percY, ref percBlocks, globalStartTime);

    }

    private void SpawnInstrumentBlocks(List<float> times, GameObject blockPrefab, float yPos, ref List<GameObject> blocks, float globalStartTime)
{
    blocks.Clear();

    for (int i = 0; i < times.Count; i++)
    {
        float xPos = -10f + (times[i] - globalStartTime) * xSpacingFactor;
        GameObject block = Instantiate(blockPrefab, new Vector3(xPos, -50f, 0f), Quaternion.identity);
        blocks.Add(block);
    }
}


    private void GenerateAndPlay()
    {
        if (kickSample == null)
        {
            Debug.LogError("No kick sample assigned!");
            return;
        }

        kickTimes.Clear();
        snareTimes.Clear();
        hiHatTimes.Clear();
        percTimes.Clear();

        float secondsPerBeat = 60f / tempo;
        int samplesPerBeat = Mathf.RoundToInt(secondsPerBeat * sampleRate);
        int samplesPerStep = samplesPerBeat / 4;
        int totalSteps = 16 * totalBars;
        int totalSamples = samplesPerStep * totalSteps;
        ttsamples = totalSamples;

        

        float[] kickData = GetSampleData(kickSample, kickVolume);
        float[] snareData = snareSample ? GetSampleData(snareSample, snareVolume) : null;
        float[] hiHatData = hiHatSample ? GetSampleData(hiHatSample, hiHatVolume) : null;
        float[] percData = percussionSample ? GetSampleData(percussionSample, percussionVolume) : null;

        int introsize = Mathf.RoundToInt((60f/tempo) * sampleRate) * 4;
        intros = introsize;

        float[] clipData = new float[totalSamples + introsize];
        kickdata = new float[totalSamples + introsize];
        snaredata = new float[totalSamples + introsize];
        hatdata = new float[totalSamples + introsize];
        percdata = new float[totalSamples + introsize];

        float audioDuration = totalSamples  / sampleRate;
        trackLength = audioDuration * xSpacingFactor;
        trackStartX = caminital.x;

        AudioClip generatedClip = AudioClip.Create("Drum Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
        kickclip = AudioClip.Create("Kick Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
        snareclip = AudioClip.Create("Snare Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
        hatclip = AudioClip.Create("Hat Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
        percclip = AudioClip.Create("Perc Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);

        int next = 0;
        for (int i = 0 ; i < 4;i++)
        {
            for (int y=0; y < hiHatData.Length; y++)
            {
                clipData[y + next] = hiHatData[y];
                kickdata[y + next] = hiHatData[y];
                snaredata[y + next] = hiHatData[y];
                hatdata[y + next] = hiHatData[y];
                percdata[y + next] = hiHatData[y];
            }
            next += samplesPerBeat;
        }
        int sectionsCount = Mathf.CeilToInt((float)totalBars / sectionLength);
        kickSections = new Pattern[sectionsCount];
        hatSections = new Pattern[sectionsCount];
        snareSections = new Pattern[sectionsCount];
        percSections = new Pattern[sectionsCount];
        int currentStep = 0;

        for (int sectionIndex = 0; sectionIndex < sectionsCount; sectionIndex++)
        {
            //kick
            int kickindex = UnityEngine.Random.Range(0, kickPatterns.Count);
            Pattern currentKick = kickPatterns[kickindex];
            kickSections[sectionIndex] = currentKick;
            
            //snare
            int snareindex = UnityEngine.Random.Range(0, snarePatterns.Count);
            Pattern currentSnare = snarePatterns[snareindex];
            snareSections[sectionIndex] = currentSnare;

            //hat
            int hatindex = UnityEngine.Random.Range(0, hiHatPatterns.Count);
            Pattern currentHiHat = hiHatPatterns[hatindex];
            hatSections[sectionIndex] = currentHiHat;

            //Perc
            int percindex = UnityEngine.Random.Range(0, percPatterns.Count);
            Pattern currentPerc = percPatterns[percindex];
            percSections[sectionIndex] = currentPerc;


            int sectionStartStep = currentStep;
            int sectionEndStep = Mathf.Min(sectionStartStep + (16 * sectionLength), totalSteps);

            for (; currentStep < sectionEndStep; currentStep++)
            {
                int patternStep = currentStep % 16;
                int samplePos = introsize + (currentStep * samplesPerStep);

                if (currentKick.ShouldPlay(patternStep))
                {
                    AddSampleToBuffer(ref clipData, kickData, samplePos);
                    AddSampleToBuffer(ref kickdata, kickData, samplePos);
                    kickTimes.Add(samplePos / sampleRate);
                }
                if (snareSample != null && currentSnare.ShouldPlay(patternStep))
                {
                    AddSampleToBuffer(ref clipData, snareData, samplePos);
                    AddSampleToBuffer(ref snaredata, snareData, samplePos);
                    snareTimes.Add(samplePos / sampleRate);
                }
                if (hiHatSample != null && currentHiHat.ShouldPlay(patternStep))
                {
                    AddSampleToBuffer(ref clipData, hiHatData, samplePos);
                    AddSampleToBuffer(ref hatdata, hiHatData, samplePos);
                    hiHatTimes.Add(samplePos / sampleRate);
                }
                if (percussionSample != null && currentPerc.ShouldPlay(patternStep))
                {
                    AddSampleToBuffer(ref clipData, percData, samplePos);
                    AddSampleToBuffer(ref percdata, percData, samplePos);
                    percTimes.Add(samplePos / sampleRate);
                }
            }
        }

        generatedClip.SetData(clipData, 0);
        kickclip.SetData(kickdata,0);
        snareclip.SetData(snaredata,0);
        hatclip.SetData(hatdata, 0);
        percclip.SetData(percdata, 0);
        clip = generatedClip;
        audioSource.clip = generatedClip;
        kickSource.clip = kickclip;
        snareSource.clip = snareclip;
        hatSource.clip = hatclip;
        percSource.clip = percclip;
    }

    

    private float[] GetSampleData(AudioClip clip, float volume)
    {
        if (!sampleCache.TryGetValue(clip, out var data))
        {
            data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            sampleCache[clip] = data;
        }

        float[] volumeAdjusted = new float[data.Length];
        for (int i = 0; i < data.Length; i++) volumeAdjusted[i] = data[i] * volume;
        return volumeAdjusted;
    }

    private void AddSampleToBuffer(ref float[] buffer, float[] sample, int startIndex)
    {
        if (startIndex < 0 || sample == null) return;
        for (int i = 0; i < sample.Length && startIndex + i < buffer.Length; i++)
        {
            buffer[startIndex + i] += sample[i];
        }
    }



    //Slider
    public void OnsliderDragStart()
    {
        isDraggin= true;
        if (audioSource.isPlaying)
            wasPlaying = true;
        else
            wasPlaying = false;
        
        if (kickSource.isPlaying)
            kickplaying = true;
        else
            kickplaying = false;
        
        if (snareSource.isPlaying)
            snareplaying = true;
        else
            snareplaying = false;
        
        if (hatSource.isPlaying)
            hatplaying = true;
        else
            hatplaying = false;
        
        if (percSource.isPlaying)
            percplaying = true;
        else
            percplaying = false;


        audioSource.Stop();
        kickSource.Stop();
        hatSource.Stop();
        percSource.Stop();
        snareSource.Stop();
    }


   public void OnsliderDrag()
{
    if (audioSource.clip == null) return;
    
    float playbackProgress = slider.value / (float)ttsamples;
    float targetX = trackStartX + (trackLength * playbackProgress);
    camera.position = new Vector3(targetX, camera.position.y, camera.position.z);
    
    audioSource.timeSamples = intros + (int)slider.value;
    kickSource.timeSamples = audioSource.timeSamples;
    hatSource.timeSamples = audioSource.timeSamples;
    snareSource.timeSamples = audioSource.timeSamples;
    percSource.timeSamples = audioSource.timeSamples;
}
    public void OnsliderDragEnd()
    {
        if (wasPlaying)
            audioSource.Play();
        if (kickplaying)
            kickSource.Play();
        if (snareplaying)
            snareSource.Play();
        if (hatplaying)
            hatSource.Play();
        if (percplaying)
            percSource.Play();
        isDraggin = false;
    }

    public void SaveClipToWAV(AudioClip audioC,string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".wav");
        byte[] wavBytes = ConvertToWAV(audioC);
        File.WriteAllBytes(filePath, wavBytes);
        Debug.Log("Saved to: " + filePath);
        Message.text = "Saved to: " + filePath;
    }
    public void SaveAll()
    {
        SaveClipToWAV(clip, Name.text);
    }
    public void SaveKick()
    {
        SaveClipToWAV(kickclip, Name.text + "_kick");
    }
    public void SaveSnare()
    {
        SaveClipToWAV(snareclip, Name.text + "_snare");
    }
    public void SaveHat()
    {
        SaveClipToWAV(hatclip, Name.text + "_hat");
    }
    public void SavePerc()
    {
        SaveClipToWAV(percclip, Name.text + "_perc");
    }
    public void SaveColl()
    {
        SaveAll();
        SaveKick();
        SaveSnare();
        SaveHat();
        SavePerc();
    }

public void GetLayout(Image img)
{
    int index = Array.IndexOf(Sections, img);

    for (int i=0; i < 4; i++)
        for (int y=0; y < 16;y++)
        {
            if (i==0)
            {
                kicktog[y].isOn= kickSections[index].steps[y];

            }
            if (i==1)
                snaretog[y].isOn = snareSections[index].steps[y];
            if (i==2)
                hattog[y].isOn = hatSections[index].steps[y];
            if (i==3)
                perctog[y].isOn = percSections[index].steps[y];
        }
    UpdateIndex = index;
}

public void UpdateSection()
{
    for (int y=0; y < 16;y++)
    {
        kickSections[UpdateIndex].steps[y] = kicktog[y].isOn;
        snareSections[UpdateIndex].steps[y] = snaretog[y].isOn;
        hatSections[UpdateIndex].steps[y] = hattog[y].isOn;
        percSections[UpdateIndex].steps[y] = perctog[y].isOn;
    }
} 


private void ClearAllBlocks()
{
    // Destroy all kick blocks
    foreach (GameObject block in kickBlocks)
    {
        if (block != null) Destroy(block);
    }
    kickBlocks.Clear();

    // Destroy all snare blocks
    foreach (GameObject block in snareBlocks)
    {
        if (block != null) Destroy(block);
    }
    snareBlocks.Clear();

    // Destroy all hi-hat blocks
    foreach (GameObject block in hiHatBlocks)
    {
        if (block != null) Destroy(block);
    }
    hiHatBlocks.Clear();

    // Destroy all percussion blocks
    foreach (GameObject block in percBlocks)
    {
        if (block != null) Destroy(block);
    }
    percBlocks.Clear();
}

public void RegeneratefromEditor()
{
    // Clear existing timing data
    kickTimes.Clear();
    snareTimes.Clear();
    hiHatTimes.Clear();
    percTimes.Clear();
    ClearAllBlocks();
    slider.value = 0;
    camera.position = new Vector3(-7.5f,7.5f,-35.53f);

    // Calculate timing values
    float secondsPerBeat = 60f / tempo;
    int samplesPerBeat = Mathf.RoundToInt(secondsPerBeat * sampleRate);
    int samplesPerStep = samplesPerBeat / 4;
    int totalSteps = 16 * totalBars;
    int totalSamples = samplesPerStep * totalSteps;
    ttsamples = totalSamples;

    // Load sample data
    float[] kickData = GetSampleData(kickSample, kickVolume);
    float[] snareData = snareSample ? GetSampleData(snareSample, snareVolume) : null;
    float[] hiHatData = hiHatSample ? GetSampleData(hiHatSample, hiHatVolume) : null;
    float[] percData = percussionSample ? GetSampleData(percussionSample, percussionVolume) : null;

    // Create intro section
    int introsize = Mathf.RoundToInt((60f/tempo) * sampleRate) * 4;
    intros = introsize;

    // Initialize audio buffers
    float[] clipData = new float[totalSamples + introsize];
    kickdata = new float[totalSamples + introsize];
    snaredata = new float[totalSamples + introsize];
    hatdata = new float[totalSamples + introsize];
    percdata = new float[totalSamples + introsize];

    // Calculate track length for visualization
    float audioDuration = totalSamples / sampleRate;
    trackLength = audioDuration * xSpacingFactor;
    trackStartX = caminital.x;

    // Create audio clips
    AudioClip generatedClip = AudioClip.Create("Drum Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
    kickclip = AudioClip.Create("Kick Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
    snareclip = AudioClip.Create("Snare Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
    hatclip = AudioClip.Create("Hat Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);
    percclip = AudioClip.Create("Perc Pattern", totalSamples + introsize, 1, Mathf.RoundToInt(sampleRate), false);

    // Add intro hi-hat
    int next = 0;
    for (int i = 0; i < 4; i++)
    {
        for (int y = 0; y < hiHatData.Length; y++)
        {
            clipData[y + next] = hiHatData[y];
            kickdata[y + next] = hiHatData[y];
            snaredata[y + next] = hiHatData[y];
            hatdata[y + next] = hiHatData[y];
            percdata[y + next] = hiHatData[y];
        }
        next += samplesPerBeat;
    }

    // Process each step using the existing section patterns
    int currentStep = 0;
    for (int sectionIndex = 0; sectionIndex < kickSections.Length; sectionIndex++)
    {
        Pattern currentKick = kickSections[sectionIndex];
        Pattern currentSnare = snareSections[sectionIndex];
        Pattern currentHiHat = hatSections[sectionIndex];
        Pattern currentPerc = percSections[sectionIndex];

        int sectionStartStep = currentStep;
        int sectionEndStep = Mathf.Min(sectionStartStep + (16 * sectionLength), totalSteps);

        for (; currentStep < sectionEndStep; currentStep++)
        {
            int patternStep = currentStep % 16;
            int samplePos = introsize + (currentStep * samplesPerStep);

            if (currentKick.ShouldPlay(patternStep))
            {
                AddSampleToBuffer(ref clipData, kickData, samplePos);
                AddSampleToBuffer(ref kickdata, kickData, samplePos);
                kickTimes.Add(samplePos / sampleRate);
            }
            if (snareSample != null && currentSnare.ShouldPlay(patternStep))
            {
                AddSampleToBuffer(ref clipData, snareData, samplePos);
                AddSampleToBuffer(ref snaredata, snareData, samplePos);
                snareTimes.Add(samplePos / sampleRate);
            }
            if (hiHatSample != null && currentHiHat.ShouldPlay(patternStep))
            {
                AddSampleToBuffer(ref clipData, hiHatData, samplePos);
                AddSampleToBuffer(ref hatdata, hiHatData, samplePos);
                hiHatTimes.Add(samplePos / sampleRate);
            }
            if (percussionSample != null && currentPerc.ShouldPlay(patternStep))
            {
                AddSampleToBuffer(ref clipData, percData, samplePos);
                AddSampleToBuffer(ref percdata, percData, samplePos);
                percTimes.Add(samplePos / sampleRate);
            }
        }

        SpawnAllBlocks();
    }

    // Finalize audio clips
    generatedClip.SetData(clipData, 0);
    kickclip.SetData(kickdata, 0);
    snareclip.SetData(snaredata, 0);
    hatclip.SetData(hatdata, 0);
    percclip.SetData(percdata, 0);
    
    // Assign clips to audio sources
    clip = generatedClip;
    audioSource.clip = generatedClip;
    kickSource.clip = kickclip;
    snareSource.clip = snareclip;
    hatSource.clip = hatclip;
    percSource.clip = percclip;
}
public void Regenerate()
{
ClearAllBlocks();
slider.value = 0;
camera.position = new Vector3(-7.5f,7.5f,-35.53f);
 Awake();
 Start();
}



private byte[] ConvertToWAV(AudioClip c, bool use32BitFloat = true)
{
    using (MemoryStream stream = new MemoryStream())
    {
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int bitDepth = use32BitFloat ? 32 : 16;
            int bytesPerSample = bitDepth / 8;
            int audioFormat = use32BitFloat ? 3 : 1; // 3 = IEEE float, 1 = PCM

            // WAV Header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + c.samples * c.channels * bytesPerSample); // File size - 8
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16); // Subchunk1 size (16 for PCM/IEEE float)
            writer.Write((ushort)audioFormat);
            writer.Write((ushort)c.channels);
            writer.Write(c.frequency);
            writer.Write(c.frequency * c.channels * bytesPerSample); // Byte rate
            writer.Write((ushort)(c.channels * bytesPerSample));         // Block align
            writer.Write((ushort)bitDepth);                                 // Bits per sample
            writer.Write("data".ToCharArray());
            writer.Write(c.samples * c.channels * bytesPerSample);    // Subchunk2 size

            // Audio Data
            float[] samples = new float[c.samples * c.channels];
            c.GetData(samples, 0);
            for (int i = 0; i < samples.Length; i++)
            {
                if (use32BitFloat)
                    writer.Write(samples[i]);      // 32-bit float
                else
                    writer.Write((short)(samples[i] * short.MaxValue)); // 16-bit PCM
            }
        }
        return stream.ToArray();
    }
}
}