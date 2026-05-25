
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Colorlog.Services
{
    public class PythonEngineService
    {
        private Process _process;

        public event Action<JObject> OnColorDetected;

        public void Start()
        {
            if (_process != null && !_process.HasExited)
            {
                Stop();
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string pythonPath = Path.Combine(userProfile, @"anaconda3\envs\colorlog\python.exe");

            if (!File.Exists(pythonPath))
            {
                pythonPath = "python";
            }


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
                    Arguments = $"\"{scriptPath}\"", 
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
                    catch (Exception)
                    {
                        Debug.WriteLine($"[Parsing Skip]: 아직 얼굴이 감지되지 않았습니다.");
                    }
                }
            };

            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.WriteLine($"!!! [파이썬 에러]: {e.Data}");
                }
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
                System.Windows.MessageBox.Show($"파이썬 엔진을 시작할 수 없습니다.\n경로를 확인해주세요.\n\nPython: {pythonPath}\nDir: {engineDir}");
            }
        }

        public void Stop() { _process?.Kill(); }
    }
}
