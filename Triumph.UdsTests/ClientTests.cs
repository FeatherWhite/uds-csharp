﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            while (client.State != Client.STATE_IDLE)
            {
                client.Poll();
            }
            Assert.AreEqual(19, client.RecvSize);
            Assert.AreEqual("62-F1-8C-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF"
                , BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize));
            Console.WriteLine($"接收数据为:{BitConverter.ToString(client.RecvBuffer, 0, client.RecvSize)}");
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