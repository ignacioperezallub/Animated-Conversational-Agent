using UnityEngine;
using System.Collections.Generic;

public class ReactiveBehavior : MonoBehaviour {

    internal VirtualAgent.Character agent_;
    private string blink_;
    private float t;
    private float ran = 0.5f;

    //private Bml.BML blink;

    public void SetAgent(VirtualAgent.Character a)
    {
        agent_ = a;
    }

    private void addBlink()
    {
        var aus = new List<VirtualAgent.ActionUnit>();
        aus.Add(new VirtualAgent.ActionUnit("43", "BOTH", 1));
        //Debug.Log("ADD BLINK !!!!!!!!!!!!!");

        agent_.scheduler.Sched.AddBml("blink", Bml.Composition.MERGE, new List<Bml.Signal>(){
            SignalBuilder.Face(agent_.scheduler.Sched, "blink", "blink", 1, false, aus, agent_.fe,
                new List<Bml.Synchro>(){
                    agent_.scheduler.Sched.NewSynchro("start", "0"),
                    agent_.scheduler.Sched.NewSynchro("ready", "0.25"),
                    agent_.scheduler.Sched.NewSynchro("relax", "0.26"),
                    agent_.scheduler.Sched.NewSynchro("end",   "0.51"),
                    }),
            });
    }

        // Update is called once per frame
    void Update () {
        float current_time = Time.time;
        if ((current_time - t) > ran)
        {
            addBlink();
            t = current_time;
            ran = Random.value * 5.0f;
        }
    }
}
