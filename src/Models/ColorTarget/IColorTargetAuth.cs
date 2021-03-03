﻿using System.Threading.Tasks;

namespace Glimmr.Models.ColorTarget {
	public interface IColorTargetAuth {
		public Task<dynamic> CheckAuthAsync(dynamic deviceData);
	}
}