using UnityEngine;
using System;
using System.Xml;
using System.Collections.Generic;
using RootMotion.FinalIK;
using RootMotion;

namespace Animation
{


public class HandShape
{
    public Dictionary<string, Quaternion> fingers;
    public Vector3 size;
}

public struct HeadDirectionPhase
    {
        public string type;
        public float lerpTime;
        public string from, to;
        public float amountFrom, amountTo;
    }

internal class GestureKeyFrame
{
    public string modality;
    public string name;
    public double time;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 swivel;
    public Dictionary<string, Quaternion> fingers;
}

struct CharDimensions
{
    public float[] scale;
    public float armLength;
    public float armToForeArmLength;
    public float foreArmToHandLength;
    public float handLength; //from wrist to the index tip

    //distances from shoulder
    // HEIGH
    public Tuple<float, float> aboveHead;
    public Tuple<float, float> head;
    public Tuple<float, float> shoulder;
    public Tuple<float, float> chest;
    public Tuple<float, float> abdomen;
    public Tuple<float, float> belt;
    public Tuple<float, float> belowBelt;

    // RADIAL ORIENTATION
    public Tuple<float, float> farOut;
    public Tuple<float, float> justOut;
    public Tuple<float, float> side;
    public Tuple<float, float> front;
    public Tuple<float, float> inward;

    // DISTANCE
    public Tuple<float, float> touch;
    public Tuple<float, float> close;
    public Tuple<float, float> normal;
    public Tuple<float, float> far;

    // ARM SWIVEL
    public Tuple<float, float> swivelTouch;
    public Tuple<float, float> swivelNormal;
    public Tuple<float, float> swivelOut;
    public Tuple<float, float> swivelOrthogonal;

    // SHOULDER for Torso movements
    public Tuple<float, float> upDown;
    public Tuple<float, float> backwardForward;
    public Tuple<float, float> inwardOutward;
}

    public class UnityAnimationEngine : MonoBehaviour, IAnimationEngine
    {
        /* AGENT BASIC INFORMATION */
        internal VirtualAgent.Character agent_;
        public Dictionary<string, HandShape> handTable = new Dictionary<string, HandShape>();
        internal FullBodyBipedIK ik;
        internal LookAtIK lookAt;
        internal GameObject transparentTarget;
        internal Transform head;
        internal Transform[] eyes; //0 left, 1 right
        private float eyesUPangle;

        public AudioSource audioS = null;

        //recompute symbolic position when agent's idle changes or when it walks around (TODO)
        private bool recomputePositions = true;

        /* ALL FOR IDLES AND GESTURES MOCAP */
        //TODO : to finish !!!!
        private Animator animator;
        //private RuntimeAnimatorController ac;
        private AnimatorOverrideController animatorOverrideController;
        private UnityEngine.Object[] clips;
        // private string startMocap = "";


        /* ALL FOR GESTURE AND POSTURE ANIMATION */

        //hands transform to animate in Update()
        private Transform leftHand;
        private Transform rightHand;


        //fingers transform to animate in LateUpdate()
        public Dictionary<string, Transform> leftHandFingers = new Dictionary<string, Transform>();
        public Dictionary<string, Transform> rightHandFingers = new Dictionary<string, Transform>();

        //fingers local rotation for rest position (without posture, just idle)
        private Dictionary<string, Quaternion> restLeftHand = new Dictionary<string, Quaternion>();
        private Dictionary<string, Quaternion> restRightHand = new Dictionary<string, Quaternion>();

        //fingers local rotation for rest position (without posture, just idle)
        private Dictionary<string, Quaternion> currentLeftHand = new Dictionary<string, Quaternion>();
        private Dictionary<string, Quaternion> currentRightHand = new Dictionary<string, Quaternion>();

        //attractors for elbow rotation
        public GameObject leftArmSwivelAttractor, rightArmSwivelAttractor;
        private float bendConstraint_weight;

        //flags to force the agent in rest position just once (when there is neither gesture nor posture to animate)  
        private bool flagLeftHandUpdate = false, flagRightHandUpdate = false;
        private bool flagLeftShoulderUpdate = false, flagRightShoulderUpdate = false;

        //internal Transform leftArm, rightArm;
        public GameObject left, right, testhand;  //fixed objects on shoulders
        internal CharDimensions characterDimensions;

        private Vector3 startShoulderLeft, startShoulderRight;
        private Vector3 endShoulderLeft, endShoulderRight;

        private Vector3 currentLeftArm, currentRightArm;

        private Vector3 currentLeftHandPosition, currentRightHandPosition;
        private Vector3 currentLeftSwivelPosition, currentRightSwivelPosition;
        private Quaternion currentLeftHandRotation, currentRightHandRotation;

        private Transform initHead;//MGtest

        //test for spline interpolation
        private List<GestureKeyFrame> leftArmKeyFrames = new List<GestureKeyFrame>();
        private List<GestureKeyFrame> rightArmKeyFrames = new List<GestureKeyFrame>();
        //spline

        //all head derictions 1)rotation, 2)translation
        private Dictionary<string, Tuple<Vector3, Vector3>> headDirections_;

        public SkinnedMeshRenderer face;
        public SkinnedMeshRenderer jaw;
        public SkinnedMeshRenderer tongue;
        public SkinnedMeshRenderer lEye;
        public SkinnedMeshRenderer rEye;
        public SkinnedMeshRenderer body;


        //variables for agent rendering
        private bool char_ready = false;
        Renderer rend;
        private SkinnedMeshRenderer[] visuGameObjects;
        private GameObject pointLightACA;
        private bool dot_light_visu = false;
        private float start_time_visu;
        private bool light_activated = false;
        private Vector3 agentPosInit;
        private Quaternion agentRotInit;
        private Vector3 currentRestAgentPos;
        private float speed_coeff = 1f;//speed the ACA disapears


        public void SetAgent(VirtualAgent.Character a)
        {
            agent_ = a;
        }

        public void Init()
        {
            agentPosInit = transform.position;
            agentRotInit = transform.rotation;
            currentRestAgentPos = agentPosInit;

            // Instantiate, add FBBIK
            audioS = gameObject.AddComponent<AudioSource>();

            animator = gameObject.GetComponent<Animator>();
            animatorOverrideController = new AnimatorOverrideController();
            animatorOverrideController.runtimeAnimatorController = animator.runtimeAnimatorController;
            clips = Resources.LoadAll("Animations", typeof(AnimationClip));

            ik = gameObject.AddComponent<FullBodyBipedIK>();

            // Auto-detect biped references
            BipedReferences references = new BipedReferences();
            BipedReferences.AutoDetectReferences(ref references, ik.transform, BipedReferences.AutoDetectParams.Default);
            ik.SetReferences(references, null);

            // Set some solver params you might want to use with head effector
            //ik.solver.leftHandEffector.maintainRelativePositionWeight = 0.75f;
            //ik.solver.rightHandEffector.maintainRelativePositionWeight = 0.75f;


            lookAt = gameObject.AddComponent<LookAtIK>();
            Transform[] spines = new Transform[3];
            spines[0] = findChild_(transform, "Spine");
            spines[1] = findChild_(transform, "Spine1");
            spines[2] = findChild_(transform, "Spine2");
            eyes = new Transform[2];
            eyes[0] = findChild_(transform, "LeftEye");
            eyes[1] = findChild_(transform, "RightEye");
            head = findChild_(transform, "Head");

            //initial head transform, needed for head animation
            initHead = Instantiate(head, head.parent);

            lookAt.solver.SetChain(spines, head, eyes, transform);
            lookAt.solver.SetIKPositionWeight(0);

            eyesUPangle = eyes[0].localRotation.eulerAngles.y;
            if (eyesUPangle > 180.0) eyesUPangle -= 360.0f;
            if (eyesUPangle < -180.0) eyesUPangle += 360.0f;

            transparentTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            transparentTarget.transform.position = new Vector3(0, 0, 0);
            transparentTarget.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            transparentTarget.name = ik.gameObject.name + "transparentTarget";
            transparentTarget.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
            transparentTarget.SetActive(false); //TODO: change to false when you don't want the green target visible anymore

            lookAt.solver.target = transparentTarget.transform;

            //TODO : IMPROVE THESE VALUES ACCORDIG TO THE TARGET POSITION (see commented function Targetangles())
            lookAt.solver.bodyWeight = 0.05f;
            lookAt.solver.headWeight = 0.7f;
            lookAt.solver.eyesWeight = 0.4f;
            // Changing the clamp weight of individual body parts
            lookAt.solver.clampWeight = 0.5f;
            lookAt.solver.clampWeightHead = 0.5f;
            lookAt.solver.clampWeightEyes = 0.5f;

            lookAt.enabled = false;
            ik.enabled = false;

            SetHeadDirection();

            leftHand = findChild_(transform, "LeftHand");
            rightHand = findChild_(transform, "RightHand");

            currentLeftArm = findChild_(transform, "LeftArm").position;
            currentRightArm = findChild_(transform, "RightArm").position;
            currentLeftHandPosition = leftHand.position;
            currentRightHandPosition = rightHand.position;
            currentLeftHandRotation = leftHand.rotation;
            currentRightHandRotation = rightHand.rotation;

            //fixed objects on shoulder
            createShoulderAttractors(false);//false to not display attractors

            SetCharacterDimensions(ref characterDimensions);
            createArmSwivelAttractors(false);//true/false display

            SetFingers(leftHandFingers, 'L');
            SetFingers(rightHandFingers, 'R');
        }

                 

