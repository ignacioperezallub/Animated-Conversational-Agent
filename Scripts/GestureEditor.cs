using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Collections.Generic;
using RootMotion.FinalIK;
using System;


// Example script with properties.
public class GestureCreator : MonoBehaviour
{
    public VirtualAgent.Character agent;
    public Animation.UnityAnimationEngine ae;

    public int leftRadialOrientation;
    public int leftHeight;
    public int leftDistance;
    public int leftBendConstrainGoal;
    public float leftROunit, leftHunit, leftDunit, leftSunit;
    public float leftWristXOrientation;
    public float leftWristYOrientation;
    public float leftWristZOrientation;

    public int rightRadialOrientation;
    public int rightHeight;
    public int rightDistance;
    public int rightBendConstrainGoal;
    public float rightROunit, rightHunit, rightDunit, rightSunit;
    public float rightWristXOrientation;
    public float rightWristYOrientation;
    public float rightWristZOrientation;

    public bool leftInteractWithObject = false;
    public bool rightInteractWithObject = false;

    public string[] leftTargetOffset = new string[] { "0", "0", "0" };
    public string[] rightTargetOffset = new string[] { "0", "0", "0" };

    public Quaternion initLeftWristOrientation;
    public Quaternion initRightWristOrientation;

    public Transform leftHandParent, rightHandParent;

    public VirtualAgent.GestureDescription gd;

    public Animation.HandShape leftHandShape;
    public Animation.HandShape rightHandShape;

    public FullBodyBipedIK fbbik;

    public RuntimeAnimatorController controller;//MG
    private int nb = 0;

    public Quaternion left = Quaternion.identity, right = Quaternion.identity;

    public bool recompute = true;


    public void init(VirtualAgent.Character a)
    {
        agent = a;
        ae = (Animation.UnityAnimationEngine)a.animationEngine;
        gd = new VirtualAgent.GestureDescription();
        gd.phases = new List<VirtualAgent.Relative_Phase>();
        leftHandShape = new Animation.HandShape();
        rightHandShape = new Animation.HandShape();
        fbbik = ae.GetComponent<FullBodyBipedIK>();
        leftHandParent = ae.findChild_(ae.transform, "LeftForeArm");
        rightHandParent = ae.findChild_(ae.transform, "RightForeArm");
        controller = ae.GetComponent<Animator>().runtimeAnimatorController;
        initAttr();
    }

    public void initAttr(bool left = true, bool right = true)
    {
        if (left)
        {
            leftHeight = (int)VirtualAgent.Relative_Hand_Position.Height.BELT;
            leftDistance = (int)VirtualAgent.Relative_Hand_Position.Distance.CLOSE;
            leftRadialOrientation = (int)VirtualAgent.Relative_Hand_Position.RadialOrientation.SIDE;
            leftBendConstrainGoal = (int)VirtualAgent.Relative_Hand_Position.ArmSwivel.NORMAL;
            leftROunit = 0;
            leftHunit = 0;
            leftDunit = 0;
            leftSunit = 0;
            leftWristXOrientation = 0; 
            leftWristYOrientation = 0;
            leftWristZOrientation = 0;

            leftInteractWithObject = false;
            for (int i = 0; i < 3; ++i) leftTargetOffset[i] = "0";
        }

        if (right)
        {
            rightHeight = (int)VirtualAgent.Relative_Hand_Position.Height.BELT;
            rightDistance = (int)VirtualAgent.Relative_Hand_Position.Distance.CLOSE;
            rightRadialOrientation = -(int)VirtualAgent.Relative_Hand_Position.RadialOrientation.SIDE;
            rightBendConstrainGoal = -(int)VirtualAgent.Relative_Hand_Position.ArmSwivel.NORMAL;
            rightROunit = 0;
            rightHunit = 0;
            rightDunit = 0;
            rightSunit = 0;

            rightWristXOrientation = 0;
            rightWristYOrientation = 0;
            rightWristZOrientation = 0;

            rightInteractWithObject = false;
            for (int i = 0; i < 3; ++i) rightTargetOffset[i] = "0";
        }
    }

    private void LateUpdate()
    {

        if (agent.scheduler.Sched.Empty)
        {
            if (leftHandShape.fingers != null)
            {
                foreach (KeyValuePair<string, Quaternion> fs in leftHandShape.fingers)
                {
                    string finger = fs.Key;
                    if (ae.leftHandFingers.ContainsKey(finger))
                        ae.leftHandFingers[finger].localRotation = fs.Value;
                }
            }

            if (rightHandShape.fingers != null)
            {
                foreach (KeyValuePair<string, Quaternion> fs in rightHandShape.fingers)
                {
                    string finger = fs.Key;
                    if (ae.rightHandFingers.ContainsKey(finger))
                        ae.rightHandFingers[finger].localRotation = fs.Value;
                }
            }
        }
        else
        {
            leftHandShape.fingers = null;
            rightHandShape.fingers = null;
        }
    }

