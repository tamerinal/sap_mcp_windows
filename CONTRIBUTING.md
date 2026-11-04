# Contributing

[Türkçe](CONTRIBUTING.tr.md)

Update [application architecture](docs/ARCHITECTURE.md) and developer guides in the same
PR as technical changes. Keep machine-specific agent instructions and paths outside
the source repository.

For bugs and feature requests, follow the [reporting and tracking guide](docs/ISSUES.md) and choose the appropriate [issue form](https://github.com/tamerinal/sap_mcp_windows/issues/new/choose). Maintainers use the documented status/area/priority labels and record acceptance, verification and release evidence in each issue.

Use issues with fictional examples and redacted errors. Never upload customer SAP data or credentials. For code changes, follow [Development](docs/DEVELOPMENT.md), preserve schema-1 and DPAPI compatibility, update both languages and run public tests. PRs must not require private repositories or a SAP account for CI. Keep SDK, local tools and output untracked. Contributions are provided under the repository's MIT license; keep third-party notices intact.
