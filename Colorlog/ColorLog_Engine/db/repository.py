from datetime import datetime
from .schema import get_connection


# ── personal_color_types ─────────────────────────────────────────────────────

def get_color_type(type_id: int) -> dict | None:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM personal_color_types WHERE type_id = ?", (type_id,))
    row = cursor.fetchone()
    conn.close()
    return dict(row) if row else None


def get_color_type_by_name(type_name: str) -> dict | None:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM personal_color_types WHERE type_name = ?", (type_name,))
    row = cursor.fetchone()
    conn.close()
    return dict(row) if row else None


def get_all_color_types() -> list[dict]:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM personal_color_types ORDER BY type_id")
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


# ── users ─────────────────────────────────────────────────────────────────────

def update_user(user_id: int, user_name: str = None, age: str = None, created_at: str = None) -> None:
    """기존 유저의 이름·나이·가입일을 수정한다."""
    conn = get_connection()
    cursor = conn.cursor()
    if user_name is not None:
        cursor.execute("UPDATE users SET user_name = ? WHERE user_id = ?", (user_name, user_id))
    if age is not None:
        cursor.execute("UPDATE users SET age = ? WHERE user_id = ?", (age, user_id))
    if created_at is not None:
        cursor.execute("UPDATE users SET created_at = ? WHERE user_id = ?", (created_at, user_id))
    conn.commit()
    conn.close()


def get_or_create_user(user_name: str, gender: str = None, age: str = None) -> int:
    """동일 이름 유저가 있으면 반환하고 나이를 갱신, 없으면 새로 생성한다."""
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT user_id FROM users WHERE user_name = ?", (user_name,))
    row = cursor.fetchone()
    if row:
        user_id = row["user_id"]
        conn.close()
        return user_id

    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    cursor.execute(
        "INSERT INTO users (user_name, gender, age, created_at) VALUES (?, ?, ?, ?)",
        (user_name, gender, age, now),
    )
    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_user(user_id: int) -> dict | None:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM users WHERE user_id = ?", (user_id,))
    row = cursor.fetchone()
    conn.close()
    return dict(row) if row else None


def get_all_users() -> list[dict]:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM users ORDER BY created_at DESC")
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


def get_user_stats(user_id: int) -> dict:
    """Settings 화면용: 진단 횟수, 가입일, 최근 퍼스널컬러를 반환한다."""
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute(
        "SELECT COUNT(*) AS cnt FROM diagnosis WHERE user_id = ?", (user_id,)
    )
    count = cursor.fetchone()["cnt"]

    cursor.execute(
        "SELECT created_at FROM users WHERE user_id = ?", (user_id,)
    )
    user_row = cursor.fetchone()
    join_date = ""
    if user_row:
        dt = datetime.strptime(user_row["created_at"], "%Y-%m-%d %H:%M:%S")
        join_date = dt.strftime("%Y.%m.%d")

    cursor.execute("""
        SELECT pct.type_name, d.diagnosis_at, d.brightness, d.redness
        FROM diagnosis d
        LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
        WHERE d.user_id = ?
        ORDER BY d.diagnosis_at DESC
        LIMIT 1
    """, (user_id,))
    latest_row = cursor.fetchone()
    latest_color = latest_row["type_name"] if latest_row and latest_row["type_name"] else ""
    latest_at = latest_row["diagnosis_at"] if latest_row else ""
    latest_brightness = latest_row["brightness"] if latest_row and latest_row["brightness"] is not None else -1
    latest_redness = latest_row["redness"] if latest_row and latest_row["redness"] is not None else -1

    conn.close()
    return {
        "diagnosis_count": count,
        "join_date": join_date,
        "latest_color_type": latest_color,
        "latest_at": latest_at,
        "latest_brightness": latest_brightness,
        "latest_redness": latest_redness,
    }


# ── diagnosis ─────────────────────────────────────────────────────────────────

