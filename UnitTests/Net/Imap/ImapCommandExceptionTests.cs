//
// ImapCommandExceptionTests.cs
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

using System.Text;

using MailKit;
using MailKit.Security;
using MailKit.Net.Imap;

namespace UnitTests.Net.Imap {
	[TestFixture]
	public class ImapCommandExceptionTests
	{
		static ImapResponseCode CreateResponseCode (string atom, bool isTagged)
		{
			switch (atom) {
			case "BADURL": return new BadUrlResponseCode ("imap://localhost/INBOX/;UID=1", isTagged, "response text");
			case "UNDEFINED-FILTER": return new UndefinedFilterResponseCode ("MyFilter", isTagged, "response text");
			case "MAXCONVERTMESSAGES": return new MaxConvertResponseCode (ImapResponseCodeType.MaxConvertMessages, 10, isTagged, "response text");
			case "MAXCONVERTPARTS": return new MaxConvertResponseCode (ImapResponseCodeType.MaxConvertParts, 10, isTagged, "response text");
			default: return new ImapResponseCode (atom, isTagged, "response text");
			}
		}

		[TestCase ("CANNOT", CommandErrorType.NotSupported)]
		[TestCase ("UNKNOWN-CTE", CommandErrorType.NotSupported)]
		[TestCase ("BADCHARSET", CommandErrorType.NotSupported)]
		[TestCase ("BADCOMPARATOR", CommandErrorType.NotSupported)]
		[TestCase ("BADEVENT", CommandErrorType.NotSupported)]
		[TestCase ("USEATTR", CommandErrorType.NotSupported)]
		[TestCase ("CLIENTBUG", CommandErrorType.InvalidCommand)]
		[TestCase ("NOPERM", CommandErrorType.PermissionDenied)]
		[TestCase ("PRIVACYREQUIRED", CommandErrorType.PermissionDenied)]
		[TestCase ("AUTHENTICATIONFAILED", CommandErrorType.PermissionDenied)]
		[TestCase ("AUTHORIZATIONFAILED", CommandErrorType.PermissionDenied)]
		[TestCase ("EXPIRED", CommandErrorType.PermissionDenied)]
		[TestCase ("CONTACTADMIN", CommandErrorType.PermissionDenied)]
		[TestCase ("NONEXISTENT", CommandErrorType.NotFound)]
		[TestCase ("TRYCREATE", CommandErrorType.NotFound)]
		[TestCase ("UNDEFINED-FILTER", CommandErrorType.NotFound)]
		[TestCase ("BADURL", CommandErrorType.NotFound)]
		[TestCase ("ALREADYEXISTS", CommandErrorType.AlreadyExists)]
		[TestCase ("OVERQUOTA", CommandErrorType.QuotaExceeded)]
		[TestCase ("LIMIT", CommandErrorType.LimitExceeded)]
		[TestCase ("TOOBIG", CommandErrorType.LimitExceeded)]
		[TestCase ("MAXCONVERTMESSAGES", CommandErrorType.LimitExceeded)]
		[TestCase ("MAXCONVERTPARTS", CommandErrorType.LimitExceeded)]
		[TestCase ("INUSE", CommandErrorType.InUse)]
		[TestCase ("UNAVAILABLE", CommandErrorType.TemporaryFailure)]
		[TestCase ("TEMPFAIL", CommandErrorType.TemporaryFailure)]
		[TestCase ("SERVERBUG", CommandErrorType.ServerError)]
		[TestCase ("CORRUPTION", CommandErrorType.ServerError)]
		[TestCase ("X-UNKNOWN-CODE", CommandErrorType.Rejected)]
		public void TestImapCommandExceptionErrorType (string atom, CommandErrorType expected)
		{
			var code = CreateResponseCode (atom, true);
			var ex = new ImapCommandException (ImapCommandResponse.No, new [] { code }, "response text", "message");

			Assert.That (ex.ResponseCodes, Has.Count.EqualTo (1), "ResponseCodes");
			Assert.That (ex.ResponseCodes[0], Is.SameAs (code), "ResponseCodes[0]");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType");
			Assert.That (ex.IsTransient, Is.EqualTo (expected == CommandErrorType.TemporaryFailure || expected == CommandErrorType.InUse), "IsTransient");

			ex = new ImapCommandException (ImapCommandResponse.No, new [] { code }, "response text", "message", new IOException ());
			Assert.That (ex.ResponseCodes, Has.Count.EqualTo (1), "ResponseCodes (inner)");
			Assert.That (ex.ResponseCodes[0], Is.SameAs (code), "ResponseCodes[0] (inner)");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType (inner)");
		}

