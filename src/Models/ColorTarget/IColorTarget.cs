﻿#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Glimmr.Services;
using Newtonsoft.Json;

#endregion

namespace Glimmr.Models.ColorTarget {
	public interface IColorTarget {
		public bool Enable { get; set; }
		public bool Streaming { get; set; }
		public bool Testing { get; set; }

		[JsonProperty] public DateTime LastSeen => DateTime.Now;

		public IColorTargetData Data { get; set; }
		public int Brightness { get; set; }
		public string Id { get; set; }
		public string IpAddress { get; set; }
		public string Tag { get; set; }

		public Task StartStream(CancellationToken ct);

		public Task StopStream();

		public void SetColor(List<Color> colors, List<Color> sectors, int fadeTime, bool force = false);

		public Task FlashColor(Color color);


		public Task ReloadData();

		public void Dispose();
	}

	public abstract class ColorTarget {
		public ColorService? ColorService { get; set; }

		protected ColorTarget(ColorService cs) {
			ColorService = cs;
		}

		protected ColorTarget() {
		}
	}
}