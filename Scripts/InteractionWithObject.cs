using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class InteractionWithObject : MonoBehaviour
{
    public InteractionObject interObj;
    GameObject leftHand;
    InteractionTarget intTar;
    Animator anim;
    AnimatorOverrideController animatorOverrideController;

    public void Init(Animator a)
    {
        if(interObj == null)   
            interObj = gameObject.AddComponent<InteractionObject>();

        interObj.weightCurves = new InteractionObject.WeightCurve[] { new InteractionObject.WeightCurve(),
                                                                      new InteractionObject.WeightCurve(),
                                                                      new InteractionObject.WeightCurve(),
                                                                      new InteractionObject.WeightCurve()};
        interObj.weightCurves[0].curve = new AnimationCurve();

        interObj.weightCurves[0].type = InteractionObject.WeightCurve.Type.PositionWeight;
        interObj.weightCurves[0].curve.keys = new UnityEngine.Keyframe[] { new Keyframe(0, 0.0f),
                                                                           new Keyframe(0.25f, 1.0f),
                                                                           new Keyframe(0.75f, 1.0f),
                                                                           new Keyframe(1.0f, 0.0f)};
        interObj.weightCurves[1].curve = new AnimationCurve();

        interObj.weightCurves[1].type = InteractionObject.WeightCurve.Type.PositionOffsetY;
        interObj.weightCurves[1].curve.keys = new UnityEngine.Keyframe[] { new Keyframe(0, 0.0f),
                                                                           new Keyframe(0.2f, 0.25f),
                                                                           new Keyframe(0.3f, 0.0f),
                                                                           new Keyframe(1.0f, 0.0f)};
        interObj.weightCurves[2].curve = new AnimationCurve();

        interObj.weightCurves[2].type = InteractionObject.WeightCurve.Type.PositionOffsetX;
        interObj.weightCurves[3].curve = new AnimationCurve();

        interObj.weightCurves[3].type = InteractionObject.WeightCurve.Type.PositionOffsetZ;


        interObj.multipliers = new InteractionObject.Multiplier[] { new InteractionObject.Multiplier(),
                                                                    new InteractionObject.Multiplier()};
        interObj.multipliers[0].curve = InteractionObject.WeightCurve.Type.PositionWeight;
        interObj.multipliers[0].multiplier = 1;
        interObj.multipliers[0].result = InteractionObject.WeightCurve.Type.RotateBoneWeight;
        interObj.multipliers[1].curve = InteractionObject.WeightCurve.Type.PositionWeight;
        interObj.multipliers[1].multiplier = 1;
        interObj.multipliers[1].result = InteractionObject.WeightCurve.Type.PoserWeight;
        interObj.events = new InteractionObject.InteractionEvent[] { new InteractionObject.InteractionEvent() };

        interObj.events[0].time = 0.5f;
        interObj.events[0].pause = false;
        interObj.events[0].pickUp = false;
        interObj.events[0].animations = new InteractionObject.AnimatorEvent[] { new InteractionObject.AnimatorEvent() };
        interObj.events[0].unityEvent = new UnityEngine.Events.UnityEvent();
        interObj.events[0].messages = new InteractionObject.Message[] { };

 /*       Animation anim = gameObject.AddComponent<Animation>();
        AnimationCurve curve;

        //nim = Instantiate<Animator>(a);// as Animator;
        // create a new AnimationClip
        AnimationClip clip = new AnimationClip();
        clip.legacy = true;

        // create a curve to move the GameObject and assign to the clip
        Keyframe[] keys;
        keys = new Keyframe[3];
        keys[0] = new Keyframe(0.0f, 0.0f);
        keys[1] = new Keyframe(1.0f, 1.5f);
        keys[2] = new Keyframe(2.0f, 0.0f);
        curve = new AnimationCurve(keys);
        clip.SetCurve("", typeof(Transform), "localPosition.x", curve);

        // update the clip to a change the red color
        curve = AnimationCurve.Linear(0.0f, 1.0f, 2.0f, 0.0f);
        clip.SetCurve("", typeof(Material), "_Color.r", curve);
        anim.AddClip(clip, clip.name);
        anim.Play(clip.name);*/
//        animatorOverrideController["idle"] = clip;
  //      anim.runtimeAnimatorController = animatorOverrideController;



        interObj.Initiate();
    }

    public void PrepareForInteraction()
    { 
        leftHand = transform.Find("LeftHand") != null ? transform.Find("LeftHand").gameObject : null;
        if (leftHand == null)
        {
            Debug.Log("HEEEEEREEEEE");
            leftHand = Instantiate(Resources.Load("HandPrefabs\\LeftHand"), transform) as GameObject;
            leftHand.name = "LeftHand";
            //leftHand.transform.TransformPoint(transform.position);
            //leftHand.transform.SetParent(transform);
            
            Vector3 newScale = new Vector3(1/transform.localScale.x, 1 / transform.localScale.y, 1 / transform.localScale.z);
            leftHand.transform.localScale = newScale; 
            //intTar = leftHand.AddComponent<InteractionTarget>();
            //intTar.effectorType = FullBodyBipedEffector.LeftHand;
        }
        //intTar.threeDOFWeight = 3;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
