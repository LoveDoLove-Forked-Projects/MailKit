using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;

namespace ImapIdleSessionExample {
	class Program
	{
		// Connection-related properties
		const SecureSocketOptions SslOptions = SecureSocketOptions.Auto;
		const string Host = "imap.gmail.com";
		const int Port = 993;

		// Authentication-related properties
		const string Username = "username@gmail.com";
		const string Password = "password";

		public static void Main (string[] args)
		{
			using (var client = new IdleClient (Host, Port, SslOptions, Username, Password)) {
				Console.WriteLine ("Hit any key to end the demo.");

				var idleTask = client.RunAsync ();

				Task.Run (() => {
					Console.ReadKey (true);
				}).Wait ();

				client.Exit ();

				idleTask.GetAwaiter ().GetResult ();
			}
		}
	}

	class IdleClient : IDisposable
	{
		readonly string host, username, password;
		readonly SecureSocketOptions sslOptions;
		readonly int port;
		readonly List<IMessageSummary> messages;
		readonly CancellationTokenSource cancel;
		readonly FetchRequest request;
		readonly ImapClient client;

		public IdleClient (string host, int port, SecureSocketOptions sslOptions, string username, string password)
		{
			this.client = new ImapClient (new ProtocolLogger (Console.OpenStandardError ()));
			this.request = new FetchRequest (MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
			this.messages = new List<IMessageSummary> ();
			this.cancel = new CancellationTokenSource ();
			this.sslOptions = sslOptions;
			this.username = username;
			this.password = password;
			this.host = host;
			this.port = port;
		}

		async Task ReconnectAsync ()
		{
			if (!client.IsConnected)
				await client.ConnectAsync (host, port, sslOptions, cancel.Token);

			if (!client.IsAuthenticated) {
				await client.AuthenticateAsync (username, password, cancel.Token);

				await client.Inbox.OpenAsync (FolderAccess.ReadOnly, cancel.Token);
			}
		}

		async Task FetchMessageSummariesAsync (bool print)
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

		async Task<ImapIdleChanges> WaitForChangesAsync (ImapIdleSession session)
		{
			do {
				try {
					// Note: The session uses IDLE when the server supports it (refreshing it periodically
					// so that the server doesn't drop the connection) and falls back to polling with NOOP
					// when it doesn't.
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
			// Note: Events are reported in the order that the server sent them, so applying
			// them in order keeps our 'messages' cache consistent with the folder.
			foreach (var e in changes.Events) {
				switch (e) {
				case CountChangedIdleEvent countChanged:
					Console.WriteLine ("{0}: message count changed from {1} to {2}.", e.Folder, countChanged.PreviousCount, countChanged.Count);
					break;
				case MessageExpungedIdleEvent expunged:
					if (expunged.Index < messages.Count) {
						var message = messages[expunged.Index];

						Console.WriteLine ("{0}: message #{1} has been expunged: {2}", e.Folder, expunged.Index, message.Envelope.Subject);

						// Note: If you are keeping a local cache of message information
						// (e.g. MessageSummary data) for the folder, then you'll need
						// to remove the message at expunged.Index.
						messages.RemoveAt (expunged.Index);
					} else {
						Console.WriteLine ("{0}: message #{1} has been expunged.", e.Folder, expunged.Index);
					}
					break;
				case MessagesVanishedIdleEvent vanished:
					// Note: This is only reported by servers when QRESYNC has been enabled.
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

		async Task IdleAsync ()
		{
			// Note: The default options are suitable for most servers (including GMail), but they
			// can be tweaked by passing an ImapIdleOptions to CreateIdleSession().
			var session = client.CreateIdleSession ();

			// Note: If you don't need to handle reconnecting, this loop can be simplified to:
			//
			// await foreach (var changes in session.ReadChangesAsync (cancel.Token)) { ... }
			try {
				do {
					var changes = await WaitForChangesAsync (session);

					ProcessChanges (changes);

					// Note: Unlike when handling the folder events (such as CountChanged) while
					// idling, it is safe to send new commands here because the IDLE command has
					// already been ended by the time WaitForChangesAsync() returns.
					if (changes.MessagesArrived)
						await FetchMessageSummariesAsync (true);
				} while (true);
			} catch (OperationCanceledException) {
				// Exit() was called. The IDLE command was ended gracefully, so the client is
				// still connected and usable.
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

		public void Exit ()
		{
			cancel.Cancel ();
		}

		public void Dispose ()
		{
			client.Dispose ();
			cancel.Dispose ();
		}
	}
}
