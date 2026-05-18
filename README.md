# 🎨 Colorlog: WPF 기반 정밀 퍼스널 컬러 및 피부 분석 시스템

> **대림대학교 소프트웨어학부 응용SW전공 2026 졸업 작품**
> 윈도우 데스크톱 환경에 최적화된 고성능 비전 엔진 기반의 뷰티 진단 솔루션

---

## 📌 1. 프로젝트 개요 (Project Overview)
기존 오프라인 컨설팅의 공간적/비용적 제약을 극복하기 위해 기획되었습니다. 본 프로젝트는 모바일 환경의 센서 의존도를 낮추고, **데스크톱의 강력한 연산 자원**을 활용하여 일반 웹캠에서도 전문가 수준의 정밀한 피부 분석 및 컬러 진단 기능을 제공합니다.

- **개발 기간**: 2026.03 ~ 2026.11 (예정)
- **개발 환경**: Windows 10/11 (Desktop)
- **핵심 가치**: 정확성(Accuracy), 접근성(Accessibility), 데이터 관리(History)

## 🚀 2. 핵심 기능 (Key Features)
- **자체 비전 엔진 (Vision Engine)**: OpenCVSharp 및 Dlib을 활용한 실시간 안면 특징점(68 Landmarks) 추출
- **지능형 환경 보정**: 실시간 조명 색온도 감지 및 화이트 밸런스 자동 보정 알고리즘
- **정밀 피부 분석**: 픽셀 단위의 RGB/Lab 색상 추출 및 피부 톤 균일도 수치화
- **개인화 데이터셋**: 분석 결과 기반의 화장품 성분 매칭 및 SQLite 기반 히스토리 트래킹

## 🛠 3. 기술 스택 (Tech Stack)
### **Framework & Language**
- **Language**: C# 12.0 / .NET 8.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Pattern**: MVVM (Model-View-ViewModel)

### **Computer Vision & Library**
- **Image Processing**: OpenCVSharp 4.x
- **Face Tracking**: DlibDotNet (68 Face Landmark Detection)
- **Database**: SQLite
- **Communication**: CommunityToolkit.Mvvm (WeakReferenceMessenger)

## 📂 4. 시스템 구조 (System Architecture)
### **Analysis Pipeline**
1. **Preprocessing**: 웹캠 프레임 캡처 및 노이즈 제거
2. **Calibration**: 주변 광원에 따른 실시간 화이트 밸런스 및 노출 보정
3. **Detection**: Dlib을 활용한 안면 68개 특징점 매핑
4. **Analysis**: 특정 ROI(관심 영역) 내 픽셀 분석 및 컬러 판별 로직 가동
5. **Output**: 분석 결과 시각화 및 데이터베이스 저장

## 👥 5. 팀원 소개 (Team Members)
| 이름 | 역할 | 주요 담당 업무 (Main Responsibilities) |
| :--- | :---: | :--- |
| **연성은** | **팀장** | **시스템 아키텍처 및 로직 통합 총괄**, 알고리즘 설계 및 WPF UI/UX 구조 관리, **전체 코드 리뷰 및 통합 테스트(QA) 주관** |
| **이재형** | **팀원** | **비전 엔진 심화 개발**, OpenCVSharp/Dlib 안면 인식 알고리즘 구현, **알고리즘 정밀도 및 성능 최적화 테스트(QA)** |
| **박시은** | **팀원** | **백엔드 및 데이터 관리**, SQLite DB 설계 및 데이터 연동, **데이터 정밀도 검증 및 시스템 예외 처리(QA)** |
| **조한슬** | **팀원** | **프론트엔드 인터페이스 구현**, WPF XAML 분석 결과 시각화, **UI/UX 사용성 테스트 및 시나리오 검증(QA)** |

> **※ 공통 사항**: 프로젝트 초기 단계에서는 전 팀원이 'C# 기반 안면 특징점 추출 엔진' 구축을 위한 R&D 및 공동 코딩을 수행함.

---
© 2026 두쫀쿠 Team. All rights reserved.
