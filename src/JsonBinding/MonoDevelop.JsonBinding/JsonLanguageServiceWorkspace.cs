//
// JsonLanguageServiceWorkspace.cs
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.JsonBinding
{
	public class JsonLanguageServiceWorkspace : IDisposable
	{
		List<JsonLanguageServiceHost> hosts = new List<JsonLanguageServiceHost> ();

		public void Initialize ()
		{
			IdeApp.Workbench.DocumentOpened += WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed += WorkbenchDocumentClosed;

			//foreach (Document document in IdeApp.Workbench.Documents.Where (doc => IsSupported (doc))) {
			//	WorkbenchDocumentOpened (document);
			//}
		}

		public void Dispose ()
		{
			IdeApp.Workbench.DocumentOpened -= WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed -= WorkbenchDocumentClosed;
		}

		static bool IsSupported (Document document)
		{
			return document.FileName.HasExtension (".json");
		}

		void WorkbenchDocumentOpened (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document))
				WorkbenchDocumentOpened (e.Document);
		}

		void WorkbenchDocumentClosed (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document))
				WorkbenchDocumentClosed (e.Document);
		}

		void WorkbenchDocumentOpened (Document document)
		{
			JsonLanguageServiceHost host = GetHost (document);
			host.OpenDocument (document);
		}

		void WorkbenchDocumentClosed (Document document)
		{
			JsonLanguageServiceHost host = GetHost (document);
			host.CloseDocument (document);
		}

		JsonLanguageServiceHost GetHost (Document document)
		{
			return GetHost (document.Project);
		}

		public JsonLanguageServiceHost GetHost (Project project)
		{
			JsonLanguageServiceHost host = hosts.FirstOrDefault (currentHost => currentHost.Project == project);
			if (host != null) {
				return host;
			}

			host = CreateHost (project);
			hosts.Add (host);

			return host;
		}

		JsonLanguageServiceHost CreateHost (Project project)
		{
			var host = new JsonLanguageServiceHost (project);
			host.Start ();
			return host;
		}
	}
}

