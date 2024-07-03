using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneStarter : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;

    public void Play()
    {
        playableDirector.Play();
    }
}
