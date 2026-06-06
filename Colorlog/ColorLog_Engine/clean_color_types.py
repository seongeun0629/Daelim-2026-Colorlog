from db.schema import get_connection

conn = get_connection()
conn.execute("PRAGMA foreign_keys = OFF")
conn.execute("DELETE FROM personal_color_types WHERE type_id >= 14")
conn.execute("PRAGMA foreign_keys = ON")
conn.commit()
conn.close()
print("완료")