        public void LoadMeshes(string faceMesh, string jawMesh, string tongueMesh, string leftEyeMesh, string rightEyeMesh, string bodyMesh)
        {
            SkinnedMeshRenderer[] smrs = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer s in smrs)
            {
                if (s.name.Equals(faceMesh)) { face = s; continue; }
                if (s.name.Equals(jawMesh)) { jaw = s; continue; }
                if (s.name.Equals(tongueMesh)) { tongue = s; continue; }
                if (s.name.Equals(leftEyeMesh)) { lEye = s; continue; }
                if (s.name.Equals(rightEyeMesh)) { rEye = s; continue; }
                if (s.name.Equals(bodyMesh)) { body = s; continue; }
            }
        }

        public int GetFaceBlenshapeCount()
        {
            return face.sharedMesh.blendShapeCount;
        }

        public int GetJawBlenshapeCount()
        {
            return jaw.sharedMesh.blendShapeCount;
        }

        public string GetAssetsPath()
        {
            return Application.streamingAssetsPath;
        }

        private void SetFingers(Dictionary<string, Transform> hand, char side)
        {
            if (side == 'L')
            {
                Transform[] children = leftHand.GetComponentsInChildren<Transform>();
                foreach (Transform t in children)
                {
                    if (t.name != "LeftHand" && t.name != "LeftFingerBase" && !t.name.Contains("4"))
                        hand.Add(t.name.Substring(8).ToUpper(), t);
                }
            }
            else
            {
                Transform[] children = rightHand.GetComponentsInChildren<Transform>();
                foreach (Transform t in children)
                {
                    if (t.name != "RightHand" && t.name != "RightFingerBase" && !t.name.Contains("4"))
                        hand.Add(t.name.Substring(9).ToUpper(), t);
                }
            }
        }

        private void SetHeadDirection()
        {
            headDirections_ = new Dictionary<string, Tuple<Vector3, Vector3>>();
            headDirections_.Add("up", new Tuple<Vector3, Vector3>(new Vector3(0, 15, 0), new Vector3(0, 0, 0)));
            headDirections_.Add("down", new Tuple<Vector3, Vector3>(new Vector3(0, -15, 0), new Vector3(0, 0, 0)));
            headDirections_.Add("left", new Tuple<Vector3, Vector3>(new Vector3(15, 0, 0), new Vector3(0, 0, 0)));
            headDirections_.Add("right", new Tuple<Vector3, Vector3>(new Vector3(-15, 0, 0), new Vector3(0, 0, 0)));
            headDirections_.Add("tiltl", new Tuple<Vector3, Vector3>(new Vector3(0, 0, -10), new Vector3(0, 0, 0)));
            headDirections_.Add("tiltr", new Tuple<Vector3, Vector3>(new Vector3(0, 0, 10), new Vector3(0, 0, 0)));
            headDirections_.Add("forward", new Tuple<Vector3, Vector3>(new Vector3(0, 0, 0), new Vector3(0f, 0, -0.05f)));
            headDirections_.Add("backward", new Tuple<Vector3, Vector3>(new Vector3(0, 0, 0), new Vector3(0f, 0, 0.05f)));
            headDirections_.Add("none", new Tuple<Vector3, Vector3>(new Vector3(0, 0, 0), new Vector3(0, 0, 0)));
        }

        public void createShoulderAttractors(bool debug)
        {
            if (debug)
            {
                left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                left.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                left.GetComponent<Renderer>().material.color = new Color(0, 1, 1, 1);
                left.SetActive(debug);
                right = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                right.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                right.GetComponent<Renderer>().material.color = new Color(0, 0, 1, 1);
                right.SetActive(debug);
            }
            else
            {
                left = new GameObject();
                right = new GameObject();
            }

            left.transform.position = findChild_(transform, "LeftArm").position;
            left.transform.rotation *= transform.rotation;
            left.name = "left";
            left.transform.SetParent(transform);

            right.transform.position = findChild_(transform, "RightArm").position;
            right.transform.rotation *= transform.rotation;
            right.name = "right";
            right.transform.SetParent(transform);
        }

        public void createArmSwivelAttractors(bool debug)
        {
            if (debug)
            {
                leftArmSwivelAttractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leftArmSwivelAttractor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                leftArmSwivelAttractor.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
                rightArmSwivelAttractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightArmSwivelAttractor.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                rightArmSwivelAttractor.GetComponent<Renderer>().material.color = new Color(1, 1, 0, 1);
            }
            else
            {
                leftArmSwivelAttractor = new GameObject();
                rightArmSwivelAttractor = new GameObject();
            }

            leftArmSwivelAttractor.transform.position = transform.TransformPoint(GetAbsoluteSwivelPosition(2));
            leftArmSwivelAttractor.transform.SetParent(transform);
            leftArmSwivelAttractor.name = "leftswivel";
            leftArmSwivelAttractor.SetActive(debug);
            rightArmSwivelAttractor.transform.position = transform.TransformPoint(GetAbsoluteSwivelPosition(-2));
            rightArmSwivelAttractor.transform.SetParent(transform);
            rightArmSwivelAttractor.name = "rightswivel";
            rightArmSwivelAttractor.SetActive(debug);

            bendConstraint_weight = 0.5f * transform.localScale.x;
            gameObject.GetComponent<FullBodyBipedIK>().solver.leftArmChain.bendConstraint.bendGoal = leftArmSwivelAttractor.transform;
            gameObject.GetComponent<FullBodyBipedIK>().solver.rightArmChain.bendConstraint.bendGoal = rightArmSwivelAttractor.transform;
        }

        private void SetCharacterDimensions(ref CharDimensions cd)
        {
            Transform spine = findChild_(transform, "Spine");
            Transform spine2 = findChild_(transform, "Spine2");
            Transform spine1 = findChild_(transform, "Spine1");
            head = findChild_(transform, "Head");
            Transform headEnd = findChild_(transform, "HeadEnd");
            Transform shoulder = findChild_(transform, "LeftShoulder");
            Transform foreArm = findChild_(transform, "LeftForeArm");
            Transform arm = findChild_(transform, "LeftArm");
            Transform hand = findChild_(transform, "LeftHand");
            Transform index = findChild_(transform, "LeftHandIndex4");
            eyes[0] = findChild_(transform, "LeftEye");
            eyes[1] = findChild_(transform, "RightEye");

            cd.scale = new float[3];
            cd.scale[0] = transform.localScale.x;
            cd.scale[1] = transform.localScale.y;
            cd.scale[2] = transform.localScale.z;

            cd.armToForeArmLength = Vector3.Distance(foreArm.position, arm.position);// / transform.localScale.x;
            cd.armLength = (Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position)); // / transform.localScale.x;
            cd.foreArmToHandLength = Vector3.Distance(foreArm.position, hand.position);// / transform.localScale.x;
            cd.handLength = Vector3.Distance(hand.position, index.position); // / transform.localScale.x;

            // HEIGHT
            float shoulder2chest = Vector3.Distance(shoulder.position, spine2.position) * 0.66f; // / transform.localScale.y;
            cd.chest = new Tuple<float, float>(shoulder2chest, shoulder2chest);

            float shoulder2abdomen = (Vector3.Distance(shoulder.position, spine2.position) + Vector3.Distance(spine1.position, spine2.position) * 0.5f);// / transform.localScale.y;
            cd.abdomen = new Tuple<float, float>(shoulder2abdomen, Math.Abs(shoulder2abdomen - shoulder2chest));

            float shoulder2belt = Vector3.Distance(shoulder.position, spine.position);// / transform.localScale.y;
            cd.belt = new Tuple<float, float>(shoulder2belt, Math.Abs(shoulder2belt - shoulder2abdomen));

            float shoulder2belowBelt = (Vector3.Distance(shoulder.position, spine.position) + Vector3.Distance(spine1.position, spine2.position));// / transform.localScale.y;
            cd.belowBelt = new Tuple<float, float>(shoulder2belowBelt, Math.Abs(shoulder2belowBelt - shoulder2belt));

            float shoulder2aboveHead = Vector3.Distance(shoulder.position, headEnd.position) * 1.5f;// / transform.localScale.y;
            cd.aboveHead = new Tuple<float, float>(shoulder2aboveHead, shoulder2chest);

            float shoulder2head = (Vector3.Distance(shoulder.position, head.position) + Vector3.Distance(shoulder.position, head.position) * 0.5f);// / transform.localScale.y;
            cd.head = new Tuple<float, float>(shoulder2head, shoulder2aboveHead);

            cd.shoulder = new Tuple<float, float>(0, shoulder2head);

            // RADIAL ORIENTATION
            float shoulder2front = Vector3.Distance(shoulder.position, arm.position);// / transform.localScale.x;
            cd.front = new Tuple<float, float>(shoulder2front, shoulder2front);

