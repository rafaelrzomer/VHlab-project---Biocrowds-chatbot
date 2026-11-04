
import json
from typing import Annotated, TypedDict, Any

from langchain_ollama import ChatOllama
from langchain_core.messages import BaseMessage, SystemMessage
from langgraph.graph.message import add_messages
from langgraph.graph import StateGraph, START, END


# --------------------------------------------------
# Modelo
# --------------------------------------------------

llm = ChatOllama(
    model="gpt-oss:120b-cloud",
    temperature=0.2,
    top_k=40,
    top_p=0.9
)


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
    # A memória é recebida diretamente da Unity.
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

    # --------------------------------------------------
    # SYSTEM PROMPT
    # --------------------------------------------------

    system_prompt = (
        f"Você é {state['agent_name']}, um humano virtual "
        "que está visitando um museu.\n"

        f"Seu identificador é exatamente '{state['agent_name']}'. "
        "Você não tem outro nome próprio.\n\n"

        "Estes são os lugares que você visitou:\n"
        f"{pois}\n\n"

        "Responda em primeira pessoa e somente ao que foi perguntado. "
        "Não invente obras ou lugares que você não visitou. "
        "Responda como um visitante empolgado, de maneira natural e casual. Nunca diga 'como posso ajudar' nem fale como um assistente virtual." 
        "Não traduza os nomes das obras."
        "Não liste todos os lugares visitados logo de cara. "
        "Só mencione uma obra específica se o usuário perguntar sobre ela "
        "Não use formatação Markdown. "
        "Não use asteriscos para destacar nomes de obras ou palavras. "
        "Não utilize negrito, itálico ou títulos formatados. "
        "Nunca utilize o caractere de travessão (— ou –)"
        "Dê respostas curtas"
    )

    history = [
        SystemMessage(content=system_prompt),
        *state["messages"]
    ]

    response = llm.invoke(history)

    return {
        "messages": [response],
        "response": response.content
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
