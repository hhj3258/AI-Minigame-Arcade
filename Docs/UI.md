# UI/UX 스펙 — AI Mini Arcade

---

## 1. 컨셉

YouTube Shorts / Instagram Reels 방식의 **스와이프 카드 UI**.
각 미니게임이 독립된 풀스크린 카드로 존재하며, 위아래 스와이프로 전환한다.

```
┌─────────────────┐
│                 │
│   미니게임 A    │  ← 풀스크린 카드 1 (퀴즈)
│                 │
│  [재시작] [공유]│
└─────────────────┘
       ↕ 스와이프
┌─────────────────┐
│                 │
│   미니게임 B    │  ← 풀스크린 카드 2 (뱀서라이크)
│                 │
└─────────────────┘
```

- 각 카드는 **독립 실행** — 스와이프 전환 시 현재 게임 상태 초기화
- AI 응답 대기 중 → 점 세 개 로딩 애니메이션 표시
- 하단 고정 버튼: **재시작** / **공유** (OS 네이티브 Share Sheet 호출)

---

## 2. UI 시스템

- **UI Toolkit (UXML/USS)** 사용 — **UI 전용** (게임 월드 렌더링에는 사용하지 않음)
- 텍스트: USS `font-size` 직접 지정

---

## 3. 레이아웃

### 해상도

- 기준 해상도: **1080 × 1920 (9:16)**
- 안전 영역(Safe Area) 반영 필수

### 카드 컨테이너 구조

```
card-container
├── game-card-0  (QuizGame)
└── game-card-1  (SurvivorGame)
```

카드 전환: 스와이프 감지 → `CardSwipeController.cs`가 300ms ease-out 이동 처리.

---

## 4. 스와이프 인터랙션 스펙

| 항목 | 값 |
|------|-----|
| 전환 임계값 | 화면 높이의 **30%** 이상 드래그 |
| 전환 방향 | 위 드래그 → 다음 카드 / 아래 드래그 → 이전 카드 |
| 미달 드래그 | 원위치 복귀 |
| 첫 카드 아래 드래그 | 제자리 고정 |
| 마지막 카드 위 드래그 | 제자리 고정 |
| 전환 시간 | **300ms**, ease-out |
| 구현 스크립트 | `Assets/Scripts/Core/CardSwipeController.cs` |

### 스와이프 감지 설정값

| 방향 | DesiredAngle | MinimumDistance | MaximumTime |
|------|-------------|-----------------|-------------|
| 위 (다음 카드) | `0` | `200px` | `0.25s` |
| 아래 (이전 카드) | `180` | `200px` | `0.25s` |

---

## 5. UI Toolkit 구현 기준

Unity의 UXML/USS 구조는 **웹 프로토타입 파일을 기준**으로 구현한다.

| 프로토타입 파일 | 대응 UXML |
|----------------|-----------|
| `Prototype/index.html` | 스와이프 카드 컨테이너 (`CardSwipeController.cs`) |
| `Prototype/QuizGame.html` | `Assets/UI/UXML/QuizGame.uxml` |
| `Prototype/SurvivorGame.html` | `Assets/UI/UXML/SurvivorGame.uxml` |

- HTML 클래스명(`.quiz-root`, `.hud-panel`, `.upgrade-card` 등)을 UXML의 `name` 또는 `class` 속성으로 1:1 매핑한다.
- CSS 속성은 USS로 동일하게 옮긴다.
- 패널 표시/숨김은 HTML의 `display:none ↔ flex` 전환과 동일하게 USS 클래스 토글로 구현한다.

> 프로토타입 미리보기: `http://localhost:3333` (Python HTTP 서버, `Prototype/` 디렉토리 서빙)

---

## 6. 파일 경로

```
Assets/UI/
├── UXML/
│   ├── QuizGame.uxml                  # 퀴즈 게임 전체 패널
│   └── SurvivorGame.uxml              # 뱀서라이크 HUD
└── USS/
    ├── QuizGame.uss
    └── SurvivorGame.uss
```

---

## 6. 퀴즈 게임 UXML 패널 구조

파일: `Assets/UI/UXML/QuizGame.uxml`

각 패널은 CSS `.hidden` 클래스 추가/제거로 표시/숨김 전환한다.

### 패널 구조

```
quiz-root
├── topic-select-panel
│   ├── title-label                    # "주제를 선택하세요"
│   └── topic-buttons-container
│       ├── topic-button-0
│       ├── topic-button-1
│       ├── topic-button-2
│       ├── topic-button-3
│       └── topic-button-4
├── loading-panel
│   ├── loading-text                   # "문제를 생성하고 있습니다"
│   └── dots-container
│       ├── dot-0                      # delay offset: 0.0s
│       ├── dot-1                      # delay offset: 0.13s
│       └── dot-2                      # delay offset: 0.27s
├── gameplay-panel
│   ├── topic-badge
│   ├── question-counter               # "1 / 5"
│   ├── question-text
│   ├── choices-container
│   │   ├── choice-0
│   │   ├── choice-1
│   │   ├── choice-2
│   │   └── choice-3
│   ├── status-bar
│   │   ├── timer-label
│   │   └── score-label
│   └── explanation-panel              # 정답/오답 후 표시 (기본 hidden)
│       └── explanation-text
└── result-panel
    ├── result-title                   # "🎉 클리어!" / "😢 실패"
    ├── result-score                   # "3 / 5 정답"
    ├── comment-text                   # AI 총평
    └── restart-button                 # "다시 하기"
```

