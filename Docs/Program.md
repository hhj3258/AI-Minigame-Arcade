# 프로그래밍 스펙 — AI Mini Arcade

> Unity 6000.3.10f1 기준. 이 문서의 구조와 컨벤션을 **그대로** 준수해서 구현할 것.

---

## 1. 씬 구성

| 씬 | 경로 | 역할 |
|----|------|------|
| MainScene | `Assets/Scenes/MainScene.unity` | 단일 씬. 스와이프 카드 컨테이너 포함. 씬 전환 없음. |

기존 `SampleScene.unity` 대신 `MainScene.unity`로 교체.

---

## 2. 프리팹 구조

```
Assets/
└── Prefabs/
    ├── GameCards/
    │   ├── QuizGameCard.prefab
    │   ├── NpcChatGameCard.prefab
    │   └── StoryRelayGameCard.prefab
    └── UI/
        └── LoadingIndicator.prefab
```

---

## 3. 스크립트 폴더 구조

```
Assets/Scripts/
├── Core/
│   ├── GameCardManager.cs      # 스와이프 카드 전환 로직
│   └── SupabaseQuizClient.cs   # Supabase Edge Function HTTP 클라이언트
├── Games/
│   ├── IMinigame.cs            # 미니게임 공통 인터페이스
│   ├── QuizGame.cs
│   ├── QuizQuestionService.cs  # 인메모리 캐시 + Supabase 호출
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
│   └── SupabaseSettings.cs     # ScriptableObject
└── UI/
    ├── LoadingIndicator.cs
    └── ShareButton.cs
```

---

## 4. 공통 인터페이스

```csharp
// Assets/Scripts/Games/IMinigame.cs
public interface IMinigame
{
    UniTask InitializeAsync();
    void OnGameStart();
    void OnGameEnd();
}
```

- `QuizGame`, `NpcChatGame`, `StoryRelayGame` 모두 `IMinigame` 구현
- `GameCardManager`는 `IMinigame` 인터페이스로만 참조 (DIP)

---

## 5. 데이터 모델

```csharp
// Assets/Scripts/Data/QuizQuestion.cs
[System.Serializable]
public class QuizQuestion
{
    [JsonProperty("topic")]       public string Topic;
    [JsonProperty("question")]    public string Question;
    [JsonProperty("choices")]     public string[] Choices;     // 길이 4
    [JsonProperty("answerIndex")] public int AnswerIndex;      // 0~3
    [JsonProperty("explanation")] public string Explanation;
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

// 디스크 캐시 제거됨 — 서버(Supabase DB)가 캐시 담당
```

---

## 6. ScriptableObject 설정 파일

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

