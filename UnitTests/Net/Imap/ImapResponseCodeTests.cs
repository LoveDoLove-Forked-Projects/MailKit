//
// ImapResponseCodeTests.cs
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

using MailKit;
using MailKit.Net.Imap;

namespace UnitTests.Net.Imap {
	[TestFixture]
	public class ImapResponseCodeTests
	{
		static void AssertCommon (ImapResponseCode code, ImapResponseCodeType type, string atom, bool isError, bool isTagged, string message)
		{
			Assert.That (code.Type, Is.EqualTo (type), "Type");
			Assert.That (code.Atom, Is.EqualTo (atom), "Atom");
			Assert.That (code.IsError, Is.EqualTo (isError), "IsError");
			Assert.That (code.IsTagged, Is.EqualTo (isTagged), "IsTagged");
			Assert.That (code.Message, Is.EqualTo (message), "Message");
		}

		[TestCase ("ALERT", ImapResponseCodeType.Alert, false)]
		[TestCase ("BADCHARSET", ImapResponseCodeType.BadCharset, true)]
		[TestCase ("CAPABILITY", ImapResponseCodeType.Capability, false)]
		[TestCase ("PARSE", ImapResponseCodeType.Parse, true)]
		[TestCase ("READ-ONLY", ImapResponseCodeType.ReadOnly, false)]
		[TestCase ("READ-WRITE", ImapResponseCodeType.ReadWrite, false)]
		[TestCase ("TRYCREATE", ImapResponseCodeType.TryCreate, true)]
		[TestCase ("REFERRAL", ImapResponseCodeType.Referral, false)]
		[TestCase ("UNKNOWN-CTE", ImapResponseCodeType.UnknownCte, true)]
		[TestCase ("UIDNOTSTICKY", ImapResponseCodeType.UidNotSticky, false)]
		[TestCase ("URLMECH", ImapResponseCodeType.UrlMech, false)]
		[TestCase ("TOOBIG", ImapResponseCodeType.TooBig, true)]
		[TestCase ("NOMODSEQ", ImapResponseCodeType.NoModSeq, false)]
		[TestCase ("COMPRESSIONACTIVE", ImapResponseCodeType.CompressionActive, true)]
		[TestCase ("CLOSED", ImapResponseCodeType.Closed, false)]
		[TestCase ("NOTSAVED", ImapResponseCodeType.NotSaved, true)]
		[TestCase ("BADCOMPARATOR", ImapResponseCodeType.BadComparator, true)]
		[TestCase ("TEMPFAIL", ImapResponseCodeType.TempFail, true)]
		[TestCase ("NOTIFICATIONOVERFLOW", ImapResponseCodeType.NotificationOverflow, false)]
		[TestCase ("BADEVENT", ImapResponseCodeType.BadEvent, true)]
		[TestCase ("UNAVAILABLE", ImapResponseCodeType.Unavailable, true)]
		[TestCase ("AUTHENTICATIONFAILED", ImapResponseCodeType.AuthenticationFailed, true)]
		[TestCase ("AUTHORIZATIONFAILED", ImapResponseCodeType.AuthorizationFailed, true)]
		[TestCase ("EXPIRED", ImapResponseCodeType.Expired, true)]
		[TestCase ("PRIVACYREQUIRED", ImapResponseCodeType.PrivacyRequired, true)]
		[TestCase ("CONTACTADMIN", ImapResponseCodeType.ContactAdmin, true)]
		[TestCase ("NOPERM", ImapResponseCodeType.NoPerm, true)]
		[TestCase ("INUSE", ImapResponseCodeType.InUse, true)]
		[TestCase ("EXPUNGEISSUED", ImapResponseCodeType.ExpungeIssued, true)]
		[TestCase ("CORRUPTION", ImapResponseCodeType.Corruption, true)]
		[TestCase ("SERVERBUG", ImapResponseCodeType.ServerBug, true)]
		[TestCase ("CLIENTBUG", ImapResponseCodeType.ClientBug, true)]
		[TestCase ("CANNOT", ImapResponseCodeType.CanNot, true)]
		[TestCase ("LIMIT", ImapResponseCodeType.Limit, true)]
		[TestCase ("OVERQUOTA", ImapResponseCodeType.OverQuota, true)]
		[TestCase ("ALREADYEXISTS", ImapResponseCodeType.AlreadyExists, true)]
		[TestCase ("NONEXISTENT", ImapResponseCodeType.NonExistent, true)]
		[TestCase ("USEATTR", ImapResponseCodeType.UseAttr, true)]
		[TestCase ("overquota", ImapResponseCodeType.OverQuota, true)]
		[TestCase ("X-UNKNOWN-CODE", ImapResponseCodeType.Unknown, true)]
		public void TestConstructor (string atom, ImapResponseCodeType type, bool isError)
		{
			var code = new ImapResponseCode (atom, true, "message");
			AssertCommon (code, type, atom, isError, true, "message");

			code = new ImapResponseCode (atom, false, string.Empty);
			AssertCommon (code, type, atom, isError, false, string.Empty);
		}

