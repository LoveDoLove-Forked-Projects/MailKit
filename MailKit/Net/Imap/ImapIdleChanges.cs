//
// ImapIdleChanges.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MailKit.Net.Imap {
	/// <summary>
	/// A batch of changes returned by an <see cref="ImapIdleSession"/>.
	/// </summary>
	/// <remarks>
	/// A batch of change notifications, in the order that they were received from the server.
	/// </remarks>
	/// <example>
	/// <code language="c#" source="Examples\ImapIdleSessionExample.cs"/>
	/// </example>
	public sealed class ImapIdleChanges
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="ImapIdleChanges"/> class.
		/// </summary>
		/// <remarks>
		/// <para>Creates a new <see cref="ImapIdleChanges"/>.</para>
		/// <para>This constructor is mainly useful for unit testing code that consumes
		/// <see cref="ImapIdleChanges"/>.</para>
		/// </remarks>
		/// <param name="events">The change notifications, in the order that they were received.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="events"/> is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="events"/> is empty.</para>
		/// <para>-or-</para>
		/// <para><paramref name="events"/> contains a <see langword="null" /> element.</para>
		/// </exception>
		public ImapIdleChanges (IEnumerable<ImapIdleEvent> events)
		{
			if (events == null)
				throw new ArgumentNullException (nameof (events));

			var list = new List<ImapIdleEvent> (events);

			if (list.Count == 0)
				throw new ArgumentException ("At least one event must be specified.", nameof (events));

			foreach (var e in list) {
				if (e == null)
					throw new ArgumentException ("The events cannot contain null elements.", nameof (events));

				if (e is CountChangedIdleEvent countChanged && countChanged.Count > countChanged.PreviousCount)
					MessagesArrived = true;
			}

			Events = new ReadOnlyCollection<ImapIdleEvent> (list);
		}

		/// <summary>
		/// Get the change notifications.
		/// </summary>
		/// <remarks>
		/// <para>Gets the change notifications, in the order that they were received from the server.</para>
		/// <para>The order matters: for example, the index of each <see cref="MessageExpungedIdleEvent"/> is
		/// relative to the state of the folder after the preceding expunges.</para>
		/// </remarks>
		/// <value>The change notifications.</value>
		public IReadOnlyList<ImapIdleEvent> Events {
			get; private set;
		}

		/// <summary>
		/// Get whether new messages have arrived.
		/// </summary>
		/// <remarks>
		/// Gets whether any of the <see cref="Events"/> is a <see cref="CountChangedIdleEvent"/> whose
		/// <see cref="CountChangedIdleEvent.Count"/> is greater than its
		/// <see cref="CountChangedIdleEvent.PreviousCount"/>.
		/// </remarks>
		/// <value><see langword="true" /> if new messages have arrived; otherwise, <see langword="false" />.</value>
		public bool MessagesArrived {
			get; private set;
		}
	}
}
