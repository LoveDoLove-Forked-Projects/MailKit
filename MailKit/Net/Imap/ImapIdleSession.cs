//
// ImapIdleSession.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2013-2026 .NET Foundation and Contributors
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MailKit.Net.Imap {
	/// <summary>
	/// A session that waits for changes to be pushed by an IMAP server.
	/// </summary>
	/// <remarks>
	/// <para>An <see cref="ImapIdleSession"/> takes care of the details of waiting for changes:</para>
	/// <list type="bullet">
	/// <item>It uses the <c>IDLE</c> command when the server supports it and periodically restarts it
	/// (see <see cref="ImapIdleOptions.RefreshInterval"/>) so that the connection isn't dropped for
	/// inactivity.</item>
	/// <item>It falls back to polling with the <c>NOOP</c> command when the server does not support
	/// <c>IDLE</c> (see <see cref="ImapIdleOptions.PollInterval"/>).</item>
	/// <item>It collects bursts of notifications into a single <see cref="ImapIdleChanges"/> batch
	/// (see <see cref="ImapIdleOptions.CoalesceDelay"/>).</item>
	/// <item>It ends the <c>IDLE</c> command before returning the changes, so the caller is free to
	/// issue other commands (such as fetching the new messages) while processing them.</item>
	/// </list>
	/// <para>Only changes that are received while <see cref="WaitForChanges(CancellationToken)"/>
	/// (or one of its variants) is running are recorded. Changes that the server reports while other
	/// commands are running are still raised as events on the folders (such as
	/// <see cref="IMailFolder.CountChanged"/>) and are reflected in the folder properties, but they are
	/// not included in the next batch.</para>
	/// <para>Cancelling the cancellation token passed to the wait methods ends the <c>IDLE</c>
	/// command gracefully, which means that the connection remains usable afterward. To abort a wait
	/// that the server never responds to, disconnect the client.</para>
	/// <para>An <see cref="ImapIdleSession"/> does not hold any resources between waits, so it does
	/// not need to be disposed.</para>
	/// </remarks>
	/// <example>
	/// <code language="c#" source="Examples\ImapIdleSessionExample.cs"/>
	/// </example>
	public sealed class ImapIdleSession
	{
		readonly List<ImapIdleEvent> pending = new List<ImapIdleEvent> ();
		readonly ImapIdleOptions options;
		readonly ImapClient client;
		readonly ImapEngine engine;
		CancellationTokenSource? done;
		bool triggered;
		int waiting;

		internal ImapIdleSession (ImapClient client, ImapEngine engine, ImapIdleOptions options)
		{
			this.options = options;
			this.client = client;
			this.engine = engine;
		}

		/// <summary>
		/// Get the client that the session belongs to.
		/// </summary>
		/// <remarks>
		/// Gets the client that the session belongs to.
		/// </remarks>
		/// <value>The IMAP client.</value>
		public IImapClient Client {
			get { return client; }
		}

		internal void OnEvent (ImapIdleEvent e)
		{
			CancellationTokenSource? cancel = null;

			lock (pending) {
				pending.Add (e);

				if (done != null && !triggered) {
					triggered = true;

					if (options.CoalesceDelay > TimeSpan.Zero)
						done.CancelAfter (options.CoalesceDelay);
					else
						cancel = done;
				}
			}

			cancel?.Cancel ();
		}

		void BeginWait (CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref waiting, 1, 0) != 0)
				throw new InvalidOperationException ("The ImapIdleSession is already waiting for changes.");

			if (cancellationToken.IsCancellationRequested) {
				waiting = 0;
				cancellationToken.ThrowIfCancellationRequested ();
			}
		}

		void EndWait ()
		{
			Interlocked.Exchange (ref waiting, 0);
		}

		void Attach ()
		{
			if (engine.IdleSession != null)
				throw new InvalidOperationException ("Another ImapIdleSession is already waiting for changes.");

			engine.IdleSession = this;
		}

		void Detach ()
		{
			if (engine.IdleSession == this)
				engine.IdleSession = null;
		}

		CancellationTokenSource BeginIdle ()
		{
			Attach ();

			var source = new CancellationTokenSource ();

			lock (pending) {
				triggered = false;
				done = source;
			}

			source.CancelAfter (options.RefreshInterval);

			return source;
		}

		void EndIdle (CancellationTokenSource source)
		{
			lock (pending) {
				done = null;
			}

			Detach ();
			source.Dispose ();
		}

		static void CancelIdle (object? state)
		{
			((CancellationTokenSource) state!).Cancel ();
		}

		bool TryTakeChanges (out ImapIdleChanges? changes)
		{
			lock (pending) {
				if (pending.Count == 0) {
					changes = null;
					return false;
				}

				changes = new ImapIdleChanges (pending);
				pending.Clear ();
				return true;
			}
		}

		bool SupportsIdle {
			get { return engine.Capabilities.Contains (ImapCapability.Idle); }
		}

		/// <summary>
		/// Wait for the server to report changes.
		/// </summary>
		/// <remarks>
		/// <para>Waits until the server reports one or more changes, using the <c>IDLE</c> command if
		/// the server supports it or by polling with the <c>NOOP</c> command otherwise.</para>
		/// <para>When this method returns, the client is no longer idle, so other commands may be issued.</para>
		/// <para>A folder must be open before calling this method.</para>
		/// </remarks>
		/// <example>
		/// <code language="c#" source="Examples\ImapIdleSessionExample.cs"/>
		/// </example>
		/// <param name="cancellationToken">The cancellation token used to stop waiting. Cancelling it ends
		/// the <c>IDLE</c> command gracefully, so the client remains usable.</param>
		/// <returns>The changes reported by the server.</returns>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>A <see cref="ImapFolder"/> has not been opened.</para>
		/// <para>-or-</para>
		/// <para>The session (or another session for the same client) is already waiting for changes.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied to the IDLE or NOOP command with a NO or BAD response.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		public ImapIdleChanges WaitForChanges (CancellationToken cancellationToken = default)
		{
			ImapIdleChanges? changes;

			client.CheckCanWaitForChanges ();
			BeginWait (cancellationToken);

			try {
				if (TryTakeChanges (out changes))
					return changes!;

				if (SupportsIdle) {
					do {
						var source = BeginIdle ();

						try {
							using (cancellationToken.Register (CancelIdle, source))
								client.Idle (source.Token, CancellationToken.None);
						} finally {
							EndIdle (source);
						}

						cancellationToken.ThrowIfCancellationRequested ();
					} while (!TryTakeChanges (out changes));
				} else {
					do {
						Attach ();

						try {
							client.NoOp (CancellationToken.None);
						} finally {
							Detach ();
						}

						if (TryTakeChanges (out changes))
							break;

						if (cancellationToken.CanBeCanceled) {
							if (cancellationToken.WaitHandle.WaitOne (options.PollInterval))
								cancellationToken.ThrowIfCancellationRequested ();
						} else {
							Thread.Sleep (options.PollInterval);
						}
					} while (true);
				}

				return changes!;
			} finally {
				EndWait ();
			}
		}

		/// <summary>
		/// Asynchronously wait for the server to report changes.
		/// </summary>
		/// <remarks>
		/// <para>Waits until the server reports one or more changes, using the <c>IDLE</c> command if
		/// the server supports it or by polling with the <c>NOOP</c> command otherwise.</para>
		/// <para>When the returned task completes, the client is no longer idle, so other commands may be issued.</para>
		/// <para>A folder must be open before calling this method.</para>
		/// </remarks>
		/// <example>
		/// <code language="c#" source="Examples\ImapIdleSessionExample.cs"/>
		/// </example>
		/// <param name="cancellationToken">The cancellation token used to stop waiting. Cancelling it ends
		/// the <c>IDLE</c> command gracefully, so the client remains usable.</param>
		/// <returns>The changes reported by the server.</returns>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>A <see cref="ImapFolder"/> has not been opened.</para>
		/// <para>-or-</para>
		/// <para>The session (or another session for the same client) is already waiting for changes.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied to the IDLE or NOOP command with a NO or BAD response.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		public async Task<ImapIdleChanges> WaitForChangesAsync (CancellationToken cancellationToken = default)
		{
			ImapIdleChanges? changes;

			client.CheckCanWaitForChanges ();
			BeginWait (cancellationToken);

			try {
				if (TryTakeChanges (out changes))
					return changes!;

				if (SupportsIdle) {
					do {
						var source = BeginIdle ();

						try {
							using (cancellationToken.Register (CancelIdle, source))
								await client.IdleAsync (source.Token, CancellationToken.None).ConfigureAwait (false);
						} finally {
							EndIdle (source);
						}

						cancellationToken.ThrowIfCancellationRequested ();
					} while (!TryTakeChanges (out changes));
				} else {
					do {
						Attach ();

						try {
							await client.NoOpAsync (CancellationToken.None).ConfigureAwait (false);
						} finally {
							Detach ();
						}

						if (TryTakeChanges (out changes))
							break;

						await Task.Delay (options.PollInterval, cancellationToken).ConfigureAwait (false);
					} while (true);
				}

				return changes!;
			} finally {
				EndWait ();
			}
		}

		/// <summary>
		/// Asynchronously read batches of changes as they are reported by the server.
		/// </summary>
		/// <remarks>
		/// <para>Repeatedly calls <see cref="WaitForChangesAsync(CancellationToken)"/> and yields each batch
		/// of changes.</para>
		/// <para>While the caller is processing a batch, the client is not idle, so other commands may be
		/// issued. Requesting the next batch resumes waiting for changes.</para>
		/// <para>The enumeration only ends by throwing an exception, such as an
		/// <see cref="System.OperationCanceledException"/> when the <paramref name="cancellationToken"/>
		/// is cancelled.</para>
		/// </remarks>
		/// <example>
		/// <code language="c#" source="Examples\ImapIdleSessionExample.cs"/>
		/// </example>
		/// <param name="cancellationToken">The cancellation token used to stop waiting. Cancelling it ends
		/// the <c>IDLE</c> command gracefully, so the client remains usable.</param>
		/// <returns>The batches of changes reported by the server.</returns>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="ImapClient"/> has been disposed.
		/// </exception>
		/// <exception cref="ServiceNotConnectedException">
		/// The <see cref="ImapClient"/> is not connected.
		/// </exception>
		/// <exception cref="ServiceNotAuthenticatedException">
		/// The <see cref="ImapClient"/> is not authenticated.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>A <see cref="ImapFolder"/> has not been opened.</para>
		/// <para>-or-</para>
		/// <para>The session (or another session for the same client) is already waiting for changes.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ImapCommandException">
		/// The server replied to the IDLE or NOOP command with a NO or BAD response.
		/// </exception>
		/// <exception cref="ImapProtocolException">
		/// The server responded with an unexpected token.
		/// </exception>
		public async IAsyncEnumerable<ImapIdleChanges> ReadChangesAsync ([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			while (true)
				yield return await WaitForChangesAsync (cancellationToken).ConfigureAwait (false);
		}
	}
}
