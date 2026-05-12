"""
colorlog DB 스키마 설정 파일
- 이 파일을 실행하면 colorlog.db 파일이 생성되고 테이블이 만들어집니다.
- 설계 범위: 5개 테이블 전체 (ERD 기준)

테이블 생성 순서 (외래키 때문에 참조되는 쪽을 먼저 만들어야 합니다)
  1. personal_color_types  ← diagnosis가 참조
  2. users                 ← diagnosis가 참조
  3. diagnosis             ← rec_products가 참조
  4. products              ← rec_products가 참조
  5. rec_products
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
    """5개 테이블을 생성합니다. 이미 있으면 건너뜁니다."""
    conn = get_connection()
    cursor = conn.cursor()

    # ─────────────────────────────────────────────────────────────────
    # 테이블 1: personal_color_types (퍼스널컬러 유형)
    # 봄 웜톤, 여름 쿨톤 등 퍼스널컬러 유형 정보를 저장합니다.
    # diagnosis 테이블이 이 테이블을 참조하므로 가장 먼저 만들어야 합니다.
    # ─────────────────────────────────────────────────────────────────
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS personal_color_types (
            type_id      INTEGER PRIMARY KEY AUTOINCREMENT,
                         -- 유형 고유 번호 (자동 증가)

            type_name    TEXT    NOT NULL,
                         -- 유형 이름 예: '봄 웜톤', '여름 쿨톤'

            colors       TEXT,
                         -- 어울리는 컬러 목록 예: '코랄, 피치, 아이보리'

            worst_colors TEXT,
                         -- 피해야 할 컬러 목록 예: '그레이, 블루블랙'

            tone         TEXT,
                         -- 웜/쿨 구분 예: '웜', '쿨'

            keyword      TEXT
                         -- 유형 키워드 예: '생기있는, 따뜻한, 화사한'
        )
    """)

    # ─────────────────────────────────────────────────────────────────
    # 테이블 2: users (사용자)
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
    # 테이블 3: diagnosis (진단결과)
    # 퍼스널 컬러 진단 1회의 측정값과 결과를 저장합니다.
    # users, personal_color_types 테이블을 참조합니다.
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
                            -- 진단 결과 퍼스널컬러 유형 번호
                            -- personal_color_types 테이블의 type_id 참조

            user_id         INTEGER NOT NULL,
                            -- 이 진단을 받은 사용자 번호 (users 테이블의 user_id 참조)

            FOREIGN KEY (user_id) REFERENCES users(user_id),
            FOREIGN KEY (type_id) REFERENCES personal_color_types(type_id)
        )
    """)

    # ─────────────────────────────────────────────────────────────────
    # 테이블 4: products (화장품)
    # 추천 가능한 화장품 정보를 저장합니다.
    # ─────────────────────────────────────────────────────────────────
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS products (
            product_id      INTEGER PRIMARY KEY AUTOINCREMENT,
                            -- 화장품 고유 번호 (자동 증가)

            product_url     TEXT    NOT NULL,
                            -- 화장품 상품 링크 (필수)

            product_name    TEXT,
                            -- 화장품 이름 예: '데일리 수분 톤업 선크림'

            keyword         TEXT,
                            -- 화장품 키워드 예: '수분, 자외선차단'

            category        TEXT
                            -- 화장품 카테고리 예: '선크림', '파운데이션', '립'
        )
    """)

    # ─────────────────────────────────────────────────────────────────
    # 테이블 5: rec_products (추천 화장품)
    # 진단결과에 따라 어떤 화장품을 추천했는지 기록합니다.
    # diagnosis, products 두 테이블을 동시에 참조합니다.
    # ─────────────────────────────────────────────────────────────────
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS rec_products (
            rec_id          INTEGER PRIMARY KEY AUTOINCREMENT,
                            -- 추천 기록 고유 번호 (자동 증가)

            product_id      INTEGER NOT NULL,
                            -- 추천된 화장품 번호 (products 테이블의 product_id 참조)

            diagnosis_id    INTEGER NOT NULL,
                            -- 어떤 진단에서 추천됐는지 (diagnosis 테이블의 diagnosis_id 참조)

            rec_reason      TEXT,
                            -- 추천 이유 예: '웜톤에 어울리는 코랄 계열'

            FOREIGN KEY (product_id)   REFERENCES products(product_id),
            FOREIGN KEY (diagnosis_id) REFERENCES diagnosis(diagnosis_id)
        )
    """)

    conn.commit()   # 변경사항을 DB에 저장
    conn.close()    # 연결 종료
    print("테이블 생성 완료: personal_color_types, users, diagnosis, products, rec_products")


# 이 파일을 직접 실행할 때만 아래 코드가 동작합니다.
if __name__ == "__main__":
    create_tables()
    print(f"DB 경로: {DB_PATH}")
