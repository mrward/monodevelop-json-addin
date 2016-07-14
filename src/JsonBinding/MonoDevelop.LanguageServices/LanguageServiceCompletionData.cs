//
// LanguageServiceCompletionData.cs
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

using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.LanguageServices.Messages;

namespace MonoDevelop.LanguageServices
{
	public class LanguageServiceCompletionData : CompletionData
	{
		CompletionItem completionItem;

		public LanguageServiceCompletionData (CompletionItem completionItem)
		{
			this.completionItem = completionItem;
			Icon = GetIcon (completionItem);
			CompletionText = completionItem.insertText.Replace("{{", "").Replace ("}}", "");
		}

		public override string DisplayText {
			get {
				return completionItem.label;
			}
			set {
				base.DisplayText = value;
			}
		}

		public override string Description {
			get {
				return completionItem.documentation;
			}
			set {
				base.Description = value;
			}
		}

		IconId GetIcon (CompletionItem item)
		{
			switch (item.kind) {
				case CompletionItemKind.Property:
				return Stock.Property;

				case CompletionItemKind.Constructor:
				case CompletionItemKind.Method:
				case CompletionItemKind.Function:
				return Stock.Method;

				case CompletionItemKind.Text:
				case CompletionItemKind.Keyword:
				return Stock.Literal;

				case CompletionItemKind.Class:
				return Stock.Class;

				case CompletionItemKind.Field:
				return Stock.PrivateField;

				case CompletionItemKind.Interface:
				return Stock.Interface;

				case CompletionItemKind.Module:
				return Stock.NameSpace;

				case CompletionItemKind.Enum:
				return Stock.Enum;

				default:
				return null;
			}
		}
	}
}

