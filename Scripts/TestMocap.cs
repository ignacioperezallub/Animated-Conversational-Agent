using UnityEngine;
using RootMotion.FinalIK;
using System.Collections.Generic;
using System;
using System.Collections;

class Position
{
    public int r_, h_, d_;
    public Position(int r, int h, int d)
    {
        r_ = r; h_ = h; d_ = d;
    }
}

struct CharacterDimensions
{
    //distances
    public float spine2ToSpine;
    public float spine2ToBelt;
    public float spine2ToBelowBelt;
    public float spine2ToChest;
    public float spine2ToUpperLegs;
    public float spine2ToNeck;
    public float armLength;
    public float spine2ToShoulder;
    public float spine2ToHead;
    public float spine2ToAboveHead;
    public float spine2ToAbdomen;
    public float clavicleLength;
    public float touch;
}


class KFrame
{
    internal Vector3 position;
    internal Position relative_position;
    internal float armSwivel;
    internal Quaternion to;
    internal float angleto;
    internal Quaternion form;
    internal float start;
    internal float end;
}

public class TestMocap : MonoBehaviour
{
    // test mocap
    protected Animator animator;
    protected AnimatorOverrideController animatorOverrideController;
    private FullBodyBipedIK fbbik;

    //test wrist rotation
    internal Transform leftWrist;
    internal Transform rightWrist;
    internal Quaternion fromLeft, fromRight, fromLeftBone, toLeft;
    internal float start = 1000, end = 0;
    internal float xl = 0, zl = 0, yl = 0, toLeftX = 0, toLeftY = 0, toLeftZ = 0;
    internal float xr = 0, zr = 0, yr = 0, toRightX = 0, toRightY = 0, toRightZ = 0;
    internal List<KFrame> keyframesleft = new List<KFrame>();

    //test symbolic positions
    public GameObject left;
    internal Transform spine;
    internal float spine_shoulder;
    internal float head_spine;
    internal float depth;
    internal float armLength;
    internal Dictionary<Vector3, Vector3> symbolicPositions;
    internal bool recomputePositions = true;

    internal CharacterDimensions characterDimensions;

    public GameObject leftArmSwivelAttractor, rightArmSwivelAttractor;

    internal Transform leftElbow;
    internal GameObject initialPose;
    internal Transform initialTransform;
    private float angle = 0;
//    private float limits = 70;
//    private Vector3 spine2Rotation;

    internal int r = 1, h = -1, d = -1;

    //Transform initialSpine2;


    void Start()
    {
        animator = GetComponent<Animator>();
        animatorOverrideController = new AnimatorOverrideController();
        animatorOverrideController.runtimeAnimatorController = animator.runtimeAnimatorController;
        fbbik = GetComponent<FullBodyBipedIK>();
        leftWrist = findChild_(transform, "LeftHand");
        rightWrist = findChild_(transform, "RightHand");

        spine = findChild_(transform, "Spine");
        Transform headEnd = findChild_(transform, "HeadEnd");
        Transform leftShoulder = findChild_(transform, "LeftShoulder");
        head_spine = -Vector3.Distance(spine.position, headEnd.position) / transform.localScale.x;
        spine_shoulder = -Vector3.Distance(spine.position, leftShoulder.position) / transform.localScale.x;

        Transform leftHand = findChild_(transform, "LeftHand");
        Transform leftArm = findChild_(transform, "LeftArm");
        Transform leftForeArm = findChild_(transform, "LeftForeArm");
        armLength = (Vector3.Distance(leftShoulder.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftArm.position) + Vector3.Distance(leftForeArm.position, leftHand.position)) / transform.localScale.x;

        Transform neck = findChild_(transform, "Neck");
        depth = -Vector3.Distance(headEnd.position, neck.position) / transform.localScale.x;

        
        left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //left.transform.SetParent(findChild_(transform, "Spine2"));
        left.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        left.name = "left";
        left.GetComponent<Renderer>().material.color = new Color(0, 1, 1, 1);
        left.SetActive(true);

        leftArmSwivelAttractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftArmSwivelAttractor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        //leftArmSwivelAttractor.transform.SetParent(leftForeArm);
        leftArmSwivelAttractor.name = "leftswivel";
        leftArmSwivelAttractor.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
        leftArmSwivelAttractor.SetActive(true);
        transform.GetComponent<FullBodyBipedIK>().solver.leftArmChain.bendConstraint.bendGoal = leftArmSwivelAttractor.transform;

        rightArmSwivelAttractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightArmSwivelAttractor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        rightArmSwivelAttractor.name = "rightswivel";
        rightArmSwivelAttractor.GetComponent<Renderer>().material.color = new Color(1, 1, 0, 1);
        rightArmSwivelAttractor.SetActive(true);
        transform.GetComponent<FullBodyBipedIK>().solver.rightArmChain.bendConstraint.bendGoal = rightArmSwivelAttractor.transform;

        leftElbow = findChild_(transform, "LeftForeArm");
        initialPose = Instantiate<GameObject>(transform.gameObject);
        initialPose.SetActive(false);
        initialTransform = Instantiate<Transform>(findChild_(transform, "Spine2"));
        //initialTransform.position = transform.InverseTransformPoint(initialTransform.position);
        left.transform.position = initialTransform.position;

        SetCharacterDimensions(ref characterDimensions);
    }