            float shoulder2farOut = (Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position));// / transform.localScale.x;
            cd.farOut = new Tuple<float, float>(shoulder2farOut, shoulder2front);

            float shoulder2out = Vector3.Distance(foreArm.position, arm.position);// / transform.localScale.x;
            cd.justOut = new Tuple<float, float>(shoulder2out, Math.Abs(shoulder2farOut - shoulder2out));

            cd.side = new Tuple<float, float>(0, shoulder2out);

            float shoulder2inward = Vector3.Distance(shoulder.position, arm.position) * 2;// / transform.localScale.x;
            cd.inward = new Tuple<float, float>(shoulder2inward, Math.Abs(shoulder2inward - shoulder2front));

            // DISTANCE
            float shoulder2far = (Vector3.Distance(foreArm.position, arm.position) + Vector3.Distance(foreArm.position, hand.position));// / transform.localScale.z;
            cd.far = new Tuple<float, float>(shoulder2far, shoulder2far * 0.3f);

            float shoulder2normal = shoulder2far * 0.7f;
            cd.normal = new Tuple<float, float>(shoulder2normal, Math.Abs(shoulder2far - shoulder2normal));

            float shoulder2close = shoulder2far * 0.45f;
            cd.close = new Tuple<float, float>(shoulder2close, Math.Abs(shoulder2normal - shoulder2close));

            float shoulder2touch = shoulder2far * 0.01f;
            cd.touch = new Tuple<float, float>(shoulder2touch, Math.Abs(shoulder2close - shoulder2touch));

            // ARM SWIVEL radiant   (Math.PI * (degree) / 180.0)
            cd.swivelTouch = new Tuple<float, float>(1.7453f, -0.349f);   //(100 degree, -20 degree)  
            cd.swivelNormal = new Tuple<float, float>(1.3962f, -0.6981f); // (80 degree, -40 degree)
            cd.swivelOut = new Tuple<float, float>(0.6981f, -0.6981f);    // (40 degree, -40 degree)
            cd.swivelOrthogonal = new Tuple<float, float>(0, -0.349f);     // ( 0 degree,  20 degree)

            // SHOULDERS for Torso mouvements
            float distTorso = 0.1f;// / transform.localScale.x;
            cd.upDown = new Tuple<float, float>(distTorso, distTorso);
            cd.inwardOutward = new Tuple<float, float>(distTorso, distTorso);
            cd.backwardForward = new Tuple<float, float>(distTorso, distTorso);
        }

        public Vector3 GetAbsoluteShoulderPosition(char side, int radialOrientation, int heigh, int distance, float ru = 0.0f, float hu = 0, float du = 0, float amount = 1)
        {
            //radialOrientation : 1 left inward 0 normal -1 right outward
            //radialOrientation : -2 right inward 0 normal 2 right outward
            //height : 1 up 0 normal -1 down
            //distance : 1 forward 0 normal -1 backward
            GameObject shoulder = left;
            if (side == 'R')
            {
                radialOrientation = -radialOrientation / 2;
                shoulder = right;
            }
            Vector3 pos = new Vector3(radialOrientation * characterDimensions.inwardOutward.Item1 * amount + characterDimensions.inwardOutward.Item2 * ru,
                               heigh * characterDimensions.upDown.Item1 * amount + characterDimensions.upDown.Item2 * hu,
                               distance * characterDimensions.backwardForward.Item1 * amount + characterDimensions.backwardForward.Item2 * du);
            //SCALE!!
            pos.x *= transform.localScale.x;
            pos.y *= transform.localScale.y;
            pos.z *= transform.localScale.z;

            pos = shoulder.transform.TransformDirection(pos);
            return shoulder.transform.position + pos;
        }

        public Vector3 GetAbsoluteSwivelPosition(int i, float su = 0)
        {
            double radiant;
            float depth;
            int sign = i / Math.Abs(i);
            switch (i)
            {
                case 1:
                case -1: //touch
                    radiant = sign * (characterDimensions.swivelTouch.Item1 + characterDimensions.swivelTouch.Item2 * su);
                    depth = 0.15f;
                    break;
                case 2:
                case -2: //normal
                    radiant = sign * (characterDimensions.swivelNormal.Item1 + characterDimensions.swivelNormal.Item2 * su);
                    depth = 0.13f;
                    break;
                case 3:
                case -3: // out
                    radiant = sign * (characterDimensions.swivelOut.Item1 + characterDimensions.swivelOut.Item2 * su);
                    depth = 0.1f;
                    break;
                case 4:
                case -4: //orthogonal
                    radiant = sign * (characterDimensions.swivelOrthogonal.Item1 + characterDimensions.swivelOrthogonal.Item2 * su);
                    depth = 0;
                    break;
                default:
                    radiant = sign * characterDimensions.swivelNormal.Item1;
                    depth = 0.1f;
                    break;
            }

            float foreArmLength = characterDimensions.armToForeArmLength;// * transform.localScale.x;
            var pos = new Vector3(foreArmLength * (float)Math.Cos(radiant), foreArmLength * (float)Math.Sin(radiant), sign * depth);// * transform.localScale.z);

            if (i > 0)
            {
                pos = left.transform.TransformDirection(pos);
                return transform.InverseTransformPoint(left.transform.position - pos);

            }
            else
            {
                pos = right.transform.TransformDirection(pos);
                return transform.InverseTransformPoint(right.transform.position + pos);
            }
        }

        public
        Vector3 GetAbsoluteWristPosition(int radialOrientation, int heigh, int distance, float ru = 0.0f, float hu = 0, float du = 0)
        {
            Vector3 pos = new Vector3();
            switch (heigh)
            {
                case 1: //above_head
                    pos.y = characterDimensions.aboveHead.Item1 + characterDimensions.aboveHead.Item2 * hu;
                    break;
                case 2: //head
                    pos.y = characterDimensions.head.Item1 + characterDimensions.head.Item2 * hu;
                    break;
                case 3: //shoulder
                    pos.y = characterDimensions.shoulder.Item1 + characterDimensions.shoulder.Item2 * hu;
                    break;
                case 4: //chest
                    pos.y = -characterDimensions.chest.Item1 + characterDimensions.chest.Item2 * hu;
                    break;
                case 5: //abdoment
                    pos.y = -characterDimensions.abdomen.Item1 + characterDimensions.abdomen.Item2 * hu;
                    break;
                case 6: //belt
                    pos.y = -characterDimensions.belt.Item1 + characterDimensions.belt.Item2 * hu;
                    break;
                case 7: //below_belt
                    pos.y = -characterDimensions.belowBelt.Item1 + characterDimensions.belowBelt.Item2 * hu;
                    break;
                default:
                    pos.y = 0; break;
            }

            int sign = radialOrientation / Math.Abs(radialOrientation);
            switch (radialOrientation)
            {
                case 1:
                case -1://inward
                    pos.x = sign * (characterDimensions.inward.Item1 - characterDimensions.inward.Item2 * ru);
                    break;
                case 2:
                case -2: //front
                    pos.x = sign * (characterDimensions.front.Item1 - characterDimensions.front.Item2 * ru);
                    break;
                case 3:
                case -3://side
                    pos.x = -sign * characterDimensions.side.Item1 - sign * characterDimensions.side.Item2 * ru;
                    break;
                case 4:
                case -4: //out
                    pos.x = -sign * (characterDimensions.justOut.Item1 + characterDimensions.justOut.Item2 * ru);
                    break;
                case 5:
                case -5://far_out
                    pos.x = -sign * (characterDimensions.farOut.Item1 + characterDimensions.farOut.Item2 * ru);
                    break;
                default:
                    pos.x = 0; break;
            }

            switch (distance)
            {
                case 1: //touch
                    pos.z = characterDimensions.touch.Item1 + characterDimensions.touch.Item2 * du;
                    break;
                case 2:
                default://close
                    pos.z = characterDimensions.close.Item1 + characterDimensions.close.Item2 * du;
                    break;
                case 3: //normal
                    pos.z = characterDimensions.normal.Item1 + characterDimensions.normal.Item2 * du;
                    break;
                case 4: //far
                    pos.z = characterDimensions.far.Item1 + characterDimensions.far.Item2 * du;
                    break;
            }

            if (pos.magnitude > characterDimensions.armLength && hu == 0 && ru == 0 && du == 0)
            {
                pos.Normalize();
                pos *= characterDimensions.armLength; // * transform.localScale.x;
            }

            /*pos.x *= transform.localScale.x;
            pos.y *= transform.localScale.y;
            pos.z *= transform.localScale.z;*/

            if (radialOrientation > 0)
            {
                pos = left.transform.TransformDirection(pos);
                return transform.InverseTransformPoint(left.transform.position + pos);
            }
            else
            {
                pos = right.transform.TransformDirection(pos);
                return transform.InverseTransformPoint(right.transform.position + pos);
            }
        }

        private Quaternion GetUnityQuaternion(VirtualAgent.Quaternion rotation)
        {
            return new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        private VirtualAgent.Quaternion GetVirtualAgentQuaternion(Quaternion rotation)
        {
            return new VirtualAgent.Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
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

        public VirtualAgent.Vector3 FindJointPosition(string name)
        {
            Vector3 pos = findChild_(transform, name).position;
            return new VirtualAgent.Vector3 { x = pos.x, y = pos.y, z = pos.z };
        }

        public bool TargetFound(string target)
        {
            return GameObject.Find(target) != null ? true : false;
        }

        public int GetTargetSide(string target, string mode)
        {
            GameObject obj = GameObject.Find(target);
            if (obj == null)
                return 0;

            int side = 0;
            var dist = obj.transform.position - transform.position;
            var projectionOnRight = Vector3.Dot(dist, transform.right); //- left, + right
            var projectionOnForward = Vector3.Dot(dist, transform.forward);  //- behind, + forward

            if (projectionOnForward < 0)
                return -1; //Object behind

            if (projectionOnRight >= 0)
            {
                if (mode == "LEFT" && projectionOnRight < 0.15)
                    side = 1; // "LEFT";
                else
                    side = 2; // "RIGHT";
            }
            else
            {
                if (mode == "RIGHT" && projectionOnRight > -0.15)
                    side = 2; // "RIGHT";
                else
                    side = 1; // "LEFT";
            }
            return side;
        }

        private void GetGazeWeights(Transform target)
        {
            var dist = target.position - head.position;
            dist.Normalize();
            var projectionOnRight = Math.Abs(Vector3.Dot(dist, head.up)); //- left, + right
            var projectionOnUp = Math.Abs(Vector3.Dot(dist, head.right)); //- down, + up

            var max = projectionOnRight > projectionOnUp ? projectionOnRight : projectionOnUp;

            lookAt.solver.eyesWeight = 1;
            lookAt.solver.headWeight = max*1.2f;
            lookAt.solver.bodyWeight = 0; //TO IMPROVE

            //Debug.Log(max);
        }

        // Update is called once per frame
        void Update()
        {
            double tm = Time.time;

            //TODO : reset character dimensions
            if (characterDimensions.scale[0] != transform.localScale.x ||
                characterDimensions.scale[1] != transform.localScale.y ||
                characterDimensions.scale[2] != transform.localScale.z)
                SetCharacterDimensions(ref characterDimensions);

            if (findChild_(transform, "LeftArm").position.y - findChild_(transform, "LeftHand").position.y > characterDimensions.armLength / 2.0) char_ready = true;//MGcorr

            
            //Keep track of movements whent the agent is gesturing
            //this is necessary to chain gestures and start from the current position
            //a new gesture
            if (char_ready == true && recomputePositions == true)
            {
                GetHandCurrentShape(restRightHand, 'R');
                GetHandCurrentShape(restLeftHand, 'L');
                GetHandCurrentShape(currentLeftHand, 'L');
                GetHandCurrentShape(currentRightHand, 'R');
                left.transform.position = findChild_(transform, "LeftArm").position;
                right.transform.position = findChild_(transform, "RightArm").position;

                currentLeftArm = transform.InverseTransformPoint(left.transform.position);
                currentRightArm = transform.InverseTransformPoint(right.transform.position);

                leftHand = findChild_(transform, "LeftHand");
                rightHand = findChild_(transform, "RightHand");

                currentLeftHandPosition = transform.InverseTransformPoint(leftHand.position);
                currentRightHandPosition = transform.InverseTransformPoint(rightHand.position);
                currentLeftSwivelPosition = leftArmSwivelAttractor.transform.position;
                currentRightSwivelPosition = rightArmSwivelAttractor.transform.position;
                currentLeftHandRotation = leftHand.rotation;
                currentRightHandRotation = rightHand.rotation;

                recomputePositions = false;
            }
            lookAt.solver.FixTransforms();
            ik.solver.FixTransforms();//MG to check if ok not to have it; if not, add a condition to remove it when gestureEditor
            currentLeftSwivelPosition = leftArmSwivelAttractor.transform.position;
            currentRightSwivelPosition = rightArmSwivelAttractor.transform.position;
        }

        void LateUpdate()
        {
            float time = Time.time;

            //TODO : reset everything just ONCE !!
            agent_.fe.ResetBlendshapesDictionaries();
            lookAt.solver.SetIKPositionWeight(0);

            /********   FACE AND SPEECH   ******/
            agent_.fe.GetCurrentFacialBlendshapes("speech", time);
            agent_.fe.GetCurrentFacialBlendshapes("face", time);


            foreach (KeyValuePair<int, float> bs in agent_.fe.face_blendshapes)
            {
                face.SetBlendShapeWeight(bs.Key, bs.Value);
            }
            foreach (KeyValuePair<int, float> bs in agent_.fe.jaw_tongue_blendshapes)
            {
                jaw.SetBlendShapeWeight(bs.Key, bs.Value);
            }

            /********   HEAD DIRECTION AND ACTION ******/

            MoveHeadDirection(time);

            /********   GAZE   ******/
            float lerpTimeGaze = 0;
            string gazeFrom = "", gazeTo = "";
            if (agent_.fe.GetCurrentGazeDirection(time, ref lerpTimeGaze, ref gazeFrom, ref gazeTo))
            {                
                lookAt.solver.eyesWeight = 1f;
                GameObject target_from = GameObject.Find(gazeFrom);
                GameObject target_to = GameObject.Find(gazeTo);

                if (target_from == null && target_to != null)
                {
                    lookAt.solver.SetIKPositionWeight(lerpTimeGaze);
                    transparentTarget.transform.position = target_to.transform.position;
                }

                if (target_to == null && target_from != null)
                {
                    lerpTimeGaze = 1 - lerpTimeGaze;
                    lookAt.solver.SetIKPositionWeight(lerpTimeGaze);
                    transparentTarget.transform.position = target_from.transform.position;
                }

                if (target_to != null && target_from != null)
                {
                    lookAt.solver.SetIKPositionWeight(1);
                    transparentTarget.transform.position = Vector3.Lerp(target_from.transform.position, target_to.transform.position, lerpTimeGaze);
                }
                GetGazeWeights(transparentTarget.transform);
                lookAt.solver.Update();
            }

            MoveHeadAction(time);

            //make eyelid follow eyes rotation
            //Blendshapes 47 48 open eyelids, Blendshapes 45 46 close eyelids
            float eyesAngle = Quaternion.Angle(eyes[0].localRotation, Quaternion.Euler(Vector3.up)) + eyesUPangle;
            if (eyesAngle < -1)
            {
                //looking UP
                float weight = 80f * (Mathf.Abs(eyesAngle) / 20);
                face.SetBlendShapeWeight(47, face.GetBlendShapeWeight(47) + weight);
                face.SetBlendShapeWeight(48, face.GetBlendShapeWeight(48) + weight);
            }
            if (eyesAngle > 1)
            {
                //looking DOWN
                float weight = 40f * (Mathf.Abs(eyesAngle) / 20);
                face.SetBlendShapeWeight(45, face.GetBlendShapeWeight(45) + weight);
                face.SetBlendShapeWeight(46, face.GetBlendShapeWeight(46) + weight);
            }


            /******** TORSO *******/
            moveShoulder(agent_.toe.leftTorsoKeyFrames, time, 'L');
            moveShoulder(agent_.toe.rightTorsoKeyFrames, time, 'R');


            /******** GESTURE ******/
            moveArm(agent_.ge.keyFrames.left, time, 'L');
            moveArm(agent_.ge.keyFrames.right, time, 'R');
            ik.solver.Update();

            if (agent_.toe.leftTorsoKeyFrames.Count > 0)
                currentLeftArm = transform.InverseTransformPoint(findChild_(transform, "LeftArm").position);
            if (agent_.toe.rightTorsoKeyFrames.Count > 0)
                currentRightArm = transform.InverseTransformPoint(findChild_(transform, "RightArm").position);

            if (agent_.ge.keyFrames.left.Count > 0)
            {
                leftHand = findChild_(transform, "LeftHand");
                currentLeftHandPosition = transform.InverseTransformPoint(leftHand.position);
                currentLeftHandRotation = leftHand.rotation;
                GetHandCurrentShape(currentLeftHand, 'L');
            }
            if (agent_.ge.keyFrames.right.Count > 0)
            {
                rightHand = findChild_(transform, "RightHand");
                currentRightHandPosition = transform.InverseTransformPoint(rightHand.position);
                currentRightHandRotation = rightHand.rotation;
                GetHandCurrentShape(currentRightHand, 'R');
            }

        }

        bool MoveHeadDirection(float time)
        {
            List<HeadDirectionPhase> headDirections = null;
            if (!agent_.fe.GetCurrentHeadDirection(time, ref headDirections)) return false;

            Quaternion angle = Quaternion.identity;
            Vector3 delta = Vector3.zero;

            foreach (var hd in headDirections)
            {
                float lerpTime = hd.lerpTime;
                Vector3 fromPos = headDirections_[hd.from].Item2 * hd.amountFrom;
                Vector3 toPos = headDirections_[hd.to].Item2 * hd.amountTo;
                Vector3 rotation_from = headDirections_[hd.from].Item1 * hd.amountFrom;
                Vector3 rotation_to = headDirections_[hd.to].Item1 * hd.amountTo;


                Vector3 world_angleFrom = initHead.TransformDirection(rotation_from);
                Vector3 world_angleTo = initHead.TransformDirection(rotation_to);
                Vector3 world_angleStep = Vector3.Lerp(world_angleFrom, world_angleTo, lerpTime);
                Vector3 angleToApply = head.InverseTransformDirection(world_angleStep);
                angle *= Quaternion.Euler(angleToApply);
                delta += Vector3.Lerp(fromPos, toPos, lerpTime);
            }

            head.localPosition += delta;
            head.localRotation *= angle;

            return true;
        }

        void MoveHeadAction(float time)
        {
            HeadDirectionPhase action = agent_.fe.GetCurrentHeadAction(time);
            if (action.from.Equals("")) return;  //Nothing to do
            
            string from = action.from, to = action.to;

            float lerpTime = action.lerpTime;
            Vector3 rotation_from = headDirections_[from].Item1 * action.amountFrom;
            Vector3 rotation_to = headDirections_[to].Item1 * action.amountTo;

            Quaternion angle = Quaternion.Slerp(Quaternion.Euler(rotation_from), Quaternion.Euler(rotation_to), lerpTime);

            head.localRotation *= angle;
            eyes[0].localRotation *= Quaternion.Inverse(angle);
            eyes[1].localRotation *= Quaternion.Inverse(angle);
        }

        public void PlayMotionCapture(string name)
        {
            int j = 0;
            AnimationClip clip = null;
            for (; j < clips.Length; j++)
            {
                if (clips[j].name == name)
                {
                    clip = (AnimationClip)clips[j];
                    break;
                }
            }

            if (clip == null)
            {
                Debug.Log("Mocap for gesture " + name + " not found");
                return;
            }
            animatorOverrideController["anim1"] = clip;
            animator.runtimeAnimatorController = animatorOverrideController;
            animator.ResetTrigger("idle");
            animator.SetTrigger("playAnim");
        }

        public void PlayVoice(TextToSpeech.SpeechData sdata)
        {
            audioS.clip = AudioClip.Create("text", sdata.Audiobuf.Length, 1, sdata.Audiorate, false); //47250
            float[] buffer = new float[sdata.Audiobuf.Length];
            for (int iPCM = 0; iPCM < sdata.Audiobuf.Length; iPCM++)
            {
                float f;
                int i = (int)sdata.Audiobuf[iPCM];
                f = ((float)i) / (float)32768;
                if (f > 1)
                    f = 1;
                if (f < -1)
                    f = -1;
                buffer[iPCM] = f;
            }
            audioS.clip.SetData(buffer, 0);
            audioS.Play();
        }

        public void StopVoice()
        {
            audioS.Stop();
            audioS.clip.UnloadAudioData();
        }

        private Vector3 GetPointingPosition(Transform target, char side, ref Quaternion rotation)
        {
            Vector3 startPosition, sidePosition;
            Vector3 shoulderTosideDirection;
            Transform hand;

            //startPotision is the starting point of the pointing vector
            //at the beginning it corresponds to the shoulder position (LeftArm or RightArm)
            //sidePosition is a point on the side of the agent

            if (side == 'L')
            {
                startPosition = findChild_(transform, "LeftArm").position;
                hand = findChild_(transform, "LeftHand");
                sidePosition = transform.TransformPoint(GetAbsoluteSwivelPosition(2));
            }
            else
            {
                startPosition = findChild_(transform, "RightArm").position;
                hand = findChild_(transform, "RightHand");
                sidePosition = transform.TransformPoint(GetAbsoluteSwivelPosition(-2));
            }

            //shoulderTosideDirection : direction from the shoulder to the side
            shoulderTosideDirection = sidePosition - startPosition;
            shoulderTosideDirection.Normalize();
            float shoulderToSideDistance = Vector3.Distance(startPosition, sidePosition);

            //according to the distance of the target, the startPosition goes from the shoulder to the side
            //mult=0 if the object is distant (startPosition won't move, it remains on the shoulder)
            //mult close to 1 if the object is very close and startPosition will move toward the side
            float mult = 1 - Math.Min(Vector3.Distance(target.position, startPosition) /
                                      (characterDimensions.armLength + characterDimensions.handLength), 1);
            startPosition += shoulderTosideDirection * (mult * shoulderToSideDistance);

            //Middle point between the target and the startPosition
            Vector3 middle_point = target.position - startPosition;

            //if the distance to the target is higher that the agent arm length,
            //the middle point is moved closer (arm length is the maximum distance allowed)
            float target_distance = Vector3.Distance(target.position, startPosition);

            if (target_distance > characterDimensions.armLength + characterDimensions.handLength)
            {
                middle_point.Normalize();
                middle_point *= characterDimensions.armLength;
            }
            else
            {
                middle_point.Normalize();
                middle_point *= target_distance - characterDimensions.handLength;
            }

            //endPosition = wrist position for pointing
            Vector3 endPosition = startPosition + middle_point;
            Vector3 direction = target.position - endPosition;

            //MG pointing corrections 
            Quaternion q2 = Quaternion.Euler(GameObject.Find("LeftHandIndex1").transform.localRotation.eulerAngles);
            rotation = Quaternion.Inverse(q2) * Quaternion.LookRotation(direction) * Quaternion.Euler(180f, -90f, 0f);//forward = zAxis align to the direction => * Quaternion.Euler(0f, -90f, 0f) to align xAxis

            return endPosition;
        }

        void GetHandCurrentShape(Dictionary<string, Quaternion> fingers, char side)
        {
            fingers.Clear();
            if (side == 'L')
            {
                Transform[] children = leftHand.GetComponentsInChildren<Transform>();
                foreach (Transform t in children)
                {
                    if (t.name != "LeftHand" && t.name != "LeftFingerBase" && !t.name.Contains("4"))
                        fingers.Add(t.name.Substring(8).ToUpper(), t.localRotation);
                }
            }
            else
            {
                Transform[] children = rightHand.GetComponentsInChildren<Transform>();
                foreach (Transform t in children)
                {
                    if (t.name != "RightHand" && t.name != "RightFingerBase" && !t.name.Contains("4"))
                        fingers.Add(t.name.Substring(9).ToUpper(), t.localRotation);
                }
            }
        }


        //test spline interpolation
        private GestureKeyFrame SetHand(VirtualAgent.HandKeyFrame kframe, char side, VirtualAgent.HandKeyFrame posture = null)
        {
            int sign = side == 'L' ? 1 : -1;
            GestureKeyFrame k = new GestureKeyFrame();
            k.name = kframe.id + ':' + kframe.name;
            k.modality = kframe.modality;
            k.time = kframe.time;

            if (kframe.name == "start" || (kframe.name == "end" && posture == null))
            {
                k.position = side == 'L' ? currentLeftHandPosition : currentRightHandPosition;
                k.rotation = side == 'L' ? currentLeftHandRotation : currentRightHandRotation;
                k.swivel = transform.InverseTransformPoint(side == 'L' ? currentLeftSwivelPosition : currentRightSwivelPosition);
                if (kframe.name == "start")
                    k.fingers = side == 'L' ? currentLeftHand : currentRightHand;
                else
                    k.fingers = side == 'L' ? restLeftHand : restRightHand;
                return k;
            }


            if (kframe.name == "end" && posture != null)
            {
                k.position = GetAbsoluteWristPosition(sign * (int)posture.rhp.radialOrientation, (int)posture.rhp.height, (int)posture.rhp.distance,
                                                posture.rhp.radialOrientationPercentage, posture.rhp.heightPercentage, posture.rhp.distancePercentage);
                k.rotation = transform.rotation * GetUnityQuaternion(posture.rhp.wristRotation);
                k.swivel = GetAbsoluteSwivelPosition(sign * (int)posture.rhp.armSwivel, posture.rhp.armSwivelPercentage);
                k.fingers = SetFingers(posture.rhp.handShape, side);
                return k;
            }

            if (kframe.modality == "pointing")
            {
                //pointing
                var target = GameObject.Find(kframe.target);
                Quaternion r = Quaternion.identity;
                k.position = transform.InverseTransformPoint(GetPointingPosition(target.transform, side, ref r));
                k.rotation = r;// Quaternion.Inverse(findChild_(transform, "LeftHand").localRotation); // transform.rotation * PointingWristRotation(pointingPosition, target.transform, side);
                               // k.rotation = transform.rotation * PointingWristRotation(GetPointingPosition(target.transform, side, ref r), target.transform, side);
                k.swivel = GetAbsoluteSwivelPosition(sign * 1, 0.5f);
                k.fingers = handTable[side + kframe.rhp.handShape].fingers;
                return k;
            }

            GameObject objTarget = GameObject.Find(kframe.target);

            if (!kframe.rhp.target || kframe.target == "" || objTarget == null)
            {
                k.position = GetAbsoluteWristPosition(sign * (int)kframe.rhp.radialOrientation, (int)kframe.rhp.height, (int)kframe.rhp.distance,
                                                     kframe.rhp.radialOrientationPercentage, kframe.rhp.heightPercentage, kframe.rhp.distancePercentage);
            }
            else
            {
                //POSITION DU TARGET
                //Met dans repère agent car transform inverse systematique pour animation
                k.position = transform.InverseTransformPoint(new Vector3(objTarget.transform.position.x + kframe.rhp.radialOrientationOffset * transform.localScale.x,
                                             objTarget.transform.position.y + kframe.rhp.heightOffset * transform.localScale.y,
                                             objTarget.transform.position.z + kframe.rhp.distanceOffset * transform.localScale.z));
            }
            k.rotation = transform.rotation * GetUnityQuaternion(kframe.rhp.wristRotation);


            //Hand offset
            if (objTarget != null)//Gesture applied on a target
                k.position -= sign * transform.InverseTransformDirection(k.rotation * (handTable[side + kframe.rhp.handShape].size));

            k.swivel = GetAbsoluteSwivelPosition(sign * (int)kframe.rhp.armSwivel, kframe.rhp.armSwivelPercentage);

            k.fingers = SetFingers(kframe.rhp.handShape, side);
            return k;
        }

        Dictionary<string, Quaternion> SetFingers(string handShape, char side)
        {
            if (handShape == "NONE" || handShape == "LNONE" || handShape == "RNONE")
                return side == 'L' ? restLeftHand : restRightHand;
            else
                return handTable[side + handShape].fingers;
        }


        private void moveArm(List<VirtualAgent.HandKeyFrame> frames, double time, char side)
        {
            IKEffector effector;
            FBIKChain armChain;
            VirtualAgent.HandKeyFrame posture;
            Dictionary<string, Transform> hand;
            List<GestureKeyFrame> handFrames;
            bool flagHandUpdate;
            int sign = side == 'L' ? 1 : -1;

            if (side == 'L')
            {
                effector = ik.solver.leftHandEffector;
                armChain = ik.solver.leftArmChain;
                hand = leftHandFingers;
                posture = agent_.ge.currentPostures.Count > 0 ? agent_.ge.currentPostures[agent_.ge.currentPostures.Count - 1].LeftHandPosture : null;
                flagHandUpdate = flagLeftHandUpdate;
                handFrames = leftArmKeyFrames;
            }
            else
            {
                effector = ik.solver.rightHandEffector;
                armChain = ik.solver.rightArmChain;
                hand = rightHandFingers;
                posture = agent_.ge.currentPostures.Count > 0 ? agent_.ge.currentPostures[agent_.ge.currentPostures.Count - 1].RightHandPosture : null;
                flagHandUpdate = flagRightHandUpdate;
                handFrames = rightArmKeyFrames;
            }

            if (frames.Count >= 3)
            {
                if (time >= frames[2].time)
                {
                    frames.RemoveAt(0);
                    if (handFrames.Count > 3) handFrames.RemoveAt(0);
                }
                if (frames.Count < 3 && (frames[1].name == "end" || (frames[1].name == "ready" && frames[1].modality == "posture")))
                {
                    frames.Clear(); //clear the key frames list after the last end
                    handFrames.Clear();
                }
            }

            if (frames.Count < 2 && frames.Count > 0)
            {
                Debug.Log("There's a problem!!! Just " + frames.Count + " key frames instead of 2 at least!!!");
                return;
            }

            if (frames.Count >= 3)
            {
                if (handFrames.Count == 0)
                {
                    handFrames.Add(SetHand(frames[0], side, posture));
                    handFrames.Add(SetHand(frames[1], side, posture));
                    handFrames.Add(SetHand(frames[2], side, posture));
                    if (frames.Count == 3)
                        handFrames.Add(SetHand(frames[2], side, posture)); //last frame added twice when there are just 3 key frames                 
                    else
                        handFrames.Add(SetHand(frames[3], side, posture));
                }
                else
                {
                    if (handFrames.Count == 3) // a key frame has changed
                    {

                        if (frames.Count >= 3)
                            handFrames.Add(SetHand(frames[2], side, posture));
                        else
                            handFrames.Add(handFrames[2]);
                    }

                    for (int i = 0; i < handFrames.Count - 1; ++i)
                    {

                        if ((handFrames[i].name.Contains(":end") ||
                             handFrames[i].name.Contains(":start") ||
                            (handFrames[i].name.Contains("ready") && handFrames[i].modality.Equals("posture"))) &&
                             handFrames[i].name.Equals(frames[i].id + ':' + frames[i].name)) continue;
                        handFrames[i] = SetHand(frames[i], side, posture);
                    }
                }

                if (side == 'L')
                    flagLeftHandUpdate = true;
                else
                    flagRightHandUpdate = true;

                float lerpTime = (float)((time - frames[1].time) / (frames[2].time - frames[1].time));

                effector.positionWeight = 1;
                effector.rotationWeight = 1;
                armChain.bendConstraint.weight = bendConstraint_weight;
                if (handFrames[2].name.Contains(":end") && posture == null)
                {
                    effector.positionWeight = 1 - lerpTime;
                    effector.rotationWeight = 1 - lerpTime;
                    armChain.bendConstraint.weight = LinearInterpolate2D(0, bendConstraint_weight, 1 - lerpTime);
                }

                effector.rotation = Quaternion.Lerp(handFrames[1].rotation, handFrames[2].rotation, lerpTime);
                effector.position = transform.TransformPoint(SplineInterpolation(lerpTime, handFrames[0].position, handFrames[1].position, handFrames[2].position, handFrames[3].position));
                armChain.bendConstraint.bendGoal.position = transform.TransformPoint(Vector3.Lerp(handFrames[1].swivel, handFrames[2].swivel, lerpTime));
                MoveHand(hand, handFrames[1].fingers, handFrames[2].fingers, lerpTime);
            }
            else
            {
                if (posture != null)
                {
                    effector.position = transform.TransformPoint(GetAbsoluteWristPosition(sign * (int)posture.rhp.radialOrientation, (int)posture.rhp.height, (int)posture.rhp.distance,
                                                    posture.rhp.radialOrientationPercentage, posture.rhp.heightPercentage, posture.rhp.distancePercentage));
                    effector.rotation = transform.rotation * GetUnityQuaternion(posture.rhp.wristRotation);
                    armChain.bendConstraint.bendGoal.position = transform.TransformPoint(GetAbsoluteSwivelPosition(sign * (int)posture.rhp.armSwivel, posture.rhp.armSwivelPercentage));
                    effector.positionWeight = 1f;
                    effector.rotationWeight = 1.0f;
                    armChain.bendConstraint.weight = bendConstraint_weight;
                    MoveHand(hand, SetFingers(posture.rhp.handShape, side), null, -1);
                }
                else
                    if (flagHandUpdate == true)
                {
                    effector.positionWeight = 0f;
                    armChain.bendConstraint.weight = 0f;
                    effector.rotationWeight = 0f;
                    armChain.bendConstraint.bendGoal.position = transform.TransformPoint(GetAbsoluteSwivelPosition((side == 'L') ? 2 : -2));
                    if (side == 'L')
                        flagLeftHandUpdate = false;
                    else
                        flagRightHandUpdate = false;
                }
            }
        }

        private void MoveHand(Dictionary<string, Transform> hand, Dictionary<string, Quaternion> A, Dictionary<string, Quaternion> B, float lerpTime)
        {
            foreach (KeyValuePair<string, Quaternion> fs in A)
            {
                if (hand.ContainsKey(fs.Key))
                {
                    Quaternion qb = Quaternion.Euler(0f, 0f, 0f);
                    if (B != null) qb = B[fs.Key];
                    if (lerpTime >= 0)
                    {
                        hand[fs.Key].localRotation = Quaternion.Lerp(fs.Value, qb, lerpTime);
                    }
                    else
                    {
                        hand[fs.Key].localRotation = fs.Value;
                    }
                }
            }
        }

        private Vector3 SetShoulder(ref Vector3 p, TorsoKeyFrame kframe, char side)
        {
            Vector3 position = p;
            if (kframe.name == "start" || kframe.name == "end")
            {
                if (p == new Vector3())
                {
                    position = side == 'L' ? currentLeftArm : currentRightArm;
                    p = position;
                }
                return transform.TransformPoint(position);
            }

            position = GetAbsoluteShoulderPosition(side, (int)kframe.rtp.radialOrientation, (int)kframe.rtp.height, (int)kframe.rtp.distance,
                        kframe.rtp.radialOrientationPercentage, kframe.rtp.heightPercentage, kframe.rtp.distancePercentage, (float)kframe.amount);

            return position;
        }

        private void moveShoulder(List<TorsoKeyFrame> frames, double time, char side)
        {
            IKEffector effector;

            bool flagShoulderUpdate;

            if (side == 'L')
            {
                effector = ik.solver.leftShoulderEffector;
                flagShoulderUpdate = flagLeftShoulderUpdate;
            }
            else
            {
                effector = ik.solver.rightShoulderEffector;
                flagShoulderUpdate = flagRightShoulderUpdate;
            }

            Vector3 positionA = new Vector3();
            Vector3 positionB = new Vector3();

            if (frames.Count > 0)
            {
                int i = frames.Count - 1;

                if (i >= 1)
                {
                    if (time >= frames[i - 1].time)
                    {
                        if (frames[i].name == "start")
                        {
                            if (side == 'L') startShoulderLeft = new Vector3();
                            else startShoulderRight = new Vector3();
                        }
                        if (i - 1 == 0)
                        {
                            if (frames[i - 1].name == "end")
                            {
                                if (side == 'L') endShoulderLeft = new Vector3();
                                else endShoulderRight = new Vector3();
                            }
                            frames.RemoveRange(i - 1, 2);
                        }
                        else
                            frames.RemoveAt(i);
                    }
                }

                i = frames.Count - 1;
                if (i >= 1)
                {
                    if (side == 'L')
                    {
                        flagLeftShoulderUpdate = true;
                        if (frames[i - 1].name == "end")
                        {
                            positionA = SetShoulder(ref endShoulderLeft, frames[i - 1], side);
                        }
                        else
                        {
                            positionA = SetShoulder(ref startShoulderLeft, frames[i], side);
                            positionB = SetShoulder(ref startShoulderLeft, frames[i - 1], side);
                        }
                    }
                    else
                    {
                        flagRightShoulderUpdate = true;
                        if (frames[i - 1].name == "end")
                        {
                            positionA = SetShoulder(ref endShoulderRight, frames[i - 1], side);
                        }
                        else
                        {
                            positionA = SetShoulder(ref startShoulderRight, frames[i], side);
                            positionB = SetShoulder(ref startShoulderRight, frames[i - 1], side);
                        }
                    }


                    float lerpTime = (float)((time - frames[i].time) / (frames[i - 1].time - frames[i].time));

                    if (frames[i - 1].name == "end")
                    {
                        effector.positionWeight = 1 - lerpTime;
                        //effector.position = positionA;
                        if (side == 'L')
                            effector.position = Vector3.Lerp(positionA, findChild_(transform, "LeftArm").position, lerpTime); //MG
                        else
                            effector.position = Vector3.Lerp(positionA, findChild_(transform, "RightArm").position, lerpTime);
                    }
                    else
                    {
                        effector.positionWeight = 1;
                        effector.position = Vector3.Lerp(positionA, positionB, lerpTime);
                    }
                }
            }

            if (frames.Count == 0)
            {
                if (flagShoulderUpdate == true)
                {
                    effector.positionWeight = 0f;
                    if (side == 'L')
                        flagLeftShoulderUpdate = false;
                    else
                        flagRightShoulderUpdate = false;
                }
            }
        }

        public static float LinearInterpolate2D(float y1, float y2, float time)
        {
            return y1 * (1 - time) + y2 * time;
        }

        public static Vector3 CosInterpolate3D(Vector3 a, Vector3 b, float time)
        {
            float y = (1 - (float)Math.Cos(time * Math.PI)) / 2;
            return new Vector3(a.x * (1 - y) + b.x * y, a.y * (1 - y) + b.y * y, a.z * (1 - y) + b.z * y);
        }

        Vector3 SplineInterpolation(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            Vector3 pos = 0.5f * (a + (b * t) + (t * t * c) + (t * t * t * d));

            return pos;
        }

        public HandShape setHandShape(string handShape, char side)
        {
            if (handShape == "") handShape = "NONE";
            if (handTable == null)
            {
                Debug.Log("No hand shapes!!!");
                return null;
            }

            HandShape current = new HandShape();

            if (handShape == "NONE" || handShape == "LNONE" || handShape == "RNONE")
            {
                return current;
            }

            if (handTable.ContainsKey(handShape))
            {
                string otherSide = "L_";
                if (side == 'L') otherSide = "R_";
                HandShape hand = new HandShape();
                HandShape hs = handTable[handShape];
                hand.fingers = new Dictionary<string, Quaternion>();
                hand.size = hs.size;
                foreach (KeyValuePair<string, Quaternion> h in hs.fingers)
                {
                    string start = h.Key.Substring(0, 2);
                    if (start != otherSide)
                    {
                        string finger = h.Key;
                        if (h.Key[0] == side && h.Key[1] == '_') finger = h.Key.Substring(2);
                        hand.fingers.Add(finger, h.Value);
                    }
                }
                return hand;
            }
            else
            {
                Debug.Log("Hand shapes " + handShape + " does not exist");
                return current;
            }
        }

        public void LoadHandShapes(string filename)
        {
            System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");

            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(filename);

            while (reader.Read())
            {
                string name = "";
                if (reader.NodeType == XmlNodeType.Element && reader.Name.ToUpper() == "HANDSHAPE")
                {
                    HandShape hs = new HandShape();
                    hs.fingers = new Dictionary<string, Quaternion>();
                    //hand offset
                    float size_x = 0, size_y = 0, size_z = 0;
                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                    {
                        reader.MoveToAttribute(attInd);
                        if (reader.Name == "name")
                            name = reader.Value.ToUpper();
                        if (reader.Name == "size_x")
                            try { float.TryParse(reader.Value, ns, ci, out size_x); }
                            catch { }
                        if (reader.Name == "size_y")
                            try { float.TryParse(reader.Value, ns, ci, out size_y); }
                            catch { }
                        if (reader.Name == "size_z")
                            try { float.TryParse(reader.Value, ns, ci, out size_z); }
                            catch { }
                    }
                    hs.size = new Vector3(size_x, size_y, size_z);//MG

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement) break;
                        if (reader.NodeType != XmlNodeType.Element) continue;
                        string finger = reader.Name.ToUpper();
                        float x = 0, y = 0, z = 0, w = 0;
                        for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                        {
                            reader.MoveToAttribute(attInd);
                            if (reader.Name == "x")
                                try { float.TryParse(reader.Value, ns, ci, out x); }
                                catch { }
                            if (reader.Name == "y")
                                try { float.TryParse(reader.Value, ns, ci, out y); }
                                catch { }
                            if (reader.Name == "z")
                                try { float.TryParse(reader.Value, ns, ci, out z); }
                                catch { }
                            if (reader.Name == "w")
                                try { float.TryParse(reader.Value, ns, ci, out w); }
                                catch { }
                        }
                        hs.fingers.Add(finger, new Quaternion(x, y, z, w));
                    }

                    HandShape handLeft = new HandShape();
                    HandShape handRight = new HandShape();
                    handLeft.fingers = new Dictionary<string, Quaternion>();
                    handLeft.size = hs.size;
                    handRight.fingers = new Dictionary<string, Quaternion>();
                    handRight.size = hs.size;
                    foreach (KeyValuePair<string, Quaternion> h in hs.fingers)
                    {
                        string start = h.Key.Substring(0, 2);
                        if (start != "R_")
                        {
                            string finger = h.Key;
                            if (h.Key[0] == 'L' && h.Key[1] == '_') finger = h.Key.Substring(2);
                            handLeft.fingers.Add(finger, h.Value);
                        }
                        if (start != "L_")
                        {
                            string finger = h.Key;
                            if (h.Key[0] == 'R' && h.Key[1] == '_') finger = h.Key.Substring(2);
                            handRight.fingers.Add(finger, h.Value);
                        }
                    }
                    handTable.Add('L' + name, handLeft);
                    handTable.Add('R' + name, handRight);
                }
            }
            handTable.Add("LNONE", new HandShape());
            handTable.Add("RNONE", new HandShape());
            //for debug
            //printHandTable(handTable);
        }

        private void printHandTable(Dictionary<string, HandShape> handTable)
        {
            Debug.Log("HAND SHAPES");
            foreach (KeyValuePair<string, HandShape> bs in handTable)
            {
                Debug.Log("hand shape name: " + bs.Key + " offset " + bs.Value.size);
                foreach (KeyValuePair<string, Quaternion> fs in bs.Value.fingers)
                {
                    Debug.Log("\tfinger:" + fs.Key + " vector: " + fs.Value);
                }
            }
        }
    }

    //OLD HEAD ANIMATION CODE octobre 2022
 /* private void MoveHead(bool dir, float time, float lerpTimeHead, string from, string to, float amount, bool gaze, bool start)//MGtest dir non utilisé, but? la différence vient de headDirection et headAction
    { //TO DO (Elisabetta): correct the code to be able to add head signals => function to be modified

        Vector3 rotation_from = new Vector3(0, 0, 0);
        Vector3 rotation_to = new Vector3(0, 0, 0);
        Debug.Log("ICI!!!!!");
        Vector3 translation_from = new Vector3(0, 0, 0);//MG lire directement la valeur de la tranlation (defaut prendre position tete actuelle?)
        Vector3 translation_to = new Vector3(0, 0, 0);//MGtest
                                                      //Debug.Log("head value " + head.localRotation.eulerAngles + " " + initHead.localRotation.eulerAngles);
                                                      //MGtest si phase start, copier valeur position et translation de head dans head init
        if (start == true)// /!\ si action prise en cours (on passe sur une autre, est-ce que ça gene? peut etre/sûrement car non prévu sauf si passe pas par start)
        {
            initHead.localPosition = head.localPosition;
            initHead.localRotation = head.localRotation;
        }
        //else to_change=true;//MGtest

        if (from == "" && to != "")
        {
            if (headDirections_.ContainsKey(to))
            {
                //rotation_to = headDirections_[to];
                rotation_to = headDirections_[to].Item1;// *amount;//MGtest
                translation_to = headDirections_[to].Item2;// *amount;//MGtest
            }
        }

        if (to != "" && from != "")
        {
            if (headDirections_.ContainsKey(from))
            {
                //rotation_from = headDirections_[from];//MGtest
                rotation_from = headDirections_[from].Item1;// *amount;
                translation_from = headDirections_[from].Item2;// *amount;
            }
            if (headDirections_.ContainsKey(to))
            {
                // rotation_to = headDirections_[to];//MGtest
                rotation_to = headDirections_[to].Item1;// *amount;
                translation_to = headDirections_[to].Item2;// *amount;
            }
        }

        if (to == "" && from != "")
        {
            if (headDirections_.ContainsKey(from))
            {
                //rotation_from = headDirections_[from];//MGtest
                rotation_from = headDirections_[from].Item1;// *amount;
                translation_from = headDirections_[from].Item2;// *amount;
            }
        }

        if (gaze)
            amount *= 0.8f;
*/

        //MGtest pour faire come un MERGE
        //head.localPosition =initHead.localPosition+ Vector3.Lerp(translation_from, translation_to, lerpTimeHead); //MG
        //if (translation_from == new Vector3(0, 0, 0)) translation_from = head.transform.localPosition;
        //if (translation_to == new Vector3(0, 0, 0)) translation_from = head.transform.localPosition;
        //MGtest : appliquer la translation et revoir la rotation car tourne autour d'un axe qui tourne
