# 프로그래밍 스펙 — AI Mini Arcade

> Unity 6000.3.10f1 기준.
> 코딩 컨벤션: `C:\Users\WN-ND000431\Desktop\MyClaude\config\naming-conventions.mdc`

---

## 1. 씬 구성

단일 씬(`MainScene`). 씬 전환 없음.

```
MainCamera          [Camera, AudioListener]
EventSystem         [EventSystem, InputSystemUIInputModule]
QuizUI_Toolkit      [UIDocument, QuizUIGame, SupabaseQuizClient]
```

---

## 2. UI 스택

**UIToolkit** 기반. UGUI / LeanGUI / Canvas 사용하지 않음.

| 파일 | 역할 |
|------|------|
| `QuizPanels.uxml` | 전체 패널 레이아웃 |
| `QuizPanels.uss` | 스타일시트 |
| `QuizPanelSettings.asset` | PanelSettings |

패널 전환: USS `.hidden` 클래스 추가/제거

---

## 3. 스크립트 구조

> 현재 `Assets/TestUIToolkit/`에 구현 중. 안정화 후 `Assets/Scripts/`로 이동 예정.

| 스크립트 | 위치 | 역할 |
|---------|------|------|
| `QuizUIGame.cs` | `TestUIToolkit/` | 퀴즈 게임 컨트롤러 (UIDocument 기반) |
| `QuizUITabController.cs` | `TestUIToolkit/` | 탭 네비게이션 |
| `SupabaseQuizClient.cs` | `Scripts/Core/` | Supabase HTTP 클라이언트 |
| `QuizQuestionService.cs` | `Scripts/Games/` | 인메모리 캐시 + Supabase 호출 |
| `IMinigame.cs` | `Scripts/Games/` | 미니게임 공통 인터페이스 |
| `QuizSettings.cs` | `Scripts/Settings/` | ScriptableObject 설정 |
| `SupabaseSettings.cs` | `Scripts/Settings/` | ScriptableObject 설정 |
| `QuizQuestion.cs` | `Scripts/Data/` | 데이터 모델 |

---

## 4. 백엔드 아키텍처

```
Unity (SupabaseQuizClient)
    │  POST /functions/v1/generate-quiz
    │  POST /functions/v1/generate-comment
    ▼
Supabase Edge Function (Deno + Gemini 2.5 Flash Lite)
    ▼
Supabase PostgreSQL (quiz_questions / quiz_comments / GameConfig)
```

- API Key는 서버 환경변수(`GEMINI_API_KEY`)로 관리. 클라이언트에 미포함.
- DB는 RLS 활성화. Edge Function(Service Role Key)을 통해서만 접근.
- 시스템 프롬프트는 Edge Function 서버 코드 내에 포함.

| Edge Function | 동작 |
|---------------|------|
| `generate-quiz` | DB 캐시 확인 → 부족 시 Gemini 생성 → 저장 → 반환 |
| `generate-comment` | DB 캐시 확인 → 부족 시 Gemini 생성 → 저장 → 반환 |

---

## 5. 의존성 패키지

| 패키지 | 용도 |
|--------|------|
| UniTask (`com.cysharp.unitask`) | async/await |
| Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`) | JSON 파싱 |
| Input System (`com.unity.inputsystem`) | 터치/마우스 입력 |
| UI Toolkit (Unity 내장) | UXML + USS UI |

> LeanGUI는 프로젝트에 에셋으로 존재하나 현재 미사용.
