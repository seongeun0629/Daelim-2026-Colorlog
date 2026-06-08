import sqlite3
import os

DB_PATH = os.path.join(os.path.dirname(__file__), "colorlog.db")


def get_connection():
    conn = sqlite3.connect(DB_PATH)
    conn.execute("PRAGMA foreign_keys = ON")
    conn.row_factory = sqlite3.Row
    return conn


def create_tables():
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        CREATE TABLE IF NOT EXISTS personal_color_types (
            type_id      INTEGER PRIMARY KEY AUTOINCREMENT,
            type_name    TEXT    NOT NULL,
            colors       TEXT,
            worst_colors TEXT,
            tone         TEXT,
            keyword      TEXT
        )
    """)

    cursor.execute("""
        CREATE TABLE IF NOT EXISTS users (
            user_id     INTEGER PRIMARY KEY AUTOINCREMENT,
            user_name   TEXT    NOT NULL UNIQUE,
            gender      TEXT,
            age         TEXT,
            created_at  TEXT    NOT NULL
        )
    """)

    cursor.execute("""
        CREATE TABLE IF NOT EXISTS diagnosis (
            diagnosis_id    INTEGER PRIMARY KEY AUTOINCREMENT,
            diagnosis_at    TEXT    NOT NULL,
            lab_l           REAL,
            lab_a           REAL,
            lab_b           REAL,
            brightness      INTEGER,
            redness         INTEGER,
            note            TEXT,
            type_id         INTEGER,
            user_id         INTEGER NOT NULL,
            FOREIGN KEY (user_id) REFERENCES users(user_id),
            FOREIGN KEY (type_id) REFERENCES personal_color_types(type_id)
        )
    """)

    cursor.execute("""
        CREATE TABLE IF NOT EXISTS products (
            product_id      INTEGER PRIMARY KEY AUTOINCREMENT,
            product_url     TEXT    NOT NULL,
            product_name    TEXT,
            keyword         TEXT,
            category        TEXT,
            tone_type       TEXT
        )
    """)

    cursor.execute("""
        CREATE TABLE IF NOT EXISTS rec_products (
            rec_id          INTEGER PRIMARY KEY AUTOINCREMENT,
            product_id      INTEGER NOT NULL,
            diagnosis_id    INTEGER NOT NULL,
            rec_reason      TEXT,
            FOREIGN KEY (product_id)   REFERENCES products(product_id),
            FOREIGN KEY (diagnosis_id) REFERENCES diagnosis(diagnosis_id)
        )
    """)

    cursor.execute("""
    CREATE TABLE IF NOT EXISTS diagnosis (
        diagnosis_id    INTEGER PRIMARY KEY AUTOINCREMENT,
        diagnosis_at    TEXT    NOT NULL,
        lab_l           REAL,
        lab_a           REAL,
        lab_b           REAL,
        brightness      INTEGER,
        redness         INTEGER,
        oily_status     TEXT,
        oily_score      REAL,
        note            TEXT,
        type_id         INTEGER,
        user_id         INTEGER NOT NULL,
        FOREIGN KEY (user_id) REFERENCES users(user_id),
        FOREIGN KEY (type_id) REFERENCES personal_color_types(type_id)
    )