    private void SetCharacterDimensions(ref CharacterDimensions cd)
    {
        Transform spine = findChild_(transform, "Spine");
        Transform spine2 = findChild_(transform, "Spine2");
        Transform spine1 = findChild_(transform, "Spine1");
        Transform hips = findChild_(transform, "Hips");
        Transform neck = findChild_(transform, "Neck");
        Transform head = findChild_(transform, "Head");
        Transform headEnd = findChild_(transform, "HeadEnd");
        Transform leftUpLeg = findChild_(transform, "LeftUpLeg");
        Transform rightUpLeg = findChild_(transform, "RightUpLeg");
        Transform shoulder = findChild_(transform, "LeftShoulder");
        Transform foreArm = findChild_(transform, "LeftForeArm");
        Transform arm = findChild_(transform, "LeftArm");
        Transform hand = findChild_(transform, "LeftHand");
        Transform eye = findChild_(transform, "LeftEye");
        cd.spine2ToSpine = Vector3.Distance(spine.position, spine2.position) / transform.localScale.x;
        cd.spine2ToAbdomen = cd.spine2ToSpine * 0.5f;
        cd.spine2ToBelt = Vector3.Distance(spine.position, spine2.position) / transform.localScale.x;
        cd.spine2ToBelowBelt = (Vector3.Distance(hips.position, spine2.position) + Vector3.Distance(hips.position, spine.position)) / transform.localScale.x;
        cd.spine2ToUpperLegs = Mathf.Min(Vector3.Distance(spine2.position, leftUpLeg.position), Vector3.Distance(spine2.position, rightUpLeg.position)) / transform.localScale.x;
        cd.spine2ToNeck = Vector3.Distance(neck.position, spine2.position) / transform.localScale.x;
        cd.spine2ToChest = cd.spine2ToNeck * 0.25f;
        cd.armLength= (Vector3.Distance(shoulder.position, arm.position) + Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position)) / transform.localScale.x;
        cd.spine2ToShoulder = Vector3.Distance(shoulder.position, spine2.position) / transform.localScale.x;
        cd.spine2ToHead = Vector3.Distance(eye.position, spine2.position) / transform.localScale.x;
        cd.spine2ToAboveHead = Vector3.Distance(headEnd.position, spine2.position) / transform.localScale.x;
        cd.clavicleLength = Vector3.Distance(shoulder.position, arm.position);
        //cd.touch = (Vector3.Distance(spine.position, spine1.position) + Vector3.Distance(spine2.position, spine1.position)) / transform.localScale.x;
        cd.touch = Vector3.Distance(hips.position, spine1.position) / transform.localScale.x;
    }


    Vector3 GetAbsoluteSwivelPosition()
    {

        return new Vector3();
    }

    Vector3 GetAbsolutePosition(int radialOrientation, int heigh, int distance, float ru = 0, float hu = 0, float du = 0)
    {        
        float y = 0, x = 0, z_touch_chest = -characterDimensions.spine2ToSpine;
        float ax = 0, ay = 0, az = 0;
        float fromShoulder = 0;
        switch (heigh)
        {
            case 0: //above_head
                y = -characterDimensions.spine2ToAboveHead + characterDimensions.spine2ToBelt * hu;
                fromShoulder = -y - characterDimensions.spine2ToShoulder;
                ay = 0.2f;
                break;
            case 1: //head
                y = -characterDimensions.spine2ToHead + Math.Abs(characterDimensions.spine2ToAboveHead - characterDimensions.spine2ToHead) * hu;
                fromShoulder = -y - characterDimensions.spine2ToShoulder;
                ay = 0.6f;
                break;
            case 2: //shoulder
                y = -characterDimensions.spine2ToShoulder + Math.Abs(characterDimensions.spine2ToHead - characterDimensions.spine2ToShoulder) * hu;
                fromShoulder = -y - characterDimensions.spine2ToShoulder;
                ay = 0.8f;
                break;
            case 3: //chest
                y = -characterDimensions.spine2ToChest + Math.Abs(characterDimensions.spine2ToShoulder - characterDimensions.spine2ToChest) * hu;
                fromShoulder = y + characterDimensions.spine2ToShoulder;
                ay = 0.6f;
                break;
            case 4: //abdoment
                y = characterDimensions.spine2ToAbdomen + Math.Abs(characterDimensions.spine2ToChest - characterDimensions.spine2ToAbdomen) * hu;
                fromShoulder = y + characterDimensions.spine2ToShoulder;
                ay = 0.4f;
                break;
            case 5: //belt
                y = characterDimensions.spine2ToBelt + Math.Abs(characterDimensions.spine2ToAbdomen - characterDimensions.spine2ToBelt) * hu;
                fromShoulder = y + characterDimensions.spine2ToShoulder;
                ay = 0.2f;
                break;
            case 6: //below_belt
                y = characterDimensions.spine2ToBelowBelt + Math.Abs(characterDimensions.spine2ToBelt - characterDimensions.spine2ToBelowBelt) * hu;
                fromShoulder = y + characterDimensions.spine2ToShoulder;
                ay = 0f;
                break;
            default:
                y = 0; break;
        }

        //Debug.Log("fromShoulder " + fromShoulder + " armLength " + armLength);

        if (fromShoulder > armLength)
            fromShoulder = armLength;
        x = Mathf.Sqrt(armLength * armLength - fromShoulder * fromShoulder);
        //Debug.Log("x " + x);
        switch (radialOrientation)
        {
            case 1: //left far_out
                x *= 1; ax = 1; break;
            case 2: //left out
                x *= 0.75f; ax = 1; break;
            case 3: //left side
                x *= 0.4f;  ax = 1; break;
            case 4: //left front
                x *= 0.15f; ax = 1;  break;
            case 5: //left inward
                x *= -0.2f;
                ax = 0.5f * ay;
                break;
            case -1: //right far_out
                x *= -1; ax = 1; break;
            case -2: //right out
                x *= -0.75f; ax = 1; break;
            case -3: //right side
                x = -0.4f; ax = 1; break;
            case -4: //right front
                x = -0.15f; ax = 1; break;
            case -5: //right inward
                x *= 0.2f;
                ax = 0.5f * ay;
                break;
            default:
                x = 0; break;
        }

        //depth = head 
        
        float z = -Mathf.Sqrt(armLength*0.8f * armLength * 0.8f - fromShoulder * fromShoulder);
        switch(distance)
        {
            case 3: //touch
                z *= 0.3f;
                az = 1 * ay;
                break;
            case 2: //close
                z *= 0.55f;
                az = 0.75f * ay;
                break;
            case 1: //normal
                z *= 0.8f;
                az = 0.5f * ay;
                break;
            case 0: //far
                z *= 1;
                az = 0; break;
            default:
                z = 0.2f; break;
        }
        Transform init_spine =  findChild_(initialPose.transform, "Spine2");
        Vector3 init_sp = init_spine.TransformPoint(Vector3.up * x + Vector3.right * y + Vector3.forward * z); // +Vector3.up*(-0.2f));  //right=down, forward=behind, up=left
        Transform spine = findChild_(transform, "Spine2");
        Vector3 sp = spine.TransformPoint(Vector3.up * x + Vector3.right * y + Vector3.forward * z); // +Vector3.up*(-0.2f));  //right=down, forward=behind, up=left
        float bx = 1f - ax;
        float by = 1f - ay;
        float bz = 1f - az;
        return new Vector3((init_sp.x * bx + sp.x * ax), (init_sp.y * by + sp.y * ay), init_sp.z * bz + sp.z * az);
    }

    public void PlayAnimation(AnimationClip clip)
    {
        Debug.Log("in play animation " + clip.name);
        animatorOverrideController["anim1"] = clip;
        animator.runtimeAnimatorController = animatorOverrideController;
        animator.ResetTrigger("idle");
        animator.SetTrigger("playAnim");
    }

    public void ChangeStance(AnimationClip clip)
    {
        string idle = "", resetTrigger = "", setTrigger = "";

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle2"))
        {
            idle = "idle1";
            resetTrigger = "idle1idle2";
            setTrigger = "idle2idle1";
        }
        else
        {
            idle = "idle2";
            resetTrigger = "idle2idle1";
            setTrigger = "idle1idle2";
        }

        animatorOverrideController[idle] = clip;
        animator.runtimeAnimatorController = animatorOverrideController;
        animator.ResetTrigger(resetTrigger);
        animator.SetTrigger(setTrigger);
    }

    public void StopAnimation()
    {
        animator.ResetTrigger("playAnim");
        animator.SetTrigger("idle");
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

    private float RotationOnAxis(int axis, Quaternion rot)
    {
        rot.x /= rot.w;
        rot.y /= rot.w;
        rot.z /= rot.w;
        rot.w = 1;

        return 2.0f * Mathf.Rad2Deg * Mathf.Atan(rot[axis]);
    }

    public void Update()
    {
        if (Time.time > 0.3 && recomputePositions == true)
        {
            GameObject.Destroy(initialPose);
            initialPose = Instantiate<GameObject>(transform.gameObject);
            initialPose.SetActive(false);

            SetCharacterDimensions(ref characterDimensions);
            recomputePositions = false;
            Vector3 targetDir = findChild_(transform, "LeftArm").position - findChild_(initialPose.transform, "LeftArm").position;
            Quaternion rot = Quaternion.FromToRotation(findChild_(transform, "LeftArm").forward, findChild_(initialPose.transform, "LeftArm").forward);
            Vector3 axis = Vector3.right;
            rot.ToAngleAxis(out angle, out axis);
            //angle = Vector3.Angle(transform.right, targetDir);
            angle = RotationOnAxis(0, findChild_(transform, "LeftArm").localRotation * Quaternion.Inverse(findChild_(initialPose.transform, "LeftArm").localRotation));
            //Debug.Log("0 --- " + angle);
            

            //Debug.Log("2 " + findChild_(initialPose.transform, "LeftArm").localRotation.eulerAngles + "    " + findChild_(transform, "LeftArm").localRotation.eulerAngles + "   " + angle);
            //Debug.Log("3 " + findChild_(initialPose.transform, "RightArm").localRotation.eulerAngles + "    " + findChild_(transform, "RightArm").localRotation.eulerAngles + "   " + angle);

            targetDir = findChild_(transform, "Spine2").localPosition - findChild_(initialPose.transform, "Spine2").localPosition;
            //spine2Rotation = new Vector3(Vector3.Angle(transform.right, targetDir), Vector3.Angle(transform.up, targetDir), Vector3.Angle(transform.forward, targetDir));
        }


        /*Vector3 targetDir = findChild_(transform, "Spine2").localPosition - findChild_(initialPose.transform, "Spine2").localPosition;
        spine2Rotation = new Vector3(Vector3.Angle(transform.right, targetDir), Vector3.Angle(transform.up, targetDir), Vector3.Angle(transform.forward, targetDir));

        GameObject c = new GameObject();
        c.transform.position = Vector3.Lerp(keyframesleft[i].position, keyframesleft[i - 1].position, (float)lerpTime);
        c.transform.RotateAround(findChild_(transform, "Spine2").position, transform.right, 10);*/

        /*if (Input.GetKeyDown(KeyCode.T))
        {
            //writeAngles();
            KFrame kf = new KFrame();
            kf.start = Time.time;
            kf.end = kf.start + 1;
            kf.form = findChild_(transform, "LeftHand").localRotation;
            Debug.Log(kf.form);
            kf.to = Quaternion.Euler(0, 0, 45);// * Quaternion.Inverse(kf.form);


            keyframesleft.Insert(0, kf);

            kf = new KFrame();
            kf.start = Time.time + 1;
            kf.end = kf.start + 1;
            kf.form = findChild_(transform, "LeftHand").rotation * Quaternion.Euler(0, 0, 45) * Quaternion.Inverse(fromLeft);
            kf.to = Quaternion.Euler(45, -45, 0); // * Quaternion.Inverse(kf.form);
            keyframesleft.Insert(0, kf);

            fbbik.solver.leftHandEffector.rotation = findChild_(transform, "LeftHand").localRotation;
            fbbik.solver.leftHandEffector.rotationWeight = 1;
        }


        int i = keyframesleft.Count - 1;

        float time = Time.time;
        float endTime = -1;

        if (i < 0) return;

        if (time >= keyframesleft[i].start && time <= keyframesleft[i].end)
        {
            endTime = keyframesleft[i].end;
            //float angleLeftX = keyframesleft[i].angle.x / (keyframesleft[i].end - keyframesleft[i].start) * Time.deltaTime;
            //Debug.Log(i + " " + angleLeftX);
            fbbik.solver.leftHandEffector.rotation = Quaternion.Lerp(fbbik.solver.leftHandEffector.bone.rotation, fbbik.solver.leftHandEffector.bone.rotation * keyframesleft[i].to, Time.deltaTime);
        }
        if (i == 0 && time > keyframesleft[i].end && time <= keyframesleft[i].end + 0.25f)
        {
            endTime = keyframesleft[i].end + 0.25f;
            float lerpTime = 1 - (time - keyframesleft[i].end) / 0.25f;
            fbbik.solver.leftHandEffector.rotationWeight = lerpTime;
            //fbbik.solver.rightHandEffector.rotationWeight = lerpTime;
        }
        if (time > endTime)
        {
            keyframesleft.RemoveAt(i);
        }*/
        /*
        if (time >= start && time <= start + end)
        {
            float angleLeftZ = (toLeftZ/end) * Time.deltaTime;
            float angleLeftX = (toLeftX / end) * Time.deltaTime;
            float angleLeftY = (toLeftY / end) * Time.deltaTime;
            //fbbik.solver.leftHandEffector.rotation = fbbik.solver.leftHandEffector.bone.rotation * Quaternion.AngleAxis(angleLeftX, Vector3.up);// * Quaternion.AngleAxis(angleLeftY, Vector3.right) * Quaternion.AngleAxis(angleLeftZ, Vector3.forward);
            //fbbik.solver.leftHandEffector.rotation = fbbik.solver.leftHandEffector.bone.rotation * Quaternion.AngleAxis(angleLeftZ, Vector3.forward);
            fbbik.solver.leftHandEffector.rotation = Quaternion.Lerp(fbbik.solver.leftHandEffector.bone.rotation, fbbik.solver.leftHandEffector.bone.rotation * toLeft, Time.deltaTime);

            float angleRightZ = (toRightZ / end) * Time.deltaTime;
            float angleRightX = (toRightX / end) * Time.deltaTime;
            float angleRightY = (toRightY / end) * Time.deltaTime;
            //fbbik.solver.rightHandEffector.rotation = fbbik.solver.rightHandEffector.bone.rotation * Quaternion.AngleAxis(angleRightX, Vector3.up); // * Quaternion.AngleAxis(angleRightY, Vector3.right) * Quaternion.AngleAxis(angleRightZ, Vector3.forward);
            //fbbik.solver.rightHandEffector.rotation = fbbik.solver.rightHandEffector.bone.rotation * Quaternion.AngleAxis(angleRightZ, Vector3.forward);

        }
        if (time > start + end && time <= start+end+0.25f)
        {
            float lerpTime = 1 - (time - (start + end)) / 0.25f;
            fbbik.solver.leftHandEffector.rotationWeight = lerpTime;
            fbbik.solver.rightHandEffector.rotationWeight = lerpTime;
        }
        if (time > start + end + 0.25f)
        {
            fbbik.solver.leftHandEffector.rotationWeight = 0;
            fbbik.solver.rightHandEffector.rotationWeight = 0;
        }**/


        fbbik.solver.leftHandEffector.positionWeight = 1;
        fbbik.solver.rightHandEffector.positionWeight = 0;
        if (Input.GetKeyDown(KeyCode.W))
        {
            r = (r + 1) % 6;
            Debug.Log("r: " + r);
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            h = (h + 1) % 7;
            Debug.Log("h: " + h);
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            d = (d + 1) % 4;
            Debug.Log("d: " + d);
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            r = 1; h = 0; d = 0;
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            fbbik.solver.leftHandEffector.position = GetAbsolutePosition(r, h, d);
            fbbik.solver.rightHandEffector.position = GetAbsolutePosition(-r, h, d);
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
            Vector3 directionleft = findChild_(transform, "LeftArm").forward;
            Vector3 directionright = findChild_(transform, "RightArm").forward;
            //direction.y = 0;
            //direction = direction.normalized;
            //transform.position = target.position + distance * direction;
            leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position - 0.1f * transform.localScale.z * directionleft;
            leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftArm").position, findChild_(transform, "LeftArm").right, -angle);
            rightArmSwivelAttractor.transform.position = findChild_(transform, "RightForeArm").position + 0.1f * transform.localScale.z * directionright;
            rightArmSwivelAttractor.transform.RotateAround(findChild_(transform, "RightArm").position, findChild_(transform, "RightArm").right, -angle);
            fbbik.solver.leftArmChain.bendConstraint.weight = 1;
            fbbik.solver.rightArmChain.bendConstraint.weight = 1;

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

        if(Time.time >= start && Time.time < start + 3)
        {
            leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, -0.5f);
            rightArmSwivelAttractor.transform.RotateAround(findChild_(transform, "RightForeArm").position, findChild_(transform, "RightArm").right, -0.5f);
        }
        if (Time.time >= start+3 && Time.time < start + 10)
        {
            leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, 0.5f);
            rightArmSwivelAttractor.transform.RotateAround(findChild_(transform, "RightForeArm").position, findChild_(transform, "RightArm").right, 0.5f);
        }


        if (Input.GetKeyDown(KeyCode.Y))
        {
            //writeAngles();
            KFrame kf = new KFrame();
            kf.start = Time.time;
            kf.end = kf.start + 3;
            kf.position = findChild_(transform, "LeftHand").position;
            kf.relative_position = null;
            Vector3 directionleft = findChild_(transform, "LeftArm").forward;
            kf.armSwivel = 0;
            kf.form = findChild_(transform, "LeftHand").localRotation;
            kf.to = Quaternion.Euler(0, 0, 45);// * Quaternion.Inverse(kf.form);
            leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position - 0.1f * transform.localScale.z * findChild_(transform, "LeftArm").forward;
            leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftArm").position, findChild_(transform, "LeftArm").right, -angle);
            rightArmSwivelAttractor.transform.position = findChild_(transform, "RightForeArm").position + 0.1f * transform.localScale.z * findChild_(transform, "RightArm").forward;
            rightArmSwivelAttractor.transform.RotateAround(findChild_(transform, "RightArm").position, findChild_(transform, "RightArm").right, -angle);


            keyframesleft.Insert(0, kf);

            kf = new KFrame();
            kf.start = Time.time + 3;
            kf.end = kf.start + 3;
            kf.relative_position = new Position(1, 2, 2);
            kf.position = new Vector3(0, 0, 0);
            kf.armSwivel = -50;
            kf.form = findChild_(transform, "LeftHand").rotation * Quaternion.Euler(0, 0, 45) * Quaternion.Inverse(fromLeft);
            kf.to = Quaternion.Euler(45, -45, 0); // * Quaternion.Inverse(kf.form);
            keyframesleft.Insert(0, kf);

            kf = new KFrame();
            kf.start = Time.time + 6;
            kf.end = kf.start + 3;
            kf.position = new Vector3(0, 0, 0);
            kf.relative_position = new Position(3, 4, 2); // GetAbsolutePosition(3, 2, 2);//symbolicPositions[new Vector3(3, 2, 2)];
            kf.armSwivel = 10;
            kf.form = findChild_(transform, "LeftHand").rotation * Quaternion.Euler(0, 0, 45) * Quaternion.Inverse(fromLeft);
            kf.to = Quaternion.Euler(45, -45, 0); // * Quaternion.Inverse(kf.form);
            keyframesleft.Insert(0, kf);

            //fbbik.solver.leftHandEffector.rotation = findChild_(transform, "LeftHand").localRotation;
            //fbbik.solver.leftHandEffector.rotationWeight = 1;
           
        }


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
                fbbik.solver.leftHandEffector.positionWeight = 1f;
                Vector3 from, to;
                if (keyframesleft[i].relative_position == null)
                    from = keyframesleft[i].position;
                else
                    from = GetAbsolutePosition((int)keyframesleft[i].relative_position.r_, (int)keyframesleft[i].relative_position.h_, (int)keyframesleft[i].relative_position.d_);

                if (keyframesleft[i-1].relative_position == null)
                    to = keyframesleft[i-1].position;
                else
                    to = GetAbsolutePosition((int)keyframesleft[i-1].relative_position.r_, (int)keyframesleft[i-1].relative_position.h_, (int)keyframesleft[i-1].relative_position.d_);

                fbbik.solver.leftHandEffector.position = Vector3.Lerp(from, to, (float)lerpTime);

                if (keyframesleft[i].armSwivel == 0 && keyframesleft[i - 1].armSwivel != 0)
                {
                    fbbik.solver.leftArmChain.bendConstraint.weight = (float)lerpTime;
                    float angleSwivel = -angle + keyframesleft[i - 1].armSwivel;
                    //Debug.Log("1 --- " + angleSwivel);
                    leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position + (leftArmSwivelAttractor.transform.position - findChild_(transform, "LeftForeArm").position).normalized * 0.1f;
                    leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, angleSwivel * Time.deltaTime);
                }
                else
                {
                    leftArmSwivelAttractor.transform.position = findChild_(transform, "LeftForeArm").position + (leftArmSwivelAttractor.transform.position - findChild_(transform, "LeftForeArm").position).normalized * 0.1f;
                    float angleSwivel = - angle - keyframesleft[i].armSwivel + keyframesleft[i - 1].armSwivel;
                    //Debug.Log("2 --- " + angleSwivel);
                    leftArmSwivelAttractor.transform.RotateAround(findChild_(transform, "LeftForeArm").position, findChild_(transform, "LeftArm").right, angleSwivel * Time.deltaTime);
                    fbbik.solver.leftArmChain.bendConstraint.weight = 1;
                }
                //Debug.Log(i + "   " + keyframesleft[i - 1].armSwivel * Time.deltaTime + " " + testangle);
                //testangle += keyframesleft[i - 1].armSwivel * Time.deltaTime;
            }
            else
            {
                double lerpTime = 1.0f - ((time - keyframesleft[i].start) / (keyframesleft[i].end - keyframesleft[i].start));
                endTime = keyframesleft[i].end;
                fbbik.solver.leftHandEffector.positionWeight = (float)lerpTime;
                fbbik.solver.leftArmChain.bendConstraint.weight = (float)lerpTime;
            }

            if (time > endTime)
            {
                keyframesleft.RemoveAt(i);
            }
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
