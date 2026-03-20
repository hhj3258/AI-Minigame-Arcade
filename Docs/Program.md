# 프로그래밍 스펙 — AI Mini Arcade

> Unity 6000.3.10f1 기준.
> 코딩 컨벤션: `C:\Users\WN-ND000431\Desktop\MyClaude\config\naming-conventions.mdc`

---

## 1. 씬 구성

단일 씬(`MainScene`). 씬 전환 없음.

```
MainCamera              [Camera, AudioListener]
EventSystem             [EventSystem, InputSystemUIInputModule]
CardSwipeController     [CardSwipeController]
├── GameCard_0          [UIDocument, QuizUIGame, SupabaseQuizClient, SafeAreaApplier]
└── GameCard_1          [UIDocument, SurvivorGame, SafeAreaApplier]
    ├── SurvivorCamera  [Camera, CameraFollow, UniversalAdditionalCameraData]
    ├── GameWorld       [Transform]
    │   ├── Player      [Rigidbody2D, CircleCollider2D, PlayerController, ShotgunWeapon, OrbWeapon, MissileWeapon]
    │   ├── EnemySpawner [EnemySpawner]
    │   └── BossSpawner  [BossSpawner]
    └── PoolRoot        [Transform] ← 모든 풀 오브젝트(적/발사체/ExpOrb)의 부모
```

카드 전환: `CardSwipeController`가 위아래 스와이프 감지 후 UIDocument translate 애니메이션.

---

## 2. UI 스택

**UIToolkit** 기반. UGUI / LeanGUI / Canvas 사용하지 않음.

| 파일 | 역할 |
|------|------|
| `QuizPanels.uxml` | 퀴즈 게임 전체 패널 레이아웃 |
| `QuizPanels.uss` | 퀴즈 스타일시트 |
| `QuizPanelSettings.asset` | 퀴즈 PanelSettings |
| `SurvivorGame.uxml` | 서바이버 HUD / 업그레이드 / 결과 레이아웃 |
| `SurvivorGame.uss` | 서바이버 스타일시트 |

패널 전환: USS `.hidden` 클래스 추가/제거

USS 애니메이션 유틸 클래스:

| 클래스 | 용도 |
|--------|------|
| `.anim-in` | opacity 1 + translate 0 (진입 완료 상태) |
| `.anim-pop-in` | opacity 1 + scale 1 (팝업 진입 완료) |
| `.anim-pop-hidden` | opacity 0 + scale 0.82 (팝업 초기 숨김) |
| `.anim-done` | topic-btn 진입 후 — opacity/translate transition 제거, hover/press만 유지 |

---

## 3. 스크립트 구조

