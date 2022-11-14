using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sequencer : MonoBehaviour
{
    // obj prefabs
    public GameObject the_drumPrefab;
    public GameObject the_glowPrefab;
    // UI
    public Button[] the_buttons;
    public Camera the_cam;
    // playhead prefab
    public GameObject the_playheadPrefab;
    // selector prefab
    public GameObject the_selectorPrefab;
    // stars
    public GameObject[] the_stars;
    // snows
    public GameObject[] the_snows;

    //--------- GRAPHICS -----------
    // number of steps (MUST match NUM_STEPS in ChucK code)
    int NUM_STEPS = 24;
    // number of tracks (MUST match NUM_TRACKS in ChucK code)
    int NUM_TRACKS = 4;
    // number of trees (MUST match NUM_TREES in ChucK code)
    int NUM_TREES = 3;
    // number of instruments
    int NUM_INSTRUMENTS = 4;
    // the global obj array
    GameObject[,] m_obj;
    // obj spacing
    Vector2 m_objSpacing = new Vector2(0.42f, 0.75f);
    // the sequencer playhead
    GameObject m_playhead;
    // the selector thingy over the selected obj
    GameObject m_selector;
    // reference points
    Vector2 treeLeftStart = new Vector2(-7.05f, 0.485f);
    Vector2 treeMidStart = new Vector2(-1.47f, 0.485f);
    Vector2 treeRightStart = new Vector2(4.11f, 0.485f);
    // glows
    GameObject[,] m_glows;
    // instruments (0 - bells; 1 - piano high; 2 - piano low; 3 - drums)
    int[,] m_instruments; // 4x3
    GameObject[,] labels; // 4x3
    public Sprite[] instrumentImages;
    public Sprite[] instrumentTexts;

    // time step for run animation
    private float m_animPlayheadTimeRun;
    // which obj is selected for editing
    private Vector2 m_selectedObj = new Vector2(0, 0);

    // for UI
    private bool zoomedIn = false;

    // --------- AUDIO -------------
    // float sync
    private ChuckFloatSyncer m_ckPlayheadPos;
    // int sync
    private ChuckIntSyncer m_ckCurrentStep;

    // --------- SHARED ------------
    // sequence: this information is duplicated across Chuck and Unity
    // and is communicated to chuck using ChucK events
    private float[,] m_seqRate;
    private float[,] m_seqGain;
    private int[] m_treeStatus;
    private float tempo = 1f;
    // previous discrete obj number
    private int m_previousStep = -1;


    // Start is called before the first frame update
    void Start()
    {
        InitGraphics();
        InitAudio();
        InitSequence();
    }

    // initialize graphics
    void InitGraphics()
    {
        // UI
        the_buttons[0].onClick.AddListener(BackOnClick);
        the_buttons[0].gameObject.SetActive(false);
        the_buttons[1].onClick.AddListener(LeftTreeOnClick);
        the_buttons[1].gameObject.SetActive(true);
        the_buttons[2].onClick.AddListener(MidTreeOnClick);
        the_buttons[2].gameObject.SetActive(true);
        the_buttons[3].onClick.AddListener(RightTreeOnClick);
        the_buttons[3].gameObject.SetActive(true);

        // instantiate obj array
        m_obj = new GameObject[NUM_TRACKS, NUM_STEPS];
        m_glows = new GameObject[NUM_TRACKS, NUM_STEPS];
        m_instruments = new int[NUM_TRACKS, NUM_TREES];
        labels = new GameObject[NUM_TRACKS, NUM_TREES];
        float OBJ_Z = -0.3f;
        float PLAYHEAD_Y = -2.1f;

        // initialize instruments
        for (int i = 0; i < NUM_TRACKS; i++)
        {
            for (int j = 0; j < NUM_TREES; j++)
            {
                m_instruments[i, j] = i;
                labels[i, j] = GameObject.Find("instrument" + j + i);
                labels[i, j].GetComponent<SpriteRenderer>().sprite = instrumentTexts[m_instruments[i, j]];
            }
        }

        // make objects
        for (int i = 0; i < NUM_TRACKS; i++)
        {
            for (int j = 0; j < NUM_STEPS; j++)
            {
                int jSub = j % 8;
                // left tree
                if (j < 8) 
                {
                    m_obj[i, j] = Instantiate(the_drumPrefab, new Vector3(
                        treeLeftStart.x + jSub * m_objSpacing.x, treeLeftStart.y - i * m_objSpacing.y, OBJ_Z), Quaternion.identity);
                }
                // mid tree
                else if (j >= 8 && j < 16) 
                {
                    m_obj[i, j] = Instantiate(the_drumPrefab, new Vector3(
                        treeMidStart.x + jSub * m_objSpacing.x, treeMidStart.y - i * m_objSpacing.y, OBJ_Z), Quaternion.identity);
                }
                // right tree
                else if (j >= 16) 
                {
                    m_obj[i, j] = Instantiate(the_drumPrefab, new Vector3(
                        treeRightStart.x + jSub * m_objSpacing.x, treeRightStart.y - i * m_objSpacing.y, OBJ_Z), Quaternion.identity);
                }
                m_obj[i, j].SetActive(false);
                // glow
                m_glows[i, j] = Instantiate(the_glowPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
                m_glows[i, j].SetActive(false);
                // instruments
                m_obj[i, j].GetComponent<SpriteRenderer>().sprite = instrumentImages[m_instruments[i, j/8]];
                m_obj[i, j].GetComponent<Float>().SetY(m_obj[i, j].transform.position.y);
            }
        }

        // instantiate the playhead
        m_playhead = Instantiate(the_playheadPrefab, new Vector3(treeLeftStart.x, PLAYHEAD_Y, OBJ_Z), Quaternion.identity);

        // instantiate the selector
        m_selector = Instantiate(the_selectorPrefab);
        m_selector.SetActive(false);
    }

    // initialize audio
    void InitAudio()
    {
        // run the sequencer
        GetComponent<ChuckSubInstance>().RunFile("sequencer.ck", true);

        // add the float sync
        m_ckPlayheadPos = gameObject.AddComponent<ChuckFloatSyncer>();
        m_ckPlayheadPos.SyncFloat(GetComponent<ChuckSubInstance>(), "playheadPos");
        // add the int sync
        m_ckCurrentStep = gameObject.AddComponent<ChuckIntSyncer>();
        m_ckCurrentStep.SyncInt(GetComponent<ChuckSubInstance>(), "currentStep");
    }

    // initialize sequence
    void InitSequence()
    {
        // sequence, the rate for each element
        m_seqRate = new float[NUM_TRACKS, NUM_STEPS];
        // sequence, the volume for each element
        m_seqGain = new float[NUM_TRACKS, NUM_STEPS];
        // initialize
        for (int i = 0; i < NUM_TRACKS; i++)
        {
            for (int j = 0; j < NUM_STEPS; j++)
            {
                m_seqRate[i,j] = 1.0f;
                m_seqGain[i,j] = 0f;
            }
        }
        // the tree status
        m_treeStatus = new int[NUM_TREES];
        for (int i = 0; i < NUM_TREES; i++)
        {
            m_treeStatus[i] = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // get bounds of current segment
        int left, right;
        if (m_selectedObj.y < 8) 
        {
            left = 0;
            right = 7;
        }
        else if (m_selectedObj.y >= 8 && m_selectedObj.y < 16)
        {
            left = 8;
            right = 15;
        }
        else
        {
            left = 16;
            right = 23;
        }

        // get input (for obj editing)
        if (zoomedIn)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                m_selectedObj.y--;
                if (m_selectedObj.y < left) m_selectedObj.y = right;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                m_selectedObj.y++;
                if (m_selectedObj.y > right) m_selectedObj.y = left;
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                m_selectedObj.x--;
                if (m_selectedObj.x < 0) m_selectedObj.x = 3;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                m_selectedObj.x++;
                if (m_selectedObj.x > 3) m_selectedObj.x = 0;
            }
            else if (Input.GetKeyDown("left"))
            {
                AdjustGain((int)m_selectedObj.x, (int)m_selectedObj.y, true, false);
            }
            else if (Input.GetKeyDown("right"))
            {
                AdjustGain((int)m_selectedObj.x, (int)m_selectedObj.y, false, false);
            }
            else if (Input.GetKeyDown("up"))
            {
                AdjustRate((int)m_selectedObj.x, (int)m_selectedObj.y, true);
            }
            else if (Input.GetKeyDown("down"))
            {
                AdjustRate((int)m_selectedObj.x, (int)m_selectedObj.y, false);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                AdjustGain((int)m_selectedObj.x, (int)m_selectedObj.y, false, true);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                SwitchInstruments((int)m_selectedObj.x, (int)m_selectedObj.y);
            }
        }

        // detect tempo change
        if (Input.GetKeyDown(KeyCode.V))
        {
            AdjustTempo(false);
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            AdjustTempo(true);
        }

        // update the playhead using info from ChucK's playheadPos
        float playheadPos = m_ckPlayheadPos.GetCurrentValue();
        float playheadPosSub = playheadPos % 8.0f;
        float x = treeLeftStart.x + m_objSpacing.x * playheadPosSub;
        if (playheadPos >= 8.0f && playheadPos < 16.0f) 
        {
            x = treeMidStart.x + m_objSpacing.x * playheadPosSub;
        } 
        else if (playheadPos >= 16.0f)
        {
            x = treeRightStart.x + m_objSpacing.x * playheadPosSub; 
        }
        m_playhead.transform.position = new Vector3(
            x,
            m_playhead.transform.position.y,
            m_playhead.transform.position.z);

        // get current step number
        int currentStep = m_ckCurrentStep.GetCurrentValue();
        // should glow?
        if (currentStep != m_previousStep)
        {
            // object, glow!
            Glow(m_previousStep, currentStep);
            // remember
            m_previousStep = currentStep;
        }
        // position the selector
        PositionSelector();
        // snow fall
        for (int i = 0; i < the_snows.Length; i++)
        {
            the_snows[i].transform.position = new Vector3(
                the_snows[i].transform.position.x,
                the_snows[i].transform.position.y - (((-1f * tempo + 2f) - 0.5f) * 0.02f + 0.001f),
                the_snows[i].transform.position.z
            );
            if (the_snows[i].transform.position.y < -13.78f) {
                the_snows[i].transform.position = new Vector3(
                    the_snows[i].transform.position.x,
                    5.18f,
                    the_snows[i].transform.position.z
                );
            };
        }
    }

    // position selector
    void PositionSelector()
    {
        float selectorOffset = 0.235f;
        // the selected object
        GameObject o = m_obj[(int)m_selectedObj.x, (int)m_selectedObj.y];
        // translate the selector
        m_selector.transform.position = new Vector3(o.transform.position.x, o.GetComponent<Float>().GetY() + selectorOffset, o.transform.position.z);
    }

    // glow
    void Glow(int previousStep, int currentStep)
    {
        // glow!
        for (int i = 0; i < NUM_TRACKS; i++)
        {
            if (previousStep >= 0) m_glows[i, previousStep].SetActive(false);
            m_glows[i, currentStep].SetActive(true);
            m_glows[i, currentStep].transform.position = m_obj[i, currentStep].transform.position;
        }
    }

    // adjust rate
    void AdjustRate(int whichTrack, int whichStep, bool isUp)
    {
        // ajdust and transform according to rate
        GameObject o = m_obj[whichTrack, whichStep];
        if (isUp)
        {
            m_seqRate[whichTrack, whichStep] += .2f;
            if (m_seqRate[whichTrack, whichStep] > 1.9f) 
            {
                m_seqRate[whichTrack, whichStep] = 1.8f;
            }
            else
            {
                o.transform.position = new Vector3(o.transform.position.x, o.GetComponent<Float>().GetY() + .05f, o.transform.position.z);
                o.GetComponent<Float>().SetY(o.transform.position.y);
            }
        }
        else
        {
            m_seqRate[whichTrack, whichStep] -= .2f;
            if (m_seqRate[whichTrack, whichStep] < .4f)
            {
                m_seqRate[whichTrack, whichStep] = .4f;
            }
            else
            {
                o.transform.position = new Vector3(o.transform.position.x, o.GetComponent<Float>().GetY() - .05f, o.transform.position.z);
                o.GetComponent<Float>().SetY(o.transform.position.y);
            }
        }

        // set which, rate/gain, and instrument, and fire the event
        GetComponent<ChuckSubInstance>().SetInt("editWhichTrack", whichTrack);
        GetComponent<ChuckSubInstance>().SetInt("editWhichStep", whichStep);
        GetComponent<ChuckSubInstance>().SetFloat("editRate", m_seqRate[whichTrack, whichStep]);
        GetComponent<ChuckSubInstance>().SetFloat("editGain", m_seqGain[whichTrack, whichStep]);
        GetComponent<ChuckSubInstance>().SetInt("editWhichTree", whichStep / 8);
        GetComponent<ChuckSubInstance>().SetInt("editInstrument", m_instruments[whichTrack, whichStep / 8]);
        GetComponent<ChuckSubInstance>().SetInt("editTreeStatus", m_treeStatus[whichStep / 8]);
        GetComponent<ChuckSubInstance>().SetFloat("editTempo", tempo);
        GetComponent<ChuckSubInstance>().BroadcastEvent("editHappened");
    }

    // adjust gain
    void AdjustGain(int whichTrack, int whichStep, bool isLeft, bool isSpace)
    {
        if (isSpace)
        {
            // mute or unmute
            if (m_seqGain[whichTrack, whichStep] == 0f) 
            {
                m_seqGain[whichTrack, whichStep] = 1f;
                m_obj[whichTrack, whichStep].SetActive(true);
                m_treeStatus[whichStep / 8] += 1;
                the_stars[whichStep / 8].GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                m_seqGain[whichTrack, whichStep] = 0f;
                m_obj[whichTrack, whichStep].SetActive(false);
                m_treeStatus[whichStep / 8] -= 1;
                the_stars[whichStep / 8].GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, .2f);
            }
        }

        else
        {
            // ajdust
            if (!isLeft)
            {
                m_seqGain[whichTrack, whichStep] *= 1.1f;
                if (m_seqGain[whichTrack, whichStep] > 5f) m_seqGain[whichTrack, whichStep] = 5f;
            }
            else
            {
                m_seqGain[whichTrack, whichStep] *= .9f;
                if (m_seqGain[whichTrack, whichStep] < .2f) m_seqGain[whichTrack, whichStep] = .2f;
            }

            // scale according to gain
            GameObject o = m_obj[whichTrack, whichStep];
            o.transform.localScale = new Vector3(
                o.transform.localScale.y * m_seqGain[whichTrack, whichStep], o.transform.localScale.y, o.transform.localScale.z);
        }

        // set which, rate/gain, and instrument, and fire the event
        GetComponent<ChuckSubInstance>().SetInt("editWhichTrack", whichTrack);
        GetComponent<ChuckSubInstance>().SetInt("editWhichStep", whichStep);
        GetComponent<ChuckSubInstance>().SetFloat("editRate", m_seqRate[whichTrack, whichStep]);
        GetComponent<ChuckSubInstance>().SetFloat("editGain", m_seqGain[whichTrack, whichStep]);
        GetComponent<ChuckSubInstance>().SetInt("editWhichTree", whichStep / 8);
        GetComponent<ChuckSubInstance>().SetInt("editInstrument", m_instruments[whichTrack, whichStep / 8]);
        GetComponent<ChuckSubInstance>().SetInt("editTreeStatus", m_treeStatus[whichStep / 8]);
        GetComponent<ChuckSubInstance>().SetFloat("editTempo", tempo);
        GetComponent<ChuckSubInstance>().BroadcastEvent("editHappened");
    }

    // switch instruments
    void SwitchInstruments(int whichTrack, int whichStep)
    {
        // update m_instruments
        int whichTree = whichStep / 8;
        m_instruments[whichTrack, whichTree] += 1;
        if (m_instruments[whichTrack, whichTree] >= NUM_INSTRUMENTS) {
            m_instruments[whichTrack, whichTree] = 0;
        }
        // update images
        for (int j = 0; j < 8; j++)
        {
            m_obj[whichTrack, whichTree * 8 + j].GetComponent<SpriteRenderer>().sprite = instrumentImages[m_instruments[whichTrack, whichTree]];
        }
        // update track labels
        labels[whichTrack, whichTree].GetComponent<SpriteRenderer>().sprite = instrumentTexts[m_instruments[whichTrack, whichTree]];

        // set which, rate/gain, and instrument, and fire the event
        GetComponent<ChuckSubInstance>().SetInt("editWhichTrack", whichTrack);
        GetComponent<ChuckSubInstance>().SetInt("editWhichStep", whichStep);
        GetComponent<ChuckSubInstance>().SetFloat("editRate", m_seqRate[whichTrack, whichStep]);
        GetComponent<ChuckSubInstance>().SetFloat("editGain", m_seqGain[whichTrack, whichStep]);
        GetComponent<ChuckSubInstance>().SetInt("editWhichTree", whichStep / 8);
        GetComponent<ChuckSubInstance>().SetInt("editInstrument", m_instruments[whichTrack, whichStep / 8]);
        GetComponent<ChuckSubInstance>().SetInt("editTreeStatus", m_treeStatus[whichStep / 8]);
        GetComponent<ChuckSubInstance>().SetFloat("editTempo", tempo);
        GetComponent<ChuckSubInstance>().BroadcastEvent("editHappened");
    }

    // adjust tempo with clamping
    void AdjustTempo(bool increasing)
    {
        Debug.Log("adjust tempo " + increasing);
        if (increasing) 
        {
            tempo -= 0.1f;
            if (tempo < 0.5f) tempo = 0.5f;
        }
        else
        {
            tempo += 0.1f;
            if (tempo > 1.5f) tempo = 1.5f;
        }
        GetComponent<ChuckSubInstance>().SetFloat("editTempo", tempo);
        GetComponent<ChuckSubInstance>().BroadcastEvent("editHappened");
    }

    void BackOnClick()
    {
        m_selector.SetActive(false);
        the_cam.transform.position = new Vector3(0, 0, -1);
        the_cam.orthographicSize += 3.5f;
        the_buttons[0].gameObject.SetActive(false);
        the_buttons[1].gameObject.SetActive(true);
        the_buttons[2].gameObject.SetActive(true);
        the_buttons[3].gameObject.SetActive(true);
        zoomedIn = false;
    }

    void LeftTreeOnClick()
    {
        the_cam.transform.position = new Vector3(-5.58f, -0.69f, -1f);
        m_selectedObj = new Vector2(0, 0);
        TreeOnClick();
    }

    void MidTreeOnClick()
    {
        the_cam.transform.position = new Vector3(0f, -0.69f, -1f);
        m_selectedObj = new Vector2(0, 8);
        TreeOnClick();
    }

    void RightTreeOnClick()
    {
        the_cam.transform.position = new Vector3(5.58f, -0.69f, -1f);
        m_selectedObj = new Vector2(0, 16);
        TreeOnClick();
    }

    void TreeOnClick()
    {
        m_selector.SetActive(true);
        PositionSelector();
        the_cam.orthographicSize -= 3.5f;
        the_buttons[0].gameObject.SetActive(true);
        the_buttons[1].gameObject.SetActive(false);
        the_buttons[2].gameObject.SetActive(false);
        the_buttons[3].gameObject.SetActive(false);
        zoomedIn = true;
    }
}