		[TestCase (ImapCommandResponse.No, CommandErrorType.Rejected)]
		[TestCase (ImapCommandResponse.Bad, CommandErrorType.InvalidCommand)]
		[TestCase (ImapCommandResponse.Ok, CommandErrorType.Unknown)]
		[TestCase (ImapCommandResponse.None, CommandErrorType.Unknown)]
		public void TestImapCommandExceptionErrorTypeWithoutResponseCode (ImapCommandResponse response, CommandErrorType expected)
		{
			var ex = new ImapCommandException (response, "response text", "message");

			Assert.That (ex.ResponseCodes, Is.Empty, "ResponseCodes");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType");

			ex = new ImapCommandException (response, "response text", "message", new IOException ());
			Assert.That (ex.ResponseCodes, Is.Empty, "ResponseCodes (inner)");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType (inner)");

			ex = new ImapCommandException (response, "response text");
			Assert.That (ex.ResponseCodes, Is.Empty, "ResponseCodes (2 args)");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType (2 args)");

			ex = new ImapCommandException (response, Array.Empty<ImapResponseCode> (), "response text", "message");
			Assert.That (ex.ResponseCodes, Is.Empty, "ResponseCodes (empty)");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType (empty)");

			ex = new ImapCommandException (response, Array.Empty<ImapResponseCode> (), "response text", "message", new IOException ());
			Assert.That (ex.ResponseCodes, Is.Empty, "ResponseCodes (empty, inner)");
			Assert.That (ex.ErrorType, Is.EqualTo (expected), "ErrorType (empty, inner)");
		}

		[Test]
		public void TestConstructors ()
		{
			var inner = new IOException ("inner");
			var codes = new List<ImapResponseCode> {
				new ImapResponseCode ("OVERQUOTA", false, "untagged"),
				new ImapResponseCode ("LIMIT", true, "tagged")
			};

			var ex = new ImapCommandException (ImapCommandResponse.No, codes, "response text", "message", inner);
			Assert.That (ex.Response, Is.EqualTo (ImapCommandResponse.No), "Response");
			Assert.That (ex.ResponseText, Is.EqualTo ("response text"), "ResponseText");
			Assert.That (ex.Message, Is.EqualTo ("message"), "Message");
			Assert.That (ex.InnerException, Is.SameAs (inner), "InnerException");
			Assert.That (ex.ResponseCodes, Is.EqualTo (codes), "ResponseCodes");

			ex = new ImapCommandException (ImapCommandResponse.Bad, codes, "response text", "message");
			Assert.That (ex.Response, Is.EqualTo (ImapCommandResponse.Bad), "Response (4 args)");
			Assert.That (ex.ResponseText, Is.EqualTo ("response text"), "ResponseText (4 args)");
			Assert.That (ex.Message, Is.EqualTo ("message"), "Message (4 args)");
			Assert.That (ex.InnerException, Is.Null, "InnerException (4 args)");
			Assert.That (ex.ResponseCodes, Is.EqualTo (codes), "ResponseCodes (4 args)");

			// the exception should take a snapshot of the response codes
			codes.Clear ();
			Assert.That (ex.ResponseCodes, Has.Count.EqualTo (2), "ResponseCodes snapshot");

			// the response codes should be read-only
			var list = (IList<ImapResponseCode>) ex.ResponseCodes;
			Assert.That (list.IsReadOnly, Is.True, "IsReadOnly");
			Assert.Throws<NotSupportedException> (() => list.Add (new ImapResponseCode ("LIMIT", true, "tagged")));
			Assert.Throws<NotSupportedException> (() => list.Clear ());
		}

