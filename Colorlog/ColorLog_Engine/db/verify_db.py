"""DB 검증 스크립트: 모든 테이블의 레코드를 출력합니다.
사용법: python -m db.verify_db  (ColorLog_Engine 폴더에서 실행)
"""
import sys
sys.stdout.reconfigure(encoding="utf-8")
from .schema import get_connection, DB_PATH, create_tables

create_tables()

print(f"=== DB 경로: {DB_PATH} ===\n")

TABLES = ["personal_color_types", "users", "diagnosis", "products", "rec_products"]

conn = get_connection()
cursor = conn.cursor()

for table in TABLES:
    cursor.execute(f"SELECT COUNT(*) FROM {table}")
    count = cursor.fetchone()[0]
    print(f"┌── [{table}] 총 {count}개")

    cursor.execute(f"SELECT * FROM {table} LIMIT 10")
    rows = cursor.fetchall()
    for row in rows:
        print(f"│   {dict(row)}")
    if count > 10:
        print(f"│   ... 외 {count - 10}개")
    print("│")

conn.close()
print("검증 완료.")
