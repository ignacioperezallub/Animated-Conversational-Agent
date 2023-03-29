using UnityEngine;
using System.Collections.Generic;


public class Body
{
    public Dictionary<string, SkinnedMeshRenderer> meshes_;
    public GameObject agent;
    public AudioSource audioS = null;

    public Body(Object vh, Dictionary<string, string> m)
    {
        agent = (GameObject)vh;
        audioS = agent.AddComponent<AudioSource>();
        meshes_ = new Dictionary<string, SkinnedMeshRenderer>();
        SkinnedMeshRenderer[] smrs = agent.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer s in smrs)
        {
            if (m.ContainsKey(s.name)) meshes_.Add(m[s.name], s);
        }
    }


    //blendshapes animation
    public void SetBlendShapeWeight(string mesh, int key, float weight)
    {
        if (meshes_.ContainsKey(mesh))
            meshes_[mesh].SetBlendShapeWeight(key, weight);
    }

    //audio management
    public void AudioClipCreate(string text, int length, int channels, int frequency, bool stream)
    {
        audioS.clip = AudioClip.Create(text, length, channels, frequency, stream);
    }

    public void SetAudioData(float[] data, int offsetSamples)
    {
        audioS.clip.SetData(data, offsetSamples);
    }
    
    public void PlayAudio()
    { 
        audioS.Play();
    }

    public void StopAudio()
    {
        audioS.Stop();
    }

    public void UnloadAudioData()
    {
        audioS.clip.UnloadAudioData();
    }
}