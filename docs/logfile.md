# Migration log file

The tools will create import/export log files with unique timestamps. Use the log-level property in the configuration file to set an appropriate level. We recommend starting with Info and move to Debug when you need to troubleshoot.

## Structure

The log file contains data on the following format:

```log
Info header
=============================
[severity][timestamp] message
```

## Example file

```log
====================================================================
Jira Export Log
====================================================================
Tool version : 2.2.13
Start time   : 2019-05-01 21:36:43
Telemetry    : Enabled
Session id   : b01a7fb6-e2ad-45cc-b912-0dc888f54421
Tool user    : DOMAIN\user
Config       : samples\config-scrum.json
Force        : yes
Log level    : Info
Machine      : user-pc
System       : Microsoft Windows 10.0.17763
Jira url     : https://account.atlassian.net
Jira user    : user.name@domain.com
Jira version : 1001.0.0-SNAPSHOT
Jira type    : Cloud
====================================================================
[I][21:03:01] Connecting to Jira...
[I][21:03:01] Retrieving Jira fields...
[I][21:03:02] Retrieving Jira link types...
[I][21:03:03] Export started. Exporting 39 items.
[I][21:03:04] Initializing Jira field mapping...
[I][21:03:06] Processing 1/39 - 'SCRUM-8'.
[I][21:03:08] Processing 2/39 - 'SCRUM-26'.
[I][21:03:10] Processing sub-item 'SCRUM-32'.
[I][21:03:12] Processing sub-item 'SCRUM-28'.
[W][21:03:14] Missing mapping value 'Analysis Complete' for field 'status'.
[I][21:03:14] Processing sub-item 'SCRUM-29'.
[I][21:03:16] Processing sub-item 'SCRUM-30'.
```