// Assets/Scripts/Settings/SupabaseSettings.cs
[CreateAssetMenu(fileName = "SupabaseSettings", menuName = "Settings/SupabaseSettings")]
public class SupabaseSettings : ScriptableObject
{
    public string ProjectUrl;    // https://{ref}.supabase.co
    public string AnonKey;       // 공개 키 (JWT), 클라이언트에 포함 가능
    public int TimeoutSeconds = 30;
}
```

설정 에셋 파일 경로: `Assets/Settings/*.asset`

---

## 7. 백엔드 아키텍처

### 구조

```
Unity 클라이언트
    └─ SupabaseQuizClient (MonoBehaviour)
           │  POST /functions/v1/generate-quiz
           │  POST /functions/v1/generate-comment
           ▼
    Supabase Edge Function (Deno)
           │  Gemini API 호출 (gemini-2.5-flash)
           │  API Key: 서버 환경변수 GEMINI_API_KEY
           ▼
    Supabase PostgreSQL
           └─ quiz_questions 테이블 (문제 캐시)
```

### SupabaseQuizClient

```csharp
// Assets/Scripts/Core/SupabaseQuizClient.cs
public class SupabaseQuizClient : MonoBehaviour
{
    // SupabaseSettings ScriptableObject 주입
    // Authorization: Bearer {AnonKey} 헤더로 인증
    UniTask<List<QuizQuestion>> GenerateQuestionsAsync(string topic, int count);
    UniTask<string> GenerateCommentAsync(string topic, int correctCount, int totalCount);
}
```

### Supabase Edge Functions

| 함수명 | 입력 | 동작 |
|--------|------|------|
| `generate-quiz` | `{ topic, count }` | play_count 초과 문제 삭제 → DB 캐시 확인 → 부족 시 Gemini 호출 → DB 저장 → 반환 |
| `generate-comment` | `{ topic, correctCount, totalCount }` | DB 캐시 확인 → 부족 시 Gemini 호출 → DB 저장 → 반환 |

### DB 테이블 (PostgreSQL)

```sql
-- 퀴즈 문제 캐시
CREATE TABLE quiz_questions (
  id          UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  topic       TEXT NOT NULL,
  question    TEXT NOT NULL,
  choices     JSONB NOT NULL,   -- string[4]
  answer      INT NOT NULL,     -- 0~3 인덱스
  explanation TEXT,
  play_count  INT NOT NULL DEFAULT 0,  -- 출제 횟수 (초과 시 삭제)
  created_at  TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX ON quiz_questions (topic);

-- 총평 코멘트 캐시 (topic + correct_count별)
CREATE TABLE quiz_comments (
  id            UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  topic         TEXT NOT NULL,
  correct_count INT NOT NULL,
  total_count   INT NOT NULL,
  comment       TEXT NOT NULL,
  created_at    TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX ON quiz_comments (topic, correct_count);

-- 게임 설정값 (key/value)
CREATE TABLE "GameConfig" (
  key   TEXT PRIMARY KEY,
  value TEXT NOT NULL
);
```

**GameConfig 초기값:**

| key | value | 설명 |
|-----|-------|------|
| `QUIZ_MAX_PLAY_COUNT` | `5` | 문제가 삭제되기까지 최대 출제 횟수 |
| `QUIZ_COMMENT_CACHE_SIZE` | `10` | 토픽+정답수 조합별 코멘트 캐시 개수 |

> 모든 테이블은 RLS 활성화. 외부 직접 접근 불가, Edge Function(Service Role Key)을 통해서만 접근.

### QuizQuestionService (캐시 전략)

```
인메모리 캐시: Dictionary<string, List<QuizQuestion>> (세션 내 중복 호출 방지)
전략:
  - 인메모리 캐시 히트 → 랜덤 선택 반환
  - 미스 → SupabaseQuizClient.GenerateQuestionsAsync 호출
         (서버: play_count 초과 문제 삭제 → DB 캐시 확인 → 부족 시 Gemini 생성 → DB 저장)
  - 실패 → null 반환 (호출부에서 폴백)
```

---

## 8. 시스템 프롬프트

시스템 프롬프트는 클라이언트가 아닌 **Supabase Edge Function 서버 코드** 안에 포함됨.

| 함수 | 프롬프트 요약 |
|------|-------------|
| `generate-quiz` | 4지선다 퀴즈 5개를 JSON 배열로만 출력 |
| `generate-comment` | 디시인사이드 말투로 2~3문장 총평, 욕설 제외 |

---

## 9. 코딩 컨벤션

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

### 초기화 (Awake / Start)

```csharp
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
    default:
        Debug.LogError($"미구현 GameType: {gameType}", this);
        break;
}
```

### 기타

- 빈 생명주기 메서드 (`Start()`, `Update()` 등) 로직 없으면 **작성 금지**
- 주석은 **한국어** 작성
- 메서드 간 빈 줄 1줄 유지
- 로그 두 번째 인자로 context 전달 필수: `Debug.LogError("...", this)`

### SOLID 원칙

| 원칙 | 적용 방법 |
|------|---------|
| SRP | API 호출(SupabaseQuizClient) ↔ 게임 로직(XxxGame) ↔ UI(UXML) 완전 분리 |
| OCP | 새 미니게임 추가 시 `IMinigame` 구현만 추가, 기존 코드 수정 없음 |
| LSP | 모든 미니게임 클래스는 `IMinigame`을 완전히 대체 가능하게 구현 |
| ISP | 게임 로직 인터페이스(`IMinigame`)와 UI 바인딩 로직 혼합 금지 |
| DIP | `GameCardManager`는 `IMinigame`에 의존, 구체 클래스 직접 참조 금지 |

---

## 10. 의존성 패키지

| 패키지 | Package ID | 용도 |
|--------|-----------|------|
| UniTask | `com.cysharp.unitask` | async/await API 호출 |
| Newtonsoft.Json | `com.unity.nuget.newtonsoft-json` | JSON 파싱 |
| UI Toolkit | Unity 내장 | UI (별도 설치 불필요) |

Package Manager → Add by name으로 설치.

---

## 11. .gitignore 추가 항목

```
# Supabase AnonKey는 공개 키이므로 gitignore 불필요
# Gemini API Key는 Supabase 서버 환경변수로 이전됨 (클라이언트에 미포함)
```
