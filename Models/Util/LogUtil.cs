﻿using System.Diagnostics;
using Newtonsoft.Json;
using Serilog;

namespace HueDream.Models.Util {
    public static class LogUtil {
        private static int _msgCount;
        
        public static void Write(string text, dynamic myObject, string level="INFO") {
            var objStr = string.Empty;
            if (!myObject.GetType().IsSerializable) {
                Write(text, level);
            } else {
                objStr = JsonConvert.SerializeObject(myObject, myObject.GetType());
            }
            Write(text + objStr, level);
        
        }

        public static void Write(string text, string level="INFO") {
            var cls = GetCaller();
            var msg = $@"{cls} - {text}";
            switch (level) {
                case "INFO":
                    Log.Information(msg);
                    break;
                case "DEBUG":
                    Log.Debug(msg);
                    break;
                case "WARN":
                    Log.Warning(msg);
                    break;
                case "ERROR":
                    Log.Error(msg);
                    break;
            }
        }

        public static void WriteInc(string text, dynamic myObject, string level="INFO") {
            var objStr = string.Empty;
            if (!myObject.GetType().IsSerializable) {
                WriteInc(text, level);
            } else {
                objStr = JsonConvert.SerializeObject(myObject, myObject.GetType());
            }
            WriteInc(text + objStr, level);
        }

        public static void WriteDec(string text, dynamic myObject, string level="INFO") {
            var objStr = string.Empty;
            if (!myObject.GetType().IsSerializable) {
                WriteDec(text, level);
            } else {
                objStr = JsonConvert.SerializeObject(myObject, myObject.GetType());
            }
            WriteDec(text + objStr, level);
        }
        
        public static void WriteInc(string text, string level="INFO") {
            Write($@"C{_msgCount} - {text}", level);
            _msgCount++;
        }

        public static void WriteDec(string text, string level="INFO") {
            _msgCount--;
            if (_msgCount < 0) _msgCount = 0;
            Write($@"C{_msgCount} - {text}", level);
        }

        private static string GetCaller() {
            var stackInt = 1;
            var st = new StackTrace();
            while (stackInt < 10) {
                var mth = st.GetFrame(stackInt).GetMethod();
                var dType = mth.DeclaringType;
                if (dType != null) {
                    var cls = dType.Name;
                    if (!string.IsNullOrEmpty(cls)) {
                        if (cls != "LogUtil") {
                            return cls + "::" + mth.Name;
                        }
                    }
                }

                stackInt++;
            }

            return string.Empty;
        }
    }
}