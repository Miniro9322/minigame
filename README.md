<div align="center">

# 2D 액션 보스 러쉬 게임
Fallen Forest

개인 프로젝트로 제작한 2D 사이드뷰 보스 러쉬 액션 게임입니다.
같은 프로젝트 안에서 보스 AI를 **커스텀 FSM**과 **Unity Behavior(비헤이비어 그래프) + 자작 노드**, 두 가지 다른 방식으로 직접 구현하고 비교하는 것을 기술적 목표로 삼았습니다.

</div>

---

## 목차
- [프로젝트 개요](#프로젝트-개요)
- [핵심 기술 스택](#핵심-기술-스택)
- [조작 방법](#조작-방법)
- [아키텍처 하이라이트](#아키텍처-하이라이트)
  - [1. 액션감(Game Feel)을 위한 디테일](#1-액션감game-feel을-위한-디테일)
  - [2. 플레이어 — FSM 구조](#2-플레이어--fsm-구조)
  - [3. 보스 AI — 두 가지 방식으로 설계](#3-보스-ai--두-가지-방식으로-설계)
  - [4. 오브젝트 풀링](#4-오브젝트-풀링)
  - [5. 그 외](#5-그-외)
- [폴더 구조](#폴더-구조)
- [향후 개선 방향](#향후-개선-방향)
- [실행 방법](#실행-방법)
- [Contact](#contact)

## 프로젝트 개요

- **장르**: 2D 사이드뷰 액션 (보스 러쉬)
- **개발 인원**: 1인 개발 (기획·프로그래밍·시스템 설계 전담)
- **개발 기간**: 2026.05.18~2026.06.08
- **엔진 / 언어**: Unity 6000.3.15f1 · C#
- **저장소**: https://github.com/Miniro9322/minigame

플레이어는 대시, 패링, 낙하 공격(다운어택) 등을 활용해 서로 다른 패턴을 가진 두 보스와 순차적으로 전투합니다.

## 핵심 기술 스택

| 분류 | 사용 기술 |
|---|---|
| 엔진 | Unity 6000.3.15f1 (URP 2D) |
| 비동기 처리 | UniTask (Cysharp) |
| 입력 | Unity Input System — 액션 기반 입력 + 버퍼링 처리 |
| 보스 AI | Unity Behavior(비헤이비어 그래프) + 커스텀 Composite 노드 |
| 카메라 | Cinemachine — 보스별 LookAt/Confiner 전환 |
| 현지화 | Unity Localization |
| 오브젝트 관리 | UnityEngine.Pool (`IObjectPool<T>`) |

## 조작 방법

| 동작 | 키 |
|---|---|
| 이동 | ← → |
| 점프 | X |
| 공격 | Z |
| 회피(대시) | Shift |
| 패링 | C |
| 아래 (크라우치 / 공중 낙하 공격) | ↓ |
| 일시정지 | Esc |

## 아키텍처 하이라이트

### 1. 액션감(Game Feel)을 위한 디테일

조작감과 타격감을 직접 체감하며 수치를 튜닝하는 데 가장 공을 들인 부분입니다. 수치는 모두 `PlayerData` ScriptableObject에 있어 코드 수정 없이 인스펙터에서 바로 조정할 수 있습니다.

**입력 반응성**
- **점프 버퍼(Jump Buffer, 0.1초) / 코요테 타임(Coyote Time, 0.1초)** — 착지 직전·직후의 입력 오차를 흡수해서 "분명히 눌렀는데 점프가 안 나간" 느낌을 없앰
- **가변 점프(Variable Jump Height)** — 점프 버튼을 일찍 떼면 상승 구간 중력에 `LowJumpMultiplier`(2배)를, 하강 구간에는 `FallMultiplier`(2.5배)를 곱해 더 빨리 떨어지게 해서 점프 궤적에 무게감을 줌
- **입력 버퍼(커맨드 큐)** — 공격 애니메이션 재생 중 들어온 다음 공격 입력을 `CommandQueue`에 저장했다가 선딜레이 없이 바로 다음 콤보로 연결. 공중에서 ↓ 입력도 같은 큐에 담아뒀다가 착지 타이밍과 무관하게 낙하 공격(Plunge)으로 즉시 연계

**회피 / 패링**
- 회피(대시) 중에는 무적 프레임과 잔상 이펙트(`DashAfterImage`)가 동시에 재생되고, 회피 시작 0.2초 이내에 공격 입력이 들어오면 별도 이펙트가 붙은 "회피 공격"으로 캔슬 연결되는 콤보 캔슬 윈도우가 있음
- 패링 성공 시 **0.08초 줌인 → 0.15초 홀드 → 0.3초 줌아웃**의 카메라 펀치(`CinemachineCamera.TriggerZoom`)가 들어감. 전부 unscaled time 기준이라 히트스탑·슬로우모션과 동시에 걸려도 끊기지 않음

**타격감**
- 피격 시 넉백(지정된 방향이 없으면 바라보는 방향의 반대로 자동 계산)과, 무적시간 동안의 점멸(블링크) 연출을 분리해서 관리
- **히트스탑** — 피격 순간 `Time.timeScale`을 아주 짧게(0.04~0.1초) 낮췄다가 복구하는 패턴을 플레이어와 보스 전투 로직 여러 군데서 공통으로 재사용
- 사망 연출은 완전 정지 → 슬로모션 → 정상 속도 3단계로 처리하되, `Animator.updateMode`를 `UnscaledTime`으로 바꿔서 `Time.timeScale`이 0이어도 사망 애니메이션 자체는 끊기지 않고 재생되도록 처리
- 카메라 쉐이크(Cinemachine Noise 컴포넌트의 `AmplitudeGain`을 순간적으로 올렸다 복구)

**히트박스 판정**
- 공격 판정에 `OnTriggerEnter2D`뿐 아니라 `OnTriggerStay2D`도 함께 사용하고, `HashSet`으로 판정당 대상 1회만 데미지가 들어가도록 방지
- 히트박스가 켜지는 순간 이미 겹쳐 있던 콜라이더도 `Physics2D.OverlapCollider`로 즉시 검사 — 타이밍이 애매해서 판정이 씹히는 문제를 제거

`Assets/Scripts/Player/PlayerData.cs`
`Assets/Scripts/Utility/CinemachineCamera.cs`

### 2. 플레이어 — FSM 구조

`Player.cs`는 직접 구현한 `FSM`/`IState` 구조로 Idle, Jump, Fall, Attack, Dodge, Parry, Hit, Death, Plunge, Crouch 총 10개 상태를 관리합니다. 각 상태는 `Enter`/`Exit`/`Update`/`FixedUpdate`만 구현하면 되는 단순한 인터페이스(`IState`)라 상태 추가·수정 시 다른 상태를 건드릴 필요가 없습니다.

- `IDamageable` 인터페이스로 플레이어·보스·그로기 파츠가 동일한 데미지 처리 흐름(`DamageInfo` — 패링 가능 여부, 넉백 방향, 무적 무시 여부)을 공유해 새로운 피격 대상을 추가해도 데미지 로직을 재작성하지 않도록 설계
- 위 액션감 디테일(점프 버퍼, 입력 버퍼, 회피 캔슬 등)이 실제로 얹혀 있는 상태 구조입니다

`Assets/Scripts/Player/PlayerFSM/Player.cs`

### 3. 보스 AI — 두 가지 방식으로 설계

같은 게임에 의도적으로 서로 다른 AI 설계 방식을 적용했습니다.

**Boss1 — 커스텀 FSM**

`BossController` 추상 클래스가 템플릿 메서드 패턴(`ChooseNextAction`)으로 "다음에 무엇을 할지 결정하는 시점"을 정의하고, `Boss1`이 이를 상속해 실제 패턴 선택 로직을 구현합니다.

- 플레이어와의 거리에 따라 사용 가능한 패턴 후보를 추리고, 가중치 기반 확률로 선택
- 일정 주기마다 강제로 발동하는 쿨다운형 패턴을 이동 중에도 끊고 즉시 발동하도록 별도 처리
- 패링 성공 시 즉시 Idle로 전환 후 스턴

**Boss2 — Unity Behavior(비헤이비어 그래프) + 커스텀 Composite 노드**

Unity의 공식 비주얼 비헤이비어 트리 툴(`com.unity.behavior`) 위에서 동작하며, 기본 제공 노드만으로 표현하기 어려운 로직은 직접 노드로 확장했습니다.

- **`WeightedRandomSelector`**: 자식 패턴을 가중치 기반으로 랜덤 선택하되, 선택되지 않은 패턴의 가중치를 매 선택마다 누적 증가시켜 같은 패턴이 연속으로 나오는 "불운"을 방지하는 Composite 노드를 직접 작성
- HP 50% 시점에 페이즈 전환(`OnPhase2Start`)이 트리거되어 2페이즈 전용 패턴 풀이 열림
- 5종의 패턴을 UniTask 기반 비동기 시퀀스로 구현
  - 패링형 투사체 — 플레이어가 패링하면 반사되어 2.5배 데미지로 보스에게 되돌아감
  - 경고 연출 후 폭발하는 화염 기둥
  - 원형 탄막
  - 층별 레이저 — 경고(얇은 선) → 발동(굵은 선) 2단계 연출
  - 파츠를 모두 부수면 그로기에 걸리는 브레이크 패턴 (제한 시간 내 실패 시 보스 체력 회복으로 페널티)
- 매 패턴 종료 후 `CancellationToken` 기반으로 안전하게 취소 가능한 텔레포트 이동 (층 × 좌/중/우 좌표계)

`Assets/Scripts/Boss2/Boss2Controller.cs`
`Assets/Scripts/Boss2/Composite/WeightedRandomSelector.cs`

### 4. 오브젝트 풀링

투사체, 화염 기둥(경고/폭발), 레이저, 그로기 파츠, 패링 투사체, 대시 잔상까지 여러 종류의 `IObjectPool<T>`를 개별 관리합니다. `Get`/`Release`/`Destroy` 콜백에서 활성화 상태와 시각 효과(색상, 알파 등)를 매번 초기화해, 재사용 시 이전 사용 흔적이 남지 않도록 처리했습니다.

### 5. 그 외

- **현지화**: Unity Localization 패키지로 보스 이름 등 텍스트를 다국어로 관리
- **세이브**: `JsonUtility` 기반으로 볼륨/언어 설정을 `Application.persistentDataPath`에 저장

## 향후 개선 방향

- [ ] 보스 패턴 데이터를 ScriptableObject로 분리해 밸런싱 편의성 개선 및 보스 종류 추가

## 실행 방법

1. Unity Hub에서 **Unity 6000.3.15f1** 버전으로 프로젝트 열기
2. `Assets/Scenes` 하위의 시작 씬(Title)에서 실행
