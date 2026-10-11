//
// ImapIdleOptions.cs
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

namespace MailKit.Net.Imap {
	/// <summary>
	/// Options for an <see cref="ImapIdleSession"/>.
	/// </summary>
	/// <remarks>
	/// <para>Options that control how an <see cref="ImapIdleSession"/> waits for changes.</para>
	/// <para>The option values are copied when the session is created by
	/// <see cref="IImapClient.CreateIdleSession(ImapIdleOptions?)"/>, so changing them afterward
	/// has no effect on existing sessions.</para>
	/// </remarks>
	public sealed class ImapIdleOptions
	{
		/// <summary>
		/// The default value of the <see cref="RefreshInterval"/> property (9 minutes).
		/// </summary>
		/// <remarks>
		/// The default value of the <see cref="RefreshInterval"/> property (9 minutes).
		/// </remarks>
		public static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromMinutes (9);

		/// <summary>
		/// The default value of the <see cref="PollInterval"/> property (1 minute).
		/// </summary>
		/// <remarks>
		/// The default value of the <see cref="PollInterval"/> property (1 minute).
		/// </remarks>
		public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMinutes (1);

		/// <summary>
		/// The default value of the <see cref="CoalesceDelay"/> property (250 milliseconds).
		/// </summary>
		/// <remarks>
		/// The default value of the <see cref="CoalesceDelay"/> property (250 milliseconds).
		/// </remarks>
		public static readonly TimeSpan DefaultCoalesceDelay = TimeSpan.FromMilliseconds (250);

		TimeSpan refreshInterval = DefaultRefreshInterval;
		TimeSpan pollInterval = DefaultPollInterval;
		TimeSpan coalesceDelay = DefaultCoalesceDelay;

		/// <summary>
		/// Initialize a new instance of the <see cref="ImapIdleOptions"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new set of <see cref="ImapIdleOptions"/> with default values.
		/// </remarks>
		public ImapIdleOptions ()
		{
		}

		/// <summary>
		/// Get or set the interval at which the <c>IDLE</c> command is restarted.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets how long a single <c>IDLE</c> command may run before the session ends it
		/// and issues a new one.</para>
		/// <para><a href="https://tools.ietf.org/html/rfc2177">rfc2177</a> requires clients to restart
		/// <c>IDLE</c> at least every 29 minutes to avoid being logged off for inactivity, and many NAT
		/// gateways and firewalls drop idle TCP connections much sooner than that, which is why the default
		/// is <see cref="DefaultRefreshInterval"/>.</para>
		/// </remarks>
		/// <value>The refresh interval.</value>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="value"/> is less than or equal to <see cref="TimeSpan.Zero"/> or greater than
		/// <see cref="int.MaxValue"/> milliseconds.
		/// </exception>
		public TimeSpan RefreshInterval {
			get { return refreshInterval; }
			set {
				ValidatePositive (value, nameof (value));
				refreshInterval = value;
			}
		}

		/// <summary>
		/// Get or set the interval at which the server is polled when it does not support <c>IDLE</c>.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets how long the session waits between <c>NOOP</c> commands when the server does
		/// not support the <c>IDLE</c> extension.</para>
		/// </remarks>
		/// <value>The poll interval.</value>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="value"/> is less than or equal to <see cref="TimeSpan.Zero"/> or greater than
		/// <see cref="int.MaxValue"/> milliseconds.
		/// </exception>
		public TimeSpan PollInterval {
			get { return pollInterval; }
			set {
				ValidatePositive (value, nameof (value));
				pollInterval = value;
			}
		}

		/// <summary>
		/// Get or set how long to keep listening for more changes after the first change arrives.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets how long the session continues to idle after receiving the first change
		/// notification so that bursts of notifications (for example, when hundreds of messages are moved
		/// into the folder at once) are returned as a single <see cref="ImapIdleChanges"/> batch instead of
		/// costing one <c>DONE</c>/<c>IDLE</c> round trip per notification.</para>
		/// <para>A value of <see cref="TimeSpan.Zero"/> ends the <c>IDLE</c> command as soon as the first
		/// change arrives.</para>
		/// </remarks>
		/// <value>The coalesce delay.</value>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="value"/> is less than <see cref="TimeSpan.Zero"/> or greater than
		/// <see cref="int.MaxValue"/> milliseconds.
		/// </exception>
		public TimeSpan CoalesceDelay {
			get { return coalesceDelay; }
			set {
				if (value < TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
					throw new ArgumentOutOfRangeException (nameof (value));

				coalesceDelay = value;
			}
		}

		static void ValidatePositive (TimeSpan value, string paramName)
		{
			if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
				throw new ArgumentOutOfRangeException (paramName);
		}

		internal ImapIdleOptions Clone ()
		{
			return new ImapIdleOptions {
				refreshInterval = refreshInterval,
				pollInterval = pollInterval,
				coalesceDelay = coalesceDelay
			};
		}
	}
}
