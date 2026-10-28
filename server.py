
import json
from typing import Annotated, Any, TypedDict

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from langchain_ollama import ChatOllama
from langchain_core.messages import (
    BaseMessage,
    HumanMessage,
    SystemMessage,
)
from langgraph.graph.message import add_messages
from langgraph.graph import StateGraph, START, END


# --------------------------------------------------
# Modelo
# --------------------------------------------------

llm = ChatOllama(
    model="gpt-oss:120b-cloud",
    temperature=0.2,
    top_k=40,
    top_p=0.9,
)


# --------------------------------------------------
# API
# --------------------------------------------------

api = FastAPI()

# Histórico mantido separadamente para cada agente.
conversation_histories: dict[str, list[BaseMessage]] = {}


class AgentChatRequest(BaseModel):
    agentName: str
    message: str
    memory: list[dict[str, Any]] = Field(default_factory=list)


# --------------------------------------------------
# Estado do LangGraph
# --------------------------------------------------

class State(TypedDict):
    agent_name: str
    agent_memory: list[dict[str, Any]]
    messages: Annotated[list[BaseMessage], add_messages]
    response: str
    user_message: str


# --------------------------------------------------
# Nós do grafo
# --------------------------------------------------

def select_agent(state: State) -> dict:
    return {"agent_name": state["agent_name"]}


def load_memory(state: State) -> dict:
    # A memória vem diretamente da Unity.
    return {"agent_memory": state["agent_memory"]}


def chat(state: State) -> dict:
    pois = "\n".join(
        f"- {poi.get('name', 'Lugar desconhecido')}: "
        f"{json.dumps(poi.get('description', {}), ensure_ascii=False)} "
        f"às {poi.get('time', 'horário desconhecido')}"
        for poi in state["agent_memory"]
    )

    if not pois:
        pois = "Você ainda não registrou nenhum lugar visitado."

    system_prompt = (
        f"Você é {state['agent_name']}, um humano virtual "
        "que está visitando um museu.\n"
        f"Seu identificador é exatamente '{state['agent_name']}'. "
        "Você não tem outro nome próprio.\n\n"
        "Estes são os lugares que você visitou:\n"
        f"{pois}\n\n"
        "Responda em primeira pessoa e somente ao que foi perguntado. "
        "Não invente obras ou lugares que você não visitou. "
        "Responda como um visitante empolgado, de maneira natural "
        "e casual. Nunca diga 'como posso ajudar' nem fale como "
        "um assistente virtual. Não traduza os nomes das obras. "
        "Não liste todos os lugares visitados logo de cara. "
        "Só mencione uma obra específica se o usuário perguntar "
        "sobre ela."
    )

    history = [
        SystemMessage(content=system_prompt),
        *state["messages"],
    ]

    response = llm.invoke(history)

    return {
        "messages": [response],
        "response": response.content,
    }


# --------------------------------------------------
# Construção do grafo
# --------------------------------------------------

def build_graph():
    graph = StateGraph(State)

    graph.add_node("select_agent", select_agent)
    graph.add_node("load_memory", load_memory)
    graph.add_node("chat", chat)

    graph.add_edge(START, "select_agent")
    graph.add_edge("select_agent", "load_memory")
    graph.add_edge("load_memory", "chat")
    graph.add_edge("chat", END)

    return graph.compile()


app = build_graph()


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

    history = conversation_histories.get(agent_name, [])

    state: State = {
        "agent_name": agent_name,
        "agent_memory": request.memory,
        "messages": [*history, HumanMessage(content=message)],
        "response": "",
        "user_message": message,
    }

    try:
        result = app.invoke(state)
    except Exception as error:
        print(f"Erro ao executar LangGraph: {error}")
        raise HTTPException(500, "Erro ao gerar resposta do agente.")

    # Guarda as mensagens para a próxima pergunta.
    conversation_histories[agent_name] = result["messages"]

    answer = result["response"]

    print(f"\n[{agent_name}]")
    print(f"Pergunta: {message}")
    print(f"Resposta: {answer}")

    return {"response": answer}
