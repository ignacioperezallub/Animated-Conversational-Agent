using UnityEngine;
using RootMotion.FinalIK;
using System.Collections.Generic;
using System;
using System.Collections;



internal class KeyFrameGaze
{
    internal readonly string id;
    internal GameObject target_from, target_to;
    internal readonly double time, endTime;
    internal readonly bool shift;

    public KeyFrameGaze(string i, GameObject from, GameObject to, double t, double et, bool s = false)
    {
        id = i; target_from = from; target_to = to;
        time = t; endTime = et;
        shift = s;
    }
}

public struct KeyFrameFace
{
    public float time;
    public float value;

    public KeyFrameFace(float t, float v)
    {
        time = t; value = v;
    }
}

class HeadKeyFrame
{
    internal Quaternion angle;
    internal Vector3 eulerAngle;
    internal float start;
    internal float end;
}

public class TestSolvers : MonoBehaviour
{
    // test mocap
    protected Animator animator;
    private FullBodyBipedIK fbbik;
    internal LookAtIK lookAt;

    //test wrist rotation
    internal Transform leftWrist;
    internal Transform rightWrist;
    internal Quaternion fromLeft, fromRight, fromLeftBone, toLeft;
    internal float start = 1000, end = 0;
    internal float xl = 0, zl = 0, yl = 0, toLeftX = 0, toLeftY = 0, toLeftZ = 0;
    internal float xr = 0, zr = 0, yr = 0, toRightX = 0, toRightY = 0, toRightZ = 0;
    internal List<KFrame> leftArmKeyframes = new List<KFrame>();
    internal List<KFrame> torsoKeyframes = new List<KFrame>();
    internal List<KeyFrameGaze> keyframesgaze = new List<KeyFrameGaze>();
    internal List<HeadKeyFrame> headKeyframes = new List<HeadKeyFrame>();

    //test symbolic positions
    public GameObject left, right;
    internal Transform spine;
    internal float spine_shoulder;
    internal float head_spine;
    internal float depth;
    internal float armLength, foreArmLength;
    internal Dictionary<Vector3, Vector3> symbolicPositions;
    internal bool recomputePositions = true;
    public Vector3 left_shoulder, right_shoulder;
    internal Transform head_transform;

    internal Animation.CharDimensions characterDimensions;

    public GameObject leftArmSwivelAttractor, rightArmSwivelAttractor;

    internal Transform leftArm, rightArm;
    internal GameObject initialPose;
    internal Transform initialTransform;


    internal int r = 1, h = -1, d = -1;

    Transform init_spine;

    bool animGesture = false;
    bool animTorso = false;
    bool animGaze = false;