""")

    conn.commit()
    conn.close()
    _migrate()


def _migrate():
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("PRAGMA table_info(diagnosis)")
    diag_col_info = {row["name"]: row for row in cursor.fetchall()}
    diag_cols = set(diag_col_info.keys())

    # 구 스키마(rpv_a NOT NULL)가 있으면 테이블을 깨끗하게 재생성한다
    if "rpv_a" in diag_col_info and diag_col_info["rpv_a"]["notnull"] == 1:
        cursor.execute("""
            CREATE TABLE diagnosis_new (
                diagnosis_id    INTEGER PRIMARY KEY AUTOINCREMENT,
                diagnosis_at    TEXT    NOT NULL,
                lab_l           REAL,
                lab_a           REAL,
                lab_b           REAL,
                brightness      INTEGER,
                redness         INTEGER,
                note            TEXT,
                type_id         INTEGER,
                user_id         INTEGER NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users(user_id),
                FOREIGN KEY (type_id) REFERENCES personal_color_types(type_id)
            )
        """)
        cursor.execute("""
            INSERT INTO diagnosis_new
                (diagnosis_id, diagnosis_at, lab_l, lab_a, lab_b,
                 brightness, redness, note, type_id, user_id)
            SELECT
                diagnosis_id, diagnosis_at,
                COALESCE(lab_l, lab_a),
                rpv_a,
                rpv_b,
                brightness,
                redness,
                note, type_id, user_id
            FROM diagnosis
        """)
        cursor.execute("DROP TABLE diagnosis")
        cursor.execute("ALTER TABLE diagnosis_new RENAME TO diagnosis")
        conn.commit()
    else:
        for col, typedef in [
            ("brightness", "INTEGER"),
            ("redness",    "INTEGER"),
            ("note",       "TEXT"),
            ("lab_l",      "REAL"),
            ("lab_a",      "REAL"),
            ("lab_b",      "REAL"),
            ("oily_status",  "TEXT"),    
            ("oily_score",   "REAL"),
            ("zone_forehead_r", "INTEGER"),  
            ("zone_forehead_g", "INTEGER"),  
            ("zone_forehead_b", "INTEGER"),  
            ("zone_lcheek_r",   "INTEGER"), 
            ("zone_lcheek_g",   "INTEGER"),  
            ("zone_lcheek_b",   "INTEGER"),  
            ("zone_rcheek_r",   "INTEGER"), 
            ("zone_rcheek_g",   "INTEGER"),
            ("zone_rcheek_b",   "INTEGER"), 
            ("zone_nose_r",     "INTEGER"), 
            ("zone_nose_g",     "INTEGER"),
            ("zone_nose_b",     "INTEGER"),  
            ("zone_chin_r",     "INTEGER"),  
            ("zone_chin_g",     "INTEGER"),  
            ("zone_chin_b",     "INTEGER"),  
        ]:
            if col not in diag_cols:
                cursor.execute(f"ALTER TABLE diagnosis ADD COLUMN {col} {typedef}")
        conn.commit()

    cursor.execute("PRAGMA table_info(products)")
    prod_cols = {row["name"] for row in cursor.fetchall()}
    if "tone_type" not in prod_cols:
        cursor.execute("ALTER TABLE products ADD COLUMN tone_type TEXT")
        conn.commit()

    conn.close()


_PERSONAL_COLOR_TYPES = [
    {
        "type_name": "봄 라이트 웜톤 (Spring Light Warm)",
        "colors": "피치, 살구색, 아이보리, 연한 코랄",
        "worst_colors": "그레이, 블루블랙, 형광색",
        "tone": "웜",
        "keyword": "밝은, 따뜻한, 화사한",
    },
    {
        "type_name": "봄 비비드 웜톤 (Spring Vivid Warm)",
        "colors": "코랄, 오렌지, 선명한 노랑, 밝은 그린",
        "worst_colors": "다크 네이비, 블랙, 그레이",
        "tone": "웜",
        "keyword": "생기있는, 선명한, 활기찬",
    },
    {
        "type_name": "여름 라이트 쿨톤 (Summer Light Cool)",
        "colors": "라벤더, 파우더핑크, 블루, 민트",
        "worst_colors": "오렌지, 카키, 골드",
        "tone": "쿨",
        "keyword": "부드러운, 청량한, 투명한",
    },
    {
        "type_name": "여름 뮤트 쿨톤 (Summer Mute Cool)",
        "colors": "로즈, 모브, 회색빛 블루, 연보라",
        "worst_colors": "오렌지, 카키, 선명한 색상",
        "tone": "쿨",
        "keyword": "차분한, 우아한, 뮤트한",
    },
    {
        "type_name": "가을 딥 웜톤 (Autumn Deep Warm)",
        "colors": "브라운, 버건디, 카키, 올리브",
        "worst_colors": "형광핑크, 네온, 블루블랙",
        "tone": "웜",
        "keyword": "깊이있는, 성숙한, 클래식한",
    },
    {
        "type_name": "가을 뮤트 웜톤 (Autumn Mute Warm)",
        "colors": "베이지, 머스타드, 테라코타, 카멜",
        "worst_colors": "형광색, 쨍한 핑크, 블루블랙",
        "tone": "웜",
        "keyword": "차분한, 내추럴한, 따뜻한",
    },
    {
        "type_name": "겨울 비비드 쿨톤 (Winter Vivid Cool)",
        "colors": "블랙, 화이트, 선명한 레드, 로얄블루",
        "worst_colors": "오렌지, 피치, 카멜",
        "tone": "쿨",
        "keyword": "강렬한, 또렷한, 도시적인",
    },
    {
        "type_name": "겨울 딥 쿨톤 (Winter Deep Cool)",
        "colors": "네이비, 다크 버건디, 차콜, 다크 플럼",
        "worst_colors": "오렌지, 피치, 밝은 웜톤",
        "tone": "쿨",
        "keyword": "깊은, 미스터리한, 세련된",
    },
    {
        "type_name": "뉴트럴톤 (Neutral)",
        "colors": "누드, 그레이지, 내추럴 베이지, 소프트 화이트",
        "worst_colors": "극단적인 웜톤, 극단적인 쿨톤",
        "tone": "뉴트럴",
        "keyword": "균형잡힌, 자연스러운, 범용적인",
    },
]


def seed_personal_color_types():
    conn = get_connection()
    cursor = conn.cursor()
    for t in _PERSONAL_COLOR_TYPES:
        cursor.execute(
            "SELECT COUNT(*) FROM personal_color_types WHERE type_name = ?",
            (t["type_name"],)
        )
        if cursor.fetchone()[0] == 0:
            cursor.execute(
                """INSERT INTO personal_color_types
                   (type_name, colors, worst_colors, tone, keyword)
                   VALUES (?, ?, ?, ?, ?)""",
                (t["type_name"], t["colors"], t["worst_colors"], t["tone"], t["keyword"])
            )
    conn.commit()
    conn.close()


_PRODUCTS_SEED = [
    # 봄 웜톤
    ("https://example.com/p1", "코랄 무드 블러셔",     "코랄,피치,웜톤",   "치크",   "웜"),
    ("https://example.com/p2", "살구빛 크림 립틴트",   "살구,피치,웜톤",   "립",     "웜"),
    ("https://example.com/p3", "피치 아이 팔레트",     "피치,웜,골드",     "아이",   "웜"),
    ("https://example.com/p4", "시카 진정 토너 패드",  "진정,저자극",      "스킨케어","웜"),
    ("https://example.com/p5", "광채 세럼 쿠션 21N",  "밝기,웜베이지",    "베이스",  "웜"),
    # 여름 쿨톤
    ("https://example.com/p6", "라벤더 쉬머 블러셔",  "라벤더,핑크,쿨",   "치크",   "쿨"),
    ("https://example.com/p7", "로즈 틴트",           "로즈,쿨핑크",      "립",     "쿨"),
    ("https://example.com/p8", "블루 베이스 파운데이션","쿨베이지,투명",   "베이스",  "쿨"),
    ("https://example.com/p9", "히알루론산 수분 크림", "수분,진정",        "스킨케어","쿨"),
    ("https://example.com/p10","나이아신아마이드 세럼", "미백,쿨톤",       "스킨케어","쿨"),
    # 가을 웜톤
    ("https://example.com/p11","브라운 무드 블러셔",   "브라운,테라코타",  "치크",   "웜"),
    ("https://example.com/p12","뮤트 버건디 립",       "버건디,웜브라운",  "립",     "웜"),
    ("https://example.com/p13","머스타드 아이 팔레트", "머스타드,카키",    "아이",   "웜"),
    # 겨울 쿨톤
    ("https://example.com/p14","쿨 로즈 블러셔",       "로즈,쿨핑크",     "치크",   "쿨"),
    ("https://example.com/p15","선명한 레드 립",        "레드,선명,쿨",    "립",     "쿨"),
    ("https://example.com/p16","블루 언더톤 파운데이션","쿨,밝은베이지",  "베이스",  "쿨"),
]


def seed_products():
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) FROM products")
    if cursor.fetchone()[0] == 0:
        cursor.executemany(
            "INSERT INTO products (product_url, product_name, keyword, category, tone_type) VALUES (?,?,?,?,?)",
            _PRODUCTS_SEED
        )
        conn.commit()
    conn.close()


if __name__ == "__main__":
    create_tables()
    print(f"DB 경로: {DB_PATH}")
