
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\NOW\anaconda3\envs\colorlog\python.exe",
                    Arguments = @"main.py",
                    WorkingDirectory = @"D:\project\Colorlog\Colorlog\ColorLog_Engine",
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
                    catch (Exception ex)
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

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine(); 
        }

        public void Stop() { _process?.Kill(); }
    }
}
