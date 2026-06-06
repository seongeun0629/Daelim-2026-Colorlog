from db.schema import get_connection
from datetime import datetime, timedelta
import random

conn = get_connection()

# 가가가 (user_id=3) - 여름 뮤트 쿨톤, 유분 보통
# 나나나 (user_id=4) - 봄 라이트 웜톤, 건조
# 다다다 (user_id=5) - 가을 딥 웜톤, 유분 많음

user_configs = [
    {
        "user_id": 3,
        "type_id": 8,  # 여름 뮤트 쿨톤
        "brightness_range": (65, 80),
        "redness_range": (45, 60),
        "oily_status": "Normal",
        "oily_score_range": (15, 35),
    },
    {
        "user_id": 4,
        "type_id": 5,  # 봄 라이트 웜톤
        "brightness_range": (75, 90),
        "redness_range": (30, 50),
        "oily_status": "Not Oily",
        "oily_score_range": (5, 20),
    },
    {
        "user_id": 5,
        "type_id": 9,  # 가을 딥 웜톤
        "brightness_range": (50, 70),
        "redness_range": (55, 75),
        "oily_status": "Oily",
        "oily_score_range": (40, 70),
    },
]

today = datetime.now()

for config in user_configs:
    for i in range(30):
        # 30일치, 가끔 빠지게 (약 80% 확률)
        if random.random() < 0.2:
            continue

        date = today - timedelta(days=29 - i)
        diagnosis_at = date.strftime("%Y-%m-%d") + f" {random.randint(8,22):02d}:{random.randint(0,59):02d}:00"

        brightness = random.randint(*config["brightness_range"])
        redness    = random.randint(*config["redness_range"])
        oily_score = random.uniform(*config["oily_score_range"])

        conn.execute("""
            INSERT INTO diagnosis
                (diagnosis_at, lab_l, lab_a, lab_b, brightness, redness,
                 oily_status, oily_score, note, type_id, user_id)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            diagnosis_at,
            round(brightness * 2.55, 2),
            round(random.uniform(5, 15), 2),
            round(random.uniform(3, 10), 2),
            brightness,
            redness,
            config["oily_status"],
            round(oily_score, 2),
            None,
            config["type_id"],
            config["user_id"],
        ))

conn.commit()
conn.close()
print("가짜 데이터 입력 완료")
