using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Glimmr.Models.Util;
using Glimmr.Services;
using rpi_ws281x;
using Serilog;
using ColorUtil = Glimmr.Models.Util.ColorUtil;

namespace Glimmr.Models.ColorTarget.Led {
	public class LedDevice : ColorTarget, IColorTarget {
		public bool Streaming { get; set; }
		public bool Testing { get; set; }
		public int Brightness { get; set; }
		public string Id { get; set; }
		public string IpAddress { get; set; }
		public string Tag { get; set; }
		public bool Enable { get; set; }
		public bool Online { get; set; }

		IColorTargetData IColorTarget.Data {
			get => Data;
			set => Data = (LedData) value;
		}
		public float CurrentMilliamps { get; set; }
		public LedData Data;
		
		private int _ledCount;
		private int _offset;
		private bool _enableAbl;
		private WS281x _strip;
		
		
		public LedDevice(LedData ld, ColorService colorService) : base(colorService) {
			var cs = colorService;
			cs.ColorSendEvent += SetColor;
			Data = ld;
			Id = Data.Id;
			ReloadData();
		}
		

		
		private void CreateStrip() {
			var settings = LoadLedData(Data);
			// Hey, look, this is built natively into the LED app
			try {
				_strip = new WS281x(settings);
				Streaming = true;
			} catch (Exception) {
				Streaming = false;
			}
		}

		public async Task StartStream(CancellationToken ct) {
			if (!Enable) return;
			Log.Debug($"Starting LED stream for LED {Id}...");
			Streaming = true;
			await Task.FromResult(Streaming);
			Log.Debug("LED stream started.");
		}

		public async Task StopStream() {
			if (!Enable) return;
			await StopLights().ConfigureAwait(false);
			Streaming = false;
		}

		
		public Task FlashColor(Color color) {
			_strip?.SetAll(color);
			return Task.CompletedTask;
		}

		
		
		public void Dispose() {
			_strip?.Reset();
			_strip?.Dispose();
		}

		public Task ReloadData() {
			var ld = DataUtil.GetDevice<LedData>(Id);
			var sd = DataUtil.GetSystemData();
			if (ld == null) {
				Log.Warning("No LED Data");
				return Task.CompletedTask;
			}
			Data = ld;
			if (Id == "2" || Id == "1") {
				Enable = false;
				Streaming = false;
				return Task.CompletedTask;
			}
			Enable = Data.Enable;
			_ledCount = Data.LedCount;
			if (_ledCount > sd.LedCount) _ledCount = sd.LedCount;
			_enableAbl = Data.AutoBrightnessLevel;
			_offset = Data.Offset;

			if (Enable) {
				CreateStrip();
			} else {
				return Task.CompletedTask;
			}

			if (_strip == null) {
				return Task.CompletedTask;
			}

			if (Data.Brightness != ld.Brightness) _strip.SetBrightness(ld.Brightness/100 * 255);
			if (Data.LedCount != ld.LedCount) _strip.SetLedCount(ld.LedCount);
			if (!_enableAbl) _strip.SetBrightness(ld.Brightness / 100 * 255);
			return Task.CompletedTask;
		}

	

		private Settings LoadLedData(LedData ld) {
			var settings = Settings.CreateDefaultSettings();
			if (!ld.FixGamma) settings.SetGammaCorrection(0, 0, 0);
			var stripType = ld.StripType switch {
				1 => StripType.SK6812W_STRIP,
				2 => StripType.WS2811_STRIP_RBG,
				0 => StripType.WS2812_STRIP,
				_ => StripType.WS2812_STRIP
			};
			
			// 18 = PWM0, 13 = PWM1, 21 = PCM, 10 = SPI0/MOSI
			var pin = ld.GpioNumber switch {
				19 => Pin.Gpio19,
				10 => Pin.Gpio10,
				18 => Pin.Gpio18,
				13 => Pin.Gpio13,
				_ => Pin.Gpio18
			};
			
			settings.AddController(_ledCount, pin, stripType);
			Data = ld;
			return settings;
		}
		
