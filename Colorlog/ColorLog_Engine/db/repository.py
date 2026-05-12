"""
DB 저장/조회 함수 모음 (Repository)
- schema.py 에서 DB 연결을 가져와 사용합니다.
- 5개 테이블 전체의 기본 기능을 제공합니다.

  [테이블별 함수 목록]
  personal_color_types : add_color_type / get_color_type / get_all_color_types
  users                : add_user / get_user / get_all_users
  diagnosis            : add_diagnosis / get_diagnosis / get_diagnoses_by_user
  products             : add_product / get_product / get_all_products
  rec_products         : add_rec_product / get_rec_products_by_diagnosis
"""

from datetime import datetime
from .schema import get_connection


# ══════════════════════════════════════════════════════════════════════
# 퍼스널컬러 유형(personal_color_types) 관련 함수
# ══════════════════════════════════════════════════════════════════════

def add_color_type(
    type_name: str,
    colors: str = None,
    worst_colors: str = None,
    tone: str = None,
    keyword: str = None,
) -> int:
    """
    퍼스널컬러 유형 1개를 DB에 저장합니다.

    사용 예:
        type_id = add_color_type(
            type_name="봄 웜톤",
            colors="코랄, 피치, 아이보리",
            worst_colors="그레이, 블루블랙",
            tone="웜",
            keyword="생기있는, 따뜻한",
        )

    반환값: 새로 생성된 type_id (정수)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        INSERT INTO personal_color_types (type_name, colors, worst_colors, tone, keyword)
        VALUES (?, ?, ?, ?, ?)
    """, (type_name, colors, worst_colors, tone, keyword))

    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_color_type(type_id: int) -> dict | None:
    """
    type_id로 퍼스널컬러 유형 1개를 조회합니다.

    사용 예:
        ctype = get_color_type(1)
        print(ctype["type_name"])  # 봄 웜톤

    반환값: 유형 정보 딕셔너리 또는 None(없으면)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM personal_color_types WHERE type_id = ?", (type_id,))
    row = cursor.fetchone()
    conn.close()

    return dict(row) if row else None


def get_all_color_types() -> list[dict]:
    """
    모든 퍼스널컬러 유형을 조회합니다.

    사용 예:
        types = get_all_color_types()
        for t in types:
            print(t["type_name"], t["tone"])

    반환값: 유형 딕셔너리 리스트
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM personal_color_types ORDER BY type_id")
    rows = cursor.fetchall()
    conn.close()

    return [dict(row) for row in rows]


# ══════════════════════════════════════════════════════════════════════
# 사용자(users) 관련 함수
# ══════════════════════════════════════════════════════════════════════

def add_user(user_name: str, gender: str = None, age: str = None) -> int:
    """
    새 사용자를 DB에 저장합니다.

    사용 예:
        user_id = add_user("홍길동", gender="남", age="20대")

    반환값: 새로 생성된 user_id (정수)
    """
    conn = get_connection()
    cursor = conn.cursor()

    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")  # 현재 시각

    cursor.execute("""
        INSERT INTO users (user_name, gender, age, created_at)
        VALUES (?, ?, ?, ?)
    """, (user_name, gender, age, now))
    # ? 는 값을 안전하게 넣어주는 자리표시자입니다 (SQL 인젝션 방지)

    conn.commit()
    new_id = cursor.lastrowid  # 방금 저장된 행의 user_id
    conn.close()
    return new_id


def get_user(user_id: int) -> dict | None:
    """
    user_id로 사용자 한 명을 조회합니다.

    사용 예:
        user = get_user(1)
        print(user["user_name"])  # 홍길동

    반환값: 사용자 정보 딕셔너리 또는 None(없으면)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM users WHERE user_id = ?", (user_id,))
    row = cursor.fetchone()
    conn.close()

    return dict(row) if row else None


def get_all_users() -> list[dict]:
    """
    모든 사용자 목록을 조회합니다.

    사용 예:
        users = get_all_users()
        for user in users:
            print(user["user_name"])

    반환값: 사용자 딕셔너리 리스트
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM users ORDER BY created_at DESC")
    rows = cursor.fetchall()
    conn.close()

    return [dict(row) for row in rows]


# ══════════════════════════════════════════════════════════════════════
# 진단결과(diagnosis) 관련 함수
# ══════════════════════════════════════════════════════════════════════

