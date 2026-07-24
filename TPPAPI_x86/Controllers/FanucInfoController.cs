using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace TPPAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FanucInfoController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FanucInfoController> _logger;

        public FanucInfoController(ILogger<FanucInfoController> logger)
        {
            var builder = new ConfigurationBuilder()
                               .SetBasePath(Directory.GetCurrentDirectory())
                               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _config = builder.Build();
            _logger = logger;
        }

        [HttpGet("/")]
        public IEnumerable<string> Get()
        {
            return new string[] { "FanucProgGcode1", "FanucProgGcode2" };
        }

        [HttpGet("getmachine/{ip}")]
        public async Task<IActionResult> GetMachine(string ip)
        {
            try
            {
                using (var cncClient = new FanucCncClient(ip, 8193))
                {
                    // ถ้าเชื่อมต่อสำเร็จและดึงข้อมูลได้สำเร็จ จะส่ง Object ข้อมูลกลับไปตามปกติ
                    var result = await cncClient.GetMachine();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // 💥 ดักจับข้อผิดพลาดทั้งหมดที่เคยถูกซ่อนไว้ แล้วพ่นออกมาเป็น JSON
                return StatusCode(500, new
                {
                    status = "Error",
                    message = $"เกิดข้อผิดพลาดในการเชื่อมต่อหรือดึงข้อมูลจาก IP: {ip}",
                    error_details = ex.Message,                    // ข้อความข้อผิดพลาดหลัก
                    inner_error = ex.InnerException?.Message,      // ข้อความลึกซึ้ง (ถ้ามี เช่น ปัญหาเรื่องสิทธิ์โหลด DLL)
                    stack_trace = ex.StackTrace,                   // ชี้จุดบรรทัดที่โค้ดพัง
                    server_architecture = IntPtr.Size == 8 ? "64-bit (IIS Run Mode)" : "32-bit (IIS Run Mode)"
                });
            }
        }

        [HttpGet("getprogdata/{ip}")]
        public async Task<IActionResult> GetProgramData(string ip)
        {
            try
            {
                using (var cncClient = new FanucCncClient(ip, 8193))
                {
                    // ถ้าเชื่อมต่อสำเร็จและดึงข้อมูลได้สำเร็จ จะส่ง Object ข้อมูลกลับไปตามปกติ
                    var result = await cncClient.GetProgramData();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // 💥 ดักจับข้อผิดพลาดทั้งหมดที่เคยถูกซ่อนไว้ แล้วพ่นออกมาเป็น JSON
                return StatusCode(500, new
                {
                    status = "Error",
                    message = $"เกิดข้อผิดพลาดในการเชื่อมต่อหรือดึงข้อมูลจาก IP: {ip}",
                    error_details = ex.Message,                    // ข้อความข้อผิดพลาดหลัก
                    inner_error = ex.InnerException?.Message,      // ข้อความลึกซึ้ง (ถ้ามี เช่น ปัญหาเรื่องสิทธิ์โหลด DLL)
                    stack_trace = ex.StackTrace,                   // ชี้จุดบรรทัดที่โค้ดพัง
                    server_architecture = IntPtr.Size == 8 ? "64-bit (IIS Run Mode)" : "32-bit (IIS Run Mode)"
                });
            }

        }

        [HttpGet("getlistprog/{ip}")]
        public async Task<IActionResult> GetListProgram(string ip)
        {
            try
            {
                using (var cncClient = new FanucCncClient(ip, 8193))
                {
                    // ถ้าเชื่อมต่อสำเร็จและดึงข้อมูลได้สำเร็จ จะส่ง Object ข้อมูลกลับไปตามปกติ
                    var result = await cncClient.GetListProgram();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // 💥 ดักจับข้อผิดพลาดทั้งหมดที่เคยถูกซ่อนไว้ แล้วพ่นออกมาเป็น JSON
                return StatusCode(500, new
                {
                    status = "Error",
                    message = $"เกิดข้อผิดพลาดในการเชื่อมต่อหรือดึงข้อมูลจาก IP: {ip}",
                    error_details = ex.Message,                    // ข้อความข้อผิดพลาดหลัก
                    inner_error = ex.InnerException?.Message,      // ข้อความลึกซึ้ง (ถ้ามี เช่น ปัญหาเรื่องสิทธิ์โหลด DLL)
                    stack_trace = ex.StackTrace,                   // ชี้จุดบรรทัดที่โค้ดพัง
                    server_architecture = IntPtr.Size == 8 ? "64-bit (IIS Run Mode)" : "32-bit (IIS Run Mode)"
                });
            }


        }

        [HttpGet("getstatus/{ip}")]
        public async Task<IActionResult> GetStatus(string ip)
        {
            try
            {
                using (var cncClient = new FanucCncClient(ip, 8193))
                {
                    // ถ้าเชื่อมต่อสำเร็จและดึงข้อมูลได้สำเร็จ จะส่ง Object ข้อมูลกลับไปตามปกติ
                    var result = await cncClient.GetStatus();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // 💥 ดักจับข้อผิดพลาดทั้งหมดที่เคยถูกซ่อนไว้ แล้วพ่นออกมาเป็น JSON
                return StatusCode(500, new
                {
                    status = "Error",
                    message = $"เกิดข้อผิดพลาดในการเชื่อมต่อหรือดึงข้อมูลจาก IP: {ip}",
                    error_details = ex.Message,                    // ข้อความข้อผิดพลาดหลัก
                    inner_error = ex.InnerException?.Message,      // ข้อความลึกซึ้ง (ถ้ามี เช่น ปัญหาเรื่องสิทธิ์โหลด DLL)
                    stack_trace = ex.StackTrace,                   // ชี้จุดบรรทัดที่โค้ดพัง
                    server_architecture = IntPtr.Size == 8 ? "64-bit (IIS Run Mode)" : "32-bit (IIS Run Mode)"
                });
            }

        }

        [HttpGet("gettool/{ip}")]
        public async Task<IActionResult> GetToolData(string ip)
        {
            try
            {
                using (var cncClient = new FanucCncClient(ip, 8193))
                {
                    // ถ้าเชื่อมต่อสำเร็จและดึงข้อมูลได้สำเร็จ จะส่ง Object ข้อมูลกลับไปตามปกติ
                    var result = await cncClient.GetToolData();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // 💥 ดักจับข้อผิดพลาดทั้งหมดที่เคยถูกซ่อนไว้ แล้วพ่นออกมาเป็น JSON
                return StatusCode(500, new
                {
                    status = "Error",
                    message = $"เกิดข้อผิดพลาดในการเชื่อมต่อหรือดึงข้อมูลจาก IP: {ip}",
                    error_details = ex.Message,                    // ข้อความข้อผิดพลาดหลัก
                    inner_error = ex.InnerException?.Message,      // ข้อความลึกซึ้ง (ถ้ามี เช่น ปัญหาเรื่องสิทธิ์โหลด DLL)
                    stack_trace = ex.StackTrace,                   // ชี้จุดบรรทัดที่โค้ดพัง
                    server_architecture = IntPtr.Size == 8 ? "64-bit (IIS Run Mode)" : "32-bit (IIS Run Mode)"
                });
            }

        }

        [HttpGet("getparam/{ip}")]
        public async Task<IActionResult> GetParam(string ip)
        {
            try
            {
                using (var cncClient = new FanucCncClient(ip, 8193))
                {
                    // ถ้าเชื่อมต่อสำเร็จและดึงข้อมูลได้สำเร็จ จะส่ง Object ข้อมูลกลับไปตามปกติ
                    var result = await cncClient.GetParam();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // 💥 ดักจับข้อผิดพลาดทั้งหมดที่เคยถูกซ่อนไว้ แล้วพ่นออกมาเป็น JSON
                return StatusCode(500, new
                {
                    status = "Error",
                    message = $"เกิดข้อผิดพลาดในการเชื่อมต่อหรือดึงข้อมูลจาก IP: {ip}",
                    error_details = ex.Message,                    // ข้อความข้อผิดพลาดหลัก
                    inner_error = ex.InnerException?.Message,      // ข้อความลึกซึ้ง (ถ้ามี เช่น ปัญหาเรื่องสิทธิ์โหลด DLL)
                    stack_trace = ex.StackTrace,                   // ชี้จุดบรรทัดที่โค้ดพัง
                    server_architecture = IntPtr.Size == 8 ? "64-bit (IIS Run Mode)" : "32-bit (IIS Run Mode)"
                });
            }

        }


        [HttpGet("gengcode/{ip},{program}")]
        public async Task<IActionResult> GetGCode(string ip, string program)
        {
            // ตรวจสอบค่าพารามิเตอร์เบื้องต้นก่อนทำงาน
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(program))
            {
                return BadRequest(new { status = "Error", message = "กรุณาระบุข้อมูล IP และ ชื่อโปรแกรม ให้ครบถ้วน" });
            }

            try
            {
                var pythonPath = _config.GetValue<string>("PythonSettings:PythonPath");
                var scriptPath = _config.GetValue<string>("PythonSettings:ScriptPath");

                if (string.IsNullOrEmpty(pythonPath) || string.IsNullOrEmpty(scriptPath))
                {
                    return StatusCode(500, new { 
                        status = "Error", 
                        message = "ไม่พบการตั้งค่า PythonPath หรือ ScriptPath ใน Configuration" 
                    });
                }

                var start = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"\"{scriptPath}\" \"{ip}\" \"{program}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string output = "";
                string errors = "";

                using (var process = Process.Start(start))
                {
                    if (process == null)
                    {
                        return StatusCode(500, new { status = "Error", message = "ไม่สามารถเริ่มต้นระบบ Process ของ Python ได้" });
                    }

                    // อ่านข้อมูลจาก Stream พร้อมกันเพื่อป้องกันปัญหา Deadlock ของ Process
                    var errorTask = process.StandardError.ReadToEndAsync();
                    var outputTask = process.StandardOutput.ReadToEndAsync();

                    await Task.WhenAll(errorTask, outputTask);
                    
                    errors = errorTask.Result;
                    output = outputTask.Result;

                    await process.WaitForExitAsync();

                    // กรณี Python สคริปต์ทำงานไม่สำเร็จ (Exit Code ไม่เป็น 0)
                    if (process.ExitCode != 0)
                    {
                        return StatusCode(500, new 
                        { 
                            status = "Python Runtime Error", 
                            exit_code = process.ExitCode,
                            message = "สคริปต์ Python รายงานข้อผิดพลาด",
                            error_details = errors.Trim()
                        });
                    }
                }

                // คืนค่า G-Code กลับไปเมื่อสำเร็จ
                return Ok(output);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // ดักจับกรณีชี้ Path ไปหา python.exe ผิด หรือสิทธิ์ของ IIS Access โดนปฏิเสธ
                return StatusCode(500, new 
                { 
                    status = "System Error", 
                    message = "เกิดข้อผิดพลาดในการเรียกใช้ไฟล์ปฏิบัติการ (อาจเกิดจาก Path ของ Python ไม่ถูกต้อง)",
                    error_details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                // ดักจับข้อผิดพลาดทั่วไปอื่น ๆ (Uncaught Exceptions)
                return StatusCode(500, new 
                { 
                    status = "Internal Server Error", 
                    message = "เกิดข้อผิดพลาดที่ไม่คาดคิดในระบบ API",
                    error_details = ex.Message,
                    stack_trace = ex.StackTrace 
                });
            }
        }


    }
}