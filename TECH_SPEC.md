# TECH_SPEC — AI Mini Arcade (Cursor IDE용 구현 스펙)

> Unity 6000.3.10f1 기준. 이 문서의 구조와 컨벤션을 **그대로** 준수해서 구현할 것.

---

## 1. 씬 구성

| 씬 | 경로 | 역할 |
|----|------|------|
| MainScene | `Assets/Scenes/MainScene.unity` | 단일 씬. 스와이프 카드 컨테이너 포함. 씬 전환 없음. |

기존 `SampleScene.unity` 대신 `MainScene.unity` 로 교체.

---

## 2. 프리팹 구조

```
Assets/
└── Prefabs/
    ├── GameCards/
    │   ├── QuizGameCard.prefab       # 퀴즈 게임 카드
    │   ├── NpcChatGameCard.prefab    # NPC 대화 게임 카드
    │   └── StoryRelayGameCard.prefab # 스토리 릴레이 게임 카드
    └── UI/
        └── LoadingIndicator.prefab   # 로딩 점 3개 애니메이션
```

---

## 3. 스크립트 구조

```
Assets/Scripts/
├── Core/
│   ├── GameCardManager.cs      # 스와이프 카드 전환 로직
│   ├── ClaudeApiClient.cs      # Claude HTTP 클라이언트
│   └── ClaudePrompts.cs        # 게임별 시스템 프롬프트 상수
├── Games/
│   ├── IMinigame.cs            # 미니게임 공통 인터페이스
│   ├── QuizGame.cs
│   ├── NpcChatGame.cs
│   └── StoryRelayGame.cs
├── Data/
│   ├── QuizQuestion.cs
│   ├── ChatMessage.cs
│   ├── NpcContext.cs
│   └── StoryState.cs
├── Settings/
│   ├── QuizSettings.cs         # ScriptableObject
│   ├── NpcSettings.cs          # ScriptableObject
│   ├── StorySettings.cs        # ScriptableObject
│   └── ApiSettings.cs          # ScriptableObject
└── UI/
    ├── LoadingIndicator.cs
    └── ShareButton.cs
```

---

## 4. UI 시스템: Unity UI Toolkit

- **UGUI 사용 금지** (`Canvas`, `Text`, `Button` 컴포넌트 일절 사용 금지)
- 모든 UI는 **UI Toolkit** (`UIDocument` + UXML + USS) 으로 작성

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

### 스와이프 구현 방법
- `PointerDownEvent` → 시작 Y 좌표 기록
- `PointerMoveEvent` → 현재 Y와 시작 Y 차이로 드래그 거리 계산
- `PointerUpEvent` → 임계값(화면 높이 30% 이상) 초과 시 카드 전환
- 카드 전환 애니메이션: UI Toolkit `Transitions` (translateY, duration 300ms, easing ease-out)

---

## 5. 미니게임 공통 인터페이스

```csharp
// Assets/Scripts/Games/IMinigame.cs
public interface IMinigame
{
    UniTask InitializeAsync();
    void OnGameStart();
    void OnGameEnd();
}
```

`QuizGame`, `NpcChatGame`, `StoryRelayGame` 모두 `IMinigame` 구현.
`GameCardManager`는 `IMinigame` 인터페이스로만 참조 (DIP).

---

## 6. 데이터 모델

```csharp
// Assets/Scripts/Data/QuizQuestion.cs
[System.Serializable]
public class QuizQuestion
{
    public string Topic;
    public string Question;
    public string[] Choices;      // 길이 4
    public int AnswerIndex;       // 0~3
    public string Explanation;
}

// Assets/Scripts/Data/ChatMessage.cs
[System.Serializable]
public class ChatMessage
{
    public string Role;           // "user" | "assistant"
    public string Content;
}

// Assets/Scripts/Data/NpcContext.cs
[System.Serializable]
public class NpcContext
{
    public string ScenarioId;
    public string NpcName;
    public string Mission;
    public List<ChatMessage> ConversationHistory;
}

// Assets/Scripts/Data/StoryState.cs
[System.Serializable]
public class StoryState
{
    public List<string> Rounds;   // 유저/Claude 교대 저장
    public bool IsComplete;
    public string Title;
}
```

---

## 7. ScriptableObject 설정 파일

```csharp
// Assets/Scripts/Settings/QuizSettings.cs
[CreateAssetMenu(fileName = "QuizSettings", menuName = "AIMiniArcade/QuizSettings")]
public class QuizSettings : ScriptableObject
{
    public int QuestionCount = 5;
    public float TimeLimitSeconds = 20f;
    public string[] Topics = { "역사", "과학", "상식", "문화", "스포츠" };
}

// Assets/Scripts/Settings/NpcSettings.cs
[CreateAssetMenu(fileName = "NpcSettings", menuName = "AIMiniArcade/NpcSettings")]
public class NpcSettings : ScriptableObject
{
    public string[] ScenarioIds = { "fantasy_village", "space_station", "detective_office" };
    public int MaxTurns = 10;
}

// Assets/Scripts/Settings/StorySettings.cs
[CreateAssetMenu(fileName = "StorySettings", menuName = "AIMiniArcade/StorySettings")]
public class StorySettings : ScriptableObject
{
    public int TotalRounds = 5;
    public string[] Genres = { "판타지", "로맨스", "공포", "SF", "일상" };
}

// Assets/Scripts/Settings/ApiSettings.cs
[CreateAssetMenu(fileName = "ApiSettings", menuName = "AIMiniArcade/ApiSettings")]
public class ApiSettings : ScriptableObject
{
    public string Endpoint = "https://api.anthropic.com/v1/messages";
    public string Model = "claude-haiku-4-5-20251001";
    public int TimeoutSeconds = 10;
    public int MaxTokens = 1024;
}
```