		[TestCase ("NEWNAME")]
		[TestCase ("PERMANENTFLAGS")]
		[TestCase ("UIDNEXT")]
		[TestCase ("UIDVALIDITY")]
		[TestCase ("UNSEEN")]
		[TestCase ("APPENDUID")]
		[TestCase ("COPYUID")]
		[TestCase ("BADURL")]
		[TestCase ("HIGHESTMODSEQ")]
		[TestCase ("MODIFIED")]
		[TestCase ("ANNOTATE")]
		[TestCase ("ANNOTATIONS")]
		[TestCase ("MAXCONVERTMESSAGES")]
		[TestCase ("MAXCONVERTPARTS")]
		[TestCase ("NOUPDATE")]
		[TestCase ("METADATA")]
		[TestCase ("UNDEFINED-FILTER")]
		[TestCase ("MAILBOXID")]
		[TestCase ("WEBALERT")]
		[TestCase ("copyuid")]
		public void TestConstructorRequiresSubclass (string atom)
		{
			Assert.Throws<ArgumentException> (() => new ImapResponseCode (atom, true, "message"));
		}

		[Test]
		public void TestConstructorArgumentExceptions ()
		{
			Assert.Throws<ArgumentNullException> (() => new ImapResponseCode (null, true, "message"));
			Assert.Throws<ArgumentException> (() => new ImapResponseCode (string.Empty, true, "message"));
			Assert.Throws<ArgumentNullException> (() => new ImapResponseCode ("OVERQUOTA", true, null));
		}

		[Test]
		public void TestNewNameResponseCode ()
		{
			var code = new NewNameResponseCode ("Old", "New", false, "renamed");

			AssertCommon (code, ImapResponseCodeType.NewName, "NEWNAME", false, false, "renamed");
			Assert.That (code.OldName, Is.EqualTo ("Old"), "OldName");
			Assert.That (code.NewName, Is.EqualTo ("New"), "NewName");

			Assert.Throws<ArgumentNullException> (() => new NewNameResponseCode (null, "New", false, "renamed"));
			Assert.Throws<ArgumentNullException> (() => new NewNameResponseCode ("Old", null, false, "renamed"));
			Assert.Throws<ArgumentNullException> (() => new NewNameResponseCode ("Old", "New", false, null));
		}

