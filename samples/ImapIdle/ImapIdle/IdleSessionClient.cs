//
// IdleSessionClient.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014-2026 Jeffrey Stedfast
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
using System.IO;
using System.Threading.Tasks;

using MailKit;
using MailKit.Net.Imap;

namespace ImapIdle
{
	/// <summary>
	/// Demonstrates the <see cref="ImapIdleSession"/> way of using IMAP IDLE: no event handlers,
	/// no <c>done</c> token and no re-entrancy concerns. The session handles the IDLE refresh
	/// interval and the NOOP fallback for servers without IDLE, and hands back a batch of changes
	/// that can be processed (including issuing new commands) once IDLE has ended.
	/// </summary>
	public class IdleSessionClient : IdleClientBase
	{
		async Task<ImapIdleChanges> WaitForChangesAsync (ImapIdleSession session)
		{
			do {
				try {
					return await session.WaitForChangesAsync (cancel.Token);
				} catch (ImapProtocolException) {
					// protocol exceptions often result in the client getting disconnected
					await ReconnectAsync ();
				} catch (IOException) {
					// I/O exceptions always result in the client getting disconnected
					await ReconnectAsync ();
				}
			} while (true);
		}

		void ProcessChanges (ImapIdleChanges changes)
		{
			// Note: events are reported in the order that the server sent them, so applying
			// them in order keeps our 'messages' cache consistent with the folder.
			foreach (var e in changes.Events) {
				switch (e) {
				case CountChangedIdleEvent countChanged:
					Console.WriteLine ("{0}: message count changed from {1} to {2}.", e.Folder, countChanged.PreviousCount, countChanged.Count);
					break;
				case MessageExpungedIdleEvent expunged:
					RemoveExpungedMessage (e.Folder, expunged.Index);
					break;
				case MessagesVanishedIdleEvent vanished:
					// Note: only sent by servers when QRESYNC has been enabled.
					Console.WriteLine ("{0}: {1} messages have vanished.", e.Folder, vanished.UniqueIds.Count);
					break;
				case MessageChangedIdleEvent changed:
					if (changed.Message.Flags.HasValue)
						Console.WriteLine ("{0}: flags have changed for message #{1} ({2}).", e.Folder, changed.Message.Index, changed.Message.Flags.Value);
					break;
				case FolderStatusChangedIdleEvent statusChanged:
					Console.WriteLine ("{0}: folder status changed ({1}).", e.Folder, statusChanged.Item);
					break;
				}
			}
		}

		protected override async Task IdleAsync ()
		{
			// Note: The defaults are suitable for most servers (including GMail), but can be
			// tweaked by passing an ImapIdleOptions to CreateIdleSession().
			var session = client.CreateIdleSession ();

			// Note: If you don't need to handle reconnecting, this loop can be simplified to:
			//
			// await foreach (var changes in session.ReadChangesAsync (cancel.Token)) { ... }
			try {
				do {
					var changes = await WaitForChangesAsync (session);

					ProcessChanges (changes);

					// Unlike within an event handler, it is safe to send new commands here
					// because the IDLE command has already completed.
					if (changes.MessagesArrived)
						await FetchMessageSummariesAsync (true);
				} while (true);
			} catch (OperationCanceledException) {
				// Exit() was called
			}
		}
	}
}
