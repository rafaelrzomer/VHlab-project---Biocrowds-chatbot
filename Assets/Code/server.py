
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import Any

from langchain_core.messages import BaseMessage, HumanMessage

from AgentChat import build_graph, State


# --------------------------------------------------
# API
# --------------------------------------------------

api = FastAPI()


# --------------------------------------------------
# Grafo do agente
# --------------------------------------------------

app = build_graph()


# Histórico separado por agente
conversation_histories: dict[str, list[BaseMessage]] = {}


# --------------------------------------------------
# Dados recebidos da Unity
# --------------------------------------------------

class AgentChatRequest(BaseModel):
    agentName: str
    message: str
    memory: list[dict[str, Any]] = Field(default_factory=list)


# --------------------------------------------------
# Endpoint chamado pela Unity
# --------------------------------------------------

@api.post("/chat")
def chat_endpoint(request: AgentChatRequest):

    agent_name = request.agentName.strip()
    message = request.message.strip()

    if not agent_name:
        raise HTTPException(400, "Nome do agente vazio.")

    if not message:
        raise HTTPException(400, "A mensagem está vazia.")

    # Recupera o histórico do agente
    history = conversation_histories.get(agent_name, [])

    # Monta o estado enviado ao LangGraph
    state: State = {
        "agent_name": agent_name,
        "agent_memory": request.memory,
        "messages": [
            *history,
            HumanMessage(content=message)
        ],
        "response": "",
        "user_message": message
    }

    try:
        result = app.invoke(state)

    except Exception as error:
        print(f"Erro ao executar LangGraph: {error}")
        raise HTTPException(
            500,
            "Erro ao gerar resposta do agente."
        )

    # Atualiza o histórico
    conversation_histories[agent_name] = result["messages"]

    answer = result["response"]

    print(f"\n[{agent_name}]")
    print(f"Pergunta: {message}")
    print(f"Resposta: {answer}")

    return {"response": answer}
