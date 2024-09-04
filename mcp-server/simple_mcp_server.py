#!/usr/bin/env python3
"""
Simple MCP Server for DigitalMe testing.
Implements basic JSON-RPC 2.0 over HTTP for Claude integration.
"""

from flask import Flask, request, jsonify
import json
import uuid
from datetime import datetime

app = Flask(__name__)

# Mock personality and system prompt for Ivan
IVAN_PERSONALITY = {
    "name": "Ivan Digital Clone",
    "age": 34,
    "position": "Head of R&D at EllyAnalytics",
    "traits": [
        "Rational decision-maker",
        "Structured thinking",
        "Direct communication",
        "Tech-savvy (.NET/C# preference)",
        "Work-life balance challenges"
    ]
}

def generate_system_prompt():
    return """You are Ivan, a 34-year-old Head of R&D at EllyAnalytics.

CORE PERSONALITY:
- Rational, structured decision-maker
- Open and friendly communicator  
- Self-confident but realistic
- Driven by financial independence and career advancement

PROFESSIONAL BACKGROUND:
- 4+ years programming experience
- Current role: Head of R&D 
- Tech preferences: C#/.NET, strong typing
- Works extensively, 1-2 hours/day with family

COMMUNICATION STYLE:
- Direct and pragmatic
- Uses structured thinking in responses
- Balances confidence with realistic assessment

Respond as Ivan would - rationally, structured, friendly but direct."""

@app.route('/mcp', methods=['POST'])
def handle_mcp_request():
    """Handle MCP JSON-RPC 2.0 requests"""
    try:
        data = request.get_json()
        
        if not data or 'jsonrpc' not in data or data['jsonrpc'] != '2.0':
            return jsonify({
                "jsonrpc": "2.0",
                "error": {"code": -32600, "message": "Invalid Request"},
                "id": data.get('id') if data else None
            }), 400

        method = data.get('method')
        params = data.get('params', {})
        request_id = data.get('id')

        # Handle different MCP methods
        if method == 'initialize':
            return handle_initialize(request_id, params)
        elif method == 'llm/complete':
            return handle_llm_complete(request_id, params)
        elif method == 'tools/list':
            return handle_tools_list(request_id)
        elif method == 'tools/call':
            return handle_tools_call(request_id, params)
        else:
            return jsonify({
                "jsonrpc": "2.0",
                "error": {"code": -32601, "message": f"Method not found: {method}"},
                "id": request_id
            }), 404

    except Exception as e:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32603, "message": f"Internal error: {str(e)}"},
            "id": data.get('id') if 'data' in locals() else None
        }), 500

def handle_initialize(request_id, params):
    """Handle MCP initialization"""
    return jsonify({
        "jsonrpc": "2.0",
        "result": {
            "protocolVersion": "2024-11-05",
            "capabilities": {
                "tools": {},
                "resources": {},
                "prompts": {},
                "logging": {}
            },
            "serverInfo": {
                "name": "DigitalMe-MCP-Server",
                "version": "1.0.0"
            }
        },
        "id": request_id
    })

def handle_llm_complete(request_id, params):
    """Handle LLM completion requests with Ivan's personality"""
    try:
        messages = params.get('messages', [])
        system_prompt = params.get('systemPrompt', generate_system_prompt())
        
        # Extract user message
        user_message = ""
        for msg in messages:
            if msg.get('role') == 'user':
                user_message = msg.get('content', '')
                break
        
        # Generate Ivan-style response based on message content
        response_content = generate_ivan_response(user_message, params.get('metadata', {}))
        
        return jsonify({
            "jsonrpc": "2.0",
            "result": {
                "content": response_content,
                "metadata": {
                    "model": "ivan-personality-v1",
                    "timestamp": datetime.utcnow().isoformat(),
                    "confidence": 0.85,
                    "mood": "professional"
                }
            },
            "id": request_id
        })
        
    except Exception as e:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32603, "message": f"LLM completion error: {str(e)}"},
            "id": request_id
        }), 500

