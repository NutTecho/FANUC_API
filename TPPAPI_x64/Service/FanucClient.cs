using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TPPAPI.Models;

public class FanucCncClient : IDisposable
{
    public string IpAddress { get; }
    public ushort Port { get; }

    // 🔒 ตัวควบคุมคิวการเข้าถึง (SemaphoreSlim) เพื่อรองรับ async/await แบบ Non-blocking แทน lock object ตัวเดิม
    private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

    public FanucCncClient(string ipAddress, ushort port = 8193)
    {
        IpAddress = ipAddress;
        Port = port;
    }

    private async Task<T> ExecuteShortLivedCommandAsync<T>(Func<ushort, T> focasAction, T defaultResult) 
    {
        // 1. รอคิวเข้าใช้งานเครื่องจักรนี้สูงสุด 5 วินาที
        if (!await _asyncLock.WaitAsync(5000))
        {
            throw new TimeoutException($"[Semaphore Timeout] Request ติดคิวนานเกิน 5 วินาทีในการเข้าถึงเครื่องจักร IP: {IpAddress} (มี Thread อื่นล็อกท่ออยู่)");
        }

        ushort currentHandle = 0;
        try
        {
            // 2. เรียกฟังก์ชันดึงการเชื่อมต่อ (ในสถาปัตยกรรม 64-bit ให้เช็กว่าใช้ Focas หรือ Focas1 ตามคลาสที่คุณนำเข้ามา)
            // หมายเหตุ: ถ้าใช้โครงสร้าง 64-bit เต็มตัวของบางค่าย ตัวแปร currentHandle อาจต้องประกาศเป็น IntPtr หรือ uint 
            // ให้ปรับชนิดข้อมูลตรงพารามิเตอร์ตามคำประกาศในโปรเจกต์ใหม่ของคุณได้เลยครับ
            short ret = Focas1.cnc_allclibhndl3(IpAddress, Port, 10, out currentHandle);

            if (ret != Focas1.EW_OK)
            {
                // 💥 จุดสำคัญ 1: เจอรหัส Error จาก FOCAS ให้ Throw ออกไปทันที ไม่ต้องอมค่าไว้
                throw new InvalidOperationException($"[FOCAS Error] cnc_allclibhndl3 ล้มเหลวด้วยรหัส Return Code (RC): {ret}. (IP: {IpAddress}, Port: {Port})");
            }

            if (currentHandle == 0)
            {
                throw new InvalidOperationException($"[FOCAS Handle Error] cnc_allclibhndl3 คืนค่า EW_OK แต่ได้ Handle เป็น 0 (มีโอกาสเกิดจากโครงสร้างข้อมูลผิดพลาดในโหมด 64-bit)");
            }

            // 3. ทำงานฟังก์ชันดึงข้อมูลดั้งเดิม
            return focasAction(currentHandle);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException || ex is TimeoutException))
        {
            // 💥 จุดสำคัญ 2: ดักจับ System Exception เช่น โหลด DLL 64-bit ไม่ขึ้น หรือหาไฟล์ไม่เจอ
            throw new DllNotFoundException($"[System Exception] เกิดข้อผิดพลาดระดับ OS: {ex.Message} -> สันนิษฐาน: หาไฟล์ Fwlib64.dll ไม่เจอ หรือติดสิทธิ์ App Pool บน IIS", ex);
        }
        finally
        {
            // 4. บังคับสับท่อสัญญาณทิ้งทันทีตามสูตร Stateless 
            if (currentHandle != 0)
            {
                Focas1.cnc_freelibhndl(currentHandle);
            }

            // 5. ปล่อยคิวให้ Request ถัดไป
            _asyncLock.Release();
        }
    }

    // =========================================================================
    // ฟังก์ชันดึงข้อมูลทั้งหมด ปรับปรุงให้สร้างและปิด Handle ภายในตัวเองแบบอัตโนมัติ
    // =========================================================================

    public async Task<TimeCount> GetParam()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            TimeCount timecount = new TimeCount();
            Focas1.IODBPSD_2 param = new Focas1.IODBPSD_2();

            short[] paramlist = new short[7] { 6712, 6711, 6750, 6751, 6752, 6757, 6758 };
            double[] outparam = new double[7];

            for (int i = 0; i < paramlist.Count(); i++)
            {
                short _ret = Focas1.cnc_rdparam(handle, paramlist[i], Focas1.ALL_AXES, 1024, param);

                if (_ret == Focas1.EW_OK)
                {
                    outparam[i] = Convert.ToDouble(param.rdata.prm_val);
                }
                else
                {
                    outparam[i] = 0;
                }
            }

            timecount.PART_TOTAL = outparam[0];  //6712
            timecount.PART_COUNT = outparam[1];  //6711
            timecount.POWER_ON = outparam[2] != 0 ? Math.Round(outparam[2] / 60.0, 2) : 0.0; //6750
            timecount.OPERATE_SEC = outparam[3] != 0 ? outparam[3] / 1000.0 : 0.0; //6751
            timecount.OPERATE_HR = outparam[4] != 0 ? Math.Round(outparam[4] / 60, 2) : 0.0; //6752 
            timecount.CYCLE_SEC = outparam[5] != 0 ? outparam[5] / 1000.0 : 0.0;  //6757
            timecount.CYCLE_MIN = outparam[6]; //6758

            return timecount;
        }, null);
    }

    public async Task<MachineInfo> GetMachine()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            MachineInfo mcinfo = new MachineInfo();
            Focas1.ODBSYS sys = new Focas1.ODBSYS();
            Focas1.ODBSYSS soft = new Focas1.ODBSYSS();

            short _ret1 = Focas1.cnc_sysinfo(handle, sys);
            short _ret2 = Focas1.cnc_rdsyssoft(handle, soft);

            if (_ret1 == Focas1.EW_OK && _ret2 == Focas1.EW_OK)
            {
                short max_axis = sys.max_axis;
                char[] cnc_type = sys.cnc_type;
                char[] mt_type = sys.mt_type;
                char[] series = sys.series;
                char[] axes = sys.axes;

                string soft_series = string.Concat(soft.soft_series1, ",", soft.soft_series2);
                string soft_version = string.Concat(soft.soft_version1, ",", soft.soft_version2);

                string module_id = string.Concat(soft.module_id[0].ToString("X4"), ",", soft.module_id[1].ToString("X4"));
                string soft_id = string.Concat(soft.soft_id[0].ToString("X4"), ",", soft.soft_id[1].ToString("X4"));

                mcinfo.CNC_TYPE = new string(cnc_type);
                mcinfo.MC_TYPE = CNCTypetoString(new string(cnc_type));
                mcinfo.MT_TYPE = new string(mt_type);
                mcinfo.MT_DETAIL = MTTypetoString(new string(mt_type));
                mcinfo.MAX_AXIS = Convert.ToInt16(max_axis);
                mcinfo.AXIS_USE = Convert.ToInt16(new string(axes));
                mcinfo.SERIES = new string(series);
                mcinfo.MODULE_ID = module_id;
                mcinfo.SOFT_ID = soft_id;
                mcinfo.SOFT_SERIES = soft_series;
                mcinfo.SOFT_VERSION = soft_version;

                return mcinfo;
            }
            return mcinfo;
        }, null);
    }

    public async Task<StatusModel> GetStatus()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            StatusModel mcstatus = new StatusModel();
            Focas1.ODBDY2_1 dynamic = new Focas1.ODBDY2_1();
            Focas1.ODBST status = new Focas1.ODBST();

            short _ret1 = Focas1.cnc_statinfo(handle, status);
            short _ret2 = Focas1.cnc_rddynamic2(handle, Focas1.ALL_AXES, 1024, dynamic);

            if (_ret1 == Focas1.EW_OK && _ret2 == Focas1.EW_OK)
            {
                Focas1.FAXIS pos = dynamic.pos;
                int[] absolute = pos.absolute;
                int[] distance = pos.distance;
                int[] machine = pos.machine;
                int[] relative = pos.relative;

                short auto = status.aut;
                short tmmode = status.tmmode;
                short runmode = status.run;
                short emergency = status.emergency;
                short alarm = status.alarm;
                short edit = status.edit;
                short mstb = status.mstb;
                short motion = status.motion;
                int act_feed = dynamic.actf;             
                int act_sindle = dynamic.acts;             

                mcstatus.AUTO = auto;
                mcstatus.AUTO_DETAIL = ModeNumberToString(auto);
                mcstatus.RUN = runmode;
                mcstatus.RUN_DETAIL = RunNumberToString(runmode);
                mcstatus.TM_MODE = tmmode;
                mcstatus.TM_DETAIL = TMModeNumberToString(tmmode);
                mcstatus.MSTB = mstb;
                mcstatus.MSTB_DETAIL = MstbToString(mstb);
                mcstatus.ALARM = alarm;
                mcstatus.ALARM_DETAIL = AlarmNumberToString(alarm);
                mcstatus.EMER = emergency;
                mcstatus.EMER_DETAIL = EmerNumberToString(emergency);
                mcstatus.MOTION = motion;
                mcstatus.MOTION_DETAIL = MotionToString(motion);
                mcstatus.EDIT = edit;
                mcstatus.EDIT_DETAIL = EditNumberToString(tmmode, edit);
                mcstatus.ACT_FEED_RATE = act_feed;
                mcstatus.ACT_FEED_RATE = act_sindle;
                mcstatus.ABSOLUTE_POS = absolute;
                mcstatus.DISTANCE_POS = distance;
                mcstatus.MACHINE_POS = machine;
                mcstatus.RELATIVE_POS = relative;

                return mcstatus;
            }
            return mcstatus;
        }, null);
    }

    public async Task<ProgramInfo> GetProgramData()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            ProgramInfo progdata = new ProgramInfo();
            Focas1.ODBDY2_1 status = new Focas1.ODBDY2_1();
            Focas1.ODBNC_1 proginfo = new Focas1.ODBNC_1();

            short _ret1 = Focas1.cnc_rddynamic2(handle, Focas1.ALL_AXES, 1024, status);
            short _ret2 = Focas1.cnc_rdproginfo(handle, 0, 12, proginfo);

            if (_ret1 == Focas1.EW_OK && _ret2 == Focas1.EW_OK)
            {
                int program_main = status.prgmnum; 
                int program_current = status.prgnum;        
                int seqnum = status.seqnum;         

                short reg_prog = proginfo.reg_prg;
                short unreg_prog = proginfo.unreg_prg;
                int used_mem = proginfo.used_mem;
                int unused_mem = proginfo.unused_mem;

                progdata.MAIN_PROG = program_main.ToString();
                progdata.CURRENT_PROG = program_current.ToString();
                progdata.UNREG_PROG = unreg_prog;
                progdata.SEQNUM = seqnum.ToString();
                progdata.REG_PROG = reg_prog;
                progdata.USED_MEM = used_mem;
                progdata.UNUSED_MEM = unused_mem;

                return progdata;
            }
            return progdata;
        }, null);
    }

    public async Task<List<ProgramList>> GetListProgram()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            List<ProgramList> proglist = new List<ProgramList>();
            Focas1.PRGDIR3_data[] listdir = new Focas1.PRGDIR3_data[10];
            Focas1.PRGDIR3 dir1 = new Focas1.PRGDIR3();

            int start = 1;
            short end = 10;
            short index = 1;

            while (start != 0)
            {
                short _ret = Focas1.cnc_rdprogdir3(handle, 2, ref start, ref end, dir1);

                if (_ret == Focas1.EW_OK)
                {
                    listdir[0] = dir1.dir1;
                    listdir[1] = dir1.dir2;
                    listdir[2] = dir1.dir3;
                    listdir[3] = dir1.dir4;
                    listdir[4] = dir1.dir5;
                    listdir[5] = dir1.dir6;
                    listdir[6] = dir1.dir7;
                    listdir[7] = dir1.dir8;
                    listdir[8] = dir1.dir9;
                    listdir[9] = dir1.dir10;
                    start = listdir[9].number;

                    foreach (Focas1.PRGDIR3_data item in listdir)
                    {
                        if (item.number != 0)
                        {
                            int progname = item.number;
                            int size = item.length;
                            string mdata = string.Concat(item.mdate.year, "-",
                                                        item.mdate.month.ToString("00"), "-",
                                                        item.mdate.day.ToString("00"), " ",
                                                        item.mdate.hour.ToString("00"), ":",
                                                        item.mdate.minute.ToString("00"));

                            if (!proglist.Any(x => x.PROGRAM == progname.ToString()))
                            {
                                proglist.Add(new ProgramList { NO = index, PROGRAM = progname.ToString(), SIZE = size, MDATE = mdata });
                                index++;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return proglist;
        }, null);
    }

    public async Task<string> GetToolInfo()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.ODBPTLINF tool = new Focas1.ODBPTLINF();
            short _ret = Focas1.cnc_rdtoolinfo(handle, tool);

            if (_ret == Focas1.EW_OK)
            {
                short tld_max = tool.tld_max;
                return tld_max.ToString();
            }
            return "UNAVAILABLE";
        }, "UNAVAILABLE");
    }

    public async Task<TurretInfo> GetToolData()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            TurretInfo turretinfo = new TurretInfo();
            Focas1.IODBTLCTL tool = new Focas1.IODBTLCTL();
            short _ret = Focas1.cnc_rdtlctldata(handle, tool);

            if (_ret == Focas1.EW_OK)
            {
                int[] total_punch = tool.total_punch;
                short turret_index = tool.turret_indx;
                short used_tool = tool.used_tool;
                int t_ax_move = tool.t_axis_move;

                turretinfo.TOTAL_PUNCH = total_punch[0];
                turretinfo.ZERO_POINT = total_punch[1];
                turretinfo.TURRET_INDEX = turret_index;
                turretinfo.USED_TOOL = used_tool;
                turretinfo.T_AX_MOVE = t_ax_move;

                return turretinfo;
            }
            return turretinfo;
        }, null);
    }

    public async Task<string> GetToolCon()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.IODBTLDT tool = new Focas1.IODBTLDT();
            short a = 65;
            short b = 10;

            short _ret = Focas1.cnc_rdtooldata(handle, a, ref b, tool);

            if (_ret == Focas1.EW_OK)
            {
                return ""; // รักษาพฤติกรรมเดิมที่คืนสตริงว่างเมื่อสำเร็จ
            }
            return "UNAVAILABLE";
        }, "UNAVAILABLE");
    }

    public async Task<string> GetToolCon2()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.IODBMLTTL tool = new Focas1.IODBMLTTL();
            short a = 10;
            short b = 10;

            short _ret = Focas1.cnc_rdmultitldt(handle, a, ref b, tool);

            if (_ret == Focas1.EW_OK)
            {
                return "";
            }
            return "UNAVAILABLE";
        }, "UNAVAILABLE");
    }

    public async Task<string> GetSpindle()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.ODBSPN spindle = new Focas1.ODBSPN();
            Focas1.ODBACT2 speed = new Focas1.ODBACT2();
            Focas1.ODBSPN rpm = new Focas1.ODBSPN();
            Focas1.ODBSPDLNAME splname = new Focas1.ODBSPDLNAME();
            Focas1.ODBSVLOAD servo = new Focas1.ODBSVLOAD();
            Focas1.ODBSPN gear = new Focas1.ODBSPN();
            Focas1.ODBSPLOAD spdmeter = new Focas1.ODBSPLOAD();
            Focas1.ODBAXISNAME axname = new Focas1.ODBAXISNAME();

            short a = 1;
            short b = 1;
            short c = 1;
            
            short _ret = Focas1.cnc_rdspload(handle, Focas1.ALL_SPINDLES, spindle);
            _ret = Focas1.cnc_rdsvmeter(handle, ref b, servo);
            _ret = Focas1.cnc_rdspmeter(handle, -1, ref c, spdmeter);
            _ret = Focas1.cnc_rdspgear(handle, Focas1.ALL_SPINDLES, gear);
            _ret = Focas1.cnc_acts2(handle, 1, speed);
            _ret = Focas1.cnc_rdspmaxrpm(handle, 1, rpm);
            _ret = Focas1.cnc_rdspdlname(handle, ref a, splname);
            _ret = Focas1.cnc_rdaxisname(handle, ref a, axname);

            if (_ret == Focas1.EW_OK)
            {
                return "";
            }
            return "";
        }, "");
    }

    public async Task<string> GetAlarmHist()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.ODBAHIS alarmhist = new Focas1.ODBAHIS();
            short _ret = Focas1.cnc_rdalmhistry(handle, 2, 10, 486, alarmhist);

            if (_ret == Focas1.EW_OK)
            {
                return "";
            }
            return "";
        }, "");
    }

    public async Task<string> GetSignal()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.IODBSIG signal = new Focas1.IODBSIG();
            short _ret = Focas1.cnc_rdhissgnl(handle, signal);

            if (_ret == Focas1.EW_OK)
            {
                return "";
            }
            return "";
        }, "");
    }

    public async Task<string> GetCurrentMotor()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            short _ret = Focas1.cnc_rdcurrent(handle, out short current);
            _ret = Focas1.cnc_rdsrvspeed(handle, out int speed);
            _ret = Focas1.cnc_rdloopgain(handle, out int loopg);
            _ret = Focas1.cnc_rdnspdl(handle, out short spdl);

            if (_ret == Focas1.EW_OK)
            {
                return "";
            }
            return "";
        }, "");
    }

    public async Task<double> AbsolutePosition()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.ODBAXIS _axisPositionAbsolute = new Focas1.ODBAXIS();
            short _ret = Focas1.cnc_absolute2(handle, 88, 8, _axisPositionAbsolute);

            if (_ret != Focas1.EW_OK)
                return 0;

            return _axisPositionAbsolute.data[0] / 10000.0;
        }, 0.0);
    }

    public async Task<double> RelativePosition()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.ODBAXIS _axisPositionRelative = new Focas1.ODBAXIS();
            short _ret = Focas1.cnc_relative2(handle, 88, 8, _axisPositionRelative);

            if (_ret != Focas1.EW_OK)
                return 0;

            return _axisPositionRelative.data[0] / 10000.0;
        }, 0.0);
    }

    public async Task<double> MachinePosition()
    {
        return await ExecuteShortLivedCommandAsync((handle) =>
        {
            Focas1.ODBAXIS _axisPositionMachine = new Focas1.ODBAXIS();
            short _ret = Focas1.cnc_machine(handle, 88, 8, _axisPositionMachine);

            if (_ret != Focas1.EW_OK)
                return 0;

            return _axisPositionMachine.data[0] / 10000.0;
        }, 0.0);
    }

    public void Dispose()
    {
        _asyncLock.Dispose();
    }

    // =========================================================================
    // 🌟 ฟังก์ชันการ Mapping สตริงเดิมทั้งหมด คงไว้ตามต้นฉบับดั้งเดิม 100%
    // =========================================================================

    private string CNCTypetoString(string cnctype)
    {
        switch (cnctype)
        {
            case "15": { return "Series 15/15i"; }
            case "16": { return "Series 16/16i"; }
            case "18": { return "Series 18/18i"; }
            case "21": { return "Series 21/21i"; }
            case "30": { return "Series 30i"; }
            case "31": { return "Series 31i"; }
            case "32": { return "Series 32i"; }
            case "35": { return "Series 35i"; }
            case " 0": { return "Series 0i"; }
            case "PD": { return "Power Mate i-D"; }
            case "PH": { return "Power Mate i-H"; }
            case "PM": { return "Power Motion i"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string MTTypetoString(string mttype)
    {
        switch (mttype)
        {
            case " M": { return "Machining center"; }
            case " T": { return "Lathe"; }
            case "MM": { return "M series with 2 path control"; }
            case "TT": { return "T series with 2/3 path control"; }
            case "MT": { return "T series with compound machining function"; }
            case " P": { return "Punch press"; }
            case " L": { return "Laser"; }
            case " W": { return "Wire cut"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string ModeNumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "MDI"; }
            case 1: { return "MEM"; }
            case 2: { return "***"; }
            case 3: { return "EDIT"; }
            case 4: { return "HND"; }
            case 5: { return "JOG"; }
            case 6: { return "Teach in JOG"; }
            case 7: { return "Teach in HND"; }
            case 8: { return "INC"; }
            case 9: { return "REF"; }
            case 10: { return "RMT"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string RunNumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "***"; }
            case 1: { return "STOP"; }
            case 2: { return "HOLD"; }
            case 3: { return "START"; }
            case 4: { return "MSTR"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string MotionToString(int num)
    {
        switch (num)
        {
            case 0: { return "***"; }
            case 1: { return "MOTION"; }
            case 2: { return "DWLL"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string MstbToString(int num)
    {
        switch (num)
        {
            case 0: { return "***"; }
            case 1: { return "FIN"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string AlarmNumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "***"; }
            case 1: { return "ALARM"; }
            case 2: { return "BATTERY LOW"; }
            case 3: { return "FAN SERVO"; }
            case 4: { return "PS WARNING"; }
            case 5: { return "FSSB WARNING"; }
            case 6: { return "INSULATE WARNING"; }
            case 7: { return "ENCODER WARNING"; }
            case 8: { return "PMC ALARM"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string Alarm2NumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "P/S100"; }
            case 1: { return "P/S000"; }
            case 2: { return "P/S101"; }
            case 3: { return "P/S alarm except above"; }
            case 4: { return "Overtravel alarm"; }
            case 5: { return "Overheat alarm"; }
            case 6: { return "Servo alarm"; }
            case 7: { return "System alarm"; }
            case 8: { return "APC alarm"; }
            case 9: { return "Spindle alarm"; }
            case 10: { return "P/S alarm(No.5000,..), Punchpress alarm"; }
            case 11: { return "Laser alarm"; }
            case 12: { return "*** (Not used)"; }
            case 13: { return "Rigid tap alarm"; }
            case 14: { return "*** (Not used)"; }
            case 15: { return "External alarm message"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string EmerNumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "NO EMER"; }
            case 1: { return "EMERGENCY"; }
            case 2: { return "RESET"; }
            case 3: { return "WAIT"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string TMModeNumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "T"; }
            case 1: { return "M"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string StatusNumberToString(int num)
    {
        switch (num)
        {
            case 0: { return "****"; }
            case 1: { return "STOP"; }
            case 2: { return "HOLD"; }
            case 3: { return "STRT"; }
            case 4: { return "MSTR"; }
            default: { return "UNAVAILABLE"; }
        }
    }
    private string EditNumberToString(short mode, short num)
    {
        string output = "";
        if (mode == 0) // edit in T series
        {
            switch (num)
            {
                case 0: { output = "****"; break; }
                case 1: { output = "EDIT"; break; }
                case 2: { output = "SEARCH"; break; }
                case 3: { output = "OUTPUT"; break; }
                case 4: { output = "INPUT"; break; }
                case 5: { output = "COMPARE"; break; }
                case 6: { output = "LABEL_SKIP"; break; }
                case 7: { output = "RESTART"; break; }
                case 8: { output = "HPCC"; break; }
                case 9: { output = "PTRR"; break; }
                case 10: { output = "RVRS"; break; }
                case 11: { output = "RTRY"; break; }
                case 12: { output = "RVED"; break; }
                case 13: { output = "HANDLE"; break; }
                case 14: { output = "OFFSET"; break; }
                case 15: { output = "WORK_OFFSET"; break; }
                case 16: { output = "AICC"; break; }
                case 17: { output = "MEMORY_CHECK"; break; }
                case 18: { output = "CUSTOMER_BOARD"; break; }
                case 19: { output = "SAVE"; break; }
                case 20: { output = "AI NANO"; break; }
                case 21: { output = "AI APC"; break; }
                case 22: { output = "MBL APC"; break; }
                case 23: { output = "NANO HP"; break; }
                case 24: { output = "AI HPCC"; break; }
                case 25: { output = "5_AXIS"; break; }
                case 26: { output = "LEN"; break; }
                case 27: { output = "RAD"; break; }
                case 28: { output = "WZR"; break; }
                case 39: { output = "TCP"; break; }
                case 40: { output = "TWP"; break; }
                case 41: { output = "TCP_TWP"; break; }
                case 42: { output = "APC"; break; }
                case 43: { output = "PRG_CHK"; break; }
                case 44: { output = "APC"; break; }
                case 45: { output = "S_TCP"; break; }
                case 46: { output = "AICC2"; break; }
                case 59: { output = "ALLSAVE"; break; }
                case 60: { output = "NOTSAVE"; break; }
                default: { output = "UNAVAILABLE"; break; }
            }
        }
        else if (mode == 1) // edit in M series
        {
            switch (num)
            {
                case 0: { output = "****"; break; }
                case 1: { output = "EDIT"; break; }
                case 2: { output = "SEARCH"; break; }
                case 3: { output = "OUTPUT"; break; }
                case 4: { output = "INPUT"; break; }
                case 5: { output = "COMPARE"; break; }
                case 6: { output = "LABEL_SKIP"; break; }
                case 7: { output = "OFFSET"; break; }
                case 8: { output = "WORK_SHIFT"; break; }
                case 9: { output = "RESTART"; break; }
                case 10: { output = "RVRS"; break; }
                case 11: { output = "RTRY"; break; }
                case 12: { output = "RVED"; break; }
                case 13: { output = "***"; break; }
                case 14: { output = "PTRR"; break; }
                case 15: { output = "***"; break; }
                case 16: { output = "AICC"; break; }
                case 17: { output = "MEMORY_CHECK"; break; }
                case 18: { output = "***"; break; }
                case 19: { output = "SAVE"; break; }
                case 20: { output = "AI NANO"; break; }
                case 21: { output = "HPCC"; break; }
                case 22: { output = "***"; break; }
                case 23: { output = "NANO HP"; break; }
                case 24: { output = "AI HPCC"; break; }
                case 25: { output = "5_AXIS"; break; }
                case 26: { output = "OFSX"; break; }
                case 27: { output = "OFSZ"; break; }
                case 28: { output = "WZR"; break; }
                case 29: { output = "OFSY"; break; }
                case 31: { output = "TOFS"; break; }
                case 39: { output = "TCP"; break; }
                case 40: { output = "TWP"; break; }
                case 41: { output = "TCP_TWP"; break; }
                case 42: { output = "APC"; break; }
                case 43: { output = "PRG_CHK"; break; }
                case 44: { output = "APC"; break; }
                case 45: { output = "S_TCP"; break; }
                case 59: { output = "ALLSAVE"; break; }
                case 60: { output = "NOTSAVE"; break; }
                default: { output = "UNAVAILABLE"; break; }
            }
        }
        return output;
    }
}