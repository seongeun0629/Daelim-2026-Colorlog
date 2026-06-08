from db.schema import get_connection

conn = get_connection()
conn.execute("UPDATE users SET user_name = ? WHERE user_id = ?", ("김민지", 3))
conn.execute("UPDATE users SET user_name = ? WHERE user_id = ?", ("이수지", 4))
conn.execute("UPDATE users SET user_name = ? WHERE user_id = ?", ("김철수", 5))
conn.commit()
conn.close()
print("완료")
