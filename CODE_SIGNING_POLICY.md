# Code signing policy

*Deutsch: Diese Seite beschreibt, wie die Windows-Programmdatei von NeuroSim signiert wird und wer dafür verantwortlich ist. Sie ist auf Englisch gehalten, weil die SignPath Foundation sie in dieser Form verlangt.*

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

## What is signed

Only the Windows executable `NeuroSim.exe` that is attached to the [releases of this repository](https://github.com/soetbaer/EEGSimulator/releases) is signed.

- The executable is built from the source code in this repository and nothing else.
- It is built by GitHub Actions on GitHub-hosted runners (`.github/workflows/ci.yml`), not on a developer machine.
- Every release corresponds to a commit on the `main` branch and to the version number in `NeuroSim.vbproj`.

## Team roles

| Role | Who | What they do |
|---|---|---|
| Committers and reviewers | [@soetbaer](https://github.com/soetbaer) | Write and review all changes to the source code |
| Approvers | [@soetbaer](https://github.com/soetbaer) | Approve each signing request for a release |

Changes from other contributors are accepted only as pull requests and are reviewed by a committer before they are merged.

All team members use multi-factor authentication for GitHub and for SignPath.

## Privacy policy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

The only network access is the update check:

- NeuroSim asks once, on first start, whether it may look for new versions. Without consent nothing is sent.
- With consent, or when the user clicks "Jetzt nach Updates suchen", NeuroSim requests the latest version number from GitHub (`https://github.com/soetbaer/EEGSimulator/releases/latest`), automatically at most once per day. GitHub sees the IP address of the computer; no usage data and no personal data are sent.
- If the user chooses to install an update, NeuroSim downloads `NeuroSim.exe` from the releases of this repository.
- Administrators can switch off the update function completely (see [README](README.md), section "Info, Lizenz und Updates").

NeuroSim processes no patient data. The only data stored locally are the update setting and the date of the last update check (`%AppData%\NeuroSim\settings.txt`).

## Reporting problems

If you believe a signed file of this project is harmful or does not match the source code, please open an [issue](https://github.com/soetbaer/EEGSimulator/issues).