		[Test]
		public void TestPermanentFlagsResponseCode ()
		{
			var keywords = new List<string> { "$Forwarded", "$Junk", "$Forwarded" };
			var code = new PermanentFlagsResponseCode (MessageFlags.Seen | MessageFlags.UserDefined, keywords, false, "Limited");

			AssertCommon (code, ImapResponseCodeType.PermanentFlags, "PERMANENTFLAGS", false, false, "Limited");
			Assert.That (code.Flags, Is.EqualTo (MessageFlags.Seen | MessageFlags.UserDefined), "Flags");
			Assert.That (code.Keywords, Has.Count.EqualTo (2), "Keywords.Count");
			Assert.That (code.Keywords.Contains ("$Forwarded"), Is.True, "$Forwarded");
			Assert.That (code.Keywords.Contains ("$Junk"), Is.True, "$Junk");
			Assert.That (code.Keywords.Contains ("$junk"), Is.False, "Keywords are case-sensitive");

			// the response code should take a snapshot of the keywords
			keywords.Clear ();
			Assert.That (code.Keywords, Has.Count.EqualTo (2), "Keywords snapshot");

			Assert.Throws<ArgumentNullException> (() => new PermanentFlagsResponseCode (MessageFlags.None, null, false, "Limited"));
			Assert.Throws<ArgumentNullException> (() => new PermanentFlagsResponseCode (MessageFlags.None, Array.Empty<string> (), false, null));
		}

		[Test]
		public void TestUidNextResponseCode ()
		{
			var code = new UidNextResponseCode (new UniqueId (42), false, "Predicted next UID");

			AssertCommon (code, ImapResponseCodeType.UidNext, "UIDNEXT", false, false, "Predicted next UID");
			Assert.That (code.Uid, Is.EqualTo (new UniqueId (42)), "Uid");

			Assert.Throws<ArgumentNullException> (() => new UidNextResponseCode (new UniqueId (42), false, null));
		}

		[Test]
		public void TestUidValidityResponseCode ()
		{
			var code = new UidValidityResponseCode (12345, false, "UIDs valid");

			AssertCommon (code, ImapResponseCodeType.UidValidity, "UIDVALIDITY", false, false, "UIDs valid");
			Assert.That (code.UidValidity, Is.EqualTo (12345), "UidValidity");

			Assert.Throws<ArgumentNullException> (() => new UidValidityResponseCode (12345, false, null));
		}

		[Test]
		public void TestUnseenResponseCode ()
		{
			var code = new UnseenResponseCode (7, false, "First unseen");

			AssertCommon (code, ImapResponseCodeType.Unseen, "UNSEEN", false, false, "First unseen");
			Assert.That (code.Index, Is.EqualTo (7), "Index");

			Assert.Throws<ArgumentOutOfRangeException> (() => new UnseenResponseCode (-1, false, "First unseen"));
			Assert.Throws<ArgumentNullException> (() => new UnseenResponseCode (7, false, null));
		}

		[Test]
		public void TestAppendUidResponseCode ()
		{
			var uids = new UniqueIdSet (new [] { new UniqueId (5), new UniqueId (6) });
			var code = new AppendUidResponseCode (12345, uids, true, "Append completed");

			AssertCommon (code, ImapResponseCodeType.AppendUid, "APPENDUID", false, true, "Append completed");
			Assert.That (code, Is.InstanceOf<UidValidityResponseCode> (), "UidValidityResponseCode");
			Assert.That (code.UidValidity, Is.EqualTo (12345), "UidValidity");
			Assert.That (code.UidSet, Is.SameAs (uids), "UidSet");

			Assert.Throws<ArgumentNullException> (() => new AppendUidResponseCode (12345, null, true, "Append completed"));
			Assert.Throws<ArgumentNullException> (() => new AppendUidResponseCode (12345, uids, true, null));
		}

		[Test]
		public void TestCopyUidResponseCode ()
		{
			var src = new UniqueIdSet (new [] { new UniqueId (1), new UniqueId (2) });
			var dest = new UniqueIdSet (new [] { new UniqueId (101), new UniqueId (102) });
			var code = new CopyUidResponseCode (12345, src, dest, true, "Copy completed");

			AssertCommon (code, ImapResponseCodeType.CopyUid, "COPYUID", false, true, "Copy completed");
			Assert.That (code, Is.InstanceOf<UidValidityResponseCode> (), "UidValidityResponseCode");
			Assert.That (code.UidValidity, Is.EqualTo (12345), "UidValidity");
			Assert.That (code.SourceUidSet, Is.SameAs (src), "SourceUidSet");
			Assert.That (code.DestinationUidSet, Is.SameAs (dest), "DestinationUidSet");

			Assert.Throws<ArgumentNullException> (() => new CopyUidResponseCode (12345, null, dest, true, "Copy completed"));
			Assert.Throws<ArgumentNullException> (() => new CopyUidResponseCode (12345, src, null, true, "Copy completed"));
			Assert.Throws<ArgumentNullException> (() => new CopyUidResponseCode (12345, src, dest, true, null));
		}

