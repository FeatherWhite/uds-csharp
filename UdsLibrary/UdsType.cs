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
        public byte[] Payload { get; set; }
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
        public byte[] payload { get; set; }
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

    public enum UDSDiagnosticServiceId : byte
    {
        kSID_DIAGNOSTIC_SESSION_CONTROL = 0x10,
        kSID_ECU_RESET = 0x11,
        kSID_CLEAR_DIAGNOSTIC_INFORMATION = 0x14,
        kSID_READ_DTC_INFORMATION = 0x19,
        kSID_READ_DATA_BY_IDENTIFIER = 0x22,
        kSID_READ_DATA_BY_IDENTIFIERCustomization = 0x80,
        kSID_READ_MEMORY_BY_ADDRESS = 0x23,
        kSID_READ_SCALING_DATA_BY_IDENTIFIER = 0x24,
        kSID_SECURITY_ACCESS = 0x27,
        kSID_COMMUNICATION_CONTROL = 0x28,
        kSID_READ_PERIODIC_DATA_BY_IDENTIFIER = 0x2A,
        kSID_DYNAMICALLY_DEFINE_DATA_IDENTIFIER = 0x2C,
        kSID_WRITE_DATA_BY_IDENTIFIER = 0x2E,
        kSID_INPUT_CONTROL_BY_IDENTIFIER = 0x2F,
        kSID_ROUTINE_CONTROL = 0x31,
        kSID_REQUEST_DOWNLOAD = 0x34,
        kSID_REQUEST_UPLOAD = 0x35,
        kSID_TRANSFER_DATA = 0x36,
        kSID_REQUEST_TRANSFER_EXIT = 0x37,
        kSID_REQUEST_FILE_TRANSFER = 0x38,
        kSID_WRITE_MEMORY_BY_ADDRESS = 0x3D,
        kSID_TESTER_PRESENT = 0x3E,
        kSID_ACCESS_TIMING_PARAMETER = 0x83,
        kSID_SECURED_DATA_TRANSMISSION = 0x84,
        kSID_CONTROL_DTC_SETTING = 0x85,
        kSID_RESPONSE_ON_EVENT = 0x86,
    };

    public class DiagnosticRequestLink
    {
        public DiagnosticRequest Request { get; set; }
        public DiagnosticResponse Response { get; set; }
        public ushort RecvSize { get; set; }
        public ushort SendSize { get; set; }

        public byte State { get; set; }
        public byte[] SendBuffer { get; set; }
        public byte[] RecvBuffer { get; set; }

        public bool Success { get; set; }
        public bool Completed { get; set; }
    }
    public enum DiagnosticPidRequestType
    {
        DIAGNOSTIC_STANDARD_PID,
        DIAGNOSTIC_ENHANCED_PID
    }
    public enum UDSErr_t
    {
        UDS_FAIL = -1, // 通用错误
        UDS_OK = 0,    // 成功

        // Negative Response Codes (NRCs) as defined in ISO14229-1:2020 Table A.1 - Negative Response
        // Code (NRC) definition and values
        UDS_PositiveResponse = 0,
        // 0x01 to 0x0F are reserved by ISO14229-1:2020
        UDS_NRC_GeneralReject = 0x10,
        UDS_NRC_ServiceNotSupported = 0x11,
        UDS_NRC_SubFunctionNotSupported = 0x12,
        UDS_NRC_IncorrectMessageLengthOrInvalidFormat = 0x13,
        UDS_NRC_ResponseTooLong = 0x14,
        // 0x15 to 0x20 are reserved by ISO14229-1:2020
        UDS_NRC_BusyRepeatRequest = 0x21,
        UDS_NRC_ConditionsNotCorrect = 0x22,
        UDS_NRC_RequestSequenceError = 0x24,
        UDS_NRC_NoResponseFromSubnetComponent = 0x25,
        UDS_NRC_FailurePreventsExecutionOfRequestedAction = 0x26,
        // 0x27 to 0x30 are reserved by ISO14229-1:2020
        UDS_NRC_RequestOutOfRange = 0x31,
        // 0x32 is reserved by ISO14229-1:2020
        UDS_NRC_SecurityAccessDenied = 0x33,
        UDS_NRC_AuthenticationRequired = 0x34,
        UDS_NRC_InvalidKey = 0x35,
        UDS_NRC_ExceedNumberOfAttempts = 0x36,
        UDS_NRC_RequiredTimeDelayNotExpired = 0x37,
        UDS_NRC_SecureDataTransmissionRequired = 0x38,
        UDS_NRC_SecureDataTransmissionNotAllowed = 0x39,
        UDS_NRC_SecureDataVerificationFailed = 0x3A,
        // 0x3B to 0x4F are reserved by ISO14229-1:2020
        UDS_NRC_CertficateVerificationFailedInvalidTimePeriod = 0x50,
        UDS_NRC_CertficateVerificationFailedInvalidSignature = 0x51,
        UDS_NRC_CertficateVerificationFailedInvalidChainOfTrust = 0x52,
        UDS_NRC_CertficateVerificationFailedInvalidType = 0x53,
        UDS_NRC_CertficateVerificationFailedInvalidFormat = 0x54,
        UDS_NRC_CertficateVerificationFailedInvalidContent = 0x55,
        UDS_NRC_CertficateVerificationFailedInvalidScope = 0x56,
        UDS_NRC_CertficateVerificationFailedInvalidCertificate = 0x57,
        UDS_NRC_OwnershipVerificationFailed = 0x58,
        UDS_NRC_ChallengeCalculationFailed = 0x59,
        UDS_NRC_SettingAccessRightsFailed = 0x5A,
        UDS_NRC_SessionKeyCreationOrDerivationFailed = 0x5B,
        UDS_NRC_ConfigurationDataUsageFailed = 0x5C,
        UDS_NRC_DeAuthenticationFailed = 0x5D,
        // 0x5E to 0x6F are reserved by ISO14229-1:2020
        UDS_NRC_UploadDownloadNotAccepted = 0x70,
        UDS_NRC_TransferDataSuspended = 0x71,
        UDS_NRC_GeneralProgrammingFailure = 0x72,
        UDS_NRC_WrongBlockSequenceCounter = 0x73,
        // 0x74 to 0x77 are reserved by ISO14229-1:2020
        UDS_NRC_RequestCorrectlyReceived_ResponsePending = 0x78,
        // 0x79 to 0x7D are reserved by ISO14229-1:2020
        UDS_NRC_SubFunctionNotSupportedInActiveSession = 0x7E,
        UDS_NRC_ServiceNotSupportedInActiveSession = 0x7F,
        // 0x80 is reserved by ISO14229-1:2020
        UDS_NRC_RpmTooHigh = 0x81,
        UDS_NRC_RpmTooLow = 0x82,
        UDS_NRC_EngineIsRunning = 0x83,
        UDS_NRC_EngineIsNotRunning = 0x84,
        UDS_NRC_EngineRunTimeTooLow = 0x85,
        UDS_NRC_TemperatureTooHigh = 0x86,
        UDS_NRC_TemperatureTooLow = 0x87,
        UDS_NRC_VehicleSpeedTooHigh = 0x88,
        UDS_NRC_VehicleSpeedTooLow = 0x89,
        UDS_NRC_ThrottlePedalTooHigh = 0x8A,
        UDS_NRC_ThrottlePedalTooLow = 0x8B,
        UDS_NRC_TransmissionRangeNotInNeutral = 0x8C,
        UDS_NRC_TransmissionRangeNotInGear = 0x8D,
        // 0x8E is reserved by ISO14229-1:2020
        UDS_NRC_BrakeSwitchNotClosed = 0x8F,
        UDS_NRC_ShifterLeverNotInPark = 0x90,
        UDS_NRC_TorqueConverterClutchLocked = 0x91,
        UDS_NRC_VoltageTooHigh = 0x92,
        UDS_NRC_VoltageTooLow = 0x93,
        UDS_NRC_ResourceTemporarilyNotAvailable = 0x94,

        /* 0x95 to 0xEF are reservedForSpecificConditionsNotCorrect */
        /* 0xF0 to 0xFE are vehicleManufacturerSpecificConditionsNotCorrect */
        /* 0xFF is ISOSAEReserved */

        // The following values are not defined in ISO14229-1:2020
        UDS_ERR_TIMEOUT = 0x100,      // A request has timed out
        UDS_ERR_DID_MISMATCH,         // The response DID does not match the request DID
        UDS_ERR_SID_MISMATCH,         // The response SID does not match the request SID
        UDS_ERR_SUBFUNCTION_MISMATCH, // The response SubFunction does not match the request SubFunction
        UDS_ERR_TPORT,                // Transport error. Check the transport layer for more information
        UDS_ERR_RESP_TOO_SHORT,       // The response is too short
        UDS_ERR_BUFSIZ,               // The buffer is not large enough
        UDS_ERR_INVALID_ARG,          // The function has been called with invalid arguments
        UDS_ERR_BUSY,                 // The client is busy and cannot process the request
        UDS_ERR_MISUSE,               // The library is used incorrectly
    }
}
