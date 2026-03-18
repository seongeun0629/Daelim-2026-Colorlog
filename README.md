# 🎨 Colorlog: WPF 기반 정밀 퍼스널 컬러 및 피부 분석 시스템

> **대림대학교 소프트웨어학부 응용SW전공 2026 졸업 작품** > 윈도우 데스크톱 환경에 최적화된 고성능 비전 엔진 기반의 뷰티 진단 솔루션

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
- **Language**: C# 12.0
- **Framework**: .NET 8.0 / WPF (Windows Presentation Foundation)
- **Pattern**: MVVM (Model-View-ViewModel)

### **Computer Vision & Library**
- **Image Processing**: OpenCVSharp 4.x
- **Face Tracking**: DlibDotNet (68 Face Landmark Detection)
- **Database**: SQLite
- **Communication**: CommunityToolkit.Mvvm (WeakReferenceMessenger)

## 📂 4. 시스템 구조 (System Architecture)

- **View (XAML)**: 고해상도 대시보드 UI 및 실시간 영상 출력 전용 컨트롤
- **ViewModel**: 분석 로직과 UI 간의 실시간 데이터 바인딩 및 상태 제어
- **Vision Logic**: 웹캠 프레임 캡처 -> 영상 보정 -> 특징점 추출 -> 색상 분석 파이프라인
- **Model**: 사용자 진단 데이터 및 제품 매칭 데이터 정의

## 👥 5. 팀원 소개 (Team Members)
| 이름 | 역할 | 담당 업무 |
| :--- | :---: | :--- |
| **연성은** | **팀장** | **시스템 아키텍처 설계, 전체 로직 통합, WPF/MVVM 구조 설계** |
| 이재형 | 팀원 | OpenCVSharp 기반 비전 엔진 구현, 안면 특징점 추출 최적화 |
| 박시은 | 팀원 | UI/UX 디자인 (XAML), SQLite 데이터베이스 스키마 설계 |
| 조한슬 | 팀원 | 피부 분석 알고리즘 리서치, 컬러 데이터셋 구축 및 QA |

---
© 2026 두쫀쿠 Team. All rights reserved.
