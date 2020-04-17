﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HueDream.Models;
using HueDream.Models.DreamGrab;
using HueDream.Models.DreamScreen;
using HueDream.Models.DreamScreen.Devices;
using HueDream.Models.Hue;
using HueDream.Models.Nanoleaf;
using HueDream.Models.Util;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HueDream.Controllers {
    [Route("api/[controller]"), ApiController]
    public class DreamDataController : ControllerBase {
        // GET: api/DreamData/getMode
        [Route("getMode")]
        public static int GetMode() {
            var dev = DreamData.GetDeviceData();
            return dev.Mode;
        }


        // POST: api/DreamData/mode
        [HttpPost("mode")]
        public IActionResult Mode([FromBody] int dMode) {
            SetMode((byte) dMode);
            return Ok(dMode);
        }


        // POST: api/DreamData/capturemode
        [HttpPost("capturemode")]
        public IActionResult CaptureMode([FromBody] int cMode) {
            SetCaptureMode(cMode);
            return Ok(cMode);
        }

        // POST: api/DreamData/devname
        [HttpPost("devname")]
        public IActionResult Devname([FromBody] JObject devInfo) {
            var devIp = (string) devInfo.SelectToken("ip");
            var devName = (string) devInfo.SelectToken("name");
            var devGroup = (int) devInfo.SelectToken("group");
            SetDevName(devIp, devName, devGroup);
            return Ok(devName);
        }

        // POST: api/DreamData/brightness
        [HttpPost("brightness")]
        public IActionResult Brightness([FromBody] int dBright) {
            SetBrightness(dBright);
            return Ok(dBright);
        }

        // POST: api/DreamData/camType
        [HttpPost("camType")]
        public IActionResult CamType([FromBody] int cType) {
            DreamData.SetItem<int>("camType", cType);
            ResetMode();
            return Ok(cType);
        }

        // POST: api/DreamData/vcount
        [HttpPost("vcount")]
        public IActionResult Vcount([FromBody] int count) {
            var ledData = DreamData.GetItem<LedData>("ledData");
            var capMode = DreamData.GetItem<int>("captureMode");
            int hCount;
            if (capMode == 0) {
                hCount = ledData.HCountDs;
                ledData.VCountDs = count;
            } else {
                hCount = ledData.HCount;
                ledData.VCount = count;
            }

            ledData.ledCount = hCount * 2 + count * 2;
            DreamData.SetItem<LedData>("ledData", ledData);
            ResetMode();
            return Ok(count);
        }

        // POST: api/DreamData/hcount
        [HttpPost("hcount")]
        public IActionResult Hcount([FromBody] int count) {
            LedData ledData = DreamData.GetItem<LedData>("ledData");
            var capMode = DreamData.GetItem<int>("captureMode");
            int vCount;
            if (capMode == 0) {
                vCount = ledData.VCountDs;
                ledData.HCountDs = count;
            } else {
                vCount = ledData.VCount;
                ledData.HCount = count;
            }

            ledData.LedCount = vCount * 2 + count * 2;
            DreamData.SetItem<LedData>("ledData", ledData);
            ResetMode();
            return Ok(count);
        }

        // POST: api/DreamData/bridges
        [HttpPost("bridges")]
        public IActionResult PostBridges([FromBody] JArray bData) {
            DreamData.SetItem("bridges", bData);
            ResetMode();
            return Ok();
        }

        // POST: api/DreamData/leaves
        [HttpPost("leaves")]
        public IActionResult PostLeaves([FromBody] List<NanoData> lData) {
            LogUtil.Write(@"Leaf Post received..." + JsonConvert.SerializeObject(lData));
            DreamData.SetItem("leaves", lData);
            ResetMode();
            return Ok(lData.Count);
        }

        // POST: api/DreamData/leaf
        [HttpPost("leaf")]
        public IActionResult PostLeaf([FromBody] NanoData lData) {
            LogUtil.Write(@"Leaf Post received..." + JsonConvert.SerializeObject(lData));
            var leaves = DreamData.GetItem<List<NanoData>>("leaves");
            var leafInt = 0;
            if (leaves.Count > 0) {
                foreach (var leaf in leaves) {
                    if (lData != null)
                        if (leaf.Id == lData.Id) {
                            leaves[leafInt] = lData;
                            break;
                        }

                    leafInt++;
                }
            } else {
                leaves = new List<NanoData>();
                leaves.add(lData);
            }

            DreamData.SetItem("leaves", leaves);
            ResetMode();
            return Ok(lData);
        }

        // POST: api/DreamData/dsIp
        [HttpPost("flipNano")]
        public IActionResult PostIp([FromBody] JObject flipData) {
            var flipDir = (string) flipData.SelectToken("dir");
            var flipVal = (bool) flipData.SelectToken("val");
            var flipDev = (string) flipData.SelectToken("id");
            var leaves = DreamData.GetItem<List<NanoData>>("leaves");
            var newLeaves = new List<NanoData>();
            NanoData nanoTarget = null;
            foreach (NanoData leaf in leaves) {
                if (leaf.Id == flipDev) {
                    if (flipDir == "h") {
                        leaf.MirrorX = flipVal;
                    } else {
                        leaf.MirrorY = flipVal;
                    }

                    nanoTarget = leaf;
                }

                newLeaves.Add(leaf);
            }

            DreamData.SetItem("leaves", newLeaves);
            ResetMode();
            return new JsonResult(nanoTarget);
        }


        // POST: api/DreamData/dsIp
        [HttpPost("dsIp")]
        public IActionResult PostIp([FromBody] string dsIp) {
            LogUtil.Write(@"Did it work? " + dsIp);
            DreamData.SetItem("dsIp", dsIp);
            ResetMode();
            return Ok(dsIp);
        }

        // POST: api/DreamData/dsSidekick
        [HttpPost("dsSidekick")]
        public IActionResult PostSk([FromBody] SideKick skDevice) {
            LogUtil.Write(@"Did it work? " + JsonConvert.SerializeObject(skDevice));
            DreamData.SetItem("myDevice", skDevice);
            return Ok("ok");
        }

        // POST: api/DreamData/dsConnect
        [HttpPost("dsConnect")]
        public IActionResult PostDevice([FromBody] Connect myDevice) {
            LogUtil.Write(@"Did it work? " + JsonConvert.SerializeObject(myDevice));
            DreamData.SetItem("myDevice", myDevice);
            return Ok(myDevice);
        }

        [HttpGet("action")]
        public IActionResult Action(string action, string value = "") {
            var message = "Unrecognized action";
            var exInt = 0;
            LogUtil.Write($@"{action} called from Web API.");
            switch (action) {
                case "loadData":
                    return Content(DreamData.GetStoreSerialized(), "application/json");
                case "refreshDevices":
                    DreamData.RefreshDevices();
                    return Content(DreamData.GetStoreSerialized(), "application/json");
                case "findDreamDevices": {
                    List<BaseDevice> dev = DreamDiscovery.FindDevices();
                    return new JsonResult(dev);
                }
                case "refreshNanoLeaf": {
                    var leaves = DreamData.GetItem<List<NanoData>>("leaves");
                    NanoData nd = null;
                    var leafInt = 0;
                    foreach (NanoData leaf in leaves) {
                        if (leaf.IpV4Address == value) {
                            nd = leaf;
                            break;
                        }

                        leafInt++;
                    }

                    if (nd != null) {
                        var panel = new Panel(nd.IpV4Address, nd.Token);
                        var layout = panel.GetLayout().Result;
                        panel.Dispose();
                        if (layout == null) return new JsonResult(null);
                        nd.Layout = layout;
                        leaves[leafInt] = nd;
                        DreamData.SetItem<List<NanoData>>("leaves", leaves);

                        return new JsonResult(layout);
                    }

                    message = "You suck, supply an IP.";
                    break;
                }
                case "findNanoLeaf": {
                    LogUtil.Write("Find Nano Leaf called.");
                    var existingLeaves = DreamData.GetItem<List<NanoData>>("leaves");
                    var leaves = NanoDiscovery.Discover(2);

                    var all = new List<NanoData>();
                    LogUtil.Write("Got all devices: " + JsonConvert.SerializeObject(existingLeaves));
                    if (existingLeaves != null) {
                        LogUtil.Write("Adding range.");
                        foreach (var newLeaf in leaves) {
                            var add = true;
                            foreach (var leaf in existingLeaves) {
                                if (leaf.Id == newLeaf.Id) {
                                    LogUtil.Write("Updating existing leaf.");
                                    newLeaf.Token = leaf.Token;
                                    existingLeaves[exInt] = newLeaf;
                                    add = false;
                                    break;
                                }

                                exInt++;
                            }

                            if (add) {
                                LogUtil.Write("Adding new leaf.");
                                all.Add(newLeaf);
                            }
                        }

                        all.AddRange(existingLeaves);
                    } else {
                        all.AddRange(leaves);
                    }

                    LogUtil.Write("Looping: " + all.Count);
                    foreach (var leaf in all.Where(leaf => leaf.Token != null)) {
                        LogUtil.Write("Fetching leaf data.");
                        try {
                            var nl = new Panel(leaf.IpV4Address, leaf.Token);
                            var layout = nl.GetLayout().Result;
                            if (layout != null) leaf.Layout = layout;
                            nl.Dispose();
                        } catch (Exception) {
                            LogUtil.Write("An exception occurred, probably the nanoleaf is unplugged.", "WARN");
                        }

                        LogUtil.Write("Device: " + JsonConvert.SerializeObject(leaf));
                    }

                    LogUtil.Write("Saving nano data from nano search: " + JsonConvert.SerializeObject(all));
                    DreamData.SetItem<List<NanoData>>("leaves", all);
                    message = JsonConvert.SerializeObject(all);
                    break;
                }
                case "authorizeHue": {
                    var doAuth = true;
                    var bridges = DreamData.GetItem<List<BridgeData>>("bridges");
                    BridgeData bd = null;
                    var bridgeInt = -1;

                    if (!string.IsNullOrEmpty(value)) {
                        var bCount = 0;
                        foreach (var b in bridges) {
                            if (b.IpAddress == value) {
                                bd = b;
                                bridgeInt = bCount;
                                doAuth = b.Key == null || b.User == null;
                            }

                            bCount++;
                        }
                    }

                    if (doAuth) {
                        var appKey = HueBridge.CheckAuth(value).Result;
                        if (appKey != null && bd != null) {
                            bd.Key = appKey.StreamingClientKey;
                            bd.User = appKey.Username;
                            bridges[bridgeInt] = bd;
                            DreamData.SetItem("bridges", bridges);
                        }
                    }
                    return new JsonResult(bd);
                }
                case "findHue": {
                    var bridges = HueBridge.FindBridges(3);
                    if (bridges != null) {
                        DreamData.SetItem("bridges", bridges);
                        return new JsonResult(bridges);
                    }

                    message = "Error: No bridge found.";
                    break;
                }
                case "authorizeNano": {
                    var doAuth = true;
                    var leaves = DreamData.GetItem<List<NanoData>>("leaves");
                    NanoData bd = null;
                    var nanoInt = -1;
                    if (!string.IsNullOrEmpty(value)) {
                        var nanoCount = 0;
                        foreach (var n in leaves) {
                            if (n.IpV4Address == value) {
                                bd = n;
                                doAuth = n.Token == null;
                                nanoInt = nanoCount;
                            }
                            nanoCount++;
                        }
                    }

                    if (doAuth) {
                        var panel = new Panel(value);
                        var appKey = panel.CheckAuth();
                        if (appKey != null && bd != null) {
                            bd.Token = appKey.Token;
                            leaves[nanoInt] = bd;
                            DreamData.SetItem("leaves", leaves);
                        }
                        panel.Dispose();
                    }
                    return new JsonResult(bd);
                }
            }

            LogUtil.Write(message);
            return new JsonResult(message);
        }

        // GET: api/DreamData/json
        [HttpGet("json")]
        public IActionResult GetJson() {
            LogUtil.Write(@"GetJson Called.");
            var store = DreamData.GetStore();
            var bridgeArray = DreamData.GetItem<List<BridgeData>>("bridges");
            if (bridgeArray.Count == 0) {
                var newBridges = HueBridge.FindBridges();
                store.ReplaceItem("bridges", newBridges, true);
            }

            if (store.GetItem("dsIp") == "0.0.0.0") {
                var devices = DreamDiscovery.FindDevices();
                store.ReplaceItem("devices", devices);
            }

            store.Dispose();
            return Content(DreamData.GetStoreSerialized(), "application/json");
        }


        private void ResetMode() {
            var myDev = DreamData.GetDeviceData();
            var curMode = myDev.Mode;
            if (curMode == 0) return;
            SetMode(0);
            SetMode(curMode);

        }

        private void SetMode(int mode) {
            var newMode = ByteUtils.IntByte(mode);
            var myDev = DreamData.GetDeviceData();
            var curMode = myDev.Mode;
            if (curMode == newMode) return;
            LogUtil.Write("Updating mode to " + mode);
            var ipAddress = myDev.IpAddress;
            var groupNumber = (byte) myDev.GroupNumber;

            var groupSend = false;
            byte mFlag = 0x11;
            if (ipAddress == "255.255.255.0") {
                groupSend = true;
            } else {
                mFlag = 0x21;
            }

            DreamSender.SendUdpWrite(0x03, 0x01, new[] {newMode}, mFlag, groupNumber,
                new IPEndPoint(IPAddress.Parse(ipAddress), 8888), groupSend);
        }


        private void SetCaptureMode(int capMode) {
            LogUtil.Write("Updating capture mode to " + capMode);
            var curMode = DreamData.GetItem<int>("captureMode");
            var dev = DreamData.GetDeviceData();
            if (curMode == capMode) return;
            var colorMode = dev.Mode;
            DreamData.SetItem<int>("captureMode", capMode);
            var devType = "SideKick";
            if (capMode != 0 && curMode == 0) {
                devType = "DreamVision";
                SwitchDeviceType(devType, dev);
            }

            if (capMode == 0) {
                SwitchDeviceType(devType, dev);
            }

            DreamData.SetItem<string>("devType", devType);
            if (colorMode == 0) return;
            SetMode(0);
            SetMode(colorMode);
        }

        private static void SwitchDeviceType(string devType, BaseDevice curDevice) {
            if (devType == "DreamVision") {
                var newDevice = new DreamVision();
                newDevice.SetDefaults();
                newDevice.Id = curDevice.Id;
                newDevice.Name = curDevice.Name;
                newDevice.IpAddress = curDevice.IpAddress;
                newDevice.Brightness = curDevice.Brightness;
                newDevice.GroupNumber = curDevice.GroupNumber;
                newDevice.flexSetup = curDevice.flexSetup;
                newDevice.Saturation = curDevice.Saturation;
                newDevice.Mode = curDevice.Mode;
                DreamData.SetItem("myDevice", newDevice);
            } else {
                var newDevice = new SideKick();
                newDevice.SetDefaults();
                newDevice.Id = curDevice.Id;
                newDevice.Name = curDevice.Name;
                newDevice.IpAddress = curDevice.IpAddress;
                newDevice.Brightness = curDevice.Brightness;
                newDevice.GroupNumber = curDevice.GroupNumber;
                newDevice.flexSetup = curDevice.flexSetup;
                newDevice.Saturation = curDevice.Saturation;
                newDevice.Mode = curDevice.Mode;
                DreamData.SetItem("myDevice", newDevice);
            }
        }

        private void SetDevName(string ipAddress, string name, int group) {
            var groupNumber = (byte) group;
            bool groupSend = false;
            byte mFlag = 0x11;
            if (ipAddress == "255.255.255.0") {
                groupSend = true;
            } else {
                mFlag = 0x21;
            }

            LogUtil.Write($@"Setting device name for {ipAddress} to {name}.");
            DreamSender.SendUdpWrite(0x01, 0x07, ByteUtils.StringBytePad(name, 16).ToArray(), mFlag, groupNumber,
                new IPEndPoint(IPAddress.Parse(ipAddress), 8888), groupSend);
            List<JObject> devices = DreamData.GetItem<List<JObject>>("devices");
            List<JObject> newDevices = new List<JObject>();
            foreach(var dev in devices) {
                var dIp = (string) dev.SelectToken("ipAddress");
                if (dIp == ipAddress) {
                    dev["name"] = ByteUtils.StringBytePad(name, 16).ToArray();
                    LogUtil.Write("We dun got us an updated device: " + JsonConvert.SerializeObject(dev));
                }
                newDevices.Add(dev);
            }
            DreamData.SetItem("devices", newDevices);
        }

        private void SetBrightness(int dData) {
            var myDev = DreamData.GetDeviceData();
            var ipAddress = myDev.IpAddress;
            var groupNumber = (byte) myDev.GroupNumber;
            var groupSend = false;
            byte mFlag = 0x11;
            if (ipAddress == "255.255.255.0") {
                groupSend = true;
            } else {
                mFlag = 0x21;
            }

            DreamSender.SendUdpWrite(0x03, 0x02, new[] {ByteUtils.IntByte(dData)}, mFlag, groupNumber,
                new IPEndPoint(IPAddress.Parse(ipAddress), 8888), groupSend);
        }
    }
}