---

## 7. 로딩 인디케이터 스펙

- 점 3개 순서대로 깜박임, 주기: **0.4초 (400ms)**
- 구현: `QuizGame.cs`에서 `schedule`로 순차 애니메이션 처리

| Dot | Delay Offset |
|-----|--------------|
| dot-0 | 0.0s |
| dot-1 | 0.13s |
| dot-2 | 0.27s |

---

## 9. 텍스트 사이즈 스펙

| 용도 | 크기 | 스타일 |
|------|------|--------|
| 화면 타이틀 | **100** | Bold |
| 결과 타이틀 ("클리어!" / "실패") | **100** | Bold |
| 결과 점수 | **72** | Bold |
| 문제 텍스트 | **60** | Regular |
| 버튼 텍스트 | **60** | Medium |
| 타이머 / 점수 | **52** | Bold |
| 결과 코멘트 | **52** | Regular |
| 선택지 버튼 텍스트 | **56** | Medium |
| 주제 뱃지 / 카운터 | **48** | Regular |
| 선택지 번호 (IndexLabel) | **48** | Regular |
| 해설 텍스트 | **48** | Regular |

---

## 10. 디자인 토큰

USS 변수(`--color-*`) 또는 `ColorPalette` ScriptableObject로 관리한다.

### 컬러 팔레트 (캐주얼 밝은 테마)

| 토큰 | 값 | 용도 |
|------|----|------|
| Background | `#f0f4ff` | 화면 배경 (연한 라벤더 화이트) |
| Card Background | `#ffffff` | 카드/패널 배경 |
| Primary | `#5b6ef5` | 주요 강조색 (인디고 바이올렛) |
| Accent | `#ff6b35` | 보조 강조색 (비비드 오렌지) |
| Text | `#1e1e2e` | 기본 텍스트 (딥 네이비) |
| Text Secondary | `#7c7f9e` | 보조 텍스트 (블루그레이) |
| Correct | `#29c77a` | 정답 (민트 그린) |
| Wrong | `#ff4d6d` | 오답 (코랄 레드) |
| Button Default BG | `#eff1ff` | 기본 버튼 배경 (페일 퍼플) |

### 선택지 버튼 상태 색상

| 상태 | 배경색 | 테두리 |
|------|--------|--------|
| 기본 | `#eff1ff` | `#c5c8f0` |
| 정답 선택 | `#29c77a` | `#1da862` |
| 오답 선택 | `#ff4d6d` | `#e03558` |
| 정답 하이라이트 (오답 선택 시) | `#29c77a` | `#ff6b35` |

---

## 11. 뱀서라이크 게임 (SurvivorGame)

### 11-1. 화면 구성 개요

| 패널 | 표시 조건 | 설명 |
|------|-----------|------|
| `hud-panel` | 항상 표시 | HP·타이머·처치 수·EXP·조이스틱 |
| `upgrade-panel` | 레벨업 시 | 게임 일시정지 + 무기 3장 선택 |
| `result-panel` | HP 0 시 | 결과 요약 + 재시작 |

---

### 11-2. UXML 패널 구조

파일: `Assets/UI/UXML/SurvivorGame.uxml`

```
survivor-root
├── hud-panel                          ← 항상 표시
│   ├── top-bar
│   │   ├── hp-bar-container
│   │   │   ├── hp-label               # "HP 75/100"
│   │   │   └── hp-bar-bg
│   │   │       └── hp-bar-fill        # width % 로 HP 반영
│   │   ├── timer-label                # "01:23" (mm:ss)
│   │   └── kill-label                 # "처치 42"
│   ├── exp-bar-container
│   │   ├── exp-bar-fill               # width % 로 EXP 반영
│   │   └── level-label                # "Lv.3"
│   └── joystick-area                  ← 화면 하단 좌측, PointerEvent 수신
│       └── joystick-handle
├── upgrade-panel                      ← 레벨업 시 Flex (기본 None), 게임 일시정지
│   ├── upgrade-title                  # "무기를 선택하세요"
│   └── upgrade-cards
│       ├── upgrade-card-0             # 무기 카드 버튼
│       ├── upgrade-card-1
│       └── upgrade-card-2
└── result-panel                       ← HP 0 시 Flex (기본 None)
    ├── result-title                   # "💀 게임 오버"
    ├── survive-time-label             # "생존 시간  02:37"
    ├── kill-count-label               # "처치 수  58"
    ├── score-label                    # "점수  1,697"
    └── restart-button                 # "다시 하기"
```

---

### 11-3. HUD 패널 상세

#### HP 바
- `hp-bar-fill` width를 `currentHp / maxHp * 100%`로 설정
- HP 30% 이하: fill 색상 `#e74c3c` → 깜박임 효과 (`schedule` 0.5s 간격 opacity 토글)

