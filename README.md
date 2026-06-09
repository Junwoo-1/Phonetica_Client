# Phonetica Client

**Phonetica Client**는 사용자의 한국어 발음을 자모(초성/중성/종성) 단위로 정밀하게 평가하고 피드백을 제공하는 Phonetica 프로젝트의 Unity 기반 프론트엔드 애플리케이션입니다. 

사용자의 음성을 녹음하여 백엔드 서버에 전달하며, 서버의 정밀한 발음 채점 결과를 직관적인 음절 박스 UI로 시각화하여 사용자가 자신의 발음 취약점을 한눈에 파악할 수 있도록 돕습니다.

## 📌 주요 기능 (Key Features)

* **음성 녹음 및 오디오 처리**: 클라이언트 기기의 마이크를 활용하여 사용자의 음성을 녹음하고, 서버 처리에 적합한 포맷으로 변환하여 전송합니다.
* **실시간 채점 스트리밍 수신 (SSE)**: 서버의 채점 진행 상태(음원 로드, 음소 분석, G2P 변환, 정렬 등)를 Server-Sent Events(SSE)로 실시간 수신하여 로딩 UI 및 상태를 업데이트합니다.
* **직관적인 발음 피드백 시각화**: 서버로부터 전달받은 JSON 채점 데이터를 바탕으로 화면에 렌더링합니다.
  * **`detailed_jamos` 시각화**: 정답 단어(Ref) 기준의 음절 박스 UI에 색상 및 점수를 매핑하여, 사용자가 초/중/종성 중 어떤 부분에서 실수를 했는지 정확히 짚어줍니다.
  * **`heard_jamos` 시각화**: 사용자가 실제로 발음한 내역(Hyp)을 별도의 영역에 표시하여, 원래 단어와 어떻게 다르게 발음했는지 직관적으로 비교할 수 있습니다.

## 🛠 기술 스택 (Tech Stack)

* **Game Engine**: Unity 3D
* **Programming Language**: C#
* **Graphics / UI**: ShaderLab, HLSL (Unity UI 및 커스텀 셰이더 적용)
* **Network**: Client-Server REST API & SSE 통신

## 🔗 연동 서버 (Server Repository)

본 클라이언트 앱이 동작하기 위해서는 발음 평가 AI 모델을 서빙하는 백엔드 서버가 필요합니다. 서버 구축 및 실행 방법은 아래 리포지토리를 참조해 주세요.

* **[Phonetica-server 저장소 바로가기](https://github.com/G4tsby/Phonetica-server)**
* **서버 역할**: Kkonjeong/wav2vec2-base-korean 모델을 이용한 음성 인식, 후보 단어에 대한 g2p 변환, 그리고 가중 Needleman-Wunsch 알고리즘을 통한 자모 단위 점수 산출 및 SSE 이벤트 응답.

## 🚀 통신 흐름 (Client-Server Flow)

1. **요청 전송**: 사용자가 발음한 음성 파일(.wav 등)과 대상 후보 단어 리스트(`candidates`)를 멀티파트 폼 데이터(Multipart form-data) 형식으로 서버의 `POST /pronounce` 엔드포인트에 전송합니다.
2. **이벤트 스트리밍**: 클라이언트는 서버로부터 다음의 이벤트를 순차적으로 스트리밍 받습니다.
   `accepted` → `audio_loaded` → `phoneme_completed` → `g2p_completed` → `alignment_completed` → `score` → `done`
3. **결과 파싱 및 UI 반영**: 최종 `score` 이벤트 수신 시, 페이로드 내에 있는 발음 정확도 종합 점수(`overall_score`)와 자모별 디테일 배열을 파싱하여 Unity Canvas 상의 음절 UI 박스를 업데이트합니다.

## ⚙️ 설치 및 실행 방법 (Getting Started)

1. 본 저장소를 로컬 환경에 클론합니다.
   ```bash
   git clone [https://github.com/Junwoo-1/Phonetica_Client.git](https://github.com/Junwoo-1/Phonetica_Client.git)
2. Unity Hub를 실행하고 [Add] 또는 [열기] 버튼을 눌러 복제한 Phonetica_Client 폴더를 프로젝트로 추가합니다.

3. Unity Editor에서 프로젝트를 엽니다.

4. 백엔드 서버(Phonetica-server)가 로컬(localhost:8000) 또는 지정된 원격 서버에서 실행 중인지 확인합니다.

5. Unity 프로젝트 내 네트워크 매니저 또는 설정 스크립트에서 서버의 API Endpoint URL이 백엔드 주소와 일치하는지 확인 및 수정합니다.

6. Unity Editor의 Play 버튼(▶)을 눌러 테스트를 진행하거나, 타겟 플랫폼(Android, iOS, PC 등)에 맞춰 Build합니다.

## 📁 프로젝트 구조 (Project Structure)
Assets/: C# 스크립트, UI 프리팹, 씬(Scene), 셰이더, 사운드, 메타데이터 등 실질적인 게임/앱 리소스 포함

Packages/: Unity 패키지 매니저 및 외부 라이브러리 종속성 설정

ProjectSettings/: Unity 플레이어, 입력, 품질, 그래픽 등 글로벌 프로젝트 설정 파일

.vscode/, .vsconfig/, Phonetica_Client.slnx: IDE(Visual Studio 등) 연동 및 설정 파일

Developed for the Phonetica Project.