		[Test]
		public void TestBadUrlResponseCode ()
		{
			var code = new BadUrlResponseCode ("imap://localhost/INBOX/;UID=1", true, "Bad URL");

			AssertCommon (code, ImapResponseCodeType.BadUrl, "BADURL", true, true, "Bad URL");
			Assert.That (code.BadUrl, Is.EqualTo ("imap://localhost/INBOX/;UID=1"), "BadUrl");

			Assert.Throws<ArgumentNullException> (() => new BadUrlResponseCode (null, true, "Bad URL"));
			Assert.Throws<ArgumentNullException> (() => new BadUrlResponseCode ("imap://localhost/INBOX/;UID=1", true, null));
		}

		[Test]
		public void TestHighestModSeqResponseCode ()
		{
			var code = new HighestModSeqResponseCode (ulong.MaxValue, false, "Highest");

			AssertCommon (code, ImapResponseCodeType.HighestModSeq, "HIGHESTMODSEQ", false, false, "Highest");
			Assert.That (code.HighestModSeq, Is.EqualTo (ulong.MaxValue), "HighestModSeq");

			Assert.Throws<ArgumentNullException> (() => new HighestModSeqResponseCode (1, false, null));
		}

		[Test]
		public void TestModifiedResponseCode ()
		{
			var uids = new UniqueIdSet (new [] { new UniqueId (3) });
			var code = new ModifiedResponseCode (uids, true, "Conditional store failed");

			AssertCommon (code, ImapResponseCodeType.Modified, "MODIFIED", false, true, "Conditional store failed");
			Assert.That (code.UidSet, Is.SameAs (uids), "UidSet");

			Assert.Throws<ArgumentNullException> (() => new ModifiedResponseCode (null, true, "Conditional store failed"));
			Assert.Throws<ArgumentNullException> (() => new ModifiedResponseCode (uids, true, null));
		}

		[TestCase (ImapResponseCodeType.MaxConvertMessages, "MAXCONVERTMESSAGES")]
		[TestCase (ImapResponseCodeType.MaxConvertParts, "MAXCONVERTPARTS")]
		public void TestMaxConvertResponseCode (ImapResponseCodeType type, string atom)
		{
			var code = new MaxConvertResponseCode (type, 10, true, "Too many");

			AssertCommon (code, type, atom, true, true, "Too many");
			Assert.That (code.MaxConvert, Is.EqualTo (10), "MaxConvert");

			Assert.Throws<ArgumentNullException> (() => new MaxConvertResponseCode (type, 10, true, null));
		}

		[Test]
		public void TestMaxConvertResponseCodeInvalidType ()
		{
			Assert.Throws<ArgumentOutOfRangeException> (() => new MaxConvertResponseCode (ImapResponseCodeType.Limit, 10, true, "Too many"));
		}

		[Test]
		public void TestNoUpdateResponseCode ()
		{
			var code = new NoUpdateResponseCode ("A00000005", false, "No more updates");

			AssertCommon (code, ImapResponseCodeType.NoUpdate, "NOUPDATE", true, false, "No more updates");
			Assert.That (code.Tag, Is.EqualTo ("A00000005"), "Tag");

			Assert.Throws<ArgumentNullException> (() => new NoUpdateResponseCode (null, false, "No more updates"));
			Assert.Throws<ArgumentNullException> (() => new NoUpdateResponseCode ("A00000005", false, null));
		}