#### EXP 바
- `exp-bar-fill` width를 `currentExp / nextLevelExp * 100%`로 설정
- 레벨업 시 fill을 즉시 0%로 초기화 후 `upgrade-panel` 표시

#### 타이머
- 포맷: `mm:ss` (예: `01:23`)
- 2분(120초) 도달 시 `timer-label`에 `boss-incoming` USS 클래스 추가 → 텍스트 빨간색 + 진동 효과

---

### 11-4. 업그레이드 카드 구조

레벨업 시 무기 3종(샷건 / 오브 / 미사일) 중 랜덤 3장 제시. 이미 보유한 무기는 "강화"로 표시.

```
upgrade-card (버튼)
├── weapon-icon                        # 무기 아이콘 (Image)
├── weapon-name                        # "샷건" / "오브" / "미사일"
├── weapon-level                       # "NEW" or "Lv.1 → Lv.2"
└── weapon-desc                        # 효과 설명
                                       #   샷건: "3방향 산탄 — 레벨업 시 발사 수 +1"
                                       #   오브: "회전 구체 — 레벨업 시 구체 수 +1"
                                       #   미사일: "유도 추적 — 레벨업 시 데미지 +50%"
```

| 상태 | `weapon-level` 텍스트 | 카드 테두리 색 |
|------|-----------------------|----------------|
| 신규 획득 | `NEW` | `#ffd700` (골드) |
| 강화 | `Lv.1 → Lv.2` 형식 | `#5b6ef5` (Primary) |
| 최대 레벨 (Lv.3) | 카드 목록에서 제외 | — |

---

### 11-5. 결과 패널 상세

| 요소 | 표시 내용 | 포맷 |
|------|-----------|------|
| `result-title` | "💀 게임 오버" | font-size 48px, white |
| `survive-time-label` | 생존 시간 | `생존 시간  02:37` |
| `kill-count-label` | 처치 수 | `처치 수  58` |
| `score-label` | 최종 점수 | `점수  1,697` |
| 점수 계산식 | 처치 수 × 10 + 생존 시간(초) | — |

---

### 11-6. 가상 조이스틱 (VirtualJoystick)

- **구현 방식**: UI Toolkit `PointerEvent`
- **바인딩**: `joystick-area` VisualElement에 `PointerDownEvent` / `PointerMoveEvent` / `PointerUpEvent` 등록
- **최대 반경**: 100px
- **Direction**: `Vector2`, 크기 0~1 (최대 반경 도달 시 1)
- **배치**: `hud-panel` 하단 좌측 고정

---

### 11-7. SurvivorGame 카메라

| 항목 | 값 |
|------|-----|
| 카메라 이름 | `SurvivorCamera` |
| 투영 방식 | Orthographic, Size 5 |
| 용도 | 게임 월드 전용 (QuizGame 등과 독립) |
| 카드 전환 시 | 비활성화 → `SetActive(false)` / 활성화 → `SetActive(true)` |
| MainCamera와 관계 | 별개로 동작 |

---

### 11-8. USS 스타일 가이드

파일: `Assets/UI/USS/SurvivorGame.uss`

| 클래스 | 주요 스타일 |
|--------|------------|
| `survivor-root` | position: absolute, width/height: 100%, background: transparent |
| `hp-bar-fill` | background: `#e74c3c` |
| `exp-bar-fill` | background: `#3498db` |
| `joystick-area` | 200×200px, border-radius: 100px, background: rgba(255,255,255,0.15) |
| `joystick-handle` | 80×80px, border-radius: 40px, background: rgba(255,255,255,0.4) |
| `upgrade-panel` | position: absolute, width/height: 100%, background: rgba(0,0,0,0.8), align-items: center, justify-content: center |
| `upgrade-card` | width: 300px, height: 150px, background: #ffffff, border-radius: 12px, margin: 8px |
| `result-panel` | position: absolute, width/height: 100%, background: rgba(0,0,0,0.85) |
| `result-title` | font-size: 48px, color: #ffffff |
| `score-label` | font-size: 36px, color: #ffd700 |
| `restart-button` | width: 200px, height: 56px, background: #5b6ef5, border-radius: 12px |

> `survivor-root` 배경을 투명으로 설정해야 아래 `SurvivorCamera`가 렌더링하는 게임 월드가 비쳐 보인다.

---

### 11-9. 렌더링 레이어 분리 원칙

| 레이어 | 렌더러 | 내용 |
|--------|--------|------|
| 게임 월드 | **Unity 3D** (SurvivorCamera) | 플레이어, 적, EXP 오브, 배경 타일맵 등 모든 게임 오브젝트 |
| UI 오버레이 | **UI Toolkit (UXML/USS)** | HUD, 업그레이드 패널, 결과 패널 |

- 캐릭터·적·오브젝트는 **반드시 3D 월드(Scene)에서 렌더링**한다.
- UI Toolkit은 **화면 위에 올리는 투명 오버레이**로만 사용한다.
- UI에서 게임 오브젝트를 직접 생성하거나 조작하지 않는다.
