# v0.1.0-preview.6 — Basic ADT discovery hotfix

[Türkçe](RELEASE-NOTES.tr.md)

Fix a Preview.5 regression: entering a server without a port could send `explicitPort: null`, which the ADT process rejected before making any network request. The generic warning concealed this input error. RFC could succeed independently. Manager now omits absent optional fields, and updated ADT 0.3.1 accepts legacy null fields. Safe input/process codes distinguish local discovery failures from network/TLS results.

Upgrade Manager, then **Prepare installation** in Basic or install/repair ADT in Advanced; restart client MCP processes. Both the Manager and its ADT package must be updated. Profiles, passwords, settings and RFC archives are preserved. Known HTTPS endpoints can be entered manually. An instance number does not establish the HTTPS port; DNS names and CA trust must still match the actual service.

Regression coverage includes the exact Manager host-only JSON, the legacy null request through the real Node CLI, and Manager-to-Node discovery against a local HTTPS fixture. Actual test counts and limitations are in the validation assets. [Basic help](https://github.com/tamerinal/sap_mcp_windows/blob/v0.1.0-preview.6/docs/BASIC-SETUP.md) · [issue #6](https://github.com/tamerinal/sap_mcp_windows/issues/6).

Unsigned preview. Clean Windows and live SAP/affected-machine acceptance remain pending. SDK is not bundled. Inno Setup 6.7.3 reports non-commercial use only; commercial distribution requires appropriate licensing. MIT does not replace third-party terms. Compiled private MCP JavaScript/dependencies are extractable from EXEs. Previous releases are preserved. No unconditional compatibility claim.
