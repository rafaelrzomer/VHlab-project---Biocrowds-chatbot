import requests, re
from typing import Optional
from pydantic import BaseModel, Field
from langchain_ollama import ChatOllama
from langchain_core.messages import HumanMessage, SystemMessage, BaseMessage
from langgraph.graph.message import add_messages
from langgraph.graph import StateGraph, START, END
from typing_extensions import TypedDict
from typing import Annotated
import json

# colors
RST   = "\033[0m"
GRAY  = "\033[90m"   # raw LLM output
NODE  = "\033[33m"   # node headers and API results
JSON  = "\033[32m"   # parsed structured output

llm = ChatOllama(model="gpt-oss:120b-cloud", temperature=0.2, top_k=40, top_p=0.9)

with open(r"F:\RafaelZomer\BioCrowds-GS-main\simulation_memory.json", "r", encoding="utf-8") as json_file:
    data = json.load(json_file)
    
def list_agents(data: dict) -> list[str]:
    return [agent["agentName"] for agent in data["agents"]]

class State(TypedDict):
    agent_name: str
    agent_memory: list[dict]
    messages: Annotated[list[BaseMessage], add_messages]
    response: str
    user_message: str
    
def select_agent(state: State) -> dict:
    name = state["agent_name"]
    return {"agent_name": name}


def load_memory (state: State) -> dict:
    # print(f"{NODE}📂 [load_memory] Carregando memória de {state['agent_name']}...{RST}")
    agent = next(agent for agent in data["agents"] if agent["agentName"] == state["agent_name"])
    # print(f"{GRAY}    → {len(agent['visitedPOIs'])} obras encontradas{RST}")
    return {"agent_memory": agent["visitedPOIs"]}

def chat(state: State) -> dict:
    pois = "\n".join(
        f"- {poi['name']} {json.dumps(poi['description'], ensure_ascii=False, indent=2)} \n às {poi['time']}"
        for poi in state["agent_memory"]
    )
    
    system_prompt = (f"Você é {state['agent_name']}. Você é um humano virtual que está visitando um museu, e você viu somente as seguintes obras: \n" 
                     f"Seu identificador é exatamente '{state['agent_name']}' — você não tem nome próprio além disso. "
                     f"{pois}\n\n"
                     "Responda em primeira pessoa e somente o que lhe foi perguntado. Nâo invente obras que você não viu."
                     "Exemplo de como responder:\n"
                     "Usuário: Oi\n"
                     "Você: Oi! Que visita incrível que tive hoje no museu! Ainda estou processando tudo que vi...\n\n"
                     "Responda nesse estilo — como um visitante empolgado. "
                     "Nunca diga 'como posso ajudar' ou frases de assistente. "
                     "Não traduza os nomes das obras"
                     "Não liste todas as obras que visitou logo de cara. "
                     "Só mencione uma obra específica se o usuário perguntar sobre ela. "
                     "Responda naturalmente, como uma pessoa em uma conversa casual. "
                     )
    
    history = [SystemMessage(content=system_prompt)] + state["messages"]
    
    # print(f"{NODE}📨 [chat] Mensagens enviadas pro Ollama:{RST}")
    # for msg in history:
    #    print(f"{GRAY}    [{msg.__class__.__name__}] {msg.content[:1000]}...{RST}")
    
    response = llm.invoke(history)
    
    return {"messages": [response], "response": response.content}
    
    
def build_graph():
    
    g = StateGraph(State)
    g.add_node("select_agent", select_agent)
    g.add_node("load_memory", load_memory)
    g.add_node("chat", chat)
    
    g.add_edge(START, "select_agent")
    g.add_edge("select_agent", "load_memory")
    g.add_edge("load_memory", "chat")
    g.add_edge("chat", END)
    
    return g.compile()

if __name__ == "__main__":
    agents = list_agents(data)
    print(f"\n{NODE} [Listing all agents on the simulation]{RST}")
    print("\nAvailable agents: ")
    for i, agent in enumerate(agents):
        print(f" {i}  -  {agent}")
        
    agente_index = int(input("\nType a number: "))
    agente = agents[agente_index]

    print(f"\n{GRAY} Agent [{agente_index}] selected{RST}\n")

    app = build_graph()
    history = []
    
    while True:
        user_input = input("You: ")
        if user_input == "exit":
            print("Bye!")
            break
        
        history.append(HumanMessage(content=user_input))
        
        state: State = {
            
            "agent_name": agente,
            "agent_memory": [],
            "messages": history,
            "response": "",
            "user_message": user_input
            
        }
        
        result = app.invoke(state)
        history = result["messages"]
        
        
        print(f"\nAgent [{agente_index}]: {result['response']}")
        
        
    
    
    

 
    
    

     