//        head.localPosition += Vector3.Lerp(translation_from, translation_to, lerpTimeHead); //MG
                                                                                            //       Debug.Log(start+"  "+from+"  "+to+"  "+translation_from + "  " + translation_to + "  " + Vector3.Lerp(translation_from, translation_to, lerpTimeHead));
        /* Quaternion angle = Quaternion.Slerp(Quaternion.Euler(0, 0, rotation_from.z * amount), Quaternion.Euler(0, 0, rotation_to.z * amount), lerpTimeHead);
         angle *= Quaternion.Slerp(Quaternion.Euler(rotation_from.x * amount, 0, 0), Quaternion.Euler(rotation_to.x * amount, 0, 0), lerpTimeHead);
         angle *= Quaternion.Slerp(Quaternion.Euler(0, rotation_from.y * amount, 0), Quaternion.Euler(0, rotation_to.y * amount, 0), lerpTimeHead);
         */

        //MGtest the angle to be applied has to be done auround static axis (the original head axis)
        // if (start == true) Debug.Log("start "+ initHead.transform.rotation.eulerAngles+"  "+initHead.TransformDirection(rotation_from)+"  "+ initHead.TransformDirection(rotation_to));
        //Debug.Log("from " + rotation_from + " to " + rotation_to + " step " + Vector3.Lerp(rotation_from, rotation_to, lerpTimeHead));
