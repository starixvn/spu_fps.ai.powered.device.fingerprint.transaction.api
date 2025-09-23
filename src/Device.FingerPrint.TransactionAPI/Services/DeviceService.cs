using Device.FingerPrint.Model;
using Device.FingerPrint.Models;
using Device.FingerPrint.TransactionAPI.Helpers;
using Fanex.Logging;
using System.Text.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IDeviceService
    {
        DeviceInfoModel GetDeviceInfo(string deviceCode, MessageQueue message);
        DeviceInfoModel GetDeviceInfo(string deviceCode);
    }

    public class DeviceService : IDeviceService
    {
        private readonly ILogger _logger;

        public DeviceService(ILogger logger)
        {
            _logger = logger;
        }

        public DeviceInfoModel GetDeviceInfo(string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode) || deviceCode.ToLower(System.Globalization.CultureInfo.CurrentCulture) == "null")
            {
                return TransformEmptyDeviceInfo();
            }

            var deviceInfo = DecodeAndDeserializeDeviceInfo(deviceCode);

            return TransformDeviceInfo(deviceInfo);
        }

        public DeviceInfoModel GetDeviceInfo(string deviceCode, MessageQueue message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceCode) || deviceCode.ToLower(System.Globalization.CultureInfo.CurrentCulture) == "null")
                {
                    return TransformEmptyDeviceInfo();
                }

                var deviceInfo = DecodeAndDeserializeDeviceInfo(deviceCode);

                if (deviceInfo != null)
                {
                    return TransformDeviceInfo(deviceInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Consumer GetDeviceInfo: " + ex.Message + "\n" + JsonSerializer.Serialize(message));
            }

            return TransformEmptyDeviceInfo();
        }

        private DeviceInfo DecodeAndDeserializeDeviceInfo(string deviceInfo)
        {
            if (string.IsNullOrEmpty(deviceInfo))
            {
                return GenerateEmptyDeviceInfo();
            }

            string value = Base64.NormalDecode(deviceInfo);

            return JsonSerializer.Deserialize<DeviceInfo>(value);
        }

        private static DeviceInfo GenerateEmptyDeviceInfo()
        {
            return new DeviceInfo
            {
                DeviceCode = string.Empty,
                AppVersion = string.Empty,
                TimeZone = string.Empty,
                UserAgent = string.Empty,
                DeviceId = "N/A",
                Screen = new DeviceScreen(),
                HRef = string.Empty,
                BotD = new BotD()
            };
        }

        private DeviceInfoModel TransformDeviceInfo(DeviceInfo deviceInfo)
        {
            var botD = new BotDModel();
            try
            {
                if (deviceInfo.BotD?.Result != null)
                {
                    botD = new BotDModel
                    {
                        Result = new BotDResultModel
                        {
                            Bot = deviceInfo.BotD.Result.Bot,
                            BotKind = deviceInfo.BotD.Result.BotKind
                        },
                        Detection = deviceInfo.BotD.Detection,
                        Component = deviceInfo.BotD.Component
                    };
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Consumer TransformDeviceInfo: " + ex.Message + "\n" + JsonSerializer.Serialize(deviceInfo));
            }

            return new DeviceInfoModel
            {
                AppVersion = deviceInfo.AppVersion,
                Version = deviceInfo.Version,
                CapturedDate = deviceInfo.CapturedDate ?? DateTimeOffset.Now.Ticks,
                DeviceFPCode = deviceInfo.DeviceCode,
                DeviceNo = deviceInfo.DeviceId,
                HRef = deviceInfo.HRef,
                NA = deviceInfo.NA,
                TimeZone = deviceInfo.TimeZone,
                UserAgent = deviceInfo.UserAgent,
                Screen = deviceInfo.Screen == null
                    ? new ScreenInfoModel
                    {
                        ColorDepth = 0,
                        Height = 0,
                        Width = 0
                    }
                    : new ScreenInfoModel
                    {
                        ColorDepth = deviceInfo.Screen.ColorDepth,
                        Height = deviceInfo.Screen.Height,
                        Width = deviceInfo.Screen.Width
                    },
                BotD = botD
            };
        }

        private DeviceInfoModel TransformEmptyDeviceInfo()
        {
            return new DeviceInfoModel
            {
                AppVersion = string.Empty,
                CapturedDate = 0,
                DeviceFPCode = string.Empty,
                DeviceNo = string.Empty,
                HRef = string.Empty,
                NA = string.Empty,
                Screen = new ScreenInfoModel
                {
                    ColorDepth = 0,
                    Height = 0,
                    Width = 0
                },
                TimeZone = string.Empty,
                UserAgent = string.Empty,
                InvalidDeviceId = true
            };
        }
    }
}
