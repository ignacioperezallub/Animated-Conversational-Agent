using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Collections.Generic;
using RootMotion.FinalIK;
using System;

public class Range
{
    public float min, max;
    public Range(float mi, float ma)
    {
        min = mi; max = ma;
    }

    public float value(float p)
    {
        return min + (max - min) * p / 100f;
    }
}

public class HandShapeCreator : MonoBehaviour
{
    public VirtualAgent.Character agent_;
    public Animation.UnityAnimationEngine ae;

    public float thumb1Y = 50, thumb1Z = 50, thumb2Z = 50, thumb3Z = 50;
    public float index1Y = 50, index1Z = 50, index2Z = 50, index3Z = 50;
    public float middle1Y = 50, middle1Z = 50, middle2Z = 50, middle3Z = 50;
    public float ring1Y = 50, ring1Z = 50, ring2Z = 50, ring3Z = 50;
    public float pinky1Y = 50, pinky1Z = 50, pinky2Z = 50, pinky3Z = 50;

    Transform leftHand, rightHand;
    public Transform leftThumb1, leftIndex1, leftMiddle1, leftRing1, leftPinky1;
    public Transform rightThumb1, rightIndex1, rightMiddle1, rightRing1, rightPinky1;

    public Transform leftThumb2, leftIndex2, leftMiddle2, leftRing2, leftPinky2;
    public Transform rightThumb2, rightIndex2, rightMiddle2, rightRing2, rightPinky2;

    public Transform leftThumb3, leftIndex3, leftMiddle3, leftRing3, leftPinky3;
    public Transform rightThumb3, rightIndex3, rightMiddle3, rightRing3, rightPinky3;

    //hand distances
    public double a1, a2, a3, a4, a5;

    public Range range;

    public FullBodyBipedIK fbbik;

    public void init(VirtualAgent.Character a)
    {
        agent_ = a;
        ae = (Animation.UnityAnimationEngine)a.animationEngine;

        fbbik = ae.GetComponent<FullBodyBipedIK>();
        fbbik.solver.leftArmChain.bendConstraint.bendGoal = ae.findChild_(ae.transform, "Hips");
        fbbik.solver.rightArmChain.bendConstraint.bendGoal = ae.findChild_(ae.transform, "Hips");
        leftHand = ae.findChild_(ae.transform, "LeftHand");
        rightHand = ae.findChild_(ae.transform, "RightHand");

        range = new Range(-180, 180);

        //left fingers
        leftThumb1 = ae.findChild_(ae.transform, "LeftHandThumb1");
        leftIndex1 = ae.findChild_(ae.transform, "LeftHandIndex1");
        leftMiddle1 = ae.findChild_(ae.transform, "LeftHandMiddle1");
        leftRing1 = ae.findChild_(ae.transform, "LeftHandRing1");
        leftPinky1 = ae.findChild_(ae.transform, "LeftHandPinky1");

        leftThumb2 = ae.findChild_(ae.transform, "LeftHandThumb2");
        leftIndex2 = ae.findChild_(ae.transform, "LeftHandIndex2");
        leftMiddle2 = ae.findChild_(ae.transform, "LeftHandMiddle2");
        leftRing2 = ae.findChild_(ae.transform, "LeftHandRing2");
        leftPinky2 = ae.findChild_(ae.transform, "LeftHandPinky2");

        leftThumb3 = ae.findChild_(ae.transform, "LeftHandThumb3");
        leftIndex3 = ae.findChild_(ae.transform, "LeftHandIndex3");
        leftMiddle3 = ae.findChild_(ae.transform, "LeftHandMiddle3");
        leftRing3 = ae.findChild_(ae.transform, "LeftHandRing3");
        leftPinky3 = ae.findChild_(ae.transform, "LeftHandPinky3");

        //right fingers
        rightThumb1 = ae.findChild_(ae.transform, "RightHandThumb1");
        rightIndex1 = ae.findChild_(ae.transform, "RightHandIndex1");
        rightMiddle1 = ae.findChild_(ae.transform, "RightHandMiddle1");
        rightRing1 = ae.findChild_(ae.transform, "RightHandRing1");
        rightPinky1 = ae.findChild_(ae.transform, "RightHandPinky1");

        rightThumb2 = ae.findChild_(ae.transform, "RightHandThumb2");
        rightIndex2 = ae.findChild_(ae.transform, "RightHandIndex2");
        rightMiddle2 = ae.findChild_(ae.transform, "RightHandMiddle2");
        rightRing2 = ae.findChild_(ae.transform, "RightHandRing2");
        rightPinky2 = ae.findChild_(ae.transform, "RightHandPinky2");

        rightThumb3 = ae.findChild_(ae.transform, "RightHandThumb3");
        rightIndex3 = ae.findChild_(ae.transform, "RightHandIndex3");
        rightMiddle3 = ae.findChild_(ae.transform, "RightHandMiddle3");
        rightRing3 = ae.findChild_(ae.transform, "RightHandRing3");
        rightPinky3 = ae.findChild_(ae.transform, "RightHandPinky3");

        Transform rightFingerBase = ae.findChild_(ae.transform, "RightFingerBase");
        Transform rightIndex4 = ae.findChild_(ae.transform, "RightHandIndex4");
        a1 = Vector3.Distance(rightFingerBase.position, rightHand.position)/transform.localScale.z;//MG
        a2 = Vector3.Distance(rightIndex1.position, rightFingerBase.position) / transform.localScale.z;
        a3 = Vector3.Distance(rightIndex2.position, rightIndex1.position) / transform.localScale.z;
        a4 = Vector3.Distance(rightIndex3.position, rightIndex2.position) / transform.localScale.z;
        a5 = Vector3.Distance(rightIndex4.position, rightIndex3.position) / transform.localScale.z;
    }

