"""
DB 저장/조회 함수 모음 (Repository)
- schema.py 에서 DB 연결을 가져와 사용합니다.
- 사용자(users)와 진단결과(diagnosis) 관련 기본 기능을 제공합니다.
"""

from datetime import datetime
from .schema import get_connection


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
    row = cursor.fetchone()  # 결과 1행 가져오기
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
    rows = cursor.fetchall()  # 결과 전체 가져오기
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
