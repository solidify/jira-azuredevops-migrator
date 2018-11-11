# Migration log file

## Structure

The log file contains data on the following format:

        severity;timestamp;message

## Example file

```
[l:D][d:14:55:08] Connecting to Jira...
[l:I][d:14:55:08] Connected to Jira.
[l:I][d:14:55:08] Gathering project info...
[l:I][d:14:55:09] Custom field cache set up.
[l:I][d:14:55:09] Custom parsers set up.
[l:I][d:14:55:10] Link types cache set up.
[l:I][d:14:55:10] Processing issues...
[l:D][d:14:55:12] Downloaded SCRUM-8
[l:D][d:14:55:13] Formed representation of jira item SCRUM-8
[l:I][d:14:55:13] Downloaded SCRUM-8 - [1/24]
[l:I][d:14:55:14] Exported [Bug]SCRUM-8/-1
[l:D][d:14:55:15] Downloaded SCRUM-9
[l:D][d:14:55:16] Formed representation of jira item SCRUM-9
[l:I][d:14:55:16] Downloaded SCRUM-9 - [2/24]
[l:I][d:14:55:16] Exported [Product Backlog Item]SCRUM-9/-1
[l:D][d:14:55:17] Downloaded SCRUM-6
[l:D][d:14:55:19] Formed representation of jira item SCRUM-6
[l:I][d:14:55:19] Downloaded SCRUM-6 - [3/24]
[l:I][d:14:55:19] Exported [Product Backlog Item]SCRUM-6/-1
[l:D][d:14:55:20] Downloaded SCRUM-7
[l:D][d:14:55:21] Formed representation of jira item SCRUM-7
[l:I][d:14:55:21] Downloaded SCRUM-7 - sub-item of SCRUM-6
[l:I][d:14:55:21] Exported [Task]SCRUM-7/-1
[l:I][d:14:55:21] Skipped SCRUM-7 - already downloaded [4/24]
[l:D][d:14:55:23] Downloaded SCRUM-5
```
