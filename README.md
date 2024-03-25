[![Build Status](https://brendel-storage.visualstudio.com/R%C3%BCdigers%20Spielwiese/_apis/build/status%2FBrendel.Toolbelt.NLog.Extensions?branchName=master)](https://brendel-storage.visualstudio.com/R%C3%BCdigers%20Spielwiese/_build/latest?definitionId=36&branchName=master)

# Brendel.Toolbelt.NLog.Extensions

A comprehensive suite of NLog extensions provided by Brendel Software.

## Usage
Incorporate the assembly into your `nlog.config` by adding the following lines:

```xml
	<extensions>
		<add assembly="Brendel.Toolbelt.NLog.Extensions" />
	</extensions>
```

## Targets

### LimitingAutoFlushWrapper

## Targets
### LimitingAutoFlushWrapper
The `LimitingAutoFlushWrapper` serves as a wrapper for another target, imposing restrictions on the automatic flushing of NLog events to a designated time interval. This functionality is particularly beneficial for controlling the frequency of log entry writing, especially when used alongside a `BufferingWrapper`.

Available properties include:

* **Interva**l: The duration after which the internal counter is reset.
* **FlushLimit**: The maximum number of flush operations allowed within each interval.
* **DebounceLostFlushes**: When set to true, any flushes that are discarded will be postponed until the end of the interval. Executing `Flush()` or `Shutdown()` will trigger all pending debounce operations to complete ahead of schedule.

The following example demonstrates a `MailTarget` which sends 1000 log entries every time a warning occurs, limited to 2 emails every 10 minutes:

```xml
<?xml version="1.0" encoding="utf-8"?>

<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
		  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
		  throwConfigExceptions="true">

	<extensions>
		<add assembly="Brendel.Toolbelt.NLog.Extensions" />
	</extensions>

	<variable name="mail-layout"
			  value="${longdate}|${level}|${message}|${all-event-properties}|${exception:format=toString,Data}${newline}" />
	<variable name="mail-subject"
			  value="${onexception:inner=${exception:format=Message}:whenEmpty=${message}}" />

	<targets>
		<target xsi:type="LimitingAutoFlushWrapper"
				name="mailFlush"
				condition="level >= LogLevel.Warn"
				flushOnConditionOnly="true"
				flushLimit="1"
				interval="00:10:00"
				debounceDiscardedFlushes="true">
			<target xsi:type="BufferingWrapper"
					name="mailBuffer"
					bufferSize="1000"
					overflowAction="Discard">
				<target xsi:type="Mail"
						name="mail"
						layout="${mail-layout}"
						subject="${mail-subject}"
						from="warn@exmaple.com"
						to="admin@example.com"
						priority="High"
						enableSsl="true"
						smtpServer=""
						smtpPort="587"
						smtpUserName=""
						smtpPassword=""
						smtpAuthentication="Basic" />
			</target>
		</target>
	</targets>

	<rules>
		<logger name="*" minlevel="Debug" writeTo="mailFlush" />
	</rules>
</nlog>
```
