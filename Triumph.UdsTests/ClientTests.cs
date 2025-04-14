using Microsoft.VisualStudio.TestTools.UnitTesting;
using Triumph.Uds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLG.CAN;
using System.Threading;
using System.Runtime.InteropServices;

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
            client.Tp.Init(0x782, 0x78A, 0xFFFFFFFF, 0x7DF);

        }
        [TestMethod]
        public void Test0x22UnpackRDBIResponse()
        {
            ushort[] did_list = { 0xF18C };
            client.UDSSendRDBI(did_list, (ushort)did_list.Length);
            Thread.Sleep(100);
            UDSErr_t err = new UDSErr_t();
            while (client.State != Client.STATE_IDLE)
            {
                err = client.Poll();
            }
            Assert.AreEqual(19, client.RecvSize);
            Assert.AreEqual("62-F1-8C-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF"
                , BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize));
            Assert.AreEqual(UDSErr_t.UDS_OK, err);
            UDSRDBIVar<RDBITestModel>[] rdbi =
            {
                new UDSRDBIVar<RDBITestModel>()
                {
                    DID = 0xF18C,
                    Len = (ushort)Marshal.SizeOf(typeof(RDBITestModel)),
                    Unpack = Unpack,
                    Data = new byte[Marshal.SizeOf(typeof(RDBITestModel))]
                }
            };
            var res = client.UDSUnpackRDBIResponse(rdbi, 1);

            Assert.AreEqual(UDSErr_t.UDS_OK, res);
            for (int i = 0; i < rdbi[0].Len; i++)
            {
                Assert.AreEqual(0xff, rdbi[0].Data[i]);
            }
            Assert.AreEqual(0xffffffff, rdbi[0].Value.one);
            Assert.AreEqual(0xffffffff, rdbi[0].Value.two);
            Assert.AreEqual(0xffffffff, rdbi[0].Value.three);
            Assert.AreEqual(0xffffffff, rdbi[0].Value.four);
        }

        private void Unpack(byte[] target, byte[] source, int offset, int len, ref RDBITestModel res)
        {
            Array.Copy(source, offset, target, 0, len);
            res = Marshal.PtrToStructure<RDBITestModel>(Marshal.UnsafeAddrOfPinnedArrayElement(target, 0));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RDBITestModel
        {
            public uint one;
            public uint two;
            public uint three;
            public uint four;
        }

        [TestMethod]
        public void Test0x22UnpackRDBINegativeResponse()
        {
            ushort[] did_list = { 0xF18D };
            client.UDSSendRDBI(did_list, (ushort)did_list.Length);
            Thread.Sleep(100);
            UDSErr_t err = new UDSErr_t();
            while (client.State != Client.STATE_IDLE)
            {
                err = client.Poll();
            }
            Assert.AreEqual(3, client.RecvSize);
            Assert.AreEqual("7F-22-31"
                , BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize));
            Assert.AreEqual(UDSErr_t.UDS_NRC_RequestOutOfRange, err);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            can.Close();
        }

        [TestMethod()]
        public void Test0x10UDSSendDiagSessCtrl()
        {
            client.UDSSendDiagSessCtrl(0x02);
            Thread.Sleep(100);
            UDSErr_t err = new UDSErr_t();
            while (client.State != Client.STATE_IDLE)
            {
                err = client.Poll();
            }
            Assert.AreEqual(6, client.RecvSize);
            Assert.AreEqual("50-02-00-32-01-F4"
                , BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize));
            Assert.AreEqual(UDSErr_t.UDS_OK, err);
        }
        [TestMethod()]
        public void Test0x2eUDSSendWDBI()
        {
            byte[] payload = [ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 ];
            client.UDSSendWDBI(0xF184, payload);
            Thread.Sleep(100);
            UDSErr_t err = new UDSErr_t();
            while (client.State != Client.STATE_IDLE)
            {
                err = client.Poll();
            }
            Assert.AreEqual(3, client.RecvSize);
            Assert.AreEqual("6E-F1-84"
                , BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize));
            Assert.AreEqual(UDSErr_t.UDS_OK, err);
        }
    }
}