    void Start()
    {
        animator = GetComponent<Animator>();
        fbbik = GetComponent<FullBodyBipedIK>();
        lookAt = GetComponent<LookAtIK>();
        fbbik.enabled = false;
        lookAt.enabled = false;
        leftWrist = findChild_(transform, "LeftHand");
        rightWrist = findChild_(transform, "RightHand");

        spine = findChild_(transform, "Spine");
        Transform headEnd = findChild_(transform, "HeadEnd");
        Transform leftShoulder = findChild_(transform, "LeftShoulder");
        head_spine = -Vector3.Distance(spine.position, headEnd.position) / transform.localScale.x;
        spine_shoulder = -Vector3.Distance(spine.position, leftShoulder.position) / transform.localScale.x;

        head_transform = findChild_(transform, "Head");

        Transform leftHand = findChild_(transform, "LeftHand");
        leftArm = findChild_(transform, "LeftArm");
        rightArm = findChild_(transform, "RightArm");
        Transform leftForeArm = findChild_(transform, "LeftForeArm");
        armLength = (Vector3.Distance(leftShoulder.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftHand.position)) / transform.localScale.x;
        foreArmLength = Vector3.Distance(leftForeArm.position, leftArm.position);

        Transform neck = findChild_(transform, "Neck");
        depth = -Vector3.Distance(headEnd.position, neck.position) / transform.localScale.x;

        left_shoulder = new Vector3(transform.InverseTransformPoint(leftArm.position).x,
                                 transform.InverseTransformPoint(leftArm.position).y - 0.05f,
                                 transform.InverseTransformPoint(leftArm.position).z + 0.05f);

        right_shoulder = new Vector3(transform.InverseTransformPoint(rightArm.position).x,
                         transform.InverseTransformPoint(rightArm.position).y - 0.05f,
                         transform.InverseTransformPoint(rightArm.position).z + 0.05f);


        left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        left.transform.position = leftArm.position;
        left.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        left.name = "left";
        left.transform.SetParent(transform);
        left.GetComponent<Renderer>().material.color = new Color(0, 1, 1, 1);
        left.SetActive(true);

        right = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        right.transform.position = rightArm.position;
        right.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        right.name = "right";
        right.transform.SetParent(transform);
        right.GetComponent<Renderer>().material.color = new Color(0, 0, 1, 1);
        right.SetActive(true);

        SetCharacterDimensions(ref characterDimensions);

        leftArmSwivelAttractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftArmSwivelAttractor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        leftArmSwivelAttractor.transform.position = GetAbsoluteSwivelPosition(2);
        leftArmSwivelAttractor.transform.SetParent(transform);
        leftArmSwivelAttractor.name = "leftswivel";
        leftArmSwivelAttractor.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
        leftArmSwivelAttractor.SetActive(true);
        transform.GetComponent<FullBodyBipedIK>().solver.leftArmChain.bendConstraint.bendGoal = leftArmSwivelAttractor.transform;

        rightArmSwivelAttractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightArmSwivelAttractor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        rightArmSwivelAttractor.transform.position = GetAbsoluteSwivelPosition(-2);
        rightArmSwivelAttractor.transform.SetParent(transform);
        rightArmSwivelAttractor.name = "rightswivel";
        rightArmSwivelAttractor.GetComponent<Renderer>().material.color = new Color(1, 1, 0, 1);
        rightArmSwivelAttractor.SetActive(true);
        transform.GetComponent<FullBodyBipedIK>().solver.rightArmChain.bendConstraint.bendGoal = rightArmSwivelAttractor.transform;

        initialPose = Instantiate<GameObject>(transform.gameObject);
        initialPose.SetActive(false);
        initialTransform = Instantiate<Transform>(findChild_(transform, "Spine2"));
        //initialTransform.position = transform.InverseTransformPoint(initialTransform.position);
        //left.transform.position = initialTransform.position;

        init_spine = findChild_(initialPose.transform, "Spine2");
        init_spine.parent = transform;

        
    }

    private void SetCharacterDimensions(ref Animation.CharDimensions cd)
    {
        cd = new Animation.CharDimensions();
        Transform spine = findChild_(transform, "Spine");
        Transform spine2 = findChild_(transform, "Spine2");
        Transform spine1 = findChild_(transform, "Spine1");
        Transform head = findChild_(transform, "Head");
        Transform headEnd = findChild_(transform, "HeadEnd");
        Transform shoulder = findChild_(transform, "LeftShoulder");
        Transform foreArm = findChild_(transform, "LeftForeArm");
        Transform arm = findChild_(transform, "LeftArm");
        Transform hand = findChild_(transform, "LeftHand");

        cd.armLength = (Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position)) / transform.localScale.x;
        // HEIGH

        float shoulder2chest = Vector3.Distance(shoulder.position, spine2.position) * 0.66f / transform.localScale.y;
        cd.chest = new Tuple<float, float>(shoulder2chest, shoulder2chest);

        float shoulder2abdomen = (Vector3.Distance(shoulder.position, spine2.position) + Vector3.Distance(spine1.position, spine2.position) * 0.5f) / transform.localScale.y;
        cd.abdomen = new Tuple<float, float>(shoulder2abdomen, Math.Abs(shoulder2abdomen - shoulder2chest));

        float shoulder2belt = Vector3.Distance(shoulder.position, spine.position) / transform.localScale.y;
        cd.belt = new Tuple<float, float>(shoulder2belt, Math.Abs(shoulder2belt - shoulder2abdomen));

