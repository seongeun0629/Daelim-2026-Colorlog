"""
colorlog DB 스키마 설정 파일
- 이 파일을 실행하면 colorlog.db 파일이 생성되고 테이블이 만들어집니다.
- 설계 범위: 사용자(users), 진단결과(diagnosis) 테이블
"""

import sqlite3
import os

# DB 파일 경로: 이 파일(schema.py)이 있는 db 폴더 안에 colorlog.db 생성
DB_PATH = os.path.join(os.path.dirname(__file__), "colorlog.db")


def get_connection():
    """DB에 연결하고 연결 객체를 반환합니다."""
    conn = sqlite3.connect(DB_PATH)
    # 외래 키(Foreign Key) 기능을 활성화합니다 (SQLite는 기본이 꺼져 있음)
    conn.execute("PRAGMA foreign_keys = ON")
    # 쿼리 결과를 컬럼 이름으로 접근할 수 있게 설정합니다 (row["user_id"] 처럼 사용 가능)
    conn.row_factory = sqlite3.Row
    return conn


def create_tables():
    """users, diagnosis 테이블을 생성합니다. 이미 있으면 건너뜁니다."""
    conn = get_connection()
    cursor = conn.cursor()

    # ─────────────────────────────────────────────────────────────────
    # 테이블 1: users (사용자)
    # 앱을 사용하는 사람의 기본 정보를 저장합니다.
    # ─────────────────────────────────────────────────────────────────
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS users (
            user_id     INTEGER PRIMARY KEY AUTOINCREMENT,
                        -- 사용자 고유 번호 (자동으로 1, 2, 3... 증가)

            user_name   TEXT    NOT NULL,
                        -- 사용자 이름 (반드시 입력해야 함)

            gender      TEXT,
                        -- 성별 예: '남', '여', NULL(입력 안 함)

            age         TEXT,
                        -- 나이 예: '20대', '30대', NULL(입력 안 함)

            created_at  TEXT    NOT NULL
                        -- 가입 날짜·시간 예: '2026-05-12 14:30:00'
        )
    """)

    # ─────────────────────────────────────────────────────────────────
    # 테이블 2: diagnosis (진단결과)
    # 퍼스널 컬러 진단 1회의 측정값과 결과를 저장합니다.
    # ─────────────────────────────────────────────────────────────────
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS diagnosis (
            diagnosis_id    INTEGER PRIMARY KEY AUTOINCREMENT,
                            -- 진단 고유 번호 (자동 증가)

            diagnosis_at    TEXT    NOT NULL,
                            -- 진단한 날짜·시간 예: '2026-05-12 14:35:00'

            rpv_a           REAL    NOT NULL,
                            -- 퍼스널컬러 측정값 A (소수점 포함 숫자, 필수)

            rpv_b           REAL    NOT NULL,
                            -- 퍼스널컬러 측정값 B (소수점 포함 숫자, 필수)

            lab_a           REAL,
                            -- Lab 색상값 A (선택 입력)

            lab_b           REAL,
                            -- Lab 색상값 B (선택 입력)

            lab_c           REAL,
                            -- Lab 색상값 C (선택 입력)

            landmark        REAL,
                            -- 얼굴 랜드마크 측정값 (선택 입력)

            type_id         INTEGER,
                            -- 진단 결과 퍼스널컬러 유형 번호 (선택 입력)
                            -- 추후 personal_color_types 테이블과 연결될 예정

            user_id         INTEGER NOT NULL,
                            -- 이 진단을 받은 사용자 번호 (users 테이블의 user_id 참조)

            FOREIGN KEY (user_id) REFERENCES users(user_id)
                            -- user_id는 반드시 users 테이블에 존재하는 번호여야 함
        )
    """)

    conn.commit()   # 변경사항을 DB에 저장
    conn.close()    # 연결 종료
    print("테이블 생성 완료: users, diagnosis")


# 이 파일을 직접 실행할 때만 아래 코드가 동작합니다.
# (다른 파일에서 import 할 때는 실행되지 않습니다)
if __name__ == "__main__":
    create_tables()
    print(f"DB 경로: {DB_PATH}")
