using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TextToSpeech;



public class StartEnvironment : MonoBehaviour
{
    //Agent
    public Dictionary<string, VirtualAgent.Character> agents = new Dictionary<string, VirtualAgent.Character>();
    public GestureCreator gc;
    public HandShapeCreator hsc;

    //BML
    static public bool newbml = false;
    static string bml = "";


    void Start()
    {

        //CREATION DU TTS ENGINE 
        Engine te = new Engine(UnityEngine.Application.streamingAssetsPath + "\\cereproc", delegate (string msg)
        {
            UnityEngine.Debug.Log(msg);
        });

        foreach (GameObject vh in GameObject.FindGameObjectsWithTag("VirtualHuman"))
        {
            // INSTRUCTIONS NECESSAIRES POUR LA CREATION D'UN AGENT VIRTUEL
            UnityEngine.Debug.Log(vh.name);
            Animation.UnityAnimationEngine ae = vh.AddComponent<Animation.UnityAnimationEngine>();
            ae.Init();
            BmlSchedulerBehaviour bmlScheduler = vh.AddComponent<BmlSchedulerBehaviour>();
            bmlScheduler.Init();
            ReactiveBehavior rb = vh.AddComponent<ReactiveBehavior>();
            VirtualAgent.Character a = new VirtualAgent.Character(vh.name, ae, te, bmlScheduler, rb, delegate (string msg)
            {
                UnityEngine.Debug.Log(msg);
            });

            if (a.name == "Audrey")
            {
                //ATTENTION L'AGENT DOIT ETRE EN 000 ET AVOIR UNE ROTATION DE 000
                //gc = vh.AddComponent<GestureCreator>(); gc.init(a);
                //hsc = vh.AddComponent<HandShapeCreator>(); hsc.init(a);
            }
            agents.Add(vh.name, a);
        }
    }


    void Update()
    {
        float zPos = GameObject.Find("Camera").transform.position.z - GameObject.Find("Audrey").transform.position.z; ;

        //TO directly launch a bml for testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //string bml;
            using (var streamReader = new System.IO.StreamReader(UnityEngine.Application.streamingAssetsPath + "\\xml\\test.xml", System.Text.Encoding.UTF8))
            {
                bml = streamReader.ReadToEnd();
            }
            if (agents.ContainsKey("Audrey"))
                agents["Audrey"].AddBml(bml, agents["Audrey"].name);
            else
                UnityEngine.Debug.Log("The bml has been sent to an agent who does not exist");
        }
    }
}