		[Test]
		public void TestArgumentExceptions ()
		{
			var inner = new IOException ();

			Assert.Throws<ArgumentNullException> (() => new ImapCommandException (ImapCommandResponse.No, (IEnumerable<ImapResponseCode>) null, "response text", "message"));
			Assert.Throws<ArgumentNullException> (() => new ImapCommandException (ImapCommandResponse.No, (IEnumerable<ImapResponseCode>) null, "response text", "message", inner));
			Assert.Throws<ArgumentException> (() => new ImapCommandException (ImapCommandResponse.No, new ImapResponseCode [] { null }, "response text", "message"));
			Assert.Throws<ArgumentException> (() => new ImapCommandException (ImapCommandResponse.No, new ImapResponseCode [] { null }, "response text", "message", inner));
		}

		[Test]
		public void TestErrorTypePrefersTaggedResponseCode ()
		{
			var codes = new [] {
				new ImapResponseCode ("OVERQUOTA", false, "untagged"),
				new ImapResponseCode ("INUSE", true, "tagged"),
				new ImapResponseCode ("NONEXISTENT", false, "untagged")
			};
			var ex = new ImapCommandException (ImapCommandResponse.No, codes, "response text", "message");

			Assert.That (ex.ErrorType, Is.EqualTo (CommandErrorType.InUse));
		}

		[Test]
		public void TestErrorTypeUsesLastUntaggedResponseCode ()
		{
			var codes = new [] {
				new ImapResponseCode ("OVERQUOTA", false, "untagged"),
				new ImapResponseCode ("NONEXISTENT", false, "untagged")
			};
			var ex = new ImapCommandException (ImapCommandResponse.No, codes, "response text", "message");

			Assert.That (ex.ErrorType, Is.EqualTo (CommandErrorType.NotFound));
		}

		[Test]
		public void TestErrorTypeIgnoresNonErrorResponseCodes ()
		{
			var codes = new ImapResponseCode [] {
				new ImapResponseCode ("ALERT", true, "alert"),
				new MetadataResponseCode (MetadataResponseCodeSubType.LongEntries, 1024, true, "long entries")
			};
			var ex = new ImapCommandException (ImapCommandResponse.No, codes, "response text", "message");

			Assert.That (ex.ResponseCodes, Has.Count.EqualTo (2), "ResponseCodes");
			Assert.That (ex.ErrorType, Is.EqualTo (CommandErrorType.Rejected), "ErrorType");
		}

		static List<ImapReplayCommand> CreateMultipleErrorResponseCodesCommands ()
		{
			return new List<ImapReplayCommand> {
				new ImapReplayCommand ("", Encoding.ASCII.GetBytes ("* OK [CAPABILITY IMAP4rev2 AUTH=PLAIN] IMAP4rev2 Service Ready\r\n")),
				new ImapReplayCommand ("A00000000 LOGIN username password\r\n", Encoding.ASCII.GetBytes ("A00000000 OK [CAPABILITY IMAP4rev2] Logged in\r\n")),
				new ImapReplayCommand ("A00000001 NAMESPACE\r\n", Encoding.ASCII.GetBytes ("* NAMESPACE ((\"\" \"/\")) NIL NIL\r\nA00000001 OK Namespace completed.\r\n")),
				new ImapReplayCommand ("A00000002 LIST \"\" \"INBOX\" RETURN (SUBSCRIBED CHILDREN)\r\n", Encoding.ASCII.GetBytes ("* LIST (\\Subscribed \\HasNoChildren) \"/\" INBOX\r\nA00000002 OK List completed.\r\n")),
				new ImapReplayCommand ("A00000003 LIST (SPECIAL-USE) \"\" \"*\" RETURN (SUBSCRIBED CHILDREN)\r\n", Encoding.ASCII.GetBytes ("A00000003 OK List completed.\r\n")),
				new ImapReplayCommand ("A00000004 SELECT INBOX\r\n", Encoding.ASCII.GetBytes ("* NO [UNAVAILABLE] The backend is unavailable.\r\n* OK [ALERT] The server will restart soon.\r\n* NO [OVERQUOTA] The mailbox is over quota.\r\nA00000004 NO [INUSE] The mailbox is locked.\r\n")),
				new ImapReplayCommand ("A00000005 LOGOUT\r\n", Encoding.ASCII.GetBytes ("* BYE Logging out.\r\nA00000005 OK Logout completed.\r\n"))
			};
		}