/*        Vector3 world_angleFrom = start == true ? initHead.TransformDirection(rotation_from) : initHead.TransformDirection(rotation_from * amount);
        Vector3 world_angleTo = initHead.TransformDirection(rotation_to * amount);
        Vector3 world_angleStep = Vector3.Lerp(world_angleFrom, world_angleTo, lerpTimeHead);
        Vector3 angleToApply = head.InverseTransformDirection(world_angleStep);
        Quaternion angle = Quaternion.Euler(angleToApply);
        head.localRotation *= angle;

        if (gaze)
        {
            eyes[0].localRotation *= Quaternion.Inverse(angle);
            eyes[1].localRotation *= Quaternion.Inverse(angle);
        }
    }
*/

    //TEST MG FOR POINTING
    /*  public Quaternion PointingWristRotation(Vector3 pointingPosition, Transform cible, char side)//MG : Adapté de version des gestes#2
      {
          float angleX = 0.0f;
          float angleY = 0.0f;
          float angleZ = 0.0f;
          float pointing_wrist_angle = 0; // 90.0f;

          //Transform hand = agent_.findChild(agent_.agent.transform, "LeftHand");
          Transform hand = GameObject.Find("LeftHand").transform;
          //if (side == 'R') hand = agent_.findChild(agent_.agent.transform, "RightHand");
          if (side == 'R') hand = GameObject.Find("RightHand").transform;

          //Transform cible = GameObject.Find(target).transform;//MG mercredi
          if (cible.position == pointingPosition)
          { //MG mercredi GetAbsoluteSwivelPosition(2)
              if (side == 'L') pointingPosition = transform.TransformPoint(GetAbsoluteSwivelPosition(2));// correspond au armswivel init
              else pointingPosition = transform.TransformPoint(GetAbsoluteSwivelPosition(-2));// correspond au armswivel init
          }
          //position qu'à le poignet en se collant à l'effecteur non tourné (? A VERIFIER)
          // Matrix4x4 handrotinit = (side=='L'? Matrix4x4.Rotate(agent_transform.rotation)*Matrix4x4.Rotate(Quaternion.Euler(wristInitLeftRot)): Matrix4x4.Rotate(agent_transform.rotation)*Matrix4x4.Rotate(Quaternion.Euler(wristInitRightRot)));//Le repère diffère de 180° en X selon la main choisie 
          Matrix4x4 handrotinit = (side == 'L' ? Matrix4x4.Rotate(transform.rotation) * Matrix4x4.Rotate(StartLeftHandRotation) : Matrix4x4.Rotate(transform.rotation) * Matrix4x4.Rotate(StartRightHandRotation));
          //angleX = (side == 'L' ? -pointing_wrist_angle:pointing_wrist_angle);//MG mercredi
          angleX = pointing_wrist_angle;//MG mercredi
          Matrix4x4 rotX = handrotinit * Matrix4x4.Rotate(Quaternion.Euler(angleX, 0.0f, 0.0f)) * handrotinit.inverse;

          //Matrix corresponding to the rotation around the Y hand axis to point the target (angle between hand position and target), expressed in the global axis
          Matrix4x4 targethand = (rotX * handrotinit).inverse * Matrix4x4.Translate(cible.position - pointingPosition) * (rotX * handrotinit);

          //MG mercredi
          // Cas où l'objet est derriere non traité (le pointage est annulé?)
          // int sign = ((cible.position.z - GameObject.Find(agent_transform.gameObject.name + "master").transform.position.z) >= 0.0 ? 0 : 1);Debug.Log("sign :"+sign);
          //Debug.Log(targethand.m03 + "  " + targethand.m13 + "  " + targethand.m23);
          angleY = -(float)Math.Atan((targethand.m23) / (targethand.m03)) * 180.0f / (float)Math.PI;

          //Corrections liées au artan (car tan à un modif répétitif), déduit des tests, devrait se calculer en fonction des quadrants //MG mercredi
          if (side == 'L' && targethand.m03 < 0) angleY += 180.0f;
          else if (side == 'R' && targethand.m03 > 0) angleY += 180.0f;

          //The angle is defined for the hand, but the index is a little bit translated from this axis => We have to compensate
          //The compensation is a compromise between the arm/hand and index axes because the overall aspect is more important than only the index direction (therefore it is not optimal)
          float anglecompY = -8f;
          Matrix4x4 rotY = (rotX * handrotinit) * Matrix4x4.Rotate(Quaternion.Euler(0.0f, angleY - anglecompY, 0.0f)) * (rotX * handrotinit).inverse;

          //Matrix corresponding to the rotation around the Z hand axis to point the target (angle between hand position and target), expressed in the global axis
          Matrix4x4 targethand2 = (rotY * rotX * handrotinit).inverse * Matrix4x4.Translate(cible.position - pointingPosition) * (rotY * rotX * handrotinit);
          //int sign2 = ((cible.position.x - GameObject.Find(agent_transform.gameObject.name + "master").transform.position.x) >= 0.0 ? 1 : 0); Debug.Log("sign2 :" + sign2);
          angleZ = (float)Math.Atan((targethand2.m13) / (targethand2.m03)) * 180.0f / (float)Math.PI;// + sign2*180;// 
          float anglecompZ = hand.GetChild(0).localRotation.eulerAngles.z;//Mettre une valeur arbitraire si besoin
          Matrix4x4 rotZ = (rotY * rotX * handrotinit) * Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, angleZ - anglecompZ)) * (rotY * rotX * handrotinit).inverse;

          //Final rotation
          Matrix4x4 rot = Matrix4x4.Rotate(transform.rotation).inverse * (rotZ * rotY * rotX * handrotinit);
          Vector3 angles = rot.rotation.eulerAngles;
          for (int i = 0; i < 3; i++)
          {
              if (angles[i] > 180) angles[i] -= 360f;
              else if (angles[i] <= -180) angles[i] += 360f;
          }

          return Quaternion.Euler(angles.x, angles.y, angles.z);
      }*/
    /*  EXAMPLE OF HEAD EFFECTOR
        private FBBIKHeadEffector headEffector;
        GameObject headEffectorGO;
        // Creating the head direction effector GameObject
        headEffectorGO = GameObject.CreatePrimitive(PrimitiveType.Cube); //new GameObject();
        headEffectorGO.transform.localScale = new Vector3(0.05f, 0.15f, 0.15f);
        headEffectorGO.GetComponent<Renderer>().material.color = new Color(1, 1, 0, 1);
        headEffectorGO.name = ik.gameObject.name + " Head Direction Effector";
        headEffectorGO.transform.position = references.head.position + new Vector3(0, 0, 0);
        headEffectorGO.transform.rotation = references.head.rotation;
        headEffectorGO.transform.SetParent(references.head);
        headEffectorGO.SetActive(true);

        // Adding the FBBIKHeadEffector script
        headEffector = headEffectorGO.AddComponent<FBBIKHeadEffector>();
        headEffector.ik = ik;

        // Assigning bend bones (just realized I need to make a constructor for FBBIKHeadEffector.BendBone)
        FBBIKHeadEffector.BendBone spine = new FBBIKHeadEffector.BendBone();
        spine.transform = agent_.findChild(agent_.agent.transform, "Spine1"); // references.spine[0];
        spine.weight = 1f;

        FBBIKHeadEffector.BendBone chest = new FBBIKHeadEffector.BendBone();
        chest.transform = agent_.findChild(agent_.agent.transform, "Spine2"); // references.spine[1];
        chest.weight = 0.75f;

        FBBIKHeadEffector.BendBone neck = new FBBIKHeadEffector.BendBone();
        neck.transform = agent_.findChild(agent_.agent.transform, "Neck");  //references.head;
        neck.weight = 1f;

        headEffector.bendBones = new FBBIKHeadEffector.BendBone[3] {
            neck,
            spine,
            chest
        };

        // Set weights
        headEffector.bendWeight = 0f;
        headEffector.positionWeight = 0f;
        headEffector.rotationWeight = 1f;
     */
}