        float shoulder2belowBelt = (Vector3.Distance(shoulder.position, spine.position) + Vector3.Distance(spine1.position, spine2.position)*0.5f) / transform.localScale.y;
        cd.belowBelt = new Tuple<float, float>(shoulder2belowBelt, Math.Abs(shoulder2belowBelt - shoulder2belt));

        float shoulder2aboveHead = Vector3.Distance(shoulder.position, headEnd.position)*1.5f / transform.localScale.y;
        cd.aboveHead = new Tuple<float, float>(shoulder2aboveHead, shoulder2chest);

        float shoulder2head = Vector3.Distance(shoulder.position, head.position) / transform.localScale.y;
        cd.head = new Tuple<float, float>(shoulder2head, shoulder2aboveHead);

        cd.shoulder = new Tuple<float, float>(0, shoulder2head);



        // RADIAL ORIENTATION
        float shoulder2front = Vector3.Distance(shoulder.position, arm.position) / transform.localScale.x;
        cd.front = new Tuple<float, float>(shoulder2front, shoulder2front);

        float shoulder2farOut = (Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position)) / transform.localScale.x;
        cd.farOut = new Tuple<float, float>(shoulder2farOut, shoulder2front);

        float shoulder2out = Vector3.Distance(foreArm.position, arm.position) / transform.localScale.x;
        cd.justOut = new Tuple<float, float>(shoulder2out, Math.Abs(shoulder2farOut - shoulder2out));

        cd.side = new Tuple<float, float>(0, shoulder2out);

        float shoulder2inward = Vector3.Distance(shoulder.position, arm.position)*2 / transform.localScale.x;
        cd.inward = new Tuple<float, float>(shoulder2inward, Math.Abs(shoulder2inward - shoulder2front));

        // DISTANCE
        float shoulder2far = (Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position)) / transform.localScale.z;
        cd.far = new Tuple<float, float>(shoulder2far, shoulder2far * 0.3f);

        float shoulder2normal = shoulder2far * 0.7f;
        cd.normal = new Tuple<float, float>(shoulder2normal, Math.Abs(shoulder2far - shoulder2normal)); 
        
        float shoulder2close = shoulder2far * 0.45f;
        cd.close = new Tuple<float, float>(shoulder2close, Math.Abs(shoulder2normal - shoulder2close));

        
        float shoulder2touch = shoulder2far * 0.3f;
        cd.touch = new Tuple<float, float>(shoulder2touch, Math.Abs(shoulder2close - shoulder2touch));

        // ARM SWIVEL radiant   (Math.PI * (degree) / 180.0)
        cd.swivelTouch = new Tuple<float, float>(1.7453f, -0.349f);   //(100 degree, -20 degree)  
        cd.swivelNormal = new Tuple<float, float>(1.3962f, -0.6981f); // (80 degree, -40 degree)
        cd.swivelOut = new Tuple<float, float>(0.6981f, -0.6981f);    // (40 degree, -40 degree)
        cd.swivelOrthogonal = new Tuple<float, float>(0, -0.349f);     // ( 0 degree,  20 degree)
    }


    Vector3 GetAbsoluteSwivelPosition(int i, float su = 0)
    {
        double radiant = 0;
        int sign = i / Math.Abs(i);
        switch(i)
        {
            case 1: case -1: //touch
                radiant = sign * (characterDimensions.swivelTouch.Item1 + characterDimensions.swivelTouch.Item2 * su);
                break;
            case 2: case -2: //normal
                radiant = sign * (characterDimensions.swivelNormal.Item1 + characterDimensions.swivelNormal.Item2 * su);
                break;
            case 3: case -3: // out
                radiant = sign * (characterDimensions.swivelOut.Item1 + characterDimensions.swivelOut.Item2 * su);
                break;
            case 4: case -4: //orthogonal
                radiant = sign * (characterDimensions.swivelOrthogonal.Item1 + characterDimensions.swivelOrthogonal.Item2 * su);
                break;
            default:
                radiant = sign * characterDimensions.swivelNormal.Item1;
                break;
        }

        Vector3 pos = new Vector3();
        if(i>0) // left
            pos = leftArm.position - new Vector3(foreArmLength * (float)Math.Cos(radiant), foreArmLength * (float)Math.Sin(radiant), 0);
        else
            pos = rightArm.position + new Vector3(foreArmLength * (float)Math.Cos(radiant), foreArmLength * (float)Math.Sin(radiant), 0);

        return pos;
    }


    Vector3 GetAbsoluteWristPosition(int radialOrientation, int heigh, int distance, float ru = 0.0f, float hu = 0, float du = 0)
    {
        Vector3 pos = new Vector3();
        switch (heigh)
        {
            case 0: //above_head
                pos.y = characterDimensions.aboveHead.Item1 + characterDimensions.aboveHead.Item2 * hu;
                break;
            case 1: //head
                pos.y = characterDimensions.head.Item1 + characterDimensions.head.Item2 * hu;
                break;
            case 2: //shoulder
                pos.y = characterDimensions.shoulder.Item1 + characterDimensions.shoulder.Item2 * hu;
                break;
            case 3: //chest
                pos.y = -characterDimensions.chest.Item1 + characterDimensions.chest.Item2 * hu;
                break;
            case 4: //abdoment
                pos.y = -characterDimensions.abdomen.Item1 + characterDimensions.abdomen.Item2 * hu;
                break;
            case 5: //belt
                pos.y = -characterDimensions.belt.Item1 + characterDimensions.belt.Item2 * hu;
                break;
            case 6: //below_belt
                pos.y = -characterDimensions.belowBelt.Item1 + characterDimensions.belowBelt.Item2 * hu;
                break;
            default:
                pos.y = 0; break;
        }

        int sign = radialOrientation / Math.Abs(radialOrientation);
        switch (radialOrientation)
        {
            case 1: case -1://left far_out
                pos.x = -sign * characterDimensions.farOut.Item1 - characterDimensions.farOut.Item2 * ru;
                break;
            case 2: case -2: //left out
                pos.x = -sign * characterDimensions.justOut.Item1 - characterDimensions.justOut.Item2 * ru;
                break;
            case 3: case -3://left side
                pos.x = -sign * characterDimensions.side.Item1 - characterDimensions.side.Item2 * ru;
                break;
            case 4: case -4: //left front
                pos.x = sign * characterDimensions.front.Item1 - characterDimensions.front.Item2 * ru;
                break;
            case 5: case -5://left inward
                pos.x = sign * characterDimensions.inward.Item1 - characterDimensions.inward.Item2 * ru;
                break;
            default:
                pos.x = 0; break;
        }

        switch(distance)
        {
            case 3: //touch
                pos.z = characterDimensions.touch.Item1 + characterDimensions.touch.Item2 * du;
                break;
            case 2:
            default://close
                pos.z = characterDimensions.close.Item1 + characterDimensions.close.Item2 * du;
                break;
            case 1: //normal
                pos.z = characterDimensions.normal.Item1 + characterDimensions.normal.Item2 * du;
                break;
            case 0: //far
                pos.z = characterDimensions.far.Item1 + characterDimensions.far.Item2 * du;
                break;
        }

        if (pos.magnitude > characterDimensions.armLength && hu==0 && ru ==0 && du==0)
        {
            pos.Normalize();
            pos *= characterDimensions.armLength;
        }

        return radialOrientation > 0? left.transform.position + pos : right.transform.position + pos;
    }

    public Transform findChild_(Transform obj, string name)
    {
        if (obj.name == name)
            return obj;
        for (int i = 0; i < obj.childCount; i++)
        {
            Transform t = findChild_(obj.GetChild(i).transform, name);
            if (t != null) return t;
        }
        return null;
    }

    public void writeAngles()
    {

        //Debug.Log("Right " + rightWrist.rotation.eulerAngles + " " + rightWrist.localRotation.eulerAngles + " " + fbbik.solver.rightHandEffector.rotation.eulerAngles);
        //Debug.Log("Left " + leftWrist.rotation.eulerAngles + " " + leftWrist.localRotation.eulerAngles + " " + fbbik.solver.leftHandEffector.rotation.eulerAngles);
    }

    public void gaze()
    {
        GameObject target = GameObject.Find("comm");
        //writeAngles();
        KeyFrameGaze kf = new KeyFrameGaze("start", null, target, Time.time, (double)Time.time + 3);
        keyframesgaze.Insert(0, kf);

        kf = new KeyFrameGaze("ready", target, target, (double)Time.time+3, (double)Time.time + 6);
        keyframesgaze.Insert(0, kf);

        kf = new KeyFrameGaze("relax", target, null, (double)Time.time+6, (double)Time.time + 9);
        keyframesgaze.Insert(0, kf);
    }

    public void head()
    {
        //writeAngles();
        HeadKeyFrame kf = new HeadKeyFrame();
        kf.start = Time.time;
        kf.end = kf.start + 0.5f;
        kf.angle = Quaternion.identity;
        kf.eulerAngle = new Vector3(0, 0, 0);
        headKeyframes.Insert(0, kf);

        kf = new HeadKeyFrame();
        kf.start = Time.time + 0.5f;
        kf.end = kf.start + 0.5f;
        kf.angle = Quaternion.Euler(20, 0, 20);
        kf.eulerAngle = new Vector3(0, -20, 20);
        headKeyframes.Insert(0, kf);

        kf = new HeadKeyFrame();
        kf.start = Time.time + 1f;
        kf.end = kf.start + 0.5f;
        kf.angle = Quaternion.Euler(-20, 0, 20);
        kf.eulerAngle = new Vector3(0, -20, -20);
        headKeyframes.Insert(0, kf);

        kf = new HeadKeyFrame();
        kf.start = Time.time + 1.5f;
        kf.end = kf.start + 0.5f;
        kf.angle = Quaternion.Euler(20, 0, 20);
        kf.eulerAngle = new Vector3(0, -20, 20);
        headKeyframes.Insert(0, kf);

        kf = new HeadKeyFrame();
        kf.start = Time.time + 2;
        kf.end = kf.start + 0.5f;
        kf.angle = Quaternion.Euler(-20, 0, 20);
        kf.eulerAngle = new Vector3(0, -20, -20);
        headKeyframes.Insert(0, kf);

        kf = new HeadKeyFrame();
        kf.start = Time.time + 2.5f;
        kf.end = kf.start + 0.5f;
        kf.angle = Quaternion.identity;
        kf.eulerAngle = new Vector3(0, 0, 0);
        headKeyframes.Insert(0, kf);

    }

    public void gesture()
    {
        //writeAngles();
        KFrame kf = new KFrame();
        kf.start = Time.time;
        kf.end = kf.start + 2;
        kf.position = transform.InverseTransformPoint(findChild_(transform, "RightHand").position);
        kf.relative_position = null;
        leftArmKeyframes.Insert(0, kf);

        kf = new KFrame();
        kf.start = Time.time + 2.5f;
        kf.end = kf.start + 4;
        kf.relative_position = new Position(-5, 2, 1);
        kf.position = new Vector3(0, 0, 0);
        leftArmKeyframes.Insert(0, kf);

        /*kf = new KFrame();
        kf.start = Time.time + 4.5f;
        kf.end = kf.start + 6;
        kf.position = new Vector3(0, 0, 0);
        kf.relative_position = new Position(-4, 4, 2);
        leftArmKeyframes.Insert(0, kf);

        kf = new KFrame();
        kf.start = Time.time + 6.5f;
        kf.end = kf.start + 8;
        kf.position = new Vector3(0, 0, 0);
        kf.relative_position = new Position(-5, 6, 1);
        leftArmKeyframes.Insert(0, kf);

        /*kf = new KFrame();
        kf.start = Time.time + 9;
        kf.end = kf.start + 3;
        kf.relative_position = null;
        kf.position = transform.InverseTransformPoint(findChild_(transform, "LeftHand").position);
        leftArmKeyframes.Insert(0, kf);*/

    }

    public void torso()
    {
        //writeAngles();
        KFrame kf = new KFrame();
        kf.start = Time.time;
        kf.end = kf.start + 3;
        kf.position = transform.InverseTransformPoint(findChild_(transform, "LeftArm").position);
        kf.relative_position = null;
        torsoKeyframes.Insert(0, kf);

        kf = new KFrame();
        kf.start = Time.time + 3;
        kf.end = kf.start + 3;
        kf.relative_position = null;
        kf.position = left_shoulder;
        torsoKeyframes.Insert(0, kf);

        kf = new KFrame();
        kf.start = Time.time + 6;
        kf.end = kf.start + 3;
        kf.relative_position = null;
        kf.position = left_shoulder + new Vector3(0,0, 0.15f);
        torsoKeyframes.Insert(0, kf);
    }

    private float RotationOnAxis(int axis, Quaternion rot)
    {
        rot.x /= rot.w;
        rot.y /= rot.w;
        rot.z /= rot.w;
        rot.w = 1;

        return 2.0f * Mathf.Rad2Deg * Mathf.Atan(rot[axis]);
    }

    public void LateUpdate()
    {

        float time = Time.time;

        /*
        fbbik.solver.leftShoulderEffector.positionWeight = 1;
        fbbik.solver.rightShoulderEffector.positionWeight = 1;*/
        if (Input.GetKeyDown(KeyCode.W))
        {
            /*fbbik.solver.leftShoulderEffector.position = transform.TransformPoint(left_shoulder);
            fbbik.solver.rightShoulderEffector.position = transform.TransformPoint(right_shoulder);
            fbbik.solver.Update();*/
            fbbik.solver.leftHandEffector.positionWeight = 1;
            fbbik.solver.rightHandEffector.positionWeight = 1;
            fbbik.solver.leftHandEffector.position = GetAbsoluteWristPosition(1, 5, 2);
            //fbbik.solver.rightHandEffector.position = GetAbsoluteWristPosition(-3, 5, 2);
            fbbik.solver.Update();

        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            //fbbik.solver.leftShoulderEffector.position = transform.TransformPoint(left_shoulder);
           // fbbik.solver.rightShoulderEffector.position = transform.TransformPoint(right_shoulder);
            fbbik.solver.Update();
            fbbik.solver.leftHandEffector.position = GetAbsoluteWristPosition(4, 3, 2);
            fbbik.solver.rightHandEffector.position = GetAbsoluteWristPosition(-4, 3, 2);
            fbbik.solver.Update();

        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            recomputePositions = true;
            fbbik.solver.leftHandEffector.positionWeight = 0;
            fbbik.solver.leftArmChain.bendConstraint.weight = 0;
            fbbik.solver.rightArmChain.bendConstraint.weight = 0;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            gaze();
            head();
            //torso();
            gesture();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            //start = Time.time;
            leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position + (leftArmSwivelAttractor.transform.position - findChild_(transform, "LeftForeArm").position).normalized * 0.1f;
            rightArmSwivelAttractor.transform.position = findChild_(transform, "RightForeArm").position + (rightArmSwivelAttractor.transform.position - findChild_(transform, "RightForeArm").position).normalized * 0.1f;
            leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, 10);
            rightArmSwivelAttractor.transform.RotateAround(findChild_(transform, "RightForeArm").position, findChild_(transform, "RightArm").right, 10);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, -10);
            rightArmSwivelAttractor.transform.RotateAround(findChild_(transform, "RightForeArm").position, findChild_(transform, "RightArm").right, -10);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            leftArmSwivelAttractor.transform.position += new Vector3(0, 0.02f, 0);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            leftArmSwivelAttractor.transform.position -= new Vector3(0, 0.02f, 0);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            leftArmSwivelAttractor.transform.position += new Vector3(0, 0, 0.02f);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            leftArmSwivelAttractor.transform.position -= new Vector3(0, 0, 0.02f);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            leftArmSwivelAttractor.transform.position += new Vector3(0.02f, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            leftArmSwivelAttractor.transform.position -= new Vector3(0.02f, 0, 0);
        }

        if (keyframesgaze.Count > 0)
        {
            int i = keyframesgaze.Count - 1;
            KeyFrameGaze k = keyframesgaze[i];
            animGaze = true;
            double lerpTime = (time - k.time) / (k.endTime - k.time);
            if (k.target_from == null)
            {
                lookAt.solver.target = k.target_to.transform;
                lookAt.solver.SetIKPositionWeight((float)lerpTime);
            }
            if (k.target_to == null)
            {
                lookAt.solver.target = k.target_from.transform;
                lookAt.solver.SetIKPositionWeight(1 - (float)lerpTime);
            }
            if (k.target_to != null && k.target_from != null)
            {
                lookAt.solver.target = k.target_from.transform;
                lookAt.solver.SetIKPositionWeight(1);
            }
            lookAt.solver.Update();
            
            if (time > k.endTime)
                keyframesgaze.RemoveAt(i);
        }
        else if (animGaze == true)
        {
            lookAt.solver.SetIKPositionWeight(0);
            animGaze = false;
        }

        if (headKeyframes.Count > 1)
        {
            int i = headKeyframes.Count - 1;
            HeadKeyFrame k = headKeyframes[i];
            HeadKeyFrame k1 = headKeyframes[i - 1];

            double lerpTime = (time - k.start) / (k1.start - k.start);
            //head_transform.rotation *= Quaternion.Slerp(k.angle, k1.angle, (float)lerpTime);
            head_transform.rotation *= Quaternion.Slerp(Quaternion.Euler(0, 0, k.eulerAngle.z), Quaternion.Euler(0, 0, k1.eulerAngle.z), (float)lerpTime);
            head_transform.rotation *= Quaternion.Slerp(Quaternion.Euler(k.eulerAngle.x, 0, 0), Quaternion.Euler(k1.eulerAngle.x, 0,0), (float)lerpTime);
            head_transform.rotation *= Quaternion.Slerp(Quaternion.Euler(0, k.eulerAngle.y, 0), Quaternion.Euler(0, k1.eulerAngle.y, 0), (float)lerpTime);
            

            if (time > k.end)
                headKeyframes.RemoveAt(i);
        }
        else if (headKeyframes.Count == 1)
        {
            HeadKeyFrame k = headKeyframes[0];

            double lerpTime = (time - k.start) / (k.end - k.start);
            head_transform.rotation *= Quaternion.Slerp(k.angle, Quaternion.identity, (float)lerpTime);

            if (time > k.end)
                headKeyframes.RemoveAt(0);
        }

        if (torsoKeyframes.Count > 0)
        {
            int i = torsoKeyframes.Count - 1;
            double endTime = -1;
            animTorso = true;

            if (i >= 1)
            {
                //double lerpTime = (time - frames[i].startTime) / (frames[i].endTime - frames[i].startTime);
                double lerpTime = (time - torsoKeyframes[i].start) / (torsoKeyframes[i - 1].start - torsoKeyframes[i].start);
                endTime = torsoKeyframes[i - 1].start;
                fbbik.solver.leftShoulderEffector.positionWeight = 1f;
                fbbik.solver.leftShoulderEffector.position = Vector3.Lerp(transform.TransformPoint(torsoKeyframes[i].position), transform.TransformPoint(torsoKeyframes[i - 1].position), (float)lerpTime);
            }
            else
            {
                double lerpTime = 1.0f - ((time - torsoKeyframes[i].start) / (torsoKeyframes[i].end - torsoKeyframes[i].start));
                endTime = torsoKeyframes[i].end;
                fbbik.solver.leftShoulderEffector.position = transform.TransformPoint(torsoKeyframes[i].position);
                fbbik.solver.leftShoulderEffector.positionWeight = (float)lerpTime;
            }

            fbbik.solver.Update();
            init_spine = findChild_(transform, "Spine2");

            if (time > endTime)
            {
                torsoKeyframes.RemoveAt(i);
            }
        }
        else if (animTorso == true)
        {
            fbbik.solver.leftShoulderEffector.positionWeight = 0;
            fbbik.solver.Update();
            animTorso = false;
        }

        IKEffector handEffector = fbbik.solver.rightHandEffector;
        if (leftArmKeyframes.Count > 0)
        {
            int i = leftArmKeyframes.Count - 1;
            double endTime = -1;
            animGesture = true;

            if (i >= 1)
            {
                //double lerpTime = (time - frames[i].startTime) / (frames[i].endTime - frames[i].startTime);
                double lerpTime = (time - leftArmKeyframes[i].start) / (leftArmKeyframes[i - 1].start - leftArmKeyframes[i].start);
                endTime = leftArmKeyframes[i - 1].start;
                handEffector.positionWeight = 1f;
                Vector3 from, to;
                if (leftArmKeyframes[i].relative_position == null)
                    from = transform.TransformPoint(leftArmKeyframes[i].position);
                else
                    from = GetAbsoluteWristPosition(leftArmKeyframes[i].relative_position.r_, leftArmKeyframes[i].relative_position.h_, leftArmKeyframes[i].relative_position.d_);

                if (leftArmKeyframes[i-1].relative_position == null)
                    to = transform.TransformPoint(leftArmKeyframes[i-1].position);
                else
                    to = GetAbsoluteWristPosition(leftArmKeyframes[i-1].relative_position.r_, leftArmKeyframes[i-1].relative_position.h_, leftArmKeyframes[i-1].relative_position.d_);

                handEffector.position = Vector3.Lerp(from, to, (float)lerpTime);
            }
            else
            {
                double lerpTime = 1.0f - ((time - leftArmKeyframes[i].start) / (leftArmKeyframes[i].end - leftArmKeyframes[i].start));
                Vector3 from;
                if (leftArmKeyframes[i].relative_position == null)
                    from = transform.TransformPoint(leftArmKeyframes[i].position);
                else
                    from = GetAbsoluteWristPosition(leftArmKeyframes[i].relative_position.r_, leftArmKeyframes[i].relative_position.h_, leftArmKeyframes[i].relative_position.d_);
                handEffector.position = from;
                endTime = leftArmKeyframes[i].end;
                handEffector.positionWeight = (float)lerpTime;
            }

            fbbik.solver.Update();

            if (time > endTime)
            {
                leftArmKeyframes.RemoveAt(i);
            }
        }
        else if(animGesture == true)
        {
            handEffector.positionWeight = 0;
            fbbik.solver.Update();
            animGesture = false;
        }

    }

    /*
    private void LateUpdate()
    {
        if (keyframesleft.Count > 0)
        {
            int i = keyframesleft.Count - 1;
            double endTime = -1;
            float time = Time.time;

            if (i >= 1)
            {
                //double lerpTime = (time - frames[i].startTime) / (frames[i].endTime - frames[i].startTime);
                double lerpTime = (time - keyframesleft[i].start) / (keyframesleft[i - 1].start - keyframesleft[i].start);
                endTime = keyframesleft[i - 1].start;
                if (keyframesleft[i].armSwivel == 0 && keyframesleft[i - 1].armSwivel != 0)
                {
                    fbbik.solver.leftArmChain.bendConstraint.weight = (float)lerpTime;
                    leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position + (leftArmSwivelAttractor.transform.position - findChild_(transform, "LeftForeArm").position).normalized * 0.1f;
                    leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, keyframesleft[i - 1].armSwivel * Time.deltaTime);

                }
                else
                {
                    leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position + (leftArmSwivelAttractor.transform.position - findChild_(transform, "LeftForeArm").position).normalized * 0.1f;
                    //transform.RotateAround(target.position, Vector3.up, orbitDegreesPerSec * Time.deltaTime);
                    leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, keyframesleft[i - 1].armSwivel * Time.deltaTime);
                    fbbik.solver.leftArmChain.bendConstraint.weight = 1;
                }
                Debug.Log(i + "   " + keyframesleft[i - 1].armSwivel * Time.deltaTime + " " + testangle);
                testangle += keyframesleft[i - 1].armSwivel * Time.deltaTime;
            }
            else
            {
                double lerpTime = 1.0f - ((time - keyframesleft[i].start) / (keyframesleft[i].end - keyframesleft[i].start));
                endTime = keyframesleft[i].end;
                fbbik.solver.leftArmChain.bendConstraint.weight = (float)lerpTime;
            }
        }

    }*/
}