    void Update()
    {
        if (nb == 10)
        {
            initLeftWristOrientation = ae.findChild_(ae.transform, "LeftHand").rotation;
            initRightWristOrientation = ae.findChild_(ae.transform, "RightHand").rotation;
        }
        if (nb < 11) nb++;

        if (Time.time > 0.5 && recompute == true)
        {
            initAttr();
            recompute = false;
        }

        if (agent.scheduler.Sched.Empty && nb > 10)
        {
            //Quaternion askedLeftRot = new Quaternion(leftWristXOrientation, leftWristYOrientation, leftWristZOrientation, 0.1f);
            //Quaternion askedRightRot = new Quaternion(rightWristXOrientation, rightWristYOrientation, rightWristZOrientation, 0.1f);
            //Quaternion worldRotation = transform.parent.rotation * localRotation;
            /* Quaternion askedLeftRot = Quaternion.Euler(leftWristXOrientation, leftWristYOrientation, leftWristZOrientation);
             Quaternion askedRightRot = Quaternion.Euler(rightWristXOrientation, rightWristYOrientation, rightWristZOrientation);

             fbbik.solver.leftHandEffector.rotationWeight = 1f;
             fbbik.solver.rightHandEffector.rotationWeight = 1f;
             fbbik.solver.leftHandEffector.rotation = initLeftWristOrientation * askedLeftRot;
             fbbik.solver.rightHandEffector.rotation = initRightWristOrientation * askedRightRot;*/

            //V1
            // Quaternion askedLeftRot = Quaternion.Euler(leftWristXOrientation, leftWristYOrientation, leftWristZOrientation);
            //Quaternion askedRightRot = Quaternion.Euler(rightWristXOrientation, rightWristYOrientation, rightWristZOrientation);

            //V2
            Matrix4x4 rotLX = Matrix4x4.Rotate(initLeftWristOrientation) * Matrix4x4.Rotate(Quaternion.Euler(new Vector3(leftWristXOrientation, 0.0f, 0.0f))) * Matrix4x4.Rotate(initLeftWristOrientation).inverse;
            Matrix4x4 rotLY = (rotLX * Matrix4x4.Rotate(initLeftWristOrientation)) * Matrix4x4.Rotate(Quaternion.Euler(new Vector3(0.0f, leftWristYOrientation, 0.0f))) * (rotLX * Matrix4x4.Rotate(initLeftWristOrientation)).inverse;
            Matrix4x4 rotLZ = (rotLY * rotLX * Matrix4x4.Rotate(initLeftWristOrientation)) * Matrix4x4.Rotate(Quaternion.Euler(new Vector3(0.0f, 0.0f, leftWristZOrientation))) * (rotLY * rotLX * Matrix4x4.Rotate(initLeftWristOrientation)).inverse;

            Matrix4x4 rotRX = Matrix4x4.Rotate(initRightWristOrientation) * Matrix4x4.Rotate(Quaternion.Euler(new Vector3(rightWristXOrientation, 0.0f, 0.0f))) * Matrix4x4.Rotate(initRightWristOrientation).inverse;
            Matrix4x4 rotRY = (rotRX * Matrix4x4.Rotate(initRightWristOrientation)) * Matrix4x4.Rotate(Quaternion.Euler(new Vector3(0.0f, rightWristYOrientation, 0.0f))) * (rotRX * Matrix4x4.Rotate(initRightWristOrientation)).inverse;
            Matrix4x4 rotRZ = (rotRY * rotRX * Matrix4x4.Rotate(initRightWristOrientation)) * Matrix4x4.Rotate(Quaternion.Euler(new Vector3(0.0f, 0.0f, rightWristZOrientation))) * (rotRY * rotRX * Matrix4x4.Rotate(initRightWristOrientation)).inverse;


            //Quaternion askedLeftRot 
            left = (rotLZ * rotLY * rotLX * Matrix4x4.Rotate(initLeftWristOrientation)).rotation;
            //Quaternion askedRightRot 
            right = (rotRZ * rotRY * rotRX * Matrix4x4.Rotate(initRightWristOrientation)).rotation;


            ae.ik.solver.leftHandEffector.rotationWeight = 1f;
            ae.ik.solver.rightHandEffector.rotationWeight = 1f;
            //V1
            /*fbbik.solver.leftHandEffector.rotation = initLeftWristOrientation * askedLeftRot;
            fbbik.solver.rightHandEffector.rotation = initRightWristOrientation * askedRightRot;*/
            //V2
            ae.ik.solver.leftHandEffector.rotation = left; // askedLeftRot;
            ae.ik.solver.rightHandEffector.rotation = right; // askedRightRot;

            ae.ik.solver.leftHandEffector.position = transform.TransformPoint(ae.GetAbsoluteWristPosition(leftRadialOrientation, leftHeight, leftDistance,
                                                                                             leftROunit, leftHunit, leftDunit));
            ae.ik.solver.leftHandEffector.positionWeight = 1f;
            ae.ik.solver.rightHandEffector.position = transform.TransformPoint(ae.GetAbsoluteWristPosition(rightRadialOrientation, rightHeight, rightDistance, 
                                                                                              rightROunit, rightHunit, rightDunit));
            ae.ik.solver.rightHandEffector.positionWeight = 1f;

            ae.ik.solver.leftArmChain.bendConstraint.bendGoal.position = transform.TransformPoint(ae.GetAbsoluteSwivelPosition(leftBendConstrainGoal, leftSunit));
            ae.ik.solver.leftArmChain.bendConstraint.weight = 0.5f;
            ae.ik.solver.rightArmChain.bendConstraint.bendGoal.position = transform.TransformPoint(ae.GetAbsoluteSwivelPosition(rightBendConstrainGoal, rightSunit));
            ae.ik.solver.rightArmChain.bendConstraint.weight = 0.5f;
        }
    }

    public VirtualAgent.Quaternion GetVirtualAgentQuaternion(Quaternion rotation)
    {
        return new VirtualAgent.Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
    }

    public void PlayGesture(float duration, bool useThigh)
    {
        string bml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
        bml += "<bml xmlns=\"http://www.bml-initiative.org/bml/bml-1.0\" xmlns:ext=\"http://www.bml-initiative.org/bml/coreextensions-1.0\" id=\"bml1\" characterId=\"" + agent.name + "\" composition=\"APPEND\">\n";
        bml += "<gesture id=\"ge1\" lexeme=\"newGesture\" start=\"0\"/>";
        //bml +="<gesture id = \"ge1\" lexeme = \"newGesture\" start = \"1\" end = \"4\" />";
        //bml += "<gesture id=\"ge2\" lexeme=\"openOne\" start=\"2.5\"/>";
        bml += "</bml>";

        if (agent.gestureTable.ContainsKey("newGesture"))
            agent.gestureTable.Remove("newGesture");
        gd.duration = duration;
        for (int i = 0; i < gd.phases.Count; i++)
        {
            VirtualAgent.Relative_Phase rp = gd.phases[i];
            rp.time = rp.time * duration;
            gd.phases[i] = rp;
            
            if (gd.side == "LEFT" || gd.side == "BOTH" || gd.side == "LEFT_RIGHT") gd.phases[i].initLeftWristRotation =  GetVirtualAgentQuaternion(initLeftWristOrientation);//MG
            if (gd.side == "RIGHT" || gd.side == "BOTH" || gd.side == "LEFT_RIGHT") gd.phases[i].initRightWristRotation = GetVirtualAgentQuaternion(initRightWristOrientation);//MG
        }
        gd.useThigh = useThigh;
        gd.timeMandatory = false;

        agent.gestureTable.Add("newGesture", gd);
        agent.AddBml(bml, agent.name);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GestureCreator))]
public class GestureCreatorEditor : Editor
{

    static readonly System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
    static readonly System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;

