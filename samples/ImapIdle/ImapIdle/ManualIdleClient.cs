//
// ManualIdleClient.cs
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
using System.Threading;
using System.Threading.Tasks;

using MailKit;
using MailKit.Net.Imap;

namespace ImapIdle
{
	/// <summary>
	/// Demonstrates the "classic" way of using IMAP IDLE: subscribe to the folder events,
	/// call <see cref="ImapClient.IdleAsync(CancellationToken, CancellationToken)"/> in a loop
	/// and cancel the <c>done</c> token from within an event handler when there is work to do.
	/// </summary>
	public class ManualIdleClient : IdleClientBase
	{
		CancellationTokenSource done;
		bool messagesArrived;

		async Task WaitForNewMessagesAsync ()
		{
			do {
				try {
					if (client.Capabilities.Contains (ImapCapability.Idle)) {
						// Note: IMAP servers are only supposed to drop the connection after 30 minutes, so normally
						// we'd IDLE for a max of, say, ~29 minutes... but GMail seems to drop idle connections after
						// about 10 minutes, so we'll only idle for 9 minutes.
						done = new CancellationTokenSource (TimeSpan.FromMinutes (9));
						try {
							await client.IdleAsync (done.Token, cancel.Token);
						} finally {
							done.Dispose ();
							done = null;
						}
					} else {
						// Note: we don't want to spam the IMAP server with NOOP commands, so lets wait a minute
						// between each NOOP command.
						await Task.Delay (TimeSpan.FromMinutes (1), cancel.Token);
						await client.NoOpAsync (cancel.Token);
					}
					break;
				} catch (ImapProtocolException) {
					// protocol exceptions often result in the client getting disconnected
					await ReconnectAsync ();
				} catch (IOException) {
					// I/O exceptions always result in the client getting disconnected
					await ReconnectAsync ();
				}
			} while (true);
		}

		protected override async Task IdleAsync ()
		{
			// Note: We capture client.Inbox here because cancelling IdleAsync() *may* require
			// disconnecting the IMAP client connection, and, if it does, the `client.Inbox`
			// property will no longer be accessible which means we won't be able to disconnect
			// our event handlers.
			var inbox = client.Inbox;

			// keep track of changes to the number of messages in the folder (this is how we'll tell if new messages have arrived).
			inbox.CountChanged += OnCountChanged;

			// keep track of messages being expunged so that when the CountChanged event fires, we can tell if it's
			// because new messages have arrived vs messages being removed (or some combination of the two).
			inbox.MessageExpunged += OnMessageExpunged;

			// keep track of flag changes
			inbox.MessageFlagsChanged += OnMessageFlagsChanged;

			try {
				do {
					try {
						await WaitForNewMessagesAsync ();

						if (messagesArrived) {
							await FetchMessageSummariesAsync (true);
							messagesArrived = false;
						}
					} catch (OperationCanceledException) {
						break;
					}
				} while (!cancel.IsCancellationRequested);
			} finally {
				inbox.MessageFlagsChanged -= OnMessageFlagsChanged;
				inbox.MessageExpunged -= OnMessageExpunged;
				inbox.CountChanged -= OnCountChanged;
			}
		}

		// Note: the CountChanged event will fire when new messages arrive in the folder and/or when messages are expunged.
		void OnCountChanged (object sender, EventArgs e)
		{
			var folder = (ImapFolder) sender;

			// Note: because we are keeping track of the MessageExpunged event and updating our
			// 'messages' list, we know that if we get a CountChanged event and folder.Count is
			// larger than messages.Count, then it means that new messages have arrived.
			if (folder.Count > messages.Count) {
				int arrived = folder.Count - messages.Count;

				if (arrived > 1)
					Console.WriteLine ("\t{0} new messages have arrived.", arrived);
				else
					Console.WriteLine ("\t1 new message has arrived.");

				// Note: your first instinct may be to fetch these new messages now, but you cannot do
				// that in this event handler (the ImapFolder is not re-entrant).
				//
				// Instead, cancel the `done` token and update our state so that we know new messages
				// have arrived. We'll fetch the summaries for these new messages later...
				messagesArrived = true;
				done?.Cancel ();
			}
		}

		void OnMessageExpunged (object sender, MessageEventArgs e)
		{
			RemoveExpungedMessage ((IMailFolder) sender, e.Index);
		}

		void OnMessageFlagsChanged (object sender, MessageFlagsChangedEventArgs e)
		{
			var folder = (ImapFolder) sender;

			Console.WriteLine ("{0}: flags have changed for message #{1} ({2}).", folder, e.Index, e.Flags);
		}
	}
}
