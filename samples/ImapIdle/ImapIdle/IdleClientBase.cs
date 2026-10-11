//
// IdleClientBase.cs
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
using System.Collections.Generic;

using MailKit;
using MailKit.Net.Imap;

namespace ImapIdle
{
	/// <summary>
	/// Shared plumbing for both IDLE demos: connecting, reconnecting and keeping a
	/// local cache of message summaries for the Inbox.
	/// </summary>
	public abstract class IdleClientBase : IDisposable
	{
		protected readonly List<IMessageSummary> messages;
		protected readonly CancellationTokenSource cancel;
		protected readonly FetchRequest request;
		protected readonly ImapClient client;

		protected IdleClientBase ()
		{
			client = new ImapClient (new ProtocolLogger (Console.OpenStandardError ()));
			request = new FetchRequest (MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
			messages = new List<IMessageSummary> ();
			cancel = new CancellationTokenSource ();
		}

		protected async Task ReconnectAsync ()
		{
			if (!client.IsConnected)
				await client.ConnectAsync (Program.Host, Program.Port, Program.SslOptions, cancel.Token);

			if (!client.IsAuthenticated) {
				await client.AuthenticateAsync (Program.Username, Program.Password, cancel.Token);

				await client.Inbox.OpenAsync (FolderAccess.ReadOnly, cancel.Token);
			}
		}

		protected async Task FetchMessageSummariesAsync (bool print)
		{
			IList<IMessageSummary> fetched;

			do {
				try {
					// fetch summary information for messages that we don't already have
					int startIndex = messages.Count;

					fetched = await client.Inbox.FetchAsync (startIndex, -1, request, cancel.Token);
					break;
				} catch (ImapProtocolException) {
					// protocol exceptions often result in the client getting disconnected
					await ReconnectAsync ();
				} catch (IOException) {
					// I/O exceptions always result in the client getting disconnected
					await ReconnectAsync ();
				}
			} while (true);

			foreach (var message in fetched) {
				if (print)
					Console.WriteLine ("{0}: new message: {1}", client.Inbox, message.Envelope.Subject);
				messages.Add (message);
			}
		}

		protected void RemoveExpungedMessage (IMailFolder folder, int index)
		{
			if (index < messages.Count) {
				var message = messages[index];

				Console.WriteLine ("{0}: message #{1} has been expunged: {2}", folder, index, message.Envelope.Subject);

				// Note: If you are keeping a local cache of message information
				// (e.g. MessageSummary data) for the folder, then you'll need
				// to remove the message at the expunged index.
				messages.RemoveAt (index);
			} else {
				Console.WriteLine ("{0}: message #{1} has been expunged.", folder, index);
			}
		}

		public async Task RunAsync ()
		{
			// connect to the IMAP server and get our initial list of messages
			try {
				await ReconnectAsync ();
				await FetchMessageSummariesAsync (false);
			} catch (OperationCanceledException) {
				await client.DisconnectAsync (true);
				return;
			}

			await IdleAsync ();

			await client.DisconnectAsync (true);
		}

		/// <summary>
		/// Wait for and process changes to the Inbox until <see cref="Exit"/> is called.
		/// </summary>
		protected abstract Task IdleAsync ();

		public void Exit ()
		{
			cancel.Cancel ();
		}

		public void Dispose ()
		{
			client.Dispose ();
			cancel.Dispose ();

			GC.SuppressFinalize (this);
		}
	}
}
