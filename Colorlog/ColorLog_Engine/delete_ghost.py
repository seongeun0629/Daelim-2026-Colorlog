from db.schema import get_connection

conn = get_connection()
conn.execute("PRAGMA foreign_keys = OFF")
conn.execute("DELETE FROM rec_products WHERE diagnosis_id IN (SELECT diagnosis_id FROM diagnosis WHERE user_id = 1)")
conn.execute("DELETE FROM diagnosis WHERE user_id = 1")
conn.execute("DELETE FROM users WHERE user_id = 1")
conn.execute("PRAGMA foreign_keys = ON")
conn.commit()
conn.close()
print("완료")
