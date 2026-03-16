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
│   미니게임 B    │  ← 풀스크린 카드 2 (NPC 대화)
│                 │
└─────────────────┘
```

- 각 카드는 **독립 실행** — 스와이프 전환 시 현재 게임 상태 초기화
- AI 응답 대기 중 → 점 세 개 로딩 애니메이션 표시
- 하단 고정 버튼: **재시작** / **공유** (OS 네이티브 Share Sheet 호출)

---

## 2. 레이아웃

### 해상도

- 기준 해상도: **1080 × 1920 (9:16)**
- 안전 영역(Safe Area) 반영 필수 (노치, 다이나믹 아일랜드 대응)

### 카드 컨테이너 구조

```
app-root  (height: 100%, overflow: hidden)
└── card-container  (flex-direction: column, height: 100% × 카드 수)
    ├── game-card-0  (min-height: 100%)  ← 퀴즈 게임
    ├── game-card-1  (min-height: 100%)  ← NPC 대화
    └── game-card-2  (min-height: 100%)  ← 스토리 릴레이
```

`card-container`를 translateY로 이동해 카드 전환.

---

## 3. 스와이프 인터랙션 스펙

| 항목 | 값 |
|------|-----|
| 전환 임계값 | 화면 높이의 **30%** 이상 드래그 |
| 전환 방향 | 위 드래그 → 다음 카드 / 아래 드래그 → 이전 카드 |
| 미달 드래그 | 원위치 복귀 (SnapToCurrent) |
| 첫 카드 아래 드래그 | 제자리 고정 |
| 마지막 카드 위 드래그 | 제자리 고정 |

### 구현 방식

```
PointerDownEvent  → startY 기록
PointerMoveEvent  → card-container translateY = baseOffset + deltaY (실시간 반영)
PointerUpEvent    → |deltaY| >= height × 0.3 이면 인덱스 변경, SnapToCurrent 호출
```

### 애니메이션

- 속성: `translateY` (UI Toolkit `style.translate`)
- 전환 시간: **300ms**
- Easing: **ease-out**
- 방법: `Common.uss`의 `.card-container`에 `transition-property: translate` 선언

---

## 4. UI Toolkit 규칙

- **UGUI 사용 금지** (`Canvas`, `Text`, `Button` 컴포넌트 일절 금지)
- 모든 UI: **UI Toolkit** (`UIDocument` + UXML + USS)

### 파일 경로

```
Assets/UI/
├── UXML/
│   ├── MainLayout.uxml           # 스와이프 카드 컨테이너
│   ├── QuizGame.uxml
│   ├── NpcChatGame.uxml
│   └── StoryRelayGame.uxml
├── USS/
│   ├── Common.uss                # 공통 스타일 (색상 변수, 폰트)
│   ├── QuizGame.uss
│   ├── NpcChatGame.uss
│   └── StoryRelayGame.uss
└── PanelSettings.asset
```

---

## 5. 게임별 패널 구조

### 5-1. 퀴즈 게임 패널

```
quiz-root
├── topic-select-panel     ← 주제 선택
│   └── topic-buttons-container (topic-0 ~ topic-4)
├── loading-panel          ← 문제 생성 중
│   └── loading-dots (dot-0, dot-1, dot-2)
├── game-panel             ← 퀴즈 진행
│   ├── topic-badge
│   ├── question-counter   (예: "2 / 5")
│   ├── question-label
│   ├── choices-container
│   │   └── choice-0 ~ choice-3 (Button)
│   ├── status-bar
│   │   ├── timer-label
│   │   └── score-label
│   └── explanation-panel  ← 평소 display:none, 정답/오답 후 표시
│       └── explanation-label
└── result-panel           ← 결과
    ├── result-title       (클리어 / 실패)
    ├── result-score
    ├── comment-label      (Claude 총평)
    └── restart-button
```

### 5-2. NPC 대화 게임 패널 (Phase 2)

```
npc-root
├── scenario-select-panel  ← 배경 선택
├── chat-panel             ← 대화 진행
│   ├── npc-message-label
│   ├── conversation-scroll
│   ├── input-field
│   └── send-button
└── result-panel
```

### 5-3. 스토리 릴레이 패널 (Phase 2)

```
story-root
├── genre-select-panel     ← 장르 선택
├── relay-panel            ← 릴레이 진행
│   ├── story-scroll
│   ├── round-counter
│   ├── input-field
│   └── next-button
└── result-panel
    ├── title-label
    ├── full-story-scroll
    └── share-button
```

---

## 6. 로딩 인디케이터 스펙

- 점 3개(`dot-0`, `dot-1`, `dot-2`) 순서대로 깜박임
- 간격: **0.4초** (400ms)
- 방법: `LoadingIndicator.cs`에서 UniTask 루프로 순환 표시/숨김
- 사용 시점: Claude API 응답 대기 중 (퀴즈 문제 생성, 총평, NPC 응답 등)

---

## 7. 공통 USS 디자인 토큰

```css
/* Assets/UI/USS/Common.uss */
:root {
    --background-color: #1a1a1a;
    --card-background: #2a2a2a;
    --accent-color: #ffc107;
    --text-color: #ffffff;
    --text-secondary: #888888;
    --correct-color: #2d8a4e;
    --wrong-color: #8a2d2d;
    --border-radius-card: 12px;
    --border-radius-button: 8px;
    --padding-card: 16px;
}
```

### 공통 버튼 상태 클래스

| 클래스 | 용도 | 스타일 |
|--------|------|--------|
| `.choice-correct` | 정답 선택 강조 | 초록 배경 (`#2d8a4e`) |
| `.choice-wrong` | 오답 선택 강조 | 빨강 배경 (`#8a2d2d`) |
| `.choice-correct-highlight` | 오답 선택 시 정답 표시 | 노란 테두리 + 초록 배경 |
