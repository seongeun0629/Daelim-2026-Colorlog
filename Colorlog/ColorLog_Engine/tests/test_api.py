"""
API 서버 테스트 스크립트
"""

import sys
import os
import time
import asyncio

PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

import requests

API_BASE = "http://127.0.0.1:5000/api"


def test_health():
    """헬스 체크"""
    print("1️⃣  헬스 체크 중...")
    try:
        response = requests.get(f"{API_BASE}/health", timeout=2)
        if response.status_code == 200:
            print(f"   ✅ 헬스 체크 성공: {response.json()}")
            return True
    except Exception as e:
        print(f"   ❌ 헬스 체크 실패: {e}")
    return False


def test_config():
    """설정 조회"""
    print("\n2️⃣  설정 조회 중...")
    try:
        response = requests.get(f"{API_BASE}/config", timeout=2)
        if response.status_code == 200:
            print(f"   ✅ 설정 조회 성공: {response.json()}")
            return True
    except Exception as e:
        print(f"   ❌ 설정 조회 실패: {e}")
    return False


def test_status():
    """상태 조회"""
    print("\n3️⃣  상태 조회 중...")
    try:
        response = requests.get(f"{API_BASE}/status", timeout=2)
        if response.status_code == 200:
            print(f"   ✅ 상태 조회 성공: {response.json()}")
            return True
    except Exception as e:
        print(f"   ❌ 상태 조회 실패: {e}")
    return False


def test_start_stop():
    """시작/중지 테스트"""
    print("\n4️⃣  카메라 시작 중...")
    try:
        response = requests.post(f"{API_BASE}/start", timeout=5)
        if response.status_code == 200:
            print(f"   ✅ 시작 성공: {response.json()}")

            # 결과 조회 시도 (카메라가 없으면 에러일 수 있음)
            print("\n5️⃣  3초 후 결과 조회...")
            time.sleep(3)
            try:
                result = requests.get(f"{API_BASE}/result", timeout=2)
                print(f"   결과: {result.json()}")
            except Exception as e:
                print(f"   결과 조회 실패 (카메라 없음일 수 있음): {e}")

            # 중지
            print("\n6️⃣  카메라 중지 중...")
            response = requests.post(f"{API_BASE}/stop", timeout=5)
            if response.status_code == 200:
                print(f"   ✅ 중지 성공: {response.json()}")
                return True
    except Exception as e:
        print(f"   ❌ 시작/중지 실패: {e}")
    return False


if __name__ == "__main__":
    print("=" * 60)
    print("ColorLog API 테스트")
    print("=" * 60)
    print(f"API 주소: {API_BASE}\n")

    # API 서버 연결 재시도
    print("⏳ API 서버 연결 대기 중... (최대 10초)")
    for i in range(10):
        try:
            response = requests.get(f"{API_BASE}/health", timeout=1)
            if response.status_code == 200:
                print("✅ API 서버 연결됨\n")
                break
        except:
            print(".", end="", flush=True)
            time.sleep(1)
    else:
        print("\n⚠️  API 서버에 연결할 수 없습니다.")
        print("   다음 명령으로 서버를 시작하세요:")
        print("   python main_api.py")
        sys.exit(1)

    # 테스트 실행
    results = [
        test_health(),
        test_config(),
        test_status(),
        test_start_stop(),
    ]

    print("\n" + "=" * 60)
    print(f"테스트 완료: {sum(results)}/{len(results)} 성공")
    print("=" * 60)

