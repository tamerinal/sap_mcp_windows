# OpenCode and Advanced automatic verification

[Turkce](OPENCODE-ADVANCED.tr.md) · [User guide](USER-GUIDE.md)

## OpenCode

In Basic or Advanced client integration, select OpenCode. Choose Global (all projects) or Project, choose the exact configuration file, then choose V1 or V2. Existing MCP content determines the initial version. A mismatched or mixed file is blocked instead of silently migrated. For a new file, V2 is the default.

Global configuration is normally `~/.config/opencode/opencode.json(c)`. Project files can be at the project root or in `.opencode`. If several candidate files exist, choose one; Manager never writes all candidates. Project or managed configuration can override global settings. Use the file that your OpenCode installation actually loads.

V1 stores servers directly in `mcp` with `enabled`; V2 uses `mcp.servers` with `disabled`. Both run SapMcpHost through a local stdio command array. See the official [V1 MCP guide](https://opencode.ai/docs/mcp-servers/) and [V2 MCP guide](https://opencode.ai/v2/docs/mcp-servers/).

Preview shows New, Update or Already current entries. Review conflicts and explicitly allow replacement before applying. Existing comments, shared agent `instructions`, models and unrelated server entries are retained. A backup is made before changes, and a file edited after preview must be previewed again. Reapplying current settings does not rewrite the file. An existing name for the same Host/component/configuration is reused; multiple matches block the write.

Only eligible components for the current mode are offered. An RFC registration does not bypass SDK readiness. Registrations record client `OpenCode`, the selected file and actual server names. Uninstall removes only entries still owned by this Manager; changed commands and unrelated entries remain.

Restart OpenCode after applying, then run `opencode mcp list` in the intended project. The copyable setup help includes both OpenCode formats. Do not replace your whole configuration with a copied example.

## Advanced: Automatically discover and verify

Select a system and an existing or new profile in Connections. Enter the user/client/language and, if replacing it, the password. Click **Automatically discover and verify**. Opening the page or editing fields starts no scan.

The action uses Basic's readiness, RFC-assisted discovery, consented smart/full scan and separate ADT/RFC verification. The same explicit transport warning applies: HTTP is unencrypted and HTTPS does not verify the server certificate during this evaluation. Existing certificates remain available. Cancellation or declining consent does not save the form.

A successful ADT result fills the discovered URL and shows the TLS checkbox required by the evaluated connection. Read the separate results and review those fields. **Nothing is committed until Save is pressed.** The existing activation checkbox controls profile activation. Changing fields/profile invalidates the displayed evaluation; verify again. Missing RFC readiness can still yield an ADT-only result.

External SAP GUI XML is read-only and is not converted to a managed source by evaluation. Failure retains the existing profile, secrets and active mapping. The original Basic workflow and Advanced diagnostic/certificate tools remain available.

## Stable 0.1.0

Advanced exposes one automatic discovery/verification action. Legacy AdtAutoDiscovery and AdtTimeoutSeconds remain schema-1 fields for compatibility, with existing values preserved and defaults false/15 for new profiles; their controls are no longer shown. The MCP contract is unchanged.
