using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

// 활성화되는 순간 목표 지점까지 갔다가 잠시 후에 다시 돌아오는 엘리베이터.
// 이미 시작된 이동은 취소할 수 없다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ObjectGUID))]
public class Elevator : MonoBehaviour, IPersistentSceneState
{
    [Header("Movement Setting")]
    // 현재 위치에서 얼마나 멀리 움직일지 결정.
    // 대각선이나 수평 이동도 상관 없다.
    [SerializeField] private Vector2 destinationOffset;
    // 이동에 걸리는 시간 및 속도 커브
    [SerializeField] private float forwardMovementDuration;
    [SerializeField] private float backwardMovementDuration;
    [SerializeField] private Ease forwardMovementEase = Ease.InOutSine;
    [SerializeField] private Ease backwardMovementEase = Ease.InOutSine;
    // 상승을 시작하기 전에 잠깐 기다리며 이펙트를 보여주는 시간
    [SerializeField] private float initialMovementDelay = 1f;
    // 꼭대기에 도달한 뒤 다시 아래로 내려오기 전에 기다리는 시간
    [SerializeField] private float delayBeforeReturning = 3f;


    [Header("Shake Effect")]
    // rigidbody 자체를 흔들면 물리가 불안정하니까 tilemap 등 시각적인 요소만
    // 다 하나의 child object에 넣어두고 얘를 흔드는 방식으로 처리함
    [SerializeField] private Transform visualElements;
    // 엘리베이터가 "쾅"하고 떨어지는 수준인 경우 화면 흔들림을 주고 싶을 수 있음
    [SerializeField] private bool screenShakeOnStartPosition; // 초기 지점으로 돌아오는 경우 흔들림 여부
    [SerializeField] private bool screenShakeOnEndPosition; // 목적지에 도달한 경우 흔들림 여부
    [SerializeField] private float screenShakeForce;
    [SerializeField] private float screenShakeDuration;

    private Rigidbody2D rb;
    private ScreenShake screenShake;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private bool isActive = false;

    // 엘리베이터 작동 상태를 저장할 때 기준이 될 위치.
    // 이동 중이라면 이동이 끝났을 때의 상태를 기록한다.
    // ex) 왕복 => 초기 지점, 편도 => 목표 지점
    private Vector2 destination;

    //SFX
    private AudioSource elevatorAudioSource;

    private CancellationTokenSource cancelOnDestruction = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        screenShake = GetComponent<ScreenShake>();

