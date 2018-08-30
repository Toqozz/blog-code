using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[System.Serializable]
public class StringAudioClipDictionary : SerializableDictionary<string, AudioClip> {}

[System.Serializable]
[CreateAssetMenu(menuName = "SFXDatabase", fileName = "SFXDatabase.asset")]
public class SoundDatabase : ScriptableObject {
    public AudioSourceHelper AudioSourcePrefab;
    [ShowNonSerializedField]
    public const int MaxAudioSourceInstances = 5;
    
    // To keep the scene neat...
    private GameObject audioParent = null;
    public GameObject AudioParent {
        get {
            if (audioParent == null) {
                audioParent = new GameObject("SFX");
            }

            return audioParent;
        }
    }

    private List<AudioSourceHelper> audioSourceInstances = new List<AudioSourceHelper>();
    public AudioSource AudioSourceInstance {
        get {
            AudioSourceHelper src = null;
            int size = audioSourceInstances.Count;
            for (int i = 0; i < size; i++) {
                if (!audioSourceInstances[i].source.isPlaying) {
                    src = audioSourceInstances[i];
                    break;
                }
            }

            if (src == null) {
                if (size < MaxAudioSourceInstances) {
                    src = Instantiate(AudioSourcePrefab, AudioParent.transform);
                    audioSourceInstances.Add(src);
                } else {
                    audioSourceInstances[0].source.Stop();
                    src = audioSourceInstances[0];
                }
            }

            return src.source;
        }
    }
    
    public StringAudioClipDictionary KeyedAudioClips;
    
    public AudioClip GetClip(string key) {
        AudioClip a;
        if (KeyedAudioClips.TryGetValue(key, out a)) {
            return a;
        }

        Debug.Log("Couldn't find an AudioClip matching key \"" + key + "\".");
        return null;
    }

    public void StopAll() {
        for (int i = 0; i < audioSourceInstances.Count; i++) {
            audioSourceInstances[i].source.Stop();
        }
    }

    public void StopSpecific(string sound) {
        var clip = GetClip(sound);
        for (int i = 0; i < audioSourceInstances.Count; i++) {
            if (audioSourceInstances[i].source.clip == clip && audioSourceInstances[i].source.isPlaying) {
                audioSourceInstances[i].source.Stop();
                break;
            }
        }
    }

    public void RemoveSource(AudioSourceHelper source) {
        audioSourceInstances.Remove(source);
    }
}
