# seed_ai_recs.py 수정 — 기존 데이터 삭제 후 재생성
from db.repository import get_monthly_stats, save_ai_recommendations
from db.recommendation import get_ai_recommendation
from db.schema import get_connection

users = [
    (2, '연성은'),
    (3, '김민지'),
    (4, '이수지'),
    (5, '김철수'),
]

conn = get_connection()

for user_id, name in users:
    row = conn.execute(
        "SELECT diagnosis_id FROM diagnosis WHERE user_id=? ORDER BY diagnosis_at DESC LIMIT 1",
        (user_id,)
    ).fetchone()

    if not row:
        print(f"{name}: 진단 없음, 스킵")
        continue

    diagnosis_id = row[0]

    conn.execute("DELETE FROM rec_products WHERE diagnosis_id = ?", (diagnosis_id,))
    conn.commit()

    stats = get_monthly_stats(user_id)
    color_type = stats["most_color_type"]
    brightness = stats["avg_brightness"]
    redness = stats["avg_redness"]

    if not color_type:
        print(f"{name}: 퍼스널컬러 데이터 없음, 스킵")
        continue

    print(f"{name}: {color_type} 기반 추천 생성 중...")
    recs = get_ai_recommendation(color_type, "", brightness=brightness, redness=redness)

    if recs:
        save_ai_recommendations(diagnosis_id, recs)
        print(f"{name}: {len(recs)}개 추천 저장 완료")
    else:
        print(f"{name}: AI 추천 실패")

conn.close()
print("전체 완료")