        CalculateMovementBoundary();
    }

    private void OnDestroy()
    {
        // 엘리베이터 작동 도중에 맵을 이동하는 경우 비동기 작업 모두 종료
        cancelOnDestruction.Cancel();
        rb.DOKill();
    }

    // 엘리베이터의 종점을 기록
    private void CalculateMovementBoundary()
    {
        startPosition = transform.position;
        endPosition = (Vector2)transform.position + destinationOffset;

        // 일단 시작 지점이 목적지인 것으로 취급
        destination = startPosition;
    }

    public void ActivateElevatorBackward()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }
        StartOneWayMovementAsync(startPosition, cancelOnDestruction.Token).Forget();
    }

    public void ActivateElevatorForward()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }
        StartOneWayMovementAsync(endPosition, cancelOnDestruction.Token).Forget();
    }

    private async UniTask StartOneWayMovementAsync(Vector2 destination, CancellationToken cancellationToken)
    {
        // 엘리베이터 움직임은 비활성화 상태에서만 시작될 수 있음.
        // 중간에 취소하거나 재시작하는 상황이 일어나면 안됨.
        Assert.IsFalse(isActive);

        // 세이브 데이터를 불러오면서 엘리베이터의 초기 상태가 복원된 경우
        // 이미 목적지에 있는데 엘리베이터가 작동되는 경우가 생길 수 있음
        // ex) 챕터1 증원 몹들 타고있는 엘리베이터가 이미 작동된 상태로 저장 => 로딩 후 다시 증원 트리거 접촉
        if (IsOnPosition(destination))
        {
            return;
        }

        isActive = true;

        this.destination = destination;

        // 1. 이펙트 출력하고 잠시 기다린다
        PlayerElevatorMoveStartEffect();
        await UniTask.WaitForSeconds(initialMovementDelay, cancellationToken: cancellationToken);

        // 2. 목표 위치로 이동한다
        await MoveToAsync(destination, cancellationToken);

        isActive = false;
    }

    // 목적지에 도착했는지 판별.
    // Mathf.Approximatly()는 tweening 오차마저 false로 판단할 정도로 민감해서 직접 구현함...
    private bool IsOnPosition(Vector2 destination)
    {
        return (rb.position - destination).magnitude < 0.0001f;
    }

    // 현재 위치에서 다른 종점까지 갔다가 돌아오는 움직임
    public void ActivateElevatorBackAndForth()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }

        StartBackAndForthMovementAsync(cancelOnDestruction.Token).Forget();
    }

    private async UniTask StartBackAndForthMovementAsync(CancellationToken cancellationToken)
    {
        // 엘리베이터 움직임은 비활성화 상태에서만 시작될 수 있음.
        // 중간에 취소하거나 재시작하는 상황이 일어나면 안됨.
        Assert.IsFalse(isActive);
        isActive = true;

        // 현재 위치에 따라 목적지를 결정
        Vector2 current = rb.position;
        Vector2 destination = IsOnPosition(startPosition) ? endPosition : startPosition;

        // 1. 이펙트 출력하고 잠시 기다린다
        PlayerElevatorMoveStartEffect();
        await UniTask.WaitForSeconds(initialMovementDelay, cancellationToken: cancellationToken);

        // 2. 목적지로 이동한다
        await MoveToAsync(destination, cancellationToken);

        // 3. 플레이어가 내릴 때까지 잠시 기다린다
        await UniTask.WaitForSeconds(delayBeforeReturning, cancellationToken: cancellationToken);

        // 4. 다시 원래 자리로 돌아간다
        await MoveToAsync(current, cancellationToken);

        isActive = false;
    }

    private async UniTask MoveToAsync(Vector2 destination, CancellationToken cancellationToken)
    {
        // 목적지를 보고 이동 시간과 ease 타입을 선택
        float movementDuration = destination == startPosition ? backwardMovementDuration : forwardMovementDuration;
        Ease movementEase = destination == startPosition ? backwardMovementEase : forwardMovementEase;

        rb.DOMove(destination, movementDuration)
            .SetEase(movementEase)
            .SetUpdate(UpdateType.Fixed);

        await UniTask.WaitForSeconds(movementDuration, cancellationToken: cancellationToken);
        AudioManager.instance.StopSFX(elevatorAudioSource);

        // 엘리베이터가 "쾅"하고 떨어지는 경우 추가적인 화면 흔들림 효과를 주기 위해 사용됨
        if (NeedScreenShakeOnMoveEnd())
        {
            screenShake.ApplyScreenShake(screenShakeForce, screenShakeDuration);
        }
    }

    private bool NeedScreenShakeOnMoveEnd()
    {
        if (IsOnPosition(endPosition))
        {
            return screenShakeOnEndPosition;
        }
        else
        {
            return screenShakeOnStartPosition;
        }
    }

    // 이제 곧 엘리베이터 움직인다는 효과 재생
    // ex) "덜그럭" 하는 효과음, 약간의 진동
    private void PlayerElevatorMoveStartEffect()
    {
        //SFX
        int[] elevatorIndices = {14, 15};
        elevatorAudioSource = AudioManager.instance.PlaySFX(elevatorIndices);

        visualElements.DOShakePosition(duration: 0.5f, strength: 0.05f, vibrato: 20);
    }

    void IPersistentSceneState.Save(SceneState sceneState)
    {
        // 초기 위치와 다른 곳에서 정지한 상태인지 기록
        // ex) 챕터1 증원 몹들 타고있는 엘리베이터
        string id = GetComponent<ObjectGUID>().ID;
        if (sceneState.ObjectActivation.ContainsKey(id))
        {
            sceneState.ObjectActivation[id] = IsOnPosition(endPosition);
        }
        else
        {
            sceneState.ObjectActivation.Add(id, IsOnPosition(endPosition));
        }
    }

    void IPersistentSceneState.Load(SceneState sceneState)
    {
        // 만약 엘리베이터가 작동되어서 초기 위치와 다른 곳에 정지한 상태로 저장되었다면
        // 세이브 데이터를 불러오는 순간에 초기 상태와 반대되는 곳으로 즉시 이동.
        // ex) 아래에서 시작하는 엘리베이터인데 위치가 바뀌었다고 기록됨 => 위에서 시작
        string id = GetComponent<ObjectGUID>().ID;
        if (sceneState.ObjectActivation.TryGetValue(id, out bool shouldChangeInitialPosition) && shouldChangeInitialPosition)
        {
            ChangeInitialPosition();
        }
    }

    private void ChangeInitialPosition()
    {
        transform.position = endPosition;
        destination = endPosition;
    }

    // 엘리베이터 이동 범위를 에디터에서 시각적으로 확인할 수 있게 기즈모로 표시
    private void OnDrawGizmosSelected()
    {
        // 플레이 하기 전까지는 destinationOffset을 실시간으로 반영해서 보여줌.
        // 반대로 게임을 시작하면 Start() 시점의 설정을 보여줌.
        // 여기서 보여주는 기즈모는 "예상 이동 범위"이므로
        // 플레이 도중에 엘리베이터가 이동하는 것에 영향을 받으면 안 되기 때문!
        if (!Application.isPlaying)
        {
            CalculateMovementBoundary();
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPosition, endPosition);

        var collider = GetComponent<BoxCollider2D>();

        var bounds = collider.bounds;
        bounds.center = endPosition + collider.offset;
        var boundPoints = new Vector3[]
        {
            new(bounds.min.x, bounds.min.y),
            new(bounds.min.x, bounds.max.y),
            new(bounds.max.x, bounds.max.y),
            new(bounds.max.x, bounds.min.y),
        };
        Gizmos.DrawLineStrip(boundPoints, looped: true);
    }
}
