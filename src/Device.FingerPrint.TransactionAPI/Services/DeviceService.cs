using Device.FingerPrint.Model;
using Device.FingerPrint.TransactionAPI.Helpers;
using Device.FingerPrint.TransactionAPI.Model;
using Fanex.Logging;
using Newtonsoft.Json;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface IDeviceService
    {
        DeviceInfoModel GetDeviceInfo(string deviceCode);

        BotDModel TransformBotdModel(string rawbotD);
    }

    public class DeviceService : IDeviceService
    {
        private const int MaxVersionLength = 10;

        public DeviceInfoModel GetDeviceInfo(string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode) || deviceCode.ToLower(System.Globalization.CultureInfo.CurrentCulture) == "null")
            {
                return TransformEmptyDeviceInfo();
            }

            var deviceInfo = DecodeAndDeserializeDeviceInfo(deviceCode);

            return TransformDeviceInfo(deviceInfo);
        }

        private DeviceInfo DecodeAndDeserializeDeviceInfo(string deviceInfo)
        {
            var value = deviceInfo.StartsWith("eyJ") ? Base64.NormalDecode(deviceInfo) : Base64.Decode(deviceInfo);

            return JsonConvert.DeserializeObject<DeviceInfo>(value, JsonSerializerSettingsFactory.Instance);
        }

        public BotDModel TransformBotdModel(string rawbotD)
        {
            if (!string.IsNullOrEmpty(rawbotD))
            {
                try
                {
                    var botD = rawbotD.StartsWith("eyJ") ? Base64.NormalDecode(rawbotD) : Base64.Decode(rawbotD);

                    if (!string.IsNullOrEmpty(botD) && Utilities.IsValidJson(botD))
                    {
                        return JsonConvert.DeserializeObject<BotDModel>(botD);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Info("Error TransformBotdModel: " + ex.Message + "\n" + rawbotD);
                }
            }

            return new BotDModel();
        }

        private DeviceInfoModel TransformDeviceInfo(DeviceInfo deviceInfo)
        {
            var version = TransformVersion(deviceInfo);
            var isNewFlow = false;

            if (!string.IsNullOrEmpty(version))
            {
                var majorVersion = version.Split('.').FirstOrDefault();
                if (int.TryParse(majorVersion, out int ver) && ver >= 3)
                {
                    isNewFlow = true;
                }
            }

            return new DeviceInfoModel
            {
                AppVersion = deviceInfo.AppVersion,
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
                Tagging = deviceInfo.Tagging,
                F4 = deviceInfo.F4,
                F5 = deviceInfo.F5,
                Rtc = deviceInfo.Rtc,
                SessionKey = deviceInfo.SessionKey,
                FPVersion = deviceInfo.FPVersion,
                TaggingType = deviceInfo.TaggingType,
                Version = version,
                IsNewFlow = isNewFlow
            };
        }

        private string TransformVersion(DeviceInfo deviceInfo)
        {
            if(deviceInfo.Version is null && deviceInfo.ScriptVersion is null)
            {
                return "-1";
            }

            var version = deviceInfo.Version ?? deviceInfo.ScriptVersion;

            if(version == string.Empty)
            {
                return "-2";
            }

            if (version.Length > MaxVersionLength)
            {
                return "-3";
            }

            return version;
        }

        private DeviceInfoModel TransformEmptyDeviceInfo()
        {
            return new DeviceInfoModel
            {
                DeviceNo = "N/A",
                AppVersion = string.Empty,
                CapturedDate = 0,
                DeviceFPCode = string.Empty,
                HRef = string.Empty,
                NA = string.Empty,
                Screen = new ScreenInfoModel
                {
                    ColorDepth = 0,
                    Height = 0,
                    Width = 0
                },
                BotD = new BotDModel(),
                TimeZone = string.Empty,
                UserAgent = string.Empty,
                InvalidDeviceId = true,
                Version = "-1"
            };
        }
    }
}
