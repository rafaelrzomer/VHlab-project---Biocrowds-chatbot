# BioCrowds
 BioCrowds based on https://github.com/Virtual-Humans-Lab/BioCrowds

This version includes some functionalities including:
- Spawn Area for Agents. Allows repeated spawn in the simulation;
- Goal list for agents. Including wait times and a distance threshold;
- 2 methods for spawning Markers (Grid and Dart Throwing);


# WebCrowds
 WebCrowds is available in the WebCrowds brach (https://github.com/Virtual-Humans-Lab/BioCrowds-GS/tree/WebCrowds)


## Executar o servidor de conversa

A conversa dos agentes usa dois processos: o Ollama, que fornece o modelo de linguagem, e a API Python, que recebe mensagens da Unity. Mantenha ambos em execução durante a simulação. A Unity envia as mensagens para `http://127.0.0.1:8000/chat`.

### 1. Preparar o ambiente Python

No macOS ou Linux, abra um terminal na pasta principal do projeto e execute:

```bash
python3 -m venv .venv
source .venv/bin/activate
python -m pip install fastapi uvicorn langchain-core langchain-ollama langgraph
```

No Windows PowerShell, use:

```powershell
py -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install fastapi uvicorn langchain-core langchain-ollama langgraph
```

### 2. Iniciar o Ollama

Instale e abra o [Ollama](https://ollama.com/download). O projeto usa o modelo `gpt-oss:120b-cloud`, que requer uma conta Ollama conectada. Em um terminal, execute:

```bash
ollama signin
ollama run gpt-oss:120b-cloud
```

Se o Ollama já estiver aberto e conectado, basta confirmar que a API local responde:

```bash
curl http://127.0.0.1:11434/api/tags
```

### 3. Iniciar a API Python

Abra outro terminal e inicie o servidor a partir da pasta `Assets/Code`, usando o interpretador do ambiente virtual.

macOS ou Linux:

```bash
cd Assets/Code
../../.venv/bin/python -m uvicorn server:api --host 127.0.0.1 --port 8000
```

Windows PowerShell:

```powershell
cd Assets/Code
..\..\.venv\Scripts\python.exe -m uvicorn server:api --host 127.0.0.1 --port 8000
```

Quando aparecer `Uvicorn running on http://127.0.0.1:8000`, a API está pronta. Também é possível abrir `http://127.0.0.1:8000/docs` no navegador para conferir a documentação da API. Deixe esse terminal aberto enquanto usa a Unity. Para encerrar o servidor, pressione `Ctrl+C`.
