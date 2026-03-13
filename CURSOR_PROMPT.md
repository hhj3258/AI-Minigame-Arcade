# Cursor 시작 프롬프트 — Phase 1

아래 내용을 Cursor 채팅에 붙여넣고 `@TECH_SPEC.md`를 첨부하세요.

---

## 📋 붙여넣기용 프롬프트

```
@TECH_SPEC.md

Unity 6000.3.10f1 프로젝트에서 Phase 1을 구현해줘.

## 목표
스와이프 카드 UI + 퀴즈 게임 프로토타입 (Claude API 연동 없이 더미 데이터 사용)

## 작업 순서

### Step 1: 프로젝트 초기 세팅
- `Assets/Scenes/MainScene.unity` 생성 (기존 SampleScene 대체)
- 패키지 설치:
  - UniTask: `com.cysharp.unitask` (Package Manager → Add by git URL: https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask)
  - Newtonsoft.Json: `com.unity.nuget.newtonsoft-json`
- `Assets/Scripts/`, `Assets/UI/UXML/`, `Assets/UI/USS/`, `Assets/Prefabs/GameCards/`, `Assets/Settings/` 폴더 생성

### Step 2: ScriptableObject 설정 파일
TECH_SPEC.md의 "ScriptableObject 설정 파일" 섹션 기준으로 아래 파일 생성:
- `Assets/Scripts/Settings/QuizSettings.cs`
- `Assets/Scripts/Settings/ApiSettings.cs`
에셋 파일도 `Assets/Settings/`에 생성할 것

### Step 3: 데이터 모델
TECH_SPEC.md의 "데이터 모델" 섹션 기준으로 생성:
- `Assets/Scripts/Data/QuizQuestion.cs`
- `Assets/Scripts/Data/ChatMessage.cs`

### Step 4: IMinigame 인터페이스
- `Assets/Scripts/Games/IMinigame.cs` 생성

### Step 5: 스와이프 카드 UI (UI Toolkit)
- `Assets/UI/UXML/MainLayout.uxml`: 세로 풀스크린, 카드 3개를 수직으로 쌓은 구조
- `Assets/UI/USS/Common.uss`: 기본 스타일 (배경색, 폰트 크기)
- `Assets/Scripts/Core/GameCardManager.cs`:
  - PointerDownEvent/PointerMoveEvent/PointerUpEvent로 수직 스와이프 감지
  - 화면 높이 30% 이상 드래그 시 카드 전환
  - UI Toolkit Transitions으로 translateY 애니메이션 (300ms, ease-out)

### Step 6: 퀴즈 게임 프로토타입
더미 데이터 3문제를 하드코딩해서 동작하는 퀴즈 게임 구현:
- `Assets/UI/UXML/QuizGame.uxml`: 문제 텍스트 + 보기 4개 버튼 + 타이머 + 점수
- `Assets/UI/USS/QuizGame.uss`
- `Assets/Scripts/Games/QuizGame.cs`: IMinigame 구현, 20초 타이머, 정답/오답 판정
- `Assets/Prefabs/GameCards/QuizGameCard.prefab`

### Step 7: 로딩 인디케이터
- `Assets/Scripts/UI/LoadingIndicator.cs`: 점 3개 깜박이는 애니메이션 (나중에 API 대기 시 사용)

## 완료 기준
- MainScene 실행 시 퀴즈 카드가 표시됨
- 위아래 스와이프 시 카드 전환 애니메이션 동작
- 퀴즈 5문제 진행 후 점수 표시
- 컴파일 에러 없을 것

## 주의사항
- UGUI(Canvas, Text, Button 컴포넌트) 절대 사용 금지 — UI Toolkit만 사용
- .cursorrules의 코딩 컨벤션 반드시 준수
- 한 번에 모든 파일 생성하지 말고 Step 순서대로 진행하고 각 Step 완료 후 알려줘
```

---

## 팁

- Step별로 완료 확인 후 다음 단계 진행 요청
- 컴파일 에러 발생 시 에러 메시지 그대로 붙여넣기
- Phase 2 (NPC 대화, 스토리 릴레이) 시작 전에 `@GAME_DESIGN.md`도 함께 첨부
