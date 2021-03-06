﻿#region

using System;
using System.Globalization;
using System.Net;
using Glimmr.Models.Util;
using Newtonsoft.Json;

#endregion

namespace Glimmr.Models.ColorTarget.Glimmr {
	[Serializable]
	public class GlimmrData : IColorTargetData {
		[JsonProperty] public bool MirrorHorizontal { get; set; }
		[JsonProperty] public bool UseCenter { get; set; }
		[JsonProperty] public int BottomCount { get; set; }
		[JsonProperty] public int HCount { get; set; }
		[JsonProperty] public int LedCount { get; set; }
		[JsonProperty] public int LeftCount { get; set; }
		[JsonProperty] public int RightCount { get; set; }
		[JsonProperty] public int SectorCount { get; set; }
		[JsonProperty] public int TopCount { get; set; }
		[JsonProperty] public int VCount { get; set; }
		[JsonProperty] public string Name { get; set; } = "";
		[JsonProperty] public string Id { get; set; } = "";
		[JsonProperty] public string Tag { get; set; }
		[JsonProperty] public string IpAddress { get; set; } = "";
		[JsonProperty] public int Brightness { get; set; } = 255;
		[JsonProperty] public bool Enable { get; set; }

		public string LastSeen { get; set; }


		public GlimmrData() {
			Tag = "Glimmr";
			Name ??= Tag;
			if (Id != null) {
				Name = StringUtil.UppercaseFirst(Id);
			}

			LastSeen = DateTime.Now.ToString(CultureInfo.InvariantCulture);
		}

		public GlimmrData(string id, IPAddress ip) {
			Id = id;
			Tag = "Glimmr";
			Name ??= Tag;
			if (Id != null) {
				Name = StringUtil.UppercaseFirst(Id);
			}

			IpAddress = ip.ToString();
			FetchData();
			LastSeen = DateTime.Now.ToString(CultureInfo.InvariantCulture);
		}

		public GlimmrData(SystemData sd) {
			Tag = "Glimmr";
			Name ??= Tag;
			LastSeen = DateTime.Now.ToString(CultureInfo.InvariantCulture);
			LedCount = sd.LedCount;
			LeftCount = sd.LeftCount;
			RightCount = sd.RightCount;
			TopCount = sd.TopCount;
			VCount = sd.VSectors;
			HCount = sd.HSectors;
			BottomCount = sd.BottomCount;
			Brightness = sd.Brightness;
			SectorCount = sd.SectorCount;
			IpAddress = IpUtil.GetLocalIpAddress();
			Id = Dns.GetHostName();
		}

	

		public void UpdateFromDiscovered(IColorTargetData data) {
			var input = (GlimmrData) data;
			if (input == null) {
				throw new ArgumentNullException(nameof(input));
			}

			LedCount = input.LedCount;
			IpAddress = data.IpAddress;
			FetchData();
			if (Id != null) {
				Name = StringUtil.UppercaseFirst(Id);
			}
		}

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public SettingsProperty[] KeyProperties { get; set; } = {
			new("MirrorHorizontal", "check", "Mirror LED Colors")
		};

		private void FetchData() {
			using var webClient = new WebClient();
			try {
				var url = "http://" + IpAddress + "/api/DreamData/json";
				var jsonData = webClient.DownloadString(url);
				var sd = JsonConvert.DeserializeObject<GlimmrData>(jsonData);
				if (sd != null) {
					LedCount = sd.LedCount;
					LeftCount = sd.LeftCount;
					RightCount = sd.RightCount;
					TopCount = sd.TopCount;
					BottomCount = sd.BottomCount;
					Brightness = sd.Brightness;
					HCount = sd.HCount;
					VCount = sd.VCount;
					UseCenter = sd.UseCenter;
					SectorCount = sd.SectorCount;
				}
			} catch (Exception) {
				// Ignored
			}
		}
	}
}