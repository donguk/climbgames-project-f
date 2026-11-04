# project-f

## 프로젝트 설명
- **장르:** 3D 낚시 시뮬레이션
- **핵심 메커니즘:** 
  - 3D 공간 월드에서 낚시포인트로 이동, 뷰 전환 후 1인칭으로 낚시
  - **Verlet Integration** 기반의 로프 물리 및 장력(Tension) 시스템
  - **FSM(Finite State Machine)** 기반의 캐릭터 및 물고기 행동 패턴 제어

## 기술 스택
- **Engine:** Unity6.3
- **Language:** C# 9.0.312
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Framework:** ClimbGames Framework 커스텀 패키지 ([GitHub Repository](https://github.com/donguk/climbgames-framework/tree/develop))

### ClimbGames Framework 구현 항목
- UniTask 기반 비동기 씬 전환 매니저
- `(Mono)Singleton` 구조 및 제네릭 FSM 시스템
- 카메라 스택 기반 UIManager 레이어 관리

## 코딩 및 개발 규칙 (Coding Conventions)
- **C# Naming:**
  - Private fields: `camelCase` (`fieldName`)
  - Properties & Public Methods: `PascalCase` (`UpdateTension`)
- **Architecture:**
  - 비동기 작업은 `UniTask` 사용

## 폴더 구조 및 설명
```text
Assets/
└── ClimbGames/                     
    └── Project F/                  
        ├── Data/                   # 게임 데이터 및 ScriptableObject
        ├── Scenes/                 # 게임 씬 및 월드 nevmesh
        ├── Scripts/                
        │   ├── AppLauncher/        # 앱 시작 체크 작업        
        │   ├── Fishing/            # 낚시 시스템 루트
        │   │   ├── Actor/          # 1인칭 캐릭터 fsm
        │   │   ├── Component/      # 낚시대, 찌, 바늘, 줄에서 사용되는 컴포넌트
        │   │   │   └── Core/       # 낚시 줄 코어 시스템(verlet line)
        │   │   ├── Fish/           # 물고기 fsm
        │   │   └── Info/           # 낚시 시뮬레이션 중 전달되는 정보
        │   ├── UI/                 # UI 컴포넌트
        │   └── World/              # 낚시 월드 및 카메라 스크립트
        └── Sources/                
            ├── Animator/           # 1인칭 캐릭터 애니메이션 컨트롤러 리소스
            └── Prefab/             # 1인칭 캐릭터, 물고기, 낚시 장비, UI 프리팹 리소스
```