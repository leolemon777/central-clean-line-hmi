using System.Net.Sockets;
using System.Text;
using NModbus;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Servo.Mapping;

namespace PipelineControl.UI.Services.Servo.Drivers;

public sealed class RealServoDriver : IServoDriver, IConfigurableServoDriver, IDisposable
{
    private readonly object syncRoot = new();
    private ServoConnectionOptions options = new();
    private TcpClient? tcpClient;
    private IModbusMaster? master;
    private bool connected;

    public string DriverName => "Real HeChuan Modbus TCP";

    public void Configure(ServoConnectionOptions options)
    {
        this.options = options;
    }

    public ApiResult Connect(string gatewayIp, int gatewayPort)
    {
        lock (syncRoot)
        {
            if (connected)
            {
                return ApiResult.Ok();
            }

            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(gatewayIp, gatewayPort);
                master = new ModbusFactory().CreateMaster(tcpClient);
                master.Transport.ReadTimeout = Math.Max(500, options.ScanCycleMs);
                master.Transport.WriteTimeout = Math.Max(500, options.ScanCycleMs);
                connected = true;
                return ApiResult.Ok($"已连接伺服网关 {gatewayIp}:{gatewayPort}");
            }
            catch (Exception ex)
            {
                DisposeUnlocked();
                connected = false;
                return new ApiResult(-1, $"连接伺服网关失败：{ex.Message}");
            }
        }
    }

    public ApiResult Disconnect()
    {
        lock (syncRoot)
        {
            DisposeUnlocked();
            connected = false;
            return ApiResult.Ok("伺服网关已断开");
        }
    }

    public ApiResult<ServoAxisState> ReadAxis(ServoAxisDefinition axis, ServoRegisterMap registerMap)
    {
        lock (syncRoot)
        {
            if (!connected || master is null)
            {
                return ApiResult<ServoAxisState>.Fail(-1, "ReadAxis");
            }

            var isEnabled = false;
            var targetRpm = 0;

            // 现场资料已确认部分写命令寄存器不能用 FC03 回读。状态轮询不应因此把 TCP 网关连接判为离线。
            if (registerMap.TryGetRegister("ServoOn", out var servoOnRegister))
            {
                TryReadHoldingRegister(axis, servoOnRegister.AddressValue, out var servoOnValue);
                isEnabled = servoOnValue == (ushort)(servoOnRegister.OnValue ?? 2);
            }

            if (registerMap.TryGetRegister("SpeedCommand", out var speedRegister) &&
                TryReadHoldingRegister(axis, speedRegister.AddressValue, out var speedValue))
            {
                targetRpm = unchecked((short)speedValue);
            }

            return ApiResult<ServoAxisState>.Ok(new ServoAxisState(
                axis.Axis,
                axis.Station,
                axis.Name,
                isEnabled,
                IsOnline: true,
                TargetRpm: targetRpm,
                ActualRpm: targetRpm,
                FaultCode: 0));
        }
    }

    public ApiResult WriteServoOn(ServoAxisDefinition axis, ServoRegisterMap registerMap, bool enable)
    {
        lock (syncRoot)
        {
            if (!connected || master is null)
            {
                return ApiResult.Fail(-1, "WriteServoOn");
            }

            if (!registerMap.TryGetRegister("ServoOn", out var register))
            {
                return ApiResult.Fail(7, "未配置 ServoOn 寄存器");
            }

            var value = (ushort)(enable ? (register.OnValue ?? 2) : (register.OffValue ?? 0));
            try
            {
                master.WriteSingleRegister((byte)axis.Station, register.AddressValue, value);
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return new ApiResult(-1, $"{axis.Name} 使能写入失败：{ex.Message}");
            }
        }
    }

    private bool TryReadHoldingRegister(ServoAxisDefinition axis, ushort address, out ushort value)
    {
        value = 0;
        if (!connected || master is null)
        {
            return false;
        }

        try
        {
            var values = master.ReadHoldingRegisters((byte)axis.Station, address, 1);
            if (values.Length == 0)
            {
                return false;
            }

            value = values[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ApiResult WriteSpeed(ServoAxisDefinition axis, ServoRegisterMap registerMap, int rpm)
    {
        lock (syncRoot)
        {
            if (!connected || master is null)
            {
                return ApiResult.Fail(-1, "WriteSpeed");
            }

            if (!registerMap.TryGetRegister("SpeedCommand", out var register))
            {
                return ApiResult.Fail(7, "未配置 SpeedCommand 寄存器");
            }

            var clamped = Math.Clamp(rpm, -options.MaxSpeedRpm, options.MaxSpeedRpm);
            var value = unchecked((ushort)(short)clamped);
            try
            {
                master.WriteSingleRegister((byte)axis.Station, register.AddressValue, value);
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return new ApiResult(-1, $"{axis.Name} 速度写入失败：{ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            DisposeUnlocked();
        }
    }

    private void DisposeUnlocked()
    {
        try
        {
            master = null;
            tcpClient?.Dispose();
        }
        catch
        {
            // 释放过程不抛异常。
        }

        tcpClient = null;
    }

    private static string BuildLoadFailureMessage(Exception exception)
    {
        var builder = new StringBuilder("伺服网关通信失败");
        builder.Append("：").Append(exception.Message);
        return builder.ToString();
    }
}