    private void LateUpdate()
    {
        leftHand.rotation *= Quaternion.Euler(10, 0, 0);
        rightHand.rotation *= Quaternion.Euler(10, 0, 0);

        leftThumb1.rotation *= Quaternion.Euler(0, range.value(thumb1Y), 0);
        rightThumb1.rotation *= Quaternion.Euler(0, 0, -range.value(thumb1Y));
        leftThumb1.rotation *= Quaternion.Euler(0, 0, range.value(thumb1Z));
        rightThumb1.rotation *= Quaternion.Euler(0, range.value(thumb1Z), 0);

        leftThumb2.rotation *= Quaternion.Euler(0,0, range.value(thumb2Z));
        rightThumb2.rotation *= Quaternion.Euler(0, range.value(thumb2Z), 0);
        leftThumb3.rotation *= Quaternion.Euler(0, 0, range.value(thumb3Z));
        rightThumb3.rotation *= Quaternion.Euler(0, range.value(thumb3Z), 0);

        leftIndex1.rotation *= Quaternion.Euler(0, range.value(index1Y), 0);
        rightIndex1.rotation *= Quaternion.Euler(0, range.value(index1Y), 0);
        leftIndex1.rotation *= Quaternion.Euler(0, 0, range.value(index1Z));
        rightIndex1.rotation *= Quaternion.Euler(0, 0, range.value(index1Z));

        leftIndex2.rotation *= Quaternion.Euler(0, 0, range.value(index2Z));
        rightIndex2.rotation *= Quaternion.Euler(0, 0, range.value(index2Z));
        leftIndex3.rotation *= Quaternion.Euler(0, 0, range.value(index3Z));
        rightIndex3.rotation *= Quaternion.Euler(0, 0, range.value(index3Z));

        leftMiddle1.rotation *= Quaternion.Euler(0, range.value(middle1Y), 0);
        rightMiddle1.rotation *= Quaternion.Euler(0, range.value(middle1Y), 0);
        leftMiddle1.rotation *= Quaternion.Euler(0, 0, range.value(middle1Z));
        rightMiddle1.rotation *= Quaternion.Euler(0, 0, range.value(middle1Z));

        leftMiddle2.rotation *= Quaternion.Euler(0, 0, range.value(middle2Z));
        rightMiddle2.rotation *= Quaternion.Euler(0, 0, range.value(middle2Z));
        leftMiddle3.rotation *= Quaternion.Euler(0, 0, range.value(middle3Z));
        rightMiddle3.rotation *= Quaternion.Euler(0, 0, range.value(middle3Z));

        leftRing1.rotation *= Quaternion.Euler(0, range.value(ring1Y), 0);
        rightRing1.rotation *= Quaternion.Euler(0, range.value(ring1Y), 0);
        leftRing1.rotation *= Quaternion.Euler(0, 0, range.value(ring1Z));
        rightRing1.rotation *= Quaternion.Euler(0, 0, range.value(ring1Z));

        leftRing2.rotation *= Quaternion.Euler(0, 0, range.value(ring2Z));
        rightRing2.rotation *= Quaternion.Euler(0, 0, range.value(ring2Z));
        leftRing3.rotation *= Quaternion.Euler(0, 0, range.value(ring3Z));
        rightRing3.rotation *= Quaternion.Euler(0, 0, range.value(ring3Z));

        leftPinky1.rotation *= Quaternion.Euler(0, range.value(pinky1Y), 0);
        rightPinky1.rotation *= Quaternion.Euler(0, range.value(pinky1Y), 0);
        leftPinky1.rotation *= Quaternion.Euler(0, 0, range.value(pinky1Z));
        rightPinky1.rotation *= Quaternion.Euler(0, 0, range.value(pinky1Z));

        leftPinky2.rotation *= Quaternion.Euler(0, 0, range.value(pinky2Z));
        rightPinky2.rotation *= Quaternion.Euler(0, 0, range.value(pinky2Z));
        leftPinky3.rotation *= Quaternion.Euler(0, 0, range.value(pinky3Z));
        rightPinky3.rotation *= Quaternion.Euler(0, 0, range.value(pinky3Z));
    }

