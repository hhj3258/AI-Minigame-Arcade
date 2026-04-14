# AI MiniGame Arcade

> Claude와 바이브코딩으로 만든 모바일 미니게임 모음 | Unity 6

[![Unity](https://img.shields.io/badge/Unity-6000.3.10f1-black?logo=unity)](https://unity.com/)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-brightgreen)]()
[![Status](https://img.shields.io/badge/Status-In%20Development-yellow)]()

---

## 🎮 미니게임

| 게임 | 설명 | AI 연동 | &nbsp;&nbsp;&nbsp;상태&nbsp;&nbsp;&nbsp; |
|------|------|---------|:----:|
| **AI Quiz** | AI가 출제한 5지선다 퀴즈. 주제 선택 후 문제 풀기, 종료 후 커뮤니티 스타일 AI 총평 | Gemini (문제 생성 + 총평) | ✅&nbsp;완료 |
| **Survivor** | 뱀서라이크 — 조이스틱으로 이동, 무기 자동 공격. 3종 무기 × 3레벨 업그레이드 | 없음 (순수 액션) | ✅&nbsp;완료 |
| **Story Relay** | Claude와 번갈아 이야기를 이어 단편소설 완성 | Claude (이야기 생성) | 🔜&nbsp;예정 |
| **Talk to NPC** | AI NPC와 대화하며 힌트를 모아 미션 클리어 | Claude (NPC 페르소나) | 🔜&nbsp;예정 |

---

## 🔧 개발 환경

- **Engine**: Unity 6000.3.10f1
- **Platform**: Android / iOS
- **Min SDK**: Android 7.1 (API 25)

---

## 🏗️ 아키텍처

```
Unity (UnityWebRequest)
    │  POST /functions/v1/generate-quiz
    │  POST /functions/v1/generate-comment
    ▼
Supabase Edge Function (Deno)
    │  DB 캐시 확인 → 캐시 미스 시 Gemini 호출
    ▼
Gemini API (gemini-2.0-flash-lite)    PostgreSQL (quiz_questions / quiz_comments)
```

- AI API Key는 Supabase 서버 환경변수로 관리. 클라이언트에 미포함.
- DB는 RLS 활성화. Edge Function(Service Role Key)을 통해서만 접근.

---

## 📂 프로젝트 구조

```
AIGameDemo/
├── Docs/
│   ├── GameDesign.md     게임 기획 (미니게임 4종, 로드맵, 리스크)
│   ├── Program.md        프로그래밍 스펙 (씬 구성, 스크립트 구조, API)
│   ├── UI.md             UI/UX 스펙 (스와이프 카드, UXML/USS 구조)
│   ├── Art.md            아트 스펙 (컬러, 타이포, 애니메이션)
│   └── Build.md          빌드 가이드 (Android APK, Addressables)
├── UIPrototype/          UI 프로토타입
└── Unity/                Unity 프로젝트
    └── Assets/
        └── Scripts/
            ├── Core/         앱 초기화, 카드 스와이프, Safe Area
            ├── QuizGame/     퀴즈 게임 로직 + Supabase 통신
            ├── SurvivorGame/ 뱀서라이크 게임 로직
            ├── UI/           UIToolkit 공통 애니메이션 유틸
            ├── Settings/     ScriptableObject 설정
            └── Editor/       에디터 전용 툴 (빌드 스크립트 등)
```

---

## 🚀 실행 방법

1. Unity 6000.3.10f1 이상에서 `Unity/` 폴더 열기
2. `Unity/Assets/Scenes/MainScene.unity` 씬 실행
3. 위아래 스와이프로 미니게임 전환

---

## 📦 의존성 패키지

| 패키지 | 용도 |
|--------|------|
| UniTask | async/await |
| Newtonsoft.Json | JSON 파싱 |
| Input System | 터치/마우스 입력 |
| UI Toolkit (내장) | UXML + USS UI |
| Addressables | 무기 프리팹 런타임 로드 |

---

## 🗺️ 개발 로드맵

| 단계 | 내용 | 상태 |
|------|------|------|
| Phase 1 | 퀴즈 게임 + 스와이프 카드 UI | ✅ 완료 |
| Phase 2 | 뱀서라이크 + NPC 대화 | 🔄 진행 중 |
| Phase 3 | 스토리 릴레이 + Claude 연동 고도화 | 🔜 예정 |
| Phase 4 | 폴리싱 + 빌드 | 🔜 예정 |

---

## 📄 문서

| 파일 | 내용 |
|------|------|
| [`Docs/GameDesign.md`](Docs/GameDesign.md) | 게임 기획 (미니게임 4종, 로드맵, 리스크) |
| [`Docs/Program.md`](Docs/Program.md) | 프로그래밍 스펙 (구조, 코딩 컨벤션, API) |
| [`Docs/UI.md`](Docs/UI.md) | UI/UX 스펙 (스와이프 카드, UXML/USS 구조) |
| [`Docs/Art.md`](Docs/Art.md) | 아트 스펙 (컬러, 타이포, 애니메이션) |
| [`Docs/Build.md`](Docs/Build.md) | 빌드 가이드 (Android APK, Addressables) |
