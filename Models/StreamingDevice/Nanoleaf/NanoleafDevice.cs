﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Glimmr.Models.Util;
using Nanoleaf.Client;
using Nanoleaf.Client.Exceptions;
using Nanoleaf.Client.Models.Responses;
using Newtonsoft.Json;
using Serilog;

namespace Glimmr.Models.StreamingDevice.Nanoleaf {
	public sealed class NanoleafDevice : IStreamingDevice, IDisposable {
		private string _token;
		private string _basePath;
		private NanoLayout _layout;
		private int _streamMode;
		private bool _disposed;
		private bool _sending;
		private int _captureMode;
		public bool Enable { get; set; }
		StreamingData IStreamingDevice.Data {
			get => Data;
			set => Data = (NanoleafData) value;
		}

		public NanoleafData Data { get; set; }
		public int Brightness { get; set; }
		public string Id { get; set; }
		public string IpAddress { get; set; }
		public string Tag { get; set; }
		private readonly Socket _sender;
		private readonly HttpClient _client;

		


		public NanoleafDevice(string ipAddress, string token = "") {
			_captureMode = DataUtil.GetItem<int>("captureMode");
			IpAddress = ipAddress;
			_token = token;
			_basePath = "http://" + IpAddress + ":16021/api/v1/" + _token;
			_disposed = false;
		}

		public NanoleafDevice(NanoleafData n, Socket socket, HttpClient client) {
			_captureMode = DataUtil.GetItem<int>("captureMode");
			if (n != null) {
				SetData(n);
				_sender = socket;
				_client = client;
			}

			_disposed = false;
		}


		public void ReloadData() {
			var newData = DataUtil.GetCollectionItem<NanoleafData>("Dev_Nanoleaf", Id);
			SetData(newData);
		}

		private void SetData(NanoleafData n) {
			Data = n;
			_captureMode = DataUtil.GetItem<int>("captureMode");
			IpAddress = n.IpAddress;
			_token = n.Token;
			_layout = n.Layout;
			Brightness = n.Brightness;
			var nanoType = n.Type;
			_streamMode = nanoType == "NL29" ? 2 : 1;
			_basePath = "http://" + IpAddress + ":16021/api/v1/" + _token;
			Id = n.Id;
		}
        
		public bool IsEnabled() {
			return Data.Enable;
		}

        
		public bool Streaming { get; set; }

		public async void StartStream(CancellationToken ct) {
			if (!Data.Enable) return;
			Log.Debug($@"Nanoleaf: Starting panel: {IpAddress}");
			// Turn it on first.
			//var currentState = NanoSender.SendGetRequest(_basePath).Result;
			//await NanoSender.SendPutRequest(_basePath, JsonConvert.SerializeObject(new {on = new {value = true}}),
			//"state");
			var controlVersion = "v" + _streamMode;
			var body = new
				{write = new {command = "display", animType = "extControl", extControlVersion = controlVersion}};

			await SendPutRequest(_basePath, JsonConvert.SerializeObject(new {on = new {value = true}}),
				"state");
			await SendPutRequest(_basePath, JsonConvert.SerializeObject(body), "effects");
			Log.Debug("Nanoleaf: Streaming is active...");
			_sending = true;
			while (!ct.IsCancellationRequested) {
				Streaming = true;
			}
			_sending = false;
			StopStream();
		}

		public void StopStream() {
			Streaming = false;
			SendPutRequest(_basePath, JsonConvert.SerializeObject(new {on = new {value = false}}), "state")
				.ConfigureAwait(false);
			Log.Debug($@"Nanoleaf: Stopped panel: {IpAddress}");

		}


		public void SetColor(List<Color> _, List<Color> colors, double fadeTime = 1) {
			int ft = (int) fadeTime;
			if (!Streaming) {
				Log.Debug("Streaming is  not active?");
				return;
			}

			var capCount = _captureMode == 0 ? 12 : 28;
            
			if (colors == null || colors.Count < capCount) {
				throw new ArgumentException("Invalid color list.");
			}

			var byteString = new List<byte>();
			if (_streamMode == 2) {
				byteString.AddRange(ByteUtils.PadInt(_layout.NumPanels));
			} else {
				byteString.Add(ByteUtils.IntByte(_layout.NumPanels));
			}
			foreach (var pd in _layout.PositionData) {
				var id = pd.PanelId;
				var colorInt = pd.TargetSector - 1;
				if (_streamMode == 2) {
					byteString.AddRange(ByteUtils.PadInt(id));
				} else {
					byteString.Add(ByteUtils.IntByte(id));
				}

				if (pd.TargetSector == -1) continue;
				//Log.Debug("Sector for light " + id + " is " + pd.Sector);
				var color = colors[colorInt];
				if (Brightness < 100) {
					color = ColorTransformUtil.ClampBrightness(color, Brightness);
				}

				// Add rgb values
				byteString.Add(ByteUtils.IntByte(color.R));
				byteString.Add(ByteUtils.IntByte(color.G));
				byteString.Add(ByteUtils.IntByte(color.B));
				// White value
				byteString.AddRange(ByteUtils.PadInt(0, 1));
				// Pad duration time
				byteString.AddRange(_streamMode == 2 ? ByteUtils.PadInt(ft) : ByteUtils.PadInt(ft, 1));
			}

			Task.Run(() => {
				SendUdpUnicast(byteString.ToArray());
			}); 
                
		}


     
		public async Task<UserToken> CheckAuth() {
			var nanoleaf = new NanoleafClient(IpAddress);
			UserToken result = null;
			try {
				result = await nanoleaf.CreateTokenAsync().ConfigureAwait(false);
				Log.Debug("Authorized.");
			} catch (AggregateException e) {
				Log.Debug("Unauthorized Exception: " + e.Message);
			}

			nanoleaf.Dispose();
			return result;
		}

		private void SendUdpUnicast(byte[] data) {
			if (!_sending) return;
			var ep = IpUtil.Parse(IpAddress, 60222);
			if (ep != null) _sender.SendTo(data, ep);
		}

		public async Task<NanoLayout> GetLayout() {
			if (string.IsNullOrEmpty(_token)) return null;
			var fLayout = await SendGetRequest(_basePath, "panelLayout/layout").ConfigureAwait(false);
			var lObject = JsonConvert.DeserializeObject<NanoLayout>(fLayout);
			return lObject;
		}

		private async Task<string> SendPutRequest(string basePath, string json, string path = "") {
            var authorizedPath = new Uri(basePath + "/" + path);
            try {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var responseMessage = await _client.PutAsync(authorizedPath, content).ConfigureAwait(false);
                if (!responseMessage.IsSuccessStatusCode) {
                    HandleNanoleafErrorStatusCodes(responseMessage);
                }

                return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            } catch (HttpRequestException) {
                return null;
            }
        }

        private async Task<string> SendGetRequest(string basePath, string path = "") {
            var authorizedPath = basePath + "/" + path;
            var uri = new Uri(authorizedPath);
            try {
                using var responseMessage = await _client.GetAsync(uri).ConfigureAwait(false);
                if (responseMessage.IsSuccessStatusCode)
                    return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.Debug("Error contacting nanoleaf: " + responseMessage.Content);
                HandleNanoleafErrorStatusCodes(responseMessage);

                return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            } catch (HttpRequestException) {
                return null;
            }
        }

        private static void HandleNanoleafErrorStatusCodes(HttpResponseMessage responseMessage) {
	        Log.Warning("Error with nano request: ", responseMessage);
        }


        public void Dispose() {
			Dispose(true);
		}

		private void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}

			if (!disposing) return;
			_disposed = true;
		}
	}
}