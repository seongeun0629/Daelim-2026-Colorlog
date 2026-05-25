
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Colorlog.Services
{
    public class PythonEngineService
    {
        private Process _process;

        public event Action<JObject> OnColorDetected;
        public event Action<JObject> OnDiagnosisSaved;

        // 팀원 각자 Python 실행 파일 경로를 맞게 수정하세요.
        private const string PythonExe = @"C:\Users\alice\AppData\Local\Programs\Python\Python312\python.exe";

        public int CurrentUserId { get; set; } = -1;

        private static string GetEngineDir()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\ColorLog_Engine"));
        }

        public void Start(string userName = "사용자", string age = null)
        {
            if (_process != null && !_process.HasExited)
                Stop();

            var engineDir = GetEngineDir();
            var scriptPath = Path.Combine(engineDir, "main.py");
            var arguments = $"\"{scriptPath}\" --user-name \"{userName}\"";
            if (!string.IsNullOrWhiteSpace(age))
                arguments += $" --age \"{age}\"";
            if (CurrentUserId >= 0)
                arguments += $" --user-id {CurrentUserId}";

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PythonExe,
                    Arguments = arguments,
                    WorkingDirectory = engineDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    try
                    {
                        var json = JObject.Parse(e.Data);
                        if (json["diagnosis_saved"]?.Value<bool>() == true)
                        {
                            // main.py가 실제로 사용한 user_id로 CurrentUserId 동기화
                            var uid = json["user_id"]?.Value<int>() ?? -1;
                            if (uid >= 0) CurrentUserId = uid;
                            OnDiagnosisSaved?.Invoke(json);
                            Debug.WriteLine($"[DB] 진단 저장 완료 (user_id={CurrentUserId})");
                        }

                        if (json["face_detected"]?.Value<bool>() == true)
                        {
                            var colorType = json["personal_color"]?["type"]?.ToString();
                            if (!string.IsNullOrEmpty(colorType))
                            {
                                OnColorDetected?.Invoke(json);
                                Debug.WriteLine($"[성공] 분석된 컬러: {colorType}");
                            }
                        }
                    }
                    catch
                    {
                        Debug.WriteLine("[Parsing Skip]: 아직 얼굴이 감지되지 않았습니다.");
                    }
                }
            };

            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"!!! [파이썬 에러]: {e.Data}");
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Stop() { _process?.Kill(); }

        /// <summary>save_user.py를 실행하고 stdout에서 user_id를 읽어 CurrentUserId를 설정합니다.</summary>
        public async Task<int> SaveUserAsync(string userName, string age = null)
        {
            var engineDir = GetEngineDir();
            var arguments = $"-m db.save_user --user-name \"{userName}\"";
            if (!string.IsNullOrWhiteSpace(age))
                arguments += $" --age \"{age}\"";
            if (CurrentUserId >= 0)
                arguments += $" --user-id {CurrentUserId}";

            var output = await RunScriptAsync(engineDir, arguments);
            if (int.TryParse(output.Trim(), out var userId))
                CurrentUserId = userId;

            return CurrentUserId;
        }

        /// <summary>Settings 화면에서 이름/나이 변경 시 즉시 DB 저장 (fire-and-forget용 동기 래퍼).</summary>
        public void SaveUser(string userName, string age)
        {
            _ = SaveUserAsync(userName, age);
        }

        /// <summary>특정 년/월의 히스토리 + 최근 7일 트렌드를 반환합니다.</summary>
        public async Task<JObject> QueryHistoryAsync(int year, int month)
        {
            var engineDir = GetEngineDir();
            var arguments = $"-m db.query_history --year {year} --month {month}";
            if (CurrentUserId >= 0)
                arguments += $" --user-id {CurrentUserId}";

            var output = await RunScriptAsync(engineDir, arguments);
            try { return JObject.Parse(output); }
            catch { return new JObject(); }
        }

        /// <summary>현재 유저의 진단·추천 기록을 전부 삭제합니다.</summary>
        public async Task DeleteRecordsAsync()
        {
            if (CurrentUserId < 0) return;
            var engineDir = GetEngineDir();
            var arguments = $"-m db.delete_records --user-id {CurrentUserId}";
            await RunScriptAsync(engineDir, arguments);
        }

        /// <summary>Settings 화면용 유저 통계(진단 횟수, 가입일, 최근 퍼스널컬러)를 반환합니다.</summary>
        public async Task<JObject> QueryUserStatsAsync()
        {
            var engineDir = GetEngineDir();
            var arguments = $"-m db.query_user_stats";
            if (CurrentUserId >= 0)
                arguments += $" --user-id {CurrentUserId}";

            var output = await RunScriptAsync(engineDir, arguments);
            try { return JObject.Parse(output); }
            catch { return new JObject(); }
        }

        private async Task<string> RunScriptAsync(string engineDir, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PythonExe,
                    Arguments = arguments,
                    WorkingDirectory = engineDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit());
            var output = await stdoutTask;
            var errors = await stderrTask;
            if (!string.IsNullOrWhiteSpace(errors))
                Debug.WriteLine($"[Python Script Error] {arguments}\n{errors}");
            return output.Trim();
        }
    }
}