		public void SetColor(List<Color> colors, List<Color> sectors, int fadeTime, bool force=false) {
			if (colors == null) {
				throw new ArgumentException("Invalid color input.");
			}

			if (!Streaming || !Enable || (Testing && !force)) {
				return;
			}

			var c1 = TruncateColors(colors, _ledCount, _offset);
			if (_enableAbl) {
				c1 = VoltAdjust(c1, Data);
			}
			
			for (var i = 0; i < c1.Count; i++) {
				var tCol = c1[i];
				if (Data.StripType == 1) {
					tCol = ColorUtil.ClampAlpha(tCol);
				}
				_strip?.SetLed(i,tCol);
			}
			_strip?.Render();
			ColorService.Counter.Tick(Id);
		}

		private static List<Color> TruncateColors(List<Color> input, int len, int offset) {
			var truncated = new List<Color>();
			// Subtract one from our offset because arrays
			// Start at the beginning
			if (offset + len > input.Count) {
				// Set the point where we need to end the loop
				var offsetLen = offset + len - input.Count;
				// Where do we start midway?
				var loopLen = input.Count - offsetLen;
				if (loopLen > 0) {
					for (var i = loopLen - 1; i < input.Count; i++) {
						truncated.Add(input[i]);
					}
				}

				// Now calculate how many are needed from the front
				for (var i = 0; i < len - offsetLen; i++) {
					truncated.Add(input[i]);
				}
			} else {
				for (var i = offset; i < offset + len; i++) {
					truncated.Add(input[i]);
				}    
			}

			return truncated;
		}

		private async Task StopLights() {
			if (!Enable) return;
			_strip?.SetAll(Color.FromArgb(0,0,0,0));
			await Task.FromResult(true);
		}

		private List<Color> VoltAdjust(List<Color> input, LedData ld) {
			//power limit calculation
			//each LED can draw up 195075 "power units" (approx. 53mA)
			//one PU is the power it takes to have 1 channel 1 step brighter per brightness step
			//so A=2,R=255,G=0,B=0 would use 510 PU per LED (1mA is about 3700 PU)
			var actualMilliampsPerLed = ld.MilliampsPerLed; // 20
			var defaultBrightness = (int) (ld.Brightness / 100f * 255);
			var ablMaxMilliamps = ld.AblMaxMilliamps; // 4500
			var length = input.Count;
			var output = input;
			if (ablMaxMilliamps > 149 && actualMilliampsPerLed > 0) {
				//0 mA per LED and too low numbers turn off calculation

				var puPerMilliamp = 195075 / actualMilliampsPerLed;
				var powerBudget = ablMaxMilliamps * puPerMilliamp; //100mA for ESP power
				if (powerBudget > puPerMilliamp * length) {
					//each LED uses about 1mA in standby, exclude that from power budget
					powerBudget -= puPerMilliamp * length;
				} else {
					powerBudget = 0;
				}

				var powerSum = 0;

				for (var i = 0; i < length; i++) {
					//sum up the usage of each LED
					var c = input[i];
					powerSum += c.R + c.G + c.B + c.A;
				}

				if (ld.StripType == 1) {
					//RGBW led total output with white LEDs enabled is still 50mA, so each channel uses less
					powerSum *= 3;
					powerSum >>= 2; //same as /= 4
				}

				var powerSum0 = powerSum;
				powerSum *= defaultBrightness;

				if (powerSum > powerBudget) {
					//scale brightness down to stay in current limit
					var scale = powerBudget / (float) powerSum;
					var scaleI = scale * 255;
					var scaleB = scaleI > 255 ? 255 : scaleI;
					var newBri = scale8(defaultBrightness, scaleB);
					_strip?.SetBrightness((int)newBri);
					CurrentMilliamps = powerSum0 * newBri / puPerMilliamp;
					if (newBri < defaultBrightness) {
						//output = ColorUtil.ClampBrightness(input, newBri);
					}
				} else {
					CurrentMilliamps = (float) powerSum / puPerMilliamp;
					if (defaultBrightness < 255) {
						_strip?.SetBrightness(defaultBrightness);
					}
				}

				CurrentMilliamps += length; //add standby power back to estimate
			} else {
				CurrentMilliamps = 0;
				if (defaultBrightness < 255) {
					_strip?.SetBrightness(defaultBrightness);
				}
			}

			return output;
		}

		private float scale8(float i, float scale) {
			return i * (scale / 256);
		}
	}
}