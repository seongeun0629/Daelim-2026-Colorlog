# seed_zone_data.py
from db.schema import get_connection

conn = get_connection()

# 각 유저의 가장 최근 진단에 zone 데이터 추가
users = [2, 3, 4, 5]

for user_id in users:
    row = conn.execute(
        "SELECT diagnosis_id FROM diagnosis WHERE user_id=? ORDER BY diagnosis_at DESC LIMIT 1",
        (user_id,)
    ).fetchone()

    if not row:
        print(f"user_id={user_id}: 진단 없음, 스킵")
        continue

    diagnosis_id = row[0]
    conn.execute("""
        UPDATE diagnosis SET
            zone_forehead_r=210, zone_forehead_g=175, zone_forehead_b=155,
            zone_lcheek_r=205,   zone_lcheek_g=168,   zone_lcheek_b=148,
            zone_rcheek_r=208,   zone_rcheek_g=170,   zone_rcheek_b=150,
            zone_nose_r=215,     zone_nose_g=178,     zone_nose_b=158,
            zone_chin_r=203,     zone_chin_g=165,     zone_chin_b=145
        WHERE diagnosis_id=?
    """, (diagnosis_id,))
    print(f"user_id={user_id}: zone 데이터 추가 완료")

conn.commit()
conn.close()
print("전체 완료")
