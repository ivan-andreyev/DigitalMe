# DigitalMe MCP Server

Simple MCP (Model Context Protocol) Server implementation for testing DigitalMe's MCPClient integration.

## Features

- **JSON-RPC 2.0 over HTTP** - Full MCP protocol compliance
- **Ivan Personality Integration** - Responds as Ivan's digital clone
- **Tool Support** - Personality info and structured thinking tools
- **Health Monitoring** - Health check endpoints
- **Docker Support** - Containerized deployment

## Quick Start

### Using Docker (Recommended)

```bash
# Start the MCP server
cd mcp-server
docker-compose up -d

# Check health
curl http://localhost:3000/health

# Test basic functionality
curl http://localhost:3000/
```

### Manual Setup

```bash
# Install dependencies
pip install -r requirements.txt

# Run server
python simple_mcp_server.py
```

## API Endpoints

### MCP Protocol Endpoint
- **POST /mcp** - Main MCP JSON-RPC 2.0 endpoint

### Health & Info
- **GET /health** - Health check
- **GET /** - Server information

## MCP Methods Supported

### Core Methods
- `initialize` - MCP initialization handshake
- `llm/complete` - LLM completion with Ivan's personality
- `tools/list` - List available tools
- `tools/call` - Execute tools

### Available Tools
- `get_personality_info` - Get Ivan's personality traits
- `structured_thinking` - Apply Ivan's decision-making process

## Testing with DigitalMe

1. Start the MCP server: `docker-compose up -d`
2. Configure DigitalMe to use: `http://localhost:3000/mcp`
3. Test integration through DigitalMe's MCPClient

## Sample Requests

### Initialize MCP Connection
```json
POST /mcp
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {"tools": {}}
  },
  "id": "1"
}
```

### LLM Completion
```json
POST /mcp
{
  "jsonrpc": "2.0", 
  "method": "llm/complete",
  "params": {
    "messages": [{"role": "user", "content": "Привет, как дела?"}],
    "maxTokens": 1000
  },
  "id": "2"
}
```

### List Tools
```json
POST /mcp
{
  "jsonrpc": "2.0",
  "method": "tools/list", 
  "params": {},
  "id": "3"
}
```

## Configuration

- **Port**: 3000 (configurable via Docker)
- **Host**: 0.0.0.0 (all interfaces)
- **Protocol**: JSON-RPC 2.0
- **Transport**: HTTP

## Development

The server implements Ivan's personality with context-aware responses for:
- Greetings and casual conversation
- Work and technical questions  
- Family and personal topics
- Decision-making and problem-solving

## Integration with DigitalMe

This MCP server is designed to work with DigitalMe's `MCPClient` and `MCPServiceProper` implementations, providing a complete end-to-end MCP integration for testing and development.