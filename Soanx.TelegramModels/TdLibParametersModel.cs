using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.TelegramModels;
public class TdLibParametersModel {
    public required int ApiId { get; set; }
    public required string ApiHash { get; set; }
    public string PhoneNumber { get; set; }
    public required string ApplicationVersion { get; set; }
    public required string DeviceModel { get; set; }
    public required string SystemLanguageCode { get; set; }
    public required string DatabaseDirectory { get; set; }
    public required string FilesDirectory { get; set; }
}