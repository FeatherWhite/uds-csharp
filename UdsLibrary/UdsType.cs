using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
    public enum DiagnosticRequestType
    {
        DIAGNOSTIC_REQUEST_TYPE_PID,
        DIAGNOSTIC_REQUEST_TYPE_DTC,
        DIAGNOSTIC_REQUEST_TYPE_MIL_STATUS,
        DIAGNOSTIC_REQUEST_TYPE_VIN
    }
    public class DiagnosticRequest
    {
        public uint ArbitrationId { get; set; }
        public byte Mode { get; set; }
        public bool HasPid { get; set; }
        public ushort Pid { get; set; }
        public byte PidLength { get; set; }
        public byte[] Payload { get; set; } = new byte[UdsConfig.MAX_UDS_REQUEST_PAYLOAD_LENGTH];
        public byte PayloadLength { get; set; }
        public bool NoFramePadding { get; set; }
        public DiagnosticRequestType Type { get; set; }
    }
    public enum DiagnosticNegativeResponseCode
    {
        NRC_SUCCESS = 0x0,
        NRC_SERVICE_NOT_SUPPORTED = 0x11,
        NRC_SUB_FUNCTION_NOT_SUPPORTED = 0x12,
        NRC_INCORRECT_LENGTH_OR_FORMAT = 0x13,
        NRC_CONDITIONS_NOT_CORRECT = 0x22,
        NRC_REQUEST_OUT_OF_RANGE = 0x31,
        NRC_SECURITY_ACCESS_DENIED = 0x33,
        NRC_INVALID_KEY = 0x35,
        NRC_TOO_MANY_ATTEMPS = 0x36,
        NRC_TIME_DELAY_NOT_EXPIRED = 0x37,
        NRC_RESPONSE_PENDING = 0x78
    }
    public class DiagnosticResponse 
    {
        public bool Completed { get; set; }
        public bool Success {get;set;}
        public bool MultiFrame {get;set;}
        public uint ArbitrationId {get;set;}
        public byte Mode {get;set;}
        public bool HasPid {get;set;}
        public ushort Pid {get;set;}
        public DiagnosticNegativeResponseCode NegativeResponseCode {get;set;}
        public byte[] payload { get; set; } = new byte[UdsConfig.MAX_UDS_RESPONSE_PAYLOAD_LENGTH];
        public byte payload_length {get;set;}
    }
    public enum DiagnosticMode
    {
        OBD2_MODE_POWERTRAIN_DIAGNOSTIC_REQUEST = 0x1,
        OBD2_MODE_POWERTRAIN_FREEZE_FRAME_REQUEST = 0x2,
        OBD2_MODE_EMISSIONS_DTC_REQUEST = 0x3,
        OBD2_MODE_EMISSIONS_DTC_CLEAR = 0x4,
        // 0x5 is for non-CAN only
        // OBD2_MODE_OXYGEN_SENSOR_TEST = 0x5,
        OBD2_MODE_TEST_RESULTS = 0x6,
        OBD2_MODE_DRIVE_CYCLE_DTC_REQUEST = 0x7,
        OBD2_MODE_CONTROL = 0x8,
        OBD2_MODE_VEHICLE_INFORMATION = 0x9,
        OBD2_MODE_PERMANENT_DTC_REQUEST = 0xa,
        // this one isn't technically in uds, but both of the enhanced standards
        // have their PID requests at 0x22
        OBD2_MODE_ENHANCED_DIAGNOSTIC_REQUEST = 0x22
    }
    public class DiagnosticRequestLink
    {
        public DiagnosticRequest Request { get; set; }
        public bool Success { get; set; }
        public bool Completed { get; set; }
    }
    public enum DiagnosticPidRequestType
    {
        DIAGNOSTIC_STANDARD_PID,
        DIAGNOSTIC_ENHANCED_PID
    }
}
