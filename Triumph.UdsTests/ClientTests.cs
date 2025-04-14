using Microsoft.VisualStudio.TestTools.UnitTesting;
using Triumph.Uds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLG.CAN;
using System.Threading;

namespace Triumph.Uds.Tests
{
    [TestClass()]
    public class ClientTests
    {
        Client client;
        USBCanIICommunication can = new();
        [TestInitialize]
        public void TestInitialize()
        {
            can.SetPara(new ZLGCANPara()
            {
                deviceIndex = 0,
                deviceInfoIndex = ZLG.CAN.Models.DeviceInfoIndex.ZCAN_USBCAN2,
                kBaudrates = [ZLG.CAN.Models.KBaudrate._500kbps, ZLG.CAN.Models.KBaudrate._500kbps],
                frameType = [ZLG.CAN.Models.FrameType.Standard, ZLG.CAN.Models.FrameType.Standard]
            });
            can.Open();
            client = new Client();
            client.Init();
            client.Tp = new IsoTpZLGUSBCANII(can);
            client.Tp.Init(0x782, 0x78A,0xFFFFFFFF, 0x7DF);

        }
        [TestMethod]
        public void Test0x22UnpackRDBIResponse()
        {
            ushort[] did_list = { 0xF18C };
            client.UDSSendRDBI(did_list, 1);
            Thread.Sleep(100);
            while (client.Poll() == UDSErr_t.UDS_OK)
            {
                if(client.State == Client.STATE_IDLE)
                {
                    break;
                }
            }
            Assert.AreEqual(19, client.RecvSize);
            Assert.AreEqual("62-F1-8C-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF"
                , BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize));
            //Console.WriteLine($"接收数据为:{BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize)}");
        }
        [TestCleanup]
        public void TestCleanup()
        {
            can.Close();
        }
    }
}