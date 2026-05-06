# How to send a bug report for Jira Migrator Pro

Thank you for taking the time to report an issue! To help us diagnose and fix the problem as quickly as possible, please include the files described below when you email us at **support.jira-migrator@solidify.dev**.

---

## 1. Find your workspace folder

All the files you need live in your **workspace folder**. This is the directory you set as the `workspace` value in your `config.json`. If you are unsure where that is, open your `config.json` and look for this line:

```json
"workspace": "C:\\migrations\\my-project"
```

That path is your workspace folder.

---

## 2. Files to include

### 1. config.json

This is the configuration file you used for the migration. It tells us how your Jira project and Azure DevOps project were set up.

> **Where to find it:** This file is not inside the workspace folder. It is the file you passed as an argument when you ran `jira-export` or `wi-import`. Check the command you ran, or look in the folder where you launched the tool.

---

### 2. Log files

These files record everything that happens during the migration run and are essential for diagnosing errors.

| File | What it covers |
|------|---------------|
| `jira-export-log-*.txt` | The export phase (downloading from Jira) |
| `wi-import-log-*.txt` | The import phase (uploading to Azure DevOps) |

The `*` in the file name is a timestamp, so you may have several log files if you have run the tool multiple times. **Include the ones from the run where the problem occurred**, usually the most recent ones.

> **Where to find them:** In the root of your workspace folder.
>
> Example: `C:\migrations\my-project\jira-export-log-2026-05-06T10-00-00.txt`

---

### 3. Exported JSON files

When `jira-export` runs, it creates one `.json` file per Jira issue (e.g. `ABC-123.json`). Attach the affected issue(s) by name if you know them, or a small sample (3–5 files) if the problem is more general.

> **Where to find them:** In the root of your workspace folder, named after the Jira issue key.
>
> Example: `C:\migrations\my-project\ABC-123.json`

---

### 4. Sprints folder (if the problem involves sprints)

If your issue is related to sprint data, include the contents of the `Sprints` folder.

> **Where to find it:** `<workspace>\Sprints\`
>
> Example: `C:\migrations\my-project\Sprints\`

---

### 5. Releases folder (if the problem involves releases or fix versions)

If your issue is related to Jira Fix Versions / Release work items (only applicable when `export-releases: true` is set in your config), include the contents of the `Releases` folder.

> **Where to find it:** `<workspace>\Releases\`
>
> Example: `C:\migrations\my-project\Releases\`

---

## Quick checklist

Use this as a reminder before you hit Send:

- [ ] `config.json`
- [ ] `jira-export-log-*.txt` (the relevant run)
- [ ] `wi-import-log-*.txt` (the relevant run)
- [ ] Exported JSON file(s) — the affected issue(s), or a few representative examples
- [ ] `Sprints\` folder zipped *(if the problem involves sprints)*
- [ ] `Releases\` folder zipped *(if the problem involves releases / fix versions)*
- [ ] A short description of what you expected to happen and what happened instead

---

## What to write in the email

Please include the following in the body of your email:

1. **What you were doing** — e.g. *"I ran jira-export for project ABC"*
2. **What went wrong** — e.g. *"Issue ABC-42 was not created in Azure DevOps"*
3. **What you expected** — e.g. *"I expected to find work item ABC-42 in the target project"*
4. **Steps to reproduce** — the exact steps that lead to the problem, so we can replicate it on our end. For example:
   1. Run `jira-export` with the attached `config.json`
   2. Open the workspace folder and inspect `ABC-42.json`
   3. Run `wi-import`
   4. Check Azure DevOps — work item is missing
5. **Any error message** — copy and paste it from the terminal or the log file

Send the email with the files attached to: **support.jira-migrator@solidify.dev**
