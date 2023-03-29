using System;

namespace Animation
{

    public interface IAnimationEngine
    {
        void SetAgent(VirtualAgent.Character a);
        bool TargetFound(string target);
        int GetTargetSide(string target, string mode);
        void StopVoice();
        void PlayVoice(TextToSpeech.SpeechData sdata);
        void PlayMotionCapture(string name);
        void LoadMeshes(string faceMesh, string jawMesh, string tongueMesh, string leftEyeMesh, string rightEyeMesh, string bodyMesh);
        VirtualAgent.Vector3 FindJointPosition(string name);
        int GetFaceBlenshapeCount();
        int GetJawBlenshapeCount();
        string GetAssetsPath();
        void LoadHandShapes(string filename);
    }
}
