# 빌드 가이드 — AI Mini Arcade

> Unity 6000.3.10f1 / Android (IL2CPP, ARM64)

---

## 1. 프로젝트 설정

| 항목 | 값 |
|------|-----|
| Company Name | `MyLittleWorks` |
| Product Name | `AI Mini Arcade` |
| Bundle Identifier (Android) | `com.mylittleworks.aigamedemo` |
| Version | `0.1.0` |
| Min SDK | 25 (Android 7.1) |
| Target SDK | 34 (Android 14) |
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 |
| 화면 방향 | Portrait 고정 (Upside Down / Landscape 비활성화) |

---

## 2. 빌드 방법

### 자동 빌드 (권장)

Unity 메뉴 → **Tools > Build Android APK**

- 스크립트: `Assets/Editor/AndroidBuildScript.cs`
- 출력 경로: `Builds/AIMiniArcade.apk`
- Development Build 옵션 포함

### 수동 빌드

1. **File > Build Settings** → Platform: Android 선택
2. **Switch Platform**
3. **Build** 클릭 → APK 파일 저장 위치 지정

---

## 3. Development Build

- 현재 빌드 옵션: **Development Build 활성화** (`BuildOptions.Development`)
- 릴리스 배포 시: `AndroidBuildScript.cs`의 `BuildOptions.Development` → `BuildOptions.None`으로 변경

---

## 4. Addressables

### 빌드 전 주의사항

Addressables 콘텐츠를 먼저 빌드해야 APK에 포함된다.

**Window > Asset Management > Addressables > Groups** → **Build > New Build > Default Build Script**

### 그룹 구성

| 그룹 | 내용 | 로드 방식 |
|------|------|-----------|
| Default Local Group | 기본 에셋 | Local |
| Weapons | 무기 Sprite 에셋 (샷건/오브/미사일) | Local |

> **주의**: 그룹의 `m_Schemas`가 비어있으면 빌드에서 제외됨.
> 새 그룹 추가 시 Default Local Group의 스키마를 복사하여 적용할 것.

---

## 5. 네트워크 (Supabase Edge Functions)

### SSL 인증서

- Android 7.1 이하: Let's Encrypt ISRG Root X1 인증서 미신뢰 → SSL CA 오류 발생
- **Development Build** 시: `BypassCertificateHandler`로 자동 우회 (`#if DEVELOPMENT_BUILD`)
- **Release Build** 시: 우회 없음 → Android 7.1+ 기기 필수, 또는 별도 CA 번들 처리 필요

### 관련 파일

- `Assets/Scripts/QuizGame/SupabaseQuizClient.cs` — `BypassCertificateHandler` 내부 클래스

---

## 6. IL2CPP 주의사항

현재 Managed Stripping Level: **Minimal** (기본값)

- Minimal에서는 리플렉션 타입 strip 위험이 낮음
- Stripping Level을 **Medium 이상**으로 올릴 경우 `Assets/link.xml` 생성 필요:

```xml
<linker>
  <assembly fullname="Newtonsoft.Json" preserve="all"/>
  <assembly fullname="Assembly-CSharp">
    <type fullname="QuizQuestion" preserve="all"/>
    <type fullname="SupabaseQuizClient" preserve="all"/>
    <type fullname="SupabaseQuizClient+CommentResponse" preserve="all"/>
  </assembly>
</linker>
```

---

## 7. Safe Area

노치/펀치홀 기기 대응.

- 스크립트: `Assets/Scripts/Core/SafeAreaApplier.cs`
- 적용 대상: `GameCard_0` (QuizGame), `GameCard_1` (SurvivorGame) GameObject에 컴포넌트 추가
- 상세 주의사항: [UI.md — Safe Area 적용 방식](UI.md#safe-area-적용-방식) 참고

---

## 8. 퍼포먼스 설정

- `targetFrameRate`: 기기 실제 주사율에 맞춤 (`Screen.currentResolution.refreshRateRatio.value`)
- 설정 위치: `Assets/Scripts/SurvivorGame/SurvivorGame.cs` `Awake()`

---

## 9. 인게임 디버그 콘솔

모바일 런타임 로그 확인용.

- 패키지: `com.yasirkula.ingamedebugconsole`
- 씬에 IngameDebugConsole 프리팹 배치 (`Packages/IngameDebugConsole/...`)
- `Debug.Log/Warning/Error` 자동 출력
- 릴리스 배포 전 씬에서 제거 또는 비활성화 권장

---

## 10. 검증 체크리스트

### 에디터 (빌드 전)

- [ ] Game View 해상도 1080×1920 Portrait로 설정
- [ ] 카드 스와이프 전환 동작
- [ ] VirtualJoystick 마우스 반응
- [ ] QuizGame 버튼 동작 및 AI 문제 생성
- [ ] SurvivorGame 웨폰 업그레이드 카드 표시

### 실기기 (APK 설치 후)

- [ ] 노치/펀치홀 기기에서 Safe Area 정상 적용
- [ ] 퀴즈 AI 문제 생성 (Supabase 통신)
- [ ] SurvivorGame 웨폰 표시 (Addressables 로드)
- [ ] 인게임 콘솔 로그 정상 출력 (Development Build)
