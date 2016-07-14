//
// JsonTextEditorExtension.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.LanguageServices;
using MonoDevelop.LanguageServices.Messages;

namespace MonoDevelop.JsonBinding
{
	public class JsonTextEditorExtension : CompletionTextEditorExtension
	{
		JsonLanguageServiceHost host;
		List<IErrorMarker> errorMarkers = new List<IErrorMarker> ();
		int version = 1;

		protected override void Initialize ()
		{
			JsonServices.Activate ();

			host = JsonServices.Workspace.GetHost (DocumentContext.Project);
			host.OnDiagnostics += OnDiagnostics;

			Editor.TextChanged += TextChanged;

			base.Initialize ();
		}

		public override void Dispose ()
		{
			if (host != null) {
				host.OnDiagnostics -= OnDiagnostics;
			}

			host = null;
			base.Dispose ();
		}

		public override string CompletionLanguage {
			get { return "json"; }
		}

		public async override Task<ICompletionDataList> HandleCodeCompletionAsync (
			CodeCompletionContext completionContext,
			char completionChar,
			CancellationToken token = default (CancellationToken))
		{
			try {
				ResponseMessage response = await host.GetCompletionInfo (Editor.FileName, completionContext);
				if (response == null)
					return null;

				var completionList = response.result.ToObject<CompletionList> ();
				var items = new CompletionDataList ();
				items.TriggerWordLength = 1;
				items.AddRange (completionList.items.Select (item => new LanguageServiceCompletionData (item)));

				return items;
			} catch (Exception ex) {
				LoggingService.LogError ("HandleCodeCompletionAsync error.", ex);
			}
			return null;
		}

		void OnDiagnostics (object sender, DiagnosticEventArgs e)
		{
			if (e.FileName == null || !(Editor.FileName == e.FileName))
				return;

			Runtime.RunInMainThread (() => {
				ShowDiagnostics (e.Diagnostics);
			});
		}

		void ClearDiagnostics ()
		{
			errorMarkers.ForEach (error => Editor.RemoveMarker (error));
			errorMarkers.Clear ();
		}

		void ShowDiagnostics (Diagnostic[] diagnostics)
		{
			ClearDiagnostics ();

			foreach (Error error in diagnostics.Select (diagnostic => CreateError (diagnostic))) {
				IErrorMarker marker = TextMarkerFactory.CreateErrorMarker (Editor, error);
				Editor.AddMarker (marker); 
				errorMarkers.Add (marker);
			}
		}

		Error CreateError (Diagnostic diagnostic)
		{
			return new Error (
				GetErrorType (diagnostic.severity),
				diagnostic.message,
				GetRegion (diagnostic.range)
			);
		}

		ErrorType GetErrorType (DiagnosticSeverity severity)
		{
			switch (severity) {
				case DiagnosticSeverity.Error:
				return ErrorType.Error;

				case DiagnosticSeverity.Warning:
				return ErrorType.Warning;

				default:
				return ErrorType.Unknown;
			}
		}

		DocumentRegion GetRegion (Range range)
		{
			if (range == null) {
				return DocumentRegion.Empty;
			}

			return new DocumentRegion (
				range.start.line + 1,
				range.start.character + 1,
				range.end.line + 1,
				range.end.character + 1
			);
		}

		void TextChanged (object sender, TextChangeEventArgs e)
		{
			version++;
			host.DocumentTextChanged (Editor.FileName, Editor.Text, version);
		}
	}
}

