using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

// 활성화되는 순간 꼭대기까지 올라갔다가 다시 돌아오는 엘리베이터.
// 이미 시작된 이동은 취소할 수 없다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ObjectGUID))]
public class Elevator : MonoBehaviour, IPersistentSceneState
{
    [Header("Initial State")]
    // 아랫층에서 시작하는 경우 true를 주면 되고
    // 윗층에서 시작하는 경우 false를 주면 됨.
    [SerializeField] private bool isInitialPositionDown = true;


    [Header("Movement Setting")]
    // 현재 위치에서 얼마나 멀리 위/아래로 움직일지
    [SerializeField] private float movementRange;
    // 이동에 걸리는 시간 및 속도 커브
    [SerializeField] private float upwardMovementDuration;
    [SerializeField] private float downwardMovementDuration;
    [SerializeField] private Ease movementEase = Ease.InOutSine;
    // 상승을 시작하기 전에 잠깐 기다리며 이펙트를 보여주는 시간
    [SerializeField] private float initialMovementDelay = 1f;
    // 꼭대기에 도달한 뒤 다시 아래로 내려오기 전에 기다리는 시간
    [SerializeField] private float delayBeforeReturning = 3f;


    [Header("Shake Effect")]
    // rigidbody 자체를 흔들면 물리가 불안정하니까 tilemap 등 시각적인 요소만
    // 다 하나의 child object에 넣어두고 얘를 흔드는 방식으로 처리함
    [SerializeField] private Transform visualElements;
    // 엘리베이터가 "쾅"하고 떨어지는 수준인 경우 화면 흔들림을 주고 싶을 수 있음
    [SerializeField] private bool screenShakeOnLand;
    [SerializeField] private float screenShakeForce;
    [SerializeField] private float screenShakeDuration;

    private Rigidbody2D rb;
    private ScreenShake screenShake;
    private float heightUpperBound;
    private float heightLowerBound;
    private bool isActive = false;

    // 엘리베이터 작동 상태를 저장할 때 기준이 될 위치.
    // 이동 중이라면 이동이 끝났을 때의 상태를 기록한다.
    // ex) 왕복 => 초기 지점, 편도 => 목표 지점
    private float currentHeightAfterMove;

    //SFX
    private AudioSource elevatorAudioSource;

    private CancellationTokenSource cancelOnDestruction = new();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        screenShake = GetComponent<ScreenShake>();

        CalculateMovementBoundary();

        currentHeightAfterMove = rb.position.y;
    }

    private void OnDestroy()
    {
        // 엘리베이터 작동 도중에 맵을 이동하는 경우 비동기 작업 모두 종료
        cancelOnDestruction.Cancel();
        rb.DOKill();
    }

    // 엘리베이터의 y축 이동 범위를 계산함
    private void CalculateMovementBoundary()
    {
        // 하강 상태로 시작하는 경우
        if (isInitialPositionDown)
        {
            heightLowerBound = rb.position.y;
            heightUpperBound = rb.position.y + movementRange;
        }
        // 상승 상태로 시작하는 경우
        else
        {
            heightLowerBound = rb.position.y - movementRange;
            heightUpperBound = rb.position.y;
        }
    }

    public void ActivateElevatorDown()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }
        StartOneWayMovementAsync(heightLowerBound, downwardMovementDuration, cancelOnDestruction.Token).Forget();
    }

    public void ActivateElevatorUp()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }
        StartOneWayMovementAsync(heightUpperBound, upwardMovementDuration, cancelOnDestruction.Token).Forget();
    }

    private async UniTask StartOneWayMovementAsync(float destinationHeight, float movementDuration, CancellationToken cancellationToken)
    {
        // 엘리베이터 움직임은 비활성화 상태에서만 시작될 수 있음.
        // 중간에 취소하거나 재시작하는 상황이 일어나면 안됨.
        Assert.IsFalse(isActive);

        // 세이브 데이터를 불러오면서 엘리베이터의 초기 상태가 복원된 경우
        // 이미 목적지에 있는데 엘리베이터가 작동되는 경우가 생길 수 있음
        // ex) 챕터1 증원 몹들 타고있는 엘리베이터가 이미 작동된 상태로 저장 => 로딩 후 다시 증원 트리거 접촉
        if (Mathf.Approximately(rb.position.y, destinationHeight))
        {
            return;
        }

        isActive = true;

        this.currentHeightAfterMove = destinationHeight;

        // 1. 이펙트 출력하고 잠시 기다린다
        PlayerElevatorMoveStartEffect();
        await UniTask.WaitForSeconds(initialMovementDelay, cancellationToken: cancellationToken);

        // 2. 목표 위치로 이동한다
        await MoveToAsync(destinationHeight, movementDuration, cancellationToken);

        isActive = false;
    }

    // 아래에 있는 상태에서 위로 올라갔다가 다시 내려오는 움직임
    public void ActivateElevatorUpDown()
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

        // 왕복 이동은 엘리베이터가 아래에 있는 상태에서만 호출되어야 함.
        Assert.AreApproximatelyEqual(rb.position.y, heightLowerBound);

        isActive = true;

        // 1. 이펙트 출력하고 잠시 기다린다
        PlayerElevatorMoveStartEffect();
        await UniTask.WaitForSeconds(initialMovementDelay, cancellationToken: cancellationToken);

        // 2. 위로 이동한다
        await MoveToAsync(heightUpperBound, upwardMovementDuration, cancellationToken);

        // 3. 플레이어가 내릴 때까지 잠시 기다린다
        await UniTask.WaitForSeconds(delayBeforeReturning, cancellationToken: cancellationToken);

        // 4. 다시 원래 자리로 돌아간다
        await MoveToAsync(heightLowerBound, downwardMovementDuration, cancellationToken);

        isActive = false;
    }

    private async UniTask MoveToAsync(float destinationHeight, float movementDuration, CancellationToken cancellationToken)
    {
        rb.DOMoveY(destinationHeight, movementDuration)
            .SetEase(movementEase)
            .SetUpdate(UpdateType.Fixed);

        await UniTask.WaitForSeconds(movementDuration, cancellationToken: cancellationToken);
        AudioManager.instance.StopSFX(elevatorAudioSource);

        // 엘리베이터가 "쾅"하고 떨어지는 경우 추가적인 화면 흔들림 효과 부여
        if (screenShakeOnLand && Mathf.Approximately(destinationHeight, heightLowerBound))
        {
            screenShake.ApplyScreenShake(screenShakeForce, screenShakeDuration);
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
            sceneState.ObjectActivation[id] = IsNotOnInitialPosition();
        }
        else
        {
            sceneState.ObjectActivation.Add(id, IsNotOnInitialPosition());
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

    // 초기 위치와 다른 곳에서 정지한 상태인가?
    // 이동 중인 경우는 목적지를 기준으로 판단함.
    private bool IsNotOnInitialPosition()
    {
        if (isInitialPositionDown)
        {
            return Mathf.Approximately(currentHeightAfterMove, heightUpperBound);
        }
        else
        {
            return Mathf.Approximately(currentHeightAfterMove, heightLowerBound);
        }
    }

    private void ChangeInitialPosition()
    {
        if (isInitialPositionDown)
        {
            rb.MovePosition(new Vector2(rb.position.x, heightUpperBound));
            currentHeightAfterMove = heightUpperBound;
        }
        else
        {
            rb.MovePosition(new Vector2(rb.position.x, heightLowerBound));
            currentHeightAfterMove = heightLowerBound;
        }
    }
}