```
Scripts/
├── Core/
│   ├── IMinigame.cs              미니게임 공통 인터페이스
│   ├── CardSwipeController.cs    카드 스와이프 전환 컨트롤러
│   └── SafeAreaApplier.cs        노치/펀치홀 Safe Area 자동 적용 (GameCard_0·1에 부착)
│
├── Settings/
│   └── SupabaseSettings.cs       ScriptableObject — Supabase URL·Key·Timeout
│
├── QuizGame/
│   ├── QuizUIGame.cs             퀴즈 게임 컨트롤러 (홈버튼·주제이모지·버튼 애니메이션)
│   ├── QuizQuestionService.cs    Supabase 호출 래퍼 (캐시 없음, 매 도전마다 새로 요청)
│   ├── SupabaseQuizClient.cs     Supabase HTTP 클라이언트 (UnityWebRequest)
│   ├── QuizSettings.cs           ScriptableObject — TopicEntry[] 포함
│   └── QuizQuestion.cs           퀴즈 문제 데이터 모델
│
├── Editor/                         ← Unity Editor 전용 (빌드에 미포함)
│   ├── AndroidBuildScript.cs       Tools > Build Android APK 자동 빌드
│   ├── CaptureGameView.cs          Tools > Capture Game View 스크린샷 캡처
│   └── SurvivorDebugWindow.cs      SurvivorGame 디버그 윈도우
│
└── SurvivorGame/
    ├── SurvivorGame.cs           게임 루프 총괄 (IMinigame 구현)
    ├── SurvivorSettings.cs       ScriptableObject — 웨이브/보스/EXP 설정
    ├── SurvivorRunData.cs        런타임 상태 (HP/킬수/생존시간 등)
    ├── PlayerData.cs             ScriptableObject — 플레이어 기본 스탯
    ├── WeaponData.cs             ScriptableObject — 무기 레벨별 스탯
    ├── OrbWeaponData.cs          ScriptableObject — 오브 전용 추가 설정
    ├── EnemyData.cs              ScriptableObject — 적 스탯
    ├── ColliderSizeUtil.cs       스프라이트 bounds → CircleCollider2D 반지름 계산 유틸
    ├── CameraFollow.cs           SurvivorCamera 플레이어 추적
    ├── Player/
    │   └── PlayerController.cs   Rigidbody2D 이동, HP 관리, 무기 보유
    ├── Weapons/
    │   ├── WeaponBase.cs         무기 추상 기반 클래스 (발화 타이머)
    │   ├── ShotgunWeapon.cs      산탄 무기
    │   ├── OrbWeapon.cs          회전 오브 무기
    │   └── MissileWeapon.cs      유도 미사일 무기
    ├── Projectiles/
    │   ├── Bullet.cs             샷건 발사체 (ObjectPool)
    │   ├── OrbProjectile.cs      회전 오브 발사체
    │   └── MissileProjectile.cs  유도 미사일 발사체 (ObjectPool)
    ├── Enemies/
    │   ├── EnemyBase.cs          적 추상 기반 클래스
    │   ├── SlimeEnemy.cs         슬라임 (HP 30, EXP 5)
    │   ├── BatEnemy.cs           박쥐 (HP 15, 빠름, EXP 3)
    │   ├── GolemEnemy.cs         골렘 (HP 100, 느림, EXP 15)
    │   └── MiniBossEnemy.cs      미니보스 (HP 500, EXP 50)
    ├── Spawning/
    │   ├── EnemySpawner.cs       웨이브 기반 적 스포너 (ObjectPool×3)
    │   └── BossSpawner.cs        주기적 보스 스포너 (ObjectPool)
    ├── Pickup/
    │   └── ExpOrb.cs             EXP 오브 — 자석 흡수, 플레이어 충돌 시 수집
    ├── Level/
    │   └── LevelSystem.cs        EXP 누적, 레벨업 이벤트, TimeScale 제어
    └── UI/
        ├── SurvivorHUD.cs        UIDocument HP·EXP·타이머·킬수 바인딩
        ├── VirtualJoystick.cs    UIToolkit 포인터 이벤트 기반 가상 조이스틱
        ├── UpgradePanel.cs       레벨업 무기 선택 패널
        └── SurvivorResultPanel.cs  게임오버 결과 패널
```

---

## 4. 설계 규칙

### ObjectPool 패턴
- `UnityEngine.Pool.ObjectPool<T>` 사용
- 적(Slime/Bat/Golem/MiniBoss), Bullet, MissileProjectile, ExpOrb 모두 풀로 관리
- 모든 풀 오브젝트는 씬의 **PoolRoot** GameObject 하위에 생성
  - `EnemySpawner._poolRoot`, `BossSpawner._poolRoot`, `SurvivorGame._poolRoot` 인스펙터에서 연결

### 발사체 부모 지정
- `WeaponBase`는 `ProjectileRoot` 프로퍼티 보유
- `SurvivorGame`이 `weapon.Initialize(playerTransform, _poolRoot)` 호출 시 전달
- Bullet, MissileProjectile, OrbProjectile 모두 `ProjectileRoot` 하위에 생성