    void Update()
    {
        /*fbbik.solver.leftHandEffector.position = agent_.agent.transform.TransformPoint(agent_.ge.symbolicPositions.leftRadialOrientation[Relative_Hand_Position.RadialOrientation.OUT].Item1,
            agent_.ge.symbolicPositions.height[Relative_Hand_Position.Height.ABDOMEN].Item1, agent_.ge.symbolicPositions.distance[Relative_Hand_Position.Distance.NORMAL].Item1);
        
        fbbik.solver.rightHandEffector.position = agent_.agent.transform.TransformPoint(agent_.ge.symbolicPositions.rightRadialOrientation[Relative_Hand_Position.RadialOrientation.OUT].Item1,
            agent_.ge.symbolicPositions.height[Relative_Hand_Position.Height.ABDOMEN].Item1, agent_.ge.symbolicPositions.distance[Relative_Hand_Position.Distance.NORMAL].Item1);
        */
        fbbik.solver.leftHandEffector.positionWeight = 1f;
        fbbik.solver.rightHandEffector.positionWeight = 1f;
    }

}

/*class FingerData
{
    public float radialOrientation;
    public float height;
    public float distance;
    public float armSwivel;
    public float wristX;
    public float wristY;
    public float wristZ;
    public int handShape;
}
struct PhaseData
{
    public Phase_Type type;
    public float time;
    public bool both;
    public HandData left;
    public HandData right;
}*/

#if UNITY_EDITOR
[CustomEditor(typeof(HandShapeCreator))]
public class HandShapeCreatorEditor : Editor
{
    static readonly System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
    string handShapeName = "newHandShape";

