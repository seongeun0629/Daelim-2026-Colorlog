import argparse
from db import get_monthly_stats, save_ai_recommendations, get_user
from db.recommendation import get_ai_recommendation
from db.schema import get_connection

def regen_recommendations(user_id: int):
    user_info = get_user(user_id)
    preferred_style = user_info.get("preferred_style", "") if user_info else ""

    stats = get_monthly_stats(user_id)
    color_type = stats["most_color_type"]
    brightness = stats["avg_brightness"]
    redness = stats["avg_redness"]

    if not color_type:
        print("퍼스널컬러 데이터 없음")
        return

    preferred_style = preferred_style or color_type

    print(f"추천 재생성 중: {color_type} / {preferred_style}")
    recs = get_ai_recommendation(color_type, preferred_style, brightness=brightness, redness=redness)

    if recs:
        # 가장 최근 진단에 연결
        conn = get_connection()
        row = conn.execute(
            "SELECT diagnosis_id FROM diagnosis WHERE user_id=? ORDER BY diagnosis_at DESC LIMIT 1",
            (user_id,)
        ).fetchone()

        if row:
            diagnosis_id = row[0]
            # 기존 추천 삭제 후 재생성
            conn.execute("DELETE FROM rec_products WHERE diagnosis_id = ?", (diagnosis_id,))
            conn.commit()
            conn.close()
            save_ai_recommendations(diagnosis_id, recs)
            print(f"{len(recs)}개 추천 저장 완료")
        else:
            conn.close()
            print("진단 기록 없음")
    else:
        print("AI 추천 실패")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--user-id", type=int, required=True)
    args = parser.parse_args()
    regen_recommendations(args.user_id)
