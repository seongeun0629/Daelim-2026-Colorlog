using Colorlog.Models;
using Colorlog.ViewModels;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF.UI.Common;

namespace Colorlog.Services
{
    public class DatabaseService
    {
        // 1. 파이썬이랑 동일한 db 파일 이름 사용해야됨
        private readonly string _connectionString;

        public DatabaseService()
        {
            var dbPath = Path.GetFullPath(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\..\ColorLog_Engine\db\colorlog.db"
                )
            );
            Debug.WriteLine($"[DB] 경로: {dbPath}");
            _connectionString = $"Data Source={dbPath}";
        }

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

        public int InsertUser(User user)
        {
            using var connection = OpenConnection();
            CreateUserTableIfNotExist(connection);

            using var checkCmd = new SqliteCommand(
                "SELECT user_id FROM users WHERE user_name = $userName;", connection);
            checkCmd.Parameters.AddWithValue("$userName", user.UserName);
            var existing = checkCmd.ExecuteScalar();
            if (existing != null)
                return Convert.ToInt32(existing);

            using var cmd = new SqliteCommand(@"
                INSERT INTO users (user_name, gender, age, created_at)
                VALUES ($userName, $gender, $age, $createdAt);", connection);

            cmd.Parameters.AddWithValue("$userName", user.UserName);
            cmd.Parameters.AddWithValue("$gender", (object?)user.Gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$age", (object?)user.Age ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();

            using var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection);

            return Convert.ToInt32(idCmd.ExecuteScalar());
        }

        public void UpdateUser(int userId, string userName, string? gender, string? age)
        {
            using var connection = OpenConnection();
            using var cmd = new SqliteCommand(@"
                UPDATE users
                SET user_name = $userName,
                    gender    = $gender,
                    age       = $age
                WHERE user_id = $userId;", connection);

            cmd.Parameters.AddWithValue("$userName", userName);
            cmd.Parameters.AddWithValue("$gender", (object?)gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$age", (object?)age ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.ExecuteNonQuery();
        }

        public List<User> GetAllUsers()
        {
            var result = new List<User>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(@"
                    SELECT user_id, user_name, gender, age, profile_image_path
                    FROM users
                    ORDER BY created_at DESC ;", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    result.Add(new User
                    {
                        UserId = reader.GetInt32(0),
                        UserName = reader.GetString(1),
                        Gender = reader.IsDBNull(2) ? "선택 안 함" : reader.GetString(2),
                        Age = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        ProfileImagePath = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetAllUsers 오류: {ex.Message}");
            }
            return result;
        }


        public User? GetLatestUser()
        {
            using var connection = OpenConnection();
            CreateUserTableIfNotExist(connection);

            using var cmd = new SqliteCommand(@"
                SELECT user_id, user_name, gender, age, profile_image_path
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
                    Age = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    ProfileImagePath = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
            }
            return null;
        }

        public User? GetUserById(int userId)
        {
            using var connection = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT user_id, user_name, gender, age, profile_image_path
                FROM users
                WHERE user_id = $userId;", connection);
            cmd.Parameters.AddWithValue("$userId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Gender = reader.IsDBNull(2) ? "선택 안 함" : reader.GetString(2),
                    Age = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    ProfileImagePath = reader.IsDBNull(4) ? null : reader.GetString(4)
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

        // 프로필 이미지 경로 업데이트 함수
        public void UpdateUserProfileImage(int userId, string imagePath)
        {
            using var connection = OpenConnection();
            using var cmd = new SqliteCommand(@"
                UPDATE users SET profile_image_path = $path
                WHERE user_id = $userId;", connection);
            cmd.Parameters.AddWithValue("$path", imagePath);
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.ExecuteNonQuery();
        }

        public UserStatsDto GetUserStats(int userId)
        {
            try
            {
                using var connection = OpenConnection();

                using var countCmd = new SqliteCommand(
                    "SELECT COUNT(*) FROM diagnosis WHERE user_id = $userId;", connection);
                countCmd.Parameters.AddWithValue("$userId", userId);
                var count = Convert.ToInt32(countCmd.ExecuteScalar());

                using var joinCmd = new SqliteCommand(
                    "SELECT created_at FROM users WHERE user_id = $userId;", connection);
                joinCmd.Parameters.AddWithValue("$userId", userId);
                var joinDateStr = joinCmd.ExecuteScalar()?.ToString() ?? string.Empty;
                var joinDate = string.Empty;
                if (DateTime.TryParse(joinDateStr, out var dt))
                    joinDate = dt.ToString("yyyy.MM.dd");

                using var colorCmd = new SqliteCommand(@"
                    SELECT pct.type_name
                    FROM diagnosis d
                    LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
                    WHERE d.user_id = $userId
                    ORDER BY d.diagnosis_at DESC
                    LIMIT 1;", connection);
                colorCmd.Parameters.AddWithValue("$userId", userId);
                var latestColor = colorCmd.ExecuteScalar()?.ToString() ?? string.Empty;

                return new UserStatsDto
                {
                    DiagnosisCount = count,
                    JoinDate = joinDate,
                    LatestColorType = latestColor,
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetUserStats 오류: {ex.Message}");
                return new UserStatsDto();
            }
        }

        public string GetPreferredStyle(int userId)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "SELECT preferred_style FROM users WHERE user_id = $userId;", connection);
                cmd.Parameters.AddWithValue("$userId", userId);
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetPreferredStyle 오류: {ex.Message}");
                return string.Empty;
            }
        }

        public void UpdatePreferredStyle(int userId, string preferredStyle)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(
                    "UPDATE users SET preferred_style = $style WHERE user_id = $userId;", connection);
                cmd.Parameters.AddWithValue("$style", preferredStyle);
                cmd.Parameters.AddWithValue("$userId", userId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] UpdatePreferredStyle 오류: {ex.Message}");
            }
        }

        //-------------------------------
        //대시보드용
        //-------------------------------
        public DiagnosisSummary? GetLatestDiagnosis(int userId)
        {
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(@"
                    SELECT d.diagnosis_at, d.brightness, d.redness, pct.type_name, d.type_id,
                           d.oily_status, d.oily_score,
                           d.zone_forehead_r, d.zone_forehead_g, d.zone_forehead_b,
                           d.zone_lcheek_r,   d.zone_lcheek_g,   d.zone_lcheek_b,
                           d.zone_rcheek_r,   d.zone_rcheek_g,   d.zone_rcheek_b,
                           d.zone_nose_r,     d.zone_nose_g,     d.zone_nose_b,
                           d.zone_chin_r,     d.zone_chin_g,     d.zone_chin_b
                    FROM diagnosis d
                    LEFT JOIN personal_color_types pct ON d.type_id = pct.type_id
                    WHERE d.user_id = $userId
                    ORDER BY d.diagnosis_at DESC
                    LIMIT 1;", connection);

                cmd.Parameters.AddWithValue("$userId", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    static (int, int, int)? ReadZone(SqliteDataReader r, int ri, int gi, int bi)
                        => r.IsDBNull(ri) ? null : (r.GetInt32(ri), r.GetInt32(gi), r.GetInt32(bi));

                    return new DiagnosisSummary
                    {
                        DiagnosisAt = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        Brightness = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        Redness = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        PersonalColorName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        TypeId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        OilyStatus = reader.IsDBNull(5) ? null : reader.GetString(5),
                        OilyScore = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                        ZoneForehead = ReadZone(reader, 7, 8, 9),
                        ZoneLCheek = ReadZone(reader, 10, 11, 12),
                        ZoneRCheek = ReadZone(reader, 13, 14, 15),
                        ZoneNose = ReadZone(reader, 16, 17, 18),
                        ZoneChin = ReadZone(reader, 19, 20, 21),
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetLatestDiagnosis 오류: {ex.Message}");
            }
            return null;
        }

        public List<string> GetColorsByTypeId(int typeId)
        {
            var result = new List<string>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(@"
                    SELECT colors FROM personal_color_types
                    WHERE type_id = $typeId;", connection);
                cmd.Parameters.AddWithValue("$typeId", typeId);

                var colors = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(colors))
                    result = colors.Split(',')
                                   .Select(c => c.Trim())
                                   .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetColorsByTypeId 오류: {ex.Message}");
            }
            return result;
        }

        public List<RecommendedProduct> GetLatestRecommendations(int userId)
        {
            var result = new List<RecommendedProduct>();
            try
            {
                using var connection = OpenConnection();
                using var cmd = new SqliteCommand(@"
                    SELECT p.product_name, p.category, p.product_url, r.rec_reason
                    FROM rec_products r
                    JOIN products p ON r.product_id = p.product_id
                    WHERE r.diagnosis_id = (
                        SELECT diagnosis_id FROM diagnosis
                        WHERE user_id = $userId
                        ORDER BY diagnosis_at DESC
                        LIMIT 1
                    )
                    ORDER BY r.rec_id;", connection);
                cmd.Parameters.AddWithValue("$userId", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var recReason = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                    var parts = recReason.Split('|');
                    var reason = parts[0];
                    var rating = parts.Length > 1 ? parts[1] : "-";

                    result.Add(new RecommendedProduct
                    {
                        ProductName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                        Category = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        ProductUrl = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        RecReason = reason,
                        Rating = rating,
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] GetLatestRecommendations 오류: {ex.Message}");
            }
            return result;
        }
    }


}