def add_diagnosis(
    user_id: int,
    rpv_a: float,
    rpv_b: float,
    lab_a: float = None,
    lab_b: float = None,
    lab_c: float = None,
    landmark: float = None,
    type_id: int = None,
) -> int:
    """
    진단 결과 1건을 DB에 저장합니다.

    사용 예:
        diagnosis_id = add_diagnosis(
            user_id=1,
            rpv_a=0.72,
            rpv_b=0.58,
            lab_a=65.3,
            lab_b=12.1,
            lab_c=-5.4,
            type_id=1,
        )

    반환값: 새로 생성된 diagnosis_id (정수)
    """
    conn = get_connection()
    cursor = conn.cursor()

    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    cursor.execute("""
        INSERT INTO diagnosis
            (diagnosis_at, rpv_a, rpv_b, lab_a, lab_b, lab_c, landmark, type_id, user_id)
        VALUES
            (?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (now, rpv_a, rpv_b, lab_a, lab_b, lab_c, landmark, type_id, user_id))

    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_diagnosis(diagnosis_id: int) -> dict | None:
    """
    diagnosis_id로 진단결과 한 건을 조회합니다.

    사용 예:
        result = get_diagnosis(1)
        print(result["rpv_a"])

    반환값: 진단결과 딕셔너리 또는 None(없으면)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM diagnosis WHERE diagnosis_id = ?", (diagnosis_id,))
    row = cursor.fetchone()
    conn.close()

    return dict(row) if row else None


def get_diagnoses_by_user(user_id: int) -> list[dict]:
    """
    특정 사용자의 모든 진단결과를 최신순으로 조회합니다.

    사용 예:
        results = get_diagnoses_by_user(1)
        for r in results:
            print(r["diagnosis_at"], r["rpv_a"])

    반환값: 진단결과 딕셔너리 리스트
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        SELECT * FROM diagnosis
        WHERE user_id = ?
        ORDER BY diagnosis_at DESC
    """, (user_id,))
    rows = cursor.fetchall()
    conn.close()

    return [dict(row) for row in rows]


# ══════════════════════════════════════════════════════════════════════
# 화장품(products) 관련 함수
# ══════════════════════════════════════════════════════════════════════

def add_product(
    product_url: str,
    product_name: str = None,
    keyword: str = None,
    category: str = None,
) -> int:
    """
    화장품 1개를 DB에 저장합니다.

    사용 예:
        product_id = add_product(
            product_url="https://shop.example.com/item/123",
            product_name="데일리 수분 톤업 선크림",
            keyword="수분, 자외선차단",
            category="선크림",
        )

    반환값: 새로 생성된 product_id (정수)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        INSERT INTO products (product_url, product_name, keyword, category)
        VALUES (?, ?, ?, ?)
    """, (product_url, product_name, keyword, category))

    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_product(product_id: int) -> dict | None:
    """
    product_id로 화장품 1개를 조회합니다.

    사용 예:
        product = get_product(1)
        print(product["product_name"])

    반환값: 화장품 정보 딕셔너리 또는 None(없으면)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM products WHERE product_id = ?", (product_id,))
    row = cursor.fetchone()
    conn.close()

    return dict(row) if row else None


def get_all_products() -> list[dict]:
    """
    모든 화장품 목록을 조회합니다.

    사용 예:
        products = get_all_products()
        for p in products:
            print(p["product_name"], p["category"])

    반환값: 화장품 딕셔너리 리스트
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM products ORDER BY product_id")
    rows = cursor.fetchall()
    conn.close()

    return [dict(row) for row in rows]


# ══════════════════════════════════════════════════════════════════════
# 추천 화장품(rec_products) 관련 함수
# ══════════════════════════════════════════════════════════════════════

def add_rec_product(
    product_id: int,
    diagnosis_id: int,
    rec_reason: str = None,
) -> int:
    """
    진단결과에 대한 화장품 추천 기록 1건을 저장합니다.

    사용 예:
        rec_id = add_rec_product(
            product_id=1,
            diagnosis_id=1,
            rec_reason="봄 웜톤에 어울리는 코랄 계열 블러셔",
        )

    반환값: 새로 생성된 rec_id (정수)
    """
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        INSERT INTO rec_products (product_id, diagnosis_id, rec_reason)
        VALUES (?, ?, ?)
    """, (product_id, diagnosis_id, rec_reason))

    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_rec_products_by_diagnosis(diagnosis_id: int) -> list[dict]:
    """
    특정 진단결과에 추천된 화장품 목록을 조회합니다.
    화장품 상세 정보(이름, 카테고리 등)도 함께 반환합니다.

    사용 예:
        recs = get_rec_products_by_diagnosis(1)
        for r in recs:
            print(r["product_name"], r["rec_reason"])

    반환값: 추천 기록 + 화장품 정보가 합쳐진 딕셔너리 리스트
    """
    conn = get_connection()
    cursor = conn.cursor()

    # JOIN: rec_products와 products를 합쳐서 한 번에 조회합니다
    cursor.execute("""
        SELECT
            r.rec_id,
            r.rec_reason,
            p.product_id,
            p.product_name,
            p.product_url,
            p.keyword,
            p.category
        FROM rec_products r
        JOIN products p ON r.product_id = p.product_id
        WHERE r.diagnosis_id = ?
        ORDER BY r.rec_id
    """, (diagnosis_id,))
    rows = cursor.fetchall()
    conn.close()

    return [dict(row) for row in rows]