def add_diagnosis(
    user_id: int,
    lab_l: float,
    lab_a: float,
    lab_b: float,
    brightness: int,
    redness: int,
    type_id: int = None,
    note: str = None,
) -> int:
    conn = get_connection()
    cursor = conn.cursor()
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    cursor.execute("""
        INSERT INTO diagnosis
            (diagnosis_at, lab_l, lab_a, lab_b, brightness, redness, note, type_id, user_id)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (now, lab_l, lab_a, lab_b, brightness, redness, note, type_id, user_id))
    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_diagnosis(diagnosis_id: int) -> dict | None:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM diagnosis WHERE diagnosis_id = ?", (diagnosis_id,))
    row = cursor.fetchone()
    conn.close()
    return dict(row) if row else None


def get_diagnoses_by_user(user_id: int) -> list[dict]:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute(
        "SELECT * FROM diagnosis WHERE user_id = ? ORDER BY diagnosis_at DESC",
        (user_id,),
    )
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


def get_diagnoses_by_month(user_id: int, year: int, month: int) -> list[dict]:
    """달력 뷰용: 특정 월의 날짜별 최신 진단 1건씩 반환한다."""
    conn = get_connection()
    cursor = conn.cursor()
    month_str = f"{year}-{month:02d}"
    cursor.execute("""
        SELECT
            d.diagnosis_id,
            DATE(d.diagnosis_at) AS date,
            d.brightness,
            d.redness,
            d.note,
            pct.type_name AS personal_color
        FROM diagnosis d
        LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
        WHERE d.user_id = ?
          AND strftime('%Y-%m', d.diagnosis_at) = ?
        GROUP BY DATE(d.diagnosis_at)
        HAVING d.diagnosis_id = MAX(d.diagnosis_id)
        ORDER BY date ASC
    """, (user_id, month_str))
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


def get_diagnoses_last_7days(user_id: int) -> list[dict]:
    """주간 트렌드용: 최근 7일 날짜별 최신 진단 1건씩 반환한다."""
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT
            DATE(d.diagnosis_at) AS date,
            d.brightness,
            d.redness,
            pct.type_name AS personal_color
        FROM diagnosis d
        LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
        WHERE d.user_id = ?
          AND d.diagnosis_at >= DATE('now', 'localtime', '-6 days')
        GROUP BY DATE(d.diagnosis_at)
        HAVING d.diagnosis_id = MAX(d.diagnosis_id)
        ORDER BY date ASC
    """, (user_id,))
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


# ── diagnosis delete ──────────────────────────────────────────────────────────

def delete_diagnosis(diagnosis_id: int) -> None:
    """진단 1건과 연결된 추천 제품 기록을 삭제한다."""
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("DELETE FROM rec_products WHERE diagnosis_id = ?", (diagnosis_id,))
    cursor.execute("DELETE FROM diagnosis WHERE diagnosis_id = ?", (diagnosis_id,))
    conn.commit()
    conn.close()


def delete_diagnoses_by_user(user_id: int) -> None:
    """특정 유저의 진단 기록 전체와 연결된 추천 제품 기록을 삭제한다."""
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        DELETE FROM rec_products
        WHERE diagnosis_id IN (
            SELECT diagnosis_id FROM diagnosis WHERE user_id = ?
        )
    """, (user_id,))
    cursor.execute("DELETE FROM diagnosis WHERE user_id = ?", (user_id,))
    conn.commit()
    conn.close()


# ── users delete ──────────────────────────────────────────────────────────────

def delete_user(user_id: int) -> None:
    """유저와 연결된 진단·추천 기록을 모두 삭제한 뒤 유저를 삭제한다."""
    delete_diagnoses_by_user(user_id)
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("DELETE FROM users WHERE user_id = ?", (user_id,))
    conn.commit()
    conn.close()


# ── products / rec_products ───────────────────────────────────────────────────

def get_recommended_products(tone_type: str) -> list[dict]:
    """퍼스널컬러 톤(웜/쿨)에 맞는 추천 제품 목록을 반환한다."""
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute(
        "SELECT * FROM products WHERE tone_type = ? ORDER BY product_id",
        (tone_type,),
    )
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


def add_rec_product(product_id: int, diagnosis_id: int, rec_reason: str = None) -> int:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute(
        "INSERT INTO rec_products (product_id, diagnosis_id, rec_reason) VALUES (?, ?, ?)",
        (product_id, diagnosis_id, rec_reason),
    )
    conn.commit()
    new_id = cursor.lastrowid
    conn.close()
    return new_id


def get_rec_products_by_diagnosis(diagnosis_id: int) -> list[dict]:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT r.rec_id, r.rec_reason,
               p.product_id, p.product_name, p.product_url, p.keyword, p.category
        FROM rec_products r
        JOIN products p ON r.product_id = p.product_id
        WHERE r.diagnosis_id = ?
        ORDER BY r.rec_id
    """, (diagnosis_id,))
    rows = cursor.fetchall()
    conn.close()
    return [dict(row) for row in rows]


def save_ai_recommendations(diagnosis_id: int, recommendations: list[dict]) -> list[dict]:
    """AI 추천 결과를 products + rec_products에 저장. 저장된 레코드 리스트 반환."""
    conn = get_connection()
    saved = []
    try:
        for item in recommendations:
            cur = conn.execute(
                """INSERT INTO products (product_url, product_name, keyword, category, tone_type)
                   VALUES (?, ?, ?, ?, ?)""",
                (
                    item.get("product_url", ""),
                    item.get("product_name", ""),
                    item.get("keyword", ""),
                    item.get("category", ""),
                    item.get("tone_type", ""),
                ),
            )
            product_id = cur.lastrowid
            conn.execute(
                """INSERT INTO rec_products (product_id, diagnosis_id, rec_reason)
                   VALUES (?, ?, ?)""",
                (product_id, diagnosis_id, item.get("reason", "")),
            )
            saved.append({
                "product_id": product_id,
                "product_name": item.get("product_name", ""),
                "product_url": item.get("product_url", ""),
                "category": item.get("category", ""),
                "reason": item.get("reason", ""),
            })
        conn.commit()
    finally:
        conn.close()
    return saved
