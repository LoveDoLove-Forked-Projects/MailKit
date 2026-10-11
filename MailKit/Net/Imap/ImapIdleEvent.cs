//
// ImapIdleEvent.cs
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

namespace MailKit.Net.Imap {
	/// <summary>
	/// A change notification received while waiting in an <see cref="ImapIdleSession"/>.
	/// </summary>
	/// <remarks>
	/// <para>The base class for the change notifications reported by an <see cref="ImapIdleSession"/>.</para>
	/// <para>Each notification is also raised as the corresponding event on the <see cref="Folder"/>
	/// (such as <see cref="IMailFolder.CountChanged"/>), and the <see cref="Folder"/> properties have
	/// already been updated by the time the notification is returned.</para>
	/// </remarks>
	public abstract class ImapIdleEvent
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="ImapIdleEvent"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="ImapIdleEvent"/>.
		/// </remarks>
		/// <param name="folder">The folder that the change applies to.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folder"/> is <see langword="null" />.
		/// </exception>
		private protected ImapIdleEvent (IMailFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException (nameof (folder));

			Folder = folder;
		}

		/// <summary>
		/// Get the folder that the change applies to.
		/// </summary>
		/// <remarks>
		/// <para>Gets the folder that the change applies to.</para>
		/// <para>This is usually the selected folder, but it may be a different folder if the
		/// <c>NOTIFY</c> extension has been used to request notifications for other folders.</para>
		/// </remarks>
		/// <value>The folder.</value>
		public IMailFolder Folder {
			get; private set;
		}
	}

	/// <summary>
	/// A notification that the number of messages in a folder has changed.
	/// </summary>
	/// <remarks>
	/// <para>Reported when the server sends an <c>EXISTS</c> response, when messages are expunged or
	/// vanish, or when a <c>NOTIFY</c> <c>STATUS</c> update changes the message count of another folder.</para>
	/// <para>If <see cref="Count"/> is greater than <see cref="PreviousCount"/>, new messages have arrived
	/// and can be fetched starting at index <see cref="PreviousCount"/>.</para>
	/// </remarks>
	public sealed class CountChangedIdleEvent : ImapIdleEvent
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="CountChangedIdleEvent"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="CountChangedIdleEvent"/>.
		/// </remarks>
		/// <param name="folder">The folder.</param>
		/// <param name="previousCount">The number of messages in the folder before the change.</param>
		/// <param name="count">The number of messages in the folder after the change.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folder"/> is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <para><paramref name="previousCount"/> is negative.</para>
		/// <para>-or-</para>
		/// <para><paramref name="count"/> is negative.</para>
		/// </exception>
		public CountChangedIdleEvent (IMailFolder folder, int previousCount, int count) : base (folder)
		{
			if (previousCount < 0)
				throw new ArgumentOutOfRangeException (nameof (previousCount));

			if (count < 0)
				throw new ArgumentOutOfRangeException (nameof (count));

			PreviousCount = previousCount;
			Count = count;
		}

		/// <summary>
		/// Get the number of messages in the folder before the change.
		/// </summary>
		/// <remarks>
		/// Gets the number of messages in the folder before the change.
		/// </remarks>
		/// <value>The previous message count.</value>
		public int PreviousCount {
			get; private set;
		}

		/// <summary>
		/// Get the number of messages in the folder after the change.
		/// </summary>
		/// <remarks>
		/// Gets the number of messages in the folder after the change.
		/// </remarks>
		/// <value>The message count.</value>
		public int Count {
			get; private set;
		}
	}

	/// <summary>
	/// A notification that a message was expunged from a folder.
	/// </summary>
	/// <remarks>
	/// <para>Reported when the server sends an <c>EXPUNGE</c> response.</para>
	/// <para>The <see cref="Index"/> is relative to the state of the folder at the time the message was
	/// expunged, which takes into account any previous <see cref="MessageExpungedIdleEvent"/>s in the same
	/// batch, so the events must be processed in order.</para>
	/// </remarks>
	public sealed class MessageExpungedIdleEvent : ImapIdleEvent
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="MessageExpungedIdleEvent"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="MessageExpungedIdleEvent"/>.
		/// </remarks>
		/// <param name="folder">The folder.</param>
		/// <param name="index">The index of the message that was expunged.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folder"/> is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="index"/> is negative.
		/// </exception>
		public MessageExpungedIdleEvent (IMailFolder folder, int index) : base (folder)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException (nameof (index));

			Index = index;
		}

		/// <summary>
		/// Get the index of the message that was expunged.
		/// </summary>
		/// <remarks>
		/// Gets the index of the message that was expunged.
		/// </remarks>
		/// <value>The index of the message.</value>
		public int Index {
			get; private set;
		}
	}

	/// <summary>
	/// A notification that messages have vanished from a folder.
	/// </summary>
	/// <remarks>
	/// Reported when the server sends a <c>VANISHED</c> response, which replaces <c>EXPUNGE</c> responses
	/// once the <c>QRESYNC</c> extension has been enabled.
	/// </remarks>
	public sealed class MessagesVanishedIdleEvent : ImapIdleEvent
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="MessagesVanishedIdleEvent"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="MessagesVanishedIdleEvent"/>.
		/// </remarks>
		/// <param name="folder">The folder.</param>
		/// <param name="uids">The unique identifiers of the messages that vanished.</param>
		/// <param name="earlier"><see langword="true" /> if the messages vanished at some point in the past;
		/// otherwise, <see langword="false" />.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="folder"/> is <see langword="null" />.</para>
		/// <para>-or-</para>
		/// <para><paramref name="uids"/> is <see langword="null" />.</para>
		/// </exception>
		public MessagesVanishedIdleEvent (IMailFolder folder, IList<UniqueId> uids, bool earlier) : base (folder)
		{
			if (uids == null)
				throw new ArgumentNullException (nameof (uids));

			UniqueIds = uids;
			Earlier = earlier;
		}

		/// <summary>
		/// Get the unique identifiers of the messages that vanished.
		/// </summary>
		/// <remarks>
		/// Gets the unique identifiers of the messages that vanished.
		/// </remarks>
		/// <value>The unique identifiers.</value>
		public IList<UniqueId> UniqueIds {
			get; private set;
		}

		/// <summary>
		/// Get whether the messages vanished at some point in the past.
		/// </summary>
		/// <remarks>
		/// <para>Gets whether the messages vanished at some point in the past.</para>
		/// <para>When <see langword="true" />, the messages were removed before the current session and
		/// the <see cref="IMailFolder.Count"/> was not affected.</para>
		/// </remarks>
		/// <value><see langword="true" /> if the messages vanished earlier; otherwise, <see langword="false" />.</value>
		public bool Earlier {
			get; private set;
		}
	}

	/// <summary>
	/// A notification that the state of a message has changed.
	/// </summary>
	/// <remarks>
	/// <para>Reported when the server sends an unsolicited <c>FETCH</c> response, typically because the
	/// flags, keywords, GMail labels, annotations or mod-sequence value of a message changed.</para>
	/// <para>Only the items that the server included in the <c>FETCH</c> response are populated in the
	/// <see cref="Message"/>; check <see cref="IMessageSummary.Fields"/> to see which items are available.</para>
	/// </remarks>
	public sealed class MessageChangedIdleEvent : ImapIdleEvent
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="MessageChangedIdleEvent"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="MessageChangedIdleEvent"/>.
		/// </remarks>
		/// <param name="folder">The folder.</param>
		/// <param name="message">The updated state of the message.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="folder"/> is <see langword="null" />.</para>
		/// <para>-or-</para>
		/// <para><paramref name="message"/> is <see langword="null" />.</para>
		/// </exception>
		public MessageChangedIdleEvent (IMailFolder folder, IMessageSummary message) : base (folder)
		{
			if (message == null)
				throw new ArgumentNullException (nameof (message));

			Message = message;
		}

		/// <summary>
		/// Get the updated state of the message.
		/// </summary>
		/// <remarks>
		/// Gets the updated state of the message, including its <see cref="IMessageSummary.Index"/>.
		/// </remarks>
		/// <value>The message summary.</value>
		public IMessageSummary Message {
			get; private set;
		}
	}

	/// <summary>
	/// A notification that a status value of a folder has changed.
	/// </summary>
	/// <remarks>
	/// <para>Reported when a status value such as <see cref="IMailFolder.Unread"/>,
	/// <see cref="IMailFolder.UidNext"/> or <see cref="IMailFolder.HighestModSeq"/> changes.</para>
	/// <para>Changes to the number of messages are reported as <see cref="CountChangedIdleEvent"/>s instead.</para>
	/// <para>The new value can be read from the corresponding <see cref="ImapIdleEvent.Folder"/> property.</para>
	/// </remarks>
	public sealed class FolderStatusChangedIdleEvent : ImapIdleEvent
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="FolderStatusChangedIdleEvent"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="FolderStatusChangedIdleEvent"/>.
		/// </remarks>
		/// <param name="folder">The folder.</param>
		/// <param name="item">The status item that changed.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folder"/> is <see langword="null" />.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <paramref name="item"/> is <see cref="StatusItems.None"/>.
		/// </exception>
		public FolderStatusChangedIdleEvent (IMailFolder folder, StatusItems item) : base (folder)
		{
			if (item == StatusItems.None)
				throw new ArgumentException ("At least one status item must be specified.", nameof (item));

			Item = item;
		}

		/// <summary>
		/// Get the status item that changed.
		/// </summary>
		/// <remarks>
		/// Gets the status item that changed, such as <see cref="StatusItems.Unread"/>.
		/// </remarks>
		/// <value>The status item.</value>
		public StatusItems Item {
			get; private set;
		}
	}
}
