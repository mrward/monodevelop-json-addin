//
// LanguageServiceClient.cs
//
// Author:
//       Matt Ward <ward.matt@gmail.com>
//
// Copyright (c) 2016 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System.Diagnostics;
using System;
using Newtonsoft.Json;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.LanguageServices.Messages;

namespace MonoDevelop.LanguageServices
{
	public class LanguageServiceClient
	{
		ProcessWrapper process;
		LanguageServiceResponseReader reader;

		public void Start (string startupFile)
		{
			reader = new LanguageServiceResponseReader (ProcessResponse);

			var startInfo = new ProcessStartInfo {
				FileName = "node",
				//Arguments = "--debug --no-lazy " + startupFile,
				Arguments = "\"" + startupFile + "\"",
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				RedirectStandardInput = true
			};

			process = new ProcessWrapper ();
			process.StartInfo = startInfo;
			process.OutputStreamChanged += ProcessOutputStreamChanged;
			process.ErrorStreamChanged += ProcessErrorStreamChanged;

			process.Start ();
		}

		void ProcessOutputStreamChanged (object sender, string message)
		{
			try {
				reader.OnData (message);
			} catch (Exception ex) {
				LoggingService.LogError ("LanguageServiceClient read error.", ex);
			}
		}

		void ProcessErrorStreamChanged (object sender, string message)
		{
			LoggingService.LogError ("LanguageServiceClient error: {0}", message);
		}

		public void Stop ()
		{
			if (process != null) {
				process.OutputStreamChanged -= ProcessOutputStreamChanged;
				process.ErrorStreamChanged -= ProcessErrorStreamChanged;

				if (!process.HasExited) {
					process.Kill ();
				}
				process = null;
			}
		}

		public void SendMessage (Message message)
		{
			SendMessage (JsonConvert.SerializeObject (message));
		}

		public void SendMessage (string message)
		{
			string fullMessage = string.Format ("Content-Length: {0}\r\n\r\n{1}", message.Length, message);
			process.StandardInput.Write (fullMessage);
			process.StandardInput.Flush ();
		}

		void ProcessResponse (ResponseMessage response)
		{
			OnResponse (response);
		}

		public Action<ResponseMessage> OnResponse = response => { };
	}
}

