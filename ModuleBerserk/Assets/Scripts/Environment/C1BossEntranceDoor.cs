using TMPro;
using UnityEngine;
using UnityEngine.Playables;

// 챕터1 맵의 보스방으로 진입하는 문.
// 상호작용을 하면 문이 열리면서 컷신이 시작된다.
public class C1BossEntranceDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private PlayableDirector bossIntroCutscene;
    [SerializeField] private TextMeshPro text;

    void IInteractable.OnPlayerEnter()
    {
        text.enabled = true;
    }

    void IInteractable.OnPlayerExit()
    {
        text.enabled = false;
    }

    void IInteractable.StartInteraction()
    {
        // 중복 재생 방지
        if (bossIntroCutscene.state == PlayState.Paused)
        {
            bossIntroCutscene.Play();
        }
    }
}