    bool useThigh = true;

    bool timeMandatory = false;
    bool shapeMandatory = false;

    float phaseTime = 0.16f;
    float duration = 1.0f;
    string dur = "1";

    public string[] radialOrientation = new string[] { "INWARD", "FRONT", "SIDE", "OUT", "FAR_OUT" };
    public string[] height = new string[] { "ABOVE_HEAD", "HEAD", "SHOULDER", "CHEST", "ABDOMEN", "BELT", "BELOW_BELT" };
    public string[] distance = new string[] { "TOUCH", "CLOSE", "NORMAL", "FAR" };
    public string[] swivel = new string[] { "TOUCH", "NORMAL", "OUT", "ORTHOGONAL" };
    // public string[] wristy = new string[] { "AWAY", "NORMAL", "TOWARD" };
    // public string[] wristz = new string[] { "OUTWARD", "NORMAL", "INWARD" };
    // public string[] wristx = new string[] { "UP", "NORMAL", "DOWN" };

    public string[] phases = new string[] { "ready", "stroke_start", "stroke", "stroke_end", "relax" };
    public string[] hands = new string[] { "Left", "Right", "Left and Right", "Both" };
    public string[] handShapes = new string[] { "NONE", "PALPATE", "PULP", "FIST", "OPEN1", "OPEN", "FUCK", "THUMBUP", "POINT1", "TWO", "THREE", "FOUR", "FIVE" };
    public int index = 0;
    public int indexHand = 0;
    public int indexLeftHandShape = 0;
    public int indexRightHandShape = 0;

    public int indexRightRadial = 2, irr = 2, indexLeftRadial = 2, ilr = 2;
    public int indexRightHeight = 5, irh = 5, indexLeftHeight = 5, ilh = 5;
    public int indexRightDistance = 1, ird = 1, indexLeftDistance = 1, ild = 1;
    public int indexRightSwivel = 1, irs = 1, indexLeftSwivel = 1, ils = 1;

    public float leftWristXUnit = 0f, rightWristXUnit = 0f;
    public float leftWristYUnit = 0f, rightWristYUnit = 0f;
    public float leftWristZUnit = 0f, rightWristZUnit = 0f;

    //MG
    float lWXUnit = 0f;
    float lWYUnit = 0f;
    float lWZUnit = 0f;    
    float rWXUnit = 0f;
    float rWYUnit = 0f;
    float rWZUnit = 0f;
    private bool[] l_rotXYZ=new bool[] {false, false, false};
    private bool[] r_rotXYZ=new bool[] {false, false, false};


    bool showLeftHand = false;
    bool showRightHand = false;

    string gestureName = "newGesture";

    string msg = "";