    public override void OnInspectorGUI()
    {
        HandShapeCreator hsc = (HandShapeCreator)target;

        EditorGUILayout.Space();
        
        handShapeName = EditorGUILayout.TextField(handShapeName, GUILayout.MaxWidth(150));
        EditorGUILayout.Space();

        //LeftHandThumb1 en y et z
        hsc.thumb1Y = EditorGUILayout.Slider("Thumb1 Y", hsc.thumb1Y, 0, 100);
        hsc.thumb1Z = EditorGUILayout.Slider("Thumb1 Z", hsc.thumb1Z, 0, 100);

        //LeftHandThumb2 en z
        hsc.thumb2Z = EditorGUILayout.Slider("Thumb2 Z", hsc.thumb2Z, 0, 100);
        //LeftHandThumb3 en z
        hsc.thumb3Z = EditorGUILayout.Slider("Thumb3 Z", hsc.thumb3Z, 0, 100);

        EditorGUILayout.Space();

        //LeftHandIndex1 en y et z
        hsc.index1Y = EditorGUILayout.Slider("Index1 Y", hsc.index1Y, 0, 100);
        hsc.index1Z = EditorGUILayout.Slider("Index1 Z", hsc.index1Z, 0, 100);

        //LeftHandIndex2 en z
        hsc.index2Z = EditorGUILayout.Slider("Index2 Z", hsc.index2Z, 0, 100);
        //LeftHandIndex3 en z
        hsc.index3Z = EditorGUILayout.Slider("Index3 Z", hsc.index3Z, 0, 100);

        EditorGUILayout.Space();

        //LeftHandMiddle1 en y et z
        hsc.middle1Y = EditorGUILayout.Slider("Middle1 Y", hsc.middle1Y, 0, 100);
        hsc.middle1Z = EditorGUILayout.Slider("Middle1 Z", hsc.middle1Z, 0, 100);
        //LeftHandMiddle2 en z
        hsc.middle2Z = EditorGUILayout.Slider("Middle2 Z", hsc.middle2Z, 0, 100);
        //LeftHandMiddle3 en z
        hsc.middle3Z = EditorGUILayout.Slider("Middle3 Z", hsc.middle3Z, 0, 100);

        EditorGUILayout.Space();

        //LeftHandRing1 en y et z
        hsc.ring1Y = EditorGUILayout.Slider("Ring1 Y", hsc.ring1Y, 0, 100);
        hsc.ring1Z = EditorGUILayout.Slider("Ring1 Z", hsc.ring1Z, 0, 100);

        //LeftHandRing2 en z
        hsc.ring2Z = EditorGUILayout.Slider("Ring2 Z", hsc.ring2Z, 0, 100);
        //LeftHandRing3 en z
        hsc.ring3Z = EditorGUILayout.Slider("Ring3 Z", hsc.ring3Z, 0, 100);

        EditorGUILayout.Space();

        //LeftHandPinky1 en y et z
        hsc.pinky1Y = EditorGUILayout.Slider("Pinky1 Y", hsc.pinky1Y, 0, 100);
        hsc.pinky1Z = EditorGUILayout.Slider("Pinky1 Z", hsc.pinky1Z, 0, 100);

        //LeftHandPinky2 en z
        hsc.pinky2Z = EditorGUILayout.Slider("Pinky2 Z", hsc.pinky2Z, 0, 100);
        //LeftHandPinky3 en z
        hsc.pinky3Z = EditorGUILayout.Slider("Pinky3 Z", hsc.pinky3Z, 0, 100);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Hand Shape"))
        {
            SaveHandShape(ref hsc);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clean all"))
        {
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    void SaveHandShape(ref HandShapeCreator hsc)
    {
        Vector3 m01 = new Vector3((float)hsc.a1, 0f, 0f);
        Vector3 m02 = new Vector3((float)hsc.a2, 0f, 0f);

        Vector3 m1 = (Matrix4x4.Rotate(hsc.rightIndex1.localRotation)) * Matrix4x4.Translate(new Vector3((float)hsc.a3, 0f, 0f)).GetColumn(3);
        Vector3 m2 = (Matrix4x4.Rotate(hsc.rightIndex2.localRotation) * Matrix4x4.Rotate(hsc.rightIndex1.localRotation)) * Matrix4x4.Translate(new Vector3((float)hsc.a4, 0f, 0f)).GetColumn(3);
        Vector3 m3 = (Matrix4x4.Rotate(hsc.rightIndex3.localRotation) * Matrix4x4.Rotate(hsc.rightIndex2.localRotation) * Matrix4x4.Rotate(hsc.rightIndex1.localRotation)) * Matrix4x4.Translate(new Vector3((float)hsc.a5, 0f, 0f)).GetColumn(3);


        Vector3 hand_size = new Vector3(m01.x, m01.y, m01.z) + new Vector3(m02.x, m02.y, m02.z) + new Vector3(m1.x, m1.y, m1.z) + new Vector3(m2.x, m2.y, m2.z) + new Vector3(m3.x, m3.y, m3.z);

        string path = Application.streamingAssetsPath + "\\agents\\" + handShapeName + ".xml";
        StreamWriter writer = new StreamWriter(path);

        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        writer.WriteLine("<HandShapes>");
        writer.WriteLine("\t<HandShape name=\"" + handShapeName + "\" size_x=\"" + hand_size.x.ToString(ci) + "\" size_y=\"" + hand_size.y.ToString(ci) + "\" size_z=\"" + hand_size.z.ToString(ci) + "\">");
        
        //If fingerbase.locaRotation != (0,0,0), la sauvegarder
        writer.WriteLine("\t\t<l_Thumb1 x=\"" + hsc.leftThumb1.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftThumb1.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftThumb1.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftThumb1.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<r_Thumb1 x=\"" + hsc.rightThumb1.localRotation.x.ToString(ci) + "\" y=\"" + hsc.rightThumb1.localRotation.y.ToString(ci) + "\" z=\"" + hsc.rightThumb1.localRotation.z.ToString(ci) + "\" w=\"" + hsc.rightThumb1.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<l_Thumb2 x=\"" + hsc.leftThumb2.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftThumb2.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftThumb2.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftThumb2.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<r_Thumb2 x=\"" + hsc.rightThumb2.localRotation.x.ToString(ci) + "\" y=\"" + hsc.rightThumb2.localRotation.y.ToString(ci) + "\" z=\"" + hsc.rightThumb2.localRotation.z.ToString(ci) + "\" w=\"" + hsc.rightThumb2.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<l_Thumb3 x=\"" + hsc.leftThumb3.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftThumb3.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftThumb3.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftThumb3.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<r_Thumb3 x=\"" + hsc.rightThumb3.localRotation.x.ToString(ci) + "\" y=\"" + hsc.rightThumb3.localRotation.y.ToString(ci) + "\" z=\"" + hsc.rightThumb3.localRotation.z.ToString(ci) + "\" w=\"" + hsc.rightThumb3.localRotation.w.ToString(ci) + "\"/>");

        writer.WriteLine("\t\t<Index1 x=\"" + hsc.leftIndex1.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftIndex1.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftIndex1.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftIndex1.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Index2 x=\"" + hsc.leftIndex2.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftIndex2.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftIndex2.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftIndex2.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Index3 x=\"" + hsc.leftIndex3.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftIndex3.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftIndex3.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftIndex3.localRotation.w.ToString(ci) + "\"/>");

        writer.WriteLine("\t\t<Middle1 x=\"" + hsc.leftMiddle1.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftMiddle1.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftMiddle1.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftMiddle1.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Middle2 x=\"" + hsc.leftMiddle2.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftMiddle2.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftMiddle2.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftMiddle2.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Middle3 x=\"" + hsc.leftMiddle3.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftMiddle3.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftMiddle3.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftMiddle3.localRotation.w.ToString(ci) + "\"/>");

        writer.WriteLine("\t\t<Ring1 x=\"" + hsc.leftMiddle1.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftRing1.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftRing1.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftRing1.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Ring2 x=\"" + hsc.leftMiddle2.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftRing2.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftRing2.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftRing2.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Ring3 x=\"" + hsc.leftMiddle3.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftRing3.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftRing3.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftRing3.localRotation.w.ToString(ci) + "\"/>");

        writer.WriteLine("\t\t<Pinky1 x=\"" + hsc.leftPinky1.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftPinky1.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftPinky1.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftPinky1.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Pinky2 x=\"" + hsc.leftPinky2.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftPinky2.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftPinky2.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftPinky2.localRotation.w.ToString(ci) + "\"/>");
        writer.WriteLine("\t\t<Pinky3 x=\"" + hsc.leftPinky3.localRotation.x.ToString(ci) + "\" y=\"" + hsc.leftPinky3.localRotation.y.ToString(ci) + "\" z=\"" + hsc.leftPinky3.localRotation.z.ToString(ci) + "\" w=\"" + hsc.leftPinky3.localRotation.w.ToString(ci) + "\"/>");

        writer.WriteLine("\t</HandShape>");
        writer.WriteLine("</HandShapes>");
        writer.Close();
    }

}
#endif