		[TestCase (AnnotateResponseCodeSubType.TooBig)]
		[TestCase (AnnotateResponseCodeSubType.TooMany)]
		public void TestAnnotateResponseCode (AnnotateResponseCodeSubType subType)
		{
			var code = new AnnotateResponseCode (subType, true, "Annotation failed");

			AssertCommon (code, ImapResponseCodeType.Annotate, "ANNOTATE", true, true, "Annotation failed");
			Assert.That (code.SubType, Is.EqualTo (subType), "SubType");

			Assert.Throws<ArgumentNullException> (() => new AnnotateResponseCode (subType, true, null));
		}

		[Test]
		public void TestAnnotationsResponseCode ()
		{
			var code = new AnnotationsResponseCode (AnnotationAccess.ReadWrite, AnnotationScope.Both, 1024, false, "Annotations");

			AssertCommon (code, ImapResponseCodeType.Annotations, "ANNOTATIONS", false, false, "Annotations");
			Assert.That (code.Access, Is.EqualTo (AnnotationAccess.ReadWrite), "Access");
			Assert.That (code.Scopes, Is.EqualTo (AnnotationScope.Both), "Scopes");
			Assert.That (code.MaxSize, Is.EqualTo (1024), "MaxSize");

			Assert.Throws<ArgumentNullException> (() => new AnnotationsResponseCode (AnnotationAccess.None, AnnotationScope.None, 0, false, null));
		}

		[TestCase (MetadataResponseCodeSubType.LongEntries, false)]
		[TestCase (MetadataResponseCodeSubType.MaxSize, true)]
		[TestCase (MetadataResponseCodeSubType.TooMany, true)]
		[TestCase (MetadataResponseCodeSubType.NoPrivate, true)]
		public void TestMetadataResponseCode (MetadataResponseCodeSubType subType, bool isError)
		{
			var code = new MetadataResponseCode (subType, 2048, true, "Metadata");

			AssertCommon (code, ImapResponseCodeType.Metadata, "METADATA", isError, true, "Metadata");
			Assert.That (code.SubType, Is.EqualTo (subType), "SubType");
			Assert.That (code.Value, Is.EqualTo (2048), "Value");

			Assert.Throws<ArgumentNullException> (() => new MetadataResponseCode (subType, 0, true, null));
		}

		[Test]
		public void TestUndefinedFilterResponseCode ()
		{
			var code = new UndefinedFilterResponseCode ("MyFilter", true, "Undefined filter");

			AssertCommon (code, ImapResponseCodeType.UndefinedFilter, "UNDEFINED-FILTER", true, true, "Undefined filter");
			Assert.That (code.Name, Is.EqualTo ("MyFilter"), "Name");

			Assert.Throws<ArgumentNullException> (() => new UndefinedFilterResponseCode (null, true, "Undefined filter"));
			Assert.Throws<ArgumentNullException> (() => new UndefinedFilterResponseCode ("MyFilter", true, null));
		}

		[Test]
		public void TestMailboxIdResponseCode ()
		{
			var code = new MailboxIdResponseCode ("F2212ea87-6097-4256-9d51-71338625", true, "Create completed");

			AssertCommon (code, ImapResponseCodeType.MailboxId, "MAILBOXID", false, true, "Create completed");
			Assert.That (code.MailboxId, Is.EqualTo ("F2212ea87-6097-4256-9d51-71338625"), "MailboxId");

			Assert.Throws<ArgumentNullException> (() => new MailboxIdResponseCode (null, true, "Create completed"));
			Assert.Throws<ArgumentNullException> (() => new MailboxIdResponseCode ("F2212ea87", true, null));
		}

		[Test]
		public void TestWebAlertResponseCode ()
		{
			var uri = new Uri ("https://accounts.google.com/signin/continue");
			var code = new WebAlertResponseCode (uri, true, "Web login required");

			AssertCommon (code, ImapResponseCodeType.WebAlert, "WEBALERT", false, true, "Web login required");
			Assert.That (code.WebUri, Is.SameAs (uri), "WebUri");

			Assert.Throws<ArgumentNullException> (() => new WebAlertResponseCode (null, true, "Web login required"));
			Assert.Throws<ArgumentNullException> (() => new WebAlertResponseCode (uri, true, null));
		}
	}
}