    public override void OnInspectorGUI()
    {
        GestureCreator gc = (GestureCreator)target;

        gestureName = EditorGUILayout.TextField("Name", gestureName);
        EditorGUILayout.BeginHorizontal();
        string dur1 = EditorGUILayout.TextField("Duration", dur);
        timeMandatory = EditorGUILayout.Toggle("Time Mandatory", timeMandatory);
        if (dur1 != dur)
        {
            changeDuration(ref gc, duration, ref dur1);
            dur = dur1;
        }
        EditorGUILayout.EndHorizontal();

        useThigh = EditorGUILayout.Toggle("Use Thigh", useThigh);
        if (useThigh == true)
            gc.fbbik.solver.bodyEffector.effectChildNodes = true;
        else
            gc.fbbik.solver.bodyEffector.effectChildNodes = false;

        EditorGUILayout.BeginHorizontal();
        int phaseIndex = EditorGUILayout.Popup("Phases", index, phases);
        if (phaseIndex != index)
        {
            index = phaseIndex;
            setPhaseTime(ref gc, phases[phaseIndex]);
        }

        if (GUILayout.Button("Add/Modify", GUILayout.MaxWidth(80)))
            AddPhase(ref gc, phases[phaseIndex]);

        if (GUILayout.Button("Remove", GUILayout.MaxWidth(70)))
            RemovePhase(ref gc, phases[phaseIndex]);
        EditorGUILayout.EndHorizontal();

        phaseTime = EditorGUILayout.Slider("Phase time", phaseTime, 0, 1);
        shapeMandatory = EditorGUILayout.Toggle("Shape Mandatory", shapeMandatory);


        EditorGUILayout.Space();

        indexHand = EditorGUILayout.Popup("Hand", indexHand, hands);

        if (indexHand == 3)
        {
            showLeftHand = EditorGUILayout.Foldout(showLeftHand, "Both Hands");
            ShowLeftHand(ref gc);
        }
        else
        {
            showLeftHand = EditorGUILayout.Foldout(showLeftHand, "Left Hand");
            ShowLeftHand(ref gc);
            EditorGUILayout.Space();
            showRightHand = EditorGUILayout.Foldout(showRightHand, "Right Hand");
            ShowRighHand(ref gc);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play Gesture"))
        {
            /*if (gc.gd.phases.Count <= 0)
                msg = "The gesture has no phases!";
                //Debug.Log("The gesture has no phases!");
            else
            {*/
            msg = "";
            ResetRight(ref gc);
            ResetLeft(ref gc);
            gc.PlayGesture(duration, useThigh);
            //}
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Gesture"))
        {
            SaveGesture(ref gc);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clean all"))
        {
            gestureName = "newGesture";
            ResetLeft(ref gc);
            ResetRight(ref gc);
            gc.gd.phases.Clear();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(msg, MessageType.None, true);
    }

    void ShowLeftHand(ref GestureCreator gc)
    {
        if (showLeftHand)
        {
            EditorGUILayout.BeginHorizontal();
            indexLeftRadial = EditorGUILayout.Popup("Radial Orientation", ilr, radialOrientation);
            if (ilr != indexLeftRadial)
            {
                ilr = indexLeftRadial;
                gc.leftRadialOrientation = indexLeftRadial + 1;
                gc.leftROunit = 0;
            }
            gc.leftROunit = EditorGUILayout.Slider(gc.leftROunit, 0, 0.9f);
            if (indexHand == 3)
            {
                gc.rightRadialOrientation = -gc.leftRadialOrientation;
                gc.rightROunit = gc.leftROunit;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            indexLeftHeight = EditorGUILayout.Popup("Height", ilh, height);
            if (ilh != indexLeftHeight)
            {
                ilh = indexLeftHeight;
                gc.leftHeight = indexLeftHeight + 1;
                gc.leftHunit = 0;
            }
            gc.leftHunit = EditorGUILayout.Slider(gc.leftHunit, 0, 0.9f);
            if (indexHand == 3)
            {
                gc.rightHeight = gc.leftHeight;
                gc.rightHunit = gc.leftHunit;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            indexLeftDistance = EditorGUILayout.Popup("Distance", ild, distance);
            if (ild != indexLeftDistance)
            {
                ild = indexLeftDistance;
                gc.leftDistance = indexLeftDistance + 1;
                gc.leftDunit = 0;
            }
            gc.leftDunit = EditorGUILayout.Slider(gc.leftDunit, 0, 0.9f);
            if (indexHand == 3)
            {
                gc.rightDistance = gc.leftDistance;
                gc.rightDunit = gc.leftDunit;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            indexLeftSwivel = EditorGUILayout.Popup("Swivel", ils, swivel);
            if (ils != indexLeftSwivel)
            {
                ils = indexLeftSwivel;
                gc.leftBendConstrainGoal = indexLeftSwivel + 1;
                gc.leftSunit = 0;
            }
            gc.leftSunit = EditorGUILayout.Slider(gc.leftSunit, 0, 0.9f);
            if (indexHand == 3)
            {
                gc.rightBendConstrainGoal = -gc.leftBendConstrainGoal;
                gc.rightSunit = gc.leftSunit;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist X");
            leftWristXUnit = EditorGUILayout.Slider(leftWristXUnit, -180, 180);
            //MG test
            if (lWXUnit != leftWristXUnit)
            {
                l_rotXYZ[0] = true;
                if (indexHand == 3)
                    r_rotXYZ[0] = true;

                if (l_rotXYZ[1] == true || l_rotXYZ[2] == true)
                {
                    gc.initLeftWristOrientation = gc.ae.findChild_(gc.ae.transform, "LeftHand").rotation;
                    ResetLeftAxes();
                    l_rotXYZ[0] = false; l_rotXYZ[1] = false; l_rotXYZ[2] = false;
                    if (indexHand == 3)
                    {
                        gc.initRightWristOrientation = gc.ae.findChild_(gc.ae.transform, "RightHand").rotation;//MG fev 2022
                        ResetRightAxes();
                        r_rotXYZ[0] = false; r_rotXYZ[1] = false; r_rotXYZ[2] = false;
                    }
                    Debug.Log("action on x");
                }
                lWXUnit = leftWristXUnit;
            }
            gc.leftWristXOrientation = leftWristXUnit;
            if (indexHand == 3)
                gc.rightWristXOrientation = leftWristXUnit;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist Y");
            leftWristYUnit = EditorGUILayout.Slider(leftWristYUnit, -180, 180);
            //MG test
            if (lWYUnit != leftWristYUnit)
            {
                l_rotXYZ[1] = true;
                if (indexHand == 3)
                    r_rotXYZ[1] = true;
                if (l_rotXYZ[2] == true)
                {
                    gc.initLeftWristOrientation = gc.ae.findChild_(gc.ae.transform, "LeftHand").rotation;
                    ResetLeftAxes();
                    l_rotXYZ[0] = false; l_rotXYZ[1] = false; l_rotXYZ[2] = false;
                    if (indexHand == 3)
                    {
                        gc.initRightWristOrientation = gc.ae.findChild_(gc.ae.transform, "RightHand").rotation;//MG fev 2022
                        ResetRightAxes();
                        r_rotXYZ[0] = false; r_rotXYZ[1] = false; r_rotXYZ[2] = false;
                    }
                }
                lWYUnit = leftWristYUnit;
            }
            gc.leftWristYOrientation = leftWristYUnit;
            if (indexHand == 3)
                gc.rightWristYOrientation = leftWristYUnit;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist Z");
            leftWristZUnit = EditorGUILayout.Slider(leftWristZUnit, -180, 180);
            //MG
            if (lWZUnit != leftWristZUnit)
            {
                l_rotXYZ[2] = true;
                if (indexHand == 3)
                    r_rotXYZ[2] = true;
                lWZUnit = leftWristZUnit;
            }
            gc.leftWristZOrientation = leftWristZUnit;
            if (indexHand == 3)
                gc.rightWristZOrientation = leftWristZUnit;
            EditorGUILayout.EndHorizontal();

            int ihs = EditorGUILayout.Popup("Hand Shape", indexLeftHandShape, handShapes);
            if (ihs != indexLeftHandShape)
            {
                indexLeftHandShape = ihs;
                changeHandShape(ref gc, 'L', indexLeftHandShape);
                if (indexHand == 3)
                {
                    indexRightHandShape = indexLeftHandShape;
                    changeHandShape(ref gc, 'R', indexRightHandShape);
                }
            }

            gc.leftInteractWithObject = EditorGUILayout.Toggle("Interact with object", gc.leftInteractWithObject);
            gc.leftTargetOffset[0] = EditorGUILayout.TextField("Radial orientation offset", gc.leftTargetOffset[0]);
            gc.leftTargetOffset[1] = EditorGUILayout.TextField("Height offset", gc.leftTargetOffset[1]);
            gc.leftTargetOffset[2] = EditorGUILayout.TextField("Distance offset", gc.leftTargetOffset[2]);
            
            /*** MG fev 2022    (utile?) ***/
            if (indexHand == 3)
            {
                gc.rightInteractWithObject = EditorGUILayout.Toggle("Interact with object", gc.rightInteractWithObject);
                gc.rightTargetOffset[0] = EditorGUILayout.TextField("Radial orientation offset", gc.rightTargetOffset[0]);
                gc.rightTargetOffset[1] = EditorGUILayout.TextField("Height offset", gc.rightTargetOffset[1]);
                gc.rightTargetOffset[2] = EditorGUILayout.TextField("Distance offset", gc.rightTargetOffset[2]);
            }
            /****************************************/

            /*if (GUILayout.Button("ResetAxes", GUILayout.MaxWidth(70)))
            {
                ResetLeftAxes(ref gc);
                if (indexHand == 3)
                    ResetRightAxes(ref gc);
            }*/

            if (GUILayout.Button("Reset", GUILayout.MaxWidth(70)))
            {
                ResetLeft(ref gc);
                if (indexHand == 3)
                    ResetRight(ref gc);
            }
        }
    }

    void ShowRighHand(ref GestureCreator gc)
    {
        if (showRightHand)
        {
            EditorGUILayout.BeginHorizontal();
            indexRightRadial = EditorGUILayout.Popup("Radial Orientation", irr, radialOrientation);
            if (irr != indexRightRadial)
            {
                irr = indexRightRadial;
                gc.rightRadialOrientation = -(indexRightRadial + 1);
                gc.rightROunit = 0;
            }
            gc.rightROunit = EditorGUILayout.Slider(gc.rightROunit, 0, 0.9f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            indexRightHeight = EditorGUILayout.Popup("Height", irh, height);
            if (irh != indexRightHeight)
            {
                irh = indexRightHeight;
                gc.rightHeight = indexRightHeight + 1;
                gc.rightHunit = 0;
            }
            gc.rightHunit = EditorGUILayout.Slider(gc.rightHunit, 0, 0.9f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            indexRightDistance = EditorGUILayout.Popup("Distance", ird, distance);
            if (ird != indexRightDistance)
            {
                ird = indexRightDistance;
                gc.rightDistance = indexRightDistance + 1;
                gc.rightDunit = 0;
            }
            gc.rightDunit = EditorGUILayout.Slider(gc.rightDunit, 0, 0.9f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Vector3 sunit = new Vector3(0, 0, 0);
            indexRightSwivel = EditorGUILayout.Popup("Swivel", irs, swivel);
            if (irs != indexRightSwivel)
            {
                irs = indexRightSwivel;
                gc.rightBendConstrainGoal = -(indexRightSwivel + 1);
                gc.rightSunit = 0;
            }
            gc.rightSunit = EditorGUILayout.Slider(gc.rightSunit, 0, 0.9f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist X");
            rightWristXUnit = EditorGUILayout.Slider(rightWristXUnit, -180, 180);
            //MG
            if (rWXUnit != rightWristXUnit)
            {
                r_rotXYZ[0] = true;
                if (r_rotXYZ[1] == true || r_rotXYZ[2] == true)
                {
                    gc.initRightWristOrientation = gc.ae.findChild_(gc.ae.transform, "RightHand").rotation;
                    ResetRightAxes();
                    r_rotXYZ[0] = false; r_rotXYZ[1] = false; r_rotXYZ[2] = false;
                    Debug.Log("action on x");
                }
                rWXUnit = rightWristXUnit;
            }
            gc.rightWristXOrientation = rightWristXUnit;// * wxunit;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist Y");
            rightWristYUnit = EditorGUILayout.Slider(rightWristYUnit, -180, 180);
            //MG 
            if (rWYUnit != rightWristYUnit)
            {
                r_rotXYZ[1] = true;
                if (r_rotXYZ[2] == true)
                {
                    gc.initRightWristOrientation = gc.ae.findChild_(gc.ae.transform, "RightHand").rotation;
                    ResetRightAxes();
                    r_rotXYZ[0] = false; r_rotXYZ[1] = false; r_rotXYZ[2] = false;
                }
                rWYUnit = rightWristYUnit;
            }
            gc.rightWristYOrientation = rightWristYUnit;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wrist Z");
            rightWristZUnit = EditorGUILayout.Slider(rightWristZUnit, -180, 180);
            //MG
            if (rWZUnit != rightWristZUnit)
            {
                r_rotXYZ[2] = true;
                rWZUnit = rightWristZUnit;
            }
            gc.rightWristZOrientation = rightWristZUnit;
            EditorGUILayout.EndHorizontal();

            int ihs = EditorGUILayout.Popup("Hand Shape", indexRightHandShape, handShapes);
            if (ihs != indexRightHandShape)
            {
                indexRightHandShape = ihs;
                changeHandShape(ref gc, 'R', indexRightHandShape);
            }

            gc.rightInteractWithObject = EditorGUILayout.Toggle("Interact with object", gc.rightInteractWithObject);
            gc.rightTargetOffset[0] = EditorGUILayout.TextField("Radial orientation offset", gc.rightTargetOffset[0]);
            gc.rightTargetOffset[1] = EditorGUILayout.TextField("Height offset", gc.rightTargetOffset[1]);
            gc.rightTargetOffset[2] = EditorGUILayout.TextField("Distance offset", gc.rightTargetOffset[2]);

            /*if (GUILayout.Button("ResetAxes", GUILayout.MaxWidth(70)))
                ResetRightAxes(ref gc);*/

            if (GUILayout.Button("Reset", GUILayout.MaxWidth(70)))
                ResetRight(ref gc);
        }
    }

    void changeHandShape(ref GestureCreator gc, char side, int index)
    {
        Animation.HandShape hand;
        if(handShapes[index] == "NONE")
            hand = gc.ae.setHandShape("NONE", side);
        else
            hand = gc.ae.setHandShape((side == 'L' ? 'L' : 'R') + handShapes[index], side);

        if (side == 'L' && gc.ae.handTable.ContainsKey('L'+handShapes[index]))
        {
            gc.leftHandShape.fingers = hand.fingers; // gc.agent.handTable[handShapes[indexLeftHandShape]].fingers;
        }

        if (side == 'R' && gc.ae.handTable.ContainsKey('R'+handShapes[index]))
        {
            gc.rightHandShape.fingers = hand.fingers; // gc.agent.handTable[handShapes[indexRightHandShape]].fingers;
        }
    }

    void changeDuration(ref GestureCreator gc, float oldDuration, ref string newDuration)
    {
        try
        {
            duration = (float)Double.Parse(newDuration);
        }
        catch
        {
            duration = oldDuration;
            newDuration = oldDuration.ToString();
            return;
        }
    }

        /*void ResetLeftAxes(ref GestureCreator gc)
        {
            gc.initLeftWristOrientation = gc.ae.findChild_(gc.ae.transform, "LeftHand").rotation;
            leftWristXUnit = 0f;
            leftWristYUnit = 0f;
            leftWristZUnit = 0f;
        }*/

    //MG 
    void ResetLeftAxes()
    {
        leftWristXUnit = 0f;
        leftWristYUnit = 0f;
        leftWristZUnit = 0f;
    }

        /*void ResetRightAxes(ref GestureCreator gc)
        {
            gc.initRightWristOrientation = gc.ae.findChild_(gc.ae.transform, "RightHand").rotation;
            rightWristXUnit = 0f;
            rightWristYUnit = 0f;
            rightWristZUnit = 0f;
        }*/

        //MG
    void ResetRightAxes()
    {
        rightWristXUnit = 0f;
        rightWristYUnit = 0f;
        rightWristZUnit = 0f;
    }

    void ResetLeft(ref GestureCreator gc)
    {
        gc.initAttr(true, false);
        indexLeftRadial = ilr = gc.leftRadialOrientation - 1;
        indexLeftHeight = ilh = gc.leftHeight - 1;
        indexLeftDistance = ild = gc.leftDistance - 1;
        indexLeftSwivel = ils = gc.leftBendConstrainGoal - 1;

        leftWristXUnit = 0f;
        leftWristYUnit = 0f;
        leftWristZUnit = 0f;

        indexLeftHandShape = 0;
        changeHandShape(ref gc, 'L', indexLeftHandShape);

    }

    void ResetRight(ref GestureCreator gc)
    {
        gc.initAttr(false, true);
        indexRightRadial = irr = -(gc.rightRadialOrientation - 1);
        indexRightHeight = irh = gc.rightHeight - 1;
        indexRightDistance = ird = gc.rightDistance - 1;
        indexRightSwivel = irs = -(gc.rightBendConstrainGoal - 1);

        rightWristXUnit = 0f;
        rightWristYUnit = 0f;
        rightWristZUnit = 0f;

        indexRightHandShape = 0;
        changeHandShape(ref gc, 'R', indexRightHandShape);

    }

    int FindIndex(string[] str, string n)
    {
        for (int i = 0; i < str.Length; i++)
            if (str[i] == n) return i;
        return -1;
    }

    void setPhaseTime(ref GestureCreator gc, string name)
    {
        switch (index)
        {
            case 0:
                phaseTime = 0.16f; break;
            case 1:
                phaseTime = 0.33f; break;
            case 2:
                phaseTime = 0.5f; break;
            case 3:
                phaseTime = 0.66f; break;
            case 4:
                phaseTime = 0.83f; break;
            default:
                Debug.LogError("Unrecognized Option"); return;
        }

        int i = gc.gd.phases.FindIndex(x => x.name == name);
        if (i >= 0)
        {
            ResetLeft(ref gc);
            ResetRight(ref gc);
            VirtualAgent.Relative_Phase phd = gc.gd.phases[i];
            duration = (float)gc.gd.duration;
            phaseTime = (float)phd.time;
            if (phd.leftHand != null)
            {
                indexLeftRadial = ilr = (int)phd.leftHand.radialOrientation - 1;
                gc.leftRadialOrientation = (int)phd.leftHand.radialOrientation;
                gc.leftROunit = phd.leftHand.radialOrientationPercentage;

                indexLeftDistance = ild = (int)phd.leftHand.distance - 1;
                gc.leftDistance = (int)phd.leftHand.distance;
                gc.leftDunit = phd.leftHand.distancePercentage;

                indexLeftHeight = ilh = (int)phd.leftHand.height - 1;
                gc.leftHeight = (int)phd.leftHand.height;
                gc.leftHunit = phd.leftHand.heightPercentage;

                indexLeftSwivel = ils = (int)phd.leftHand.armSwivel - 1;
                gc.leftBendConstrainGoal = (int)phd.leftHand.armSwivel;
                gc.leftSunit = phd.leftHand.armSwivelPercentage;

                leftWristXUnit = phd.leftHand.wristX;
                leftWristYUnit = phd.leftHand.wristY;
                leftWristZUnit = phd.leftHand.wristZ;

                indexLeftHandShape = FindIndex(handShapes, phd.leftHand.handShape);
                changeHandShape(ref gc, 'L', indexLeftHandShape);
            }
            if (phd.rightHand != null)
            {
                indexRightRadial = irr = (int)phd.rightHand.radialOrientation - 1;
                gc.rightRadialOrientation = -(int)phd.rightHand.radialOrientation;
                gc.rightROunit = phd.rightHand.radialOrientationPercentage;

                indexRightDistance = ird = (int)phd.rightHand.distance - 1;
                gc.rightDistance = (int)phd.rightHand.distance;
                gc.rightDunit = phd.rightHand.distancePercentage;

                indexRightHeight = irh = (int)phd.rightHand.height - 1;
                gc.rightHeight = (int)phd.rightHand.height;
                gc.rightHunit = phd.rightHand.heightPercentage;

                indexRightSwivel = irs = (int)phd.rightHand.armSwivel - 1;
                gc.rightBendConstrainGoal = -(int)phd.rightHand.armSwivel;
                gc.rightSunit = phd.rightHand.armSwivelPercentage;

                rightWristXUnit = phd.rightHand.wristX;
                rightWristYUnit = phd.rightHand.wristY;
                rightWristZUnit = phd.rightHand.wristZ;

                indexRightHandShape = FindIndex(handShapes, phd.rightHand.handShape);
                changeHandShape(ref gc, 'R', indexRightHandShape);
            }

            switch (gc.gd.side)
            {
                case "LEFT": indexHand = 0; break;
                case "RIGHT": indexHand = 1; break;
                case "LEFT_RIGHT": indexHand = 2; break;
                case "BOTH": indexHand = 3; break;
            };
        }
    }

    void AddPhase(ref GestureCreator gc, string name)
    {
        gc.gd.duration = duration;
        int i = gc.gd.phases.FindIndex(x => x.name == name);
        VirtualAgent.Relative_Phase rph;
        if (i < 0)
            rph = new VirtualAgent.Relative_Phase();
        else
            rph = gc.gd.phases[i];

        rph.name = name;
        rph.time = phaseTime;
        rph.leftHand = null;
        rph.rightHand = null;

        if (indexHand == 0 || indexHand == 2 || indexHand == 3)
        {
            rph.leftHand = new VirtualAgent.Relative_Hand_Position();
            rph.leftHand.radialOrientation = (VirtualAgent.Relative_Hand_Position.RadialOrientation)(indexLeftRadial + 1);
            rph.leftHand.radialOrientationPercentage = gc.leftROunit;
            rph.leftHand.height = (VirtualAgent.Relative_Hand_Position.Height)(indexLeftHeight + 1);
            rph.leftHand.heightPercentage = gc.leftHunit;
            rph.leftHand.distance = (VirtualAgent.Relative_Hand_Position.Distance)(indexLeftDistance + 1);
            rph.leftHand.distancePercentage = gc.leftDunit;
            rph.leftHand.armSwivel = (VirtualAgent.Relative_Hand_Position.ArmSwivel)(indexLeftSwivel + 1);
            rph.leftHand.armSwivelPercentage = gc.leftSunit;

            rph.leftHand.wristX = gc.leftWristXOrientation;
            rph.leftHand.wristY = gc.leftWristYOrientation;
            rph.leftHand.wristZ = gc.leftWristZOrientation;

            rph.leftHand.handShape = handShapes[indexLeftHandShape];
            rph.leftHand.wristRotation = gc.GetVirtualAgentQuaternion(gc.left);

            rph.leftHand.target = gc.leftInteractWithObject;
            float.TryParse(gc.leftTargetOffset[0], ns, ci, out rph.leftHand.radialOrientationOffset);
            float.TryParse(gc.leftTargetOffset[1], ns, ci, out rph.leftHand.heightOffset);
            float.TryParse(gc.leftTargetOffset[2], ns, ci, out rph.leftHand.distanceOffset);
        }
        if (indexHand == 1 || indexHand == 2)
        {
            rph.rightHand = new VirtualAgent.Relative_Hand_Position();
            rph.rightHand.radialOrientation = (VirtualAgent.Relative_Hand_Position.RadialOrientation)(indexRightRadial + 1);
            rph.rightHand.radialOrientationPercentage = gc.rightROunit;
            rph.rightHand.height = (VirtualAgent.Relative_Hand_Position.Height)(indexRightHeight + 1);
            rph.rightHand.heightPercentage = gc.rightHunit;
            rph.rightHand.distance = (VirtualAgent.Relative_Hand_Position.Distance)(indexRightDistance + 1);
            rph.rightHand.distancePercentage = gc.rightDunit;
            rph.rightHand.armSwivel = (VirtualAgent.Relative_Hand_Position.ArmSwivel)(indexRightSwivel + 1);
            rph.rightHand.armSwivelPercentage = gc.rightSunit;

            rph.rightHand.wristX = gc.rightWristXOrientation;
            rph.rightHand.wristY = gc.rightWristYOrientation;
            rph.rightHand.wristZ = gc.rightWristZOrientation;

            rph.rightHand.handShape = handShapes[indexRightHandShape];
            rph.rightHand.wristRotation = gc.GetVirtualAgentQuaternion(gc.right);

            rph.rightHand.target = gc.rightInteractWithObject;
            float.TryParse(gc.rightTargetOffset[0], ns, ci, out rph.rightHand.radialOrientationOffset);
            float.TryParse(gc.rightTargetOffset[1], ns, ci, out rph.rightHand.heightOffset);
            float.TryParse(gc.rightTargetOffset[2], ns, ci, out rph.rightHand.distanceOffset);
        }
        if (indexHand == 3)
        {
            rph.rightHand = new VirtualAgent.Relative_Hand_Position();
            rph.rightHand.radialOrientation = (VirtualAgent.Relative_Hand_Position.RadialOrientation)(indexLeftRadial + 1);
            rph.rightHand.radialOrientationPercentage = gc.leftROunit; ;
            rph.rightHand.height = (VirtualAgent.Relative_Hand_Position.Height)(indexLeftHeight + 1);
            rph.rightHand.heightPercentage = gc.leftHunit;
            rph.rightHand.distance = (VirtualAgent.Relative_Hand_Position.Distance)(indexLeftDistance + 1);
            rph.rightHand.distancePercentage = gc.leftDunit;
            rph.rightHand.armSwivel = (VirtualAgent.Relative_Hand_Position.ArmSwivel)(indexLeftSwivel + 1);
            rph.rightHand.armSwivelPercentage = gc.leftSunit;

            rph.rightHand.wristX = gc.leftWristXOrientation;
            rph.rightHand.wristY = gc.leftWristYOrientation;
            rph.rightHand.wristZ = gc.leftWristZOrientation;

            rph.rightHand.handShape = handShapes[indexLeftHandShape];
            rph.rightHand.wristRotation = gc.GetVirtualAgentQuaternion(gc.right);

            rph.rightHand.target = gc.leftInteractWithObject;
            float.TryParse(gc.leftTargetOffset[0], ns, ci, out rph.rightHand.radialOrientationOffset);
            float.TryParse(gc.leftTargetOffset[1], ns, ci, out rph.rightHand.heightOffset);
            float.TryParse(gc.leftTargetOffset[2], ns, ci, out rph.rightHand.distanceOffset);
        }

        switch (indexHand)
        {
            case 0: gc.gd.side = "LEFT"; break;
            case 1: gc.gd.side = "RIGHT"; break;
            case 2: gc.gd.side = "LEFT_RIGHT"; break;
            case 3: gc.gd.side = "BOTH"; break;
        }

        if (i < 0)
            gc.gd.phases.Add(rph);
        else
            gc.gd.phases[i] = rph;
    }

    void RemovePhase(ref GestureCreator gc, string name)
    {
        int i = gc.gd.phases.FindIndex(x => x.name == name);
        if (i >= 0)
            gc.gd.phases.RemoveAt(i);
    }

    void SaveGesture(ref GestureCreator gc)
    {
        string path = Application.streamingAssetsPath + "\\agents\\" + gestureName + ".xml";

        gc.gd.phases.Sort(delegate (VirtualAgent.Relative_Phase p1, VirtualAgent.Relative_Phase p2)
        {
            return p1.time.CompareTo(p2.time);
        });

        StreamWriter writer = new StreamWriter(path);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        writer.WriteLine("<gestuary>");
        writer.WriteLine("\t<gesture lexeme=\"" + gestureName + "\" side=\"" + gc.gd.side + "\" timeMandatory=\"" + gc.gd.timeMandatory + "\" shapeMandatory=\"" + gc.gd.shapeMandatory + "\" useThigh=\"" + useThigh + "\" duration=\"" + duration + "\">");
        writer.WriteLine("\t<initLeftWirstOrientation x=\"" + gc.initLeftWristOrientation.x.ToString(ci) + "\" y=\"" + gc.initLeftWristOrientation.y.ToString(ci) + "\" z=\"" + gc.initLeftWristOrientation.z.ToString(ci) + "\" w=\"" + gc.initLeftWristOrientation.w.ToString(ci) + "\" /> ");//MG
        writer.WriteLine("\t<initRightWirstOrientation x=\"" + gc.initRightWristOrientation.x.ToString(ci) + "\" y=\"" + gc.initRightWristOrientation.y.ToString(ci) + "\" z=\"" + gc.initRightWristOrientation.z.ToString(ci) + "\" w=\"" + gc.initRightWristOrientation.w.ToString(ci) + "\" /> ");//MG
        foreach (VirtualAgent.Relative_Phase rp in gc.gd.phases)
        {
            writer.WriteLine("\t\t<phase type=\"" + rp.name + "\" time=\"" + (rp.time * duration).ToString(ci) + "\">");
            if (rp.leftHand != null)
            {
                /*if(gc.gd.side == "BOTH")
                    writer.WriteLine("\t\t\t<hand side=\"BOTH\">");
                else*/
                writer.WriteLine("\t\t\t<hand side=\"LEFT\" target=\"" + rp.leftHand.target + "\">");
                writer.WriteLine("\t\t\t\t<radialOrientation value=\"" + rp.leftHand.radialOrientation + "\" percentage=\"" + rp.leftHand.radialOrientationPercentage.ToString(ci) + "\"" + (rp.leftHand.target==true? " targetOffset=\"" + rp.leftHand.radialOrientationOffset.ToString(ci) + "\"" :"") + " /> ");
                writer.WriteLine("\t\t\t\t<height value=\"" + rp.leftHand.height + "\" percentage=\"" + rp.leftHand.heightPercentage.ToString(ci) + "\"" + (rp.leftHand.target == true ? " targetOffset=\"" + rp.leftHand.heightOffset.ToString(ci) + "\"" : "") + " /> ");
                writer.WriteLine("\t\t\t\t<distance value=\"" + rp.leftHand.distance + "\" percentage=\"" + rp.leftHand.distancePercentage.ToString(ci) + "\"" + (rp.leftHand.target == true ? " targetOffset=\"" + rp.leftHand.distanceOffset.ToString(ci) + "\"" : "") + " /> ");
                writer.WriteLine("\t\t\t\t<armSwivel value=\"" + rp.leftHand.armSwivel + "\" percentage=\"" + rp.leftHand.armSwivelPercentage.ToString(ci) + "\" /> ");
                writer.WriteLine("\t\t\t\t<handShape shape=\"" + rp.leftHand.handShape + "\" />");
                writer.WriteLine("\t\t\t\t<wristRotation x=\"" + rp.leftHand.wristRotation.x.ToString(ci) + "\" y=\"" + rp.leftHand.wristRotation.y.ToString(ci) +
                                                            "\" z=\"" + rp.leftHand.wristRotation.z.ToString(ci) + "\" w=\"" + rp.leftHand.wristRotation.w.ToString(ci) + "\" />");
                /*writer.WriteLine("\t\t\t\t<wristOrientation x=\"" + rp.leftHand.wristX + "\" xPercentage=\"" + rp.leftHand.wristXPercentage.ToString(ci) +
                                                            "\" y=\"" + rp.leftHand.wristY + "\" yPercentage=\"" + rp.leftHand.wristYPercentage.ToString(ci) +
                                                            "\" z=\"" + rp.leftHand.wristZ + "\" zPercentage=\"" + rp.leftHand.wristZPercentage.ToString(ci) + "\" />");*/
                writer.WriteLine("\t\t\t</hand>");
            }

            //Enregistrer pour chaque phase directement l'angle initLeftWristOrientation initRighWristOrientation et enlever la rotation par cet init dans l'engine

            if (rp.rightHand != null)// && gc.gd.side != "BOTH")
            {
                writer.WriteLine("\t\t\t<hand side=\"RIGHT\" target=\"" +  rp.rightHand.target + "\">");
                writer.WriteLine("\t\t\t\t<radialOrientation value=\"" + rp.rightHand.radialOrientation + "\" percentage=\"" + rp.rightHand.radialOrientationPercentage.ToString(ci) + "\"" + (rp.rightHand.target == true ? " targetOffset=\"" + rp.rightHand.radialOrientationOffset.ToString(ci) + "\"" : "") + " />");
                writer.WriteLine("\t\t\t\t<height value=\"" + rp.rightHand.height + "\" percentage=\"" + rp.rightHand.heightPercentage.ToString(ci) + "\"" + (rp.rightHand.target == true ? " targetOffset=\"" + rp.rightHand.heightOffset.ToString(ci) + "\"" : "") + " />");
                writer.WriteLine("\t\t\t\t<distance value=\"" + rp.rightHand.distance + "\" percentage=\"" + rp.rightHand.distancePercentage.ToString(ci) + "\"" +(rp.rightHand.target == true ? " targetOffset=\"" + rp.rightHand.distanceOffset.ToString(ci) + "\"" : "") + " />");
                writer.WriteLine("\t\t\t\t<armSwivel value=\"" + rp.rightHand.armSwivel + "\" percentage=\"" + rp.rightHand.armSwivelPercentage.ToString(ci) + "\" />");
                writer.WriteLine("\t\t\t\t<handShape shape=\"" + rp.rightHand.handShape + "\" />");
                writer.WriteLine("\t\t\t\t<wristRotation x=\"" + rp.rightHand.wristRotation.x.ToString(ci) + "\" y=\"" + rp.rightHand.wristRotation.y.ToString(ci) +
                                                            "\" z=\"" + rp.rightHand.wristRotation.z.ToString(ci) + "\" w=\"" + rp.rightHand.wristRotation.w.ToString(ci) + "\" />");
                /*writer.WriteLine("\t\t\t\t<wristOrientation x=\"" + rp.rightHand.wristX + "\" xPercentage=\"" + rp.rightHand.wristXPercentage.ToString(ci) +
                                                            "\" y=\"" + rp.rightHand.wristY + "\" yPercentage=\"" + rp.rightHand.wristYPercentage.ToString(ci) +
                                                            "\" z=\"" + rp.rightHand.wristZ + "\" zPercentage=\"" + rp.rightHand.wristZPercentage.ToString(ci) + "\" />");*/
                writer.WriteLine("\t\t\t</hand>");
            }
            writer.WriteLine("\t\t</phase>");
        }
        writer.WriteLine("\t</gesture>");
        writer.WriteLine("</gestuary>");
        writer.Close();
    }
}
#endif





