//
// LanguageServiceResponseReader.cs
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

using System;
using System.Text;
using MonoDevelop.LanguageServices.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.LanguageServices
{
	public class LanguageServiceResponseReader
	{
		static readonly string contentLengthHeader = "Content-Length:";
		StringBuilder messageBuilder = new StringBuilder ();
		State state = State.HeaderStart;
		bool continueProcessing;
		int contentLength = -1;
		Action<ResponseMessage> onResponse;

		enum State {
			HeaderStart,
			HeaderEnd,
			Body
		}

		public LanguageServiceResponseReader (Action<ResponseMessage> onResponse)
		{
			this.onResponse = onResponse;
		}

		public void OnData (string message)
		{
			messageBuilder.Append (message);
			continueProcessing = true;

			while (continueProcessing) {
				switch (state) {
					case State.HeaderStart:
					ReadHeaderStart ();
					break;
					case State.HeaderEnd:
					ReadHeaderEnd ();
					break;
					case State.Body:
					ReadBody ();
					break;
				}
			}
		}

		void ReadHeaderStart ()
		{
			string line = ReadLine ();
			if (line == null) {
				continueProcessing = false;
				return;
			}

			if (line.StartsWith (contentLengthHeader, StringComparison.OrdinalIgnoreCase)) {
				string contentLengthText = line.Substring (contentLengthHeader.Length).Trim ();
				if (int.TryParse (contentLengthText, out contentLength)) {
					state = State.HeaderEnd;
				}
			}
		}

		void ReadHeaderEnd ()
		{
			string line = ReadLine ();
			if (line == string.Empty) {
				state = State.Body;
			} else if (line == null) {
				continueProcessing = false;
			}
		}

		void ReadBody ()
		{
			string message = messageBuilder.ToString ();
			if (message.Length >= contentLength) {
				string body = message.Substring (0, contentLength);
				messageBuilder.Remove (0, contentLength);
				state = State.HeaderStart;

				OnMessageBody (body);
			} else {
				continueProcessing = false;
			}
		}

		string ReadLine ()
		{
			string message = messageBuilder.ToString ();
			int index = message.IndexOf ("\r\n", StringComparison.Ordinal);
			if (index >=0) {
				string line = message.Substring (0, index);
				messageBuilder.Remove (0, index + 2);
				return line;
			}

			return null;
		}

		void OnMessageBody (string body)
		{
			var response = JsonConvert.DeserializeObject<ResponseMessage> (body);
			onResponse (response);
		}
	}
}

