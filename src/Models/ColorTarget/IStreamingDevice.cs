﻿using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Glimmr.Models.ColorTarget {
	public interface IStreamingDevice {
		public bool Streaming { get; set; }
		public bool Testing { get; set; }
		public int Brightness { get; set; }
		public string Id { get; set; }
		public string IpAddress { get; set; }
		public string Tag { get; set; }
		public bool Enable { get; set; }
        
		public StreamingData Data { get; set; }
        
		public Task StartStream(CancellationToken ct);

		public Task StopStream();

		public Task SetColor(Object o, DynamicEventArgs args);

		public Task FlashColor(Color color);

		public bool IsEnabled() {
			return Enable;
		}

		public Task ReloadData();

		public void Dispose();
	}
}