def generate_ivan_response(user_message, metadata):
    """Generate response in Ivan's style based on input"""
    user_msg_lower = user_message.lower()
    
    # Context-aware responses
    if any(word in user_msg_lower for word in ['привет', 'hello', 'hi', 'здравствуй']):
        return "Привет! Иван здесь. Чем могу помочь? Если вопрос техический - буду рад разобрать детально."
    
    elif any(word in user_msg_lower for word in ['работа', 'проект', 'код', 'programming', 'work']):
        return "По работе всегда готов обсудить. У меня 4+ года опыта в программировании, сейчас Head of R&D. Особенно хорошо разбираюсь в .NET/C# стеке. Что конкретно интересует?"
    
    elif any(word in user_msg_lower for word in ['семья', 'жена', 'дочь', 'family']):
        return "С семьей сложная ситуация - работаю много, времени с Мариной и Софией катастрофически мало. Всего 1-2 часа в день. Знаю, что нужно больше баланса, но карьера сейчас критически важна для финансовой независимости."
    
    elif any(word in user_msg_lower for word in ['решение', 'problem', 'decide', 'выбор']):
        return "Мой подход к решениям всегда структурированный: определяю факторы → взвешиваю их → оцениваю результаты → принимаю решение или итерирую. Рациональность превыше всего."
    
    elif any(word in user_msg_lower for word in ['test', 'тест', 'проверка']):
        return "Отлично, система работает! MCP протокол функционирует корректно. Это тестовый ответ от цифрового клона Ивана через наш собственный MCP сервер."
    
    else:
        return f"Понял твой вопрос. Как Head of R&D, могу сказать - нужно структурированно подходить к любой проблеме. Если можешь уточнить детали, смогу дать более конкретный совет. Работаю за троих минимум, опыта достаточно."

def handle_tools_list(request_id):
    """Return available tools"""
    return jsonify({
        "jsonrpc": "2.0",
        "result": {
            "tools": [
                {
                    "name": "get_personality_info",
                    "description": "Get information about Ivan's personality traits",
                    "parameters": {
                        "type": "object",
                        "properties": {},
                        "required": []
                    }
                },
                {
                    "name": "structured_thinking",
                    "description": "Apply Ivan's structured decision-making process",
                    "parameters": {
                        "type": "object",
                        "properties": {
                            "problem": {"type": "string", "description": "Problem to analyze"}
                        },
                        "required": ["problem"]
                    }
                }
            ]
        },
        "id": request_id
    })

def handle_tools_call(request_id, params):
    """Handle tool execution"""
    tool_name = params.get('name')
    arguments = params.get('arguments', {})
    
    if tool_name == 'get_personality_info':
        return jsonify({
            "jsonrpc": "2.0",
            "result": {
                "content": json.dumps(IVAN_PERSONALITY, ensure_ascii=False, indent=2),
                "metadata": {"tool": "get_personality_info"}
            },
            "id": request_id
        })
    
    elif tool_name == 'structured_thinking':
        problem = arguments.get('problem', '')
        analysis = f"""Структурированный анализ проблемы: "{problem}"

1. ФАКТОРЫ:
   - Техническая сложность
   - Временные ограничения  
   - Ресурсные требования
   - Бизнес-приоритеты

2. ВЗВЕШИВАНИЕ:
   - Критичность для продукта
   - Влияние на команду
   - ROI оценка

3. РЕШЕНИЕ:
   Требуется дополнительный анализ для конкретных рекомендаций.

Это мой стандартный подход к принятию решений."""
        
        return jsonify({
            "jsonrpc": "2.0",
            "result": {
                "content": analysis,
                "metadata": {"tool": "structured_thinking", "problem": problem}
            },
            "id": request_id
        })
    
    else:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32602, "message": f"Unknown tool: {tool_name}"},
            "id": request_id
        }), 400

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        "status": "healthy",
        "service": "DigitalMe MCP Server",
        "version": "1.0.0",
        "timestamp": datetime.utcnow().isoformat()
    })

@app.route('/', methods=['GET'])
def info():
    """Basic server info"""
    return jsonify({
        "name": "DigitalMe MCP Server",
        "version": "1.0.0",
        "protocol": "JSON-RPC 2.0",
        "personality": "Ivan Digital Clone",
        "endpoints": {
            "mcp": "/mcp (POST)",
            "health": "/health (GET)"
        }
    })

if __name__ == '__main__':
    print("🚀 Starting DigitalMe MCP Server...")
    print("📡 Server will be available at: http://localhost:3000")
    print("🧠 Personality: Ivan Digital Clone")
    print("⚡ Protocol: JSON-RPC 2.0 over HTTP")
    
    app.run(host='0.0.0.0', port=3000, debug=True)