설정 에셋 파일 경로: `Assets/Settings/*.asset`

---

## 8. Claude API 클라이언트

```csharp
// Assets/Scripts/Core/ClaudeApiClient.cs
public interface IClaudeApiClient
{
    UniTask<string> SendMessageAsync(string systemPrompt, List<ChatMessage> messages);
}

public class ClaudeApiClient : MonoBehaviour, IClaudeApiClient
{
    // ApiSettings ScriptableObject 주입
    // API Key: StreamingAssets/claude_config.json 에서 런타임에 로드
    // 요청 형식: { "model": ..., "max_tokens": ..., "system": ..., "messages": [...] }
    // 헤더: x-api-key, anthropic-version: 2023-06-01, content-type: application/json
}
```

**API Key 로드 방식**
```json
// StreamingAssets/claude_config.json  (gitignore에 반드시 등록)
{
  "apiKey": "sk-ant-..."
}
```
> ⚠️ 이 파일은 빌드 시 기기에 포함됩니다. 개발/테스트 전용. 출시 시 서버 중계 방식으로 교체 필요.

---

## 9. 시스템 프롬프트

```csharp
// Assets/Scripts/Core/ClaudePrompts.cs
public static class ClaudePrompts
{
    public const string QuizGenerate =
        "당신은 퀴즈 출제자입니다. 사용자가 지정한 주제에 맞는 4지선다 문제를 5개 생성하세요.\n" +
        "반드시 다음 JSON 배열 형식으로만 응답하세요:\n" +
        "[{\"topic\":\"\",\"question\":\"\",\"choices\":[\"\",\"\",\"\",\"\"],\"answerIndex\":0,\"explanation\":\"\"}]";

    public const string QuizComment =
        "당신은 퀴즈 결과를 평가하는 MC입니다. 디시인사이드 커뮤니티 말투로 짧게 총평하세요. " +
        "욕설은 절대 사용하지 마세요. 2~3문장 이내로 응답하세요.";

    public const string NpcSystem =
        "당신은 게임 NPC입니다. 주어진 시나리오와 페르소나를 유지하며 플레이어와 대화하세요.\n" +
        "응답은 반드시 JSON 형식으로: {\"dialogue\":\"\",\"isMissionComplete\":false}";

    public const string StoryRelay =
        "당신은 공동 작가입니다. 플레이어의 문장에 이어지는 2~3문장을 작성하세요. " +
        "장르와 분위기를 유지하세요. 마지막 라운드(isFinal=true)에는 엔딩 문단과 제목을 JSON으로 반환하세요:\n" +
        "{\"content\":\"\",\"title\":\"\"}";
}
```

---

## 10. 코딩 컨벤션

Cursor가 코드 작성 시 **반드시** 준수할 규칙:

### 네이밍
| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스명, 메서드명 | PascalCase | `GameCardManager`, `SendMessageAsync` |
| public/protected 필드 | PascalCase | `public int MaxCount` |
| private 필드 | `_camelCase` | `private int _currentIndex` |
| 공개 읽기 전용 | 자동 프로퍼티 | `public int Score { get; private set; }` |

### Null 처리
```csharp
// ✅ 올바른 방법
if (obj == null) { ... }
if (obj != null) { ... }

// ❌ 금지
if (!obj) { ... }
if (obj) { ... }
```

### 초기화
```csharp
// Awake/Start에서 필수 참조 검증 후 return
private void Awake()
{
    if (_apiClient == null)
    {
        Debug.LogError("ClaudeApiClient가 할당되지 않았습니다.", this);
        return;
    }
    // 이후 _apiClient 재검증 불필요
}
```

### Enum 분기
```csharp
// ✅ switch 사용
switch (gameType)
{
    case GameType.Quiz: ... break;
    case GameType.NpcChat: ... break;
    case GameType.StoryRelay: ... break;
    default:
        Debug.LogError($"미구현 GameType: {gameType}", this);
        break;
}
```

### 로그
```csharp
Debug.LogError("메시지", this);       // context 인자 필수
Debug.LogWarning("메시지", gameObject);
```

### 기타
- 빈 `Start()`, `Update()` 등 로직 없는 생명주기 메서드 **작성 금지**
- 주석은 **한국어** 작성
- 메서드 간 **빈 줄 1줄** 유지

### SOLID 원칙
| 원칙 | 적용 방법 |
|------|---------|
| SRP | API 호출(ClaudeApiClient) ↔ 게임 로직(XxxGame) ↔ UI(UXML) 완전 분리 |
| OCP | 새 미니게임 추가 시 `IMinigame` 구현만 추가, 기존 코드 수정 없음 |
| LSP | 모든 미니게임 클래스는 `IMinigame`을 완전히 대체 가능하게 구현 |
| ISP | 게임 로직 인터페이스(`IMinigame`)와 UI 바인딩 로직 혼합 금지 |
| DIP | `GameCardManager`는 `IMinigame`에 의존, 구체 클래스 직접 참조 금지 |

---

## 11. 의존성 패키지

| 패키지 | Package ID | 용도 |
|--------|-----------|------|
| UniTask | `com.cysharp.unitask` | async/await API 호출 |
| Newtonsoft.Json | `com.unity.nuget.newtonsoft-json` | Claude 응답 JSON 파싱 |
| UI Toolkit | Unity 내장 | UI (별도 설치 불필요) |

Package Manager → Add by name 으로 설치.

---

## 12. .gitignore 추가 항목

```
# Claude API Key
Assets/StreamingAssets/claude_config.json
Assets/StreamingAssets/claude_config.json.meta
```