		static void AssertMultipleErrorResponseCodes (ImapCommandException ex)
		{
			Assert.That (ex.Response, Is.EqualTo (ImapCommandResponse.No), "Response");
			Assert.That (ex.ResponseText, Is.EqualTo ("The mailbox is locked."), "ResponseText");
			Assert.That (ex.ErrorType, Is.EqualTo (CommandErrorType.InUse), "ErrorType");
			Assert.That (ex.ResponseCodes, Has.Count.EqualTo (3), "ResponseCodes");

			Assert.That (ex.ResponseCodes[0].Type, Is.EqualTo (ImapResponseCodeType.Unavailable), "ResponseCodes[0].Type");
			Assert.That (ex.ResponseCodes[0].Atom, Is.EqualTo ("UNAVAILABLE"), "ResponseCodes[0].Atom");
			Assert.That (ex.ResponseCodes[0].IsTagged, Is.False, "ResponseCodes[0].IsTagged");
			Assert.That (ex.ResponseCodes[0].IsError, Is.True, "ResponseCodes[0].IsError");
			Assert.That (ex.ResponseCodes[0].Message, Is.EqualTo ("The backend is unavailable."), "ResponseCodes[0].Message");

			Assert.That (ex.ResponseCodes[1].Type, Is.EqualTo (ImapResponseCodeType.OverQuota), "ResponseCodes[1].Type");
			Assert.That (ex.ResponseCodes[1].IsTagged, Is.False, "ResponseCodes[1].IsTagged");
			Assert.That (ex.ResponseCodes[1].Message, Is.EqualTo ("The mailbox is over quota."), "ResponseCodes[1].Message");

			Assert.That (ex.ResponseCodes[2].Type, Is.EqualTo (ImapResponseCodeType.InUse), "ResponseCodes[2].Type");
			Assert.That (ex.ResponseCodes[2].IsTagged, Is.True, "ResponseCodes[2].IsTagged");
			Assert.That (ex.ResponseCodes[2].Message, Is.EqualTo ("The mailbox is locked."), "ResponseCodes[2].Message");
		}

		[Test]
		public void TestMultipleErrorResponseCodes ()
		{
			var commands = CreateMultipleErrorResponseCodesCommands ();

			using (var client = new ImapClient () { TagPrefix = 'A' }) {
				client.Connect (new ImapReplayStream (commands, false), "localhost", 143, SecureSocketOptions.None);
				client.AuthenticationMechanisms.Clear ();
				client.Authenticate ("username", "password");

				var ex = Assert.Throws<ImapCommandException> (() => client.Inbox.Open (FolderAccess.ReadWrite));
				AssertMultipleErrorResponseCodes (ex);

				client.Disconnect (true);
			}
		}

		[Test]
		public async Task TestMultipleErrorResponseCodesAsync ()
		{
			var commands = CreateMultipleErrorResponseCodesCommands ();

			using (var client = new ImapClient () { TagPrefix = 'A' }) {
				await client.ConnectAsync (new ImapReplayStream (commands, true), "localhost", 143, SecureSocketOptions.None);
				client.AuthenticationMechanisms.Clear ();
				await client.AuthenticateAsync ("username", "password");

				var ex = Assert.ThrowsAsync<ImapCommandException> (() => client.Inbox.OpenAsync (FolderAccess.ReadWrite));
				AssertMultipleErrorResponseCodes (ex);

				await client.DisconnectAsync (true);
			}
		}
	}
}