# Add MCP servers to other agents and applications

Open **Add to other agents and applications** at the bottom of Manager's **Client integration** screen. The same help is available in the final Basic summary. Choose JSON, TOML or **Enter fields manually**. In-app examples contain the actual Host and settings paths; paths below are illustrative.

## Compatible applications

The target application must run a local **stdio MCP** process on the same computer under the same Windows account as Manager. These commands cannot be added directly to cloud connectors requiring HTTP/SSE URLs. Manager need not stay open. DPAPI-protected passwords are bound to the Windows account.

All clients share the active SAP profiles selected in Manager. Restart client MCP processes after changing active profiles. Do not copy passwords, tokens, SDK PATH settings or SAP Landscape XML contents into the client configuration. Host manages local settings and child-process environments.

Basic displays only components with current eligible verification; RFC additionally requires current readiness. Failed RFC is omitted when only ADT succeeds. Field or dependency changes can invalidate examples; reverify. Advanced displays installed, selected components; complete connection tests separately. Opening/copying help neither connects to SAP nor changes client files.

## JSON configuration

1. Open and back up the target application's MCP configuration.
2. For a new file, use the complete example below. If `mcpServers` already exists, merge the named entries from the help's **server entries** example into that object. Do not paste its outer braces inside the existing object; adjust commas to the existing JSON.
3. Do not duplicate `mcpServers` or server names; preserve unrelated settings. JSON does not support comments.
4. Use actual paths. Do not manually add a component omitted by Basic to bypass verification.

```json
{
  "mcpServers": {
    "sap_adt_mcp": {
      "command": "C:\\SAP MCP Manager\\SapMcpHost.exe",
      "args": [
        "--component",
        "adt",
        "--config",
        "C:\\SAP MCP Data\\config\\settings.json"
      ]
    },
    "sap_rfc_call": {
      "command": "C:\\SAP MCP Manager\\SapMcpHost.exe",
      "args": [
        "--component",
        "rfc",
        "--config",
        "C:\\SAP MCP Data\\config\\settings.json"
      ]
    }
  }
}
```

This example uses the `mcpServers` schema. Follow the target application's schema if it requires another object name or extra fields such as `type: "stdio"`. The Host command and arguments stay the same. Each MCP runs as a separate process.

## TOML configuration

Merge these tables into clients supporting `mcp_servers`. Update existing SAP tables instead of adding duplicate tables. Timeout fields match the existing Codex configuration; use only supported fields in other clients.

```toml
[mcp_servers.sap_adt_mcp]
command = "C:\\SAP MCP Manager\\SapMcpHost.exe"
args = ["--component", "adt", "--config", "C:\\SAP MCP Data\\config\\settings.json"]
startup_timeout_sec = 60
tool_timeout_sec = 180
enabled = true

[mcp_servers.sap_rfc_call]
command = "C:\\SAP MCP Manager\\SapMcpHost.exe"
args = ["--component", "rfc", "--config", "C:\\SAP MCP Data\\config\\settings.json"]
startup_timeout_sec = 60
tool_timeout_sec = 180
enabled = true
```

## Manual fields

Add a separate server for each component in the Custom / Local MCP screen:

| Field | ADT | RFC |
|---|---|---|
| Name | `sap_adt_mcp` | `sap_rfc_call` |
| Transport | `stdio` | `stdio` |
| Command | Full Host executable path | Same Host path |
| Argument 1 | `--component` | `--component` |
| Argument 2 | `adt` | `rfc` |
| Argument 3 | `--config` | `--config` |
| Argument 4 | Full settings file path | Same settings path |

Enter only the executable path in Command. Supply four separate arguments; do not copy JSON escapes or surrounding quotes into individual form fields. No URL or additional environment variables are needed.

## Verification and troubleshooting

Save and restart the target application's MCP processes. Check that the server connects and SAP tools appear. Listing tools does not prove successful SAP logon; complete a user-initiated connection verification in Manager.

If tools are missing, check the Host/settings files exist, the client runs under the same Windows account, local stdio is supported, and the configuration matches its schema. Check Manager preparation/connection results and then Host logs. Distinguish RFC native import errors from SAP logon failures. Do not include passwords or a complete environment dump in support records.
