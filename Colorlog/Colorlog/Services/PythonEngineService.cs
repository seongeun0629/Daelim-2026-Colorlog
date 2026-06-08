using Colorlog.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;

namespace Colorlog.Services
{
    public class PythonEngineService
    {
        private Process? _process;

        public event Action<JObject>? OnColorDetected;

        public void Start(int userId = 1)
        {
            if (_process != null && !_process.HasExited)
                Stop();

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string pythonPath = Path.Combine(userProfile, @"anaconda3\envs\colorlog\python.exe");

            if (!File.Exists(pythonPath))
                pythonPath = "python";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\"));
            string engineDir = Path.Combine(projectRoot, "ColorLog_Engine");
            string scriptPath = Path.Combine(engineDir, "main.py");

            if (!Directory.Exists(engineDir))
            {
                engineDir = @"D:\project\Colorlog\Colorlog\ColorLog_Engine";
                scriptPath = Path.Combine(engineDir, "main.py");
            }

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"\"{scriptPath}\" --user-id {userId}", 
                    WorkingDirectory = engineDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;

                try
                {
                    var json = JObject.Parse(e.Data);

                    OnColorDetected?.Invoke(json);

                    if (json["face_detected"]?.Value<bool>() == true)
                    {
                        var colorType = json["personal_color"]?["type"]?.ToString();
                        Debug.WriteLine($"[분석] {colorType ?? "색상 추출 중"}");
                    }

                    if (json["diagnosis_saved"]?.Value<bool>() == true)
                    {
                        Debug.WriteLine($"[저장 완료] diagnosis_id: {json["diagnosis_id"]}");
                    }
                }
                catch
                {
                    Debug.WriteLine($"[파싱 스킵]: {e.Data}");
                }
            };

            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[파이썬 에러]: {e.Data}");
            };

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파이썬 프로세스 시작 실패: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"파이썬 엔진을 시작할 수 없습니다.\n\nPython: {pythonPath}\nDir: {engineDir}");
            }
        }

        public void Stop()
        {
            if (_process == null) return;

            try
            {
                if (!_process.HasExited)
                    _process.Kill();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"프로세스 종료 실패: {ex.Message}");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        public void RegenRecommendations(int userId)
        {
            var engineDir = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\ColorLog_Engine"));

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c conda activate colorlog && python regen_recs.py --user-id {userId}",
                WorkingDirectory = engineDir,
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            Process.Start(psi);
        }
    }
}