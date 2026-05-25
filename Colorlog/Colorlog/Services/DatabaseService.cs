using Colorlog.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colorlog.Services
{
    public class DatabaseService
    {
        // 1. 파이썬이랑 동일한 db 파일 이름 사용해야됨
        private readonly string _connectionString = "Data Source=colorlog.db";

        /// <summary>
        /// users 테이블이 없을 경우, 파이썬 스키마 규격과 완전히 동일하게 자동 생성
        /// </summary>
        private void CreateUserTableIfNotExist(SqliteConnection connection)
        {
            string createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                user_id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_name TEXT NOT NULL,
                gender TEXT,
                age TEXT,
                created_at TEXT NOT NULL
            );";

            using (var cmd = new SqliteCommand(createTableSql, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        ///<summary>
        /// 새로운 사용자를 DB의 users 테이블에 저장하는 함수 (create_)
        /// </summary>
        public void InsertUser(User user)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                CreateUserTableIfNotExist(connection);

                // 2. 파이썬 conn.execute("PRAGMA foreign_keys = ON")랑 같은 역할
                using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

                //3. 파이썬 스키마의 'users' 테이블 컬럼명과 정확히 매칭하는 SQL 인서트문
                string insertSql = @"
                    INSERT INTO users (user_name, gender, age, created_at) 
                    VALUES ($userName, $gender, $age, $createdAt);";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    // 4. 보안, 에러 방지를 위해 SQL 파라미터 사용
                    command.Parameters.AddWithValue("$userName", user.UserName);

                    // 성별이나 나이는 NULL(입력 안 함)일 수 있음
                    command.Parameters.AddWithValue("$gender", (object)user.Gender ?? DBNull.Value);
                    command.Parameters.AddWithValue("$age", (object)user.Age ?? DBNull.Value);

                    // 현재 시간 (예: 2026-05-25 23:45:00)을 파이썬 포맷과 맞춰서 넣습니다.
                    command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    // 5. DB에 명령 실행 
                    command.ExecuteNonQuery();

                }
            }
        }

        /// <summary>
        /// 설정창 진입 시 DB에서 가장 최근에 저장된 사용자 한 명을 불러오는 함수 (Read)
        /// </summary>
        public User? GetLatestUser()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // 여기서도 안전하게 테이블 체크 한번
                CreateUserTableIfNotExist(connection);

                // 가장 최근에 생성된 유저 1명만 조회
                string selectSql = "SELECT user_name, gender, age FROM users ORDER BY user_id DESC LIMIT 1;";

                using (var command = new SqliteCommand(selectSql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserName = reader.GetString(0),
                                // DB 데이터가 NULL일 경우를 대비한 방어문들
                                Gender = reader.IsDBNull(1) ? "선택 안 함" : reader.GetString(1),
                                Age = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                            };
                        }
                    }
                }
            }
            return null; // DB에 저장된 유저가 한 명도 없을 때
        }
    }
}
