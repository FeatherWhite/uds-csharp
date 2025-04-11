using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
    public class Config
    {
        /** ISO-TP Maximum Transmissiable Unit (ISO-15764-2-2004 section 5.3.3) */
        public const int UDS_ISOTP_MTU = 4095;

        // UDS Transport Protocol Maximum Transmission Unit
        public static readonly int UDS_TP_MTU = UDS_ISOTP_MTU;

        // Buffer sizes
        public static readonly int UDS_SERVER_SEND_BUF_SIZE = UDS_TP_MTU;
        public static readonly int UDS_SERVER_RECV_BUF_SIZE = UDS_TP_MTU;
        public static readonly int UDS_CLIENT_SEND_BUF_SIZE = UDS_TP_MTU;
        public static readonly int UDS_CLIENT_RECV_BUF_SIZE = UDS_TP_MTU;

        // Client default timing values
        public const uint UDS_CLIENT_DEFAULT_P2_MS = 150U;
        public const uint UDS_CLIENT_DEFAULT_P2_STAR_MS = 1500U;
        public const uint UDS_CLIENT_DEFAULT_S3_MS = 2000;

        // Assert conditions (C# does not have static_assert, but we can use exceptions)
        static Config()
        {
            if (UDS_CLIENT_DEFAULT_P2_STAR_MS <= UDS_CLIENT_DEFAULT_P2_MS)
            {
                throw new InvalidOperationException("UDS_CLIENT_DEFAULT_P2_STAR_MS must be greater than UDS_CLIENT_DEFAULT_P2_MS.");
            }
            if (UDS_SERVER_DEFAULT_P2_MS <= 0 ||
                UDS_SERVER_DEFAULT_P2_MS >= UDS_SERVER_DEFAULT_P2_STAR_MS ||
                UDS_SERVER_DEFAULT_P2_STAR_MS >= UDS_SERVER_DEFAULT_S3_MS)
            {
                throw new InvalidOperationException("Invalid timing values for server defaults.");
            }
            if (UDS_SERVER_DEFAULT_POWER_DOWN_TIME_MS < UDS_SERVER_DEFAULT_P2_MS)
            {
                throw new InvalidOperationException("The server shall have adequate time to respond before reset.");
            }
        }

        // Server default timing values
        public const int UDS_SERVER_DEFAULT_P2_MS = 50;
        public const int UDS_SERVER_DEFAULT_P2_STAR_MS = 5000;
        public const int UDS_SERVER_DEFAULT_S3_MS = 5100;


        // Duration for power down time
        public const int UDS_SERVER_DEFAULT_POWER_DOWN_TIME_MS = 60;

        // Ensure power down time is adequate


        // Delays for brute force mitigation
        public const int UDS_SERVER_0x27_BRUTE_FORCE_MITIGATION_BOOT_DELAY_MS = 1000;
        public const int UDS_SERVER_0x27_BRUTE_FORCE_MITIGATION_AUTH_FAIL_DELAY_MS = 1000;

        // Transfer data max block length
        public static readonly int UDS_SERVER_DEFAULT_XFER_DATA_MAX_BLOCKLENGTH = UDS_TP_MTU;

        // Custom millis
        public const int UDS_CUSTOM_MILLIS = 0;
    }
}
