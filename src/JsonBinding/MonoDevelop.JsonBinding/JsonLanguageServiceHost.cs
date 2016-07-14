//
// JsonLanguageServiceHost.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.LanguageServices;
using MonoDevelop.LanguageServices.Messages;
using MonoDevelop.Projects;

namespace MonoDevelop.JsonBinding
{
	public class JsonLanguageServiceHost
	{
		LanguageServiceClient client;
		Project project;
		string rootPath;
		int requestId = 1;
		TaskCompletionSource<ResponseMessage> taskCompletionSource;

		public JsonLanguageServiceHost (Project project)
		{
			this.project = project;
			rootPath = GetRootPath (project);
		}

		public Project Project {
			get { return project; }
		}

		static string GetRootPath (Project project)
		{
			if (project != null) {
				return project.ParentSolution.BaseDirectory;
			}

			return String.Empty;
		}

		public void Start ()
		{
			client = new LanguageServiceClient ();
			client.OnResponse = OnResponse;
			client.Start (GetServerFileName ());

			SendInitializeMessage ();

			UpdateJsonSchemaAssociations ();
		}

		public void Stop ()
		{
			client.OnResponse = null;
			client.Stop ();
		}

		string GetServerFileName ()
		{
			string rootDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);
			return Path.Combine (rootDirectory, "Server", "out", "jsonServerMain.js");
		}

		void SendInitializeMessage ()
		{
			var initParams = new InitializeParams {
				processId = Process.GetCurrentProcess ().Id,
				rootPath = rootPath,
				initializationOptions = new InitializationOptions {
					languageIds = "json"
				}
			};

			var init = new RequestMessage {
				id = requestId,
				method = "initialize",
				@params = initParams
			};

			requestId++;
			client.SendMessage (init);
		}

		void UpdateJsonSchemaAssociations ()
		{
			string directory = Path.GetDirectoryName (GetType ().Assembly.Location);
			string schema = Path.Combine (directory, "Schemas", "project.json");
			var associations = new Dictionary<string, string[]> ();

			associations["project.json"] = new string[] { new Uri (schema).ToString () };

			var schemaNotification = new NotificationMessage {
				method = "json/schemaAssociations",
				@params = associations
			};

			client.SendMessage (schemaNotification);
		}

		public void OpenDocument (Document document)
		{
			var textDocument = new TextDocumentItem {
				languageid = "json",
				uri = document.FileName,
				version = 1,
				text = document.Editor.Text
			};

			var messageParams = new DidOpenTextDocumentParams {
				uri = document.FileName,
				textDocument = textDocument
			};

			var openDocument = new NotificationMessage {
				method = "textDocument/didOpen",
				@params = messageParams
			};

			client.SendMessage (openDocument);
		}

		public void CloseDocument (Document document)
		{
			var textDocument = new TextDocumentIdentifier {
				uri = document.FileName
			};

			var messageParams = new DidCloseTextDocumentParams {
				textDocument = textDocument
			};

			var notification = new NotificationMessage {
				method = "textDocument/didClose",
				@params = messageParams
			};

			client.SendMessage (notification);
		}

		public Task<ResponseMessage> GetCompletionInfo (FilePath fileName, CodeCompletionContext context)
		{
			var positionParams = new TextDocumentPositionParams {
				textDocument = new TextDocumentIdentifier {
					uri = fileName
				},
				position = new Position {
					line = context.TriggerLine - 1,
					character = context.TriggerLineOffset - 1
				}
			};

			var completion = new RequestMessage {
				id = requestId,
				method = "textDocument/completion",
				@params = positionParams
			};

			if (taskCompletionSource != null) {
				taskCompletionSource.SetCanceled ();
			}
			taskCompletionSource = new TaskCompletionSource<ResponseMessage> (completion);

			requestId++;
			client.SendMessage (completion);

			return taskCompletionSource.Task;
		}

		void OnResponse (ResponseMessage response)
		{
			if (response.method == "textDocument/publishDiagnostics") {
				OnDiagnosticsResponse (response);
				return;
			}

			if (response.id == 0)
				return;

			if (taskCompletionSource != null) {
				var request = (RequestMessage)taskCompletionSource.Task.AsyncState;
				if (response.id == request.id) {
					taskCompletionSource.SetResult (response);
				} else if (response.id > request.id) {
					taskCompletionSource.SetResult (null);
				}
				taskCompletionSource = null;
			}
		}

		public event EventHandler<DiagnosticEventArgs> OnDiagnostics;

		void OnDiagnosticsResponse (ResponseMessage response)
		{
			if (response.@params == null) {
				LoggingService.LogError ("Invalid diagnostics response. params is null.");
				return;
			}

			var diagnostics = response.@params.ToObject<PublishDiagnosticsParams> ();
			OnDiagnostics?.Invoke (this, new DiagnosticEventArgs (diagnostics));
		}

		public void DocumentTextChanged (FilePath fileName, string text, int version)
		{
			var textDocument = new VersionedTextDocumentIdentifier {
				uri = fileName,
				version = version
			};

			var messageParams = new DidChangeTextDocumentParams {
				textDocument = textDocument,
				contentChanges = new [] {
					new TextDocumentContentChangeEvent {
						text = text
					}
				}
			};

			var notification = new NotificationMessage {
				method = "textDocument/didChange",
				@params = messageParams
			};

			client.SendMessage (notification);
		}
	}
}

