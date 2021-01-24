﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace Glimmr.Models.Util {
	public class DreamUtil {

        private readonly UdpClient _udpClient;
        private IPEndPoint _broadcastAddress;

        public DreamUtil(UdpClient udp) {
            _udpClient = udp;
            _broadcastAddress = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 8888);
        }
        
		 public void SendSectors(List<Color> sectors, string id, int group) {
            if (sectors == null) throw new InvalidEnumArgumentException("Invalid sector list.");
            const byte flag = 0x3D;
            const byte c1 = 0x03;
            const byte c2 = 0x16;
            var p = new List<byte>();
            foreach (var col in sectors) {
                p.Add(ByteUtils.IntByte(col.R));
                p.Add(ByteUtils.IntByte(col.G));
                p.Add(ByteUtils.IntByte(col.B));
            }
            var ep = new IPEndPoint(IPAddress.Parse(id), 8888);
            SendUdpWrite(c1, c2, p.ToArray(), flag, (byte) group, ep);
        }

         public void SendBroadcastMessage(int groupId) {
             SendUdpWrite(0x01, 0x0C, new byte[] {0x01}, 0x30, (byte) groupId);
         }
        
        public void SetAmbientColor(Color color, string id, int group) {
            if (color == null) throw new InvalidEnumArgumentException("Invalid sector list.");
            byte flag = 0x11;
            byte c1 = 0x03;
            byte c2 = 0x05;
            var p = new List<byte>();
            p.Add(ByteUtils.IntByte(color.R));
            p.Add(ByteUtils.IntByte(color.G));
            p.Add(ByteUtils.IntByte(color.B));
            var ep = new IPEndPoint(IPAddress.Parse(id), 8888);
            SendUdpWrite(c1, c2, p.ToArray(), flag, (byte) group, ep);
        }
        public void SendMessage(string command, dynamic value, string id) {
            var dev = DataUtil.GetDreamDevice(id);
            byte flag = 0x11;
            byte c1 = 0x03;
            byte c2 = 0x00;
            int v;
            var send = false;
            var payload = Array.Empty<byte>();
            var cFlags = MsgUtils.CommandBytes[command];
            if (cFlags != null) {
                c1 = cFlags[0];
                c2 = cFlags[1];
            }
            switch (command) {
                case "saturation":
                    c2 = 0x06;
                    payload = ByteUtils.StringBytes(value);
                    send = true;
                    break;
                case "minimumLuminosity":
                    c2 = 0x0C;
                    v = int.Parse(value);
                    payload = new[] {ByteUtils.IntByte(v), ByteUtils.IntByte(v), ByteUtils.IntByte(v)};
                    send = true;
                    break;
                case "ambientModeType":
                    if (cFlags != null) {
                        payload = new[] {ByteUtils.IntByte((int)value)};
                        c1 = cFlags[0];
                        c2 = cFlags[1];
                        send = true;
                    }
                    break;
                case "ambientScene":
                    if (cFlags != null) {
                        payload = new[] {ByteUtils.IntByte((int)value)};
                        c1 = cFlags[0];
                        c2 = cFlags[1];
                        send = true;
                    }
                    break;
                case "mode":
                    if (cFlags != null) {
                        payload = new[] {(byte)value};
                        c1 = cFlags[0];
                        c2 = cFlags[1];
                        send = true;
                    }
                    break;
            }

            if (send) {
                var ep = new IPEndPoint(IPAddress.Parse(dev.IpAddress), 8888);
                SendUdpWrite(c1, c2, payload, flag, (byte) dev.DeviceGroup, ep);
            }
        }

        public void SendUdpWrite(byte command1, byte command2, byte[] payload, byte flag = 17, byte group = 0,
            IPEndPoint ep = null) {
            if (payload is null) throw new ArgumentNullException(nameof(payload));
            // If we don't specify an endpoint...talk to self
            // Magic header
            // Payload length
            // Group number
            // Flag, should be 0x10 for subscription, 17 for everything else
            // Upper command
            // Lower command

            var msg = new List<byte> {
                0xFC,
                (byte) (payload.Length + 5),
                group,
                flag,
                command1,
                command2
            };
            // Payload
            msg.AddRange(payload);
            // CRC
            msg.Add(MsgUtils.CalculateCrc(msg.ToArray()));
            SendUdpMessage(msg.ToArray(), ep);
        }

        public void SendUdpMessage(byte[] data, IPEndPoint ep = null) {
            try {
                if (ep == null) {
                    _udpClient.SendAsync(data, data.Length, _broadcastAddress);
                } else {
                    _udpClient.SendAsync(data, data.Length, ep);
                }
            } catch (SocketException e) {
                Log.Warning("Socket exception: ", e);
            }
        }
    }
}