### 콜라이더 크기 자동화
- `ColliderSizeUtil.GetSpriteRadius(gameObject, fallback)` 사용
- `SpriteRenderer.sprite.bounds.extents`(로컬 공간) → `CircleCollider2D.radius`(로컬 공간) 직접 적용. scale 보정 불필요.
- 적용 대상: PlayerController, EnemyBase, Bullet, OrbProjectile, MissileProjectile

### 버튼 애니메이션 패턴

**등장 애니메이션 (visibility-hidden trick)**
- `style.opacity = 0` 은 CSS transition 대상이므로 역방향 fade-out이 발생함
- 즉시 숨김에는 `style.visibility = Visibility.Hidden` 사용 (transition 미적용)
- 지정 딜레이 후 `visibility = StyleKeyword.Null` 해제 + `.anim-in` 추가 → CSS transition 발동
- 구현 메서드: `AnimateTopicButtons()`, `AnimateQuestionEntrance()`

**프레스 애니메이션 (RegisterPressAnim)**
- CSS 클래스 추가/제거는 같은 프레임 내 배치(batch)되어 transition이 무시됨 → 인라인 `style.scale` 직접 제어
- PointerDown: CSS transition 유지 + `style.scale = PressedScale(0.93)` → 0.2s ease-out 축소
- PointerUp: `ZeroTransition + OvershootScale(1.12)` 즉시 → 1프레임 schedule 후 인라인 해제 → CSS ease-out으로 1.0 복귀
- `TransitionEndEvent`에서 `stylePropertyNames`를 순회해 `"scale"` 감지 → callback 실행
- topic-btn은 `<ui:VisualElement>` 사용 (`<ui:Button>` 사용 시 `:hover` 수도-상태가 자식 이모지에 틴트 적용됨)

### Physics 2D Layer
| Layer 이름 | 번호 |
|-----------|------|
| Player | 6 |
| Enemy | 7 |
| Boss | 8 |
| PlayerProjectile | 9 |
| ExpOrb | 10 |

충돌 OFF: Enemy↔Enemy, PlayerProjectile↔Player

---

## 5. 백엔드 아키텍처

```
Unity (SupabaseQuizClient)
    │  POST /functions/v1/generate-quiz
    │  POST /functions/v1/generate-comment
    ▼
Supabase Edge Function (Deno + gemini-3.1-flash-lite-preview)
    ▼
Supabase PostgreSQL (quiz_questions / quiz_comments / GameConfig)
```

- API Key는 서버 환경변수(`GEMINI_API_KEY`)로 관리. 클라이언트에 미포함.
- DB는 RLS 활성화. Edge Function(Service Role Key)을 통해서만 접근.
- 사용 모델: `gemini-3.1-flash-lite-preview` (무료 티어 RPD 500)

| Edge Function | 동작 |
|---------------|------|
| `generate-quiz` | DB 캐시 확인 → 부족 시 Gemini 생성 → 저장 → 반환. Gemini 실패 시 DB 폴백 |
| `generate-comment` | DB 캐시 확인 → 부족 시 Gemini 생성 → 저장 → 반환. Gemini 실패 시 DB 폴백 (토픽+점수 → 토픽 → 전체 3단계) |

**폴백 헤더**: Gemini 실패 시 응답에 `X-Gemini-Fallback: true` 헤더 포함.
Unity `SupabaseQuizClient`는 이 헤더를 감지해 `Debug.LogWarning`으로 표시.

---

## 6. 의존성 패키지

| 패키지 | 용도 |
|--------|------|
| UniTask (`com.cysharp.unitask`) | async/await |
| Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) | JSON 파싱 |
| Input System (`com.unity.inputsystem`) | 터치/마우스 입력 |
| UI Toolkit (Unity 내장) | UXML + USS UI |
| Addressables (`com.unity.addressables`) | 무기 프리팹 런타임 로드 |
| IngameDebugConsole (`com.yasirkula.ingamedebugconsole`) | 모바일 런타임 로그 콘솔 (릴리스 전 제거) |
