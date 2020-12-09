﻿#region

using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Threading;
using Glimmr.Models.StreamingDevice.DreamScreen.Encoders;
using Newtonsoft.Json;

#endregion

namespace Glimmr.Models.StreamingDevice.DreamScreen {
	public class DreamDevice : IStreamingDevice {
		[DataMember] [JsonProperty] public bool Streaming { get; set; }
		[DataMember] [JsonProperty] public int Brightness { get; set; }
		[DataMember] [JsonProperty] public string Id { get; set; }
		[DataMember] [JsonProperty] public string IpAddress { get; set; }
		[DataMember] [JsonProperty] public string Tag { get; set; }
		[DataMember] [JsonProperty] public string DeviceTag { get; set; }
		[DataMember] [JsonProperty] public bool Enable { get; set; }
		[DataMember] [JsonProperty] public DreamData Data { get; set; }


		public void StartStream(CancellationToken ct) {
		}

		public void StopStream() {
		}

		public void SetColor(List<Color> colors, double fadeTime) {
		}

		public void ReloadData() {
		}

		public void Dispose() {
		}

		StreamingData IStreamingDevice.Data {
			get => Data;
			set => Data = (DreamData) value;
		}

		public DreamDevice(DreamData data) {
			Data = data;
			Brightness = data.Brightness;
			Id = data.Id;
			IpAddress = data.IpAddress;
			Tag = data.Tag;
			Enable = data.Enable;
			DeviceTag = data.DeviceTag;
		}

		public byte[] EncodeState() {
			return Data.EncodeState();
		}
	}
}