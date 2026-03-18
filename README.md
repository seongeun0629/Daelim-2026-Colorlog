# Daelim-2026-Colorlog
# 🎨 Colorlog: WPF 기반 퍼스널 컬러 및 피부 분석 시스템

> **대림대학교 소프트웨어학부 응용SW전공 2026 졸업 작품** > AR 기술과 자체 분석 알고리즘을 결합한 데스크톱 정밀 뷰티 진단 솔루션

---

## 📌 1. 프로젝트 개요 (Project Overview)
기존의 고가 오프라인 퍼스널 컬러 컨설팅의 비용 및 시간적 진입 장벽을 해결하기 위해 기획되었습니다. 사용자는 별도의 센서 없이 **일반 웹캠 환경**에서도 전문적인 피부 분석과 컬러 진단을 받을 수 있습니다.

- **개발 기간**: 2026.03 ~ 2026.11 (예정)
- **개발 환경**: Windows 10/11
- **주요 타겟**: 퍼스널 컬러에 관심이 있으나 비용이 부담되는 사용자, 체계적인 피부 관리를 원하는 사용자

## 🚀 2. 핵심 기능 (Key Features)
- **정밀 진단 엔진**: Unity AR Foundation 기반 실시간 안면 추적 및 특징점 추출
- **환경 보정 알고리즘**: OpenCV를 활용한 조명 색온도 및 화이트 밸런스 실시간 보정
- **맞춤형 추천**: 분석된 퍼스널 컬러 및 피부 상태에 따른 화장품 성분 매칭
- **히스토리 관리**: SQLite를 활용한 일자별 진단 기록 로컬 저장 및 트래킹

## 🛠 3. 기술 스택 (Tech Stack)
### **Framework & Language**
- **Language**: C# 12.0
- **Framework**: .NET 8.0 / WPF (Windows Presentation Foundation)
- **Pattern**: MVVM (Model-View-ViewModel)

### **Libraries & Tools**
- **Vision/AR**: Unity AR Foundation, OpenCVSharp
- **Database**: SQLite
- **Communication**: WeakReferenceMessenger (MVVM Toolkit)

## 📂 4. 시스템 구조 (System Architecture)

- **View (XAML)**: 사용자 인터페이스 및 애니메이션 처리
- **ViewModel**: 비즈니스 로직과 UI 간의 데이터 바인딩 및 명령 처리
- **Model**: 데이터 정의 및 SQLite 연동
- **Core Logic**: AR 데이터 기반의 자체 분석 알고리즘 엔진

## 👥 5. 팀원 소개 (Team Members)
| 이름 | 역할 | 담당 업무 |
| :--- | :---: | :--- |
| **연성은** | **팀장** | **시스템 아키텍처 설계, 전체 로직 통합, WPF/MVVM 구조 설계** |
| 이재형 | 팀원 | Unity AR Foundation 모듈 구현, 실시간 안면 트래킹 최적화 |
| 박시은 | 팀원 | UI/UX 디자인 (XAML), SQLite 데이터베이스 스키마 설계 |
| 조한슬 | 팀원 | 분석 알고리즘 리서치, 데이터셋 구축 및 품질 보증(QA) |

---
© 2026 두쫀쿠 Team. All rights reserved.
