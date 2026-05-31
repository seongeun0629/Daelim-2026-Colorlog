using Colorlog.Models;
using Colorlog.ViewModels;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colorlog.Services
{
    public class DatabaseService
    {
        // 1. 파이썬이랑 동일한 db 파일 이름 사용해야됨
        private readonly string _connectionString = "Data Source=colorlog.db";

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var pragma = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            pragma.ExecuteNonQuery();
            return connection;
        }

        private void CreateUserTableIfNotExist(SqliteConnection connection)
        {
            using var cmd = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS users (
                    user_id    INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_name  TEXT    NOT NULL,
                    gender     TEXT,
                    age        TEXT,
                    created_at TEXT    NOT NULL
                );", connection);
            cmd.ExecuteNonQuery();
        }

        public void InsertUser(User user)
        {
            using var connection = OpenConnection();
            CreateUserTableIfNotExist(connection);

            using var cmd = new SqliteCommand(@"
                INSERT INTO users (user_name, gender, age, created_at)
                VALUES ($userName, $gender, $age, $createdAt);", connection);

            cmd.Parameters.AddWithValue("$userName", user.UserName);
            cmd.Parameters.AddWithValue("$gender",   (object?)user.Gender   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$age",      (object?)user.Age      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 설정창 진입 시 DB에서 가장 최근에 저장된 사용자 한 명을 불러오는 함수 (Read)
        /// </summary>
        public User? GetLatestUser()
        {
            using var connection = OpenConnection();
            CreateUserTableIfNotExist(connection);

            using var cmd = new SqliteCommand(@"
                SELECT user_id, user_name, gender, age
                FROM users
                ORDER BY user_id DESC
                LIMIT 1;", connection);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Gender = reader.IsDBNull(2) ? "선택 안 함" : reader.GetString(2),
                    Age = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                };
            }
            return null;
        }

        public List<HistoryDayRecord> GetDiagnosesLast7Days(int userId)
        {
            var result = new List<HistoryDayRecord>();

            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(@"
                    SELECT
                        DATE(d.diagnosis_at)        AS date,
                        d.brightness,
                        d.redness,
                        pct.type_name               AS personal_color
                    FROM diagnosis d
                    LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
                    WHERE d.user_id = $userId
                      AND d.diagnosis_at >= DATE('now', 'localtime', '-6 days')
                    GROUP BY DATE(d.diagnosis_at)
                    HAVING d.diagnosis_id = MAX(d.diagnosis_id)
                    ORDER BY date ASC;", connection);

                cmd.Parameters.AddWithValue("$userId", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dateStr = reader.GetString(0);
                    var brightness = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    var redness = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    var personalColor = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        result.Add(new HistoryDayRecord(personalColor, brightness, redness, string.Empty)
                        {
                            Date = date
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetDiagnosesLast7Days 오류: {ex.Message}");
            }

            return result;
        }

        public List<HistoryDayRecord> GetDiagnosesByMonth(int userId, int year, int month)
        {
            var result = new List<HistoryDayRecord>();

            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(@"
                    SELECT
                        DATE(d.diagnosis_at)        AS date,
                        d.brightness,
                        d.redness,
                        d.note,
                        pct.type_name               AS personal_color
                    FROM diagnosis d
                    LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
                    WHERE d.user_id = $userId
                      AND strftime('%Y-%m', d.diagnosis_at) = $monthStr
                    GROUP BY DATE(d.diagnosis_at)
                    HAVING d.diagnosis_id = MAX(d.diagnosis_id)
                    ORDER BY date ASC;", connection);

                cmd.Parameters.AddWithValue("$userId", userId);
                cmd.Parameters.AddWithValue("$monthStr", $"{year}-{month:00}");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dateStr = reader.GetString(0);
                    var brightness = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    var redness = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    var note = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                    var personalColor = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);

                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        result.Add(new HistoryDayRecord(personalColor, brightness, redness, note)
                        {
                            Date = date
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetDiagnosesByMonth 오류: {ex.Message}");
            }

            return result;
        }
    }
}
