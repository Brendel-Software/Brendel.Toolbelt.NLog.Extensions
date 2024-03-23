using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Brendel.Toolbelt.NLog.Extensions.Targets;

[Target("Postmark")]
public class PostmarkLogTarget : TargetWithLayout {
	private const int max_subject_length = 77; // RFC 2822 Empfehlung

	private static readonly Lazy<HttpClient> _httpClient = new(() => new HttpClient());

	private Layout _layout;

	public PostmarkLogTarget() {
		_layout = new XmlLayout {
			IndentXml = true,
			Elements = {
				new("Level", "${level}"),
				new("Message", "${message}"),
				new("Callsite", "${callsite}"),
				new("Timestamp", "${longdate}"),
				new("Machinename", "${machinename}"),
				new("ProcessID", "${processid}"),
				new("ProcessName", "${processname}"),
				new("ThreadID", "${threadid}")
			}
		};
	}

	public override Layout Layout {
		get => _layout;
		set => _layout = value;
	}

	public string Endpoint { get; set; } = "https://api.postmarkapp.com/email";

	[RequiredParameter]
	public string From { get; set; } = string.Empty;

	[RequiredParameter]
	public string To { get; set; } = string.Empty;

	[RequiredParameter]
	public string MessageStream { get; set; } = string.Empty;

	[RequiredParameter]
	public string ServerToken { get; set; } = string.Empty;

	public string? SubjectPrefix { get; set; }

	public int LogExcerptLength { get; set; } = 200;

	public string? LogExcerptTargetName { get; set; }

	protected override void Write(LogEventInfo logEvent) {
		Debug.Assert(!string.IsNullOrWhiteSpace(ServerToken)); // Das Server Token sollte zur Laufzeit gesetzt sein!

		var subject = GetSubject(logEvent);
		var body = GetHtmlBody(logEvent);
		Send(subject, body);
	}

	protected virtual string GetSubject(LogEventInfo logEvent) {
		var subjectTags = new List<string>();

		if (!string.IsNullOrEmpty(SubjectPrefix)) {
			subjectTags.Add($"[{SubjectPrefix}]");
		}

		subjectTags.Add($"[{logEvent.Level.Name}]");

		var exceptionName = logEvent.Exception?.GetType().Name ?? string.Empty;
		exceptionName = Regex.Replace(exceptionName, "exception$", string.Empty, RegexOptions.IgnoreCase);

		var subject = exceptionName.Length > 0 ? $"{exceptionName} {logEvent.Message}" : logEvent.Message;

		var result = string.Join("", subjectTags) + " " + subject;
		if (result.Length <= max_subject_length) {
			return result;
		}

		return result.Substring(0, max_subject_length - 1) + "…";
	}

	protected virtual string GetHtmlBody(LogEventInfo logEvent) {
		var msg = GetLoggingMessage(logEvent);
		var xml = RenderLogEvent(Layout, logEvent) ?? string.Empty;
		var stacktrace = logEvent.Exception?.StackTrace;
		var logExcerpt = ReadLastLines(logEvent);

		var sb = new StringBuilder();
		sb.AppendLine($"<p><b>{msg}</b></p>");
		sb.AppendLine($"<em>LogEvent</em>");
		sb.AppendLine($"<pre>{HttpUtility.HtmlEncode(xml)}</pre>");
		sb.AppendLine($"<br/>");
		sb.AppendLine($"<br/>");
		sb.AppendLine($"<em>Stacktrace</em>");
		sb.AppendLine($"<br/>");
		sb.AppendLine($"<pre>{stacktrace}</pre>");
		sb.AppendLine($"<br/>");
		sb.AppendLine($"<br/>");
		sb.AppendLine($"<em>Exception-Stack</em>");
		sb.AppendLine($"<br/>");
		sb.AppendLine($"<br/>");

		if (!string.IsNullOrEmpty(logExcerpt)) {
			sb.AppendLine($"<em>Log Auszug (letzten {LogExcerptLength} Zeilen)</em>");
			sb.AppendLine($"<br/>");
			sb.AppendLine($"<pre>{logExcerpt}</pre>");
			sb.AppendLine($"<br/>");
			sb.AppendLine($"<br/>");
		}

		return sb.ToString();
	}

	private void Send(string subject, string body) {
		var content = new {
			From,
			To,
			Subject = subject,
			HtmlBody = body,
			TrackLinks = "None",
			TrackOpens = false,
			MessageStream
		};

		var msg = new HttpRequestMessage(HttpMethod.Post, Endpoint) {
			Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
		};

		msg.Headers.Add("Accept", "application/json");
		msg.Headers.Add("X-Postmark-Server-Token", ServerToken);

		_httpClient.Value.Send(msg);
	}

	private static string GetLoggingMessage(LogEventInfo logEvent) {
		string msg;

		if ((string.IsNullOrEmpty(logEvent.Message) || logEvent.Message == "{0}") && logEvent.Exception != null) {
			msg = logEvent.Exception.Message;
		} else {
			msg = logEvent.Message;
		}

		return msg;
	}

	private string? ReadLastLines(LogEventInfo logEvent) {
		if (LogExcerptTargetName == null) {
			return null;
		}

		if (LoggingConfiguration.FindTargetByName<FileTarget>(LogExcerptTargetName) is not { } fileTarget) {
			return null;
		}

		try {
			var filePath = fileTarget.FileName.Render(logEvent);

			using var stream = new FileStream(filePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			stream.Seek(0, SeekOrigin.End);
			for (var count = 0; count < LogExcerptLength && stream.Position != 0; stream.Seek(-1, SeekOrigin.Current)) {
				var buffer = (byte) stream.ReadByte();
				stream.Seek(-1, SeekOrigin.Current);
				if (buffer == '\n') {
					count++;
				}
			}

			using var reader = new StreamReader(stream, fileTarget.Encoding);
			var result = reader.ReadToEnd();

			return result;
		} catch {
			// ignored
		}

		return null;
	}
}